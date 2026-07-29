using System.Diagnostics.CodeAnalysis;
using Conduit.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Conduit.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for registering Conduit.
/// The source generator emits a companion registration type invoked from <see cref="AddConduit"/>.
/// </summary>
public static class ConduitServiceCollectionExtensions
{
    /// <summary>
    /// Registers Conduit services, the generated dispatcher, and all discovered handlers/behaviors
    /// in the compiling assembly (zero reflection).
    /// </summary>
    public static IServiceCollection AddConduit(
        this IServiceCollection services,
        Action<ConduitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        ConduitOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        // Generated registration (handlers, sender, publisher) — no-op if generator did not run.
        ConduitGeneratedRegistrationBridge.Invoke(services, options);

        return services;
    }

    /// <summary>
    /// Registers core Conduit services without generated handler discovery.
    /// Used by tests and advanced scenarios that register handlers manually.
    /// </summary>
    public static IServiceCollection AddConduitCore(
        this IServiceCollection services,
        Action<ConduitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        ConduitOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        return services;
    }

    /// <summary>
    /// Manually registers a concrete <see cref="ISender"/> / <see cref="IPublisher"/> implementation.
    /// </summary>
    public static IServiceCollection AddConduitDispatcher<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDispatcher>(
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
