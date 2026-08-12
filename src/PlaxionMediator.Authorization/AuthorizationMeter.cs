using System.Diagnostics.Metrics;

namespace PlaxionMediator.Authorization;

/// <summary>
/// Well-known <see cref="Meter"/> and instruments for PlaxionMediator Authorization metrics.
/// </summary>
public static class AuthorizationMeter
{
    /// <summary>
    /// Name shared with <see cref="Meter"/>; use when registering listeners.
    /// </summary>
    public const string Name = "PlaxionMediator.Authorization";

    /// <summary>
    /// The <see cref="Metrics.Meter"/> used for all PlaxionMediator Authorization instruments.
    /// </summary>
    public static readonly Meter Meter = new(Name);

    /// <summary>
    /// Total number of successful authorization evaluations.
    /// </summary>
    public static readonly Counter<long> AllowedCount =
        Meter.CreateCounter<long>("plaxionmediator.authorization.allowed");

    /// <summary>
    /// Total number of denied authorization evaluations.
    /// </summary>
    public static readonly Counter<long> DeniedCount =
        Meter.CreateCounter<long>("plaxionmediator.authorization.denied");

    /// <summary>
    /// Duration, in milliseconds, of the authorization evaluation phase.
    /// </summary>
    public static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("plaxionmediator.authorization.duration", unit: "ms");

    /// <summary>
    /// Tag name for the closed request type name.
    /// </summary>
    public const string RequestTypeTag = "plaxionmediator.request.type";
}
