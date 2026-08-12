using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PlaxionMediator.Transactions.EntityFrameworkCore;

/// <summary>
/// DI helpers for registering the EF Core transaction manager adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="EfCoreTransactionManager{TDbContext}"/> as the
    /// <see cref="ITransactionManager"/> with scoped lifetime (matching typical DbContext lifetime).
    /// </summary>
    /// <remarks>
    /// Also calls <see cref="Transactions.ServiceCollectionExtensions.AddPlaxionMediatorTransactions"/>
    /// so options and <see cref="TransactionBehavior{TRequest,TResponse}"/> are registered.
    /// Does not register <typeparamref name="TDbContext"/> itself — configure EF Core separately.
    /// </remarks>
    /// <typeparam name="TDbContext">The EF Core <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="PlaxionMediatorTransactionOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddPlaxionMediatorTransactionsEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<PlaxionMediatorTransactionOptions>? configure = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddPlaxionMediatorTransactions(configure);
        services.TryAddScoped<ITransactionManager, EfCoreTransactionManager<TDbContext>>();

        return services;
    }
}
