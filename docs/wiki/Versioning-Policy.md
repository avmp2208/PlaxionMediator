# Versioning Policy

PlaxionMediator follows [Semantic Versioning 2.0.0](https://semver.org/) (`MAJOR.MINOR.PATCH`), as ratified by `ADR-0013` (the `1.x` Compatibility Constitution). This page explains, in consumer-facing terms, exactly what "breaking" means for this framework — because for a mediator/pipeline library, a breaking change is not only a changed method signature.

## The ten compatibility dimensions

Starting with `v1.0.0`, PlaxionMediator's stable contract is defined across ten independent dimensions. Each has its own guarantee boundary:

| # | Dimension | What's guaranteed across `1.x` |
|---|---|---|
| 1 | Source/API compatibility | `STABLE` public/protected members compile unchanged. |
| 2 | Binary compatibility | A consumer built against `1.0.x` runs unmodified against later `1.x` assemblies (`STABLE` surface only). |
| 3 | Behavioral compatibility | Documented dispatch/pipeline/exception/cancellation behavior is preserved. |
| 4 | Package/dependency compatibility | Dependency boundaries (e.g. `Transactions` never requires EF Core) are not silently widened. |
| 5 | Pipeline semantic compatibility | Documented ordering/short-circuit/propagation rules are preserved. |
| 6 | Source-generator compatibility | Supported request/handler shapes and registration semantics keep working. |
| 7 | Analyzer/diagnostic compatibility | Diagnostic IDs keep their meaning; IDs are never repurposed. |
| 8 | NativeAOT/trimming compatibility | Certified runtime packages keep publishing/running without new PlaxionMediator-attributable warnings. |
| 9 | Supported platform/TFM compatibility | Supported TFMs are honored for the stated window. |
| 10 | Performance compatibility | No material, unexplained regression on critical-path benchmarks. |

`EXPERIMENTAL`-marked APIs (see below) and internal-but-public-for-tooling surfaces (e.g. source-generator-emitted partial classes, analyzer diagnostic descriptor internals) are excluded from all ten guarantees.

## What triggers a MAJOR version

- Removing, renaming, or changing the signature of a `STABLE` public/protected member.
- Any change that breaks binary compatibility for `STABLE` surface (e.g. `MissingMethodException`/`TypeLoadException` scenarios).
- Changing documented pipeline ordering, short-circuit, or exception-propagation semantics.
- Narrowing a package dependency boundary in a way that requires consumer code changes, or widening it in a way that pulls a new mandatory dependency into a previously lightweight package (e.g. `PlaxionMediator.Core` gaining a hard dependency).
- Raising an existing analyzer diagnostic's default severity to `Error` (this can break consumers using `TreatWarningsAsErrors`).
- Removing support for a previously-supported target framework.
- Retiring a package entirely.

## What triggers a MINOR version

- Adding new `STABLE` public API additively (new types, new optional-parameter overloads, new members on non-`sealed` classes).
- Adding a new opt-in behavior or capability to an existing package.
- Adding a new analyzer diagnostic at `Warning` severity or below.
- Adding support for a new target framework.
- Raising a minimum dependency version in a way that does not require consumer code changes.
- Any change to an `EXPERIMENTAL` API (documented in release notes, not subject to the `STABLE` guarantees).

## What triggers a PATCH version

- Bug fixes that do not change any documented contract.
- Performance improvements with no behavior change.
- Security fixes — even ones that must alter behavior, provided the change is the minimal fix necessary and is explicitly called out in the release notes as a security exception to the normal compatibility guarantee.

## Experimental APIs

Some surfaces are intentionally excluded from the `1.x` freeze while still being useful to try. These are marked with `[System.Diagnostics.CodeAnalysis.Experimental]` (or an equivalent repository attribute where that attribute is unavailable for a supported TFM) and called out in XML doc `<remarks>`. Experimental APIs may change in any release, including patch releases, and are never the *only* path through a default/critical pipeline stage. As of `v1.0.0`, the experimental-API inventory is empty; any future experimental surface will be listed here.

## Diagnostic (analyzer) compatibility

Every diagnostic ID shipped by `PlaxionMediator.Analyzers` (currently in the `PlaxionMediator001`–`PlaxionMediator049` range — see [`Analyzers-Reference.md`](Analyzers-Reference.md)) keeps its meaning and category for the life of the `1.x` line. A retired diagnostic ID is never reused for something else. Severity changes follow the MAJOR/MINOR rules above.

## Source-generator and analyzer implementation details

`PlaxionMediator.SourceGenerators` and `PlaxionMediator.Analyzers` are Roslyn components. Some of their types are public only because the Roslyn SDK requires it (e.g. generator/analyzer classes, diagnostic descriptors). These are classified `INTERNAL-BUT-PUBLIC-FOR-TOOLING` and are **not** part of the `1.x` contract — only the *observable* generation/diagnostic behavior is (see dimensions 6 and 7 above).

## 0.x support policy

Versions prior to `1.0.0` were pre-stable. Once `v1.0.0` ships, `0.x` releases receive **no formal maintenance commitment** — there is no dedicated security-backport branch. Consumers on `0.x` should migrate using [`Migration-Guide.md`](Migration-Guide.md).

## Deprecation policy

When an API needs to change: (1) an additive replacement ships first, (2) the old member is marked `[Obsolete]` with a migration message, (3) the migration is documented, (4) the old member is removed only at the next MAJOR. `Obsolete(error: true)` is reserved for security or correctness exceptions and is documented when used.

See `ADR-0013` in the engineering documentation repository for the full rationale behind these rules.
