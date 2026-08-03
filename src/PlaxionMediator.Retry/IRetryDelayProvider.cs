namespace PlaxionMediator.Retry;

/// <summary>
/// Abstraction over the delay between retry attempts so tests can avoid real sleeps.
/// </summary>
public interface IRetryDelayProvider
{
    /// <summary>
    /// Delays for the given duration, observing <paramref name="cancellationToken"/>.
    /// </summary>
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
