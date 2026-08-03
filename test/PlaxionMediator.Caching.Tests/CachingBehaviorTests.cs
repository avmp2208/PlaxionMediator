using Microsoft.Extensions.Caching.Memory;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Caching.Tests;

public sealed class CachingBehaviorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed record CacheablePing(string Message) : IRequest<string>, ICacheableRequest<string>
    {
        public string CacheKey { get; init; } = $"ping:{Message}";
        public TimeSpan? CacheDuration { get; init; }
    }

    private sealed record EmptyKeyPing(string Message) : IRequest<string>, ICacheableRequest<string>
    {
        public string CacheKey => "   ";
    }

    private static CachingBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        IMemoryCache? cache = null,
        PlaxionMediatorCachingOptions? options = null,
        IPlaxionMediatorCacheInvalidator? invalidator = null)
        where TRequest : IRequest<TResponse>
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        options ??= new PlaxionMediatorCachingOptions();
        invalidator ??= new MemoryCacheInvalidator(cache);
        return new CachingBehavior<TRequest, TResponse>(cache, options, invalidator);
    }

    [Fact]
    public void Constructor_Throws_On_Null_Dependencies()
    {
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        PlaxionMediatorCachingOptions options = new();
        IPlaxionMediatorCacheInvalidator invalidator = new MemoryCacheInvalidator(cache);

        Assert.Throws<ArgumentNullException>(() => new CachingBehavior<Ping, string>(null!, options, invalidator));
        Assert.Throws<ArgumentNullException>(() => new CachingBehavior<Ping, string>(cache, null!, invalidator));
        Assert.Throws<ArgumentNullException>(() => new CachingBehavior<Ping, string>(cache, options, null!));
    }

    [Fact]
    public async Task Handle_Throws_On_Null_Request_Or_Next()
    {
        CachingBehavior<Ping, string> behavior = CreateBehavior<Ping, string>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(new Ping("x"), null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task NonCacheable_Request_Is_NoOp_And_Calls_Next_Every_Time()
    {
        int calls = 0;
        CachingBehavior<Ping, string> behavior = CreateBehavior<Ping, string>();

        string first = await behavior.Handle(
            new Ping("a"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("r1");
            },
            CancellationToken.None);

        string second = await behavior.Handle(
            new Ping("a"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("r2");
            },
            CancellationToken.None);

        Assert.Equal("r1", first);
        Assert.Equal("r2", second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Cache_Miss_Then_Hit_Returns_Cached_Value_Without_Recalling_Next()
    {
        int calls = 0;
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>();
        CacheablePing request = new("widget");

        string first = await behavior.Handle(
            request,
            () =>
            {
                calls++;
                return ValueTask.FromResult("computed");
            },
            CancellationToken.None);

        string second = await behavior.Handle(
            request,
            () =>
            {
                calls++;
                return ValueTask.FromResult("should-not-run");
            },
            CancellationToken.None);

        Assert.Equal("computed", first);
        Assert.Equal("computed", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Empty_CacheKey_Disables_Caching()
    {
        int calls = 0;
        CachingBehavior<EmptyKeyPing, string> behavior = CreateBehavior<EmptyKeyPing, string>();

        await behavior.Handle(
            new EmptyKeyPing("a"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("one");
            },
            CancellationToken.None);

        await behavior.Handle(
            new EmptyKeyPing("a"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("two");
            },
            CancellationToken.None);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Handler_Exception_Is_Not_Cached()
    {
        int calls = 0;
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>();
        CacheablePing request = new("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                request,
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("fail");
                },
                CancellationToken.None).AsTask());

        string result = await behavior.Handle(
            request,
            () =>
            {
                calls++;
                return ValueTask.FromResult("recovered");
            },
            CancellationToken.None);

        Assert.Equal("recovered", result);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Already_Cancelled_Token_Does_Not_Call_Next()
    {
        bool nextCalled = false;
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new CacheablePing("x"),
                () =>
                {
                    nextCalled = true;
                    return ValueTask.FromResult("nope");
                },
                cts.Token).AsTask());

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Cancellation_After_Handler_Prevents_Cache_Write()
    {
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>(cache);
        using CancellationTokenSource cts = new();
        CacheablePing request = new("cancel-write");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                request,
                () =>
                {
                    cts.Cancel();
                    return ValueTask.FromResult("value");
                },
                cts.Token).AsTask());

        Assert.False(cache.TryGetValue(request.CacheKey, out _));
    }

    [Fact]
    public async Task Cache_Expiration_Allows_Recomputation()
    {
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>(cache);
        int calls = 0;
        CacheablePing request = new("expiring") { CacheDuration = TimeSpan.FromMilliseconds(30) };

        await behavior.Handle(
            request,
            () =>
            {
                calls++;
                return ValueTask.FromResult("v1");
            },
            CancellationToken.None);

        await Task.Delay(80);

        string second = await behavior.Handle(
            request,
            () =>
            {
                calls++;
                return ValueTask.FromResult("v2");
            },
            CancellationToken.None);

        Assert.Equal("v2", second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Uses_Default_Duration_When_Request_Does_Not_Specify()
    {
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        PlaxionMediatorCachingOptions options = new() { DefaultCacheDuration = TimeSpan.FromMinutes(10) };
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>(cache, options);
        CacheablePing request = new("defaults") { CacheDuration = null };

        await behavior.Handle(request, () => ValueTask.FromResult("ok"), CancellationToken.None);

        Assert.True(cache.TryGetValue(request.CacheKey, out object? cached));
        Assert.Equal("ok", cached);
    }

    [Fact]
    public async Task Concurrent_Identical_Keys_Do_Not_Throw_Or_Corrupt_Cache()
    {
        IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>(cache);
        CacheablePing request = new("concurrent");
        int calls = 0;

        Task<string>[] tasks = Enumerable.Range(0, 20)
            .Select(_ => behavior.Handle(
                request,
                async () =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Yield();
                    return "same";
                },
                CancellationToken.None).AsTask())
            .ToArray();

        string[] results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal("same", r));
        Assert.True(cache.TryGetValue(request.CacheKey, out object? cached));
        Assert.Equal("same", cached);
        Assert.True(calls >= 1);
    }

    [Fact]
    public async Task Different_Keys_Cache_Independently()
    {
        CachingBehavior<CacheablePing, string> behavior = CreateBehavior<CacheablePing, string>();
        int calls = 0;

        string a = await behavior.Handle(
            new CacheablePing("a"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("A");
            },
            CancellationToken.None);

        string b = await behavior.Handle(
            new CacheablePing("b"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("B");
            },
            CancellationToken.None);

        Assert.Equal("A", a);
        Assert.Equal("B", b);
        Assert.Equal(2, calls);
    }
}
