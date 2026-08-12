using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace PlaxionMediator.Authorization.AspNetCore.Tests;

public sealed class HttpAuthorizationContextAccessorTests
{
    [Fact]
    public void Current_WhenHttpContextIsNull_ReturnsUnknownContext()
    {
        // Arrange
        var mockAccessor = new FakeHttpContextAccessor(null);
        var accessor = new HttpAuthorizationContextAccessor(mockAccessor);

        // Act
        IAuthorizationContext context = accessor.Current;

        // Assert
        Assert.Null(context.Principal);
        Assert.False(context.IsAuthenticated);
        Assert.Equal(CallerKind.Unknown, context.CallerKind);
    }

    [Fact]
    public void Current_WhenAuthenticated_ReturnsHttpContext()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var mockAccessor = new FakeHttpContextAccessor(httpContext);
        var accessor = new HttpAuthorizationContextAccessor(mockAccessor);

        // Act
        IAuthorizationContext context = accessor.Current;

        // Assert
        Assert.Same(principal, context.Principal);
        Assert.True(context.IsAuthenticated);
        Assert.Equal(CallerKind.Http, context.CallerKind);
    }

    [Fact]
    public void Current_WhenAnonymous_ReturnsHttpContext()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var httpContext = new DefaultHttpContext { User = principal };
        var mockAccessor = new FakeHttpContextAccessor(httpContext);
        var accessor = new HttpAuthorizationContextAccessor(mockAccessor);

        // Act
        IAuthorizationContext context = accessor.Current;

        // Assert
        Assert.Same(principal, context.Principal);
        Assert.False(context.IsAuthenticated);
        Assert.Equal(CallerKind.Http, context.CallerKind);
    }

    private sealed class FakeHttpContextAccessor(HttpContext? httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}
