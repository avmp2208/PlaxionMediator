using Microsoft.Extensions.Caching.Memory;

namespace PlaxionMediator.Caching.Tests;

public sealed class MemoryCacheInvalidatorTests
{
    [Fact]
    public void Constructor_Throws_On_Null_Cache()
    {
        Assert.Throws<ArgumentNullException>(() => new MemoryCacheInvalidator(null!));
    }

    [Fact]
    public void Remove_Throws_On_Null_Or_Whitespace_Key()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        MemoryCacheInvalidator invalidator = new(cache);

        Assert.Throws<ArgumentNullException>(() => invalidator.Remove(null!));
        Assert.Throws<ArgumentException>(() => invalidator.Remove(" "));
    }

    [Fact]
    public void Remove_Deletes_Cached_Entry()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        cache.Set("k1", "v1");
        MemoryCacheInvalidator invalidator = new(cache);

        invalidator.Remove("k1");

        Assert.False(cache.TryGetValue("k1", out _));
    }

    [Fact]
    public void RemoveByRequestType_Removes_All_Tracked_Keys_For_Type()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);
        MemoryCacheInvalidator invalidator = new(cache);

        invalidator.Track(typeof(string), "a");
        invalidator.Track(typeof(string), "b");
        invalidator.Track(typeof(int), "c");

        invalidator.RemoveByRequestType<string>();

        Assert.False(cache.TryGetValue("a", out _));
        Assert.False(cache.TryGetValue("b", out _));
        Assert.True(cache.TryGetValue("c", out _));
    }

    [Fact]
    public void RemoveByRequestType_NoTrackedKeys_Is_NoOp()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        cache.Set("x", 1);
        MemoryCacheInvalidator invalidator = new(cache);

        invalidator.RemoveByRequestType(typeof(Guid));

        Assert.True(cache.TryGetValue("x", out _));
    }

    [Fact]
    public void Remove_Also_Drops_Tracking_Entry()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        cache.Set("k", "v");
        MemoryCacheInvalidator invalidator = new(cache);
        invalidator.Track(typeof(string), "k");

        invalidator.Remove("k");
        cache.Set("k", "again");
        // Type-based remove should no longer know about k after Remove dropped tracking.
        invalidator.RemoveByRequestType<string>();

        Assert.True(cache.TryGetValue("k", out object? value));
        Assert.Equal("again", value);
    }

    [Fact]
    public void Track_Throws_On_Null_Args()
    {
        using MemoryCache cache = new(new MemoryCacheOptions());
        MemoryCacheInvalidator invalidator = new(cache);

        Assert.Throws<ArgumentNullException>(() => invalidator.Track(null!, "k"));
        Assert.Throws<ArgumentException>(() => invalidator.Track(typeof(string), " "));
    }
}
