using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Caching.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    [Fact]
    public void AddPlaxionMediatorCaching_Throws_On_Null_Services()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPlaxionMediatorCaching(null!));
    }

    [Fact]
    public void AddPlaxionMediatorCaching_Registers_Cache_Options_Invalidator_And_Behavior()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCaching(o => o.DefaultCacheDuration = TimeSpan.FromSeconds(30));

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IMemoryCache>());
        Assert.NotNull(scope.ServiceProvider.GetService<IPlaxionMediatorCacheInvalidator>());
        Assert.IsType<MemoryCacheInvalidator>(scope.ServiceProvider.GetService<IPlaxionMediatorCacheInvalidator>());

        PlaxionMediatorCachingOptions options = scope.ServiceProvider.GetRequiredService<PlaxionMediatorCachingOptions>();
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultCacheDuration);

        IPipelineBehavior<Ping, string>[] behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<Ping, string>>()
            .ToArray();

        Assert.Single(behaviors);
        Assert.IsType<CachingBehavior<Ping, string>>(behaviors[0]);
    }

    [Fact]
    public void AddPlaxionMediatorCaching_Is_Idempotent_For_Options_And_Invalidator()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCaching(o => o.DefaultCacheDuration = TimeSpan.FromMinutes(1));
        services.AddPlaxionMediatorCaching(o => o.DefaultCacheDuration = TimeSpan.FromMinutes(9));

        using ServiceProvider sp = services.BuildServiceProvider();

        // TryAddSingleton keeps the first options instance.
        Assert.Equal(TimeSpan.FromMinutes(1), sp.GetRequiredService<PlaxionMediatorCachingOptions>().DefaultCacheDuration);
        Assert.Single(sp.GetServices<IPlaxionMediatorCacheInvalidator>());
        Assert.Single(sp.GetServices<IPipelineBehavior<Ping, string>>());
    }

    [Fact]
    public void UsePlaxionMediatorCachingBehavior_Adds_Behavior_To_Options()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorCachingBehavior();

        Assert.Contains(typeof(CachingBehavior<,>), options.GlobalBehaviors);
        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void UsePlaxionMediatorCachingBehavior_Is_Idempotent()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorCachingBehavior();
        options.UsePlaxionMediatorCachingBehavior();

        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void GlobalBehaviors_Plus_Extension_Does_Not_Duplicate_Behavior()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore(o => o.GlobalBehaviors.Add(typeof(CachingBehavior<,>)));
        services.AddPlaxionMediatorCaching();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        Assert.Single(scope.ServiceProvider.GetServices<IPipelineBehavior<Ping, string>>());
    }
}
