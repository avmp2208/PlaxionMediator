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
    /// Internal marker wrapping an exception raised by the terminal handler invocation. Pipeline
    /// stages recognize this marker and pass it through unchanged (never wrapping it into a
    /// <see cref="PipelineExecutionException"/>), since a handler fault is not a behavior fault -
    /// it must reach the caller/middleware as the original, unmapped exception. Unwrapped exactly
    /// once at the <see cref="ExecuteAsync{TRequest,TResponse}(TRequest,IReadOnlyList{IPipelineBehavior{TRequest,TResponse}},Func{TRequest,CancellationToken,ValueTask{TResponse}},CancellationToken)"/> boundary.
    /// </summary>
    private sealed class HandlerFaultException : Exception
    {
        public HandlerFaultException(Exception inner)
            : base(inner.Message, inner)
        {
        }
    }

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
        try
        {
            ValueTask<TResponse> result = behaviors.Count <= 5
                ? PipelineExecutor<TRequest, TResponse>
                    .Execute(request, behaviors, handler, handlerInstance: null, cancellationToken)
                // Fallback to index trampoline for deep chains (>5)
                : PipelineRunner<TRequest, TResponse>
                    .Rent(request, behaviors, handler, handlerInstance: null, cancellationToken)
                    .Run();
            return result.IsCompletedSuccessfully ? result : UnwrapHandlerFault(result);
        }
        catch (HandlerFaultException hfe)
        {
            ExceptionDispatchInfo.Capture(hfe.InnerException!).Throw();
            throw;
        }
    }

    /// <summary>
    /// Awaits the pipeline result, unwrapping a <see cref="HandlerFaultException"/> back to the
    /// original handler exception so it surfaces to the caller raw and unmapped.
    /// </summary>
    private static async ValueTask<TResponse> UnwrapHandlerFault<TResponse>(ValueTask<TResponse> task)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (HandlerFaultException hfe)
        {
            ExceptionDispatchInfo.Capture(hfe.InnerException!).Throw();
            throw;
        }
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
        try
        {
            ValueTask<TResponse> result = behaviors.Count <= 5
                ? PipelineExecutor<TRequest, TResponse>
                    .Execute(request, behaviors, handlerFunc: null, handler, cancellationToken)
                // Fallback to index trampoline for deep chains (>5)
                : PipelineRunner<TRequest, TResponse>
                    .Rent(request, behaviors, handlerFunc: null, handler, cancellationToken)
                    .Run();
            return result.IsCompletedSuccessfully ? result : UnwrapHandlerFault(result);
        }
        catch (HandlerFaultException hfe)
        {
            ExceptionDispatchInfo.Capture(hfe.InnerException!).Throw();
            throw;
        }
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

            // Async path: must await, attribute any fault to this stage (unless already mapped,
            // or thrown by a deeper stage/the handler, which ThrowAsyncException leaves untouched),
            // and return the executor to the pool exactly once.
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

        private ValueTask<TResponse> Next1() => InvokeStage(1, _n2);

        private ValueTask<TResponse> Next2() => InvokeStage(2, _n3);

        private ValueTask<TResponse> Next3() => InvokeStage(3, _n4);

        private ValueTask<TResponse> Next4() => InvokeStage(4, _n5);

        // Invokes behaviors[index] with the given continuation. Awaits and attributes any fault
        // to THIS stage only when the stage's own ValueTask completes asynchronously, mirroring
        // PipelineRunner's per-stage wrapping so exceptions raised deeper in the chain (by another
        // behavior or by the terminal handler) are not mislabeled as this stage's fault.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<TResponse> InvokeStage(int index, RequestHandlerDelegate<TResponse> next)
        {
            if (_behaviors.Count <= index)
            {
                return InvokeHandler();
            }

            IPipelineBehavior<TRequest, TResponse> behavior = _behaviors[index];
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

            return AwaitStage(valueTask, behavior);
        }

        private static async ValueTask<TResponse> AwaitStage(
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

        private ValueTask<TResponse> Next5()
        {
            // Max depth for field-staged executor is 5
            return InvokeHandler();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask<TResponse> InvokeHandler()
        {
            ValueTask<TResponse> valueTask;
            try
            {
                valueTask = _handlerInstance is not null
                    ? _handlerInstance.Handle(_request, _cancellationToken)
                    : _handlerFunc!(_request, _cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not PlaxionMediatorException)
            {
                throw new HandlerFaultException(ex);
            }

            return valueTask.IsCompletedSuccessfully ? valueTask : MarkAsyncHandlerFault(valueTask);
        }

        // A handler fault (unlike a behavior fault) must never be wrapped into a
        // PipelineExecutionException - it is tagged here and left untouched by every stage's
        // Throw*Exception helper below, then unwrapped exactly once at the ExecuteAsync boundary.
        private static async ValueTask<TResponse> MarkAsyncHandlerFault(ValueTask<TResponse> valueTask)
        {
            try
            {
                return await valueTask.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not PlaxionMediatorException)
            {
                throw new HandlerFaultException(ex);
            }
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
            if (ex is OperationCanceledException or PlaxionMediatorException or HandlerFaultException)
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
            if (ex is OperationCanceledException or PlaxionMediatorException or HandlerFaultException)
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
                return InvokeHandler();
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

        // Terminal handler invocation: a handler fault is tagged with HandlerFaultException so
        // every stage below leaves it untouched (see ThrowSyncException/ThrowAsyncException),
        // and it is unwrapped exactly once at the ExecuteAsync boundary.
        private ValueTask<TResponse> InvokeHandler()
        {
            ValueTask<TResponse> valueTask;
            try
            {
                valueTask = _handlerInstance is not null
                    ? _handlerInstance.Handle(_request, _cancellationToken)
                    : _handlerFunc!(_request, _cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not PlaxionMediatorException)
            {
                throw new HandlerFaultException(ex);
            }

            return valueTask.IsCompletedSuccessfully ? valueTask : MarkAsyncHandlerFault(valueTask);
        }

        private static async ValueTask<TResponse> MarkAsyncHandlerFault(ValueTask<TResponse> valueTask)
        {
            try
            {
                return await valueTask.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not PlaxionMediatorException)
            {
                throw new HandlerFaultException(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask<TResponse> ThrowSyncException(
            Exception ex,
            IPipelineBehavior<TRequest, TResponse> behavior)
        {
            if (ex is OperationCanceledException or PlaxionMediatorException or HandlerFaultException)
            {
                // Intentional framework exceptions (e.g. validation) and handler faults must
                // surface unwrapped.
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
            if (ex is OperationCanceledException or PlaxionMediatorException or HandlerFaultException)
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
