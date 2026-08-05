using Polly;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Retry;

/// <summary>
/// Pipeline behavior that executes requests implementing <see cref="ICircuitBreakerRequest"/> through a
/// named circuit breaker <see cref="ResiliencePipeline"/>, failing fast with a
/// <c>Polly.CircuitBreaker.BrokenCircuitException</c> when the circuit is open. Non-opt-in requests are a
/// fast no-op pass-through.
/// </summary>
/// <remarks>
/// This behavior is an independent addition alongside <see cref="RetryBehavior{TRequest,TResponse}"/>;
/// the two are not composed into a single resilience pipeline. Register this behavior outside
/// <see cref="RetryBehavior{TRequest,TResponse}"/> so an open circuit fails fast before any retry
/// attempts are made.
/// </remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class CircuitBreakerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICircuitBreakerPolicyProvider<TRequest> _policyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    public CircuitBreakerBehavior(ICircuitBreakerPolicyProvider<TRequest> policyProvider)
    {
        _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
    }

    /// <inheritdoc />
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        // Fast path: open-generic registration applies to every request; only opt-in types are guarded.
        if (request is not ICircuitBreakerRequest)
        {
            return next();
        }

        ResiliencePipeline pipeline = _policyProvider.GetPipeline(request);
        return pipeline.ExecuteAsync(
            static (state, token) => state.Next(),
            (Next: next, Ct: cancellationToken),
            cancellationToken);
    }
}
