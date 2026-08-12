using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaxionMediator.Abstractions;
using PlaxionMediator;

namespace PlaxionMediator.Authorization;

/// <summary>
/// DI helpers for registering PlaxionMediator authorization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables the <see cref="AuthorizationBehavior{TRequest, TResponse}"/> in the PlaxionMediator pipeline.
    /// </summary>
    /// <remarks>
    /// Recommended global order (outer → inner): Validation → Authorization → Retry → Transaction → Handler,
    /// so authorization is evaluated after validation but before retries and transactions.
    /// </remarks>
    /// <param name="options">The mediator options.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static PlaxionMediatorOptions UsePlaxionMediatorAuthorizationBehavior(this PlaxionMediatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.GlobalBehaviors.Contains(typeof(AuthorizationBehavior<,>)))
        {
            options.GlobalBehaviors.Add(typeof(AuthorizationBehavior<,>));
        }

        return options;
    }

    /// <summary>
    /// Registers authorization options, the open-generic <see cref="AuthorizationBehavior{TRequest,TResponse}"/>,
    /// and a default <see cref="IAuthorizationContextAccessor"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="PlaxionMediatorAuthorizationOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorAuthorization(
        this IServiceCollection services,
        Action<PlaxionMediatorAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorAuthorizationOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>)));

        // Default accessor for background/internal callers. 
        // Adapters (e.g. ASP.NET Core) should override this with a transport-specific implementation.
        services.TryAddScoped<IAuthorizationContextAccessor, SystemAuthorizationContextAccessor>();

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TCheck"/> as an <see cref="IRequestAuthorization{TRequest}"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type authorized by <typeparamref name="TCheck"/>.</typeparam>
    /// <typeparam name="TCheck">The authorization check implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The DI lifetime for the check. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorAuthorization<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCheck>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TCheck : class, IRequestAuthorization<TRequest>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Describe(
                typeof(IRequestAuthorization<TRequest>),
                typeof(TCheck),
                lifetime));

        return services;
    }
}
