using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Caching;
using PlaxionMediator.Core;
using PlaxionMediator.Retry;
using PlaxionMediator.Validation;
using PlaxionMediator;

namespace PlaxionMediator.Benchmarks;

/// <summary>
/// Send dispatch benchmarks covering realistic pipeline behavior chains built from the
/// production Validation, Caching, CircuitBreaker, and Retry packages, each isolated in its own
/// <see cref="ServiceProvider"/> so behavior chain composition is the only variable between scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ResiliencePipelineBenchmarks
{
    private ServiceProvider _circuitBreakerOnlyProvider = null!;
    private ServiceProvider _fullChainCacheMissProvider = null!;
    private ServiceProvider _fullChainCacheHitProvider = null!;
    private ISender _circuitBreakerOnlySender = null!;
    private ISender _fullChainCacheMissSender = null!;
    private ISender _fullChainCacheHitSender = null!;
    private GuardedPing _guardedPing = null!;
    private ResilientPing _cacheMissPing = null!;
    private ResilientPing _cacheHitPing = null!;

    [GlobalSetup]
    public void Setup()
    {
        _guardedPing = new GuardedPing("benchmark");
        _cacheMissPing = new ResilientPing("benchmark-miss");
        _cacheHitPing = new ResilientPing("benchmark-hit");

        // Scenario 1: CircuitBreaker only, on the Send() path.
        ServiceCollection circuitBreakerOnlyServices = new();
        circuitBreakerOnlyServices.AddPlaxionMediator(o =>
        {
            o.UsePlaxionMediatorCircuitBreakerBehavior();
        });
        circuitBreakerOnlyServices.AddPlaxionMediatorCircuitBreaker(o =>
        {
            o.PipelineName = "benchmark-circuitbreaker-only";
        });
        _circuitBreakerOnlyProvider = circuitBreakerOnlyServices.BuildServiceProvider();
        _circuitBreakerOnlySender = _circuitBreakerOnlyProvider.GetRequiredService<ISender>();

        // Scenario 2 & 3: full chain Validation -> Caching -> CircuitBreaker -> Retry, mirroring
        // the ordering used in samples/PlaxionMediator.Sample.WebApi/Program.cs. Cache-miss and
        // cache-hit scenarios use distinct request instances/keys so a warm-up call in Setup can
        // pre-populate the cache-hit provider without affecting the cache-miss provider.
        ServiceCollection fullChainCacheMissServices = new();
        ConfigureFullChain(fullChainCacheMissServices, "benchmark-circuitbreaker-full-miss");
        _fullChainCacheMissProvider = fullChainCacheMissServices.BuildServiceProvider();
        _fullChainCacheMissSender = _fullChainCacheMissProvider.GetRequiredService<ISender>();

        ServiceCollection fullChainCacheHitServices = new();
        ConfigureFullChain(fullChainCacheHitServices, "benchmark-circuitbreaker-full-hit");
        _fullChainCacheHitProvider = fullChainCacheHitServices.BuildServiceProvider();
        _fullChainCacheHitSender = _fullChainCacheHitProvider.GetRequiredService<ISender>();

        // Warm the cache so every measured iteration is a cache hit.
        _fullChainCacheHitSender.Send(_cacheHitPing).AsTask().GetAwaiter().GetResult();
    }

    private static void ConfigureFullChain(ServiceCollection services, string circuitBreakerPipelineName)
    {
        services.AddPlaxionMediator(o =>
        {
            o.UsePlaxionMediatorValidationBehavior();
            o.UsePlaxionMediatorCachingBehavior();
            o.UsePlaxionMediatorCircuitBreakerBehavior();
            o.UsePlaxionMediatorRetryBehavior();
        });
        services.AddPlaxionMediatorValidator<ResilientPing, ResilientPingValidator>();
        services.AddPlaxionMediatorCaching(o =>
        {
            o.DefaultCacheDuration = TimeSpan.FromMinutes(5);
        });
        services.AddPlaxionMediatorCircuitBreaker(o =>
        {
            o.PipelineName = circuitBreakerPipelineName;
        });
        services.AddPlaxionMediatorRetry(o =>
        {
            o.MaxRetryAttempts = 3;
            o.BaseDelay = TimeSpan.FromMilliseconds(1);
        });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _circuitBreakerOnlyProvider.Dispose();
        _fullChainCacheMissProvider.Dispose();
        _fullChainCacheHitProvider.Dispose();
    }

    /// <summary>Only <see cref="CircuitBreakerBehavior{TRequest,TResponse}"/> registered.</summary>
    [Benchmark(Description = "Send_CircuitBreakerOnly")]
    public ValueTask<string> Send_CircuitBreakerOnly()
        => _circuitBreakerOnlySender.Send(_guardedPing);

    /// <summary>
    /// Full Validation -> Caching -> CircuitBreaker -> Retry chain, always missing the cache.
    /// </summary>
    [Benchmark(Description = "Send_FullChain_CacheMiss")]
    public ValueTask<string> Send_FullChain_CacheMiss()
        => _fullChainCacheMissSender.Send(_cacheMissPing);

    /// <summary>
    /// Full Validation -> Caching -> CircuitBreaker -> Retry chain, always hitting the warmed cache
    /// so Caching short-circuits before CircuitBreaker/Retry/the handler run.
    /// </summary>
    [Benchmark(Description = "Send_FullChain_CacheHit")]
    public ValueTask<string> Send_FullChain_CacheHit()
        => _fullChainCacheHitSender.Send(_cacheHitPing);
}

public sealed record GuardedPing(string Message) : IRequest<string>, ICircuitBreakerRequest;

public sealed class GuardedPingHandler : IRequestHandler<GuardedPing, string>
{
    public ValueTask<string> Handle(GuardedPing request, CancellationToken cancellationToken)
        => ValueTask.FromResult("Pong:" + request.Message);
}

public sealed record ResilientPing(string Message)
    : IRequest<string>, ICircuitBreakerRequest, IRetryableRequest, ICacheableRequest<string>
{
    public string CacheKey => $"resilient-ping:{Message}";
}

public sealed class ResilientPingValidator : IPlaxionMediatorValidator<ResilientPing>
{
    public ValueTask<PlaxionMediatorValidationResult> ValidateAsync(
        ResilientPing request,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(PlaxionMediatorValidationResult.Success);
}

public sealed class ResilientPingHandler : IRequestHandler<ResilientPing, string>
{
    public ValueTask<string> Handle(ResilientPing request, CancellationToken cancellationToken)
        => ValueTask.FromResult("Pong:" + request.Message);
}
