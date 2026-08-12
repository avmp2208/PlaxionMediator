using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PlaxionMediator.Authorization.AspNetCore;

/// <summary>
/// An <see cref="IAuthorizationContextAccessor"/> that retrieves the <see cref="IAuthorizationContext"/>
/// from the current <see cref="HttpContext"/>.
/// </summary>
public sealed class HttpAuthorizationContextAccessor : IAuthorizationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpAuthorizationContextAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContextAccessor"/> is null.</exception>
    public HttpAuthorizationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public IAuthorizationContext Current
    {
        get
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return new AuthorizationContext(principal: null, isAuthenticated: false, CallerKind.Unknown);
            }

            ClaimsPrincipal principal = httpContext.User;
            bool isAuthenticated = principal.Identity?.IsAuthenticated ?? false;

            return new AuthorizationContext(principal, isAuthenticated, CallerKind.Http);
        }
    }
}
