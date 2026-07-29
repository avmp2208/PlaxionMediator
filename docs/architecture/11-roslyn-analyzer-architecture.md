# 11 — Roslyn Analyzer Architecture

## Purpose

Analyzers catch usage mistakes **as the developer types**, in the IDE, before a build even runs — complementing the generator's build-time checks ([Source Generator Architecture](10-source-generator-architecture.md)). Every analyzer ships in `Conduit.Analyzers` with a matching code fix where mechanically possible.

## Diagnostic ID Range

`CONDUIT001`–`CONDUIT099`: reserved for generator + analyzer diagnostics, split as:
- `CONDUIT001`–`CONDUIT029`: structural/registration diagnostics (shared with the generator).
- `CONDUIT030`–`CONDUIT059`: request/handler shape diagnostics.
- `CONDUIT060`–`CONDUIT079`: pipeline/behavior diagnostics.
- `CONDUIT080`–`CONDUIT099`: performance/anti-pattern diagnostics.

## Analyzer Catalog

| ID | Name | Severity | Description | Code Fix |
|---|---|---|---|---|
| `CONDUIT001` | Missing Handler | Error | An `IRequest<T>` has no corresponding `IRequestHandler<,>` in the compilation. | Generate handler stub. |
| `CONDUIT002` | Multiple Handlers | Error | An `IRequest<T>` has more than one `IRequestHandler<,>` implementation. | Navigate to conflicting handlers; no auto-fix (requires human decision). |
| `CONDUIT003` | Orphaned Behavior Registration | Warning | A behavior is registered via `ConduitOptions` for a request type not present in the compilation. | Remove registration line. |
| `CONDUIT004` | Response Type Mismatch | Error | Handler's `Handle` return type disagrees with `IRequest<TResponse>`. | Adjust method signature. |
| `CONDUIT010` | Mutable Request | Error | A type implementing `IRequest<T>` is not declared `sealed record` / `readonly record struct`. | Convert to `sealed record`. |
| `CONDUIT011` | Non-Sealed Handler | Warning | A handler class is not `sealed`, allowing accidental subclassing that bypasses DI-registered behavior. | Add `sealed`. |
| `CONDUIT020` | Invalid Behavior Registration | Error | `PipelineBuilder.Use<T>()` called with a type that doesn't implement `IPipelineBehavior<,>`. | Remove/replace call. |
| `CONDUIT021` | Duplicate Registration | Warning | The same behavior type registered twice for the same request. | Remove duplicate. |
| `CONDUIT022` | Incorrect Lifetime | Warning | A `Singleton`-lifetime behavior/handler captures a `Scoped` or `Transient` dependency via constructor injection. | Change lifetime or dependency. |
| `CONDUIT030` | Blocking Call in Handler | Warning | `.Result`, `.Wait()`, or `Thread.Sleep` detected inside a handler/behavior `Handle` method. | Suggest `await`/`Task.Delay`. |
| `CONDUIT031` | Missing CancellationToken Propagation | Warning | A handler/behavior receives a `CancellationToken` parameter but doesn't pass it to an awaited async call that accepts one. | Insert token argument. |
| `CONDUIT032` | CancellationToken.None Usage | Info | `CancellationToken.None` used inside a handler where the ambient token is available. | Replace with ambient token. |
| `CONDUIT040` | Async Void Handler | Error | A handler or behavior method is declared `async void`. | Change to `ValueTask`/`Task`. |
| `CONDUIT041` | Handler Depends on ISender for Self-Type | Warning | A handler injects `ISender` and sends a request of its own type (risk of infinite recursion). | Highlight recursive call site. |
| `CONDUIT080` | Unnecessary Behavior on Hot-Path Request | Info | A request marked `[HighFrequency]` has more than N behaviors in its chain (configurable threshold). | Suggest narrowing behavior scope with `UseWhen`. |
| `CONDUIT081` | Synchronous-Only Handler Could Return `ValueTask.FromResult` | Info | Handler body has no `await` — suggests using a synchronous-completion-optimized pattern. | No-op suggestion; educational diagnostic. |
| `CONDUIT082` | Behavior Allocates in Hot Path | Info | A behavior allocates a new closure/collection per call detectable via a simple data-flow heuristic. | Suggest caching/pooling. |
| `CONDUIT090` | Notification Handler Throws Without Awaiting Others | Warning | A notification handler's exception handling pattern suggests fail-fast assumptions incompatible with fan-out semantics. | Educational diagnostic. |

## Example Code Fix: `CONDUIT010` (Mutable Request)

```csharp
// Before
public class CreateOrderCommand : IRequest<OrderId>
{
    public Guid CustomerId { get; set; }
}

// After (applied by code fix)
public sealed record CreateOrderCommand(Guid CustomerId) : IRequest<OrderId>;
```

## Example Code Fix: `CONDUIT001` (Missing Handler)

The code fix offers "Generate handler stub," which creates a new file `<RequestName>Handler.cs` next to the request:

```csharp
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderId>
{
    public ValueTask<OrderId> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

## Analyzer Implementation Notes

- All analyzers run as `DiagnosticAnalyzer` registered via `RegisterSyntaxNodeAction`/`RegisterSymbolAction`, never `RegisterCompilationAction` alone for anything IDE-latency-sensitive, to keep typing-time analysis fast.
- Analyzers share the same `HandlerModel`/`BehaviorModel` extraction logic as the generator (extracted into a common internal `Conduit.SourceGenerators.Shared` netstandard2.0 library) to avoid divergent behavior between "what the IDE flags" and "what the build enforces."
- Every analyzer has a corresponding entry in `AnalyzerReleases.Shipped.md`/`Unshipped.md` per the standard .NET analyzer release-tracking convention, keeping severity changes auditable across versions.

## Suggested Future Analyzers (Roadmap Candidates)

- `CONDUIT091` — Detects a request handled synchronously that's also marked `[Cacheable]` with a very short TTL (likely misconfiguration).
- `CONDUIT092` — Flags a transaction-marked request (`ITransactionalRequest`) whose handler calls `ISender.Send` for another transactional request without an explicit nested-transaction opt-in (see [Transactions](20-transactions.md)).
- `CONDUIT093` — Flags authorization-less mutation requests (`IRequest` whose name matches command-like patterns) with no `IConduitAuthorizationHandler<>` registered, when `Conduit.Authorization` is referenced.
