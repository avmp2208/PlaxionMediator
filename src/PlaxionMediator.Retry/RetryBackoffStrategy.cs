namespace PlaxionMediator.Retry;

/// <summary>
/// Strategy used to compute the delay between retry attempts.
/// </summary>
public enum RetryBackoffStrategy
{
    /// <summary>
    /// Always wait <see cref="PlaxionMediatorRetryOptions.BaseDelay"/> (or the per-request base delay).
    /// </summary>
    Constant = 0,

    /// <summary>
    /// Wait <c>baseDelay * 2^attemptIndex</c> (attemptIndex starts at 0 for the first retry).
    /// </summary>
    Exponential = 1,
}
