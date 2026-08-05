namespace PlaxionMediator.Retry;

/// <summary>
/// Options controlling the default circuit breaker resilience pipeline for
/// <see cref="ICircuitBreakerRequest"/> requests.
/// </summary>
/// <remarks>
/// This is an independent capability from <see cref="PlaxionMediatorRetryOptions"/>: the circuit breaker
/// pipeline built from these options does not compose with the existing hand-rolled retry loop.
/// </remarks>
public sealed class PlaxionMediatorCircuitBreakerOptions
{
    /// <summary>
    /// Name of the resilience pipeline registered via <c>AddResiliencePipeline</c>. Defaults to
    /// <c>"plaxionmediator-circuitbreaker-default"</c>.
    /// </summary>
    public string PipelineName { get; set; } = "plaxionmediator-circuitbreaker-default";

    /// <summary>
    /// Failure ratio threshold above which the circuit opens. Defaults to 0.5.
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Minimum number of calls within the sampling duration before the circuit breaker evaluates
    /// the failure ratio. Defaults to 10.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Time window over which failure ratio is measured. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Duration the circuit stays open before transitioning to half-open. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(5);
}
