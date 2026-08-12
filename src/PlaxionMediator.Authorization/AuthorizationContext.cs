using System.Security.Claims;

namespace PlaxionMediator.Authorization;

/// <summary>
/// Default implementation of <see cref="IAuthorizationContext"/>.
/// </summary>
public sealed class AuthorizationContext : IAuthorizationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationContext"/> class.
    /// </summary>
    /// <param name="principal">The current principal.</param>
    /// <param name="isAuthenticated">Whether the current caller is authenticated.</param>
    /// <param name="callerKind">Classifies the origin of the current logical operation.</param>
    public AuthorizationContext(ClaimsPrincipal? principal, bool isAuthenticated, CallerKind callerKind)
    {
        Principal = principal;
        IsAuthenticated = isAuthenticated;
        CallerKind = callerKind;
    }

    /// <inheritdoc />
    public ClaimsPrincipal? Principal { get; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <inheritdoc />
    public CallerKind CallerKind { get; }
}
