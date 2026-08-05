using Polly;
using Polly.Registry;

namespace PlaxionMediator.Retry;

/// <summary>
/// Default <see cref="ICircuitBreakerPolicyProvider{TRequest}"/> that resolves the named pipeline
/// configured via <see cref="PlaxionMediatorCircuitBreakerOptions.PipelineName"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public sealed class DefaultCircuitBreakerPolicyProvider<TRequest> : ICircuitBreakerPolicyProvider<TRequest>
{
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly PlaxionMediatorCircuitBreakerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCircuitBreakerPolicyProvider{TRequest}"/> class.
    /// </summary>
    public DefaultCircuitBreakerPolicyProvider(
        ResiliencePipelineProvider<string> pipelineProvider,
        PlaxionMediatorCircuitBreakerOptions options)
    {
        _pipelineProvider = pipelineProvider ?? throw new ArgumentNullException(nameof(pipelineProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public ResiliencePipeline GetPipeline(TRequest request)
    {
        return _pipelineProvider.GetPipeline(_options.PipelineName);
    }
}
