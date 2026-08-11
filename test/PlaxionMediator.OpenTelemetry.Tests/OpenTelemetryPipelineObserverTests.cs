using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Core;
using PlaxionMediator.Pipeline;
using Xunit;

namespace PlaxionMediator.OpenTelemetry.Tests;

/// <summary>
/// These tests mutate the process-wide <see cref="PipelineObserverHub"/>/<see cref="NotificationObserverHub"/>
/// state so parallel xUnit workers cannot interfere.
/// </summary>
[CollectionDefinition("OpenTelemetryStaticHubs", DisableParallelization = true)]
public sealed class OpenTelemetryStaticHubsCollectionDefinition;

[Collection("OpenTelemetryStaticHubs")]
public sealed class OpenTelemetryPipelineObserverTests : IDisposable
{
    public OpenTelemetryPipelineObserverTests()
    {
        PipelineObserverHub.Clear();
        NotificationObserverHub.Clear();
    }

    public void Dispose()
    {
        PipelineObserverHub.Clear();
        NotificationObserverHub.Clear();
    }

    [Fact]
    public void AddPlaxionMediatorOpenTelemetry_Registers_Observer_In_Both_Hubs()
    {
        Assert.False(PipelineObserverHub.HasObservers);
        Assert.False(NotificationObserverHub.HasObservers);

        ServiceCollection services = new();
        services.AddPlaxionMediatorOpenTelemetry();

        Assert.True(PipelineObserverHub.HasObservers);
        Assert.True(NotificationObserverHub.HasObservers);
    }

    [Fact]
    public void Successful_Send_Creates_Activity_With_Expected_Tags()
    {
        List<Activity> activities = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == PlaxionMediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        OpenTelemetryPipelineObserver observer = new();
        PipelineCallContext context = new(typeof(string), typeof(int), 2);

        observer.OnStarting(in context);
        observer.OnCompleted(in context);

        Activity activity = Assert.Single(activities);
        Assert.Equal(nameof(String), activity.OperationName);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Contains(activity.Tags, t => t.Key == PlaxionMediatorActivitySource.RequestTypeTag);
        Assert.Contains(activity.Tags, t => t.Key == PlaxionMediatorActivitySource.ResponseTypeTag);
        Assert.Contains(activity.TagObjects, t => t.Key == PlaxionMediatorActivitySource.BehaviorCountTag && Equals(t.Value, 2));
    }

    [Fact]
    public void Faulted_Send_Sets_Error_Status_And_Increments_PipelineExceptionCount()
    {
        List<Activity> activities = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == PlaxionMediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        long pipelineExceptionCount = 0;
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener2) =>
        {
            if (instrument.Meter.Name == PlaxionMediatorMeter.Name)
            {
                listener2.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "plaxionmediator.pipeline_exception.count")
            {
                pipelineExceptionCount += measurement;
            }
        });
        meterListener.Start();

        OpenTelemetryPipelineObserver observer = new();
        PipelineCallContext context = new(typeof(string), typeof(int), 0);

        observer.OnStarting(in context);
        observer.OnFaulted(in context, new InvalidOperationException("boom"));

        Activity activity = Assert.Single(activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(1, pipelineExceptionCount);
    }

    [Fact]
    public void HandlerNotFoundException_Increments_HandlerNotFoundCount()
    {
        long handlerNotFoundCount = 0;
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == PlaxionMediatorMeter.Name)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "plaxionmediator.handler_not_found.count")
            {
                handlerNotFoundCount += measurement;
            }
        });
        meterListener.Start();

        OpenTelemetryPipelineObserver observer = new();
        PipelineCallContext context = new(typeof(string), typeof(int), 0);

        observer.OnStarting(in context);
        observer.OnFaulted(in context, new HandlerNotFoundException(typeof(string)));

        Assert.Equal(1, handlerNotFoundCount);
    }

    [Fact]
    public void Successful_Publish_Creates_Activity_And_Records_Metrics()
    {
        List<Activity> activities = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == PlaxionMediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        OpenTelemetryPipelineObserver observer = new();
        NotificationCallContext context = new(typeof(string), 3);

        observer.OnStarting(in context);
        observer.OnCompleted(in context);

        Activity activity = Assert.Single(activities);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Contains(activity.Tags, t => t.Key == PlaxionMediatorActivitySource.NotificationTypeTag);
    }

    [Fact]
    public void Send_Without_Baggage_Uses_TraceId_As_CorrelationId()
    {
        List<Activity> activities = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == PlaxionMediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        OpenTelemetryPipelineObserver observer = new();
        PipelineCallContext context = new(typeof(string), typeof(int), 0);

        observer.OnStarting(in context);
        observer.OnCompleted(in context);

        Activity activity = Assert.Single(activities);
        KeyValuePair<string, object?> correlationTag = Assert.Single(activity.TagObjects, t => t.Key == PlaxionMediatorActivitySource.CorrelationIdTag);
        Assert.Equal(activity.TraceId.ToString(), correlationTag.Value);
    }

    [Fact]
    public void Send_With_Baggage_Uses_CallerSupplied_CorrelationId()
    {
        List<Activity> activities = [];
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == PlaxionMediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        using ActivitySource parentSource = new("PlaxionMediator.OpenTelemetry.Tests.Parent");
        using ActivityListener parentListener = new()
        {
            ShouldListenTo = source => source.Name == parentSource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(parentListener);

        using Activity? parentActivity = parentSource.StartActivity("parent");
        parentActivity?.SetBaggage(PlaxionMediatorActivitySource.CorrelationIdBaggageKey, "business-correlation-id");

        OpenTelemetryPipelineObserver observer = new();
        PipelineCallContext context = new(typeof(string), typeof(int), 0);

        observer.OnStarting(in context);
        observer.OnCompleted(in context);

        Activity activity = Assert.Single(activities);
        KeyValuePair<string, object?> correlationTag = Assert.Single(activity.TagObjects, t => t.Key == PlaxionMediatorActivitySource.CorrelationIdTag);
        Assert.Equal("business-correlation-id", correlationTag.Value);
    }
}
