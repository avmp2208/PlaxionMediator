using System.Runtime.CompilerServices;

namespace PlaxionMediator.Pipeline;

/// <summary>
/// Process-wide hub for <see cref="INotificationObserver"/> instrumentation subscribers (ADR-0009).
/// No-op and zero-allocation when no observers are registered (single length check on the hot path).
/// </summary>
public static class NotificationObserverHub
{
    private static INotificationObserver[] s_observers = [];

    /// <summary>
    /// Currently registered observers in registration order.
    /// </summary>
    public static IReadOnlyList<INotificationObserver> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref s_observers);
    }

    /// <summary>
    /// Returns true when at least one observer is registered.
    /// </summary>
    public static bool HasObservers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref s_observers).Length != 0;
    }

    /// <summary>
    /// Registers an observer. Duplicate instance registrations are ignored.
    /// </summary>
    public static void Register(INotificationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        INotificationObserver[] current = Volatile.Read(ref s_observers);
        for (int i = 0; i < current.Length; i++)
        {
            if (ReferenceEquals(current[i], observer))
            {
                return;
            }
        }

        INotificationObserver[] next = new INotificationObserver[current.Length + 1];
        Array.Copy(current, next, current.Length);
        next[current.Length] = observer;
        Volatile.Write(ref s_observers, next);
    }

    /// <summary>
    /// Removes a previously registered observer instance.
    /// </summary>
    public static void Unregister(INotificationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        INotificationObserver[] current = Volatile.Read(ref s_observers);
        int index = -1;
        for (int i = 0; i < current.Length; i++)
        {
            if (ReferenceEquals(current[i], observer))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        if (current.Length == 1)
        {
            Volatile.Write(ref s_observers, []);
            return;
        }

        INotificationObserver[] next = new INotificationObserver[current.Length - 1];
        if (index > 0)
        {
            Array.Copy(current, 0, next, 0, index);
        }

        if (index < current.Length - 1)
        {
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
        }

        Volatile.Write(ref s_observers, next);
    }

    /// <summary>
    /// Clears all observers (primarily for tests).
    /// </summary>
    public static void Clear() => Volatile.Write(ref s_observers, []);

    /// <summary>
    /// Notifies all registered observers that a notification publish is starting.
    /// Public because the source generator emits direct calls to this method from consumer assemblies
    /// (there is no shared internal composer for <c>Publish</c> the way there is for <c>Send</c>).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyStarting(in NotificationCallContext context)
    {
        INotificationObserver[] observers = Volatile.Read(ref s_observers);
        for (int i = 0; i < observers.Length; i++)
        {
            observers[i].OnStarting(in context);
        }
    }

    /// <summary>
    /// Notifies all registered observers that a notification publish completed successfully.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyCompleted(in NotificationCallContext context)
    {
        INotificationObserver[] observers = Volatile.Read(ref s_observers);
        for (int i = 0; i < observers.Length; i++)
        {
            observers[i].OnCompleted(in context);
        }
    }

    /// <summary>
    /// Notifies all registered observers that a notification publish faulted.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotifyFaulted(in NotificationCallContext context, Exception exception)
    {
        INotificationObserver[] observers = Volatile.Read(ref s_observers);
        for (int i = 0; i < observers.Length; i++)
        {
            observers[i].OnFaulted(in context, exception);
        }
    }
}
