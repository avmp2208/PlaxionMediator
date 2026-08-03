using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PlaxionMediator.Validation.FluentValidation;

/// <summary>
/// DI helpers for wiring FluentValidation validators into PlaxionMediator validation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a single FluentValidation <typeparamref name="TValidator"/> for <typeparamref name="TRequest"/>
    /// and ensures a <see cref="FluentValidationAdapter{TRequest}"/> is available as
    /// <see cref="IPlaxionMediatorValidator{TRequest}"/>.
    /// </summary>
    public static IServiceCollection AddPlaxionMediatorFluentValidator<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TValidator : class, IValidator<TRequest>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Describe(typeof(IValidator<TRequest>), typeof(TValidator), lifetime));

        services.TryAddEnumerable(
            ServiceDescriptor.Describe(
                typeof(IPlaxionMediatorValidator<TRequest>),
                typeof(FluentValidationAdapter<TRequest>),
                lifetime));

        return services;
    }

    /// <summary>
    /// Scans the supplied assemblies for concrete <see cref="IValidator{T}"/> implementations,
    /// registers each validator, and registers a <see cref="FluentValidationAdapter{TRequest}"/>
    /// for every distinct request type found.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembliesToScan">Assemblies that contain FluentValidation validators.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    [RequiresUnreferencedCode("Assembly scanning reflects over types and may break with trimming.")]
    [RequiresDynamicCode("Assembly scanning reflects over types and may break with Native AOT.")]
    public static IServiceCollection AddPlaxionMediatorFluentValidation(
        this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assembliesToScan is null || assembliesToScan.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided.", nameof(assembliesToScan));
        }

        HashSet<(Type RequestType, Type ValidatorType)> registrations = [];

        foreach (Assembly assembly in assembliesToScan)
        {
            if (assembly is null)
            {
                continue;
            }

            foreach (Type type in assembly.GetTypes())
            {
                if (type is not { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
                {
                    continue;
                }

                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (!interfaceType.IsGenericType
                        || interfaceType.GetGenericTypeDefinition() != typeof(IValidator<>))
                    {
                        continue;
                    }

                    Type requestType = interfaceType.GetGenericArguments()[0];
                    registrations.Add((requestType, type));
                }
            }
        }

        foreach ((Type requestType, Type validatorType) in registrations)
        {
            Type validatorServiceType = typeof(IValidator<>).MakeGenericType(requestType);
            services.TryAddEnumerable(ServiceDescriptor.Transient(validatorServiceType, validatorType));

            Type adapterServiceType = typeof(IPlaxionMediatorValidator<>).MakeGenericType(requestType);
            Type adapterImplementationType = typeof(FluentValidationAdapter<>).MakeGenericType(requestType);
            services.TryAddEnumerable(ServiceDescriptor.Transient(adapterServiceType, adapterImplementationType));
        }

        return services;
    }
}
