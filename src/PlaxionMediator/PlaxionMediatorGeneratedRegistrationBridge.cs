using Microsoft.Extensions.DependencyInjection;

namespace PlaxionMediator;

/// <summary>
/// Bridge invoked by <see cref="PlaxionMediatorServiceCollectionExtensions.AddPlaxionMediator"/>.
/// The source generator assigns <see cref="Register"/> in a module initializer so registration
/// stays zero-reflection and works across assemblies (partial methods cannot span assemblies).
/// </summary>
public static class PlaxionMediatorGeneratedRegistrationBridge
{
    /// <summary>
    /// Optional generated registration callback. Set by generated module initializer code.
    /// </summary>
    public static Action<IServiceCollection, PlaxionMediatorOptions>? Register { get; set; }

    /// <summary>
    /// Invokes the generated registration callback when present.
    /// </summary>
    public static void Invoke(IServiceCollection services, PlaxionMediatorOptions options)
    {
        Register?.Invoke(services, options);
    }
}
