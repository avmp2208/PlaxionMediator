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

    /// <summary>
    /// Tag name for the correlation id associated with the call. Sourced from the ambient
    /// <see cref="Activity.Current"/>'s W3C trace context (<see cref="ActivityTraceId"/>), or from a
    /// caller-supplied <c>correlation.id</c> baggage item when present, so callers can flow their own
    /// business-level correlation id without PlaxionMediator taking on any new dependency.
    /// </summary>
    public const string CorrelationIdTag = "plaxionmediator.correlation_id";

    /// <summary>
    /// Baggage key a caller can set via <see cref="Activity.SetBaggage(string, string?)"/> on the ambient
    /// activity to supply a business-level correlation id that takes precedence over the trace id.
    /// </summary>
    public const string CorrelationIdBaggageKey = "correlation.id";
}
