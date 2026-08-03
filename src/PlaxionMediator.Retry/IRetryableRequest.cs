namespace PlaxionMediator.Retry;

/// <summary>
/// Marker for requests that opt into automatic retry via <see cref="RetryBehavior{TRequest,TResponse}"/>.
/// Per-request values override <see cref="PlaxionMediatorRetryOptions"/> when non-null.
/// </summary>
public interface IRetryableRequest
{
    /// <summary>
    /// Maximum number of retry attempts after the initial try.
    /// When <see langword="null"/>, <see cref="PlaxionMediatorRetryOptions.MaxRetryAttempts"/> is used.
    /// </summary>
    int? MaxRetryAttempts => null;

    /// <summary>
    /// Base delay used by the configured backoff strategy between attempts.
    /// When <see langword="null"/>, <see cref="PlaxionMediatorRetryOptions.BaseDelay"/> is used.
    /// </summary>
    TimeSpan? BaseDelay => null;
}
