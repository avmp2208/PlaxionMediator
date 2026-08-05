using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace PlaxionMediator.Retry.Tests;

public sealed class DefaultCircuitBreakerPolicyProviderTests
{
    private sealed record Ping(string Message);

    [Fact]
    public void Constructor_Throws_On_Null_Dependencies()
    {
        ServiceCollection services = new();
        services.AddResiliencePipeline("some-pipeline", builder => builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions()));
        using ServiceProvider sp = services.BuildServiceProvider();
        ResiliencePipelineProvider<string> pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
        PlaxionMediatorCircuitBreakerOptions options = new();

        Assert.Throws<ArgumentNullException>(() =>
            new DefaultCircuitBreakerPolicyProvider<Ping>(null!, options));
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultCircuitBreakerPolicyProvider<Ping>(pipelineProvider, null!));
    }

    [Fact]
    public void GetPipeline_Resolves_Pipeline_Registered_Under_Options_PipelineName()
    {
        ServiceCollection services = new();
        PlaxionMediatorCircuitBreakerOptions options = new() { PipelineName = "custom-cb-pipeline" };

        services.AddResiliencePipeline(
            options.PipelineName,
            builder => builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions()));

        using ServiceProvider sp = services.BuildServiceProvider();
        ResiliencePipelineProvider<string> pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();

        DefaultCircuitBreakerPolicyProvider<Ping> provider = new(pipelineProvider, options);

        ResiliencePipeline expected = pipelineProvider.GetPipeline(options.PipelineName);
        ResiliencePipeline actual = provider.GetPipeline(new Ping("x"));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetPipeline_Uses_Default_PipelineName_When_Not_Customized()
    {
        ServiceCollection services = new();
        PlaxionMediatorCircuitBreakerOptions options = new();

        services.AddResiliencePipeline(
            options.PipelineName,
            builder => builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions()));

        using ServiceProvider sp = services.BuildServiceProvider();
        ResiliencePipelineProvider<string> pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();

        DefaultCircuitBreakerPolicyProvider<Ping> provider = new(pipelineProvider, options);

        Assert.Equal("plaxionmediator-circuitbreaker-default", options.PipelineName);
        Assert.Same(pipelineProvider.GetPipeline(options.PipelineName), provider.GetPipeline(new Ping("x")));
    }
}
