using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaxionMediator.Abstractions;
using PlaxionMediator;

namespace PlaxionMediator.Caching;

/// <summary>
/// DI helpers for registering PlaxionMediator caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables the <see cref="CachingBehavior{TRequest, TResponse}"/> in the PlaxionMediator pipeline.
    /// </summary>
    /// <param name="options">The mediator options.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static PlaxionMediatorOptions UsePlaxionMediatorCachingBehavior(this PlaxionMediatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.GlobalBehaviors.Contains(typeof(CachingBehavior<,>)))
        {
            options.GlobalBehaviors.Add(typeof(CachingBehavior<,>));
        }

        return options;
    }

    /// <summary>
    /// Registers <see cref="IMemoryCache"/> (when missing), caching options,
    /// <see cref="IPlaxionMediatorCacheInvalidator"/>, and the open-generic
    /// <see cref="CachingBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <remarks>
    /// For ordered multi-behavior pipelines, also add <c>typeof(CachingBehavior&lt;,&gt;)</c>
    /// to <c>PlaxionMediatorOptions.GlobalBehaviors</c> (e.g. Validation → Caching → Retry).
    /// Duplicate open-generic registration is safe thanks to <see cref="ServiceCollectionDescriptorExtensions.TryAddEnumerable"/>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="PlaxionMediatorCachingOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorCaching(
        this IServiceCollection services,
        Action<PlaxionMediatorCachingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorCachingOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        // AddMemoryCache is idempotent when IMemoryCache is already registered.
        services.AddMemoryCache();

        services.TryAddSingleton<IPlaxionMediatorCacheInvalidator, MemoryCacheInvalidator>();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>)));

        return services;
    }
}
