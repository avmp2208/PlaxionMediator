using Microsoft.Extensions.DependencyInjection;

namespace PlaxionMediator.DependencyInjection;

/// <summary>
/// Central options object controlling default lifetimes and global behavior ordering.
/// </summary>
public sealed class PlaxionMediatorOptions
{
    /// <summary>
    /// Default DI lifetime for discovered request/notification handlers.
    /// </summary>
    public ServiceLifetime DefaultHandlerLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Default DI lifetime for discovered pipeline behaviors.
    /// </summary>
    public ServiceLifetime DefaultBehaviorLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Explicit, ordered list of open-generic or closed behavior types applied to every request.
    /// </summary>
    public IList<Type> GlobalBehaviors { get; } = new List<Type>();
}
