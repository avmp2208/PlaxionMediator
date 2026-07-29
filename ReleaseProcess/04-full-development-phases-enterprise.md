# Full Development Phases — Commercial / Enterprise Track

> Scope note: this document is **separate** from `docs/architecture/`, `01-mvp-development-phases.md`, `02-nuget-org-publishing-process.md`, and `03-full-development-phases-oss.md`. Those documents cover the free, open-source path from "nothing" through `v1.0.0`. This document covers **only the modules and phases you intend to monetize**: `Conduit.Diagnostics.Pro`, `Conduit.Visualizer`, `Conduit.Analytics`, `Conduit.Enterprise`, `Conduit.Azure`, `Conduit.Observability`, `Conduit.PolicyEngine`.
>
> **This track must not start before the OSS track has real, demonstrated adoption.** Building commercial modules before there's an audience to sell to is the single biggest risk called out in `docs/architecture/26-risks.md`'s adoption-risk section — there is no one to convert into a paying customer, and it starves the OSS core of the polish it needs to earn trust in the first place. Treat "OSS `v1.0.0` published + non-trivial GitHub stars/NuGet download trend" as the hard gate before opening this document's Phase 1.

## Why these seven modules, specifically

Every module here was selected using the inverse of the OSS-selection rule in `03-full-development-phases-oss.md`: it is valuable primarily to **organizations**, not individual developers, and its value scales with the size/complexity of the organization using Conduit — which is exactly the segment able and willing to pay for software.

| Module | What it sells | Why it's not OSS |
|---|---|---|
| `Conduit.Diagnostics.Pro` | Advanced execution tracing, pipeline replay/time-travel debugging | Deep production-debugging tooling is a "nice to have until you're on-call at 3am," at which point it's worth paying for; building/maintaining a replay engine is expensive and not needed to prove the core pipeline works. |
| `Conduit.Visualizer` | Live, browser-based pipeline graph UI | A polished UI product is a different engineering discipline (frontend, real-time updates) than a request pipeline library; bundling it free would mean subsidizing UI development from a library project. |
| `Conduit.Analytics` | Historical performance analytics, SLA dashboards, trend reports | Requires a hosted or self-hosted data pipeline/storage component — genuine infrastructure cost that scales with usage, which is the classic SaaS monetization shape. |
| `Conduit.Enterprise` | Multi-tenant policy bundles, prioritized support SLAs, advanced multi-datastore transaction orchestration | "Enterprise" concerns (multi-tenancy, support contracts, compliance-adjacent guarantees) are purchased by procurement teams, not individual developers — a natural license-per-organization fit. |
| `Conduit.Azure` | Azure-native telemetry export, Key Vault-backed secrets for pipeline config, Service Bus-backed notification transport | Cloud-vendor-specific integrations have an ongoing maintenance burden tied to a specific vendor's API surface, and are only relevant to teams already paying Azure — a natural adjacent upsell. |
| `Conduit.Observability` | Advanced OpenTelemetry exporters, adaptive sampling profiles, anomaly detection on emitted metrics | Baseline OpenTelemetry emission is OSS (table stakes); *tuning and interpreting* telemetry at scale is a specialized, ongoing-value product. |
| `Conduit.PolicyEngine` | Declarative authorization/retry/caching policy DSL with a policy-as-config authoring experience | A full DSL + authoring/validation tooling is a meaningfully larger engineering investment than the basic code-first `IConduitAuthorizationHandler<T>`/`IRetryPolicyProvider<T>` shipped OSS; it targets platform teams standardizing policy across many services. |

## Commercial Prerequisites (before writing any code in this track)

- **Licensing model decision**: recommend a **dual-license** approach — OSS core stays MIT/Apache-2.0 forever (non-negotiable, protects adoption); commercial packages ship under a proprietary EULA distributed via a private NuGet feed (Azure Artifacts / GitHub Packages with access tokens), not NuGet.org. Do not attempt "source-available with a commercial-use restriction" on the core — it would contradict the OSS positioning already built.
- **Legal entity**: form or confirm a legal entity (LLC or equivalent) to hold contracts, issue invoices, and own the commercial EULA/trademark for the `Conduit` name before taking the first payment.
- **Trademark**: file for a `Conduit` (or `Conduit.dev`/whatever the branded domain is) trademark once the OSS project has visible traction — this protects the commercial brand from the exact copying risk you'd otherwise be exposed to by someone else white-labeling your OSS core.
- **Pricing model decision**: recommend **per-organization annual licensing** (not per-seat, not per-request-volume) for `Conduit.Enterprise`/`Conduit.PolicyEngine`, and **usage-tiered SaaS pricing** for `Conduit.Analytics` (since it has real hosting costs). `Conduit.Diagnostics.Pro`, `Conduit.Visualizer`, `Conduit.Azure`, `Conduit.Observability` are best sold as an **add-on bundle** rather than priced individually — reduces decision fatigue for buyers and matches how Sentry/Datadog bundle their paid tiers.
- **Billing/license-key infrastructure**: needed before Phase 1 ships — a lightweight license-key validation mechanism embedded in the commercial packages (checked at startup, fails closed with a clear error, never phones home per-request — respects the zero-hidden-runtime-cost principle even in the paid tier).

---

## Phase 1 — `Conduit.Diagnostics.Pro` (first commercial release)

**Goal**: ship the first paid package, deliberately chosen because it builds directly on top of the already-shipped OSS `Conduit.Telemetry`/`Conduit.Diagnostics` foundation (Phase E of the OSS track) rather than requiring new infrastructure — lowest engineering risk for "first dollar."

- Execution tracing beyond OpenTelemetry spans: full request/response payload capture (opt-in, size-bounded) correlated to a trace ID, retained locally or in a lightweight embedded store.
- Pipeline replay: given a captured trace, re-execute the same request/behavior chain against a debug build for local reproduction — the flagship differentiator that justifies payment.
- Distributed under a private feed; license key required to activate the DI extension (`AddConduitDiagnosticsPro(licenseKey)`), fails fast with an actionable error if missing/invalid/expired.
- Pricing: part of the "Pro add-on bundle" (see prerequisites).

**Exit criteria**: at least one paying design-partner customer (ideally one of the OSS project's early adopters from Phase 3 feedback) using it in a non-trivial application.

## Phase 2 — `Conduit.Visualizer`

**Goal**: the highest-visibility, easiest-to-demo commercial product — a live pipeline graph — used both as a revenue product and as a sales/marketing asset for the whole commercial line.

- Real-time browser UI (likely a small ASP.NET Core-hosted SignalR/Blazor app or a standalone SPA) rendering the request→behavior→handler graph as requests flow through a running application, sourced from `Conduit.Telemetry` + `Conduit.Diagnostics.Pro` data.
- Local/dev-mode view (bundled with the license) plus an optional hosted view for shared team dashboards (ties into `Conduit.Analytics` infrastructure — natural upsell path).
- Recorded demo/GIF of this becomes the centerpiece of commercial-tier marketing material — invest in polish here disproportionately to its own revenue, because of this marketing leverage.

**Exit criteria**: a public demo video/sandbox exists; conversion rate from OSS users trying the free demo mode to paid licenses is being tracked.

## Phase 3 — `Conduit.Observability`

**Goal**: extend the OSS baseline OpenTelemetry emission (already shipped in the OSS track's Phase E) with the tuning/interpretation layer that only matters once a team runs Conduit at meaningful production scale.

- Advanced exporters (vendor-specific formats/back-pressure-aware batching beyond the OSS OTLP default).
- Adaptive sampling profiles (dynamically adjust trace sampling rate based on error rate/load, rather than a fixed static sample rate).
- Baseline anomaly detection on emitted metrics (e.g., flag a handler whose p99 latency regresses beyond a rolling baseline) — feeds naturally into `Conduit.Analytics` if the customer also has that tier.

**Exit criteria**: shipped as an add-on to the same bundle as Phase 1/2; documented integration with at least one major APM vendor (Azure Monitor, Datadog, or Grafana) as a proof point.

## Phase 4 — `Conduit.Azure`

**Goal**: capture the largest single cloud-vendor audience with native integrations, timed deliberately after the vendor-agnostic `Conduit.Observability` so Azure-specific code reuses that foundation rather than duplicating it.

- Azure Monitor/Application Insights native exporter (thin adapter over `Conduit.Observability`'s exporter abstraction).
- Key Vault-backed configuration source for pipeline behavior options (e.g., retry policy secrets, cache connection strings) — avoids plaintext secrets in `ConduitOptions`.
- Azure Service Bus-backed `IPublisher` transport, letting `INotification` fan-out cross process/service boundaries instead of staying in-process — a genuine capability upgrade, not just telemetry glue.

**Exit criteria**: one reference architecture doc + sample showing Conduit running in an Azure Container Apps/AKS deployment using all three integrations.

## Phase 5 — `Conduit.PolicyEngine`

**Goal**: the most engineering-intensive commercial module — a declarative, config-driven policy authoring layer sitting on top of the OSS code-first `IConduitAuthorizationHandler<T>`/`IRetryPolicyProvider<T>`/`ICacheableRequest` abstractions — deliberately sequenced late because it depends on those OSS abstractions being stable (post OSS `v1.0.0`).

- A YAML/JSON (or Roslyn-generated strongly-typed config) DSL for expressing authorization rules, retry policies, and cache policies without writing C# per request type — targeted at platform teams standardizing policy across dozens/hundreds of services.
- A validation/authoring tool (CLI or Visualizer-integrated) that lints policy files at build time — consistent with the zero-runtime-surprises principle even for a config-driven feature.
- Policy hot-reload support scoped only to non-security-critical policies (retry/caching); authorization policy changes require a deploy, by design, to avoid a security-relevant runtime side channel.

**Exit criteria**: at least one enterprise design partner actively replacing hand-written policy code with the DSL in a real deployment.

## Phase 6 — `Conduit.Analytics`

**Goal**: the module with genuine ongoing hosting costs, sequenced last among the "product" modules because it requires the most operational maturity (a real hosted backend, data retention policy, multi-tenant data isolation) — don't take on that operational burden before the commercial line has proven it can sell simpler, self-hosted add-ons first.

- Ingestion pipeline (likely built on the same OpenTelemetry data already emitted OSS + `Conduit.Observability`) into a time-series/analytics store.
- Historical dashboards: request volume trends, latency percentiles over time, error-rate trends, per-handler cost attribution.
- SLA reporting: automated periodic reports against customer-defined SLOs, exportable for compliance/audit purposes.
- This is the first module that is genuinely SaaS (hosted by you) rather than "library you install" — requires its own uptime/security/data-processing-agreement considerations before selling to enterprise customers with compliance requirements.

**Exit criteria**: hosted analytics service live with defined uptime SLA, at least one customer on a paid analytics tier.

## Phase 7 — `Conduit.Enterprise` (capstone bundle)

**Goal**: the umbrella package for the concerns that only matter to the largest customers, sequenced last because each of its components depends on earlier commercial phases being production-proven individually.

- Multi-tenant policy bundles built on top of `Conduit.PolicyEngine` (Phase 5).
- Advanced multi-datastore ambient transaction orchestration, extending the OSS `Conduit.Transactions` basic tier (single-`DbContext` EF Core support) to coordinate transactions across multiple databases/message brokers.
- Prioritized support SLA (contractual response-time guarantees) — an organizational/business commitment, not a code deliverable, but gated here because you shouldn't sell support SLAs before Phases 1–6 have proven the product surface is stable enough to support at that level.
- Single sign-on / audit-log requirements commonly requested by large procurement processes (e.g., SOC 2 readiness questionnaire support) — evaluate on demand from active enterprise sales conversations rather than building speculatively.

**Exit criteria**: first `Conduit.Enterprise` contract signed with a support SLA in place.

---

## Explicit Sequencing Rationale (why this order and not another)

1. **Diagnostics.Pro first** — smallest new infrastructure, directly reuses OSS telemetry, fastest path to first revenue.
2. **Visualizer second** — highest marketing leverage, justifies disproportionate investment relative to its own direct revenue.
3. **Observability third** — natural technical extension of what Diagnostics.Pro/Visualizer already consume.
4. **Azure fourth** — reuses Observability's exporter abstraction; captures a large single-vendor audience.
5. **PolicyEngine fifth** — needs the OSS authorization/retry/caching abstractions to be post-`v1.0.0` stable before building a DSL on top of them.
6. **Analytics sixth** — the first genuinely hosted/SaaS component; deliberately not first because it carries the highest ongoing operational cost and risk.
7. **Enterprise last** — a bundle/capstone that depends on nearly everything else (PolicyEngine, Transactions, support processes) already existing and being proven.

## Summary Timeline (Commercial track only)

| Phase | Package | Depends on |
|---|---|---|
| Gate | — (OSS `v1.0.0` + demonstrated adoption) | Full OSS track |
| 1 | `Conduit.Diagnostics.Pro` | OSS Phase E (`Conduit.Telemetry`) |
| 2 | `Conduit.Visualizer` | Phase 1 |
| 3 | `Conduit.Observability` | OSS Phase E, Phase 1 |
| 4 | `Conduit.Azure` | Phase 3 |
| 5 | `Conduit.PolicyEngine` | OSS `v1.0.0` (stable Authorization/Retry/Caching APIs) |
| 6 | `Conduit.Analytics` | Phase 3 (telemetry data source) |
| 7 | `Conduit.Enterprise` | Phases 5 and 6, plus OSS `Conduit.Transactions` |

Nothing in this document blocks or modifies anything in `docs/architecture/` or the other `ReleaseProcess/` documents — it is purely a sequencing and go/no-go decision aid for when and whether to start building the commercial tier.
