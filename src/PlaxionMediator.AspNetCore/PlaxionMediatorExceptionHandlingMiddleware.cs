using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlaxionMediator.Authorization;
using PlaxionMediator.Core;
using PlaxionMediator.Validation;

namespace PlaxionMediator.AspNetCore;

/// <summary>
/// Catches mapped PlaxionMediator exceptions and writes RFC 7807 problem details responses.
/// Unmapped exceptions are rethrown.
/// </summary>
internal sealed class PlaxionMediatorExceptionHandlingMiddleware
{
    private const string ProblemJsonContentType = "application/problem+json";

    private readonly RequestDelegate _next;
    private readonly PlaxionMediatorExceptionHandlingOptions _options;

    public PlaxionMediatorExceptionHandlingMiddleware(RequestDelegate next)
        : this(next, new PlaxionMediatorExceptionHandlingOptions())
    {
    }

    public PlaxionMediatorExceptionHandlingMiddleware(RequestDelegate next, PlaxionMediatorExceptionHandlingOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (PlaxionMediatorValidationException ex)
        {
            await WriteProblemDetailsAsync(context, PlaxionMediatorProblemDetailsFactory.Create(ex)).ConfigureAwait(false);
        }
        catch (PlaxionMediatorUnauthenticatedException ex)
        {
            await WriteProblemDetailsAsync(context, PlaxionMediatorProblemDetailsFactory.Create(ex)).ConfigureAwait(false);
        }
        catch (PlaxionMediatorForbiddenException ex)
        {
            await WriteProblemDetailsAsync(context, PlaxionMediatorProblemDetailsFactory.Create(ex)).ConfigureAwait(false);
        }
        catch (HandlerNotFoundException ex)
        {
            await WriteProblemDetailsAsync(context, PlaxionMediatorProblemDetailsFactory.Create(ex)).ConfigureAwait(false);
        }
        catch (PipelineExecutionException ex)
        {
            await WriteProblemDetailsAsync(context, PlaxionMediatorProblemDetailsFactory.Create(ex, _options.IncludeRequestTypeName)).ConfigureAwait(false);
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "ProblemDetails is a known ASP.NET Core type; consumers enable JSON options at the host level when needed.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "ProblemDetails is a known ASP.NET Core type; consumers enable JSON options at the host level when needed.")]
    private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails problemDetails)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException(
                "The response has already started; PlaxionMediator exception handling cannot write problem details.");
        }

        context.Response.Clear();
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response
            .WriteAsJsonAsync(problemDetails, options: null, contentType: ProblemJsonContentType)
            .ConfigureAwait(false);
    }
}
