namespace Conduit.Abstractions;

/// <summary>
/// A composable cross-cutting step that wraps handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Invokes the behavior, optionally calling <paramref name="next"/> to continue the pipeline.
    /// </summary>
    ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
