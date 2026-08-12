namespace PlaxionMediator.Transactions;

/// <summary>
/// Isolation levels accepted by <see cref="ITransactionalRequest"/> / <see cref="ITransactionManager"/>.
/// Mirrors the common ADO.NET / EF Core isolation levels without taking a hard dependency on either.
/// </summary>
public enum TransactionIsolationLevel
{
    /// <summary>Provider default isolation level.</summary>
    Unspecified = 0,

    /// <summary>Dirty reads, non-repeatable reads, and phantom reads are possible.</summary>
    ReadUncommitted = 1,

    /// <summary>Dirty reads are prevented; non-repeatable reads and phantom reads are possible.</summary>
    ReadCommitted = 2,

    /// <summary>Dirty and non-repeatable reads are prevented; phantom reads are possible.</summary>
    RepeatableRead = 3,

    /// <summary>Dirty, non-repeatable, and phantom reads are prevented.</summary>
    Serializable = 4,

    /// <summary>Row versions used to provide transaction-level read consistency (provider-specific).</summary>
    Snapshot = 5,
}
