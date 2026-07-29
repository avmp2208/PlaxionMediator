using Conduit.Abstractions;

namespace Conduit.Core;

/// <summary>
/// The single entry point application code uses to dispatch a request and await its response.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request through the pipeline and returns the handler response.
    /// </summary>
    ValueTask<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
