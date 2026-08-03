using Microsoft.Extensions.Caching.Memory;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Caching;

/// <summary>
/// Pipeline behavior that caches successful responses for requests implementing
/// <see cref="ICacheableRequest{TResponse}"/>. Non-cacheable requests are a fast no-op.
/// Exceptions from the inner pipeline are never cached.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly PlaxionMediatorCachingOptions _options;
    private readonly IPlaxionMediatorCacheInvalidator _invalidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    public CachingBehavior(
        IMemoryCache cache,
        PlaxionMediatorCachingOptions options,
        IPlaxionMediatorCacheInvalidator invalidator)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _invalidator = invalidator ?? throw new ArgumentNullException(nameof(invalidator));
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        // Fast path: open-generic registration applies to every request; only opt-in types cache.
        if (request is not ICacheableRequest<TResponse> cacheable)
        {
            return await next().ConfigureAwait(false);
        }

        string cacheKey = cacheable.CacheKey;
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return await next().ConfigureAwait(false);
        }

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return (TResponse)cached!;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Only successful results are cached — exceptions propagate and leave the cache untouched.
        TResponse response = await next().ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        TimeSpan duration = cacheable.CacheDuration ?? _options.DefaultCacheDuration;
        if (duration <= TimeSpan.Zero)
        {
            duration = _options.DefaultCacheDuration;
        }

        MemoryCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = duration,
        };

        _cache.Set(cacheKey, response!, entryOptions);
        _invalidator.Track(typeof(TRequest), cacheKey);

        return response;
    }
}
