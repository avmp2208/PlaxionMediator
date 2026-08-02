using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator;

namespace PlaxionMediator.Benchmarks;

/// <summary>
/// Send dispatch benchmarks, each isolated in its own <see cref="ServiceProvider"/> so pipeline
/// behavior count is the only variable between scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class SendBenchmarks
{
    private ServiceProvider _noPipelineProvider = null!;
    private ServiceProvider _oneBehaviorProvider = null!;
    private ServiceProvider _fiveBehaviorsProvider = null!;
    private ISender _noPipelineSender = null!;
    private ISender _oneBehaviorSender = null!;
    private ISender _fiveBehaviorsSender = null!;
    private Ping _ping = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ping = new Ping("benchmark");

        ServiceCollection noPipelineServices = new();
        noPipelineServices.AddPlaxionMediator();
        _noPipelineProvider = noPipelineServices.BuildServiceProvider();
        _noPipelineSender = _noPipelineProvider.GetRequiredService<ISender>();

        ServiceCollection oneBehaviorServices = new();
        oneBehaviorServices.AddPlaxionMediator();
        oneBehaviorServices.AddSingleton(typeof(IPipelineBehavior<,>), typeof(NoOpBehavior<,>));
        _oneBehaviorProvider = oneBehaviorServices.BuildServiceProvider();
        _oneBehaviorSender = _oneBehaviorProvider.GetRequiredService<ISender>();

        ServiceCollection fiveBehaviorsServices = new();
        fiveBehaviorsServices.AddPlaxionMediator();
        fiveBehaviorsServices.AddSingleton(typeof(IPipelineBehavior<,>), typeof(Behavior1<,>));
        fiveBehaviorsServices.AddSingleton(typeof(IPipelineBehavior<,>), typeof(Behavior2<,>));
        fiveBehaviorsServices.AddSingleton(typeof(IPipelineBehavior<,>), typeof(Behavior3<,>));
        fiveBehaviorsServices.AddSingleton(typeof(IPipelineBehavior<,>), typeof(Behavior4<,>));
        fiveBehaviorsServices.AddSingleton(typeof(IPipelineBehavior<,>), typeof(Behavior5<,>));
        _fiveBehaviorsProvider = fiveBehaviorsServices.BuildServiceProvider();
        _fiveBehaviorsSender = _fiveBehaviorsProvider.GetRequiredService<ISender>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _noPipelineProvider.Dispose();
        _oneBehaviorProvider.Dispose();
        _fiveBehaviorsProvider.Dispose();
    }

    /// <summary>No <see cref="IPipelineBehavior{TRequest,TResponse}"/> registered.</summary>
    [Benchmark(Description = "Send_NoPipeline")]
    public ValueTask<string> Send_NoPipeline()
        => _noPipelineSender.Send(_ping);

    /// <summary>Exactly one <see cref="NoOpBehavior{TRequest,TResponse}"/> registered.</summary>
    [Benchmark(Description = "Send_OneBehavior")]
    public ValueTask<string> Send_OneBehavior()
        => _oneBehaviorSender.Send(_ping);

    /// <summary>Five distinct no-op pipeline behaviors registered.</summary>
    [Benchmark(Description = "Send_FiveBehaviors")]
    public ValueTask<string> Send_FiveBehaviors()
        => _fiveBehaviorsSender.Send(_ping);
}

/// <summary>
/// Notification fan-out benchmarks, each isolated in its own <see cref="ServiceProvider"/> with
/// exactly the number of <see cref="INotificationHandler{TNotification}"/> implementations its
/// scenario name implies.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class NotificationBenchmarks
{
    private ServiceProvider _oneHandlerProvider = null!;
    private ServiceProvider _fiveHandlersProvider = null!;
    private ServiceProvider _tenHandlersProvider = null!;
    private IPublisher _oneHandlerPublisher = null!;
    private IPublisher _fiveHandlersPublisher = null!;
    private IPublisher _tenHandlersPublisher = null!;
    private OneHandlerNotification _oneHandlerNotification = null!;
    private FiveHandlersNotification _fiveHandlersNotification = null!;
    private TenHandlersNotification _tenHandlersNotification = null!;

    [GlobalSetup]
    public void Setup()
    {
        _oneHandlerNotification = new OneHandlerNotification("id");
        _fiveHandlersNotification = new FiveHandlersNotification("id");
        _tenHandlersNotification = new TenHandlersNotification("id");

        ServiceCollection oneHandlerServices = new();
        oneHandlerServices.AddPlaxionMediator();
        _oneHandlerProvider = oneHandlerServices.BuildServiceProvider();
        _oneHandlerPublisher = _oneHandlerProvider.GetRequiredService<IPublisher>();

        ServiceCollection fiveHandlersServices = new();
        fiveHandlersServices.AddPlaxionMediator();
        _fiveHandlersProvider = fiveHandlersServices.BuildServiceProvider();
        _fiveHandlersPublisher = _fiveHandlersProvider.GetRequiredService<IPublisher>();

        ServiceCollection tenHandlersServices = new();
        tenHandlersServices.AddPlaxionMediator();
        _tenHandlersProvider = tenHandlersServices.BuildServiceProvider();
        _tenHandlersPublisher = _tenHandlersProvider.GetRequiredService<IPublisher>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _oneHandlerProvider.Dispose();
        _fiveHandlersProvider.Dispose();
        _tenHandlersProvider.Dispose();
    }

    /// <summary>Exactly one notification handler registered.</summary>
    [Benchmark(Description = "Publish_OneHandler")]
    public ValueTask Publish_OneHandler()
        => _oneHandlerPublisher.Publish(_oneHandlerNotification);

    /// <summary>Exactly five notification handlers registered.</summary>
    [Benchmark(Description = "Publish_FiveHandlers")]
    public ValueTask Publish_FiveHandlers()
        => _fiveHandlersPublisher.Publish(_fiveHandlersNotification);

    /// <summary>Exactly ten notification handlers registered.</summary>
    [Benchmark(Description = "Publish_TenHandlers")]
    public ValueTask Publish_TenHandlers()
        => _tenHandlersPublisher.Publish(_tenHandlersNotification);
}

/// <summary>
/// Streaming dispatch benchmark, isolated in its own <see cref="ServiceProvider"/>.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class StreamBenchmarks
{
    private ServiceProvider _provider = null!;
    private ISender _sender = null!;
    private NumberStream _streamRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediator();
        _provider = services.BuildServiceProvider();
        _sender = _provider.GetRequiredService<ISender>();
        _streamRequest = new NumberStream(1000);
    }

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    /// <summary>Streams 1000 items end to end through <see cref="ISender.CreateStream{TResponse}"/>.</summary>
    [Benchmark(Description = "Stream_1000Items")]
    public async Task<int> Stream_1000Items()
    {
        int count = 0;
        await foreach (int _ in _sender.CreateStream(_streamRequest))
        {
            count++;
        }

        return count;
    }
}

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult("Pong:" + request.Message);
}

public sealed record OneHandlerNotification(string Id) : INotification;

public sealed class OneHandlerNotificationHandlerA : INotificationHandler<OneHandlerNotification>
{
    public ValueTask Handle(OneHandlerNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed record FiveHandlersNotification(string Id) : INotification;

public sealed class FiveHandlersNotificationHandlerA : INotificationHandler<FiveHandlersNotification>
{
    public ValueTask Handle(FiveHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class FiveHandlersNotificationHandlerB : INotificationHandler<FiveHandlersNotification>
{
    public ValueTask Handle(FiveHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class FiveHandlersNotificationHandlerC : INotificationHandler<FiveHandlersNotification>
{
    public ValueTask Handle(FiveHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class FiveHandlersNotificationHandlerD : INotificationHandler<FiveHandlersNotification>
{
    public ValueTask Handle(FiveHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class FiveHandlersNotificationHandlerE : INotificationHandler<FiveHandlersNotification>
{
    public ValueTask Handle(FiveHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed record TenHandlersNotification(string Id) : INotification;

public sealed class TenHandlersNotificationHandlerA : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerB : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerC : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerD : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerE : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerF : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerG : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerH : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerI : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class TenHandlersNotificationHandlerJ : INotificationHandler<TenHandlersNotification>
{
    public ValueTask Handle(TenHandlersNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed record NumberStream(int Count) : IStreamRequest<int>;

public sealed class NumberStreamHandler : IStreamRequestHandler<NumberStream, int>
{
    public async IAsyncEnumerable<int> Handle(
        NumberStream request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Yield();
        }
    }
}

public sealed class NoOpBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}

public sealed class Behavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}

public sealed class Behavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}

public sealed class Behavior3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}

public sealed class Behavior4<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}

public sealed class Behavior5<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => next();
}
