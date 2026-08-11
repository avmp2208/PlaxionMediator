namespace PlaxionMediator.Pipeline;

/// <summary>
/// Lightweight, allocation-free context supplied to <see cref="IPipelineExtension"/> during composition.
/// Contains type metadata only — never request payloads — so extensions remain safe for logging/telemetry.
/// </summary>
public readonly struct PipelineExtensionContext
{
    /// <summary>
    /// Initializes a new context.
    /// </summary>
    public PipelineExtensionContext(Type requestType, Type responseType, int behaviorCount)
    {
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        BehaviorCount = behaviorCount;
    }

    /// <summary>
    /// The closed request type being dispatched.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// The closed response type produced by the handler.
    /// </summary>
    public Type ResponseType { get; }

    /// <summary>
    /// Number of <c>IPipelineBehavior&lt;,&gt;</c> instances in the inner chain.
    /// </summary>
    public int BehaviorCount { get; }
}
