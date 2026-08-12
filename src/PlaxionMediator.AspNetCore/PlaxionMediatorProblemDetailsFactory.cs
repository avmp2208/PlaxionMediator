using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlaxionMediator.Authorization;
using PlaxionMediator.Core;
using PlaxionMediator.Validation;

namespace PlaxionMediator.AspNetCore;

/// <summary>
/// Maps PlaxionMediator exceptions to RFC 7807 <see cref="ProblemDetails"/> responses.
/// </summary>
internal static class PlaxionMediatorProblemDetailsFactory
{
    internal const string HandlerNotFoundType = "https://plaxionmediator.dev/errors/handler-not-found";
    internal const string PipelineExecutionType = "https://plaxionmediator.dev/errors/pipeline-execution";
    internal const string ValidationType = "https://plaxionmediator.dev/errors/validation";
    internal const string UnauthenticatedType = "https://plaxionmediator.dev/errors/unauthenticated";
    internal const string ForbiddenType = "https://plaxionmediator.dev/errors/forbidden";

    internal const string HandlerNotFoundTitle =
        "A required PlaxionMediator handler could not be resolved at runtime — this indicates a build-time invariant was violated.";

    internal const string PipelineExecutionTitle =
        "A PlaxionMediator pipeline stage failed while handling the request.";

    internal const string ValidationTitle =
        "One or more validation errors occurred.";

    internal const string UnauthenticatedTitle =
        "The caller is not authenticated.";

    internal const string ForbiddenTitle =
        "The caller is not authorized to perform this action.";

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
    public static ProblemDetails Create(PipelineExecutionException exception, bool includeRequestTypeName = false)
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

        if (includeRequestTypeName && exception.RequestTypeName is not null)
        {
            problemDetails.Extensions["requestTypeName"] = exception.RequestTypeName;
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

    /// <summary>
    /// Creates problem details for a request validation failure (HTTP 400).
    /// </summary>
    public static ProblemDetails Create(PlaxionMediatorValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var errors = new List<Dictionary<string, string>>(exception.Failures.Count);
        foreach (PlaxionMediatorValidationFailure failure in exception.Failures)
        {
            errors.Add(new Dictionary<string, string>
            {
                ["propertyName"] = failure.PropertyName,
                ["errorMessage"] = failure.ErrorMessage,
            });
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ValidationTitle,
            Type = ValidationType,
            Detail = exception.Message,
            Extensions =
            {
                ["errors"] = errors,
            },
        };
    }

    /// <summary>
    /// Creates problem details for an unauthenticated failure (HTTP 401).
    /// </summary>
    public static ProblemDetails Create(PlaxionMediatorUnauthenticatedException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = UnauthenticatedTitle,
            Type = UnauthenticatedType,
            Detail = exception.Message,
            Extensions =
            {
                ["requestTypeName"] = exception.RequestTypeName,
            },
        };
    }

    /// <summary>
    /// Creates problem details for a forbidden failure (HTTP 403).
    /// </summary>
    public static ProblemDetails Create(PlaxionMediatorForbiddenException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = ForbiddenTitle,
            Type = ForbiddenType,
            Detail = exception.Message,
            Extensions =
            {
                ["requestTypeName"] = exception.RequestTypeName,
            },
        };
    }
}
