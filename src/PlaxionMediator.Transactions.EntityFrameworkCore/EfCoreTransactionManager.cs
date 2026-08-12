using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace PlaxionMediator.Transactions.EntityFrameworkCore;

/// <summary>
/// <see cref="ITransactionManager"/> implementation that wraps
/// <see cref="DatabaseFacade.BeginTransactionAsync(System.Data.IsolationLevel, CancellationToken)"/>
/// for a single <typeparamref name="TDbContext"/>.
/// </summary>
/// <remarks>
/// Never calls <c>SaveChanges</c>/<c>SaveChangesAsync</c> — handlers remain responsible for
/// persisting changes inside the transaction boundary.
/// When <see cref="DatabaseFacade.CurrentTransaction"/> is already set, a joining
/// (non-owning) scope is returned.
/// </remarks>
/// <typeparam name="TDbContext">The EF Core <see cref="DbContext"/> type.</typeparam>
public sealed class EfCoreTransactionManager<TDbContext> : ITransactionManager
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreTransactionManager{TDbContext}"/> class.
    /// </summary>
    public EfCoreTransactionManager(TDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async ValueTask<ITransactionScope> BeginAsync(
        TransactionIsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDbContextTransaction? existing = _dbContext.Database.CurrentTransaction;
        if (existing is not null)
        {
            return new EfCoreTransactionScope(existing, ownsTransaction: false);
        }

        IsolationLevel adoIsolation = ToAdoIsolationLevel(isolationLevel);
        IDbContextTransaction transaction = adoIsolation == IsolationLevel.Unspecified
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : await _dbContext.Database.BeginTransactionAsync(adoIsolation, cancellationToken).ConfigureAwait(false);

        return new EfCoreTransactionScope(transaction, ownsTransaction: true);
    }

    private static IsolationLevel ToAdoIsolationLevel(TransactionIsolationLevel level) => level switch
    {
        TransactionIsolationLevel.Unspecified => IsolationLevel.Unspecified,
        TransactionIsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
        TransactionIsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
        TransactionIsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
        TransactionIsolationLevel.Serializable => IsolationLevel.Serializable,
        TransactionIsolationLevel.Snapshot => IsolationLevel.Snapshot,
        _ => IsolationLevel.Unspecified,
    };

    private sealed class EfCoreTransactionScope : ITransactionScope
    {
        private readonly IDbContextTransaction _transaction;
        private readonly bool _ownsTransaction;
        private bool _completed;
        private bool _disposed;

        public EfCoreTransactionScope(IDbContextTransaction transaction, bool ownsTransaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _ownsTransaction = ownsTransaction;
        }

        public bool HasExistingTransaction => !_ownsTransaction;

        public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || _completed || !_ownsTransaction)
            {
                return;
            }

            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _completed = true;
        }

        public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || _completed || !_ownsTransaction)
            {
                return;
            }

            await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_ownsTransaction)
            {
                await _transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
