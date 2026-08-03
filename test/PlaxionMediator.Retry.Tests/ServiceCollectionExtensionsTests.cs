using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Retry.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    [Fact]
    public void AddPlaxionMediatorRetry_Throws_On_Null_Services()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPlaxionMediatorRetry(null!));
    }

    [Fact]
    public void AddPlaxionMediatorRetry_Registers_Defaults()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorRetry();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        PlaxionMediatorRetryOptions options = scope.ServiceProvider.GetRequiredService<PlaxionMediatorRetryOptions>();
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(RetryBackoffStrategy.Exponential, options.BackoffStrategy);

        Assert.IsType<TaskRetryDelayProvider>(scope.ServiceProvider.GetRequiredService<IRetryDelayProvider>());

        IPipelineBehavior<Ping, string>[] behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<Ping, string>>()
            .ToArray();

        Assert.Single(behaviors);
        Assert.IsType<RetryBehavior<Ping, string>>(behaviors[0]);
    }

    [Fact]
    public void AddPlaxionMediatorRetry_Applies_Custom_Options()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorRetry(o =>
        {
            o.MaxRetryAttempts = 7;
            o.BaseDelay = TimeSpan.FromMilliseconds(1);
            o.BackoffStrategy = RetryBackoffStrategy.Constant;
            o.NonRetryableExceptionTypes.Add(typeof(ArgumentException));
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        PlaxionMediatorRetryOptions options = sp.GetRequiredService<PlaxionMediatorRetryOptions>();

        Assert.Equal(7, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(1), options.BaseDelay);
        Assert.Equal(RetryBackoffStrategy.Constant, options.BackoffStrategy);
        Assert.Contains(typeof(ArgumentException), options.NonRetryableExceptionTypes);
    }

    [Fact]
    public void AddPlaxionMediatorRetry_Is_Idempotent()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorRetry(o => o.MaxRetryAttempts = 1);
        services.AddPlaxionMediatorRetry(o => o.MaxRetryAttempts = 99);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.Equal(1, sp.GetRequiredService<PlaxionMediatorRetryOptions>().MaxRetryAttempts);
        Assert.Single(sp.GetServices<IRetryDelayProvider>());
        Assert.Single(sp.GetServices<IPipelineBehavior<Ping, string>>());
    }

    [Fact]
    public void UsePlaxionMediatorRetryBehavior_Adds_Behavior_To_Options()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorRetryBehavior();

        Assert.Contains(typeof(RetryBehavior<,>), options.GlobalBehaviors);
        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void UsePlaxionMediatorRetryBehavior_Is_Idempotent()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorRetryBehavior();
        options.UsePlaxionMediatorRetryBehavior();

        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void GlobalBehaviors_Plus_Extension_Does_Not_Duplicate_Behavior()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore(o => o.GlobalBehaviors.Add(typeof(RetryBehavior<,>)));
        services.AddPlaxionMediatorRetry();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        Assert.Single(scope.ServiceProvider.GetServices<IPipelineBehavior<Ping, string>>());
    }

    [Fact]
    public void Custom_DelayProvider_Can_Replace_Default()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorRetry();
        services.AddSingleton<IRetryDelayProvider, NoOpDelayProvider>();

        using ServiceProvider sp = services.BuildServiceProvider();
        // Last registration of singleton concrete wins only if not TryAdd — we used AddSingleton after TryAdd,
        // which adds a second descriptor; GetRequiredService returns the last one for singleton.
        IRetryDelayProvider provider = sp.GetRequiredService<IRetryDelayProvider>();
        Assert.IsType<NoOpDelayProvider>(provider);
    }

    private sealed class NoOpDelayProvider : IRetryDelayProvider
    {
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
