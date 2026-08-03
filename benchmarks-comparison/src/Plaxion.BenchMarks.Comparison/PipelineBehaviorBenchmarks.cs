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
/// Pipeline behavior scale benchmarks across Plaxion, MediatR, and Mediator.
/// Each behavior-count tier uses an isolated <see cref="ServiceProvider"/>.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipelineBehaviorBenchmarks
{
    private ServiceProvider _plaxion0Provider = null!;
    private ServiceProvider _plaxion1Provider = null!;
    private ServiceProvider _plaxion5Provider = null!;
    private ServiceProvider _plaxion10Provider = null!;
    private ServiceProvider _plaxion20Provider = null!;

    private ServiceProvider _mediatR0Provider = null!;
    private ServiceProvider _mediatR1Provider = null!;
    private ServiceProvider _mediatR5Provider = null!;
    private ServiceProvider _mediatR10Provider = null!;
    private ServiceProvider _mediatR20Provider = null!;

    private ServiceProvider _mediator0Provider = null!;
    private ServiceProvider _mediator1Provider = null!;
    private ServiceProvider _mediator5Provider = null!;
    private ServiceProvider _mediator10Provider = null!;
    private ServiceProvider _mediator20Provider = null!;

    private ISender _plaxion0Sender = null!;
    private ISender _plaxion1Sender = null!;
    private ISender _plaxion5Sender = null!;
    private ISender _plaxion10Sender = null!;
    private ISender _plaxion20Sender = null!;

    private MediatR.IMediator _mediatR0Mediator = null!;
    private MediatR.IMediator _mediatR1Mediator = null!;
    private MediatR.IMediator _mediatR5Mediator = null!;
    private MediatR.IMediator _mediatR10Mediator = null!;
    private MediatR.IMediator _mediatR20Mediator = null!;

    private Mediator.IMediator _mediator0Mediator = null!;
    private Mediator.IMediator _mediator1Mediator = null!;
    private Mediator.IMediator _mediator5Mediator = null!;
    private Mediator.IMediator _mediator10Mediator = null!;
    private Mediator.IMediator _mediator20Mediator = null!;

    private PlaxionPipelineRequest _plaxionRequest = null!;
    private MediatRPipelineRequest _mediatRRequest = null!;
    private MediatorPipelineRequest _mediatorRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var payload = new ScenarioPayload("pipeline", "benchmark");
        _plaxionRequest = new PlaxionPipelineRequest(payload);
        _mediatRRequest = new MediatRPipelineRequest(payload);
        _mediatorRequest = new MediatorPipelineRequest(payload);

        _plaxion0Provider = PlaxionAdapterFactory.BuildServiceProviderForBehaviors(0);
        _plaxion1Provider = PlaxionAdapterFactory.BuildServiceProviderForBehaviors(1);
        _plaxion5Provider = PlaxionAdapterFactory.BuildServiceProviderForBehaviors(5);
        _plaxion10Provider = PlaxionAdapterFactory.BuildServiceProviderForBehaviors(10);
        _plaxion20Provider = PlaxionAdapterFactory.BuildServiceProviderForBehaviors(20);

        _plaxion0Sender = _plaxion0Provider.GetRequiredService<ISender>();
        _plaxion1Sender = _plaxion1Provider.GetRequiredService<ISender>();
        _plaxion5Sender = _plaxion5Provider.GetRequiredService<ISender>();
        _plaxion10Sender = _plaxion10Provider.GetRequiredService<ISender>();
        _plaxion20Sender = _plaxion20Provider.GetRequiredService<ISender>();

        _mediatR0Provider = MediatRAdapterFactory.BuildServiceProviderForBehaviors(0);
        _mediatR1Provider = MediatRAdapterFactory.BuildServiceProviderForBehaviors(1);
        _mediatR5Provider = MediatRAdapterFactory.BuildServiceProviderForBehaviors(5);
        _mediatR10Provider = MediatRAdapterFactory.BuildServiceProviderForBehaviors(10);
        _mediatR20Provider = MediatRAdapterFactory.BuildServiceProviderForBehaviors(20);

        _mediatR0Mediator = _mediatR0Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR1Mediator = _mediatR1Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR5Mediator = _mediatR5Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR10Mediator = _mediatR10Provider.GetRequiredService<MediatR.IMediator>();
        _mediatR20Mediator = _mediatR20Provider.GetRequiredService<MediatR.IMediator>();

        _mediator0Provider = MediatorAdapterFactory.BuildServiceProviderForBehaviors(0);
        _mediator1Provider = MediatorAdapterFactory.BuildServiceProviderForBehaviors(1);
        _mediator5Provider = MediatorAdapterFactory.BuildServiceProviderForBehaviors(5);
        _mediator10Provider = MediatorAdapterFactory.BuildServiceProviderForBehaviors(10);
        _mediator20Provider = MediatorAdapterFactory.BuildServiceProviderForBehaviors(20);

        _mediator0Mediator = _mediator0Provider.GetRequiredService<Mediator.IMediator>();
        _mediator1Mediator = _mediator1Provider.GetRequiredService<Mediator.IMediator>();
        _mediator5Mediator = _mediator5Provider.GetRequiredService<Mediator.IMediator>();
        _mediator10Mediator = _mediator10Provider.GetRequiredService<Mediator.IMediator>();
        _mediator20Mediator = _mediator20Provider.GetRequiredService<Mediator.IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plaxion0Provider.Dispose();
        _plaxion1Provider.Dispose();
        _plaxion5Provider.Dispose();
        _plaxion10Provider.Dispose();
        _plaxion20Provider.Dispose();

        _mediatR0Provider.Dispose();
        _mediatR1Provider.Dispose();
        _mediatR5Provider.Dispose();
        _mediatR10Provider.Dispose();
        _mediatR20Provider.Dispose();

        _mediator0Provider.Dispose();
        _mediator1Provider.Dispose();
        _mediator5Provider.Dispose();
        _mediator10Provider.Dispose();
        _mediator20Provider.Dispose();
    }

    [Benchmark(Description = "Send_Plaxion_0Behaviors")]
    public ValueTask<string> Send_Plaxion_0Behaviors()
        => _plaxion0Sender.Send(_plaxionRequest);

    [Benchmark(Description = "Send_Plaxion_1Behavior")]
    public ValueTask<string> Send_Plaxion_1Behavior()
        => _plaxion1Sender.Send(_plaxionRequest);

    [Benchmark(Description = "Send_Plaxion_5Behaviors")]
    public ValueTask<string> Send_Plaxion_5Behaviors()
        => _plaxion5Sender.Send(_plaxionRequest);

    [Benchmark(Description = "Send_Plaxion_10Behaviors")]
    public ValueTask<string> Send_Plaxion_10Behaviors()
        => _plaxion10Sender.Send(_plaxionRequest);

    [Benchmark(Description = "Send_Plaxion_20Behaviors")]
    public ValueTask<string> Send_Plaxion_20Behaviors()
        => _plaxion20Sender.Send(_plaxionRequest);

    [Benchmark(Description = "Send_MediatR_0Behaviors")]
    public Task<string> Send_MediatR_0Behaviors()
        => _mediatR0Mediator.Send(_mediatRRequest);

    [Benchmark(Description = "Send_MediatR_1Behavior")]
    public Task<string> Send_MediatR_1Behavior()
        => _mediatR1Mediator.Send(_mediatRRequest);

    [Benchmark(Description = "Send_MediatR_5Behaviors")]
    public Task<string> Send_MediatR_5Behaviors()
        => _mediatR5Mediator.Send(_mediatRRequest);

    [Benchmark(Description = "Send_MediatR_10Behaviors")]
    public Task<string> Send_MediatR_10Behaviors()
        => _mediatR10Mediator.Send(_mediatRRequest);

    [Benchmark(Description = "Send_MediatR_20Behaviors")]
    public Task<string> Send_MediatR_20Behaviors()
        => _mediatR20Mediator.Send(_mediatRRequest);

    [Benchmark(Description = "Send_Mediator_0Behaviors")]
    public ValueTask<string> Send_Mediator_0Behaviors()
        => _mediator0Mediator.Send(_mediatorRequest);

    [Benchmark(Description = "Send_Mediator_1Behavior")]
    public ValueTask<string> Send_Mediator_1Behavior()
        => _mediator1Mediator.Send(_mediatorRequest);

    [Benchmark(Description = "Send_Mediator_5Behaviors")]
    public ValueTask<string> Send_Mediator_5Behaviors()
        => _mediator5Mediator.Send(_mediatorRequest);

    [Benchmark(Description = "Send_Mediator_10Behaviors")]
    public ValueTask<string> Send_Mediator_10Behaviors()
        => _mediator10Mediator.Send(_mediatorRequest);

    [Benchmark(Description = "Send_Mediator_20Behaviors")]
    public ValueTask<string> Send_Mediator_20Behaviors()
        => _mediator20Mediator.Send(_mediatorRequest);
}
