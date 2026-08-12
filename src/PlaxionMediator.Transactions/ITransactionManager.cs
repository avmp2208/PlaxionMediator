namespace PlaxionMediator.Transactions;

/// <summary>
/// Provider-agnostic abstraction that begins transactional scopes for
/// <see cref="TransactionBehavior{TRequest,TResponse}"/>.
/// </summary>
/// <remarks>
/// Implementations must never call persistence APIs such as EF Core <c>SaveChanges</c>;
/// that remains the handler's responsibility. Prefer "join don't nest" semantics when an
/// ambient transaction already exists.
/// </remarks>
public interface ITransactionManager
{
    /// <summary>
    /// Begins a new transaction scope, or returns a joining scope when a transaction is already active.
    /// </summary>
    /// <param name="isolationLevel">Requested isolation level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ITransactionScope"/> that must be committed, rolled back, and disposed.</returns>
    ValueTask<ITransactionScope> BeginAsync(
        TransactionIsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);
}
