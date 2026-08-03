namespace PlaxionMediator.Caching;

/// <summary>
/// Removes cached mediator responses, typically after mutating operations.
/// </summary>
public interface IPlaxionMediatorCacheInvalidator
{
    /// <summary>
    /// Removes a single cache entry by key.
    /// </summary>
    /// <param name="cacheKey">The cache key previously used by a cacheable request.</param>
    void Remove(string cacheKey);

    /// <summary>
    /// Removes every cache entry previously associated with <paramref name="requestType"/>
    /// by <see cref="CachingBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <param name="requestType">The request CLR type whose cached responses should be removed.</param>
    void RemoveByRequestType(Type requestType);

    /// <summary>
    /// Removes every cache entry previously associated with <typeparamref name="TRequest"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type whose cached responses should be removed.</typeparam>
    void RemoveByRequestType<TRequest>();

    /// <summary>
    /// Associates <paramref name="cacheKey"/> with <paramref name="requestType"/> so type-based invalidation can find it.
    /// Called by <see cref="CachingBehavior{TRequest,TResponse}"/> on cache write.
    /// </summary>
    void Track(Type requestType, string cacheKey);
}
