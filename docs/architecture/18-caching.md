# 18 — Caching

## Marker & Key Strategy

```csharp
public interface ICacheableRequest
{
    TimeSpan? Expiration { get; } // Null = use default policy; framework never assumes a default silently for correctness-sensitive data.
}

public interface ICacheKeyProvider<in TRequest>
{
    string GetCacheKey(TRequest request);
}

public sealed class RecordCacheKeyProvider<TRequest> : ICacheKeyProvider<TRequest> where TRequest : notnull
{
    // Default provider: since requests are sealed records, their auto-generated value-based ToString()/equality
    // gives a stable, deterministic key derived from the record's own field values — no reflection needed,
    // this uses the record's own compiler-generated members.
    public string GetCacheKey(TRequest request) => $"{typeof(TRequest).FullName}:{request}";
}
```

**Rationale**: caching is opt-in per request via `ICacheableRequest` (never automatic/inferred) because caching is a correctness-sensitive decision — automatically caching a request the developer didn't intend to cache is a worse failure mode than requiring one line of code to opt in.

## Cache Strategies

| Strategy | Package Dependency | Use Case |
|---|---|---|
| Memory | `Microsoft.Extensions.Caching.Memory` | Single-instance apps, low-latency, no cross-instance consistency needed. |
| Distributed | `Microsoft.Extensions.Caching.Distributed` (Redis/SQL Server providers) | Multi-instance deployments needing shared cache state. |
| Hybrid | `Microsoft.Extensions.Caching.Hybrid` (the .NET 9+ `HybridCache` API) | L1 (in-memory) + L2 (distributed) with built-in stampede protection — the **recommended default** for new Conduit applications targeting .NET 10, since it solves the classic "distributed cache alone is slow, memory cache alone doesn't scale" tradeoff without bespoke code. |

`Conduit.Caching`'s `CachingBehavior<TRequest,TResponse>` is written against `HybridCache` as its primary target, with `IMemoryCache`/`IDistributedCache` adapters retained for consumers not yet on the `HybridCache` API.

## The Caching Behavior

```csharp
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableRequest
{
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var key = _keyProvider.GetCacheKey(request);
        return await _cache.GetOrCreateAsync(key, async _ => await next(), new HybridCacheEntryOptions
        {
            Expiration = request.Expiration ?? _options.DefaultExpiration
        }, cancellationToken: ct);
    }
}
```

Note this behavior is only ever registered **per-request** (via `PipelineBuilder.Use<CachingBehavior<,>>()` for specific cacheable query types, not as a `GlobalBehaviors` entry) — a global caching behavior would risk silently caching commands, which is a correctness hazard analyzer `CONDUIT024` explicitly guards against (flagging `CachingBehavior<,>` registered globally).

## Expiration

Expiration is always explicit per request (`ICacheableRequest.Expiration`) or falls back to a single `CachingOptions.DefaultExpiration` — there is no sliding-expiration-by-default, since sliding expiration silently changes cache freshness guarantees in ways that are easy to overlook.

## Invalidation

```csharp
public interface ICacheInvalidator<in TNotification> where TNotification : INotification
{
    ValueTask Invalidate(TNotification notification, ICacheInvalidationContext context, CancellationToken cancellationToken);
}
```

Invalidation is modeled as a **notification handler pattern**: when a command's handler raises a domain notification (e.g., `OrderUpdatedEvent`), a registered `ICacheInvalidator<OrderUpdatedEvent>` removes affected cache entries. This reuses the existing `INotification`/fan-out mechanism instead of inventing a separate cache-invalidation subscription model — one fewer concept for developers to learn.

## Key Design

Default keys are `{RequestFullTypeName}:{RequestRecordToString}` — because requests are `sealed record`s, their compiler-generated `ToString()`/structural equality already produces a deterministic, human-readable representation of all field values, which is exactly what a cache key needs. Consumers with large/complex requests can override `ICacheKeyProvider<TRequest>` to hash or project only cache-relevant fields.
