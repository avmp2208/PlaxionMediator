# Transactions

Provider-agnostic transactional request boundaries for PlaxionMediator (`v0.8.0`, ADR-0010).

## Problem

Handlers that touch multiple persistence operations need an atomic boundary: commit on success, roll back on failure. Doing this by hand in every handler is noisy and easy to get wrong (especially with retries and nested `Send` calls).

## Non-goals

- Distributed / multi-resource 2PC transactions
- Ambient `System.Transactions.TransactionScope` (async-flow fragility)
- Implicit `SaveChanges` — handlers always own persistence flushes
- Savepoints / nested independent transactions

## Packages

| Package | Role |
|---------|------|
| `PlaxionMediator.Transactions` | Marker, manager abstraction, `TransactionBehavior`, DI helpers |
| `PlaxionMediator.Transactions.EntityFrameworkCore` | `EfCoreTransactionManager<TDbContext>` adapter |

Core depends only on `PlaxionMediator.Abstractions` + `PlaxionMediator` (DI bundle). No EF Core reference in the core transactions package. Both packages set `IsAotCompatible=true`.

## Quick start

```csharp
services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorValidationBehavior();
    o.UsePlaxionMediatorRetryBehavior();
    o.UsePlaxionMediatorTransactionBehavior(); // after Retry
});

// Custom manager:
services.AddPlaxionMediatorTransactions();
services.AddPlaxionMediatorTransactionManager<MyTransactionManager>();

// Or EF Core:
services.AddDbContext<AppDbContext>(/* ... */);
services.AddPlaxionMediatorTransactionsEntityFrameworkCore<AppDbContext>();
```

```csharp
public sealed record CreateOrderRequest(string CustomerId, decimal Total)
    : IRequest<OrderDto>, ITransactionalRequest
{
    public TransactionIsolationLevel? IsolationLevel => TransactionIsolationLevel.ReadCommitted;
}

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, OrderDto>
{
    private readonly AppDbContext _db;
    public CreateOrderHandler(AppDbContext db) => _db = db;

    public async ValueTask<OrderDto> Handle(CreateOrderRequest request, CancellationToken ct)
    {
        var order = new Order { CustomerId = request.CustomerId, Total = request.Total };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct); // handler owns SaveChanges
        return new OrderDto(order.Id, order.CustomerId, order.Total);
    }
}
```

## Pipeline ordering

**Recommended (outer → inner):**

`Validation → Authorization → Retry → Transaction → Handler`

Rationale: each retry attempt gets a **fresh** transaction. A failed attempt’s transaction is rolled back before the next retry.

Unsafe alternative: `Transaction → Retry → Handler` keeps one transaction open across retries and is flagged by analyzer `PlaxionMediator043`.

## Retry interaction

With Retry outer and Transaction inner:

1. Attempt 1 begins TX → handler fails → rollback → dispose
2. Delay (if any)
3. Attempt 2 begins **new** TX → success → commit

## EF Core integration

`EfCoreTransactionManager<TDbContext>` wraps `Database.BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`.

- Never calls `SaveChanges`
- If `Database.CurrentTransaction` is already set, returns a joining scope (`HasExistingTransaction == true`) that does not independently commit/rollback

## Custom managers

Implement `ITransactionManager` + `ITransactionScope`:

```csharp
public interface ITransactionManager
{
    ValueTask<ITransactionScope> BeginAsync(
        TransactionIsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);
}

public interface ITransactionScope : IAsyncDisposable
{
    bool HasExistingTransaction { get; }
    ValueTask CommitAsync(CancellationToken cancellationToken = default);
    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
}
```

Prefer **join-don’t-nest** when an ambient transaction already exists.

## Commit / rollback

| Outcome | Behavior |
|---------|----------|
| Handler succeeds | `CommitAsync` then dispose |
| Handler / inner behavior throws | `RollbackAsync` (best-effort), dispose, **rethrow original exception** |
| Rollback itself throws | Swallowed so the original exception is preserved |

## Cancellation

| When | Result |
|------|--------|
| Token already cancelled before `Handle` | `BeginAsync` never called |
| Cancelled during handler | Rollback, then `OperationCanceledException` propagates |

## Nested transactions

Nested `Send` of another `ITransactionalRequest` should join the existing transaction (`HasExistingTransaction == true`). The outer owner commits/rolls back once.

## Fast path

Non-`ITransactionalRequest` requests hit a single type-check guard and call `next()` — same pattern as `RetryBehavior`. Registering `TransactionBehavior` without using transactional requests adds only that guard on the pipeline path (no manager calls).

## Analyzers

| ID | Severity | Meaning |
|----|----------|---------|
| `PlaxionMediator042` | Error | `ITransactionalRequest` without `TransactionBehavior` registered |
| `PlaxionMediator043` | Error | `TransactionBehavior` outer to `RetryBehavior` (unsafe) |
| `PlaxionMediator044` | Error | Multiple `ITransactionManager` registrations without keyed resolution |
| `PlaxionMediator045` | Warning | Isolation level statically known unsupported (e.g. Snapshot + SQLite/InMemory) |

See [Analyzers-Reference.md](Analyzers-Reference.md).

## Troubleshooting

| Symptom | Check |
|---------|-------|
| No transaction opened | Request implements `ITransactionalRequest`? Behavior registered via `UsePlaxionMediatorTransactionBehavior`? |
| DI fails resolving manager | Exactly one `ITransactionManager` registered? |
| Partial writes after exception | Handler must throw so behavior can rollback; don’t swallow exceptions |
| Retries share dirty state | Ensure Retry is **outer** to Transaction |
| Snapshot errors on SQLite | Use `ReadCommitted` / provider default; heed `PlaxionMediator045` |

## Sample

`samples/PlaxionMediator.Sample.WebApi` demonstrates `CreateOrderRequest` with Validation → Transaction → EF Core (SQLite) → Commit, plus a failure/rollback path.
