# 23 — Extensibility

## Extension Points

| Extension Point | Mechanism | Example |
|---|---|---|
| New pipeline behavior | Implement `IPipelineBehavior<TRequest,TResponse>`, register via `PipelineBuilder`/`GlobalBehaviors` | A third-party `Conduit.RateLimit` package adding rate-limiting per request type. |
| New validation source | Implement `IConduitValidator<TRequest>` | A `Conduit.Validation.DataAnnotations` community adapter. |
| New authorization source | Implement `IConduitAuthorizationHandler<TRequest>` | A custom OAuth-scope-based authorization handler. |
| New cache backend | Implement key provider + use `HybridCache`'s own provider model | A community Redis-cluster-aware key partitioning strategy. |
| New transaction backend | Implement `IConduitTransactionFactory` | A Dapper/ADO.NET-based transaction factory for non-EF-Core stacks. |
| New notification transport | Implement `IPublisher` decorator forwarding to a message broker | `Conduit.Azure`'s Service Bus-backed publisher (commercial), or a community Kafka publisher. |
| New generator-consumed metadata | Implement an `[IncrementalGeneratorProvider]`-style extension **only** by shipping a fully independent generator (Conduit's own generator is not itself plugin-extensible — see rationale below) | A hypothetical `Conduit.GraphQL.SourceGenerators` package generating GraphQL resolvers from the same handler metadata pattern. |
| New analyzer rule | Ship a standalone `DiagnosticAnalyzer` package referencing `Conduit.Abstractions` | A community "require XML docs on all requests" analyzer. |

## How Third Parties Extend Conduit

There is **no plugin-loading mechanism, no `IPlugin` interface, no runtime assembly probing** — every extension is delivered as an ordinary NuGet package containing:

1. One or more types implementing an existing Conduit extension-point interface.
2. An `IServiceCollection` extension method (`AddXyzBehavior()`) that registers them, following the same naming convention as first-party modules.

```csharp
// A third-party package's public surface looks exactly like a first-party one:
public static class RateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddConduitRateLimiting(this IServiceCollection services, Action<RateLimitOptions>? configure = null);
}
```

This is a deliberate constraint: because handler/behavior discovery is compile-time and type-based (not assembly-scanning-based), a "plugin" is architecturally indistinguishable from a first-party module — there is no separate extensibility API surface to maintain, document, or version. This also means third-party behaviors get the exact same Native AOT and zero-reflection guarantees as first-party ones, for free.

## Why the Generator Itself Is Not Plugin-Extensible

Allowing third parties to inject custom logic into `Conduit.SourceGenerators`' own incremental pipeline (e.g., via a MEF-style `[Export]` composition) would reintroduce a runtime/build-time discovery mechanism exactly analogous to the reflection-based assembly scanning Conduit rejects — instead, a third party wanting generator-level capability (like the hypothetical GraphQL example above) ships its **own** independent Incremental Generator that consumes the same public `IRequestHandler<,>`/`IRequest<,>` shapes Conduit's own generator consumes. Both generators run side-by-side in the same compilation, each doing compile-time analysis of the same source — no coordination protocol between them is needed because they operate on the same, stable, public contract surface.

## Packages, Modules, Analyzers, Generators — Summary

- **Packages**: any NuGet package following the `AddConduitXyz()` convention; no registration with Conduit itself required.
- **Modules**: a package that groups a cohesive concern (behaviors + options + a `Add...()` extension) — the pattern every first-party module (`Conduit.Caching`, `Conduit.Retry`, etc.) follows and that third parties are encouraged to mirror.
- **Behaviors**: the primary and most common extension point — see [Pipeline Architecture](12-pipeline-architecture.md).
- **Generators**: independent, side-by-side `IIncrementalGenerator`s consuming Conduit's public abstractions — not a plugin system inside Conduit's own generator.
- **Analyzers**: independent `DiagnosticAnalyzer` packages; Conduit reserves the `CONDUIT0xx`–`CONDUIT2xx` diagnostic ID prefix range for first-party use and documents (in [Versioning Strategy](24-versioning-strategy.md)) that third parties should choose a distinct prefix to avoid collisions.
