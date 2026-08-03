using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PlaxionMediator.Validation;

/// <summary>
/// DI helpers for registering PlaxionMediator validators.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TValidator"/> as an <see cref="IPlaxionMediatorValidator{TRequest}"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type validated by <typeparamref name="TValidator"/>.</typeparam>
    /// <typeparam name="TValidator">The validator implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The DI lifetime for the validator. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorValidator<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TValidator : class, IPlaxionMediatorValidator<TRequest>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Describe(
                typeof(IPlaxionMediatorValidator<TRequest>),
                typeof(TValidator),
                lifetime));

        return services;
    }
}
