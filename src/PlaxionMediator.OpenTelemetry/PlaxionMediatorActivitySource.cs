using System.Diagnostics;
using System.Reflection;

namespace PlaxionMediator.OpenTelemetry;

/// <summary>
/// Well-known <see cref="ActivitySource"/> and tag name constants for PlaxionMediator OpenTelemetry tracing.
/// </summary>
public static class PlaxionMediatorActivitySource
{
    /// <summary>
    /// Name shared with <see cref="Source"/>; use when registering listeners.
    /// </summary>
    public const string Name = "PlaxionMediator";

    /// <summary>
    /// The <see cref="ActivitySource"/> used for all PlaxionMediator Send/Publish spans.
    /// </summary>
    public static readonly ActivitySource Source = new(
        Name,
        typeof(PlaxionMediatorActivitySource).Assembly.GetName().Version?.ToString() ?? "0.7.0");

    /// <summary>
    /// Tag name for the closed request type name.
    /// </summary>
    public const string RequestTypeTag = "plaxionmediator.request.type";

    /// <summary>
    /// Tag name for the closed response type name.
    /// </summary>
    public const string ResponseTypeTag = "plaxionmediator.response.type";

    /// <summary>
    /// Tag name for the resolved handler type name.
    /// </summary>
    public const string HandlerTypeTag = "plaxionmediator.handler.type";

    /// <summary>
    /// Tag name for the notification type name.
    /// </summary>
    public const string NotificationTypeTag = "plaxionmediator.notification.type";

    /// <summary>
    /// Tag name for the number of pipeline behaviors/handlers involved in the call.
    /// </summary>
    public const string BehaviorCountTag = "plaxionmediator.behavior.count";
}
