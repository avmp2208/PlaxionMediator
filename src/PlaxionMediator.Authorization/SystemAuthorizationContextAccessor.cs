namespace PlaxionMediator.Authorization;

/// <summary>
/// Default implementation of <see cref="IAuthorizationContextAccessor"/> for non-HTTP hosts.
/// </summary>
public sealed class SystemAuthorizationContextAccessor : IAuthorizationContextAccessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemAuthorizationContextAccessor"/> class
    /// with the default <see cref="CallerKind.Background"/>.
    /// </summary>
    public SystemAuthorizationContextAccessor()
        : this(CallerKind.Background)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemAuthorizationContextAccessor"/> class
    /// with a specific <see cref="CallerKind"/>.
    /// </summary>
    /// <param name="callerKind">The kind of caller to report.</param>
    public SystemAuthorizationContextAccessor(CallerKind callerKind)
    {
        Current = new AuthorizationContext(null, isAuthenticated: false, callerKind);
    }

    /// <inheritdoc />
    public IAuthorizationContext Current { get; }
}
