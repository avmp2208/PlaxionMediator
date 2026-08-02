using System.Diagnostics.CodeAnalysis;
using PlaxionMediator.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PlaxionMediator;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for registering PlaxionMediator.
/// The source generator emits a companion registration type invoked from <see cref="AddPlaxionMediator"/>.
/// </summary>
public static class PlaxionMediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers PlaxionMediator services, the generated dispatcher, and all discovered handlers/behaviors
    /// in the compiling assembly (zero reflection).
    /// </summary>
    public static IServiceCollection AddPlaxionMediator(
        this IServiceCollection services,
        Action<PlaxionMediatorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        // Generated registration (handlers, sender, publisher) â€” no-op if generator did not run.
        PlaxionMediatorGeneratedRegistrationBridge.Invoke(services, options);

        return services;
    }

    /// <summary>
    /// Registers core PlaxionMediator services without generated handler discovery.
    /// Used by tests and advanced scenarios that register handlers manually.
    /// </summary>
    public static IServiceCollection AddPlaxionMediatorCore(
        this IServiceCollection services,
        Action<PlaxionMediatorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        return services;
    }

    /// <summary>
    /// Manually registers a concrete <see cref="ISender"/> / <see cref="IPublisher"/> implementation.
    /// </summary>
    public static IServiceCollection AddPlaxionMediatorDispatcher<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDispatcher>(
        this IServiceCollection services)
        where TDispatcher : class, ISender, IPublisher
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<TDispatcher>();
        services.TryAddScoped<ISender>(sp => sp.GetRequiredService<TDispatcher>());
        services.TryAddScoped<IPublisher>(sp => sp.GetRequiredService<TDispatcher>());
        return services;
    }
}
