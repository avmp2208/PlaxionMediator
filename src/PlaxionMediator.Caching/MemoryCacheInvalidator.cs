using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace PlaxionMediator.Caching;

/// <summary>
/// <see cref="IMemoryCache"/>-backed invalidator that tracks keys per request type.
/// </summary>
public sealed class MemoryCacheInvalidator : IPlaxionMediatorCacheInvalidator
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, byte>> _keysByType = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheInvalidator"/> class.
    /// </summary>
    /// <param name="cache">The memory cache that stores mediator responses.</param>
    public MemoryCacheInvalidator(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public void Remove(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        _cache.Remove(cacheKey);

        foreach (KeyValuePair<Type, ConcurrentDictionary<string, byte>> pair in _keysByType)
        {
            pair.Value.TryRemove(cacheKey, out _);
        }
    }

    /// <inheritdoc />
    public void RemoveByRequestType(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        if (!_keysByType.TryRemove(requestType, out ConcurrentDictionary<string, byte>? keys))
        {
            return;
        }

        foreach (string key in keys.Keys)
        {
            _cache.Remove(key);
        }
    }

    /// <inheritdoc />
    public void RemoveByRequestType<TRequest>() => RemoveByRequestType(typeof(TRequest));

    /// <inheritdoc />
    public void Track(Type requestType, string cacheKey)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        ConcurrentDictionary<string, byte> keys = _keysByType.GetOrAdd(
            requestType,
            static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));

        keys[cacheKey] = 0;
    }
}
