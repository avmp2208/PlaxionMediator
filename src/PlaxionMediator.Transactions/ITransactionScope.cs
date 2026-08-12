namespace PlaxionMediator.Transactions;

/// <summary>
/// A unit-of-work transaction boundary opened by <see cref="ITransactionManager"/>.
/// </summary>
/// <remarks>
/// Implementations must be safe to dispose after commit or rollback. When
/// <see cref="HasExistingTransaction"/> is <see langword="true"/>, the scope is participating
/// in an ambient/outer transaction and must not independently commit or roll it back.
/// </remarks>
public interface ITransactionScope : IAsyncDisposable
{
    /// <summary>
    /// <see langword="true"/> when this scope joined an already-open transaction rather than
    /// beginning a new one ("join don't nest" semantics).
    /// </summary>
    bool HasExistingTransaction { get; }

    /// <summary>
    /// Commits the underlying transaction when this scope owns it.
    /// No-op (or deferred to the owner) when <see cref="HasExistingTransaction"/> is <see langword="true"/>.
    /// </summary>
    ValueTask CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the underlying transaction when this scope owns it.
    /// No-op (or deferred to the owner) when <see cref="HasExistingTransaction"/> is <see langword="true"/>.
    /// </summary>
    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
}
