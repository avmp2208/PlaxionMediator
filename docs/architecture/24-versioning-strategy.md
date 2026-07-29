# 24 — Versioning Strategy

## Semantic Versioning

All OSS packages follow strict SemVer (`MAJOR.MINOR.PATCH`) and are released together on a single **version train** — `Conduit.Core 2.3.0` always ships alongside `Conduit.Pipeline 2.3.0`, `Conduit.DependencyInjection 2.3.0`, etc., even if a given package's content didn't change in that release, so consumers never have to reason about cross-package compatibility matrices within the OSS tier.

- **MAJOR**: breaking change to any public API in `Conduit.Abstractions`, `Conduit.Core`, or `Conduit.Pipeline` (the "load-bearing" packages every other package and every consumer depends on); or a breaking change to generated code shape that would require consumer recompilation with different generated output semantics.
- **MINOR**: new public API surface (new interface, new optional parameter with a default, new package), or new analyzer/generator diagnostics introduced at `Warning` or `Info` severity (never retroactively promoted to `Error` within a MINOR — see below).
- **PATCH**: bug fixes, performance improvements, generator output optimizations that don't change observable behavior.

## Binary Compatibility

- `Conduit.Abstractions` is held to the strictest compatibility bar: **no breaking changes ever**, if at all avoidable, because every other package, every generated artifact, and every consumer's compiled handler assembly references it directly. New capability is added via new interfaces (e.g., `IStreamRequestHandler<,>` added alongside `IRequestHandler<,>`, never by changing an existing interface's members) — an application of the Interface Segregation/Open-Closed principle at the versioning level.
- Default interface methods (C# 8+) are the sanctioned mechanism for adding an optional new member to an existing interface without breaking implementers, used sparingly and only when the default behavior is safe for all existing implementations.
- Generated code is considered an internal implementation detail, not a public contract — its exact shape may change between MINOR versions (e.g., switching from a `switch` pattern match to a jump table internally) as long as observable behavior (what handler runs, what exceptions propagate) is unchanged.

## API Evolution Rules

| Change | Allowed In | Mechanism |
|---|---|---|
| Add a new interface/type | MINOR | Standard addition. |
| Add a member to an existing interface | MINOR (if default-implemented) / MAJOR (if not) | Prefer default interface methods. |
| Add an optional parameter to an existing method | MINOR | Must have a default value; existing call sites unaffected. |
| Remove/rename a public member | MAJOR | Must go through the Deprecation Strategy below first. |
| Change analyzer diagnostic severity from `Info`/`Warning` to `Error` | MAJOR | Because this can turn a previously-successful build into a failing one — never done silently in a MINOR/PATCH. |
| Change generated code's internal shape (not its observable behavior) | PATCH or MINOR | Generated code is not a public contract. |

## Deprecation Strategy

1. Mark the member `[Obsolete("Use X instead. Will be removed in vNext-MAJOR.", error: false)]` in a MINOR release — this surfaces as an IDE/build warning, not an error.
2. Keep the obsolete member functional for at least one full MAJOR version cycle (never remove something in the very next MAJOR after deprecation without at least one MINOR of warning-only lead time).
3. Remove in the next MAJOR after the deprecation warning has shipped for at least 6 months, documented in the migration guide accompanying that MAJOR release.
4. A companion Roslyn code fix accompanies every `[Obsolete]` marker where a mechanical migration exists (renames, signature changes), consistent with the general "an analyzer diagnostic should come with a code fix wherever mechanically possible" philosophy from [Roslyn Analyzer Architecture](11-roslyn-analyzer-architecture.md).

## Commercial Package Versioning

Commercial packages (`Conduit.Diagnostics.Pro`, `Conduit.Enterprise`, etc.) version **independently** of the OSS train, but each release documents its minimum-compatible `Conduit.Core`/`Conduit.Pipeline` version range (e.g., `Conduit.Visualizer 1.4.0` requires `Conduit.Core >= 2.1.0 < 4.0.0`) — this decoupling lets commercial packages ship fixes/features on their own cadence without forcing an OSS core upgrade, while the version range check (enforced at `AddConduit()` composition time via `IConduitLicense`, see [Package Architecture](08-package-architecture.md)) prevents silent incompatibility.
