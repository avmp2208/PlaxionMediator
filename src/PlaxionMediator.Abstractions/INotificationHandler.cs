namespace PlaxionMediator.Abstractions;

/// <summary>
/// Handles a notification. Zero or more handlers may exist per notification type.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification as an independent side effect.
    /// </summary>
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
}
