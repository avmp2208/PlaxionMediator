using System.Diagnostics.Metrics;

namespace PlaxionMediator.OpenTelemetry;

/// <summary>
/// Well-known <see cref="Meter"/> and instruments for PlaxionMediator OpenTelemetry metrics.
/// </summary>
public static class PlaxionMediatorMeter
{
    /// <summary>
    /// Name shared with <see cref="Meter"/>; use when registering listeners.
    /// </summary>
    public const string Name = "PlaxionMediator";

    /// <summary>
    /// The <see cref="Metrics.Meter"/> used for all PlaxionMediator instruments.
    /// </summary>
    public static readonly Meter Meter = new(Name);

    /// <summary>
    /// Duration, in milliseconds, of a Send/Publish call from start to completion or fault.
    /// </summary>
    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>("plaxionmediator.request.duration", unit: "ms");

    /// <summary>
    /// Total number of Send/Publish calls observed.
    /// </summary>
    public static readonly Counter<long> RequestCount =
        Meter.CreateCounter<long>("plaxionmediator.request.count");

    /// <summary>
    /// Total number of calls that faulted because no handler was registered for the request type.
    /// </summary>
    public static readonly Counter<long> HandlerNotFoundCount =
        Meter.CreateCounter<long>("plaxionmediator.handler_not_found.count");

    /// <summary>
    /// Total number of calls that faulted for any other reason (behavior or handler exception).
    /// </summary>
    public static readonly Counter<long> PipelineExceptionCount =
        Meter.CreateCounter<long>("plaxionmediator.pipeline_exception.count");
}
