using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaxionMediator.Pipeline;

namespace PlaxionMediator.OpenTelemetry;

/// <summary>
/// DI helpers for registering PlaxionMediator OpenTelemetry instrumentation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="OpenTelemetryPipelineObserver"/> and subscribes it to both
    /// <see cref="PipelineObserverHub"/> (Send) and <see cref="NotificationObserverHub"/> (Publish).
    /// Safe to call multiple times; the underlying hubs de-duplicate by instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorOpenTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // PipelineObserverHub/NotificationObserverHub are static process-wide registries (ADR-0008/0009),
        // not DI services, so the observer is registered eagerly here rather than resolved lazily.
        // The instance is also added to the container so it can be resolved for diagnostics if desired.
        OpenTelemetryPipelineObserver observer = new();
        services.TryAddSingleton(observer);
        PipelineObserverHub.Register(observer);
        NotificationObserverHub.Register(observer);

        return services;
    }
}
