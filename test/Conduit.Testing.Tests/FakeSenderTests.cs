using Conduit.Abstractions;
using Conduit.Core;
using Conduit.Testing;

namespace Conduit.Testing.Tests;

public sealed class FakeSenderTests
{
    private sealed record Ping(string Message) : IRequest<string>;
    private sealed record CountRequest : IRequest<int>;

    [Fact]
    public async Task When_Sync_Returns_Configured_Response()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(r => "Pong:" + r.Message);

        string result = await sender.Send(new Ping("hi"));
        Assert.Equal("Pong:hi", result);
        Assert.Single(sender.SentRequests);
        Assert.IsType<Ping>(sender.SentRequests[0]);
    }

    [Fact]
    public async Task When_Async_Receives_CancellationToken()
    {
        FakeSender sender = new();
        using CancellationTokenSource cts = new();
        sender.When<CountRequest, int>((_, ct) =>
        {
            Assert.True(ct.CanBeCanceled);
            return ValueTask.FromResult(42);
        });

        int result = await sender.Send(new CountRequest(), cts.Token);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Send_Without_Registration_Throws()
    {
        FakeSender sender = new();
        await Assert.ThrowsAsync<HandlerNotFoundException>(async () => await sender.Send(new Ping("x")));
    }

    [Fact]
    public async Task Reset_Clears_State()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(_ => "ok");
        await sender.Send(new Ping("a"));
        sender.Reset();

        Assert.Empty(sender.SentRequests);
        await Assert.ThrowsAsync<HandlerNotFoundException>(async () => await sender.Send(new Ping("b")));
    }
}
