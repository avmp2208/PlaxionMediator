namespace PlaxionMediator.Abstractions;

/// <summary>
/// Declares the publish execution strategy for a notification type.
/// The source generator emits dispatch code matching this choice per notification type.
/// When omitted, <see cref="PublishStrategy.Sequential"/> is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class NotificationPublishStrategyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPublishStrategyAttribute"/> class.
    /// </summary>
    /// <param name="strategy">The handler execution strategy for this notification type.</param>
    public NotificationPublishStrategyAttribute(PublishStrategy strategy)
    {
        Strategy = strategy;
    }

    /// <summary>
    /// Gets the publish strategy for the annotated notification type.
    /// </summary>
    public PublishStrategy Strategy { get; }
}
