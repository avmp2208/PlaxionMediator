using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Pipeline;

namespace PlaxionMediator.Pipeline.Tests;

/// <summary>
/// Serializes tests that mutate process-wide <see cref="PipelineExtensionRegistry"/> /
/// <see cref="PipelineObserverHub"/> state so parallel xUnit workers cannot interfere.
/// </summary>
[CollectionDefinition("PipelineStaticHubs", DisableParallelization = true)]
public sealed class PipelineStaticHubsCollectionDefinition;

[Collection("PipelineStaticHubs")]
public sealed class PipelineExtensionTests : IDisposable
{
    public PipelineExtensionTests()
    {
        PipelineExtensionRegistry.Clear();
        PipelineObserverHub.Clear();
    }

    public void Dispose()
    {
        PipelineExtensionRegistry.Clear();
        PipelineObserverHub.Clear();
    }

    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class PassThroughBehavior : IPipelineBehavior<Ping, string>
    {
        private readonly List<string> _log;
        private readonly string _name;

        public PassThroughBehavior(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _log.Add($"{_name}-before");
            string result = await next();
            _log.Add($"{_name}-after");
            return result;
        }
    }

    private sealed class LoggingExtension : IPipelineExtension
    {
        private readonly List<string> _log;
        private readonly string _name;

        public LoggingExtension(string name, int order, List<string> log)
        {
            _name = name;
            Order = order;
            _log = log;
        }

        public int Order { get; }

        public RequestHandlerDelegate<TResponse> Apply<TRequest, TResponse>(
            in PipelineExtensionContext context,
            RequestHandlerDelegate<TResponse> next)
            where TRequest : IRequest<TResponse>
        {
            List<string> log = _log;
            string name = _name;
            return async () =>
            {
                log.Add($"{name}-before");
                TResponse result = await next();
                log.Add($"{name}-after");
                return result;
            };
        }
    }

    [Fact]
    public void PipelineExtensionBuilder_Use_Adds_Extension_Types()
    {
        PipelineExtensionBuilder builder = new();
        builder.Use<LoggingExtension>().Use<PassThroughBehavior>();
        Assert.Equal(2, builder.Extensions.Count);
        Assert.Equal(typeof(LoggingExtension), builder.Extensions[0]);
        Assert.Equal(typeof(PassThroughBehavior), builder.Extensions[1]);
    }

    [Fact]
    public void PipelineExtensionBuilder_UseWhen_Filters()
    {
        PipelineExtensionBuilder builder = new();
        builder.UseWhen<LoggingExtension>(_ => false);
        builder.UseWhen<LoggingExtension>(_ => true);
        Assert.Single(builder.Extensions);
    }

    [Fact]
    public void PipelineExtensionRegistry_Orders_By_Order_Property()
    {
        List<string> log = [];
        PipelineExtensionRegistry.Register(new LoggingExtension("high", order: 10, log));
        PipelineExtensionRegistry.Register(new LoggingExtension("low", order: 1, log));

        Assert.Equal(2, PipelineExtensionRegistry.Current.Count);
        Assert.Equal(1, PipelineExtensionRegistry.Current[0].Order);
        Assert.Equal(10, PipelineExtensionRegistry.Current[1].Order);
    }

    [Fact]
    public async Task Extensions_Wrap_Behavior_Chain_In_Order()
    {
        List<string> log = [];
        PipelineExtensionRegistry.Register(new LoggingExtension("ext-outer", order: 0, log));
        PipelineExtensionRegistry.Register(new LoggingExtension("ext-inner", order: 1, log));

        IPipelineBehavior<Ping, string>[] behaviors =
        [
            new PassThroughBehavior("behavior", log),
        ];

        string result = await PipelineComposer.ExecuteAsync(
            new Ping("hi"),
            behaviors,
            (req, _) =>
            {
                log.Add("handler");
                return ValueTask.FromResult(req.Message);
            },
            CancellationToken.None);

        Assert.Equal("hi", result);
        Assert.Equal(
            [
                "ext-outer-before",
                "ext-inner-before",
                "behavior-before",
                "handler",
                "behavior-after",
                "ext-inner-after",
                "ext-outer-after",
            ],
            log);
    }

    [Fact]
    public async Task Existing_Behaviors_Unaffected_When_No_Extensions_Registered()
    {
        List<string> log = [];
        IPipelineBehavior<Ping, string>[] behaviors =
        [
            new PassThroughBehavior("b0", log),
            new PassThroughBehavior("b1", log),
        ];

        string result = await PipelineComposer.ExecuteAsync(
            new Ping("x"),
            behaviors,
            (req, _) =>
            {
                log.Add("handler");
                return ValueTask.FromResult(req.Message);
            },
            CancellationToken.None);

        Assert.Equal("x", result);
        Assert.Equal(["b0-before", "b1-before", "handler", "b1-after", "b0-after"], log);
        Assert.False(PipelineExtensionRegistry.HasExtensions);
    }

    [Fact]
    public void Registry_Clear_Removes_Extensions()
    {
        PipelineExtensionRegistry.Register(new LoggingExtension("x", 0, []));
        Assert.True(PipelineExtensionRegistry.HasExtensions);
        PipelineExtensionRegistry.Clear();
        Assert.False(PipelineExtensionRegistry.HasExtensions);
    }

    [Fact]
    public void Registry_Set_Replaces_Entire_Set()
    {
        List<string> log = [];
        PipelineExtensionRegistry.Register(new LoggingExtension("old", 0, log));
        PipelineExtensionRegistry.Set(
        [
            new LoggingExtension("a", 5, log),
            new LoggingExtension("b", 1, log),
        ]);

        Assert.Equal(2, PipelineExtensionRegistry.Current.Count);
        Assert.Equal(1, PipelineExtensionRegistry.Current[0].Order);
        Assert.Equal(5, PipelineExtensionRegistry.Current[1].Order);
    }
}

[Collection("PipelineStaticHubs")]
public sealed class PipelineObserverTests : IDisposable
{
    public PipelineObserverTests()
    {
        PipelineExtensionRegistry.Clear();
        PipelineObserverHub.Clear();
    }

    public void Dispose()
    {
        PipelineExtensionRegistry.Clear();
        PipelineObserverHub.Clear();
    }

    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class RecordingObserver : IPipelineObserver
    {
        public List<string> Events { get; } = [];
        public PipelineCallContext? LastContext { get; private set; }
        public Exception? LastFault { get; private set; }

        public void OnStarting(in PipelineCallContext context)
        {
            LastContext = context;
            Events.Add("start");
        }

        public void OnCompleted(in PipelineCallContext context)
        {
            LastContext = context;
            Events.Add("stop");
        }

        public void OnFaulted(in PipelineCallContext context, Exception exception)
        {
            LastContext = context;
            LastFault = exception;
            Events.Add("fault");
        }
    }

    private sealed class PassThroughBehavior : IPipelineBehavior<Ping, string>
    {
        private readonly List<string> _log;

        public PassThroughBehavior(List<string> log) => _log = log;

        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _log.Add("behavior");
            return await next();
        }
    }

    [Fact]
    public async Task Observer_NoOp_By_Default_And_Does_Not_Change_Result()
    {
        Assert.False(PipelineObserverHub.HasObservers);

        string result = await PipelineComposer.ExecuteAsync(
            new Ping("ok"),
            Array.Empty<IPipelineBehavior<Ping, string>>(),
            (req, _) => ValueTask.FromResult(req.Message),
            CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Observer_Ordering_Is_Start_Then_Behaviors_Then_Handler_Then_Stop()
    {
        RecordingObserver observer = new();
        List<string> log = [];
        PipelineObserverHub.Register(observer);

        // Interleave pipeline log with observer via a behavior that records.
        string result = await PipelineComposer.ExecuteAsync(
            new Ping("hi"),
            [new PassThroughBehavior(log)],
            (req, _) =>
            {
                log.Add("handler");
                return ValueTask.FromResult(req.Message);
            },
            CancellationToken.None);

        Assert.Equal("hi", result);
        Assert.Equal(["start", "stop"], observer.Events);
        Assert.Equal(["behavior", "handler"], log);
        Assert.NotNull(observer.LastContext);
        Assert.Equal(nameof(Ping), observer.LastContext!.Value.RequestTypeName);
        Assert.Equal(1, observer.LastContext.Value.BehaviorCount);
        Assert.Equal(typeof(string), observer.LastContext.Value.ResponseType);
    }

    [Fact]
    public async Task Observer_Receives_Fault_On_Behavior_Exception()
    {
        RecordingObserver observer = new();
        PipelineObserverHub.Register(observer);

        var throwing = new ThrowingBehavior();

        await Assert.ThrowsAsync<PipelineExecutionException>(async () =>
            await PipelineComposer.ExecuteAsync(
                new Ping("x"),
                [throwing],
                (req, _) => ValueTask.FromResult("ok"),
                CancellationToken.None));

        Assert.Equal(["start", "fault"], observer.Events);
        Assert.IsType<PipelineExecutionException>(observer.LastFault);
        Assert.Equal(nameof(Ping), ((PipelineExecutionException)observer.LastFault!).RequestTypeName);
    }

    [Fact]
    public async Task Observer_Receives_Fault_On_Handler_Exception_With_Unwrapped_Type()
    {
        RecordingObserver observer = new();
        PipelineObserverHub.Register(observer);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PipelineComposer.ExecuteAsync(
                new Ping("x"),
                Array.Empty<IPipelineBehavior<Ping, string>>(),
                (req, _) => throw new InvalidOperationException("handler boom"),
                CancellationToken.None));

        Assert.Equal("handler boom", thrown.Message);
        Assert.Equal(["start", "fault"], observer.Events);
        Assert.IsType<InvalidOperationException>(observer.LastFault);
    }

    [Fact]
    public async Task Observer_Works_On_Deep_Pooled_Runner_Path()
    {
        RecordingObserver observer = new();
        PipelineObserverHub.Register(observer);

        List<string> log = [];
        IPipelineBehavior<Ping, string>[] behaviors = Enumerable.Range(0, 6)
            .Select(_ => (IPipelineBehavior<Ping, string>)new PassThroughBehavior(log))
            .ToArray();

        string result = await PipelineComposer.ExecuteAsync(
            new Ping("deep"),
            behaviors,
            (req, _) => ValueTask.FromResult(req.Message),
            CancellationToken.None);

        Assert.Equal("deep", result);
        Assert.Equal(["start", "stop"], observer.Events);
        Assert.Equal(6, observer.LastContext!.Value.BehaviorCount);
    }

    [Fact]
    public void Unregister_Removes_Observer()
    {
        RecordingObserver observer = new();
        PipelineObserverHub.Register(observer);
        Assert.True(PipelineObserverHub.HasObservers);
        PipelineObserverHub.Unregister(observer);
        Assert.False(PipelineObserverHub.HasObservers);
    }

    private sealed class ThrowingBehavior : IPipelineBehavior<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }
}
