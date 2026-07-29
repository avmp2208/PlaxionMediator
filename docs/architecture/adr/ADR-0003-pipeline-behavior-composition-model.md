# ADR-0003: Pipeline Behavior Composition Model

## Status

Accepted

## Context

Cross-cutting concerns (logging, validation, caching, retry, etc.) need a consistent, ordered way to wrap handler execution. The order behaviors run in materially affects correctness (e.g., authorization must run after validation, retry must wrap only the handler, not upstream behaviors) and must be easy to reason about without running the application.

## Decision

Behaviors are composed via **explicit, generator-verified registration order** (`ConduitOptions.GlobalBehaviors` list order, `PipelineBuilder.Use<T>()` call order per request), compiled into a single generated delegate chain per request type. There is no attribute-based (`[Order(n)]`) or convention-based (naming/namespace) ordering as the primary mechanism.

## Alternatives Considered

1. **Attribute-based priority** — rejected as the primary mechanism: still requires either reflection at runtime (violates non-negotiables) or generator-side attribute parsing that adds complexity without letting a reviewer see the *actual* execution order without cross-referencing every behavior's attribute value; explicit registration order is strictly more readable from a single call site.
2. **Convention-based (alphabetical/namespace) ordering** — rejected: implicit and fragile; renaming a class or moving a namespace silently changes execution order, which is exactly the kind of "hidden magic" the non-negotiables prohibit.
3. **Explicit registration order, compiled to a fixed per-request delegate chain (chosen)**.

## Tradeoffs

- Adding a new global behavior requires touching the central `AddConduit()` configuration call, rather than being fully "discoverable" purely by the behavior's own attributes — considered an acceptable and even desirable friction point, since behavior order is an architecturally significant decision that deserves a visible change in the composition root.
- Per-request behavior configuration uses a generator-emitted partial method per request type ([Pipeline Architecture](../12-pipeline-architecture.md)), which is a new pattern for developers unfamiliar with source-generator-driven partial method conventions.

## Consequences

- The exact execution order for any request is always determinable by reading `AddConduit()` plus that request's `ConfigureXxx` partial method — no need to run the application or inspect attributes reflectively.
- The generator can detect and flag ordering conflicts (`CONDUIT021` Duplicate Registration) at compile time.
- Because the chain is fixed per request type at compile time, the JIT/AOT compiler can potentially devirtualize/inline short behaviors — a direct performance benefit of the explicit, static composition model (see [Performance](../21-performance.md)).
