using PlaxionMediator.Abstractions;
using PlaxionMediator.Pipeline;

namespace PlaxionMediator.Validation.Tests;

public sealed class ValidationBehaviorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class RecordingValidator : IPlaxionMediatorValidator<Ping>
    {
        private readonly PlaxionMediatorValidationResult _result;
        private readonly Action? _onValidate;

        public int CallCount { get; private set; }

        public RecordingValidator(PlaxionMediatorValidationResult result, Action? onValidate = null)
        {
            _result = result;
            _onValidate = onValidate;
        }

        public ValueTask<PlaxionMediatorValidationResult> ValidateAsync(Ping request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            _onValidate?.Invoke();
            return ValueTask.FromResult(_result);
        }
    }

    private sealed class DelayedFailingValidator : IPlaxionMediatorValidator<Ping>
    {
        public ValueTask<PlaxionMediatorValidationResult> ValidateAsync(Ping request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(PlaxionMediatorValidationResult.Failed(
                new PlaxionMediatorValidationFailure("Message", "from-delayed")));
        }
    }

    [Fact]
    public void Constructor_Throws_On_Null_Validators()
    {
        Assert.Throws<ArgumentNullException>(() => new ValidationBehavior<Ping, string>(null!));
    }

    [Fact]
    public async Task Handle_Throws_On_Null_Request_Or_Next()
    {
        ValidationBehavior<Ping, string> behavior = new(Array.Empty<IPlaxionMediatorValidator<Ping>>());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(new Ping("x"), null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task No_Validators_Is_Fast_NoOp_And_Calls_Next()
    {
        bool nextCalled = false;
        ValidationBehavior<Ping, string> behavior = new(Array.Empty<IPlaxionMediatorValidator<Ping>>());

        string result = await behavior.Handle(
            new Ping("hi"),
            () =>
            {
                nextCalled = true;
                return ValueTask.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Success_Path_Calls_All_Validators_Then_Next()
    {
        List<string> log = [];
        RecordingValidator first = new(PlaxionMediatorValidationResult.Success, () => log.Add("v1"));
        RecordingValidator second = new(PlaxionMediatorValidationResult.Success, () => log.Add("v2"));
        ValidationBehavior<Ping, string> behavior = new([first, second]);

        string result = await behavior.Handle(
            new Ping("hi"),
            () =>
            {
                log.Add("next");
                return ValueTask.FromResult("done");
            },
            CancellationToken.None);

        Assert.Equal("done", result);
        Assert.Equal(["v1", "v2", "next"], log);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
    }

    [Fact]
    public async Task Single_Failure_Throws_And_Does_Not_Call_Next()
    {
        bool nextCalled = false;
        RecordingValidator validator = new(PlaxionMediatorValidationResult.Failed(
            new PlaxionMediatorValidationFailure("Message", "required")));
        ValidationBehavior<Ping, string> behavior = new([validator]);

        PlaxionMediatorValidationException ex = await Assert.ThrowsAsync<PlaxionMediatorValidationException>(() =>
            behavior.Handle(
                new Ping(""),
                () =>
                {
                    nextCalled = true;
                    return ValueTask.FromResult("nope");
                },
                CancellationToken.None).AsTask());

        Assert.False(nextCalled);
        Assert.Single(ex.Failures);
        Assert.Equal("Message", ex.Failures[0].PropertyName);
        Assert.Equal("required", ex.Failures[0].ErrorMessage);
    }

    [Fact]
    public async Task Multiple_Validators_Aggregate_All_Failures_Without_ShortCircuit()
    {
        RecordingValidator first = new(PlaxionMediatorValidationResult.Failed(
            new PlaxionMediatorValidationFailure("Message", "too-short"),
            new PlaxionMediatorValidationFailure("Message", "whitespace")));
        RecordingValidator second = new(PlaxionMediatorValidationResult.Failed(
            new PlaxionMediatorValidationFailure("Message", "blocked")));
        ValidationBehavior<Ping, string> behavior = new([first, second]);

        PlaxionMediatorValidationException ex = await Assert.ThrowsAsync<PlaxionMediatorValidationException>(() =>
            behavior.Handle(
                new Ping("x"),
                () => ValueTask.FromResult("nope"),
                CancellationToken.None).AsTask());

        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
        Assert.Equal(3, ex.Failures.Count);
        Assert.Contains(ex.Failures, f => f.ErrorMessage == "too-short");
        Assert.Contains(ex.Failures, f => f.ErrorMessage == "whitespace");
        Assert.Contains(ex.Failures, f => f.ErrorMessage == "blocked");
    }

    [Fact]
    public async Task Already_Cancelled_Token_Does_Not_Run_Validators_Or_Next()
    {
        bool nextCalled = false;
        RecordingValidator validator = new(PlaxionMediatorValidationResult.Success);
        ValidationBehavior<Ping, string> behavior = new([validator]);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new Ping("x"),
                () =>
                {
                    nextCalled = true;
                    return ValueTask.FromResult("nope");
                },
                cts.Token).AsTask());

        Assert.Equal(0, validator.CallCount);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Cancellation_Between_Validators_Stops_Remaining_Work()
    {
        using CancellationTokenSource cts = new();
        RecordingValidator first = new(
            PlaxionMediatorValidationResult.Success,
            () => cts.Cancel());
        RecordingValidator second = new(PlaxionMediatorValidationResult.Success);
        ValidationBehavior<Ping, string> behavior = new([first, second]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new Ping("x"),
                () => ValueTask.FromResult("nope"),
                cts.Token).AsTask());

        Assert.Equal(1, first.CallCount);
        Assert.Equal(0, second.CallCount);
    }

    [Fact]
    public async Task PipelineComposer_Surfaces_ValidationException_Unwrapped()
    {
        ValidationBehavior<Ping, string> behavior = new(
        [
            new DelayedFailingValidator(),
        ]);

        PlaxionMediatorValidationException ex = await Assert.ThrowsAsync<PlaxionMediatorValidationException>(() =>
            PipelineComposer.ExecuteAsync(
                new Ping("x"),
                [behavior],
                static (_, _) => ValueTask.FromResult("ok"),
                CancellationToken.None).AsTask());

        Assert.Single(ex.Failures);
        Assert.Equal("from-delayed", ex.Failures[0].ErrorMessage);
    }

    [Fact]
    public async Task Validation_Runs_Before_Other_Inner_Behaviors_And_Handler()
    {
        List<string> log = [];
        ValidationBehavior<Ping, string> validation = new(
        [
            new RecordingValidator(
                PlaxionMediatorValidationResult.Failed(new PlaxionMediatorValidationFailure("Message", "bad")),
                () => log.Add("validate")),
        ]);

        var inner = new LoggingBehavior(log, "inner");

        await Assert.ThrowsAsync<PlaxionMediatorValidationException>(() =>
            PipelineComposer.ExecuteAsync(
                new Ping("x"),
                new IPipelineBehavior<Ping, string>[] { validation, inner },
                (_, _) =>
                {
                    log.Add("handler");
                    return ValueTask.FromResult("ok");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(["validate"], log);
    }

    [Fact]
    public async Task Validation_Orders_With_Outer_Behavior_When_Valid()
    {
        List<string> log = [];
        ValidationBehavior<Ping, string> validation = new(
        [
            new RecordingValidator(PlaxionMediatorValidationResult.Success, () => log.Add("validate")),
        ]);
        var outer = new LoggingBehavior(log, "outer");

        string result = await PipelineComposer.ExecuteAsync(
            new Ping("x"),
            new IPipelineBehavior<Ping, string>[] { outer, validation },
            (_, _) =>
            {
                log.Add("handler");
                return ValueTask.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(["outer-before", "validate", "handler", "outer-after"], log);
    }

    private sealed class LoggingBehavior : IPipelineBehavior<Ping, string>
    {
        private readonly List<string> _log;
        private readonly string _name;

        public LoggingBehavior(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            _log.Add($"{_name}-before");
            string result = await next();
            _log.Add($"{_name}-after");
            return result;
        }
    }
}
