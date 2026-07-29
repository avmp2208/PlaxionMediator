# 20 — Transactions

## Marker & Abstraction

```csharp
public interface ITransactionalRequest { } // Opt-in: this request's handler execution must run inside a transaction.

public interface IConduitTransactionScope : IAsyncDisposable
{
    ValueTask CommitAsync(CancellationToken cancellationToken);
    // No explicit Rollback() — disposal without Commit is the rollback signal (mirrors IDbContextTransaction/TransactionScope idiom).
}

public interface IConduitTransactionFactory
{
    ValueTask<IConduitTransactionScope> BeginAsync(CancellationToken cancellationToken);
}
```

**Rationale**: like caching, transactional behavior is opt-in per request (`ITransactionalRequest` marker) — never inferred from "this looks like a write operation" heuristics, since guessing wrong in either direction (wrapping a read in a transaction, or missing a transaction on a multi-write command) is a correctness bug.

## The Transaction Behavior

```csharp
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalRequest
{
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        await using var scope = await _transactionFactory.BeginAsync(ct);
        var response = await next();
        await scope.CommitAsync(ct);
        return response;
    }
}
```

Registered per-request (`PipelineBuilder.Use<TransactionBehavior<,>>()`), positioned **outside** validation/authorization/caching but **inside** logging/telemetry (so the transaction span is visible in traces) — validation/authorization failures should never open a transaction at all, avoiding wasted connection pool usage for requests that fail before reaching the handler.

## Nested Transactions

```csharp
public sealed class NestedAwareTransactionFactory : IConduitTransactionFactory
{
    // Uses AsyncLocal<IConduitTransactionScope?> to detect an already-open ambient scope
    // and returns a no-op "participant" scope instead of opening a second physical transaction.
}
```

When a handler calls `ISender.Send` for another `ITransactionalRequest` (e.g., a saga-like command composing sub-commands), the default `NestedAwareTransactionFactory` detects the ambient transaction via `AsyncLocal<T>` and returns a **participant scope** that no-ops on `CommitAsync`/`DisposeAsync` — only the outermost scope actually commits or rolls back, matching how nested `TransactionScope` behaves in classic .NET, a well-understood semantic worth preserving.

## Ambient Transactions

Conduit explicitly avoids `System.Transactions.TransactionScope`'s ambient-via-`Transaction.Current` model for the *default* EF Core-backed implementation (DTC/ambient transaction promotion has historically been a source of confusing, hard-to-diagnose behavior) — ambient tracking here uses `AsyncLocal<IConduitTransactionScope>`, which is explicit, does not risk transaction promotion to MSDTC, and is fully `async`/`await`-flow-safe.

## EF Core Integration

```csharp
public sealed class EfCoreTransactionFactory<TDbContext> : IConduitTransactionFactory where TDbContext : DbContext
{
    public async ValueTask<IConduitTransactionScope> BeginAsync(CancellationToken ct)
    {
        var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        return new EfCoreTransactionScope(tx);
    }
}
```

`Conduit.Transactions.EntityFrameworkCore` (a thin adapter package) wraps `DbContext.Database.BeginTransactionAsync` — chosen over `SaveChanges`-implicit-transaction semantics because explicit transactions are required for the multi-`SaveChanges()`-call and multi-aggregate-write scenarios `ITransactionalRequest` exists to support.

## Design Decisions Summary

| Decision | Alternative Considered | Why Chosen |
|---|---|---|
| Opt-in marker interface | Infer from command naming convention | Naming-based inference is fragile and implicit — violates "no hidden magic." |
| `AsyncLocal`-based ambient tracking | `System.Transactions.TransactionScope` | Avoids DTC promotion risk and works cleanly with `async`/`await` without special handling. |
| Outermost-commits-only nesting | Each nested call opens its own transaction (savepoints) | Savepoints are a valid future enhancement (tracked in [Roadmap](25-roadmap.md)) but add complexity not justified for the initial release; outermost-only is simpler and matches the common case. |
| EF Core adapter as a separate package | Bake EF Core dependency into `Conduit.Transactions` core | Keeps the OSS core free of a specific ORM dependency — consumers using Dapper/ADO.NET directly implement `IConduitTransactionFactory` themselves with zero forced EF Core reference. |
