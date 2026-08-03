using System.Runtime.CompilerServices;
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
        return () => ExecuteCore(request, behaviors, handler, cancellationToken);
    }

    /// <summary>
    /// Executes the composed pipeline and returns the response.
    /// </summary>
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

        return ExecuteCore(request, behaviors, handler, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask<TResponse> ExecuteCore<TRequest, TResponse>(
        TRequest request,
        IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
        Func<TRequest, CancellationToken, ValueTask<TResponse>> handler,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        if (behaviors.Count == 0)
        {
            return handler(request, cancellationToken);
        }

        // Single heap object + one cached next-delegate instead of O(n) async lambdas per Send.
        PipelineRunner<TRequest, TResponse> runner = new(request, behaviors, handler, cancellationToken);
        return runner.Next();
    }

    /// <summary>
    /// Index-based pipeline trampoline. Each <see cref="Next"/> invocation advances one stage
    /// (behavior or terminal handler). Exception wrapping matches the historical Compose semantics.
    /// </summary>
    private sealed class PipelineRunner<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly TRequest _request;
        private readonly IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> _behaviors;
        private readonly Func<TRequest, CancellationToken, ValueTask<TResponse>> _handler;
        private readonly CancellationToken _cancellationToken;
        private RequestHandlerDelegate<TResponse>? _next;
        private int _index;

        public PipelineRunner(
            TRequest request,
            IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
            Func<TRequest, CancellationToken, ValueTask<TResponse>> handler,
            CancellationToken cancellationToken)
        {
            _request = request;
            _behaviors = behaviors;
            _handler = handler;
            _cancellationToken = cancellationToken;
        }

        public ValueTask<TResponse> Next()
        {
            int index = _index;
            if ((uint)index >= (uint)_behaviors.Count)
            {
                return _handler(_request, _cancellationToken);
            }

            _index = index + 1;
            IPipelineBehavior<TRequest, TResponse> behavior = _behaviors[index];
            // Cache the delegate once so behaviors share a single next trampoline allocation.
            RequestHandlerDelegate<TResponse> next = _next ??= Next;

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
