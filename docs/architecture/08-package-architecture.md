# 08 — Package Architecture

## Packaging Philosophy

Every NuGet package Conduit ships answers exactly one question about "what capability am I adding to the pipeline?" This mirrors the Unix philosophy applied to DI-based frameworks: small, composable packages instead of one monolithic `Conduit.dll`. A consumer who only wants the dispatcher and pipeline takes `Conduit.Core` + `Conduit.Pipeline` + `Conduit.DependencyInjection` and nothing else ships in their deployable — critical for Native AOT binary size and startup time.

## OSS Core Packages (MIT-licensed, free forever)

| Package | Contains |
|---|---|
| `Conduit.Abstractions` | Contracts only. |
| `Conduit.Core` | Dispatcher contracts (`ISender`, `IPublisher`), exceptions, context types. |
| `Conduit.Pipeline` | Behavior contracts, pipeline composition primitives. |
| `Conduit.SourceGenerators` | Incremental Generator (compile-time only, `PrivateAssets="all"`). |
| `Conduit.Analyzers` | Diagnostics + code fixes (compile-time only). |
| `Conduit.DependencyInjection` | `AddConduit()` and DI wiring. |
| `Conduit.AspNetCore` / `Conduit.MinimalApis` / `Conduit.Aspire` | Hosting integrations. |
| `Conduit.Diagnostics` | Basic pipeline introspection, execution tracing, health checks. |
| `Conduit.Telemetry` | OpenTelemetry `ActivitySource`/`Meter` instrumentation. |
| `Conduit.Validation` | Validation behavior abstraction + FluentValidation adapter. |
| `Conduit.Authorization` | Policy/claims authorization behavior. |
| `Conduit.Caching` | Memory/distributed/hybrid caching behavior. |
| `Conduit.Retry` | Transient retry behavior. |
| `Conduit.Transactions` | Ambient/nested transaction behavior. |
| `Conduit.Testing` | Test doubles and harnesses. |

**Guarantee**: none of these packages will ever require a commercial license, and none of them will ever take a dependency (direct or transitive) on a commercial package. This is enforced structurally (Layer 6 depends on Layer 5, never the reverse — see [High-Level Architecture](03-high-level-architecture.md)) and via a CI dependency-graph check.

## Commercial / Open-Core Packages ("Conduit Pro" / "Conduit Enterprise" tier)

| Package | Extends | Capability | Why it's commercial |
|---|---|---|---|
| `Conduit.Diagnostics.Pro` | `Conduit.Diagnostics` | Advanced execution tracing with full payload capture, pipeline replay, time-travel debugging of historical requests | Storage/retention infrastructure and advanced UI are ongoing operational costs beyond a library's scope. |
| `Conduit.Visualizer` | `Conduit.Diagnostics` | Live, web-based pipeline graph UI rendering real-time execution overlaid on the compile-time-known pipeline shape | A hosted/embedded UI product, not a library concern — clear value-add above "just a package." |
| `Conduit.Analytics` | `Conduit.Telemetry` | Historical performance analytics, SLA dashboards, cross-deployment regression detection | Requires a data pipeline/storage backend; mainly valuable to teams operating at scale. |
| `Conduit.Enterprise` | `Conduit.Authorization` | Multi-tenant policy bundles, audit-trail export, support SLA hooks | Governance/compliance features primarily needed by larger organizations willing to pay for support. |
| `Conduit.Azure` | `Conduit.Telemetry` | Azure Key Vault-backed secrets for behaviors, Service Bus notification transport, Azure Monitor exporters | Cloud-vendor-specific integration work with ongoing maintenance tied to Azure API changes. |
| `Conduit.Observability` | `Conduit.Telemetry` | Advanced OTel exporters, adaptive sampling profiles, vendor-specific dashboard templates | Niche, high-maintenance integrations that most OSS users never touch. |
| `Conduit.PolicyEngine` | `Conduit.Authorization` | Declarative DSL for authorization/retry/caching policies, hot-reloadable without redeployment | A policy compiler + runtime is a distinct product surface, justifying a separate commercial license. |

## OSS vs. Pro/Enterprise Comparison

| Capability | OSS Core | Pro / Enterprise |
|---|---|---|
| Request dispatch, pipeline, handlers | ✅ | ✅ (inherited) |
| Native AOT support | ✅ | ✅ |
| Basic execution tracing / health checks | ✅ (`Conduit.Diagnostics`) | — |
| Full-payload tracing + replay | — | ✅ (`Conduit.Diagnostics.Pro`) |
| Live pipeline visualization UI | — | ✅ (`Conduit.Visualizer`) |
| Basic OpenTelemetry export | ✅ (`Conduit.Telemetry`) | — |
| Historical analytics / SLA dashboards | — | ✅ (`Conduit.Analytics`) |
| Advanced exporters & sampling | — | ✅ (`Conduit.Observability`) |
| Claims/policy authorization | ✅ (`Conduit.Authorization`) | — |
| Multi-tenant governance & audit | — | ✅ (`Conduit.Enterprise`) |
| Declarative policy DSL, hot reload | — | ✅ (`Conduit.PolicyEngine`) |
| Azure-native secret/transport integration | — | ✅ (`Conduit.Azure`) |
| Support SLA | Community (GitHub issues) | Contractual SLA |

## Monetization Rationale

The split follows the principle: **"the pipeline is infrastructure, insight is a product."** Anything a single application needs to *function correctly* (dispatch, handlers, behaviors, validation, basic caching/retry/telemetry) stays free and MIT-licensed — this maximizes adoption and trust, and is a strict requirement for Native AOT/enterprise evaluation cycles where a "some functionality requires a paid license" story kills adoption outright. Anything that requires **operating at scale** (visual tooling, cross-deployment analytics, multi-tenant governance, cloud-vendor-specific integration maintenance) becomes commercial, because:

1. These features have ongoing operational costs (storage, UI hosting, vendor API maintenance) that don't fit a "download a NuGet package" cost model.
2. They are disproportionately valuable to organizations with budget (platform teams, enterprises), not individual developers or small teams — a fair place to draw the free/paid line.
3. This mirrors proven open-core models (Sentry OSS vs Sentry Cloud, Datadog Agent vs Datadog SaaS, GitLab CE vs EE): the core product that drives adoption is free; the tooling that scales with organizational size is paid.

## Package Naming & Versioning Convention

- OSS packages: `Conduit.<Module>` — SemVer, single version train, released together as `Conduit vX.Y.Z`.
- Commercial packages: `Conduit.<Module>.Pro` or a standalone commercial name (`Conduit.Visualizer`, `Conduit.Enterprise`) — versioned independently, may lag or lead the OSS release train, but always documents the minimum compatible `Conduit.Core` version.
- All commercial packages require a license key validated at startup via `IConduitLicense` (checked once during `AddConduit()` composition, not per-request — no runtime performance tax for license validation).
