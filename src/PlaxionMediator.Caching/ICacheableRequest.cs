namespace PlaxionMediator.Caching;

/// <summary>
/// Marker for requests that opt into response caching via <see cref="CachingBehavior{TRequest,TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the request.</typeparam>
public interface ICacheableRequest<TResponse>
{
    /// <summary>
    /// The cache key used to store and look up the response.
    /// Empty or whitespace keys disable caching for that request instance.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Optional absolute lifetime for the cached entry.
    /// When <see langword="null"/>, <see cref="PlaxionMediatorCachingOptions.DefaultCacheDuration"/> is used.
    /// </summary>
    TimeSpan? CacheDuration => null;
}
