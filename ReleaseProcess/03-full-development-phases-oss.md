# Full Development Phases — Open Source Track

> Scope note: this document is **separate** from `docs/architecture/` and from the two existing MVP/publishing documents (`01-mvp-development-phases.md`, `02-nuget-org-publishing-process.md`). Those documents get you from "nothing" to a published `v0.1.0`. This document picks up from there and lays out **every remaining open-source phase** of Conduit's development, end to end, phase by phase — everything that stays free/OSS forever.
>
> The commercial/enterprise phases are deliberately **not** in this file — see `04-full-development-phases-enterprise.md` for those. The split exists because the two tracks have different audiences (OSS contributors vs. paying customers), different cadences (community-driven vs. revenue-driven), and different decision-makers (you may eventually want outside contributors on this document, but not on the monetization one).

## How OSS vs. Enterprise was decided

The rule applied to sort every module from the architecture docs into "OSS forever" vs. "candidate for monetization" was:

1. **If it's required for the core value proposition to function at all** (dispatch a request, get a response, register handlers, run behaviors) → OSS. Charging for the pipeline itself kills adoption before there's anything to sell add-ons to.
2. **If it's a common, expected integration with the wider .NET ecosystem** (ASP.NET Core, Minimal APIs, basic validation/auth/caching/retry/transactions abstractions, standard OpenTelemetry emission) → OSS. These are "table stakes" — every competing library (MediatR + community packages, Wolverine) offers them for free, so charging for them is a non-starter and would just push users to a competitor.
3. **If it's operational/organizational tooling that only matters at scale** (visual pipeline debugging UI, historical analytics/SLA dashboards, multi-tenant policy bundles, vendor-specific cloud integrations, a declarative policy DSL, advanced tracing/replay) → commercial candidate. Individual hobbyists and small teams don't need these; teams that do need them are, by definition, deriving enough business value from Conduit to justify paying for it.

This document covers only the modules that satisfy rule 1 or rule 2.

---

## Phase A — MVP (recap, detailed in `01-mvp-development-phases.md`)

Already fully specified elsewhere. Included here only for continuity of numbering:

- Phase 0: Repo & tooling bootstrap.
- Phase 1: Minimal viable core (`Conduit.Abstractions`, `Conduit.Core`, `Conduit.Pipeline`, `Conduit.SourceGenerators`, `Conduit.DependencyInjection`) + `CONDUIT001` analyzer.
- Phase 2: Safety-net analyzers, `Conduit.Testing`, NuGet.org `v0.1.0` publish.
- Phase 3: Real-world feedback loop.

**Everything below is new** — it starts immediately after Phase 3 feedback has been triaged.

## Phase B — ASP.NET Core & Minimal API Integration (`v0.2.0`)

**Goal**: make Conduit a first-class citizen of web applications, since that's the dominant use case for a request pipeline.

- `Conduit.AspNetCore`: `UseConduitExceptionHandling()`, `ProblemDetails` mapping for `ConduitException` subtypes, middleware ordering guidance.
- `Conduit.MinimalApis`: `MapConduitPost<TRequest, TResponse>()` and verb-specific counterparts (`MapConduitGet`, `MapConduitPut`, `MapConduitDelete`), source-generator-assisted route-to-request binding.
- A second sample app (`samples/Conduit.Sample.WebApi`) exercising both packages end to end, published with `PublishAot=true`.
- Analyzer additions relevant to web usage: detect requests mapped to routes but missing request binding attributes, detect handlers that block on `.Result`/`.Wait()`.

**Exit criteria**: a developer can build a small CRUD API using only `Conduit.Core` + `Conduit.AspNetCore`/`Conduit.MinimalApis`, with no hand-written mediator glue code, verified via the sample app.

## Phase C — Notifications & Streaming Maturity (`v0.3.0`)

**Goal**: the two core-architecture concepts that exist in the design but get the least MVP attention — `INotification` fan-out and `IStreamRequest` — become production-hardened.

- Harden `IPublisher.Publish` execution strategies (sequential vs. parallel notification handler execution) as an explicit, documented, source-generator-emitted choice per notification type — not a hidden default.
- Harden `IStreamRequestHandler` cancellation propagation and backpressure behavior; add generator diagnostics for streaming misuse (e.g., a stream handler that buffers the entire sequence before yielding, defeating the point of streaming).
- Expand the analyzer catalog toward the full ~15-diagnostic set described in `docs/architecture/11-roslyn-analyzer-architecture.md`: Invalid Behavior, Invalid/Duplicate Registration, Performance Anti-Patterns, Incorrect Lifetime.
- Benchmark suite (`Conduit.Benchmarks`, OSS) stood up per `docs/architecture/22-benchmark-strategy.md`, published as a recurring CI job with results posted to the README/docs site — this becomes the OSS project's ongoing credibility evidence against alternatives.

**Exit criteria**: notifications and streams are exercised in the sample apps and covered by generator snapshot tests to the same standard as `Send`.

## Phase D — Core Cross-Cutting OSS Modules (`v0.4.0`–`v0.6.0`)

**Goal**: ship the cross-cutting concerns that are "expected by default" in any serious request-pipeline framework, keeping the advanced/operational variants of each for the commercial track.

Ship as three minor releases (not one), each independently valuable and independently announced:

- **`v0.4.0` — `Conduit.Validation`**: `IConduitValidator<TRequest>`, `ValidationBehavior<TRequest,TResponse>`, first-party FluentValidation adapter package (`Conduit.Validation.FluentValidation`). Chosen first because validation is the single most commonly requested pipeline behavior in community feedback for MediatR-style libraries.
- **`v0.5.0` — `Conduit.Authorization` (basic tier only)**: `IConduitAuthorizationHandler<TRequest>`, claims/role-based checks, ASP.NET Core `ClaimsPrincipal` integration. Explicitly **excludes** the declarative policy DSL and multi-tenant policy bundles — those are `Conduit.PolicyEngine` and `Conduit.Enterprise`, commercial.
- **`v0.6.0` — `Conduit.Caching` (memory + basic distributed only) and `Conduit.Retry` (basic backoff only)**: `ICacheKeyProvider<TRequest>`, `ICacheableRequest` backed by `IMemoryCache`/`IDistributedCache`; `IRetryPolicyProvider<TRequest>` backed by the community-standard `Microsoft.Extensions.Resilience`/Polly integration. Explicitly **excludes** Hybrid Cache SLA dashboards and adaptive/ML-driven retry tuning — those are commercial (`Conduit.Analytics`, `Conduit.Enterprise`).

**Exit criteria**: each module ships independently, with its own docs page, sample usage, and analyzer coverage where applicable; none requires a commercial package to function.

## Phase E — Baseline Observability (`v0.7.0`)

**Goal**: ship the OSS-tier telemetry and logging described in `docs/architecture/14-logging.md` and `docs/architecture/15-opentelemetry.md`, deliberately stopping short of the advanced tier reserved for `Conduit.Observability` (commercial).

- `Conduit.Telemetry` (OSS): `ActivitySource`/`Meter` emission per request/behavior/handler, standard tags (request type, handler type, outcome, duration), correlation ID propagation through `ILogger` scopes.
- Structured logging behavior with sensitive-data masking hooks (the masking *policy* is pluggable OSS; a pre-built PII-detection ruleset is a commercial `Conduit.Enterprise` add-on).
- OpenTelemetry Collector / Jaeger / Aspire dashboard sample wiring in `samples/`, since Aspire is a first-class target platform.
- This is the natural point to also ship **`Conduit.Aspire`**: Aspire component registration (`AddConduitServiceDefaults`) so Conduit shows up correctly in the Aspire dashboard out of the box.

**Exit criteria**: a Conduit request's full lifecycle (dispatch → behaviors → handler → response/exception) is visible end-to-end in an OpenTelemetry-compatible backend using only OSS packages.

## Phase F — Transactions & Tooling Polish (`v0.8.0`)

**Goal**: close out the remaining OSS modules from the architecture docs and invest in developer tooling that reduces adoption friction.

- `Conduit.Transactions` (basic tier): `ITransactionalRequest`, `IConduitTransactionScope`, first-party EF Core `DbContext` transaction adapter. Nested/ambient transaction *orchestration across multiple data stores* is deferred to the commercial `Conduit.Enterprise` tier (see the enterprise document) since it's a need specific to large, multi-datastore organizations.
- `Conduit.Templates`: `dotnet new conduit-webapi`, `dotnet new conduit-console` templates so new projects start from a working, idiomatic skeleton.
- Optional `dotnet conduit` CLI tool (OSS) for local diagnostics: list registered handlers/behaviors for the current project by reading the generator's emitted metadata (no runtime scanning — it's a build-time report, consistent with the zero-reflection principle).
- Full analyzer catalog completion (remaining diagnostics from `docs/architecture/11-roslyn-analyzer-architecture.md` not yet shipped).

**Exit criteria**: all OSS packages enumerated in `docs/architecture/08-package-architecture.md`'s open-core table are implemented, published, and documented; no more modules remain gated only by "not yet built" for the free tier.

## Phase G — `v1.0.0` — API Stability Commitment

**Goal**: graduate from "actively evolving pre-1.0" to a binary/source-compatible major version, per `docs/architecture/24-versioning-strategy.md`.

- Full public API review across every OSS package against `docs/architecture/06-public-api.md`; freeze anything that will carry a compatibility guarantee.
- Publish a formal deprecation/versioning policy page linked from the README.
- Announce `v1.0.0` as a dedicated milestone (blog post, release notes, community outreach) — this is the point where the OSS project actively starts building the audience the commercial tier (see the enterprise document) will later be offered to.

**Exit criteria**: `v1.0.0` published to NuGet.org; the OSS track is considered "complete" in the sense that all further OSS work is incremental (new analyzers, new integration adapters, bug fixes) rather than net-new modules.

---

## Summary Timeline (OSS track only)

| Phase | Version | Deliverable | Depends on |
|---|---|---|---|
| A | 0.1.0 | MVP core + NuGet publish (see MVP document) | — |
| B | 0.2.0 | ASP.NET Core / Minimal API integration | A |
| C | 0.3.0 | Notifications/streams hardening, full analyzer catalog groundwork, OSS benchmarks | B |
| D | 0.4.0–0.6.0 | Validation, basic Authorization, basic Caching/Retry | C |
| E | 0.7.0 | Baseline OpenTelemetry/logging, Aspire integration | D |
| F | 0.8.0 | Basic Transactions, templates, CLI, full analyzer catalog | E |
| G | 1.0.0 | API stability commitment | F |

Once `v1.0.0` ships, ongoing OSS work becomes maintenance-mode (see Ongoing Release Cadence in `02-nuget-org-publishing-process.md`), and effort can shift toward the commercial track described in `04-full-development-phases-enterprise.md` — building on top of, never replacing, the free core.
