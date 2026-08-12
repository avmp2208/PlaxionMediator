using System.Security.Claims;

namespace PlaxionMediator.Authorization;

/// <summary>
/// Minimal, transport-neutral execution identity/context available to <see cref="IRequestAuthorization{TRequest}"/> checks.
/// </summary>
public interface IAuthorizationContext
{
    /// <summary>The current principal, or <see langword="null"/> when there is none (e.g. system/background execution).</summary>
    ClaimsPrincipal? Principal { get; }

    /// <summary>Whether the current caller is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Classifies the origin of the current logical operation.</summary>
    CallerKind CallerKind { get; }
}
