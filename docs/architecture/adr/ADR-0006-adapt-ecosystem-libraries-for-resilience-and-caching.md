# ADR-0006: Adapt Ecosystem Libraries for Resilience and Caching Instead of Reimplementing

## Status

Accepted

## Context

Retry/backoff/circuit-breaking and caching are both well-solved problems in the current .NET ecosystem via `Microsoft.Extensions.Resilience` (Polly-based) and `Microsoft.Extensions.Caching.Hybrid` (`HybridCache`). Building Conduit-specific reimplementations of either would duplicate substantial, already-hardened logic (jittered backoff algorithms, cache stampede protection) for no first-principles benefit, contradicting the project's stated goal of innovating specifically on the *pipeline* model rather than every adjacent concern.

## Decision

`Conduit.Retry` and `Conduit.Caching` are thin pipeline-behavior **adapters** over `Microsoft.Extensions.Resilience` and `Microsoft.Extensions.Caching.Hybrid` respectively, not independent implementations of retry or caching algorithms.

## Alternatives Considered

1. **Reimplement retry/caching logic within Conduit** — rejected: duplicates effort, introduces a second place for retry/caching bugs to hide, and forgoes the OpenTelemetry integration and community trust these libraries already carry.
2. **Require consumers to wire resilience/caching themselves outside the pipeline** — rejected: reintroduces the exact "hand-written glue code per cross-cutting concern" problem pipeline behaviors exist to solve; also loses the opt-in-per-request-type safety model (`ICacheableRequest`, `IRetryableRequest`) that keeps these concerns from being applied by mistake.
3. **Thin adapter behaviors over best-of-breed ecosystem libraries (chosen)**.

## Tradeoffs

- Conduit's caching/retry behavior quality is now coupled to the quality and pace of `Microsoft.Extensions.Resilience`/`Microsoft.Extensions.Caching.Hybrid` — an accepted dependency given both are first-party Microsoft libraries with long-term support commitments.
- Consumers on older cache/resilience APIs (pre-`HybridCache`) need the `IMemoryCache`/`IDistributedCache` adapter fallback, adding a small amount of surface area to maintain.

## Consequences

- `Conduit.Retry`/`Conduit.Caching` are intentionally small packages — their value is the opt-in safety markers (`ICacheableRequest`, `IRetryableRequest`) and pipeline integration, not algorithmic innovation.
- Telemetry for retries/cache hits automatically benefits from these libraries' own OpenTelemetry instrumentation, requiring no duplicate work in `Conduit.Telemetry`.
- This ADR establishes a general precedent (referenced in [Authorization](../17-authorization.md) for `IAuthorizationService`, and [Transactions](../20-transactions.md) for EF Core) for preferring ecosystem-standard building blocks over reinventing them, reserving Conduit's own innovation budget for the pipeline/dispatch model itself.
