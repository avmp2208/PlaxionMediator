namespace PlaxionMediator.Transactions;

/// <summary>
/// Marker for requests that opt into automatic transaction boundaries via
/// <see cref="TransactionBehavior{TRequest,TResponse}"/>.
/// Per-request <see cref="IsolationLevel"/> overrides <see cref="PlaxionMediatorTransactionOptions.DefaultIsolationLevel"/>
/// when non-null.
/// </summary>
public interface ITransactionalRequest
{
    /// <summary>
    /// Optional isolation level for this request.
    /// When <see langword="null"/>, <see cref="PlaxionMediatorTransactionOptions.DefaultIsolationLevel"/> is used.
    /// </summary>
    TransactionIsolationLevel? IsolationLevel => null;
}
