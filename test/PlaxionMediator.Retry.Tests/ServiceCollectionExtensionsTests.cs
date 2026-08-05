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

    [Fact]
    public void AddPlaxionMediatorCircuitBreaker_Throws_On_Null_Services()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPlaxionMediatorCircuitBreaker(null!));
    }

    [Fact]
    public void AddPlaxionMediatorCircuitBreaker_Registers_Defaults()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCircuitBreaker();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        PlaxionMediatorCircuitBreakerOptions options =
            scope.ServiceProvider.GetRequiredService<PlaxionMediatorCircuitBreakerOptions>();
        Assert.Equal("plaxionmediator-circuitbreaker-default", options.PipelineName);
        Assert.Equal(0.5, options.FailureRatio);
        Assert.Equal(10, options.MinimumThroughput);

        Assert.IsType<DefaultCircuitBreakerPolicyProvider<Ping>>(
            scope.ServiceProvider.GetRequiredService<ICircuitBreakerPolicyProvider<Ping>>());

        IPipelineBehavior<Ping, string>[] behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<Ping, string>>()
            .ToArray();

        Assert.Single(behaviors);
        Assert.IsType<CircuitBreakerBehavior<Ping, string>>(behaviors[0]);
    }

    [Fact]
    public void AddPlaxionMediatorCircuitBreaker_Applies_Custom_Options()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCircuitBreaker(o =>
        {
            o.PipelineName = "custom-pipeline";
            o.FailureRatio = 0.75;
            o.MinimumThroughput = 4;
            o.BreakDuration = TimeSpan.FromSeconds(1);
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        PlaxionMediatorCircuitBreakerOptions options = sp.GetRequiredService<PlaxionMediatorCircuitBreakerOptions>();

        Assert.Equal("custom-pipeline", options.PipelineName);
        Assert.Equal(0.75, options.FailureRatio);
        Assert.Equal(4, options.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(1), options.BreakDuration);
    }

    [Fact]
    public void AddPlaxionMediatorCircuitBreaker_Is_Idempotent()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCircuitBreaker(o => o.FailureRatio = 0.1);
        services.AddPlaxionMediatorCircuitBreaker(o => o.FailureRatio = 0.9);

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.Equal(0.1, sp.GetRequiredService<PlaxionMediatorCircuitBreakerOptions>().FailureRatio);
        Assert.Single(sp.GetServices<ICircuitBreakerPolicyProvider<Ping>>());
        Assert.Single(sp.GetServices<IPipelineBehavior<Ping, string>>());
    }

    [Fact]
    public void UsePlaxionMediatorCircuitBreakerBehavior_Adds_Behavior_To_Options()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorCircuitBreakerBehavior();

        Assert.Contains(typeof(CircuitBreakerBehavior<,>), options.GlobalBehaviors);
        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void UsePlaxionMediatorCircuitBreakerBehavior_Is_Idempotent()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorCircuitBreakerBehavior();
        options.UsePlaxionMediatorCircuitBreakerBehavior();

        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void CircuitBreaker_And_Retry_Coexist_As_Independent_Behaviors()
    {
        PlaxionMediatorOptions mediatorOptions = new();
        mediatorOptions.UsePlaxionMediatorCircuitBreakerBehavior();
        mediatorOptions.UsePlaxionMediatorRetryBehavior();

        Assert.Equal(
            new[] { typeof(CircuitBreakerBehavior<,>), typeof(RetryBehavior<,>) },
            mediatorOptions.GlobalBehaviors);

        ServiceCollection services = new();
        services.AddPlaxionMediatorCircuitBreaker();
        services.AddPlaxionMediatorRetry();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        IPipelineBehavior<Ping, string>[] behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<Ping, string>>()
            .ToArray();

        Assert.Equal(2, behaviors.Length);
        Assert.Contains(behaviors, b => b is CircuitBreakerBehavior<Ping, string>);
        Assert.Contains(behaviors, b => b is RetryBehavior<Ping, string>);
    }
}
