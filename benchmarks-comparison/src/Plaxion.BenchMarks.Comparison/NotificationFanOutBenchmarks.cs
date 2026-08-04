using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Comparison.MediatorAdapter;
using Comparison.MediatRAdapter;
using Comparison.PlaxionAdapter;
using Comparison.Shared;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Core;

namespace Plaxion.BenchMarks.Comparison;

/// <summary>
/// Notification fan-out benchmarks across Plaxion, MediatR, and Mediator.
/// Each handler-count tier uses an isolated <see cref="ServiceProvider"/>.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class NotificationFanOutBenchmarks
{
    private ServiceProvider _plaxion1Provider = null!;
    private ServiceProvider _plaxion10Provider = null!;
    private ServiceProvider _plaxion50Provider = null!;
    private ServiceProvider _plaxion100Provider = null!;

    private ServiceProvider _mediatR1Provider = null!;
    private ServiceProvider _mediatR10Provider = null!;
    private ServiceProvider _mediatR50Provider = null!;
    private ServiceProvider _mediatR100Provider = null!;

    private ServiceProvider _mediator1Provider = null!;
    private ServiceProvider _mediator10Provider = null!;
    private ServiceProvider _mediator50Provider = null!;
    private ServiceProvider _mediator100Provider = null!;

    private IPublisher _plaxion1Publisher = null!;
    private IPublisher _plaxion10Publisher = null!;
    private IPublisher _plaxion50Publisher = null!;
    private IPublisher _plaxion100Publisher = null!;

    private MediatR.IMediator _mediatR1Mediator = null!;
    private MediatR.IMediator _mediatR10Mediator = null!;
    private MediatR.IMediator _mediatR50Mediator = null!;
    private MediatR.IMediator _mediatR100Mediator = null!;

    private Mediator.IMediator _mediator1Mediator = null!;
    private Mediator.IMediator _mediator10Mediator = null!;
    private Mediator.IMediator _mediator50Mediator = null!;
    private Mediator.IMediator _mediator100Mediator = null!;

    private PlaxionFanOutNotification _plaxionNotification = null!;
    private MediatRFanOutNotification _mediatRNotification = null!;
    private MediatorFanOutNotification _mediatorNotification = null!;

    [GlobalSetup]
    public void Setup()
    {
        var payload = new ScenarioPayload("notification-fanout", "benchmark");
        _plaxionNotification = new PlaxionFanOutNotification(payload);
        _mediatRNotification = new MediatRFanOutNotification(payload);
        _mediatorNotification = new MediatorFanOutNotification(payload);

        _plaxion1Provider = PlaxionAdapterFactory.BuildServiceProviderForNotifications(1);
        _plaxion10Provider = PlaxionAdapterFactory.BuildServiceProviderForNotifications(10);
        _plaxion50Provider = PlaxionAdapterFactory.BuildServiceProviderForNotifications(50);
        _plaxion100Provider = PlaxionAdapterFactory.BuildServiceProviderForNotifications(100);

        _plaxion1Publisher = _plaxion1Provider.GetRequiredService<IPublisher>();
        _plaxion10Publisher = _plaxion10Provider.GetRequiredService<IPublisher>();
        _plaxion50Publisher = _plaxion50Provider.GetRequiredService<IPublisher>();
        _plaxion100Publisher = _plaxion100Provider.GetRequiredService<IPublisher>();

        _mediatR1Provider = MediatRAdapterFactory.BuildServiceProviderForNotifications(1);
        _mediatR10Provider = MediatRAdapterFactory.BuildServiceProviderForNotifications(10);
        _mediatR50Provider = MediatRAdapterFactory.BuildServiceProviderForNotifications(50);
        _mediatR100Provider = MediatRAdapterFactory.BuildServiceProviderForNotifications(100);

        _mediatR1Mediator = _mediatR1Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR10Mediator = _mediatR10Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR50Mediator = _mediatR50Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR100Mediator = _mediatR100Provider.GetRequiredService<MediatR.IMediator>();

        _mediator1Provider = MediatorAdapterFactory.BuildServiceProviderForNotifications(1);
        _mediator10Provider = MediatorAdapterFactory.BuildServiceProviderForNotifications(10);
        _mediator50Provider = MediatorAdapterFactory.BuildServiceProviderForNotifications(50);
        _mediator100Provider = MediatorAdapterFactory.BuildServiceProviderForNotifications(100);

        _mediator1Mediator = _mediator1Provider.GetRequiredService<Mediator.IMediator>();
        _mediator10Mediator = _mediator10Provider.GetRequiredService<Mediator.IMediator>();
        _mediator50Mediator = _mediator50Provider.GetRequiredService<Mediator.IMediator>();
        _mediator100Mediator = _mediator100Provider.GetRequiredService<Mediator.IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plaxion1Provider.Dispose();
        _plaxion10Provider.Dispose();
        _plaxion50Provider.Dispose();
        _plaxion100Provider.Dispose();

        _mediatR1Provider.Dispose();
        _mediatR10Provider.Dispose();
        _mediatR50Provider.Dispose();
        _mediatR100Provider.Dispose();

        _mediator1Provider.Dispose();
        _mediator10Provider.Dispose();
        _mediator50Provider.Dispose();
        _mediator100Provider.Dispose();
    }

    [Benchmark(Description = "Publish_Plaxion_1Handler", Baseline = true)]
    public ValueTask Publish_Plaxion_1Handler()
        => _plaxion1Publisher.Publish(_plaxionNotification);

    [Benchmark(Description = "Publish_Plaxion_10Handlers")]
    public ValueTask Publish_Plaxion_10Handlers()
        => _plaxion10Publisher.Publish(_plaxionNotification);

    [Benchmark(Description = "Publish_Plaxion_50Handlers")]
    public ValueTask Publish_Plaxion_50Handlers()
        => _plaxion50Publisher.Publish(_plaxionNotification);

    [Benchmark(Description = "Publish_Plaxion_100Handlers")]
    public ValueTask Publish_Plaxion_100Handlers()
        => _plaxion100Publisher.Publish(_plaxionNotification);

    [Benchmark(Description = "Publish_MediatR_1Handler")]
    public Task Publish_MediatR_1Handler()
        => _mediatR1Mediator.Publish(_mediatRNotification);

    [Benchmark(Description = "Publish_MediatR_10Handlers")]
    public Task Publish_MediatR_10Handlers()
        => _mediatR10Mediator.Publish(_mediatRNotification);

    [Benchmark(Description = "Publish_MediatR_50Handlers")]
    public Task Publish_MediatR_50Handlers()
        => _mediatR50Mediator.Publish(_mediatRNotification);

    [Benchmark(Description = "Publish_MediatR_100Handlers")]
    public Task Publish_MediatR_100Handlers()
        => _mediatR100Mediator.Publish(_mediatRNotification);

    [Benchmark(Description = "Publish_Mediator_1Handler")]
    public ValueTask Publish_Mediator_1Handler()
        => _mediator1Mediator.Publish(_mediatorNotification);

    [Benchmark(Description = "Publish_Mediator_10Handlers")]
    public ValueTask Publish_Mediator_10Handlers()
        => _mediator10Mediator.Publish(_mediatorNotification);

    [Benchmark(Description = "Publish_Mediator_50Handlers")]
    public ValueTask Publish_Mediator_50Handlers()
        => _mediator50Mediator.Publish(_mediatorNotification);

    [Benchmark(Description = "Publish_Mediator_100Handlers")]
    public ValueTask Publish_Mediator_100Handlers()
        => _mediator100Mediator.Publish(_mediatorNotification);
}
