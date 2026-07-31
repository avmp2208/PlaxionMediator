using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlaxionMediator.Core;

namespace PlaxionMediator.AspNetCore;

/// <summary>
/// Maps PlaxionMediator exceptions to RFC 7807 <see cref="ProblemDetails"/> responses.
/// </summary>
internal static class PlaxionMediatorProblemDetailsFactory
{
    internal const string HandlerNotFoundType = "https://plaxionmediator.dev/errors/handler-not-found";
    internal const string PipelineExecutionType = "https://plaxionmediator.dev/errors/pipeline-execution";

    internal const string HandlerNotFoundTitle =
        "A required PlaxionMediator handler could not be resolved at runtime — this indicates a build-time invariant was violated.";

    internal const string PipelineExecutionTitle =
        "A PlaxionMediator pipeline stage failed while handling the request.";

    /// <summary>
    /// Creates problem details for a missing handler failure.
    /// </summary>
    public static ProblemDetails Create(HandlerNotFoundException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = HandlerNotFoundTitle,
            Type = HandlerNotFoundType,
            Detail = exception.Message,
        };

        if (exception.RequestType is not null)
        {
            problemDetails.Extensions["requestType"] = exception.RequestType.FullName ?? exception.RequestType.Name;
        }

        return problemDetails;
    }

    /// <summary>
    /// Creates problem details for a pipeline execution failure, exposing a safe inner-exception summary.
    /// </summary>
    public static ProblemDetails Create(PipelineExecutionException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = PipelineExecutionTitle,
            Type = PipelineExecutionType,
            Detail = exception.Message,
        };

        if (exception.StageName is not null)
        {
            problemDetails.Extensions["stageName"] = exception.StageName;
        }

        if (exception.InnerException is not null)
        {
            problemDetails.Extensions["innerException"] = new Dictionary<string, string?>
            {
                ["message"] = exception.InnerException.Message,
                ["type"] = exception.InnerException.GetType().FullName ?? exception.InnerException.GetType().Name,
            };
        }

        return problemDetails;
    }
}
