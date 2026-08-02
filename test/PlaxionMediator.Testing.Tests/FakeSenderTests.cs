using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Testing;

namespace PlaxionMediator.Testing.Tests;

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

    private sealed record StreamPing(int Count) : IStreamRequest<int>;

    [Fact]
    public async Task WhenStream_Yields_Configured_Items()
    {
        FakeSender sender = new();
        sender.WhenStream<StreamPing, int>((request, ct) => Stream(request.Count, ct));

        List<int> items = [];
        await foreach (int item in sender.CreateStream(new StreamPing(3)))
        {
            items.Add(item);
        }

        Assert.Equal(new[] { 0, 1, 2 }, items);
        Assert.Single(sender.SentRequests);

        static async IAsyncEnumerable<int> Stream(int count, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return i;
                await Task.Yield();
            }
        }
    }
}
