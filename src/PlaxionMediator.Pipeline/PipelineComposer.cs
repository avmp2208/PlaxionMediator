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
/// EXPERIMENT H3: Field-staged executor with pre-bound RequestHandlerDelegate methods.
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

        // Thin non-async closure: executor state is created on each invoke so the returned
        // delegate remains safe to call more than once (matches prior Compose semantics).
        return () => ExecuteAsync(request, behaviors, handler, cancellationToken);
    }

    /// <summary>
    /// Executes the composed pipeline and returns the response.
    /// H3 EXPERIMENT: Try field-staged executor for shallow depths (1-5 behaviors).
    /// Fall back to index trampoline for deeper chains.
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

        // H3: Use field-staged executor for common depths (1-5 behaviors)
        // This eliminates index trampoline overhead for typical pipelines
        if (behaviors.Count <= 5)
        {
            return PipelineExecutor<TRequest, TResponse>
                .Execute(request, behaviors, handler, handlerInstance: null, cancellationToken);
        }

        // Fallback to index trampoline for deep chains (>5)
        return PipelineRunner<TRequest, TResponse>
            .Rent(request, behaviors, handler, handlerInstance: null, cancellationToken)
            .Run();
    }

    /// <summary>
    /// Executes the composed pipeline using a resolved <see cref="IRequestHandler{TRequest,TResponse}"/>
    /// as the terminal invoker (avoids allocating a method-group <see cref="Func{T,TResult}"/> per Send).
    /// Intended for generated dispatch code.
    /// H3 EXPERIMENT: Use field-staged executor for shallow depths.
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

        // H3: Use field-staged executor for common depths (1-5 behaviors)
        if (behaviors.Count <= 5)
        {
            return PipelineExecutor<TRequest, TResponse>
                .Execute(request, behaviors, handlerFunc: null, handler, cancellationToken);
        }

        // Fallback to index trampoline for deep chains (>5)
        return PipelineRunner<TRequest, TResponse>
            .Rent(request, behaviors, handlerFunc: null, handler, cancellationToken)
            .Run();
    }

    /// <summary>
    /// H3 EXPERIMENT: Field-staged pipeline executor with pre-bound RequestHandlerDelegate methods.
    /// Replaces index trampoline for depths 1-5. Each "Next" method is bound once in the ctor,
    /// eliminating per-call index checks and list indexing overhead.
    /// Pooled (TLS + ConcurrentBag) for zero steady-state allocation.
    /// </summary>
    private sealed class PipelineExecutor<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const int MaxPoolSize = 64;
        private static readonly ConcurrentBag<PipelineExecutor<TRequest, TResponse>> Pool = new();

        [ThreadStatic]
        private static PipelineExecutor<TRequest, TResponse>? t_tls;

        // Mutable state set per Execute
        private TRequest _request = default!;
        private IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> _behaviors = null!;
        private Func<TRequest, CancellationToken, ValueTask<TResponse>>? _handlerFunc;
        private IRequestHandler<TRequest, TResponse>? _handlerInstance;
        private CancellationToken _cancellationToken;

        // Pre-bound delegates (immutable after ctor, reused across invocations)
        private readonly RequestHandlerDelegate<TResponse> _n0, _n1, _n2, _n3, _n4, _n5;

        private PipelineExecutor()
        {
            // Bind all next delegates once for the lifetime of this pooled instance
            _n0 = Next0;
            _n1 = Next1;
            _n2 = Next2;
            _n3 = Next3;
            _n4 = Next4;
            _n5 = Next5;
        }

        public static ValueTask<TResponse> Execute(
            TRequest request,
            IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
            Func<TRequest, CancellationToken, ValueTask<TResponse>>? handlerFunc,
            IRequestHandler<TRequest, TResponse>? handlerInstance,
            CancellationToken cancellationToken)
        {
            PipelineExecutor<TRequest, TResponse>? executor = t_tls;
            if (executor is not null)
            {
                t_tls = null;
            }
            else if (!Pool.TryTake(out executor))
            {
                executor = new PipelineExecutor<TRequest, TResponse>();
            }

            executor._request = request;
            executor._behaviors = behaviors;
            executor._handlerFunc = handlerFunc;
            executor._handlerInstance = handlerInstance;
            executor._cancellationToken = cancellationToken;

            ValueTask<TResponse> result;
            IPipelineBehavior<TRequest, TResponse> firstBehavior = behaviors[0];
            try
            {
                // Start with behavior 0
                result = firstBehavior.Handle(request, executor._n1, cancellationToken);
            }
            catch (Exception ex)
            {
                executor.Return();
                return ThrowSyncException(ex, firstBehavior);
            }

            if (result.IsCompletedSuccessfully)
            {
                TResponse value = result.Result;
                executor.Return();
                return new ValueTask<TResponse>(value);
            }

            // Async path: must await and handle exceptions
            return AwaitAndReturn(result, executor, firstBehavior);
        }

        private void Return()
        {
            _request = default!;
            _behaviors = null!;
            _handlerFunc = null;
            _handlerInstance = null;
            _cancellationToken = default;

            if (t_tls is null)
            {
                t_tls = this;
            }
            else if (Pool.Count < MaxPoolSize)
            {
                Pool.Add(this);
            }
        }

        // Pre-bound methods for each depth (Next0 is never called - starts with behavior[0])
        private ValueTask<TResponse> Next0() => throw new InvalidOperationException("Next0 should never be called");

        private ValueTask<TResponse> Next1()
        {
            if (_behaviors.Count <= 1)
            {
                return InvokeHandler();
            }
            try
            {
                return _behaviors[1].Handle(_request, _n2, _cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowSyncException(ex, _behaviors[1]);
            }
        }

        private ValueTask<TResponse> Next2()
        {
            if (_behaviors.Count <= 2)
            {
                return InvokeHandler();
            }
            try
            {
                return _behaviors[2].Handle(_request, _n3, _cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowSyncException(ex, _behaviors[2]);
            }
        }

        private ValueTask<TResponse> Next3()
        {
            if (_behaviors.Count <= 3)
            {
                return InvokeHandler();
            }
            try
            {
                return _behaviors[3].Handle(_request, _n4, _cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowSyncException(ex, _behaviors[3]);
            }
        }

        private ValueTask<TResponse> Next4()
        {
            if (_behaviors.Count <= 4)
            {
                return InvokeHandler();
            }
            try
            {
                return _behaviors[4].Handle(_request, _n5, _cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowSyncException(ex, _behaviors[4]);
            }
        }

        private ValueTask<TResponse> Next5()
        {
            // Max depth for field-staged executor is 5
            return InvokeHandler();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<TResponse> InvokeHandler()
        {
            return _handlerInstance is not null
                ? _handlerInstance.Handle(_request, _cancellationToken)
                : _handlerFunc!(_request, _cancellationToken);
        }

        private static async ValueTask<TResponse> AwaitAndReturn(
            ValueTask<TResponse> valueTask,
            PipelineExecutor<TRequest, TResponse> executor,
            IPipelineBehavior<TRequest, TResponse> behavior)
        {
            try
            {
                TResponse result = await valueTask.ConfigureAwait(false);
                executor.Return();
                return result;
            }
            catch (Exception ex)
            {
                executor.Return();
                ThrowAsyncException(ex, behavior);
                return default!; // unreachable
            }
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
                throw ex;
            }

            throw new PipelineExecutionException(
                $"Error executing behavior '{behavior.GetType().Name}' for request '{typeof(TRequest).Name}'.",
                ex,
                behavior.GetType().Name);
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

    /// <summary>
    /// Index-based pipeline trampoline. Each <see cref="Next"/> invocation advances one stage
    /// (behavior or terminal handler). Exception wrapping matches the historical Compose semantics.
    /// Instances are pooled (TLS + ConcurrentBag) so the class + Next delegate are not per-call allocs.
    /// Implements <see cref="IValueTaskSource{TResult}"/> so the async completion path can return the
    /// instance to the pool from <see cref="GetResult"/> without an extra async state machine.
    /// H3: This is now the FALLBACK for deep chains (>5 behaviors).
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
