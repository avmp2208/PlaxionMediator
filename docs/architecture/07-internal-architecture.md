# 07 — Internal Architecture

This document describes what happens **inside** Conduit between an `ISender.Send` call and the value returned to the caller — all of it non-public, all of it generated or hand-written for performance, none of it reflection-based.

## Dispatcher Internals

`ISender` is implemented by a single generated `internal sealed partial class ConduitSender : ISender`, emitted into the consuming assembly by the source generator. Its `Send<TResponse>` method does **not** perform a runtime type switch over `request.GetType()`. Instead:

1. The generator knows, at compile time, every `IRequest<TResponse>` type used anywhere in the assembly (via syntax + symbol analysis of the whole compilation).
2. For each concrete request type `TRequest`, the generator emits a dedicated, monomorphic method `SendCreateOrderCommand(CreateOrderCommand request, CancellationToken ct)`.
3. `ISender.Send<TResponse>(IRequest<TResponse> request, ...)` is implemented as a small, generated pattern match (`request switch { CreateOrderCommand r => ... }`) **only** at the boundary where the caller genuinely holds the request as its base interface type (e.g., a generic pipeline behavior forwarding a request); when the caller has the concrete type in hand (the overwhelmingly common case, since callers construct `new CreateOrderCommand(...)`), an additional generated overload `Send<TRequest, TResponse>(TRequest request, ...)` avoids the pattern match entirely and calls the monomorphic method directly.

This design means the "worst case" dispatch cost (pattern match over a closed, compiler-known set of types) is a jump table, not a reflection lookup — and the common case has zero dispatch overhead at all.

## Handler Resolution

There is no `IServiceProvider.GetService(typeof(IRequestHandler<,>))` anywhere. Each generated `SendXxx` method has the handler's DI-resolved instance injected as a constructor parameter of `ConduitSender` itself (one constructor parameter per handler type known at compile time) — resolution happens exactly once per `ConduitSender` instantiation (scoped to the request/DI scope), not per `Send` call.

## Caching Strategy

Because there is no reflection, there is no `ConcurrentDictionary<Type, Delegate>` cache to build or invalidate — the "cache" is simply the compiled generated code itself, JIT-compiled (or AOT-compiled) once. This eliminates an entire category of runtime state and thread-safety concerns that reflection-based dispatchers must manage.

## Execution Strategy

The pipeline for a given request type is a single generated method body that inlines the fixed behavior chain:

```csharp
// Illustrative generated code — not authored by hand
internal ValueTask<OrderId> SendCreateOrderCommand(CreateOrderCommand request, CancellationToken ct)
{
    RequestHandlerDelegate<OrderId> handlerCall = () => _createOrderHandler.Handle(request, ct);
    RequestHandlerDelegate<OrderId> withCaching  = () => _cachingBehavior.Handle(request, handlerCall, ct);
    RequestHandlerDelegate<OrderId> withValidation = () => _validationBehavior.Handle(request, withCaching, ct);
    RequestHandlerDelegate<OrderId> withLogging  = () => _loggingBehavior.Handle(request, withValidation, ct);
    return withLogging();
}
```

Because the chain is generated per request type, the JIT can devirtualize and potentially inline short behaviors, something impossible when the chain is built at runtime from an `IEnumerable<IPipelineBehavior<,>>`.

## Lifetime Management

`ConduitSender` is registered `Scoped` (it captures scoped handler/behavior dependencies through constructor injection). `AddConduit()` registers each handler/behavior with the lifetime declared via `ConduitOptions` or a `[Lifetime(ServiceLifetime.Singleton)]` attribute read by the generator at compile time (not reflected at runtime) — attribute presence is inspected by the generator's Roslyn symbol analysis during compilation, never via `GetCustomAttributes()` at runtime.

## Memory Usage

- `ValueTask<T>` everywhere on hot paths to avoid `Task<T>` heap allocation for synchronously-completing handlers (common for cache hits, simple validation).
- The `RequestHandlerDelegate<TResponse>` closures shown above allocate one small closure object per behavior per call; for the highest-throughput scenarios, [Performance](21-performance.md) documents an alternative struct-based continuation design considered and why the closure approach is the pragmatic default (readability vs. a ~24-byte-per-call allocation).
- Notification fan-out uses `ArrayPool<Task>` internally rather than `List<Task>` to avoid a growable-list allocation for the common small-N handler count.

## Concurrency & Synchronization

`ConduitSender` and generated handler-invocation code hold no mutable shared state — every field is either a constructor-injected dependency (immutable reference) or a local variable. This makes the generated dispatch code trivially thread-safe: a `Scoped` `ConduitSender` is used by one logical operation at a time by DI convention, and even if shared, it has nothing to race on.

## Cancellation Propagation

The generator enforces (backed by analyzer `CONDUIT006`) that every generated `SendXxx` method threads the incoming `CancellationToken` into every behavior and the handler unmodified — there is no internal `CancellationTokenSource` creation or linking unless a behavior explicitly opts in (e.g., a timeout behavior wrapping the token in a linked source).

## Exception Propagation

Behavior and handler exceptions propagate unmodified through the `ValueTask` chain (no wrapping) **except** when a behavior itself throws while invoking `next()` — in that specific case the generated code wraps the fault in `PipelineExecutionException` with a `Behavior` property identifying which stage failed, to make pipeline debugging tractable without a debugger attached. Notification `Publish` aggregates handler exceptions into a single `AggregateException` after all handlers run to completion (fire-and-observe semantics, not fail-fast), matching the "independent side effects" model from [Core Architecture](05-core-architecture.md).
