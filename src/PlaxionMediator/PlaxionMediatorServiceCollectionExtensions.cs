using System.Diagnostics.CodeAnalysis;
using PlaxionMediator.Abstractions;
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
        RegisterGlobalBehaviors(services, options);

        // Generated registration (handlers, sender, publisher) — no-op if generator did not run.
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
        RegisterGlobalBehaviors(services, options);
        return services;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "GlobalBehaviors are explicit open-generic types supplied by the application author (e.g. typeof(ValidationBehavior<,>)); constructors are preserved by the consumer reference.")]
    private static void RegisterGlobalBehaviors(IServiceCollection services, PlaxionMediatorOptions options)
    {
        foreach (Type behaviorType in options.GlobalBehaviors)
        {
            ArgumentNullException.ThrowIfNull(behaviorType);

            services.TryAddEnumerable(
                ServiceDescriptor.Describe(
                    typeof(IPipelineBehavior<,>),
                    behaviorType,
                    options.DefaultBehaviorLifetime));
        }
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
