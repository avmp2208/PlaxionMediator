using Polly;

namespace PlaxionMediator.Retry;

/// <summary>
/// Maps a request to a named circuit breaker <see cref="ResiliencePipeline"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface ICircuitBreakerPolicyProvider<in TRequest>
{
    /// <summary>
    /// Resolves the circuit breaker pipeline to use for <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <returns>The resolved <see cref="ResiliencePipeline"/>.</returns>
    ResiliencePipeline GetPipeline(TRequest request);
}
