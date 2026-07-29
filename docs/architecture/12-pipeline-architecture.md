# 12 — Pipeline Architecture

## Behaviors as Ordered Decorators

A pipeline behavior is a decorator around the next step (another behavior or the handler). Conduit rejects two common alternatives before landing on explicit ordering:

| Approach | Description | Verdict |
|---|---|---|
| Attribute-based ordering (`[Order(1)]`) | Behavior declares its own priority via attribute | Rejected as the *only* mechanism — attributes read at runtime require reflection; read at compile time by the generator they become viable, but still can't express "run X before Y only for request Z" without extra ceremony. |
| Convention-based (name/namespace sorting) | Behaviors execute in alphabetical/namespace order | Rejected — implicit, fragile, breaks on rename, impossible to reason about from calling code. |
| **Explicit registration order (chosen)** | Order in `ConduitOptions.GlobalBehaviors` / `PipelineBuilder.Use<T>()` calls defines execution order | Adopted — order is visible at the single registration call site, verified by the generator, and requires no attribute reflection. |

Global behaviors execute in the order added to `ConduitOptions.GlobalBehaviors`; per-request behaviors (registered via a generated `PipelineBuilder` for a specific request type) execute after global behaviors, in declared order, immediately before the handler.

## Composition Model

```csharp
builder.Services.AddConduit(options =>
{
    options.GlobalBehaviors.Add(typeof(LoggingBehavior<,>));      // 1st for every request
    options.GlobalBehaviors.Add(typeof(ValidationBehavior<,>));   // 2nd for every request
});

// Per-request customization via generated partial configuration (illustrative)
partial class ConduitPipelineConfiguration
{
    static partial void ConfigureCreateOrderCommand(PipelineBuilder builder) =>
        builder.Use<CachingBehavior<CreateOrderCommand, OrderId>>();  // 3rd, only for this request
}
```

The generator combines global + per-request behaviors into one fixed, ordered list per request type at compile time — there is no runtime merge step.

## Global vs. Per-Request Behaviors

- **Global behaviors** apply to every request in the compilation; typically cross-cutting infrastructure concerns (logging, telemetry, exception translation) that must be universally present for observability guarantees to hold.
- **Per-request behaviors** apply to a specific request type; typically domain-specific concerns (caching only cacheable queries, transactions only on commands touching multiple aggregates).

## Conditional Execution

Behaviors that should apply to a subset of requests, but aren't worth writing as a dedicated per-request registration, use `PipelineBuilder.UseWhen<TBehavior>(predicate)`:

```csharp
builder.UseWhen<AuditBehavior<TRequest, TResponse>>(t => typeof(IAuditableRequest).IsAssignableFrom(t));
```

The predicate itself runs at **generator time** (evaluated against compile-time-known types), not per-call at runtime — so conditional inclusion has zero runtime cost; the generated chain for a non-matching request simply never includes the behavior's call.

## Filters / Middleware Concepts

Conduit deliberately does **not** introduce a separate "filter" concept distinct from behaviors (unlike some frameworks that have both middleware and filters as separate abstractions). A single unified `IPipelineBehavior<TRequest,TResponse>` abstraction, differentiated only by *registration scope* (global/per-request/conditional), is simpler to teach and avoids the "which one do I use" decision fatigue of a two-abstraction system.

## Error Handling in the Pipeline

- A behavior may catch and translate exceptions from `next()` (e.g., `Conduit.Transactions`' rollback-on-exception behavior) but must always either rethrow or return a valid `TResponse` — a behavior silently swallowing an exception and returning `default!` is flagged by analyzer `CONDUIT023` (data-flow heuristic: `catch` block with no `throw`/no assignment before behavior's return).
- Unhandled exceptions propagate to the caller of `Send`/`Publish` wrapped in `PipelineExecutionException` only when raised by a behavior invoking `next()`, per [Internal Architecture](07-internal-architecture.md#exception-propagation).

## Cancellation Propagation

Every behavior receives the same `CancellationToken` passed to `Send`. A behavior wanting a derived token (e.g., a timeout behavior) creates a `CancellationTokenSource.CreateLinkedTokenSource` locally and passes the linked token to `next()` — this is explicit in behavior code, never hidden pipeline-wide state.

## Priority — Resolving Ambiguity

When a behavior appears in both `GlobalBehaviors` and a per-request `PipelineBuilder`, the generator reports `CONDUIT021` (Duplicate Registration) rather than silently de-duplicating — silent de-duplication would hide a likely developer mistake (unclear which position "wins").

## Recommended Default Global Order

1. `ExceptionTranslationBehavior` (from `Conduit.AspNetCore`, if referenced) — outermost, so it can wrap every downstream fault.
2. `LoggingBehavior` (`Conduit.Telemetry`) — captures the full request lifetime including validation/failures.
3. `ValidationBehavior` (`Conduit.Validation`) — fail fast before touching authorization/caching/handler.
4. `AuthorizationBehavior` (`Conduit.Authorization`) — after validation (don't leak authorization decisions for structurally invalid requests), before caching/handler.
5. `CachingBehavior` (`Conduit.Caching`) — per-request, only for `ICacheableRequest`.
6. `RetryBehavior` (`Conduit.Retry`) — innermost around the handler call, so retries don't re-run validation/authorization/logging per attempt.
7. Handler.

This ordering is a **recommendation shipped in the template** (`Conduit.Templates`), not a hardcoded framework default — consumers can reorder freely, but the template embodies the first-principles reasoning above.
