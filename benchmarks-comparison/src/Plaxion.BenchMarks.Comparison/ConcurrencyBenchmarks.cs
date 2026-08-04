using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using Comparison.MediatorAdapter;
using Comparison.MediatRAdapter;
using Comparison.PlaxionAdapter;
using Comparison.Shared;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Core;

namespace Plaxion.BenchMarks.Comparison;

/// <summary>
/// Concurrent request dispatch benchmarks across Plaxion, MediatR, and Mediator.
/// One shared <see cref="ServiceProvider"/> per framework is reused across concurrency levels.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ConcurrencyBenchmarks
{
    private ServiceProvider _plaxionProvider = null!;
    private ServiceProvider _mediatRProvider = null!;
    private ServiceProvider _mediatorProvider = null!;

    private ISender _plaxionSender = null!;
    private MediatR.IMediator _mediatRMediator = null!;
    private Mediator.IMediator _mediatorMediator = null!;

    private PlaxionPipelineRequest _plaxionRequest = null!;
    private MediatRPipelineRequest _mediatRRequest = null!;
    private MediatorPipelineRequest _mediatorRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var payload = new ScenarioPayload("concurrency", "benchmark");
        _plaxionRequest = new PlaxionPipelineRequest(payload);
        _mediatRRequest = new MediatRPipelineRequest(payload);
        _mediatorRequest = new MediatorPipelineRequest(payload);

        _plaxionProvider = PlaxionAdapterFactory.BuildServiceProviderForConcurrency();
        _plaxionSender = _plaxionProvider.GetRequiredService<ISender>();

        _mediatRProvider = MediatRAdapterFactory.BuildServiceProviderForConcurrency();
        _mediatRMediator = _mediatRProvider.GetRequiredService<MediatR.IMediator>();

        _mediatorProvider = MediatorAdapterFactory.BuildServiceProviderForConcurrency();
        _mediatorMediator = _mediatorProvider.GetRequiredService<Mediator.IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _plaxionProvider.Dispose();
        _mediatRProvider.Dispose();
        _mediatorProvider.Dispose();
    }

    [Benchmark(Description = "Concurrent_Plaxion_1", Baseline = true)]
    public Task Concurrent_Plaxion_1() => RunPlaxionConcurrent(1);

    [Benchmark(Description = "Concurrent_Plaxion_8")]
    public Task Concurrent_Plaxion_8() => RunPlaxionConcurrent(8);

    [Benchmark(Description = "Concurrent_Plaxion_32")]
    public Task Concurrent_Plaxion_32() => RunPlaxionConcurrent(32);

    [Benchmark(Description = "Concurrent_Plaxion_128")]
    public Task Concurrent_Plaxion_128() => RunPlaxionConcurrent(128);

    [Benchmark(Description = "Concurrent_MediatR_1")]
    public Task Concurrent_MediatR_1() => RunMediatRConcurrent(1);

    [Benchmark(Description = "Concurrent_MediatR_8")]
    public Task Concurrent_MediatR_8() => RunMediatRConcurrent(8);

    [Benchmark(Description = "Concurrent_MediatR_32")]
    public Task Concurrent_MediatR_32() => RunMediatRConcurrent(32);

    [Benchmark(Description = "Concurrent_MediatR_128")]
    public Task Concurrent_MediatR_128() => RunMediatRConcurrent(128);

    [Benchmark(Description = "Concurrent_Mediator_1")]
    public Task Concurrent_Mediator_1() => RunMediatorConcurrent(1);

    [Benchmark(Description = "Concurrent_Mediator_8")]
    public Task Concurrent_Mediator_8() => RunMediatorConcurrent(8);

    [Benchmark(Description = "Concurrent_Mediator_32")]
    public Task Concurrent_Mediator_32() => RunMediatorConcurrent(32);

    [Benchmark(Description = "Concurrent_Mediator_128")]
    public Task Concurrent_Mediator_128() => RunMediatorConcurrent(128);

    private Task RunPlaxionConcurrent(int concurrency)
    {
        var tasks = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = _plaxionSender.Send(_plaxionRequest).AsTask();
        }

        return Task.WhenAll(tasks);
    }

    private Task RunMediatRConcurrent(int concurrency)
    {
        var tasks = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = _mediatRMediator.Send(_mediatRRequest);
        }

        return Task.WhenAll(tasks);
    }

    private Task RunMediatorConcurrent(int concurrency)
    {
        var tasks = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = _mediatorMediator.Send(_mediatorRequest).AsTask();
        }

        return Task.WhenAll(tasks);
    }
}
