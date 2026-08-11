namespace PlaxionMediator.Pipeline;

/// <summary>
/// Allocation-free call context for <see cref="IPipelineObserver"/> notifications.
/// Contains type metadata and behavior depth only — never request payloads.
/// </summary>
public readonly struct PipelineCallContext
{
    /// <summary>
    /// Initializes a new call context.
    /// </summary>
    public PipelineCallContext(Type requestType, Type responseType, int behaviorCount)
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
    /// The closed response type expected from the handler.
    /// </summary>
    public Type ResponseType { get; }

    /// <summary>
    /// Number of pipeline behaviors in the chain for this call.
    /// </summary>
    public int BehaviorCount { get; }

    /// <summary>
    /// Convenience accessor matching diagnostic context on framework exceptions.
    /// </summary>
    public string RequestTypeName => RequestType.Name;
}
