using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Pipeline;

namespace PlaxionMediator.Pipeline.Tests;

public sealed class PipelineComposerTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class OuterBehavior : IPipelineBehavior<Ping, string>
    {
        public List<string> Log { get; }

        public OuterBehavior(List<string> log) => Log = log;

        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            Log.Add("outer-before");
            string result = await next();
            Log.Add("outer-after");
            return "O(" + result + ")";
        }
    }

    private sealed class InnerBehavior : IPipelineBehavior<Ping, string>
    {
        public List<string> Log { get; }

        public InnerBehavior(List<string> log) => Log = log;

        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            Log.Add("inner-before");
            string result = await next();
            Log.Add("inner-after");
            return "I(" + result + ")";
        }
    }

    [Fact]
    public async Task Compose_Runs_Behaviors_In_Order_Around_Handler()
    {
        List<string> log = [];
        OuterBehavior outer = new(log);
        InnerBehavior inner = new(log);
        Ping request = new("hi");

        string result = await PipelineComposer.ExecuteAsync(
            request,
            new IPipelineBehavior<Ping, string>[] { outer, inner },
            (req, _) =>
            {
                log.Add("handler");
                return ValueTask.FromResult(req.Message);
            },
            CancellationToken.None);

        Assert.Equal("O(I(hi))", result);
        Assert.Equal(["outer-before", "inner-before", "handler", "inner-after", "outer-after"], log);
    }

    [Fact]
    public async Task Pipeline_Propagates_CancellationToken()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        bool behaviorCalled = false;
        var behavior = new TokenCheckingBehavior(() => behaviorCalled = true);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await PipelineComposer.ExecuteAsync(
                new Ping("x"),
                [behavior],
                (req, ct) => ValueTask.FromResult("ok"),
                cts.Token));
        
        Assert.True(behaviorCalled);
    }

    private sealed class TokenCheckingBehavior : IPipelineBehavior<Ping, string>
    {
        private readonly Action _onCalled;
        public TokenCheckingBehavior(Action onCalled) => _onCalled = onCalled;

        public ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _onCalled();
            cancellationToken.ThrowIfCancellationRequested();
            return next();
        }
    }

    [Fact]
    public async Task Pipeline_Wraps_Exception_In_PipelineExecutionException()
    {
        var behavior = new ThrowingBehavior();
        
        var ex = await Assert.ThrowsAsync<PipelineExecutionException>(async () =>
            await PipelineComposer.ExecuteAsync(
                new Ping("x"),
                [behavior],
                (req, ct) => ValueTask.FromResult("ok"),
                CancellationToken.None));

        Assert.Equal(nameof(ThrowingBehavior), ex.StageName);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    private sealed class ThrowingBehavior : IPipelineBehavior<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Behavior boom");
        }
    }

    [Fact]
    public async Task Compose_With_No_Behaviors_Calls_Handler()
    {
        string result = await PipelineComposer.ExecuteAsync(
            new Ping("x"),
            Array.Empty<IPipelineBehavior<Ping, string>>(),
            (req, _) => ValueTask.FromResult(req.Message.ToUpperInvariant()),
            CancellationToken.None);

        Assert.Equal("X", result);
    }

    [Fact]
    public void PipelineBuilder_Use_Adds_Behavior_Types()
    {
        PipelineBuilder builder = new();
        builder.Use<OuterBehavior>().Use<InnerBehavior>();
        Assert.Equal(2, builder.Behaviors.Count);
        Assert.Equal(typeof(OuterBehavior), builder.Behaviors[0]);
        Assert.Equal(typeof(InnerBehavior), builder.Behaviors[1]);
    }

    [Fact]
    public void PipelineBuilder_UseWhen_Filters()
    {
        PipelineBuilder builder = new();
        builder.UseWhen<OuterBehavior>(_ => false);
        builder.UseWhen<InnerBehavior>(_ => true);
        Assert.Single(builder.Behaviors);
        Assert.Equal(typeof(InnerBehavior), builder.Behaviors[0]);
    }

    // A named, order-tracking behavior used to build chains of arbitrary depth. Mirrors the
    // v0.4.2 guidance chain: Validation -> Caching -> CircuitBreaker -> Retry -> Handler.
    private sealed class NamedBehavior : IPipelineBehavior<Ping, string>
    {
        private readonly string _name;
        private readonly List<string> _log;

        public NamedBehavior(string name, List<string> log)
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

    private sealed class ThrowingNamedBehavior : IPipelineBehavior<Ping, string>
    {
        private readonly string _name;

        public ThrowingNamedBehavior(string name) => _name = name;

        public ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"{_name} boom");
        }
    }

    private static IPipelineBehavior<Ping, string>[] BuildChain(int count, List<string> log) =>
        Enumerable.Range(0, count)
            .Select(i => (IPipelineBehavior<Ping, string>)new NamedBehavior($"b{i}", log))
            .ToArray();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task ExecuteAsync_Runs_Behaviors_In_Registration_Order_For_All_Depths(int count)
    {
        // Covers both the field-staged PipelineExecutor path (<=5 behaviors, see
        // PipelineComposer's `behaviors.Count <= 5` branch) and the pooled PipelineRunner
        // fallback path (>5 behaviors) with a single assertion shape.
        List<string> log = [];
        IPipelineBehavior<Ping, string>[] behaviors = BuildChain(count, log);
        Ping request = new("hi");

        string result = await PipelineComposer.ExecuteAsync(
            request,
            behaviors,
            (req, _) =>
            {
                log.Add("handler");
                return ValueTask.FromResult(req.Message);
            },
            CancellationToken.None);

        Assert.Equal("hi", result);

        List<string> expected = [];
        for (int i = 0; i < count; i++)
        {
            expected.Add($"b{i}-before");
        }

        expected.Add("handler");

        for (int i = count - 1; i >= 0; i--)
        {
            expected.Add($"b{i}-after");
        }

        Assert.Equal(expected, log);
    }

    [Fact]
    public async Task FiveBehaviorChain_Simulating_Standard_Ordering_Executes_And_Delegates_To_Handler_Last()
    {
        // 5 behaviors: still handled by the field-staged PipelineExecutor path
        // (Count <= 5), simulating Validation -> Caching -> CircuitBreaker -> Retry -> custom.
        List<string> log = [];
        string[] names = ["Validation", "Caching", "CircuitBreaker", "Retry", "Custom"];
        IPipelineBehavior<Ping, string>[] behaviors =
            names.Select(n => (IPipelineBehavior<Ping, string>)new NamedBehavior(n, log)).ToArray();

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
                "Validation-before", "Caching-before", "CircuitBreaker-before", "Retry-before", "Custom-before",
                "handler",
                "Custom-after", "Retry-after", "CircuitBreaker-after", "Caching-after", "Validation-after",
            ],
            log);
    }

    [Fact]
    public async Task SixBehaviorChain_Falls_Back_To_Pooled_Runner_And_Executes_In_Order()
    {
        // 6 behaviors: exceeds the field-staged executor's max depth of 5, so this must be
        // serviced by the pooled PipelineRunner fallback path.
        List<string> log = [];
        string[] names = ["Validation", "Caching", "CircuitBreaker", "Retry", "Custom1", "Custom2"];
        IPipelineBehavior<Ping, string>[] behaviors =
            names.Select(n => (IPipelineBehavior<Ping, string>)new NamedBehavior(n, log)).ToArray();

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
                "Validation-before", "Caching-before", "CircuitBreaker-before", "Retry-before",
                "Custom1-before", "Custom2-before",
                "handler",
                "Custom2-after", "Custom1-after", "Retry-after", "CircuitBreaker-after", "Caching-after",
                "Validation-after",
            ],
            log);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    public async Task Behavior_Exception_Propagates_Wrapped_As_PipelineExecutionException_At_Any_Depth(int count)
    {
        // Verifies HandlerFaultException unwrapping / behavior-fault wrapping still behaves
        // correctly for both the staged executor path (<=5) and the pooled fallback (>5).
        List<string> log = [];
        List<IPipelineBehavior<Ping, string>> behaviors = [];
        for (int i = 0; i < count - 1; i++)
        {
            behaviors.Add(new NamedBehavior($"b{i}", log));
        }

        behaviors.Add(new ThrowingNamedBehavior("last"));

        var ex = await Assert.ThrowsAsync<PipelineExecutionException>(async () =>
            await PipelineComposer.ExecuteAsync(
                new Ping("x"),
                behaviors,
                (req, _) => ValueTask.FromResult("ok"),
                CancellationToken.None));

        Assert.Equal(nameof(ThrowingNamedBehavior), ex.StageName);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    public async Task Handler_Exception_Propagates_Unwrapped_At_Any_Depth(int count)
    {
        // A handler fault must surface as the original exception type (not wrapped), for both
        // the staged executor path (<=5) and the pooled fallback path (>5).
        List<string> log = [];
        IPipelineBehavior<Ping, string>[] behaviors = BuildChain(count, log);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await PipelineComposer.ExecuteAsync(
                new Ping("x"),
                behaviors,
                (req, _) => throw new InvalidOperationException("handler boom"),
                CancellationToken.None));

        Assert.Equal("handler boom", thrown.Message);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public async Task Allocation_Sanity_StagedAndPooled_Paths_Complete_Without_Excessive_Allocation(int count)
    {
        // Pragmatic allocation-sanity check (not a full BenchmarkDotNet harness): warms up both
        // paths, then asserts a single subsequent run doesn't allocate an unreasonable amount.
        List<string> log = [];
        IPipelineBehavior<Ping, string>[] behaviors = BuildChain(count, log);
        Ping request = new("hi");

        Func<Ping, CancellationToken, ValueTask<string>> handler = (req, _) => ValueTask.FromResult(req.Message);

        // Warm up pools/JIT.
        for (int i = 0; i < 50; i++)
        {
            log.Clear();
            await PipelineComposer.ExecuteAsync(request, behaviors, handler, CancellationToken.None);
        }

        log.Clear();
        long before = GC.GetAllocatedBytesForCurrentThread();
        string result = await PipelineComposer.ExecuteAsync(request, behaviors, handler, CancellationToken.None);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal("hi", result);
        Assert.True(after - before < 20_000, $"Expected allocation delta below threshold, was {after - before} bytes.");
    }
}
