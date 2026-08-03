using PlaxionMediator.Core;

namespace PlaxionMediator.Validation.Tests;

public sealed class ValidationResultTests
{
    [Fact]
    public void Success_IsValid_And_Has_No_Failures()
    {
        Assert.True(PlaxionMediatorValidationResult.Success.IsValid);
        Assert.Empty(PlaxionMediatorValidationResult.Success.Failures);
        Assert.Same(PlaxionMediatorValidationResult.Success, PlaxionMediatorValidationResult.Success);
    }

    [Fact]
    public void Failed_Creates_Invalid_Result_With_Failures()
    {
        PlaxionMediatorValidationFailure failure = new("Name", "required");
        PlaxionMediatorValidationResult result = PlaxionMediatorValidationResult.Failed(failure);

        Assert.False(result.IsValid);
        Assert.Single(result.Failures);
        Assert.Equal("Name", result.Failures[0].PropertyName);
        Assert.Equal("required", result.Failures[0].ErrorMessage);
    }

    [Fact]
    public void Failed_Throws_On_Null_Or_Empty()
    {
        Assert.Throws<ArgumentNullException>(() => PlaxionMediatorValidationResult.Failed((IEnumerable<PlaxionMediatorValidationFailure>)null!));
        Assert.Throws<ArgumentException>(() => PlaxionMediatorValidationResult.Failed(Array.Empty<PlaxionMediatorValidationFailure>()));
        Assert.Throws<ArgumentException>(() => PlaxionMediatorValidationResult.Failed([null!]));
    }

    [Fact]
    public void Failure_Null_PropertyName_Becomes_Empty_And_Null_Message_Throws()
    {
        PlaxionMediatorValidationFailure failure = new(null!, "msg");
        Assert.Equal(string.Empty, failure.PropertyName);
        Assert.Throws<ArgumentNullException>(() => new PlaxionMediatorValidationFailure("Name", null!));
    }

    [Fact]
    public void Exception_Carries_Failures_And_Derives_From_Base()
    {
        PlaxionMediatorValidationFailure failure = new("Id", "empty");
        PlaxionMediatorValidationException ex = new([failure]);

        Assert.IsAssignableFrom<PlaxionMediatorException>(ex);
        Assert.Single(ex.Failures);
        Assert.Equal("Id", ex.Failures[0].PropertyName);
        Assert.Contains("Id", ex.Message, StringComparison.Ordinal);
        Assert.Contains("empty", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Exception_Throws_On_Null_Or_Empty_Failures()
    {
        Assert.Throws<ArgumentNullException>(() => new PlaxionMediatorValidationException(null!));
        Assert.Throws<ArgumentException>(() => new PlaxionMediatorValidationException(Array.Empty<PlaxionMediatorValidationFailure>()));
    }

    [Fact]
    public void Exception_Custom_Message_Is_Preserved()
    {
        PlaxionMediatorValidationException ex = new(
            "custom",
            [new PlaxionMediatorValidationFailure("Name", "bad")]);

        Assert.Equal("custom", ex.Message);
        Assert.Single(ex.Failures);
    }
}
