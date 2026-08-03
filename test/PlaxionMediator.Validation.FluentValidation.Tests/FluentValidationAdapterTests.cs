using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PlaxionMediator.Validation.FluentValidation.Tests;

public sealed class FluentValidationAdapterTests
{
    private sealed record CreateThing(string Name, Guid Id);

    private sealed class NameValidator : AbstractValidator<CreateThing>
    {
        public NameValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(10);
        }
    }

    private sealed class IdValidator : AbstractValidator<CreateThing>
    {
        public IdValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    [Fact]
    public void Constructor_Throws_On_Null_Validators()
    {
        Assert.Throws<ArgumentNullException>(() => new FluentValidationAdapter<CreateThing>(null!));
    }

    [Fact]
    public async Task ValidateAsync_Throws_On_Null_Request()
    {
        FluentValidationAdapter<CreateThing> adapter = new([new NameValidator()]);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            adapter.ValidateAsync(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ValidateAsync_Success_When_All_Valid()
    {
        FluentValidationAdapter<CreateThing> adapter = new([new NameValidator(), new IdValidator()]);
        PlaxionMediatorValidationResult result = await adapter.ValidateAsync(
            new CreateThing("Widget", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task ValidateAsync_Maps_Single_Failure()
    {
        FluentValidationAdapter<CreateThing> adapter = new([new NameValidator()]);
        PlaxionMediatorValidationResult result = await adapter.ValidateAsync(
            new CreateThing("", Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Name");
    }

    [Fact]
    public async Task ValidateAsync_Aggregates_Across_Multiple_Fluent_Validators()
    {
        FluentValidationAdapter<CreateThing> adapter = new([new NameValidator(), new IdValidator()]);
        PlaxionMediatorValidationResult result = await adapter.ValidateAsync(
            new CreateThing("", Guid.Empty),
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Name");
        Assert.Contains(result.Failures, f => f.PropertyName == "Id");
    }

    [Fact]
    public async Task ValidateAsync_Maps_MaxLength_Failure()
    {
        FluentValidationAdapter<CreateThing> adapter = new([new NameValidator()]);
        PlaxionMediatorValidationResult result = await adapter.ValidateAsync(
            new CreateThing(new string('a', 11), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Name");
    }

    [Fact]
    public async Task ValidateAsync_Respects_Cancellation()
    {
        FluentValidationAdapter<CreateThing> adapter = new([new NameValidator()]);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.ValidateAsync(new CreateThing("x", Guid.NewGuid()), cts.Token).AsTask());
    }

    [Fact]
    public async Task ValidateAsync_No_Fluent_Validators_Returns_Success()
    {
        FluentValidationAdapter<CreateThing> adapter = new(Array.Empty<IValidator<CreateThing>>());
        PlaxionMediatorValidationResult result = await adapter.ValidateAsync(
            new CreateThing("", Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddPlaxionMediatorFluentValidator_Registers_Adapter_And_Validator()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorFluentValidator<CreateThing, NameValidator>();

        using ServiceProvider sp = services.BuildServiceProvider();

        Assert.Single(sp.GetServices<IValidator<CreateThing>>());
        IPlaxionMediatorValidator<CreateThing> adapter =
            Assert.Single(sp.GetServices<IPlaxionMediatorValidator<CreateThing>>());
        Assert.IsType<FluentValidationAdapter<CreateThing>>(adapter);
    }

    [Fact]
    public void AddPlaxionMediatorFluentValidator_Throws_On_Null_Services()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPlaxionMediatorFluentValidator<CreateThing, NameValidator>(null!));
    }

    [Fact]
    public void AddPlaxionMediatorFluentValidation_Scans_Assembly_And_Registers()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorFluentValidation(typeof(FluentValidationAdapterTests).Assembly);

        using ServiceProvider sp = services.BuildServiceProvider();
        IReadOnlyList<IValidator<CreateThing>> validators = sp.GetServices<IValidator<CreateThing>>().ToList();
        Assert.Equal(2, validators.Count);
        Assert.Contains(validators, v => v is NameValidator);
        Assert.Contains(validators, v => v is IdValidator);

        IPlaxionMediatorValidator<CreateThing> adapter =
            Assert.Single(sp.GetServices<IPlaxionMediatorValidator<CreateThing>>());
        Assert.IsType<FluentValidationAdapter<CreateThing>>(adapter);
    }

    [Fact]
    public void AddPlaxionMediatorFluentValidation_Throws_On_Empty_Assemblies()
    {
        ServiceCollection services = new();
        Assert.Throws<ArgumentException>(() => services.AddPlaxionMediatorFluentValidation());
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPlaxionMediatorFluentValidation(null!));
    }

    [Fact]
    public async Task Registered_Adapter_Aggregates_Scanned_Validators()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorFluentValidation(typeof(FluentValidationAdapterTests).Assembly);

        using ServiceProvider sp = services.BuildServiceProvider();
        IPlaxionMediatorValidator<CreateThing> adapter =
            sp.GetRequiredService<IPlaxionMediatorValidator<CreateThing>>();

        PlaxionMediatorValidationResult result = await adapter.ValidateAsync(
            new CreateThing("", Guid.Empty),
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Failures, f => f.PropertyName == "Name");
        Assert.Contains(result.Failures, f => f.PropertyName == "Id");
    }
}
