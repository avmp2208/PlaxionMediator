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
/// Type-variety benchmarks that dispatch 50 distinct request/handler pairs once per iteration
/// for Plaxion, MediatR, and Mediator.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TypeVarietyBenchmarks
{
    private ServiceProvider _plaxionProvider = null!;
    private ServiceProvider _mediatRProvider = null!;
    private ServiceProvider _mediatorProvider = null!;

    private ISender _plaxionSender = null!;
    private MediatR.IMediator _mediatRMediator = null!;
    private Mediator.IMediator _mediatorMediator = null!;

    private PlaxionMediator.Abstractions.IRequest<string>[] _plaxionRequests = null!;
    private MediatR.IRequest<string>[] _mediatRRequests = null!;
    private Mediator.IRequest<string>[] _mediatorRequests = null!;

    [GlobalSetup]
    public void Setup()
    {
        var payload = new ScenarioPayload("type-variety", "benchmark");

        _plaxionProvider = PlaxionAdapterFactory.BuildServiceProviderForTypeVariety();
        _plaxionSender = _plaxionProvider.GetRequiredService<ISender>();
        _plaxionRequests = PlaxionTypeVarietyRegistrar.GetRequests(payload);

        _mediatRProvider = MediatRAdapterFactory.BuildServiceProviderForTypeVariety();
        _mediatRMediator = _mediatRProvider.GetRequiredService<MediatR.IMediator>();
        _mediatRRequests = MediatRTypeVarietyRegistrar.GetRequests(payload);

        _mediatorProvider = MediatorAdapterFactory.BuildServiceProviderForTypeVariety();
        _mediatorMediator = _mediatorProvider.GetRequiredService<Mediator.IMediator>();
        _mediatorRequests = MediatorTypeVarietyRegistrar.GetRequests(payload);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plaxionProvider.Dispose();
        _mediatRProvider.Dispose();
        _mediatorProvider.Dispose();
    }

    [Benchmark(Description = "Dispatch_Plaxion_50Types")]
    public async ValueTask Dispatch_Plaxion_50Types()
    {
        for (int i = 0; i < _plaxionRequests.Length; i++)
        {
            await _plaxionSender.Send(_plaxionRequests[i]);
        }
    }

    [Benchmark(Description = "Dispatch_MediatR_50Types")]
    public async Task Dispatch_MediatR_50Types()
    {
        for (int i = 0; i < _mediatRRequests.Length; i++)
        {
            await _mediatRMediator.Send(_mediatRRequests[i]);
        }
    }

    [Benchmark(Description = "Dispatch_Mediator_50Types")]
    public async ValueTask Dispatch_Mediator_50Types()
    {
        for (int i = 0; i < _mediatorRequests.Length; i++)
        {
            await _mediatorMediator.Send(_mediatorRequests[i]);
        }
    }
}
