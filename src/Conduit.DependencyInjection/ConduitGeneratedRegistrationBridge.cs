using Microsoft.Extensions.DependencyInjection;

namespace Conduit.DependencyInjection;

/// <summary>
/// Bridge invoked by <see cref="ConduitServiceCollectionExtensions.AddConduit"/>.
/// The source generator assigns <see cref="Register"/> in a module initializer so registration
/// stays zero-reflection and works across assemblies (partial methods cannot span assemblies).
/// </summary>
public static class ConduitGeneratedRegistrationBridge
{
    /// <summary>
    /// Optional generated registration callback. Set by generated module initializer code.
    /// </summary>
    public static Action<IServiceCollection, ConduitOptions>? Register { get; set; }

    /// <summary>
    /// Invokes the generated registration callback when present.
    /// </summary>
    public static void Invoke(IServiceCollection services, ConduitOptions options)
    {
        Register?.Invoke(services, options);
    }
}
