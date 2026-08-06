# Roadmap

Full detail lives in [`ReleaseProcess/01-mvp-development-phases.md`](https://github.com/avmp2208/PlaxionMediator/blob/master/ReleaseProcess/01-mvp-development-phases.md) and [`ReleaseProcess/03-full-development-phases-oss.md`](https://github.com/avmp2208/PlaxionMediator/blob/master/ReleaseProcess/03-full-development-phases-oss.md).

| Phase | Version | Deliverable | Status |
|---|---|---|---|
| MVP Phase 0-2 | `v0.1.0` | Core packages, analyzers, testing, sample app, NuGet publish | ✅ Done |
| MVP Phase 3 | — | Real-world feedback loop (community/ops, ongoing) | ⏳ Ongoing |
| OSS Phase B | `v0.2.0` | ASP.NET Core / Minimal API integration (`AspNetCore`, `MinimalApis`) | ✅ Done |
| OSS Phase C | `v0.3.0` | Notifications/streams hardening, full analyzer catalog groundwork, OSS benchmarks | ✅ Done |
| OSS Phase D | `v0.4.0` | Validation, basic Caching/Retry (Pipeline simplification) | ✅ Done |
| OSS Phase D | `v0.4.1` | Core pipeline execution engine rewrite (field-staged `PipelineExecutor`, pooled `PipelineRunner`) | ✅ Done |
| OSS Phase D | `v0.4.2` | Circuit Breaker resilience behavior (`PlaxionMediator.Retry`, `Microsoft.Extensions.Resilience`) | ✅ Done |
| OSS Phase D | `v0.4.3` | Stabilization & tech-debt pass: perf/allocation tuning, analyzer false-positive hardening, doc refresh | ⏳ In progress |
| OSS Phase E | `v0.7.0` | Baseline OpenTelemetry/logging, Aspire integration | Later |
| OSS Phase F | `v0.8.0` | Basic Transactions, templates, CLI, full analyzer catalog | Later |
| OSS Phase G | `v0.9.0` | Basic Authorization | Later |
| OSS Phase H | `v1.0.0` | API stability commitment | Later |

Commercial/enterprise phases (validation policy DSLs, dashboards, multi-tenant policy bundles, etc.) are tracked separately and are **not** part of the free/OSS roadmap.
