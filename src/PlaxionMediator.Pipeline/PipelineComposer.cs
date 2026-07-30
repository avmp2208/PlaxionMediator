using PlaxionMediator.Abstractions;

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

        RequestHandlerDelegate<TResponse> next = () => handler(request, cancellationToken);

        for (int i = behaviors.Count - 1; i >= 0; i--)
        {
            IPipelineBehavior<TRequest, TResponse> behavior = behaviors[i];
            RequestHandlerDelegate<TResponse> continuation = next;
            next = async () =>
            {
                try
                {
                    return await behavior.Handle(request, continuation, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new PlaxionMediator.Core.PipelineExecutionException(
                        $"Error executing behavior '{behavior.GetType().Name}' for request '{typeof(TRequest).Name}'.",
                        ex,
                        behavior.GetType().Name);
                }
            };
        }

        return next;
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
        RequestHandlerDelegate<TResponse> pipeline = Compose(request, behaviors, handler, cancellationToken);
        return pipeline();
    }
}
