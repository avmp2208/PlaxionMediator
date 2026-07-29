using Conduit.Abstractions;
using Conduit.Core;
using Conduit.Pipeline;

namespace Conduit.Pipeline.Tests;

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
}
