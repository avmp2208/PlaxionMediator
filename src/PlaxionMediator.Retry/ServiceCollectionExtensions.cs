using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.CircuitBreaker;
using PlaxionMediator.Abstractions;
using PlaxionMediator;

namespace PlaxionMediator.Retry;

/// <summary>
/// DI helpers for registering PlaxionMediator retry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables the <see cref="RetryBehavior{TRequest, TResponse}"/> in the PlaxionMediator pipeline.
    /// </summary>
    /// <param name="options">The mediator options.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static PlaxionMediatorOptions UsePlaxionMediatorRetryBehavior(this PlaxionMediatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.GlobalBehaviors.Contains(typeof(RetryBehavior<,>)))
        {
            options.GlobalBehaviors.Add(typeof(RetryBehavior<,>));
        }

        return options;
    }

    /// <summary>
    /// Registers retry options, the default <see cref="IRetryDelayProvider"/>, and the open-generic
    /// <see cref="RetryBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <remarks>
    /// For ordered multi-behavior pipelines, also add <c>typeof(RetryBehavior&lt;,&gt;)</c>
    /// to <c>PlaxionMediatorOptions.GlobalBehaviors</c> (e.g. Validation → Caching → Retry).
    /// This package does not reference Validation; add validation exception types to
    /// <see cref="PlaxionMediatorRetryOptions.NonRetryableExceptionTypes"/> when needed.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="PlaxionMediatorRetryOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorRetry(
        this IServiceCollection services,
        Action<PlaxionMediatorRetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorRetryOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddSingleton<IRetryDelayProvider, TaskRetryDelayProvider>();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>)));

        return services;
    }

    /// <summary>
    /// Enables the <see cref="CircuitBreakerBehavior{TRequest, TResponse}"/> in the PlaxionMediator pipeline.
    /// </summary>
    /// <remarks>
    /// This is a new, independent capability added alongside <see cref="UsePlaxionMediatorRetryBehavior"/>;
    /// it does not modify or replace it. Register it outside <see cref="UsePlaxionMediatorRetryBehavior"/>
    /// (i.e. call this first) so an open circuit fails fast before any retry attempts are made.
    /// </remarks>
    /// <param name="options">The mediator options.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static PlaxionMediatorOptions UsePlaxionMediatorCircuitBreakerBehavior(this PlaxionMediatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.GlobalBehaviors.Contains(typeof(CircuitBreakerBehavior<,>)))
        {
            options.GlobalBehaviors.Add(typeof(CircuitBreakerBehavior<,>));
        }

        return options;
    }

    /// <summary>
    /// Registers circuit breaker options, the named circuit breaker <see cref="ResiliencePipeline"/>,
    /// the <see cref="ICircuitBreakerPolicyProvider{TRequest}"/>, and the open-generic
    /// <see cref="CircuitBreakerBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <remarks>
    /// This is a new, independent addition to the retry package; it does not modify
    /// <see cref="AddPlaxionMediatorRetry"/> or any existing retry types. The circuit breaker pipeline
    /// built here is its own <see cref="ResiliencePipeline"/> (circuit breaker only) and does not compose
    /// with the existing hand-rolled retry loop. For ordered multi-behavior pipelines, also add
    /// <c>typeof(CircuitBreakerBehavior&lt;,&gt;)</c> to <c>PlaxionMediatorOptions.GlobalBehaviors</c>
    /// before <c>typeof(RetryBehavior&lt;,&gt;)</c> (e.g. Validation → Caching → CircuitBreaker → Retry),
    /// so an open circuit fails fast before any retry attempts are made.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="PlaxionMediatorCircuitBreakerOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorCircuitBreaker(
        this IServiceCollection services,
        Action<PlaxionMediatorCircuitBreakerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorCircuitBreakerOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.AddResiliencePipeline(options.PipelineName, builder =>
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.FailureRatio,
                MinimumThroughput = options.MinimumThroughput,
                SamplingDuration = options.SamplingDuration,
                BreakDuration = options.BreakDuration,
            });
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton(
                typeof(ICircuitBreakerPolicyProvider<>),
                typeof(DefaultCircuitBreakerPolicyProvider<>)));

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(CircuitBreakerBehavior<,>)));

        return services;
    }
}
