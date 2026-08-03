namespace PlaxionMediator.Caching;

/// <summary>
/// Options controlling default caching behavior for cacheable requests.
/// </summary>
public sealed class PlaxionMediatorCachingOptions
{
    /// <summary>
    /// Default absolute lifetime applied when a request does not specify <see cref="ICacheableRequest{TResponse}.CacheDuration"/>.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
