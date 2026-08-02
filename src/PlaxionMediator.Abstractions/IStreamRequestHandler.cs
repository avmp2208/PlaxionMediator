namespace PlaxionMediator.Abstractions;

/// <summary>
/// Handles a streaming request. Exactly one implementation must exist per stream request type.
/// Implementations should yield items incrementally and honor <paramref name="cancellationToken"/>.
/// </summary>
/// <typeparam name="TRequest">The stream request type.</typeparam>
/// <typeparam name="TResponse">The streamed response element type.</typeparam>
public interface IStreamRequestHandler<in TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    /// <summary>
    /// Handles the stream request and yields response items incrementally.
    /// </summary>
    /// <param name="request">The stream request.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration and downstream I/O.</param>
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
