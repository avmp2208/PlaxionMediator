using Microsoft.AspNetCore.Builder;

namespace PlaxionMediator.AspNetCore;

/// <summary>
/// ASP.NET Core application builder extensions for PlaxionMediator.
/// </summary>
public static class PlaxionMediatorApplicationBuilderExtensions
{
    /// <summary>
    /// Registers middleware that converts mapped PlaxionMediator exceptions into RFC 7807
    /// <c>application/problem+json</c> responses.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/> instance for chaining.</returns>
    /// <remarks>
    /// Register this middleware <b>before</b> routing, endpoint execution, and
    /// <c>UseRouting</c>/<c>Map*</c> so exceptions thrown by handlers and endpoint delegates are caught.
    /// Only <c>HandlerNotFoundException</c> and <c>PipelineExecutionException</c> are mapped;
    /// all other exceptions are rethrown.
    /// </remarks>
    public static IApplicationBuilder UsePlaxionMediatorExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<PlaxionMediatorExceptionHandlingMiddleware>();
    }
}
