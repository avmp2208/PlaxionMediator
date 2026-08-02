namespace PlaxionMediator.Abstractions;

/// <summary>
/// Controls how <see cref="INotificationHandler{TNotification}"/> instances are invoked
/// when a notification is published.
/// </summary>
public enum PublishStrategy
{
    /// <summary>
    /// Handlers are awaited one after another in registration order.
    /// Exceptions are collected and thrown after all handlers complete.
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Handlers are invoked concurrently. Exceptions are collected and thrown
    /// after all handlers complete.
    /// </summary>
    Parallel = 1,
}
