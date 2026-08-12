using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaxionMediator.Abstractions;
using PlaxionMediator;

namespace PlaxionMediator.Transactions;

/// <summary>
/// DI helpers for registering PlaxionMediator transaction services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables the <see cref="TransactionBehavior{TRequest, TResponse}"/> in the PlaxionMediator pipeline.
    /// </summary>
    /// <remarks>
    /// Recommended global order (outer → inner): Validation → Authorization → Retry → Transaction → Handler,
    /// so each retry attempt gets a fresh transaction.
    /// </remarks>
    /// <param name="options">The mediator options.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static PlaxionMediatorOptions UsePlaxionMediatorTransactionBehavior(this PlaxionMediatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.GlobalBehaviors.Contains(typeof(TransactionBehavior<,>)))
        {
            options.GlobalBehaviors.Add(typeof(TransactionBehavior<,>));
        }

        return options;
    }

    /// <summary>
    /// Registers transaction options and the open-generic
    /// <see cref="TransactionBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <remarks>
    /// A concrete <see cref="ITransactionManager"/> must also be registered (for example via
    /// <c>AddPlaxionMediatorTransactionsEntityFrameworkCore&lt;TDbContext&gt;()</c> or a custom manager).
    /// For ordered multi-behavior pipelines, also add <c>typeof(TransactionBehavior&lt;,&gt;)</c>
    /// to <c>PlaxionMediatorOptions.GlobalBehaviors</c> after Retry (e.g. Validation → Retry → Transaction).
    /// Duplicate open-generic registration is safe thanks to <see cref="ServiceCollectionDescriptorExtensions.TryAddEnumerable"/>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="PlaxionMediatorTransactionOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorTransactions(
        this IServiceCollection services,
        Action<PlaxionMediatorTransactionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PlaxionMediatorTransactionOptions options = new();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>)));

        return services;
    }

    /// <summary>
    /// Registers a concrete <see cref="ITransactionManager"/> implementation with the specified lifetime.
    /// </summary>
    /// <typeparam name="TManager">The manager implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">Service lifetime. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorTransactionManager<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TManager>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TManager : class, ITransactionManager
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAdd(new ServiceDescriptor(typeof(ITransactionManager), typeof(TManager), lifetime));
        return services;
    }
}
