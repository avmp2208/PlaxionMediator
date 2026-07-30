using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Abstractions.Tests;

public sealed class UnitTests
{
    [Fact]
    public void Value_Equals_Default()
    {
        Assert.Equal(default, Unit.Value);
        Assert.True(Unit.Value == default);
        Assert.False(Unit.Value != default);
        Assert.Equal(0, Unit.Value.GetHashCode());
        Assert.Equal("()", Unit.Value.ToString());
    }

    [Fact]
    public void Unit_Equals_Object()
    {
        Assert.True(Unit.Value.Equals((object)Unit.Value));
        Assert.False(Unit.Value.Equals("nope"));
        Assert.False(Unit.Value.Equals(null));
    }
}

public sealed class ContractShapeTests
{
    private sealed record SampleRequest(string Name) : IRequest<string>;

    private sealed class SampleHandler : IRequestHandler<SampleRequest, string>
    {
        public ValueTask<string> Handle(SampleRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(request.Name);
    }

    private sealed record SampleNotification(string Message) : INotification;

    private sealed class SampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        public ValueTask Handle(SampleNotification notification, CancellationToken cancellationToken)
            => default;
    }

    private sealed class SampleBehavior : IPipelineBehavior<SampleRequest, string>
    {
        public async ValueTask<string> Handle(
            SampleRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            string result = await next();
            return result + "!";
        }
    }

    [Fact]
    public async Task RequestHandler_Contract_Works()
    {
        SampleHandler handler = new();
        string result = await handler.Handle(new SampleRequest("hi"), CancellationToken.None);
        Assert.Equal("hi", result);
    }

    [Fact]
    public async Task NotificationHandler_Contract_Works()
    {
        SampleNotificationHandler handler = new();
        await handler.Handle(new SampleNotification("x"), CancellationToken.None);
    }

    [Fact]
    public async Task PipelineBehavior_Contract_Works()
    {
        SampleBehavior behavior = new();
        string result = await behavior.Handle(
            new SampleRequest("a"),
            () => ValueTask.FromResult("b"),
            CancellationToken.None);
        Assert.Equal("b!", result);
    }

    [Fact]
    public void IRequest_Without_Response_Is_Unit()
    {
        IRequest request = new UnitRequest();
        Assert.IsAssignableFrom<IRequest<Unit>>(request);
    }

    private sealed record UnitRequest : IRequest;
}
