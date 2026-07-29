# ADR-0002: Immutable Request Contracts

## Status

Accepted

## Context

Requests flow through a pipeline of behaviors, may be logged, cached, retried, and (in the commercial `Conduit.Diagnostics.Pro` tier) replayed. A mutable request risks a behavior observing a different value than the one the handler ultimately sees (if a downstream behavior mutates it), makes caching/replay unsound (the "key" used to cache might not reflect the value actually processed), and is inherently unsafe to share across threads in streaming/notification fan-out scenarios.

## Decision

Every `IRequest<TResponse>` implementation must be a `sealed record` (or `readonly record struct` for very small, high-frequency requests). This is enforced by Roslyn analyzer `CONDUIT010`, not merely documented as a convention.

## Alternatives Considered

1. **Mutable POCO requests with `{ get; set; }` properties** (the common pattern in reflection-based mediators) — rejected: allows accidental mutation mid-pipeline, breaks caching/replay soundness, requires defensive copying for thread safety in fan-out scenarios.
2. **Immutable by convention only** (recommend `record` in docs, don't enforce) — rejected: "no hidden magic" and "compile time over runtime" principles demand that a violated invariant be a build error, not a documentation suggestion a developer can silently ignore.
3. **Immutable by construction, analyzer-enforced (chosen)** — makes the guarantee real and machine-checked.

## Tradeoffs

- Slightly less familiar to developers coming from mutable-DTO-heavy codebases; requires an adjustment period and template-provided examples.
- `record`'s structural equality/`ToString()` is leveraged elsewhere (default cache key generation, [Caching](../18-caching.md#key-design)) — a deliberate secondary benefit, but it does mean requests with complex reference-type fields need to consider whether structural equality remains meaningful for their case (documented as guidance, not a hard block).

## Consequences

- Requests are safe to log, cache, and pass across threads/async boundaries without defensive copying.
- Cache key generation, request graph analysis, and future replay tooling can rely on structural equality/representation without special-casing mutable state.
- The analyzer (`CONDUIT010`) becomes a permanent, load-bearing part of the developer experience — its accuracy and code-fix quality are treated as a first-class deliverable, not an afterthought.
