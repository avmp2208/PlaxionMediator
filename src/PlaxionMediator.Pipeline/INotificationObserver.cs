namespace PlaxionMediator.Pipeline;

/// <summary>
/// Lightweight instrumentation observer notified at notification publish start, successful completion,
/// and fault. Intended as the ADR-0008/ADR-0009 seam for future telemetry packages
/// (e.g. PlaxionMediator.OpenTelemetry); Core/Pipeline take no dependency on OpenTelemetry or diagnostic
/// libraries.
/// </summary>
/// <remarks>
/// Implementations must be allocation-conscious. Context carries type metadata only (never payloads).
/// Observer exceptions are not caught by the hub — keep implementations non-throwing.
/// </remarks>
public interface INotificationObserver
{
    /// <summary>
    /// Called immediately before notification handlers begin executing.
    /// </summary>
    void OnStarting(in NotificationCallContext context);

    /// <summary>
    /// Called after all notification handlers complete successfully (no fault).
    /// </summary>
    void OnCompleted(in NotificationCallContext context);

    /// <summary>
    /// Called when publishing a notification faults.
    /// </summary>
    void OnFaulted(in NotificationCallContext context, Exception exception);
}
