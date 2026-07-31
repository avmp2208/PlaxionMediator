using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlaxionMediator.Core;

namespace PlaxionMediator.AspNetCore.Tests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_HandlerNotFound_Writes_ProblemJson()
    {
        var exception = new HandlerNotFoundException(typeof(string));
        (int status, string? contentType, string body) = await InvokeAndReadAsync(
            _ => throw exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("application/problem+json", contentType);
        using JsonDocument doc = JsonDocument.Parse(body);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.HandlerNotFoundType, doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.HandlerNotFoundTitle, doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_PipelineExecution_Writes_ProblemJson_With_InnerException()
    {
        var exception = new PipelineExecutionException(
            "pipeline failed",
            new InvalidOperationException("inner-message"),
            "StageA");

        (int status, string? contentType, string body) = await InvokeAndReadAsync(
            _ => throw exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("application/problem+json", contentType);
        using JsonDocument doc = JsonDocument.Parse(body);
        Assert.Equal(PlaxionMediatorProblemDetailsFactory.PipelineExecutionType, doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("StageA", doc.RootElement.GetProperty("stageName").GetString());
        Assert.Equal("inner-message", doc.RootElement.GetProperty("innerException").GetProperty("message").GetString());
        Assert.Equal(
            typeof(InvalidOperationException).FullName,
            doc.RootElement.GetProperty("innerException").GetProperty("type").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnrelatedException_Is_Rethrown()
    {
        var middleware = new PlaxionMediatorExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("not-mapped"));

        DefaultHttpContext context = CreateContext();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.Equal("not-mapped", ex.Message);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_AbstractBaseExceptionSubtype_NotMapped_Is_Rethrown()
    {
        // Concrete custom subtype of the abstract base that is NOT HandlerNotFound/PipelineExecution.
        var middleware = new PlaxionMediatorExceptionHandlingMiddleware(
            _ => throw new CustomPlaxionException("custom"));

        DefaultHttpContext context = CreateContext();

        CustomPlaxionException ex = await Assert.ThrowsAsync<CustomPlaxionException>(
            () => middleware.InvokeAsync(context));

        Assert.Equal("custom", ex.Message);
    }

    [Fact]
    public async Task InvokeAsync_When_Next_Succeeds_Does_Not_Touch_Response()
    {
        var middleware = new PlaxionMediatorExceptionHandlingMiddleware(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            await context.Response.WriteAsync("ok");
        });

        DefaultHttpContext context = CreateContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        Assert.Equal("ok", await reader.ReadToEndAsync());
    }

    private static async Task<(int Status, string? ContentType, string Body)> InvokeAndReadAsync(
        RequestDelegate next)
    {
        var middleware = new PlaxionMediatorExceptionHandlingMiddleware(next);
        DefaultHttpContext context = CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        string body = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, context.Response.ContentType, body);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class CustomPlaxionException : PlaxionMediatorException
    {
        public CustomPlaxionException(string message) : base(message)
        {
        }
    }
}
