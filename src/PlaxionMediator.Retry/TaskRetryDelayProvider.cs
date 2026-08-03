namespace PlaxionMediator.Retry;

/// <summary>
/// Default <see cref="IRetryDelayProvider"/> that uses <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
/// </summary>
public sealed class TaskRetryDelayProvider : IRetryDelayProvider
{
    /// <inheritdoc />
    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        if (delay <= TimeSpan.Zero)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(Task.Delay(delay, cancellationToken));
    }
}
