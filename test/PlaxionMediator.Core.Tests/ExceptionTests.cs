using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;

namespace PlaxionMediator.Core.Tests;

public sealed class ExceptionTests
{
    [Fact]
    public void HandlerNotFoundException_Contains_RequestType()
    {
        HandlerNotFoundException ex = new(typeof(string));
        Assert.Equal(typeof(string), ex.RequestType);
        Assert.Contains("String", ex.Message);
        Assert.IsAssignableFrom<PlaxionMediatorException>(ex);
    }

    [Fact]
    public void HandlerNotFoundException_MessageOnly()
    {
        HandlerNotFoundException ex = new("missing");
        Assert.Equal("missing", ex.Message);
        Assert.Null(ex.RequestType);
    }

    [Fact]
    public void PipelineExecutionException_Preserves_Inner_And_Stage()
    {
        InvalidOperationException inner = new("boom");
        PipelineExecutionException ex = new("failed", inner, "ValidationBehavior");
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("ValidationBehavior", ex.StageName);
        Assert.Null(ex.RequestTypeName);
        Assert.IsAssignableFrom<PlaxionMediatorException>(ex);
    }

    [Fact]
    public void PipelineExecutionException_Preserves_RequestTypeName()
    {
        InvalidOperationException inner = new("boom");
        PipelineExecutionException ex = new("failed", inner, "ValidationBehavior", "Ping");
        Assert.Equal("ValidationBehavior", ex.StageName);
        Assert.Equal("Ping", ex.RequestTypeName);
    }

    [Fact]
    public void HandlerFaultException_Preserves_Inner_And_RequestTypeName()
    {
        InvalidOperationException inner = new("boom");
        HandlerFaultException ex = new(inner, "Ping");
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("Ping", ex.RequestTypeName);
        Assert.Equal(inner.Message, ex.Message);
        Assert.IsAssignableFrom<PlaxionMediatorException>(ex);
    }

    [Fact]
    public void HandlerFaultException_BackwardCompatibility()
    {
        InvalidOperationException inner = new("boom");
        HandlerFaultException ex = new(inner);
        Assert.Same(inner, ex.InnerException);
        Assert.Null(ex.RequestTypeName);
    }
}

public sealed class DispatcherContractTests
{
    private sealed record Ping(string Msg) : IRequest<string>;

    private sealed class ManualSender : ISender
    {
        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is Ping ping && typeof(TResponse) == typeof(string))
            {
                return new ValueTask<TResponse>((TResponse)(object)$"Pong:{ping.Msg}");
            }

            throw new HandlerNotFoundException(request.GetType());
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new HandlerNotFoundException(request.GetType());
    }

    private sealed class ManualPublisher : IPublisher
    {
        public int Count { get; private set; }

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Count++;
            return default;
        }
    }

    private sealed record Note(string Text) : INotification;

    [Fact]
    public async Task ManualSender_Dispatches()
    {
        ManualSender sender = new();
        string result = await sender.Send(new Ping("x"));
        Assert.Equal("Pong:x", result);
    }

    [Fact]
    public async Task ManualSender_Throws_When_Unknown()
    {
        ManualSender sender = new();
        await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            await sender.Send(new OtherRequest()));
    }

    [Fact]
    public async Task ManualPublisher_Publishes()
    {
        ManualPublisher publisher = new();
        await publisher.Publish(new Note("n"));
        Assert.Equal(1, publisher.Count);
    }

    private sealed record OtherRequest : IRequest<int>;
}
