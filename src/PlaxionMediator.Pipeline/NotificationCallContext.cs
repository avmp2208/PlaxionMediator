namespace PlaxionMediator.Pipeline;

/// <summary>
/// Allocation-free call context for <see cref="INotificationObserver"/> notifications.
/// Contains type metadata and handler count only — never notification payloads.
/// </summary>
public readonly struct NotificationCallContext
{
    /// <summary>
    /// Initializes a new call context.
    /// </summary>
    public NotificationCallContext(Type notificationType, int handlerCount)
    {
        NotificationType = notificationType ?? throw new ArgumentNullException(nameof(notificationType));
        HandlerCount = handlerCount;
    }

    /// <summary>
    /// The closed notification type being published.
    /// </summary>
    public Type NotificationType { get; }

    /// <summary>
    /// Number of notification handlers subscribed for this notification type.
    /// </summary>
    public int HandlerCount { get; }

    /// <summary>
    /// Convenience accessor matching diagnostic context on framework exceptions.
    /// </summary>
    public string NotificationTypeName => NotificationType.Name;
}
