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
        return app.UseMiddleware<PlaxionMediatorExceptionHandlingMiddleware>(new PlaxionMediatorExceptionHandlingOptions());
    }

    /// <summary>
    /// Registers middleware that converts mapped PlaxionMediator exceptions into RFC 7807
    /// <c>application/problem+json</c> responses, using the supplied <paramref name="configure"/> delegate
    /// to opt into additional diagnostic fields (e.g. <see cref="PlaxionMediatorExceptionHandlingOptions.IncludeRequestTypeName"/>).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">A delegate that configures <see cref="PlaxionMediatorExceptionHandlingOptions"/>.</param>
    /// <returns>The same <paramref name="app"/> instance for chaining.</returns>
    public static IApplicationBuilder UsePlaxionMediatorExceptionHandling(
        this IApplicationBuilder app,
        Action<PlaxionMediatorExceptionHandlingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new PlaxionMediatorExceptionHandlingOptions();
        configure(options);
        return app.UseMiddleware<PlaxionMediatorExceptionHandlingMiddleware>(options);
    }
}
