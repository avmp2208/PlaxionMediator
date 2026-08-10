namespace PlaxionMediator.Core;

/// <summary>
/// Wraps a behavior/handler fault, preserving which pipeline stage failed when known.
/// </summary>
public sealed class PipelineExecutionException : PlaxionMediatorException
{
    /// <summary>
    /// Initializes a new instance wrapping the given fault.
    /// </summary>
    public PipelineExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance with pipeline stage context.
    /// </summary>
    public PipelineExecutionException(string message, Exception innerException, string? stageName)
        : base(message, innerException)
    {
        StageName = stageName;
    }

    /// <summary>
    /// Initializes a new instance with pipeline stage and request diagnostic context.
    /// </summary>
    public PipelineExecutionException(string message, Exception innerException, string? stageName, string? requestTypeName)
        : base(message, innerException)
    {
        StageName = stageName;
        RequestTypeName = requestTypeName;
    }

    /// <summary>
    /// Optional name of the pipeline stage that failed.
    /// </summary>
    public string? StageName { get; }

    /// <summary>
    /// Optional name of the request type being processed when the failure occurred.
    /// Additive diagnostic context; never populated with request payload data to avoid leaking
    /// sensitive information by default.
    /// </summary>
    public string? RequestTypeName { get; }
}
