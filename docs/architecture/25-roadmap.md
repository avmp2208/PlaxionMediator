# 25 — Roadmap

## Phase 1 — Core Pipeline (Foundation)

**Goal**: a working, benchmarked, Native AOT-verified request/response pipeline with zero reflection.

- `Conduit.Abstractions`, `Conduit.Core`, `Conduit.Pipeline`.
- `Conduit.SourceGenerators` (handler/behavior registration, dispatcher generation).
- `Conduit.DependencyInjection` (`AddConduit()`).
- `Conduit.Testing` (`FakeSender`).
- Initial `Conduit.Benchmarks` suite (`SimpleCommandDispatch`, `FullPipelineDispatch`, `ColdStartRegistration`, `NativeAotColdStart`).

**Milestone**: `dotnet publish -p:PublishAot=true` succeeds with zero trim warnings for a sample app using Conduit; core benchmarks published.

## Phase 2 — Compile-Time Safety Net

**Goal**: the analyzer/tooling experience that makes Conduit safer to use than reflection-based alternatives.

**Depends on**: Phase 1 (analyzers/generator share the `HandlerModel`/`BehaviorModel` extraction library built in Phase 1).

- `Conduit.Analyzers` (full `CONDUIT0xx`–`CONDUIT09x` catalog from [Roslyn Analyzer Architecture](11-roslyn-analyzer-architecture.md)).
- `Conduit.AspNetCore`, `Conduit.MinimalApis`.
- `Conduit.Templates` (`dotnet new conduit-webapi`).
- `dotnet conduit` CLI tool (`graph`, `validate` commands).

**Milestone**: analyzer catalog covers all structural, cancellation, and lifetime mistake categories identified in this design; templates published to NuGet.

## Phase 3 — Cross-Cutting Modules & Observability

**Goal**: the OSS modules most production applications need, plus first-class OpenTelemetry.

**Depends on**: Phase 1 (pipeline behaviors), Phase 2 (templates demonstrating recommended module composition).

- `Conduit.Telemetry`, `Conduit.Diagnostics` (basic tier).
- `Conduit.Validation` (+ FluentValidation adapter).
- `Conduit.Authorization`.
- `Conduit.Caching`, `Conduit.Retry`, `Conduit.Transactions` (+ EF Core adapter).
- `Conduit.Aspire` integration.

**Milestone**: a reference sample app demonstrates the full recommended default pipeline ([Pipeline Architecture](12-pipeline-architecture.md#recommended-default-global-order)) end-to-end with OpenTelemetry export to a local collector.

## Phase 4 — Commercial / Enterprise Tier

**Goal**: launch the open-core monetization model on top of a proven, adopted OSS foundation.

**Depends on**: Phase 3 (commercial packages extend OSS `Diagnostics`/`Telemetry`/`Authorization` modules, which must be stable first).

- `Conduit.Diagnostics.Pro` (full-payload tracing, replay).
- `Conduit.Visualizer` (live pipeline graph UI).
- `Conduit.Analytics` (historical dashboards).
- `Conduit.Enterprise` (multi-tenant policy, audit export).
- `Conduit.PolicyEngine` (declarative policy DSL).
- `IConduitLicense` licensing infrastructure.

**Milestone**: first paying design partners onboarded; licensing/version-compatibility infrastructure ([Versioning Strategy](24-versioning-strategy.md)) validated in production.

## Phase 5 — Ecosystem Integrations & Scale

**Goal**: deepen ecosystem reach and address advanced/hyperscale scenarios identified but deferred earlier.

**Depends on**: Phase 4 (stable commercial billing/licensing model funds continued ecosystem investment); Phase 1 performance baseline (for the opt-in allocation-free mode).

- `Conduit.Azure` (Key Vault, Service Bus, Azure Monitor integrations).
- `Conduit.Observability` (adaptive sampling, vendor dashboard templates).
- Opt-in "AllocationFreeMode" struct-based pipeline continuation (deferred design from [Performance](21-performance.md#closures-vs-struct-continuations)).
- Nested-transaction savepoint support (deferred design from [Transactions](20-transactions.md#design-decisions-summary)).
- Compile-time `IConduitValidator<TRequest>` generation from DataAnnotations (deferred design from [Validation](16-validation.md#compile-time-validation-hooks)).
- Community-driven additional cache/transport adapters (Kafka, RabbitMQ community publishers).

**Milestone**: Conduit is a viable choice across the full spectrum from single-instance Minimal API to multi-region, high-scale enterprise deployment, with a self-sustaining OSS + commercial contribution model.
