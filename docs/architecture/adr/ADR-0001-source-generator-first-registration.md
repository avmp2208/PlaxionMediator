# ADR-0001: Source-Generator-First Handler Registration

## Status

Accepted

## Context

Every request must be routed to exactly one handler. The dominant pattern in the .NET ecosystem discovers this mapping at runtime via reflection-based assembly scanning (`Assembly.GetTypes().Where(t => t.GetInterfaces().Any(...))`). This has well-documented costs: Native AOT incompatibility (or trimming warnings), non-trivial cold-start cost proportional to assembly/type count, and misconfiguration (missing/duplicate handlers) surfacing only as runtime exceptions.

## Decision

Handler and behavior discovery, and the resulting DI registration + dispatcher wiring, are performed entirely by a Roslyn Incremental Source Generator at compile time. No `Assembly.GetTypes()`, `Type.GetInterfaces()`, `MakeGenericType`, or `Activator.CreateInstance` appears anywhere in Conduit's runtime code path.

## Alternatives Considered

1. **Runtime reflection-based scanning** (the ecosystem status quo) — rejected: fails the non-negotiable zero-reflection/Native-AOT requirements outright.
2. **Manual registration** (developer hand-writes every `services.AddScoped<IRequestHandler<X,Y>, XHandler>()` call) — rejected as the *sole* mechanism: correct and reflection-free, but tedious and error-prone at scale (easy to forget a registration), and provides no compile-time cross-checking of "does every request have exactly one handler."
3. **Source-generator-first (chosen)** — combines the safety/performance of manual registration with the ergonomics of automatic discovery, plus compile-time cross-checking (`CONDUIT001`/`CONDUIT002`) that neither alternative provides.

## Tradeoffs

- Requires consumers to reference an analyzer/generator package (`Conduit.SourceGenerators`) as a build-time dependency — a new concept for developers unfamiliar with source generators, though this is increasingly standard in modern .NET (EF Core compiled models, `System.Text.Json` source generation, etc.).
- Generator correctness/performance is now on Conduit's critical path for developer experience — a slow or buggy generator directly harms every consumer's build times.
- Generated code must be inspectable/debuggable (`obj/generated`) to preserve the "no hidden magic" principle, requiring deliberate file/naming conventions ([Source Generator Architecture](../10-source-generator-architecture.md)).

## Consequences

- Native AOT compatibility is structural, not incidental.
- Startup cost for handler registration is O(handler count), not O(assembly/type count).
- Missing/duplicate handler mistakes are compile errors, eliminating an entire class of production incidents.
- All downstream architectural decisions (dispatcher internals, DI registration, pipeline composition) are built assuming this compile-time-first foundation.
