# 06 — Public API

Every public type Conduit ships, with a one-sentence justification for each. Snippets are illustrative design artifacts, not implementation.

## `Conduit.Abstractions`

```csharp
public interface IRequest<out TResponse>;                     // Marks a type as dispatchable, carrying its response type.
public interface IRequest : IRequest<Unit>;                    // Convenience for requests with no meaningful response.
public readonly struct Unit { public static readonly Unit Value; } // Allocation-free "no response" type, replacing generic-hostile `void`.
public interface IStreamRequest<out TResponse> : IRequest<IAsyncEnumerable<TResponse>>; // Marks a request as server-streaming.
public interface INotification;                                // Marks a type as a zero-to-many fan-out event.
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>; // Contract every request must have exactly one implementation of.
public interface IStreamRequestHandler<in TRequest, TResponse> where TRequest : IStreamRequest<TResponse>; // Streaming counterpart of IRequestHandler.
public interface INotificationHandler<in TNotification> where TNotification : INotification; // One of possibly many independent reactions to a notification.
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>; // A composable cross-cutting step wrapping handler execution.
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>(); // The "next" continuation a behavior invokes to proceed down the pipeline.
```

## `Conduit.Core`

```csharp
public interface ISender  // The single entry point application code uses to dispatch a request and await its response.
{
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
}

public interface IPublisher // The single entry point for fan-out notification dispatch.
{
    ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

public abstract class ConduitException : Exception;          // Common base so consumers can catch "any Conduit-originated failure" in one clause.
public sealed class PipelineExecutionException : ConduitException; // Wraps a behavior/handler fault, preserving which pipeline stage failed.
public sealed class HandlerNotFoundException : ConduitException;   // Defensive exception for the theoretically-unreachable missing-handler case.
```

## `Conduit.Pipeline`

```csharp
public interface IPipelineBehaviorOrder  // Optional contract a behavior implements to declare its own default ordering weight.
{
    int Order { get; }
}

public sealed class PipelineBuilder  // Fluent builder the source generator's emitted code uses to declare the fixed behavior order per request.
{
    public PipelineBuilder Use<TBehavior>() where TBehavior : notnull;
    public PipelineBuilder UseWhen<TBehavior>(Func<Type, bool> predicate) where TBehavior : notnull;
}
```

## `Conduit.DependencyInjection`

```csharp
public static class ConduitServiceCollectionExtensions
{
    // Generated per consuming assembly; registers every discovered handler/behavior with zero reflection.
    public static IServiceCollection AddConduit(this IServiceCollection services, Action<ConduitOptions>? configure = null);
}

public sealed class ConduitOptions // Central options object controlling default lifetimes and global behavior ordering.
{
    public ServiceLifetime DefaultHandlerLifetime { get; set; } = ServiceLifetime.Scoped;
    public ServiceLifetime DefaultBehaviorLifetime { get; set; } = ServiceLifetime.Scoped;
    public IList<Type> GlobalBehaviors { get; } // Explicit, ordered list of behaviors applied to every request.
}
```

## `Conduit.AspNetCore`

```csharp
public static class ConduitApplicationBuilderExtensions
{
    public static IApplicationBuilder UseConduitExceptionHandling(this IApplicationBuilder app); // Maps ConduitException subtypes to ProblemDetails responses.
}
```

## `Conduit.MinimalApis`

```csharp
public static class ConduitEndpointRouteBuilderExtensions
{
    // Maps an HTTP verb+route directly to Send<TRequest,TResponse>, removing hand-written endpoint glue.
    public static RouteHandlerBuilder MapConduitPost<TRequest, TResponse>(this IEndpointRouteBuilder endpoints, string pattern)
        where TRequest : IRequest<TResponse>;
}
```

## `Conduit.Validation`

```csharp
public interface IConduitValidator<in TRequest> // Abstraction any validation library (FluentValidation, DataAnnotations) can adapt to.
{
    ValueTask<ValidationResult> Validate(TRequest request, CancellationToken cancellationToken);
}

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>; // Generic behavior that runs all registered validators for TRequest before the handler.
```

## `Conduit.Authorization`

```csharp
public interface IConduitAuthorizationHandler<in TRequest> // Evaluates whether the current principal may execute TRequest.
{
    ValueTask<AuthorizationResult> Authorize(TRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);
}
```

## `Conduit.Caching`

```csharp
public interface ICacheKeyProvider<in TRequest> // Produces a deterministic cache key for a cacheable request.
{
    string GetCacheKey(TRequest request);
}

public interface ICacheableRequest { TimeSpan? Expiration { get; } } // Opt-in marker + policy for cacheable requests.
```

## `Conduit.Retry`

```csharp
public interface IRetryPolicyProvider<in TRequest> // Supplies the retry policy (attempts, backoff) for a specific request type.
{
    RetryPolicy GetPolicy(TRequest request);
}
```

## `Conduit.Transactions`

```csharp
public interface ITransactionalRequest { }             // Marker: this request's handler execution must run inside a transaction.
public interface IConduitTransactionScope : IAsyncDisposable // Abstraction over the ambient transaction (EF Core or ADO.NET-backed).
{
    ValueTask CommitAsync(CancellationToken cancellationToken);
}
```

## `Conduit.Testing`

```csharp
public sealed class FakeSender : ISender // In-memory ISender that records sent requests and returns pre-programmed responses, for unit tests.
{
    public IReadOnlyList<object> SentRequests { get; }
    public void When<TRequest, TResponse>(Func<TRequest, TResponse> respond) where TRequest : IRequest<TResponse>;
}
```

## API Design Rules Applied Throughout

- Every interface has a single method or a tightly related pair (Interface Segregation).
- No type accepts `object` or `dynamic` anywhere in the public surface.
- No public API exposes `Assembly`, `Type[]`, or any reflection-oriented parameter — registration inputs are always generic type parameters resolved at compile time.
- Every builder (`PipelineBuilder`, `ConduitOptions`) is additive and fails to compile (not fails at runtime) when misused, wherever the type system allows it.
