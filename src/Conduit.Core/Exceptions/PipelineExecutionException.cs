namespace Conduit.Core;

/// <summary>
/// Wraps a behavior/handler fault, preserving which pipeline stage failed when known.
/// </summary>
public sealed class PipelineExecutionException : ConduitException
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
    /// Optional name of the pipeline stage that failed.
    /// </summary>
    public string? StageName { get; }
}
