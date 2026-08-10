namespace PlaxionMediator.AspNetCore;

/// <summary>
/// Options controlling how <see cref="PlaxionMediatorExceptionHandlingMiddleware"/> maps PlaxionMediator
/// exceptions to RFC 7807 <c>problem+json</c> responses.
/// </summary>
public sealed class PlaxionMediatorExceptionHandlingOptions
{
    /// <summary>
    /// When <see langword="true"/>, includes the failing request's type name (<see cref="PlaxionMediator.Core.PipelineExecutionException.RequestTypeName"/>)
    /// as a <c>requestTypeName</c> extension on the <c>problem+json</c> response for pipeline execution failures.
    /// Defaults to <see langword="false"/> to avoid leaking internal type information by default.
    /// </summary>
    public bool IncludeRequestTypeName { get; set; }
}
