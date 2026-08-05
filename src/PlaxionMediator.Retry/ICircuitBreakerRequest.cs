namespace PlaxionMediator.Retry;

/// <summary>
/// Marker for requests that opt into circuit breaker protection via
/// <see cref="CircuitBreakerBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface ICircuitBreakerRequest
{
}
