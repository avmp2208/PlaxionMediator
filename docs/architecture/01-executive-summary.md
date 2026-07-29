# 01 — Executive Summary

## What is Conduit?

Conduit is a Request Pipeline framework for .NET that lets applications express business operations as immutable **requests**, dispatch them through a composable **pipeline of behaviors**, and handle them with strongly-typed **handlers** — with **zero reflection**, **zero runtime assembly scanning**, and full **Native AOT** compatibility. All wiring (handler-to-request mapping, pipeline composition, DI registration) is generated at **compile time** by a Roslyn Incremental Source Generator.

Conduit is not a copy of any existing mediator/pipeline library. It is a ground-up redesign that treats the request pipeline as a **compiler-verified, compile-time-linked graph** rather than a runtime-resolved, reflection-driven one.

## Why does it exist?

The dominant pattern in the .NET ecosystem for building request/response and pipeline-based architectures (CQRS-style mediators, cross-cutting behavior pipelines) relies on:

- Reflection-based assembly scanning to discover handlers at startup.
- Runtime `IServiceProvider.GetService(typeof(IRequestHandler<,>))` resolution via open generics.
- Dynamic dispatch through `dynamic` or reflection-emitted delegates for polymorphic dispatch.
- Attribute or convention-based behavior ordering resolved at runtime.

These choices made sense in 2015-era .NET (no source generators, no Native AOT, DI containers were younger). They are actively hostile to 2026-era .NET:

| Concern | Reflection-based approach | Cost |
|---|---|---|
| Native AOT | Open generic resolution via `MakeGenericType` often fails or requires trimming annotations | Breaks AOT publish or requires `[DynamicallyAccessedMembers]` escape hatches |
| Startup time | Assembly scanning walks every loaded assembly's types at startup | O(assemblies × types) cold-start cost, worse in serverless/short-lived processes |
| Compile-time safety | Missing handler / duplicate handler discovered only at runtime | Bugs ship to production instead of failing the build |
| Performance | `GetService` on open generics + reflection invoke per request | Allocations, delegate cache lookups, virtual dispatch overhead |
| Debuggability | "Magic" wiring — hard to know which handler runs for a request without running the app | Poor IDE navigation (no "Go to Implementation") |

## Philosophy

Conduit treats the pipeline as a **build artifact**, not a runtime discovery process:

1. **Source-generator-first**: every request-handler mapping and every behavior chain is discovered by the Roslyn compiler during compilation, not by scanning assemblies at runtime.
2. **Compile-time verified**: missing handlers, duplicate handlers, and invalid pipeline configurations are Roslyn diagnostics (build errors/warnings), not runtime exceptions.
3. **Immutable requests**: requests are `sealed record` types — thread-safe by construction, safe to log/cache/replay.
4. **No service locator**: handlers and behaviors are registered as ordinary DI services; the generated dispatcher calls them through regular constructor injection, never through an ambient container lookup.
5. **API as product**: the public surface is deliberately small — a request, a handler, a behavior, a dispatcher. Everything else (diagnostics, telemetry, validation, retry, caching) is an optional, composable add-on package.

## Comparison with the current .NET ecosystem

| Dimension | Typical reflection-based mediator | Conduit |
|---|---|---|
| Handler discovery | Runtime assembly scan + reflection | Compile-time source generation |
| DI registration | `services.AddHandlersFromAssembly(...)` scans types | Generated `AddConduit()` registers exact, known types |
| Native AOT | Requires trimming warnings suppression, may break | First-class, zero warnings by design |
| Missing handler | `InvalidOperationException` at runtime | Roslyn compile error (`CONDUIT001`) |
| Pipeline composition | Runtime-built `IEnumerable<IPipelineBehavior<,>>` resolved per call | Compile-time generated delegate chain, monomorphized per request type |
| Startup cost | Proportional to assembly/type count | Near-zero — registrations are static generated calls |
| Extensibility | Inheritance/marker interfaces + DI | Composable packages (behaviors, analyzers, generators) plus an explicit open-core tier for enterprise concerns |

## Strengths and weaknesses of existing approaches

**Strengths worth preserving:** the request/handler separation, pipeline behaviors as cross-cutting concerns, simple mental model of "one request → one handler", strong `Microsoft.Extensions.DependencyInjection` integration.

**Weaknesses Conduit eliminates:** reflection-driven discovery, runtime-only failure modes for misconfiguration, poor Native AOT support, hidden/implicit behavior ordering, monolithic single-package distribution that forces every consumer to take a dependency on features they don't use (validation, telemetry, etc.).

## Why this architecture wins long-term

- **Maintainability**: compiler-enforced contracts mean refactoring (renaming a request, removing a handler) surfaces as a build error, not a production incident.
- **Performance**: monomorphized, generated dispatch code avoids reflection, boxing, and open-generic resolution overhead; behaviors compose into a single delegate chain instead of an `IEnumerable` iterated per call.
- **Observability**: because the pipeline shape is known at compile time, diagnostics (tracing, pipeline visualization) can be generated with full static metadata instead of reconstructed at runtime via reflection.
- **Developer experience**: "Go to Implementation" works, IntelliSense sees real generated types, and Roslyn analyzers catch mistakes as you type — instead of the classic "why isn't my handler being called" runtime debugging session.
- **Business model sustainability**: an explicit open-core split (documented in [Package Architecture](08-package-architecture.md)) lets the core pipeline remain free and AOT-safe forever, while advanced tooling (visualizer, analytics, enterprise policy engine) funds ongoing development — a proven model in the observability/DevTools space.
