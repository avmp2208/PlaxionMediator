using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Pipeline;

/// <summary>
/// Lifetime-aware helper for caching request handlers on the scoped generated sender.
/// </summary>
/// <remarks>
/// Handlers are cached per sender scope only when no Transient <see cref="IRequestHandler{TRequest,TResponse}"/>
/// registrations exist. Transient handlers are always resolved from DI on each call.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RequestHandlerResolver
{
    private static IServiceCollection? s_serviceCollection;
    private static int s_cachePolicy; // 0 unknown, 1 cache-per-scope ok, 2 resolve each call

    /// <summary>
    /// Captures the service collection used to decide whether handler instance caching is safe.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterServiceCollection(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        s_serviceCollection = services;
        Volatile.Write(ref s_cachePolicy, 0);
    }

    /// <summary>
    /// Returns whether the generated sender may cache handler instances for the lifetime of the scope.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanCacheHandlersPerScope()
    {
        int policy = Volatile.Read(ref s_cachePolicy);
        if (policy == 0)
        {
            policy = ComputeCachePolicy();
            Volatile.Write(ref s_cachePolicy, policy);
        }

        return policy == 1;
    }

    private static int ComputeCachePolicy()
    {
        IServiceCollection? collection = s_serviceCollection;
        if (collection is null)
        {
            return 2;
        }

        for (int i = 0; i < collection.Count; i++)
        {
            ServiceDescriptor descriptor = collection[i];
            if (!IsRequestHandlerService(descriptor.ServiceType))
            {
                continue;
            }

            if (descriptor.Lifetime == ServiceLifetime.Transient)
            {
                return 2;
            }
        }

        return 1;
    }

    private static bool IsRequestHandlerService(Type serviceType)
    {
        if (!serviceType.IsGenericType)
        {
            return false;
        }

        Type definition = serviceType.IsGenericTypeDefinition
            ? serviceType
            : serviceType.GetGenericTypeDefinition();

        return definition == typeof(IRequestHandler<,>);
    }
}
