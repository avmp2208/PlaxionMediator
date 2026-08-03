using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
}
