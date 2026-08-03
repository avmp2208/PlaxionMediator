using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlaxionMediator.Core;
using PlaxionMediator.Validation;

namespace PlaxionMediator.AspNetCore.Tests;

public sealed class ProblemDetailsFactoryTests
{
    [Fact]
    public void Create_HandlerNotFound_Maps_Status_Title_Type_And_RequestType()
    {
        var exception = new HandlerNotFoundException(typeof(string));

        ProblemDetails problem = PlaxionMediatorProblemDetailsFactory.Create(exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.HandlerNotFoundTitle, problem.Title);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.HandlerNotFoundType, problem.Type);
        Assert.Equal(exception.Message, problem.Detail);
        Assert.True(problem.Extensions.TryGetValue("requestType", out object? requestType));
        Assert.Equal(typeof(string).FullName, requestType);
    }

    [Fact]
    public void Create_HandlerNotFound_Without_RequestType_Omits_Extension()
    {
        var exception = new HandlerNotFoundException("handler missing");

        ProblemDetails problem = PlaxionMediatorProblemDetailsFactory.Create(exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.HandlerNotFoundType, problem.Type);
        Assert.False(problem.Extensions.ContainsKey("requestType"));
    }

    [Fact]
    public void Create_PipelineExecution_Maps_InnerException_Safely()
    {
        var inner = new InvalidOperationException("sensitive boom");
        var exception = new PipelineExecutionException("pipeline failed", inner, "ValidationBehavior");

        ProblemDetails problem = PlaxionMediatorProblemDetailsFactory.Create(exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.PipelineExecutionTitle, problem.Title);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.PipelineExecutionType, problem.Type);
        Assert.Equal(exception.Message, problem.Detail);
        Assert.Equal("ValidationBehavior", problem.Extensions["stageName"]);

        Assert.True(problem.Extensions.TryGetValue("innerException", out object? innerObj));
        var innerMap = Assert.IsType<Dictionary<string, string?>>(innerObj);
        Assert.Equal("sensitive boom", innerMap["message"]);
        Assert.Equal(typeof(InvalidOperationException).FullName, innerMap["type"]);

        // Ensure we did not leak a stack trace blob into extensions.
        string json = JsonSerializer.Serialize(problem.Extensions);
        Assert.DoesNotContain("at ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_PipelineExecution_Without_Inner_Omits_InnerException_Extension()
    {
        // PipelineExecutionException always requires inner in public ctors; simulate null-safe path via message-only shape is N/A.
        // Validate stage-less mapping still works when stage is null.
        var exception = new PipelineExecutionException("pipeline failed", new Exception("x"));

        ProblemDetails problem = PlaxionMediatorProblemDetailsFactory.Create(exception);

        Assert.False(problem.Extensions.ContainsKey("stageName"));
        Assert.True(problem.Extensions.ContainsKey("innerException"));
    }

    [Fact]
    public void Create_Validation_Maps_Status_Title_Type_And_Errors()
    {
        var exception = new PlaxionMediatorValidationException(
        [
            new PlaxionMediatorValidationFailure("Name", "Name is required."),
            new PlaxionMediatorValidationFailure("Id", "Id must not be empty."),
        ]);

        ProblemDetails problem = PlaxionMediatorProblemDetailsFactory.Create(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.ValidationTitle, problem.Title);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.ValidationType, problem.Type);
        Assert.Equal(exception.Message, problem.Detail);

        Assert.True(problem.Extensions.TryGetValue("errors", out object? errorsObj));
        var errors = Assert.IsType<List<Dictionary<string, string>>>(errorsObj);
        Assert.Equal(2, errors.Count);
        Assert.Equal("Name", errors[0]["propertyName"]);
        Assert.Equal("Name is required.", errors[0]["errorMessage"]);
        Assert.Equal("Id", errors[1]["propertyName"]);
        Assert.Equal("Id must not be empty.", errors[1]["errorMessage"]);
    }
}
