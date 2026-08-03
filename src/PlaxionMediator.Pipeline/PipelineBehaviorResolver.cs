using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Pipeline;

/// <summary>
/// Resolves <see cref="IPipelineBehavior{TRequest,TResponse}"/> instances for generated dispatch code.
/// Caches empty-vs-nonempty metadata and, when safe, scope-local behavior arrays.
/// </summary>
/// <remarks>
/// Instance arrays are cached on the scoped sender only when no Transient pipeline behaviors are
/// registered. Transient registrations always re-resolve per call so DI lifetimes stay correct.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PipelineBehaviorResolver
{
    private enum Mode : byte
    {
        Unknown = 0,
        Empty = 1,
        ResolveEachCall = 2,
        CachePerScope = 3,
    }

    private static readonly ConcurrentDictionary<Type, Mode> Modes = new();
    private static IServiceCollection? s_serviceCollection;
    private static int s_globalCachePolicy; // 0 unknown, 1 cache-per-scope ok, 2 must resolve each call

    /// <summary>
    /// Captures the service collection so lifetime policy can be determined after the container is built.
    /// Safe to call multiple times; the latest collection is kept.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterServiceCollection(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        s_serviceCollection = services;
        // Collection may still receive registrations after AddPlaxionMediator; recompute on first use.
        Volatile.Write(ref s_globalCachePolicy, 0);
    }

    /// <summary>
    /// Returns the behaviors for <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.
    /// </summary>
    /// <param name="services">Request service provider (typically the scoped provider).</param>
    /// <param name="scopeCache">Optional sender-scoped cache dictionary; may be allocated lazily.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> GetBehaviors<TRequest, TResponse>(
        IServiceProvider services,
        ref Dictionary<Type, object>? scopeCache)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        Type key = typeof(IPipelineBehavior<TRequest, TResponse>);

        if (Modes.TryGetValue(key, out Mode mode))
        {
            if (mode == Mode.Empty)
            {
                return Array.Empty<IPipelineBehavior<TRequest, TResponse>>();
            }

            if (mode == Mode.CachePerScope
                && scopeCache is not null
                && scopeCache.TryGetValue(key, out object? cached))
            {
                return (IPipelineBehavior<TRequest, TResponse>[])cached;
            }
        }

        return ResolveSlow<TRequest, TResponse>(services, ref scopeCache, key);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> ResolveSlow<TRequest, TResponse>(
        IServiceProvider services,
        ref Dictionary<Type, object>? scopeCache,
        Type key)
        where TRequest : IRequest<TResponse>
    {
        IPipelineBehavior<TRequest, TResponse>[] behaviors = Materialize(
            services.GetServices<IPipelineBehavior<TRequest, TResponse>>());

        if (behaviors.Length == 0)
        {
            Modes[key] = Mode.Empty;
            return Array.Empty<IPipelineBehavior<TRequest, TResponse>>();
        }

        Mode mode = Modes.GetOrAdd(key, static _ => DetermineMode());

        if (mode == Mode.CachePerScope)
        {
            scopeCache ??= new Dictionary<Type, object>();
            scopeCache[key] = behaviors;
        }

        return behaviors;
    }

    private static Mode DetermineMode()
    {
        int policy = Volatile.Read(ref s_globalCachePolicy);
        if (policy == 0)
        {
            policy = ComputeGlobalCachePolicy();
            Volatile.Write(ref s_globalCachePolicy, policy);
        }

        // 1 = cache per scope allowed; 2 = transient present → resolve each call.
        return policy == 1 ? Mode.CachePerScope : Mode.ResolveEachCall;
    }

    private static int ComputeGlobalCachePolicy()
    {
        IServiceCollection? collection = s_serviceCollection;
        if (collection is null)
        {
            // No collection captured (manual/core registration) — stay conservative.
            return 2;
        }

        // Snapshot count; IServiceCollection is not expected to change after BuildServiceProvider.
        for (int i = 0; i < collection.Count; i++)
        {
            ServiceDescriptor descriptor = collection[i];
            if (!IsPipelineBehaviorService(descriptor.ServiceType))
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

    private static bool IsPipelineBehaviorService(Type serviceType)
    {
        if (!serviceType.IsGenericType)
        {
            return false;
        }

        Type definition = serviceType.IsGenericTypeDefinition
            ? serviceType
            : serviceType.GetGenericTypeDefinition();

        return definition == typeof(IPipelineBehavior<,>);
    }

    private static IPipelineBehavior<TRequest, TResponse>[] Materialize<TRequest, TResponse>(
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> source)
        where TRequest : IRequest<TResponse>
    {
        if (source is IPipelineBehavior<TRequest, TResponse>[] array)
        {
            return array;
        }

        if (source is ICollection<IPipelineBehavior<TRequest, TResponse>> collection)
        {
            int count = collection.Count;
            if (count == 0)
            {
                return Array.Empty<IPipelineBehavior<TRequest, TResponse>>();
            }

            IPipelineBehavior<TRequest, TResponse>[] result = new IPipelineBehavior<TRequest, TResponse>[count];
            collection.CopyTo(result, 0);
            return result;
        }

        // Fallback for lazy DI enumerables that only implement IEnumerable.
        List<IPipelineBehavior<TRequest, TResponse>> list = new(4);
        foreach (IPipelineBehavior<TRequest, TResponse> item in source)
        {
            list.Add(item);
        }

        return list.Count == 0
            ? Array.Empty<IPipelineBehavior<TRequest, TResponse>>()
            : list.ToArray();
    }
}
