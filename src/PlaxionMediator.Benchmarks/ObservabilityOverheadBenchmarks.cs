using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.OpenTelemetry;
using PlaxionMediator;

namespace PlaxionMediator.Benchmarks;

/// <summary>
/// Compares <see cref="ISender.Send"/> latency/allocations with no observer registered (the
/// pre-v0.7.0 hot path, gated behind <c>PipelineObserverHub.HasObservers</c>) against the same
/// dispatch with <see cref="PlaxionMediator.OpenTelemetry"/>'s <c>OpenTelemetryPipelineObserver</c>
/// registered via <see cref="ServiceCollectionExtensions.AddPlaxionMediatorOpenTelemetry"/>.
/// Each scenario is isolated in its own <see cref="ServiceProvider"/>; BenchmarkDotNet runs each
/// benchmark class in its own process, so the process-wide observer hubs never leak across scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ObservabilityOverheadBenchmarks
{
    private ServiceProvider _noObserverProvider = null!;
    private ServiceProvider _openTelemetryProvider = null!;
    private ISender _noObserverSender = null!;
    private ISender _openTelemetrySender = null!;
    private IPublisher _noObserverPublisher = null!;
    private IPublisher _openTelemetryPublisher = null!;
    private Ping _ping = null!;
    private OneHandlerNotification _notification = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ping = new Ping("benchmark");
        _notification = new OneHandlerNotification("id");

        ServiceCollection noObserverServices = new();
        noObserverServices.AddPlaxionMediator();
        _noObserverProvider = noObserverServices.BuildServiceProvider();
        _noObserverSender = _noObserverProvider.GetRequiredService<ISender>();
        _noObserverPublisher = _noObserverProvider.GetRequiredService<IPublisher>();

        ServiceCollection openTelemetryServices = new();
        openTelemetryServices.AddPlaxionMediator();
        openTelemetryServices.AddPlaxionMediatorOpenTelemetry();
        _openTelemetryProvider = openTelemetryServices.BuildServiceProvider();
        _openTelemetrySender = _openTelemetryProvider.GetRequiredService<ISender>();
        _openTelemetryPublisher = _openTelemetryProvider.GetRequiredService<IPublisher>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _noObserverProvider.Dispose();
        _openTelemetryProvider.Dispose();
    }

    /// <summary>Baseline: no <c>IPipelineObserver</c>/<c>INotificationObserver</c> registered.</summary>
    [Benchmark(Description = "Send_NoObserver", Baseline = true)]
    public ValueTask<string> Send_NoObserver()
        => _noObserverSender.Send(_ping);

    /// <summary>With <c>PlaxionMediator.OpenTelemetry</c>'s observer registered.</summary>
    [Benchmark(Description = "Send_OpenTelemetry")]
    public ValueTask<string> Send_OpenTelemetry()
        => _openTelemetrySender.Send(_ping);

    /// <summary>Baseline: no <c>INotificationObserver</c> registered.</summary>
    [Benchmark(Description = "Publish_NoObserver")]
    public ValueTask Publish_NoObserver()
        => _noObserverPublisher.Publish(_notification);

    /// <summary>With <c>PlaxionMediator.OpenTelemetry</c>'s observer registered.</summary>
    [Benchmark(Description = "Publish_OpenTelemetry")]
    public ValueTask Publish_OpenTelemetry()
        => _openTelemetryPublisher.Publish(_notification);
}
