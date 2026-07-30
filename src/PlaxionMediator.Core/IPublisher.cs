using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Core;

/// <summary>
/// The single entry point for fan-out notification dispatch.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
