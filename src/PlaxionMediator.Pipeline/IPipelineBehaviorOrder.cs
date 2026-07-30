namespace PlaxionMediator.Pipeline;

/// <summary>
/// Optional contract a behavior implements to declare its own default ordering weight.
/// Lower values run earlier (closer to the caller).
/// </summary>
public interface IPipelineBehaviorOrder
{
    /// <summary>
    /// Relative order weight. Lower runs first.
    /// </summary>
    int Order { get; }
}
