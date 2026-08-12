using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PlaxionMediator.Authorization.AspNetCore;

/// <summary>
/// An <see cref="IRequestAuthorization{TRequest}"/> that bridges to Microsoft's
/// policy-based authorization system (<see cref="IAuthorizationService"/>).
/// </summary>
/// <typeparam name="TRequest">The type of the request being authorized.</typeparam>
public sealed class MicrosoftAuthorizationPolicyCheck<TRequest> : IRequestAuthorization<TRequest>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly string _policyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftAuthorizationPolicyCheck{TRequest}"/> class.
    /// </summary>
    /// <param name="authorizationService">The Microsoft authorization service.</param>
    /// <param name="policyName">The name of the policy to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="authorizationService"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="policyName"/> is null or empty.</exception>
    public MicrosoftAuthorizationPolicyCheck(IAuthorizationService authorizationService, string policyName)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));
        }

        _policyName = policyName;
    }

    /// <inheritdoc />
    public async ValueTask<AuthorizationOutcome> AuthorizeAsync(TRequest request, IAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        ClaimsPrincipal? principal = context.Principal;

        if (principal is null || !context.IsAuthenticated)
        {
            return AuthorizationOutcome.Unauthenticated;
        }

        AuthorizationResult result = await _authorizationService.AuthorizeAsync(principal, resource: request, _policyName).ConfigureAwait(false);

        return result.Succeeded ? AuthorizationOutcome.Authorized : AuthorizationOutcome.Forbidden;
    }
}
