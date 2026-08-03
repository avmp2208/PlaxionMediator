using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaxionMediator;

namespace PlaxionMediator.Validation;

/// <summary>
/// DI helpers for registering PlaxionMediator validators.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables the <see cref="ValidationBehavior{TRequest, TResponse}"/> in the PlaxionMediator pipeline.
    /// </summary>
    /// <param name="options">The mediator options.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static PlaxionMediatorOptions UsePlaxionMediatorValidationBehavior(this PlaxionMediatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.GlobalBehaviors.Contains(typeof(ValidationBehavior<,>)))
        {
            options.GlobalBehaviors.Add(typeof(ValidationBehavior<,>));
        }

        return options;
    }

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
