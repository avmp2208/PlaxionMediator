using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Retry;

/// <summary>
/// Pipeline behavior that retries the inner pipeline for requests implementing
/// <see cref="IRetryableRequest"/> when transient exceptions occur.
/// Non-retryable requests are a fast no-op. Cancellation is never retried.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly PlaxionMediatorRetryOptions _options;
    private readonly IRetryDelayProvider _delayProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    public RetryBehavior(PlaxionMediatorRetryOptions options, IRetryDelayProvider delayProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
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

        // Fast path: open-generic registration applies to every request; only opt-in types retry.
        if (request is not IRetryableRequest retryable)
        {
            return await next().ConfigureAwait(false);
        }

        int maxRetryAttempts = retryable.MaxRetryAttempts ?? _options.MaxRetryAttempts;
        if (maxRetryAttempts < 0)
        {
            maxRetryAttempts = 0;
        }

        TimeSpan baseDelay = retryable.BaseDelay ?? _options.BaseDelay;
        if (baseDelay < TimeSpan.Zero)
        {
            baseDelay = TimeSpan.Zero;
        }

        Exception? lastException = null;

        // attempt 0 = initial try; attempts 1..maxRetryAttempts = retries
        for (int attempt = 0; attempt <= maxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // Always propagate cancellation tied to the caller's token.
                if (ex is OperationCanceledException oce && oce.CancellationToken == cancellationToken && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                if (IsNonRetryable(ex))
                {
                    throw;
                }

                lastException = ex;

                if (attempt >= maxRetryAttempts)
                {
                    break;
                }

                TimeSpan delay = ComputeDelay(baseDelay, attempt, _options.BackoffStrategy);
                if (delay > TimeSpan.Zero)
                {
                    await _delayProvider.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // All attempts exhausted.
        throw lastException!;
    }

    private bool IsNonRetryable(Exception exception)
    {
        // Cancellation is never a transient fault worth retrying.
        if (exception is OperationCanceledException)
        {
            return true;
        }

        IList<Type> nonRetryable = _options.NonRetryableExceptionTypes;
        for (int i = 0; i < nonRetryable.Count; i++)
        {
            Type? type = nonRetryable[i];
            if (type is not null && type.IsInstanceOfType(exception))
            {
                return true;
            }
        }

        return false;
    }

    private static TimeSpan ComputeDelay(TimeSpan baseDelay, int retryAttemptIndex, RetryBackoffStrategy strategy)
    {
        if (baseDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (strategy == RetryBackoffStrategy.Constant)
        {
            return baseDelay;
        }

        // Exponential: base * 2^retryAttemptIndex, clamped to avoid overflow.
        double factor = Math.Pow(2, retryAttemptIndex);
        double milliseconds = baseDelay.TotalMilliseconds * factor;
        if (double.IsInfinity(milliseconds) || milliseconds >= TimeSpan.MaxValue.TotalMilliseconds)
        {
            return TimeSpan.FromMilliseconds(int.MaxValue);
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
