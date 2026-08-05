using Polly;
using Polly.CircuitBreaker;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Retry.Tests;

public sealed class CircuitBreakerBehaviorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed record GuardedPing(string Message) : IRequest<string>, ICircuitBreakerRequest;

    private sealed class ThrowingPolicyProvider<TRequest> : ICircuitBreakerPolicyProvider<TRequest>
    {
        public ResiliencePipeline GetPipeline(TRequest request)
            => throw new InvalidOperationException("Should not be called for non-opt-in requests.");
    }

    private sealed class FixedPolicyProvider<TRequest> : ICircuitBreakerPolicyProvider<TRequest>
    {
        private readonly ResiliencePipeline _pipeline;

        public FixedPolicyProvider(ResiliencePipeline pipeline) => _pipeline = pipeline;

        public ResiliencePipeline GetPipeline(TRequest request) => _pipeline;
    }

    private static ResiliencePipeline BuildPipeline(
        double failureRatio = 0.5,
        int minimumThroughput = 2,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null)
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureRatio,
                MinimumThroughput = minimumThroughput,
                SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30),
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30),
            })
            .Build();
    }

    [Fact]
    public void Constructor_Throws_On_Null_PolicyProvider()
    {
        Assert.Throws<ArgumentNullException>(() => new CircuitBreakerBehavior<Ping, string>(null!));
    }

    [Fact]
    public async Task Handle_Throws_On_Null_Request_Or_Next()
    {
        CircuitBreakerBehavior<Ping, string> behavior =
            new(new ThrowingPolicyProvider<Ping>());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(new Ping("x"), null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task NonOptIn_Request_Is_NoOp_And_Bypasses_PolicyProvider()
    {
        CircuitBreakerBehavior<Ping, string> behavior =
            new(new ThrowingPolicyProvider<Ping>());
        int calls = 0;

        string result = await behavior.Handle(
            new Ping("x"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task OptIn_Request_Succeeds_Through_Pipeline_When_Closed()
    {
        ResiliencePipeline pipeline = BuildPipeline();
        CircuitBreakerBehavior<GuardedPing, string> behavior =
            new(new FixedPolicyProvider<GuardedPing>(pipeline));
        int calls = 0;

        string result = await behavior.Handle(
            new GuardedPing("x"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Enough_Failures_Trip_The_Breaker_And_Fail_Fast_Without_Calling_Next()
    {
        ResiliencePipeline pipeline = BuildPipeline(failureRatio: 0.5, minimumThroughput: 2);
        CircuitBreakerBehavior<GuardedPing, string> behavior =
            new(new FixedPolicyProvider<GuardedPing>(pipeline));
        int calls = 0;

        ValueTask<string> Fail() => throw new InvalidOperationException("boom");

        // Drive enough failing calls to exceed MinimumThroughput/FailureRatio and open the circuit.
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new GuardedPing("fail"),
                    () =>
                    {
                        calls++;
                        return Fail();
                    },
                    CancellationToken.None).AsTask());
        }

        int callsBeforeOpen = calls;

        // Circuit should now be open; the handler must not be invoked again.
        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            behavior.Handle(
                new GuardedPing("should-fail-fast"),
                () =>
                {
                    calls++;
                    return ValueTask.FromResult("unreachable");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(callsBeforeOpen, calls);
    }

    [Fact]
    public async Task Circuit_Recovers_To_Closed_After_BreakDuration_And_Successful_Probe()
    {
        ResiliencePipeline pipeline = BuildPipeline(
            failureRatio: 0.5,
            minimumThroughput: 2,
            breakDuration: TimeSpan.FromMilliseconds(500));
        CircuitBreakerBehavior<GuardedPing, string> behavior =
            new(new FixedPolicyProvider<GuardedPing>(pipeline));
        int calls = 0;

        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(
                    new GuardedPing("fail"),
                    () =>
                    {
                        calls++;
                        throw new InvalidOperationException("boom");
                    },
                    CancellationToken.None).AsTask());
        }

        await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            behavior.Handle(
                new GuardedPing("open"),
                () => ValueTask.FromResult("unreachable"),
                CancellationToken.None).AsTask());

        await Task.Delay(TimeSpan.FromMilliseconds(700));

        // Half-open probe succeeds, closing the circuit again.
        string result = await behavior.Handle(
            new GuardedPing("probe"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("recovered");
            },
            CancellationToken.None);

        Assert.Equal("recovered", result);
    }

    [Fact]
    public async Task Cancellation_Propagates_Without_Being_Swallowed()
    {
        ResiliencePipeline pipeline = BuildPipeline();
        CircuitBreakerBehavior<GuardedPing, string> behavior =
            new(new FixedPolicyProvider<GuardedPing>(pipeline));
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new GuardedPing("x"),
                () => ValueTask.FromResult("nope"),
                cts.Token).AsTask());
    }

    private sealed record RetryableGuardedPing(string Message) : IRequest<string>, ICircuitBreakerRequest, IRetryableRequest
    {
        public int? MaxRetryAttempts => 5;
        public TimeSpan? BaseDelay => TimeSpan.Zero;
    }

    [Fact]
    public async Task Open_Circuit_Registered_Outside_Retry_Prevents_Retry_And_Handler_From_Running()
    {
        ResiliencePipeline pipeline = BuildPipeline(failureRatio: 0.5, minimumThroughput: 2);
        CircuitBreakerBehavior<RetryableGuardedPing, string> circuitBreaker =
            new(new FixedPolicyProvider<RetryableGuardedPing>(pipeline));
        RetryBehavior<RetryableGuardedPing, string> retry =
            new(new PlaxionMediatorRetryOptions { MaxRetryAttempts = 5, BaseDelay = TimeSpan.Zero }, new NoDelayProvider());

        int handlerCalls = 0;
        RequestHandlerDelegate<string> handler = () =>
        {
            handlerCalls++;
            throw new InvalidOperationException("boom");
        };

        // Compose: CircuitBreaker (outer) -> Retry (inner) -> handler.
        RequestHandlerDelegate<string> pipelineDelegate =
            () => circuitBreaker.Handle(
                new RetryableGuardedPing("x"),
                () => retry.Handle(new RetryableGuardedPing("x"), handler, CancellationToken.None),
                CancellationToken.None);

        // Trip the circuit: each call retries internally, but the circuit sees only the outer failures.
        for (int i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => pipelineDelegate().AsTask());
        }

        int callsBeforeOpen = handlerCalls;

        // Once open, the circuit breaker must fail fast before RetryBehavior/the handler run at all.
        await Assert.ThrowsAsync<BrokenCircuitException>(() => pipelineDelegate().AsTask());

        Assert.Equal(callsBeforeOpen, handlerCalls);
    }

    private sealed class NoDelayProvider : IRetryDelayProvider
    {
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
