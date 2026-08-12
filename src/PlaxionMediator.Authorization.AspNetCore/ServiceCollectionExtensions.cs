using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaxionMediator.Authorization;
using PlaxionMediator.Authorization.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up PlaxionMediator Authorization in an ASP.NET Core application.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ASP.NET Core-specific authorization services to the <see cref="IServiceCollection"/>,
    /// enabling <see cref="ClaimsPrincipal"/>-based authorization context.
    /// </summary>
    /// <remarks>
    /// This method should generally be called AFTER <c>AddPlaxionMediatorAuthorization()</c> to correctly
    /// override the default system accessor with the HTTP-aware one.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddPlaxionMediatorAuthorizationAspNetCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        // REPLACE any existing IAuthorizationContextAccessor (e.g. SystemAuthorizationContextAccessor)
        // with the HttpAuthorizationContextAccessor.
        services.RemoveAll<IAuthorizationContextAccessor>();
        services.AddScoped<IAuthorizationContextAccessor, HttpAuthorizationContextAccessor>();

        return services;
    }
}
