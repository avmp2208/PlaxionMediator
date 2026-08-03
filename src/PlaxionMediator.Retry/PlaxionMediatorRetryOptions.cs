namespace PlaxionMediator.Retry;

/// <summary>
/// Options controlling default retry behavior for retryable requests.
/// </summary>
/// <remarks>
/// This package intentionally has no hard dependency on <c>PlaxionMediator.Validation</c>.
/// When using validation, add <c>typeof(PlaxionMediatorValidationException)</c> to
/// <see cref="NonRetryableExceptionTypes"/> so validation failures are not retried.
/// <see cref="OperationCanceledException"/> (including <see cref="TaskCanceledException"/>) is never retried.
/// </remarks>
public sealed class PlaxionMediatorRetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts after the initial try. Defaults to 3
    /// (up to 4 total executions of the inner pipeline).
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between attempts. Defaults to 200ms.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Backoff strategy applied to <see cref="BaseDelay"/>. Defaults to exponential.
    /// </summary>
    public RetryBackoffStrategy BackoffStrategy { get; set; } = RetryBackoffStrategy.Exponential;

    /// <summary>
    /// Exception types that must never be retried (matched via <see cref="Type.IsInstanceOfType(object?)"/>).
    /// Always implicitly includes <see cref="OperationCanceledException"/>.
    /// </summary>
    public IList<Type> NonRetryableExceptionTypes { get; } = new List<Type>();
}
