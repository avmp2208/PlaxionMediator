namespace PlaxionMediator.Pipeline;

/// <summary>
/// Lightweight instrumentation observer notified at pipeline start, successful completion, and fault.
/// Intended as the ADR-0008 seam for future telemetry packages (e.g. PlaxionMediator.OpenTelemetry);
/// Core/Pipeline take no dependency on OpenTelemetry or diagnostic libraries.
/// </summary>
/// <remarks>
/// Implementations must be allocation-conscious. Context carries type metadata only (never payloads).
/// Observer exceptions are not caught by the hub — keep implementations non-throwing.
/// </remarks>
public interface IPipelineObserver
{
    /// <summary>
    /// Called immediately before the behavior chain / handler begins executing.
    /// </summary>
    void OnStarting(in PipelineCallContext context);

    /// <summary>
    /// Called after the pipeline completes successfully (response produced, no fault).
    /// </summary>
    void OnCompleted(in PipelineCallContext context);

    /// <summary>
    /// Called when the pipeline faults. <paramref name="exception"/> is the exception as observed
    /// at the execution boundary (may be <see cref="Core.HandlerFaultException"/> before unwrap,
    /// <see cref="Core.PipelineExecutionException"/> for behavior faults, or a raw exception).
    /// </summary>
    void OnFaulted(in PipelineCallContext context, Exception exception);
}
