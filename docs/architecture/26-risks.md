# 26 — Risks

## Performance Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Source generator itself becomes a build-time bottleneck as handler count grows | Poor developer experience negates the framework's core value proposition | Incremental Generator design with `IEquatable` model caching ([Source Generator Architecture](10-source-generator-architecture.md#performance-considerations)); generator's own benchmark suite tracks build-time regression per handler-count tier in CI. |
| Closure-based pipeline chain allocation becomes a real bottleneck for extreme-throughput consumers | Loses ground to hand-rolled/allocation-free alternatives in high-frequency-trading-style workloads | Documented, designed-but-deferred "AllocationFreeMode" ([Performance](21-performance.md#closures-vs-struct-continuations)) as a Phase 5 opt-in escape hatch, informed by real benchmark data rather than speculative optimization. |
| Behavior chain depth grows unbounded in practice (teams add too many global behaviors) | Increased per-request latency, harder-to-debug pipelines | Analyzer `CONDUIT080` (Unnecessary Behavior on Hot-Path Request) plus documented guidance in [Pipeline Architecture](12-pipeline-architecture.md) on keeping global behaviors minimal. |

## Maintenance Risks

| Risk | Impact | Mitigation |
|---|---|---|
| `Conduit.Abstractions` API surface needs a breaking change discovered post-1.0 | Forces a MAJOR version bump early, damaging perceived stability | Extensive design review (this document set) before 1.0 ships; default interface methods as the primary extension mechanism to minimize the chance of needing a breaking change ([Versioning Strategy](24-versioning-strategy.md)). |
| Analyzer/generator shared model logic (`Conduit.SourceGenerators.Shared`) drifts out of sync between what the IDE flags and what the build enforces | Confusing developer experience ("the IDE didn't warn me, but the build failed") | Shared extraction library used by both generator and analyzers ([Roslyn Analyzer Architecture](11-roslyn-analyzer-architecture.md#analyzer-implementation-notes)); a dedicated test suite asserting parity between analyzer and generator diagnostics for the same inputs. |
| Keeping 20+ OSS packages' documentation and examples consistent as the framework evolves | Documentation rot undermines trust in a project whose core pitch is clarity/no-hidden-magic | Single versioned documentation set released alongside the OSS version train ([Versioning Strategy](24-versioning-strategy.md)); every new package requires a corresponding doc update as part of its Definition of Done. |
| .NET platform evolution (future C#/.NET versions) changes source generator APIs or Native AOT requirements | Generator/analyzer breakage on new SDKs, blocking adoption of new .NET versions | Generator targets the stable `netstandard2.0` Roslyn compatibility floor ([Source Generator Architecture](10-source-generator-architecture.md#performance-considerations)); CI matrix tests against preview .NET SDKs ahead of GA releases. |

## Adoption Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Developers accustomed to reflection-based mediators find the source-generator model unfamiliar/intimidating | Slower adoption curve than a drop-in-compatible alternative would have | `Conduit.Templates` provides a working, idiomatic starting point; documentation emphasizes "what happens when I call Send" transparency via generated code inspection, directly addressing the unfamiliarity. |
| Perceived "yet another mediator library" fatigue in the .NET community | Low initial interest despite genuine architectural differentiation | Executive Summary and marketing materials lead with concrete, measurable differentiators (Native AOT, compile-time validation, benchmarks) rather than abstract philosophy claims. |
| Open-core monetization perceived as "OSS bait-and-switch" if commercial tier feels like withheld functionality | Community backlash, distrust, forks | ADR-0004 commits explicitly to a fully production-viable OSS core; package/feature placement decisions are documented transparently in [Package Architecture](08-package-architecture.md) rather than decided ad hoc post-launch. |
| Ecosystem consolidation — a well-funded competitor (or Microsoft itself) ships an official source-generator-based pipeline framework | Conduit's differentiation disappears | Roadmap prioritizes shipping a working, benchmarked OSS core early (Phase 1-2) to establish adoption and community trust before a competing solution can capture the same first-mover advantage. |

## Cross-Cutting Mitigation Principle

Every risk mitigation above traces back to a documented architectural decision (ADR or design document) rather than an ad hoc reaction — consistent with the project's overall commitment to first-principles, justified decision-making rather than reactive patching.
