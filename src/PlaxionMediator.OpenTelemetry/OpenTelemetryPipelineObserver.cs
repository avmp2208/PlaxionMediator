using System.Diagnostics;
using PlaxionMediator.Core;
using PlaxionMediator.Pipeline;

namespace PlaxionMediator.OpenTelemetry;

/// <summary>
/// <see cref="IPipelineObserver"/>/<see cref="INotificationObserver"/> implementation that records
/// PlaxionMediator Send/Publish activity spans and metrics via the standard <see cref="ActivitySource"/>
/// and <see cref="System.Diagnostics.Metrics.Meter"/> primitives (no vendor lock-in).
/// </summary>
public sealed class OpenTelemetryPipelineObserver : IPipelineObserver, INotificationObserver
{
    private static readonly AsyncLocal<Activity?> CurrentRequestActivity = new();
    private static readonly AsyncLocal<Activity?> CurrentNotificationActivity = new();
    private static readonly AsyncLocal<long> RequestStartTimestamp = new();
    private static readonly AsyncLocal<long> NotificationStartTimestamp = new();

    /// <inheritdoc />
    public void OnStarting(in PipelineCallContext context)
    {
        Activity? activity = PlaxionMediatorActivitySource.Source.StartActivity(
            context.RequestTypeName,
            ActivityKind.Internal);

        activity?.SetTag(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName);
        activity?.SetTag(PlaxionMediatorActivitySource.ResponseTypeTag, context.ResponseType.FullName);
        activity?.SetTag(PlaxionMediatorActivitySource.BehaviorCountTag, context.BehaviorCount);
        activity?.SetTag(PlaxionMediatorActivitySource.CorrelationIdTag, ResolveCorrelationId(activity));

        CurrentRequestActivity.Value = activity;
        RequestStartTimestamp.Value = Stopwatch.GetTimestamp();
    }

    /// <inheritdoc />
    public void OnCompleted(in PipelineCallContext context)
    {
        double elapsedMs = ElapsedMilliseconds(RequestStartTimestamp.Value);

        Activity? activity = CurrentRequestActivity.Value;
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.Dispose();
        CurrentRequestActivity.Value = null;

        PlaxionMediatorMeter.RequestCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName));
        PlaxionMediatorMeter.RequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName));
    }

    /// <inheritdoc />
    public void OnFaulted(in PipelineCallContext context, Exception exception)
    {
        double elapsedMs = ElapsedMilliseconds(RequestStartTimestamp.Value);

        Activity? activity = CurrentRequestActivity.Value;
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddTag("exception.type", exception.GetType().FullName);
        activity?.Dispose();
        CurrentRequestActivity.Value = null;

        PlaxionMediatorMeter.RequestCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName));
        PlaxionMediatorMeter.RequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName));

        if (exception is HandlerNotFoundException)
        {
            PlaxionMediatorMeter.HandlerNotFoundCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName));
        }
        else
        {
            PlaxionMediatorMeter.PipelineExceptionCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.RequestTypeTag, context.RequestType.FullName));
        }
    }

    /// <inheritdoc />
    public void OnStarting(in NotificationCallContext context)
    {
        Activity? activity = PlaxionMediatorActivitySource.Source.StartActivity(
            context.NotificationTypeName,
            ActivityKind.Producer);

        activity?.SetTag(PlaxionMediatorActivitySource.NotificationTypeTag, context.NotificationType.FullName);
        activity?.SetTag(PlaxionMediatorActivitySource.BehaviorCountTag, context.HandlerCount);
        activity?.SetTag(PlaxionMediatorActivitySource.CorrelationIdTag, ResolveCorrelationId(activity));

        CurrentNotificationActivity.Value = activity;
        NotificationStartTimestamp.Value = Stopwatch.GetTimestamp();
    }

    /// <inheritdoc />
    public void OnCompleted(in NotificationCallContext context)
    {
        double elapsedMs = ElapsedMilliseconds(NotificationStartTimestamp.Value);

        Activity? activity = CurrentNotificationActivity.Value;
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.Dispose();
        CurrentNotificationActivity.Value = null;

        PlaxionMediatorMeter.RequestCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.NotificationTypeTag, context.NotificationType.FullName));
        PlaxionMediatorMeter.RequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.NotificationTypeTag, context.NotificationType.FullName));
    }

    /// <inheritdoc />
    public void OnFaulted(in NotificationCallContext context, Exception exception)
    {
        double elapsedMs = ElapsedMilliseconds(NotificationStartTimestamp.Value);

        Activity? activity = CurrentNotificationActivity.Value;
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddTag("exception.type", exception.GetType().FullName);
        activity?.Dispose();
        CurrentNotificationActivity.Value = null;

        PlaxionMediatorMeter.RequestCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.NotificationTypeTag, context.NotificationType.FullName));
        PlaxionMediatorMeter.RequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.NotificationTypeTag, context.NotificationType.FullName));
        PlaxionMediatorMeter.PipelineExceptionCount.Add(1, new KeyValuePair<string, object?>(PlaxionMediatorActivitySource.NotificationTypeTag, context.NotificationType.FullName));
    }

    private static double ElapsedMilliseconds(long startTimestamp) =>
        startTimestamp == 0 ? 0 : Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    /// <summary>
    /// Resolves a correlation id for the current call: a caller-supplied
    /// <see cref="PlaxionMediatorActivitySource.CorrelationIdBaggageKey"/> baggage item takes precedence
    /// (so business-level ids flow through untouched), falling back to the activity's own W3C
    /// <see cref="ActivityTraceId"/> so every span/metric is correlatable even without any caller setup.
    /// </summary>
    private static string? ResolveCorrelationId(Activity? activity)
    {
        if (activity is null)
        {
            return null;
        }

        string? baggageCorrelationId = activity.GetBaggageItem(PlaxionMediatorActivitySource.CorrelationIdBaggageKey);
        return !string.IsNullOrEmpty(baggageCorrelationId) ? baggageCorrelationId : activity.TraceId.ToString();
    }
}
