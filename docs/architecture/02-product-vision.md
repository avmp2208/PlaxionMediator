# 02 — Product Vision

## Target Audience

- **.NET application teams** building modular monoliths, microservices, or Minimal API backends who want a CQRS-style request pipeline without the reflection/AOT tax.
- **Platform/infrastructure teams** at mid-to-large organizations who need enterprise features (multi-tenant policy, advanced observability, SLA dashboards) and are willing to pay for a supported commercial tier.
- **Library authors** who want to build reusable cross-cutting behaviors (caching, retry, validation) on a stable, source-generated foundation.
- **AOT-first / cloud-native teams** (Native AOT Lambda/Azure Functions, containers with fast cold-start requirements) for whom reflection-based frameworks are a non-starter.

## Use Cases

1. Command/query separation in a Minimal API or MVC backend (`ISender.Send(new CreateOrderCommand(...))`).
2. Cross-cutting concerns (logging, validation, caching, retry, transactions) applied uniformly via pipeline behaviors without touching handler code.
3. Domain event / notification fan-out (`IPublisher.Publish(new OrderShippedEvent(...))`) to multiple independent handlers.
4. Streaming request handling (server-streaming query results via `IAsyncEnumerable<T>`).
5. Native AOT-published services (Azure Functions isolated worker, minimal containers) where startup time and binary size matter.
6. Enterprise deployments needing pipeline visualization, historical performance analytics, and centralized authorization policy (commercial add-ons).

## Goals

- Zero-reflection, compile-time-verified request/handler wiring.
- First-class Native AOT support with no trimming warnings in the core packages.
- A public API small enough to be fully learned in under 30 minutes.
- Behaviors composed at compile time into a single delegate chain per request type (no per-call `IEnumerable` iteration).
- Deep `Microsoft.Extensions.*` integration (DI, Logging, Options, Hosting, Diagnostics/`ActivitySource`).
- A sustainable open-core business model that keeps the pipeline itself free forever.
- Roslyn analyzers that catch the majority of misconfiguration mistakes before the first `dotnet build` even finishes.

## Non-Goals

- Conduit is **not** a general-purpose service bus, message broker, or distributed messaging framework (no persistent queues, no external transport by default).
- Conduit does **not** aim for behavioral or API compatibility with any existing mediator library — no adapter/compat shim is planned.
- Conduit does **not** support runtime plugin loading of handlers from dynamically loaded assemblies (this would require reflection and is explicitly rejected).
- Conduit is **not** an ORM, validation engine, or logging framework — it integrates with best-of-breed `Microsoft.Extensions.*` and community libraries instead of reinventing them.

## Guiding Principles

1. **Compile time over runtime.** Any decision that can be made when the compiler has full type information must be made there, not deferred to a runtime lookup.
2. **The API is the product.** Every public type must be explainable in one sentence; if it can't, it's redesigned or removed.
3. **Immutability by default.** Requests, responses, and pipeline metadata are immutable; behaviors mutate the pipeline flow (via context/results), never a shared request instance.
4. **No hidden magic.** A developer must be able to answer "what happens when I call `Send`?" by looking at generated code — not by reading framework internals or debugging with reflection.
5. **Pay only for what you use.** Every capability beyond the OSS core is an opt-in package; taking a dependency on Conduit's dispatcher never pulls in validation, telemetry, or authorization code.
6. **Free forever where it matters, paid where it scales.** The request pipeline, DI, source generators, and core analyzers are permanently open source; commercial value is created in tooling that mainly benefits teams operating at scale (visual diagnostics, analytics, enterprise governance).
