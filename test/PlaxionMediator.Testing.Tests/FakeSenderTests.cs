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

    [Fact]
    public async Task WhenStream_With_IEnumerable_Yields_Items()
    {
        FakeSender sender = new();
        sender.WhenStream<StreamPing, int>(request => Enumerable.Range(0, request.Count));

        List<int> items = [];
        await foreach (int item in sender.CreateStream(new StreamPing(3)))
        {
            items.Add(item);
        }

        Assert.Equal(new[] { 0, 1, 2 }, items);
        sender.AssertCallCount<StreamPing>(1);
    }

    [Fact]
    public async Task WhenStream_With_IAsyncEnumerable_Yields_Items()
    {
        FakeSender sender = new();
        sender.WhenStream<StreamPing, int>(request => ToAsync(request.Count));

        List<int> items = [];
        await foreach (int item in sender.CreateStream(new StreamPing(3)))
        {
            items.Add(item);
        }

        Assert.Equal(new[] { 0, 1, 2 }, items);
        sender.AssertCallCount<StreamPing>(1);

        static async IAsyncEnumerable<int> ToAsync(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return i;
                await Task.Yield();
            }
        }
    }

    [Fact]
    public async Task GetCallCount_Reflects_Number_Of_Sends_Per_Type()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(_ => "ok");
        sender.When<CountRequest, int>(_ => 1);

        await sender.Send(new Ping("a"));
        await sender.Send(new Ping("b"));
        await sender.Send(new CountRequest());

        Assert.Equal(2, sender.GetCallCount<Ping>());
        Assert.Equal(1, sender.GetCallCount<CountRequest>());
        Assert.Equal(0, sender.GetCallCount<StreamPing>());
    }

    [Fact]
    public async Task GetSent_Returns_Captured_Requests_Of_Type()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(_ => "ok");

        await sender.Send(new Ping("a"));
        await sender.Send(new Ping("b"));

        IReadOnlyList<Ping> pings = sender.GetSent<Ping>();
        Assert.Equal(new[] { "a", "b" }, pings.Select(p => p.Message));
    }

    [Fact]
    public async Task AssertCallCount_Throws_On_Mismatch()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(_ => "ok");
        await sender.Send(new Ping("a"));

        sender.AssertCallCount<Ping>(1);
        Assert.Throws<FakeSenderAssertionException>(() => sender.AssertCallCount<Ping>(2));
    }

    [Fact]
    public async Task AssertSentInOrder_Validates_Type_Sequence()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(_ => "ok");
        sender.When<CountRequest, int>(_ => 1);

        await sender.Send(new Ping("a"));
        await sender.Send(new CountRequest());

        sender.AssertSentInOrder(typeof(Ping), typeof(CountRequest));
        Assert.Throws<FakeSenderAssertionException>(
            () => sender.AssertSentInOrder(typeof(CountRequest), typeof(Ping)));
        Assert.Throws<FakeSenderAssertionException>(
            () => sender.AssertSentInOrder(typeof(Ping)));
    }

    [Fact]
    public async Task AssertSentInOrder_Mixed_Requests()
    {
        FakeSender sender = new();
        sender.When<Ping, string>(_ => "ok");
        sender.WhenStream<StreamPing, int>(_ => [1, 2]);

        await sender.Send(new Ping("a"));
        await foreach (int _ in sender.CreateStream(new StreamPing(1))) { }
        await sender.Send(new Ping("b"));

        sender.AssertSentInOrder(typeof(Ping), typeof(StreamPing), typeof(Ping));
    }
}
