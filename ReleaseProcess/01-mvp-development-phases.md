# MVP Development Phases — Path to a Public v0.1 Release

> Scope note: this document is **separate** from `docs/architecture/`. The architecture docs describe the full, long-term design of Conduit (26 documents + ADRs). This document answers a narrower, more practical question: **"What is the smallest slice of that design I actually need to build, in what order, to ship a real MVP that people can `dotnet add package` and use?"**
>
> This is a personal execution roadmap for the maintainer(s), not a marketing roadmap. It intentionally ignores commercial/open-core packages (`Conduit.Diagnostics.Pro`, `Conduit.Visualizer`, `Conduit.Analytics`, `Conduit.Enterprise`, `Conduit.Azure`, `Conduit.Observability`, `Conduit.PolicyEngine`) — none of them are required for an MVP and building them first would delay release for no benefit.

## Definition of "MVP ready to release"

The MVP is ready when a developer can:

1. `dotnet add package Conduit.Core` (+ `Conduit.DependencyInjection`) into a brand-new ASP.NET Core / Minimal API / console app.
2. Define a `sealed record` request + a handler.
3. Call `services.AddConduit()` and `ISender.Send(...)`.
4. Get compile-time errors (not runtime exceptions) for a missing handler.
5. `dotnet publish -p:PublishAot=true` successfully with **zero** trim/AOT warnings coming from Conduit.
6. Read a README with a 5-minute quickstart that works by copy-paste.

Anything beyond that (validation, caching, retry, telemetry, analyzers beyond the basics, templates, CLI) is explicitly **post-MVP** and belongs to later phases.

---

## Phase 0 — Repository & Tooling Bootstrap

**Goal**: an empty-but-correctly-shaped solution that builds, packs, and has CI green on day one.

- Create the real `Conduit.sln` and the `src/`, `test/` folder layout already described in `docs/architecture/04-solution-structure.md` — but only for the projects needed by Phase 1 (see below). Do not scaffold commercial or Phase 3+ projects yet; empty placeholder projects rot and confuse contributors.
- Set up `Directory.Build.props`/`Directory.Packages.props` for centralized versioning, `Nullable=enable`, `TreatWarningsAsErrors` for the core library projects.
- Set up GitHub Actions (or Azure Pipelines) CI: build + test on every PR, matrix over at least `net10.0` and (if targeting AOT) a `PublishAot=true` smoke build.
- Add `LICENSE` (MIT or Apache-2.0 — decide before first commit, see the licensing question in the NuGet publishing document), `README.md` skeleton, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`.
- Add `.editorconfig` matching the code style already implied by the architecture docs.

**Exit criteria**: `dotnet build` and `dotnet test` succeed on a clean checkout via CI, with zero projects yet containing real logic.

## Phase 1 — Minimal Viable Core (the only thing that must work perfectly)

**Goal**: `Conduit.Abstractions` + `Conduit.Core` + `Conduit.Pipeline` + `Conduit.SourceGenerators` + `Conduit.DependencyInjection`, hand-verified against every non-negotiable principle (zero reflection, immutable requests, Native AOT).

- Implement `Conduit.Abstractions`: `IRequest<TResponse>`, `IRequestHandler<TRequest,TResponse>`, `IPipelineBehavior<TRequest,TResponse>`, `INotification`, `INotificationHandler<TNotification>`.
- Implement `Conduit.Core`: `ISender`, `IPublisher`, exception hierarchy (`HandlerNotFoundException` is now a **build-time** concept, so the runtime hierarchy is small).
- Implement `Conduit.Pipeline`: the delegate-chain execution primitives consumed by generated code.
- Implement `Conduit.SourceGenerators` (the single highest-risk, highest-value piece): Incremental Generator that discovers handlers/behaviors in the compiling assembly and emits the dispatcher partial class + `AddConduit()`.
- Implement `Conduit.DependencyInjection`: the thin `IServiceCollection` surface the generated code calls into.
- Write the **first real Roslyn analyzer only**: `CONDUIT001` Missing Handler. (Full analyzer catalog is Phase 2+; ship the one that prevents the most common and confusing mistake.)
- Unit tests for the generator itself (`Microsoft.CodeAnalysis.Testing` snapshot tests) — this is non-negotiable; a source generator with no generator tests is unreleasable.
- A `samples/Conduit.Sample.MinimalApi` project used as a living smoke test, published with `PublishAot=true` in CI.

**Exit criteria**: the 6-point "Definition of MVP ready to release" checklist above passes end-to-end using only the projects built in this phase.

## Phase 2 — Just Enough Safety Net to Publish Publicly

**Goal**: the minimum tooling polish that separates "a working prototype" from "a package I'm comfortable telling strangers on the internet to depend on."

- Expand `Conduit.Analyzers` to at least: Missing Handler (already done), Multiple Handlers, Mutable Request, Missing CancellationToken parameter. Four analyzers, not the full ~15-item catalog from the architecture docs — the rest can land post-MVP as minor releases.
- `Conduit.Testing` with `FakeSender` — needed so early adopters can unit-test their handlers without spinning up the full DI container.
- Symbol/source-linked NuGet packages (`.snupkg`), deterministic builds, `PackageReadmeFile`, `PackageLicenseExpression`, repository URL metadata — all required for a professional NuGet.org listing (see the publishing document for the full checklist).
- A real README with: what/why in 3 sentences, install command, quickstart code block, link to the full architecture docs for people who want the deep design rationale.
- Tag `v0.1.0` and publish to NuGet.org (see `02-nuget-org-publishing-process.md`).

**Exit criteria**: `Conduit.Core`, `Conduit.Abstractions`, `Conduit.Pipeline`, `Conduit.SourceGenerators`, `Conduit.DependencyInjection`, `Conduit.Analyzers`, `Conduit.Testing` are live on NuGet.org as `v0.1.0`, installable in a fresh project with no local build steps.

## Phase 3 — First Real-World Feedback Loop (post-MVP, but immediately after)

**Goal**: validate the MVP against real usage before investing in the larger cross-cutting-module surface described in the architecture docs.

- Announce (r/dotnet, Twitter/X, .NET blogs, relevant Discords) and actively solicit issues — do this deliberately, not passively.
- Triage the first 2–4 weeks of GitHub issues; prioritize bug fixes and DX papercuts over new features.
- Only after real feedback exists, begin `Conduit.AspNetCore` / `Conduit.MinimalApis` integration packages and the first OSS cross-cutting module (`Conduit.Validation` is the highest-demand candidate based on ecosystem precedent).
- Everything from here folds back into the phased plan already captured in `docs/architecture/25-roadmap.md` (its Phase 2 and Phase 3) — this document's job was only to get from "nothing" to "a trustworthy v0.1.0 on NuGet.org."

---

## Explicit Non-Goals for the MVP

- No commercial/open-core packages (Phase 4+ of the architecture roadmap) — building these before there is an OSS user base to sell to is wasted effort.
- No `dotnet new` templates or `dotnet conduit` CLI tool yet — nice-to-have DX, not required to prove the core value proposition.
- No `Conduit.Aspire`, `Conduit.Caching`, `Conduit.Retry`, `Conduit.Transactions`, `Conduit.Telemetry`, `Conduit.Authorization` — all valid, all designed in the architecture docs, all deliberately deferred until there's evidence people are using the core pipeline.
- Full analyzer catalog (~15 diagnostics) — ship 4 high-value ones, add the rest incrementally as minor releases; each analyzer is independently shippable and doesn't block the MVP.

## Summary Timeline (indicative, not committed dates)

| Phase | Deliverable | Depends on |
|---|---|---|
| 0 | Repo, CI, solution skeleton | — |
| 1 | Working core pipeline + generator, AOT-verified | Phase 0 |
| 2 | Analyzer safety net + NuGet.org `v0.1.0` publish | Phase 1 |
| 3 | Real-world feedback loop, first post-MVP modules | Phase 2 |
