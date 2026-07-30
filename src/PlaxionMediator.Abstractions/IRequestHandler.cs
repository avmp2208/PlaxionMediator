namespace PlaxionMediator.Abstractions;

/// <summary>
/// Handles a single request type. Exactly one implementation must exist per request type.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and returns a response.
    /// </summary>
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
