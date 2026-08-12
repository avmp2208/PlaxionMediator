namespace PlaxionMediator.Transactions;

/// <summary>
/// Options controlling default transaction behavior for transactional requests.
/// </summary>
public sealed class PlaxionMediatorTransactionOptions
{
    /// <summary>
    /// Isolation level used when <see cref="ITransactionalRequest.IsolationLevel"/> is <see langword="null"/>.
    /// Defaults to <see cref="TransactionIsolationLevel.Unspecified"/> (provider default).
    /// </summary>
    public TransactionIsolationLevel DefaultIsolationLevel { get; set; } = TransactionIsolationLevel.Unspecified;
}
