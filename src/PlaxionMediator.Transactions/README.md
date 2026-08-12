# PlaxionMediator.Transactions

Provider-agnostic transactional pipeline behavior for PlaxionMediator. Requests opt in via the
`ITransactionalRequest` marker; `TransactionBehavior<TRequest,TResponse>` begins, commits, and
rolls back a transaction around the inner pipeline through an `ITransactionManager` abstraction.

This package has **no** EF Core dependency. Use
`PlaxionMediator.Transactions.EntityFrameworkCore` for the DbContext adapter, or implement
`ITransactionManager` / `ITransactionScope` for any other store.

## Install

```bash
dotnet add package PlaxionMediator.Transactions
```

## Usage

```csharp
using PlaxionMediator.Transactions;

// Recommended order (outer → inner): Validation → Retry → Transaction → Handler
services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorValidationBehavior();
    o.UsePlaxionMediatorRetryBehavior();
    o.UsePlaxionMediatorTransactionBehavior();
});

services.AddPlaxionMediatorTransactions(o =>
{
    o.DefaultIsolationLevel = TransactionIsolationLevel.ReadCommitted;
});

// Register exactly one ITransactionManager (custom or EF Core adapter).
services.AddPlaxionMediatorTransactionManager<MyTransactionManager>();
```

```csharp
public sealed record CreateOrderRequest(string CustomerId, decimal Total)
    : IRequest<OrderDto>, ITransactionalRequest
{
    // Optional per-request override; null uses PlaxionMediatorTransactionOptions.DefaultIsolationLevel.
    public TransactionIsolationLevel? IsolationLevel => TransactionIsolationLevel.ReadCommitted;
}
```

## Semantics

- **Fast path:** non-`ITransactionalRequest` requests call `next()` immediately (same pattern as `RetryBehavior`).
- **Commit / rollback:** success commits; any exception rolls back then rethrows the original exception.
- **Cancellation:** cancelled tokens before begin never open a transaction; cancellation during the handler triggers rollback.
- **Nesting:** managers should join an existing transaction (`HasExistingTransaction == true`) rather than nest.
- **SaveChanges:** never called by this package — handlers own persistence flushes.

## Learn more

See [`docs/wiki/Transactions.md`](https://github.com/avmp2208/PlaxionMediator/blob/master/docs/wiki/Transactions.md).
