using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Pipeline;

/// <summary>
/// Formal extension point for custom composition around the pipeline delegate chain.
/// Extensions wrap the already-composed behavior+handler chain as outer layers and do not
/// replace <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
/// </summary>
/// <remarks>
/// Register instances via <see cref="PipelineExtensionRegistry"/>. Prefer declaring intended
/// extension types with <see cref="PipelineExtensionBuilder"/> so analyzers can validate the
/// registration surface at build time. Implementations must be allocation-conscious and
/// reflection-free for Native AOT / trim compatibility.
/// </remarks>
public interface IPipelineExtension
{
    /// <summary>
    /// Relative order weight. Lower values run earlier (outermost, closer to the caller).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Wraps <paramref name="next"/> with extension-specific composition logic.
    /// </summary>
    /// <typeparam name="TRequest">Request type flowing through the pipeline.</typeparam>
    /// <typeparam name="TResponse">Response type produced by the pipeline.</typeparam>
    /// <param name="context">Lightweight composition context (types + behavior depth).</param>
    /// <param name="next">The next delegate in the extension chain (ultimately the behavior pipeline).</param>
    /// <returns>A delegate that invokes this extension then <paramref name="next"/>.</returns>
    RequestHandlerDelegate<TResponse> Apply<TRequest, TResponse>(
        in PipelineExtensionContext context,
        RequestHandlerDelegate<TResponse> next)
        where TRequest : IRequest<TResponse>;
}
