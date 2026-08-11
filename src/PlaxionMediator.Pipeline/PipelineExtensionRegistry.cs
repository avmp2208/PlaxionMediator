using System.Runtime.CompilerServices;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Pipeline;

/// <summary>
/// Process-wide registry of <see cref="IPipelineExtension"/> instances applied around pipeline execution.
/// Empty by default; when empty the hot path incurs only a single length check (zero allocation).
/// </summary>
/// <remarks>
/// Designed for startup registration by first-party packages and advanced consumers. Not a DI
/// container — keep registrations explicit and AOT-safe. Thread-safe for concurrent reads during
/// dispatch; registration is expected at application startup before serving traffic.
/// </remarks>
public static class PipelineExtensionRegistry
{
    private static IPipelineExtension[] s_extensions = [];

    /// <summary>
    /// Currently registered extensions, ordered by <see cref="IPipelineExtension.Order"/> then registration order.
    /// </summary>
    public static IReadOnlyList<IPipelineExtension> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref s_extensions);
    }

    /// <summary>
    /// Returns true when at least one extension is registered.
    /// </summary>
    public static bool HasExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref s_extensions).Length != 0;
    }

    /// <summary>
    /// Registers an extension instance. Re-sorts the active list by <see cref="IPipelineExtension.Order"/>.
    /// </summary>
    public static void Register(IPipelineExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);

        IPipelineExtension[] current = Volatile.Read(ref s_extensions);
        IPipelineExtension[] next = new IPipelineExtension[current.Length + 1];
        Array.Copy(current, next, current.Length);
        next[current.Length] = extension;
        Array.Sort(next, static (a, b) => a.Order.CompareTo(b.Order));
        Volatile.Write(ref s_extensions, next);
    }

    /// <summary>
    /// Replaces the entire extension set with <paramref name="extensions"/> (copied and sorted by Order).
    /// </summary>
    public static void Set(IEnumerable<IPipelineExtension> extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        List<IPipelineExtension> list = [];
        foreach (IPipelineExtension extension in extensions)
        {
            ArgumentNullException.ThrowIfNull(extension);
            list.Add(extension);
        }

        list.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        Volatile.Write(ref s_extensions, list.Count == 0 ? [] : list.ToArray());
    }

    /// <summary>
    /// Clears all registered extensions (primarily for tests).
    /// </summary>
    public static void Clear() => Volatile.Write(ref s_extensions, []);

    /// <summary>
    /// Applies registered extensions around <paramref name="inner"/> when any are present; otherwise returns <paramref name="inner"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static RequestHandlerDelegate<TResponse> ApplyExtensions<TRequest, TResponse>(
        int behaviorCount,
        RequestHandlerDelegate<TResponse> inner)
        where TRequest : IRequest<TResponse>
    {
        IPipelineExtension[] extensions = Volatile.Read(ref s_extensions);
        if (extensions.Length == 0)
        {
            return inner;
        }

        PipelineExtensionContext context = new(typeof(TRequest), typeof(TResponse), behaviorCount);

        // Apply from last to first so lower Order ends up outermost.
        RequestHandlerDelegate<TResponse> next = inner;
        for (int i = extensions.Length - 1; i >= 0; i--)
        {
            next = extensions[i].Apply<TRequest, TResponse>(in context, next);
        }

        return next;
    }
}
