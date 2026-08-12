using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace PlaxionMediator.Authorization.AspNetCore.Tests;

public sealed class MicrosoftAuthorizationPolicyCheckTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenPrincipalIsNull_ReturnsUnauthenticated()
    {
        // Arrange
        var service = new FakeAuthorizationService();
        var check = new MicrosoftAuthorizationPolicyCheck<string>(service, "MyPolicy");
        var context = new AuthorizationContext(null, false, CallerKind.Http);

        // Act
        AuthorizationOutcome outcome = await check.AuthorizeAsync("request", context, CancellationToken.None);

        // Assert
        Assert.Equal(AuthorizationOutcome.Unauthenticated, outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenNotAuthenticated_ReturnsUnauthenticated()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var service = new FakeAuthorizationService();
        var check = new MicrosoftAuthorizationPolicyCheck<string>(service, "MyPolicy");
        var context = new AuthorizationContext(principal, false, CallerKind.Http);

        // Act
        AuthorizationOutcome outcome = await check.AuthorizeAsync("request", context, CancellationToken.None);

        // Assert
        Assert.Equal(AuthorizationOutcome.Unauthenticated, outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPolicySucceeds_ReturnsAuthorized()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity("Auth"));
        var service = new FakeAuthorizationService { Succeeded = true };
        var check = new MicrosoftAuthorizationPolicyCheck<string>(service, "MyPolicy");
        var context = new AuthorizationContext(principal, true, CallerKind.Http);

        // Act
        AuthorizationOutcome outcome = await check.AuthorizeAsync("request", context, CancellationToken.None);

        // Assert
        Assert.Equal(AuthorizationOutcome.Authorized, outcome);
        Assert.Equal("MyPolicy", service.LastPolicyName);
        Assert.Same(principal, service.LastPrincipal);
        Assert.Equal("request", service.LastResource);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenPolicyFails_ReturnsForbidden()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity("Auth"));
        var service = new FakeAuthorizationService { Succeeded = false };
        var check = new MicrosoftAuthorizationPolicyCheck<string>(service, "MyPolicy");
        var context = new AuthorizationContext(principal, true, CallerKind.Http);

        // Act
        AuthorizationOutcome outcome = await check.AuthorizeAsync("request", context, CancellationToken.None);

        // Assert
        Assert.Equal(AuthorizationOutcome.Forbidden, outcome);
    }

    private sealed class FakeAuthorizationService : IAuthorizationService
    {
        public bool Succeeded { get; set; }
        public string? LastPolicyName { get; private set; }
        public ClaimsPrincipal? LastPrincipal { get; private set; }
        public object? LastResource { get; private set; }
        public IEnumerable<IAuthorizationRequirement>? LastRequirements { get; private set; }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            // MicrosoftAuthorizationPolicyCheck<TRequest> only ever calls the named-policy
            // overload below, but IAuthorizationService also exposes this requirements-based
            // overload, so the fake must implement it faithfully (mirroring the same
            // Succeeded/last-call-capture semantics) rather than throwing.
            LastPrincipal = user;
            LastResource = resource;
            LastRequirements = requirements;
            return Task.FromResult(Succeeded ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
        {
            LastPrincipal = user;
            LastResource = resource;
            LastPolicyName = policyName;
            return Task.FromResult(Succeeded ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        }
    }
}
