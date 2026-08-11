namespace PlaxionMediator.Pipeline;

/// <summary>
/// Fluent builder used to declare a fixed extension order for custom pipeline composition.
/// Mirrors <see cref="PipelineBuilder"/> for the <see cref="IPipelineExtension"/> surface.
/// </summary>
/// <remarks>
/// This builder records extension <em>types</em> for documentation, DI wiring, and analyzer
/// validation (<c>PlaxionMediator024</c>/<c>PlaxionMediator025</c>). Runtime application uses
/// concrete instances registered on <see cref="PipelineExtensionRegistry"/>.
/// </remarks>
public sealed class PipelineExtensionBuilder
{
    private readonly List<Type> _extensions = [];

    /// <summary>
    /// Extensions registered via this builder, in registration order.
    /// </summary>
    public IReadOnlyList<Type> Extensions => _extensions;

    /// <summary>
    /// Appends an extension type to the composition list.
    /// </summary>
    /// <typeparam name="TExtension">
    /// A concrete type that should implement <see cref="IPipelineExtension"/>.
    /// Enforced at build time by analyzer <c>PlaxionMediator024</c> (mirrors <see cref="PipelineBuilder.Use{TBehavior}"/>).
    /// </typeparam>
    public PipelineExtensionBuilder Use<TExtension>()
        where TExtension : notnull
    {
        _extensions.Add(typeof(TExtension));
        return this;
    }

    /// <summary>
    /// Appends an extension type when <paramref name="predicate"/> returns true.
    /// </summary>
    public PipelineExtensionBuilder UseWhen<TExtension>(Func<Type, bool> predicate)
        where TExtension : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (predicate(typeof(TExtension)))
        {
            _extensions.Add(typeof(TExtension));
        }

        return this;
    }
}
