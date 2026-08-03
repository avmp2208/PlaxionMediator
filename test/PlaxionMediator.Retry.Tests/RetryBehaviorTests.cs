using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Retry.Tests;

public sealed class RetryBehaviorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed record RetryablePing(string Message) : IRequest<string>, IRetryableRequest
    {
        public int? MaxRetryAttempts { get; init; }
        public TimeSpan? BaseDelay { get; init; }
    }

    private sealed class RecordingDelayProvider : IRetryDelayProvider
    {
        public List<TimeSpan> Delays { get; } = [];
        public int CallCount => Delays.Count;

        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Delays.Add(delay);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancellingDelayProvider : IRetryDelayProvider
    {
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static RetryBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        PlaxionMediatorRetryOptions? options = null,
        IRetryDelayProvider? delayProvider = null)
        where TRequest : IRequest<TResponse>
    {
        options ??= new PlaxionMediatorRetryOptions
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            BackoffStrategy = RetryBackoffStrategy.Exponential,
        };
        delayProvider ??= new RecordingDelayProvider();
        return new RetryBehavior<TRequest, TResponse>(options, delayProvider);
    }

    [Fact]
    public void Constructor_Throws_On_Null_Dependencies()
    {
        PlaxionMediatorRetryOptions options = new();
        IRetryDelayProvider delay = new RecordingDelayProvider();

        Assert.Throws<ArgumentNullException>(() => new RetryBehavior<Ping, string>(null!, delay));
        Assert.Throws<ArgumentNullException>(() => new RetryBehavior<Ping, string>(options, null!));
    }

    [Fact]
    public async Task Handle_Throws_On_Null_Request_Or_Next()
    {
        RetryBehavior<Ping, string> behavior = CreateBehavior<Ping, string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(new Ping("x"), null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task NonRetryable_Request_Is_NoOp_And_Does_Not_Retry()
    {
        int calls = 0;
        RetryBehavior<Ping, string> behavior = CreateBehavior<Ping, string>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new Ping("x"),
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("boom");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Succeeds_On_First_Try_Without_Delay()
    {
        RecordingDelayProvider delay = new();
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(delayProvider: delay);
        int calls = 0;

        string result = await behavior.Handle(
            new RetryablePing("ok"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("done");
            },
            CancellationToken.None);

        Assert.Equal("done", result);
        Assert.Equal(1, calls);
        Assert.Empty(delay.Delays);
    }

    [Fact]
    public async Task Retries_N_Times_Then_Succeeds()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(5),
            BackoffStrategy = RetryBackoffStrategy.Constant,
        };
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        string result = await behavior.Handle(
            new RetryablePing("flaky"),
            () =>
            {
                calls++;
                if (calls < 3)
                {
                    throw new InvalidOperationException($"transient-{calls}");
                }

                return ValueTask.FromResult("recovered");
            },
            CancellationToken.None);

        Assert.Equal("recovered", result);
        Assert.Equal(3, calls);
        Assert.Equal(2, delay.CallCount);
        Assert.All(delay.Delays, d => Assert.Equal(TimeSpan.FromMilliseconds(5), d));
    }

    [Fact]
    public async Task Exhausted_Retries_Rethrow_Last_Exception()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(3),
            BackoffStrategy = RetryBackoffStrategy.Constant,
        };
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new RetryablePing("fail"),
                () =>
                {
                    calls++;
                    throw new InvalidOperationException($"fail-{calls}");
                },
                CancellationToken.None).AsTask());

        Assert.Equal("fail-3", ex.Message); // 1 initial + 2 retries
        Assert.Equal(3, calls);
        Assert.Equal(2, delay.CallCount);
    }

    [Fact]
    public async Task Zero_BaseDelay_Skips_DelayProvider_But_Still_Retries()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.Zero,
        };
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new RetryablePing("fail"),
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("fail");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(3, calls);
        Assert.Empty(delay.Delays);
    }

    [Fact]
    public async Task Does_Not_Retry_OperationCanceledException()
    {
        RecordingDelayProvider delay = new();
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(delayProvider: delay);
        int calls = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new RetryablePing("cancel"),
                () =>
                {
                    calls++;
                    throw new OperationCanceledException();
                },
                CancellationToken.None).AsTask());

        Assert.Equal(1, calls);
        Assert.Empty(delay.Delays);
    }

    [Fact]
    public async Task Already_Cancelled_Token_Does_Not_Call_Next()
    {
        bool nextCalled = false;
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new RetryablePing("x"),
                () =>
                {
                    nextCalled = true;
                    return ValueTask.FromResult("nope");
                },
                cts.Token).AsTask());

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Cancellation_During_Delay_Propagates_Without_Further_Attempts()
    {
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(1),
        };
        RetryBehavior<RetryablePing, string> behavior = new(options, new CancellingDelayProvider());
        int calls = 0;
        using CancellationTokenSource cts = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new RetryablePing("x"),
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("transient");
                },
                cts.Token).AsTask());

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Does_Not_Retry_Configured_NonRetryable_Exception_Types()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(1),
        };
        options.NonRetryableExceptionTypes.Add(typeof(ArgumentException));
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            behavior.Handle(
                new RetryablePing("validation-like"),
                () =>
                {
                    calls++;
                    throw new ArgumentException("not transient");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(1, calls);
        Assert.Empty(delay.Delays);
    }

    [Fact]
    public async Task NonRetryable_Matches_Derived_Exception_Types()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new() { MaxRetryAttempts = 3, BaseDelay = TimeSpan.Zero };
        options.NonRetryableExceptionTypes.Add(typeof(ArgumentException));
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(
                new RetryablePing("derived"),
                () =>
                {
                    calls++;
                    throw new ArgumentNullException("param");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Exponential_Backoff_Delays_Double_Each_Attempt()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(4),
            BackoffStrategy = RetryBackoffStrategy.Exponential,
        };
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new RetryablePing("exp"),
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("always");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(4, calls); // 1 + 3 retries
        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(4),  // 4 * 2^0
                TimeSpan.FromMilliseconds(8),  // 4 * 2^1
                TimeSpan.FromMilliseconds(16), // 4 * 2^2
            },
            delay.Delays);
    }

    [Fact]
    public async Task Per_Request_MaxRetryAttempts_And_BaseDelay_Override_Options()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new()
        {
            MaxRetryAttempts = 10,
            BaseDelay = TimeSpan.FromSeconds(5),
            BackoffStrategy = RetryBackoffStrategy.Constant,
        };
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new RetryablePing("override")
                {
                    MaxRetryAttempts = 1,
                    BaseDelay = TimeSpan.FromMilliseconds(7),
                },
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("fail");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(2, calls); // 1 initial + 1 retry
        Assert.Single(delay.Delays);
        Assert.Equal(TimeSpan.FromMilliseconds(7), delay.Delays[0]);
    }

    [Fact]
    public async Task Zero_MaxRetryAttempts_Means_Single_Try_Only()
    {
        RecordingDelayProvider delay = new();
        PlaxionMediatorRetryOptions options = new() { MaxRetryAttempts = 0, BaseDelay = TimeSpan.FromMilliseconds(1) };
        RetryBehavior<RetryablePing, string> behavior = CreateBehavior<RetryablePing, string>(options, delay);
        int calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new RetryablePing("once"),
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("fail");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(1, calls);
        Assert.Empty(delay.Delays);
    }
}
