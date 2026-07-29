# 05 — Core Architecture

This document designs every core concept in Conduit and justifies each from first principles.

## Request

```csharp
public interface IRequest<out TResponse>;
public interface IRequest : IRequest<Unit>;
```

**Design rationale**: A request is a marker interface, not a base class — this preserves the ability to make requests `sealed record` types (composition over inheritance) while still giving the compiler a type to constrain generic parameters on. `TResponse` is covariant (`out`) so a handler returning a more derived type can satisfy a less derived contract without extra casting.

Requests **must** be declared as `sealed record` (enforced by analyzer `CONDUIT010`, see [Roslyn Analyzer Architecture](11-roslyn-analyzer-architecture.md)):

```csharp
public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<OrderLine> Lines) : IRequest<OrderId>;
```

Immutability is non-negotiable: requests may be logged, cached, replayed (by `Conduit.Diagnostics.Pro`), or passed across threads (e.g., streaming), and a mutable request would make every one of those unsafe.

## Response

There is no `IResponse` marker — responses are plain types (including `Unit` for "no meaningful response", modeled as a `readonly struct` singleton, avoiding the allocation that `void`-via-`Task` boxing would otherwise require in a generic pipeline).

## Dispatcher

```csharp
public interface ISender
{
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public interface IPublisher
{
    ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

**Design rationale**: `ISender` and `IPublisher` are split (not one `IMediator` doing both) because request/response dispatch and notification fan-out have fundamentally different failure semantics — one handler with one result vs. N handlers with independent success/failure. Splitting the interfaces keeps each contract explainable in one sentence and lets a consumer depend on only the capability they need (Interface Segregation Principle).

`Send` accepts `IRequest<TResponse>` (not a generic `TRequest : IRequest<TResponse>`) so callers can pass a request through a variable of its declared interface type without extra generic-argument inference — while the *generated* implementation dispatches internally using the concrete compile-time-known type, so this incurs zero runtime type-checking cost (see [Internal Architecture](07-internal-architecture.md)).

## Pipeline

A pipeline is the ordered composition of behaviors terminating in a handler invocation, represented as a single delegate chain per request type, generated at compile time:

```csharp
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
```

**Design rationale**: this is the same "middleware as a chain of responsibility" shape ASP.NET Core popularized (`RequestDelegate`), reused deliberately because it is a well-understood, battle-tested composition pattern — not because of any existing pipeline library. Unlike a runtime-resolved `IEnumerable<IPipelineBehavior<,>>`, the generator emits one method per request type that calls each registered behavior in a fixed, known order, so there is no iterator allocation or virtual dispatch through an enumerator per call.

## Behaviors

Behaviors are ordinary DI services implementing `IPipelineBehavior<TRequest, TResponse>`, either **closed generic** (behavior applies to one specific request) or **open generic** (`IPipelineBehavior<TRequest, TResponse>` registered as `typeof(LoggingBehavior<,>)`, applying to every request). Ordering is explicit and declared at registration time (see [Pipeline Architecture](12-pipeline-architecture.md)) — never inferred from attributes or reflection scanning.

## Handlers

```csharp
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

**Design rationale**: exactly one handler per request type is a compile-time-enforced invariant (`CONDUIT001`/`CONDUIT002` diagnostics for missing/duplicate handlers). This single-responsibility mapping keeps "what runs when I send this request" a pure function of the type system.

## Notifications

```csharp
public interface INotification;

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
}
```

Unlike requests, notifications support **zero-to-many** handlers, dispatched independently. `Publish` awaits all handlers via `Task.WhenAll`-equivalent `ValueTask` aggregation; a single handler's exception does not prevent others from running (aggregated into an `AggregateException` surfaced after all handlers complete), because notification handlers represent independent side effects, not a single computation.

## Streams

```csharp
public interface IStreamRequest<out TResponse> : IRequest<IAsyncEnumerable<TResponse>>;

public interface IStreamRequestHandler<in TRequest, TResponse> where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

Streaming requests model server-streaming scenarios (large query result sets) using `IAsyncEnumerable<T>` — no custom streaming abstraction is invented, because `IAsyncEnumerable<T>` with `[EnumeratorCancellation]` is already the correct, idiomatic .NET primitive for this.

## Cancellation

Every dispatch entry point takes a `CancellationToken`. Handlers and behaviors are analyzer-enforced (`CONDUIT006`) to accept and propagate it — never to ignore it or create a fresh `CancellationToken.None`. There is no ambient/ambient-flowed cancellation; it is always explicit, matching .NET's own idiom.

## Exceptions

```csharp
public abstract class ConduitException : Exception;
public sealed class HandlerNotFoundException : ConduitException;   // should be unreachable — generator guarantees a handler exists
public sealed class PipelineExecutionException : ConduitException; // wraps behavior/handler faults with pipeline context
```

Because handler existence is compile-time guaranteed, `HandlerNotFoundException` is a defensive/theoretical type (e.g., for a request type instantiated dynamically via `IRequest<TResponse>` boxing edge cases) rather than a common runtime occurrence — a structural improvement over reflection-based frameworks where this is a routine misconfiguration exception.

## Registration

Registration is described fully in [Dependency Injection](09-dependency-injection.md) — in short, the generator emits `AddConduit()` which registers every discovered handler/behavior with an explicit `ServiceLifetime` (default `Scoped`, overridable per type).

## Execution

Execution is a single virtual call from `ISender.Send` into a generated per-request-type method that walks the compile-time-known behavior chain and terminates in the handler — no runtime lookup table, no reflection `Invoke`. Full mechanics in [Internal Architecture](07-internal-architecture.md).

## Lifetime

Handlers and behaviors are DI-managed like any other service. Default lifetime is `Scoped` (matches the typical unit-of-work — one request, one `DbContext`), but `Transient` and `Singleton` are supported and validated by analyzer `CONDUIT009` (e.g., flags a `Singleton` behavior that captures a `Scoped` dependency).

## Extensibility

Every extension point (behaviors, notification handlers, custom dispatch strategies) is a plain interface implemented as a DI service — there is no plugin-loading mechanism, no `IPlugin` marker scanned at runtime. Third parties extend Conduit by shipping a NuGet package with behaviors and an `IServiceCollection` extension method, exactly like first-party modules (see [Extensibility](23-extensibility.md)).
