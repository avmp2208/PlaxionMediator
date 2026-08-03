using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Validation.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class PingValidator : IPlaxionMediatorValidator<Ping>
    {
        public ValueTask<PlaxionMediatorValidationResult> ValidateAsync(Ping request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(PlaxionMediatorValidationResult.Success);
    }

    private sealed class AnotherPingValidator : IPlaxionMediatorValidator<Ping>
    {
        public ValueTask<PlaxionMediatorValidationResult> ValidateAsync(Ping request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(PlaxionMediatorValidationResult.Success);
    }

    [Fact]
    public void AddPlaxionMediatorValidator_Throws_On_Null_Services()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPlaxionMediatorValidator<Ping, PingValidator>(null!));
    }

    [Fact]
    public void AddPlaxionMediatorValidator_Registers_Validator()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorValidator<Ping, PingValidator>();

        using ServiceProvider sp = services.BuildServiceProvider();
        IEnumerable<IPlaxionMediatorValidator<Ping>> validators = sp.GetServices<IPlaxionMediatorValidator<Ping>>();
        Assert.Single(validators);
        Assert.IsType<PingValidator>(validators.Single());
    }

    [Fact]
    public void AddPlaxionMediatorValidator_Is_Idempotent_For_Same_Type()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorValidator<Ping, PingValidator>();
        services.AddPlaxionMediatorValidator<Ping, PingValidator>();

        using ServiceProvider sp = services.BuildServiceProvider();
        Assert.Single(sp.GetServices<IPlaxionMediatorValidator<Ping>>());
    }

    [Fact]
    public void AddPlaxionMediatorValidator_Allows_Multiple_Distinct_Validators()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorValidator<Ping, PingValidator>();
        services.AddPlaxionMediatorValidator<Ping, AnotherPingValidator>();

        using ServiceProvider sp = services.BuildServiceProvider();
        Assert.Equal(2, sp.GetServices<IPlaxionMediatorValidator<Ping>>().Count());
    }

    [Fact]
    public void UsePlaxionMediatorValidationBehavior_Adds_Behavior_To_Options()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorValidationBehavior();

        Assert.Contains(typeof(ValidationBehavior<,>), options.GlobalBehaviors);
        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void UsePlaxionMediatorValidationBehavior_Is_Idempotent()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorValidationBehavior();
        options.UsePlaxionMediatorValidationBehavior();

        Assert.Single(options.GlobalBehaviors);
    }

    [Fact]
    public void GlobalBehaviors_Registers_ValidationBehavior_Open_Generic()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore(o => o.GlobalBehaviors.Add(typeof(ValidationBehavior<,>)));
        services.AddPlaxionMediatorValidator<Ping, PingValidator>();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        IPipelineBehavior<Ping, string>[] behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<Ping, string>>()
            .ToArray();

        Assert.Single(behaviors);
        Assert.IsType<ValidationBehavior<Ping, string>>(behaviors[0]);

        IEnumerable<IPlaxionMediatorValidator<Ping>> validators =
            scope.ServiceProvider.GetServices<IPlaxionMediatorValidator<Ping>>();
        Assert.Single(validators);
    }
}
