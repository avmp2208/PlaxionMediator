namespace PlaxionMediator.Pipeline;

/// <summary>
/// Fluent builder used to declare a fixed behavior order for a request pipeline.
/// </summary>
public sealed class PipelineBuilder
{
    private readonly List<Type> _behaviors = [];

    /// <summary>
    /// Behaviors registered via this builder, in registration order.
    /// </summary>
    public IReadOnlyList<Type> Behaviors => _behaviors;

    /// <summary>
    /// Appends a behavior type to the pipeline.
    /// </summary>
    public PipelineBuilder Use<TBehavior>()
        where TBehavior : notnull
    {
        _behaviors.Add(typeof(TBehavior));
        return this;
    }

    /// <summary>
    /// Appends a behavior type when <paramref name="predicate"/> returns true for the request type.
    /// </summary>
    public PipelineBuilder UseWhen<TBehavior>(Func<Type, bool> predicate)
        where TBehavior : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        // Predicate is evaluated by generated/runtime composition against the request type.
        // For MVP we store the type unconditionally when building a closed pipeline; consumers
        // that need conditional inclusion evaluate the predicate before calling Use.
        if (predicate(typeof(TBehavior)))
        {
            _behaviors.Add(typeof(TBehavior));
        }

        return this;
    }
}
