using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks.Sources;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;

namespace PlaxionMediator.Pipeline;

/// <summary>
/// Builds the delegate chain of behaviors terminating in a handler invocation.
/// Consumed by generated dispatch code and tests.
/// </summary>
public static class PipelineComposer
{
    /// <summary>
    /// Composes <paramref name="behaviors"/> around <paramref name="handler"/> into a single delegate.
    /// Behaviors are applied in order: the first behavior is outermost (closest to the caller).
    /// </summary>
    public static RequestHandlerDelegate<TResponse> Compose<TRequest, TResponse>(
        TRequest request,
        IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
        Func<TRequest, CancellationToken, ValueTask<TResponse>> handler,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(handler);

        // Thin non-async closure: runner state is created on each invoke so the returned
        // delegate remains safe to call more than once (matches prior Compose semantics).
        return () => ExecuteAsync(request, behaviors, handler, cancellationToken);
    }

    /// <summary>
    /// Executes the composed pipeline and returns the response.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
        Func<TRequest, CancellationToken, ValueTask<TResponse>> handler,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(handler);

        if (behaviors.Count == 0)
        {
            return handler(request, cancellationToken);
        }

        // Pooled trampoline: reuses the runner instance and its cached Next delegate across Sends.
        // Incomplete async paths complete via IValueTaskSource on the runner (no extra async SM).
        return PipelineRunner<TRequest, TResponse>
            .Rent(request, behaviors, handler, handlerInstance: null, cancellationToken)
            .Run();
    }

    /// <summary>
    /// Executes the composed pipeline using a resolved <see cref="IRequestHandler{TRequest,TResponse}"/>
    /// as the terminal invoker (avoids allocating a method-group <see cref="Func{T,TResult}"/> per Send).
    /// Intended for generated dispatch code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
        IRequestHandler<TRequest, TResponse> handler,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(behaviors);
        ArgumentNullException.ThrowIfNull(handler);

        if (behaviors.Count == 0)
        {
            return handler.Handle(request, cancellationToken);
        }

        // Single entry (no ExecuteCore hop): rent pooled runner and run.
        return PipelineRunner<TRequest, TResponse>
            .Rent(request, behaviors, handlerFunc: null, handler, cancellationToken)
            .Run();
    }

    /// <summary>
    /// Index-based pipeline trampoline. Each <see cref="Next"/> invocation advances one stage
    /// (behavior or terminal handler). Exception wrapping matches the historical Compose semantics.
    /// Instances are pooled (TLS + ConcurrentBag) so the class + Next delegate are not per-call allocs.
    /// Implements <see cref="IValueTaskSource{TResult}"/> so the async completion path can return the
    /// instance to the pool from <see cref="GetResult"/> without an extra async state machine.
    /// </summary>
    private sealed class PipelineRunner<TRequest, TResponse> : IValueTaskSource<TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const int MaxPoolSize = 64;

        private static readonly ConcurrentBag<PipelineRunner<TRequest, TResponse>> Pool = new();

        [ThreadStatic]
        private static PipelineRunner<TRequest, TResponse>? t_tls;

        private TRequest _request = default!;
        private IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> _behaviors = null!;
        private Func<TRequest, CancellationToken, ValueTask<TResponse>>? _handlerFunc;
        private IRequestHandler<TRequest, TResponse>? _handlerInstance;
        private CancellationToken _cancellationToken;
        private RequestHandlerDelegate<TResponse>? _next;
        private Action? _continuation;
        private ValueTask<TResponse> _pending;
        private ManualResetValueTaskSourceCore<TResponse> _vts;
        private int _index;

        private PipelineRunner()
        {
            // Bind once for the lifetime of the pooled instance (no per-call delegate allocs).
            _next = Next;
            _continuation = OnPendingCompleted;
            _vts.RunContinuationsAsynchronously = false;
        }

        public static PipelineRunner<TRequest, TResponse> Rent(
            TRequest request,
            IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
            Func<TRequest, CancellationToken, ValueTask<TResponse>>? handlerFunc,
            IRequestHandler<TRequest, TResponse>? handlerInstance,
            CancellationToken cancellationToken)
        {
            PipelineRunner<TRequest, TResponse>? runner = t_tls;
            if (runner is not null)
            {
                t_tls = null;
            }
            else if (!Pool.TryTake(out runner))
            {
                runner = new PipelineRunner<TRequest, TResponse>();
            }

            runner._request = request;
            runner._behaviors = behaviors;
            runner._handlerFunc = handlerFunc;
            runner._handlerInstance = handlerInstance;
            runner._cancellationToken = cancellationToken;
            runner._index = 0;
            runner._vts.Reset();
            return runner;
        }

        public void Return()
        {
            _request = default!;
            _behaviors = null!;
            _handlerFunc = null;
            _handlerInstance = null;
            _cancellationToken = default;
            _index = 0;
            _pending = default;
            // Keep _next / _continuation bound to this instance.

            if (t_tls is null)
            {
                t_tls = this;
            }
            else if (Pool.Count < MaxPoolSize)
            {
                Pool.Add(this);
            }
        }

        /// <summary>
        /// Runs the pipeline from stage 0. Sync-completed paths return the runner to the pool immediately.
        /// Incomplete paths return a <see cref="ValueTask{TResult}"/> backed by this instance.
        /// </summary>
        public ValueTask<TResponse> Run()
        {
            ValueTask<TResponse> result;
            try
            {
                result = Next();
            }
            catch
            {
                // Preserve historical sync-throw behavior from ThrowSyncException / handler.
                Return();
                throw;
            }

            if (result.IsCompletedSuccessfully)
            {
                TResponse value = result.Result;
                Return();
                return new ValueTask<TResponse>(value);
            }

            if (result.IsCompleted)
            {
                // Completed faulted/canceled: ValueTask is single-consumption, so observe then re-wrap.
                ExceptionDispatchInfo edi;
                try
                {
                    _ = result.Result;
                    Return();
                    return result;
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }

                Return();
                return new ValueTask<TResponse>(Task.FromException<TResponse>(edi.SourceException));
            }

            // Incomplete: this runner becomes the IValueTaskSource. Returned in GetResult.
            _pending = result;
            ValueTaskAwaiter<TResponse> awaiter = result.GetAwaiter();
            awaiter.OnCompleted(_continuation!);
            return new ValueTask<TResponse>(this, _vts.Version);
        }

        private void OnPendingCompleted()
        {
            try
            {
                TResponse value = _pending.GetAwaiter().GetResult();
                _vts.SetResult(value);
            }
            catch (Exception ex)
            {
                _vts.SetException(ex);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _vts.GetStatus(token);

        public void OnCompleted(
            Action<object?> continuation,
            object? state,
            short token,
            ValueTaskSourceOnCompletedFlags flags) =>
            _vts.OnCompleted(continuation, state, token, flags);

        public TResponse GetResult(short token)
        {
            try
            {
                return _vts.GetResult(token);
            }
            finally
            {
                Return();
            }
        }

        public ValueTask<TResponse> Next()
        {
            int index = _index;
            if ((uint)index >= (uint)_behaviors.Count)
            {
                return _handlerInstance is not null
                    ? _handlerInstance.Handle(_request, _cancellationToken)
                    : _handlerFunc!(_request, _cancellationToken);
            }

            _index = index + 1;
            IPipelineBehavior<TRequest, TResponse> behavior = _behaviors[index];
            // _next is bound once in the ctor for pooled reuse (never null after construction).
            RequestHandlerDelegate<TResponse> next = _next!;

            // Keep the try region minimal so the completed-success path stays easy to inline.
            // Exception allocation / message formatting lives in NoInlining helpers off the hot path.
            ValueTask<TResponse> valueTask;
            try
            {
                valueTask = behavior.Handle(_request, next, _cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowSyncException(ex, behavior);
            }

            if (valueTask.IsCompletedSuccessfully)
            {
                return valueTask;
            }

            return AwaitWithExceptionWrapping(valueTask, behavior);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask<TResponse> ThrowSyncException(
            Exception ex,
            IPipelineBehavior<TRequest, TResponse> behavior)
        {
            if (ex is OperationCanceledException)
            {
                throw ex;
            }

            if (ex is PlaxionMediatorException)
            {
                // Intentional framework exceptions (e.g. validation) must surface unwrapped.
                throw ex;
            }

            throw new PipelineExecutionException(
                $"Error executing behavior '{behavior.GetType().Name}' for request '{typeof(TRequest).Name}'.",
                ex,
                behavior.GetType().Name);
        }

        private static async ValueTask<TResponse> AwaitWithExceptionWrapping(
            ValueTask<TResponse> valueTask,
            IPipelineBehavior<TRequest, TResponse> behavior)
        {
            try
            {
                return await valueTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ThrowAsyncException(ex, behavior);
                return default!; // unreachable
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAsyncException(
            Exception ex,
            IPipelineBehavior<TRequest, TResponse> behavior)
        {
            if (ex is OperationCanceledException || ex is PlaxionMediatorException)
            {
                throw ex;
            }

            throw new PipelineExecutionException(
                $"Error executing behavior '{behavior.GetType().Name}' for request '{typeof(TRequest).Name}'.",
                ex,
                behavior.GetType().Name);
        }
    }
}
