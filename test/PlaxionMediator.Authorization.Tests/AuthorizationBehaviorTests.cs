using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Authorization;
using Xunit;

namespace PlaxionMediator.Authorization.Tests;

public class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_WithAuthorizedOutcome_CallsNext()
    {
        // Arrange
        var checks = new List<IRequestAuthorization<TestRequest>>
        {
            new FakeCheck(AuthorizationOutcome.Authorized)
        };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();
        bool nextCalled = false;

        // Act
        var result = await behavior.Handle(request, () =>
        {
            nextCalled = true;
            return ValueTask.FromResult("success");
        }, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task Handle_WithForbiddenOutcome_ThrowsForbiddenException()
    {
        // Arrange
        var checks = new List<IRequestAuthorization<TestRequest>>
        {
            new FakeCheck(AuthorizationOutcome.Forbidden)
        };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PlaxionMediatorForbiddenException>(() => 
            behavior.Handle(request, () => ValueTask.FromResult("success"), CancellationToken.None).AsTask());
        
        Assert.Equal(nameof(TestRequest), ex.RequestTypeName);
    }

    [Fact]
    public async Task Handle_WithUnauthenticatedOutcome_ThrowsUnauthenticatedException()
    {
        // Arrange
        var checks = new List<IRequestAuthorization<TestRequest>>
        {
            new FakeCheck(AuthorizationOutcome.Unauthenticated)
        };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PlaxionMediatorUnauthenticatedException>(() => 
            behavior.Handle(request, () => ValueTask.FromResult("success"), CancellationToken.None).AsTask());
        
        Assert.Equal(nameof(TestRequest), ex.RequestTypeName);
    }

    [Fact]
    public async Task Handle_WithZeroChecks_DoesNotTouchContextAccessor()
    {
        // Arrange
        var checks = new List<IRequestAuthorization<TestRequest>>();
        var contextAccessor = new ThrowingContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();

        // Act
        await behavior.Handle(request, () => ValueTask.FromResult("success"), CancellationToken.None);

        // Assert - No exception from ThrowingContextAccessor
    }

    [Fact]
    public async Task Handle_WithMultipleChecks_AllSucceed_CallsNext()
    {
        // Arrange
        var check1 = new FakeCheck(AuthorizationOutcome.Authorized);
        var check2 = new FakeCheck(AuthorizationOutcome.Authorized);
        var checks = new List<IRequestAuthorization<TestRequest>> { check1, check2 };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();

        // Act
        await behavior.Handle(request, () => ValueTask.FromResult("success"), CancellationToken.None);

        // Assert
        Assert.Equal(1, check1.CallCount);
        Assert.Equal(1, check2.CallCount);
    }

    [Fact]
    public async Task Handle_WithMultipleChecks_ShortCircuitsOnFirstFailure()
    {
        // Arrange
        var check1 = new FakeCheck(AuthorizationOutcome.Forbidden);
        var check2 = new FakeCheck(AuthorizationOutcome.Authorized);
        var checks = new List<IRequestAuthorization<TestRequest>> { check1, check2 };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<PlaxionMediatorForbiddenException>(() => 
            behavior.Handle(request, () => ValueTask.FromResult("success"), CancellationToken.None).AsTask());

        Assert.Equal(1, check1.CallCount);
        Assert.Equal(0, check2.CallCount);
    }

    [Fact]
    public async Task Handle_WithCancellationRequestedBefore_ThrowsOperationCanceledException()
    {
        // Arrange
        var check = new FakeCheck(AuthorizationOutcome.Authorized);
        var checks = new List<IRequestAuthorization<TestRequest>> { check };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            behavior.Handle(request, () => ValueTask.FromResult("success"), cts.Token).AsTask());
        
        Assert.Equal(0, check.CallCount);
    }

    [Fact]
    public async Task Handle_WhenCheckThrows_PropagatesException()
    {
        // Arrange
        var check = new ThrowingCheck(new InvalidOperationException("Custom error"));
        var checks = new List<IRequestAuthorization<TestRequest>> { check };
        var contextAccessor = new FakeContextAccessor();
        var behavior = new AuthorizationBehavior<TestRequest, string>(checks, contextAccessor);
        var request = new TestRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            behavior.Handle(request, () => ValueTask.FromResult("success"), CancellationToken.None).AsTask());
        
        Assert.Equal("Custom error", ex.Message);
    }

    [Fact]
    public void SystemAuthorizationContextAccessor_DefaultValues()
    {
        // Act
        var accessor = new SystemAuthorizationContextAccessor();

        // Assert
        Assert.Null(accessor.Current.Principal);
        Assert.False(accessor.Current.IsAuthenticated);
        Assert.Equal(CallerKind.Background, accessor.Current.CallerKind);
    }

    [Fact]
    public void SystemAuthorizationContextAccessor_CustomCallerKind()
    {
        // Act
        var accessor = new SystemAuthorizationContextAccessor(CallerKind.Internal);

        // Assert
        Assert.Equal(CallerKind.Internal, accessor.Current.CallerKind);
    }

    private class TestRequest : IRequest<string>;

    private class FakeCheck(AuthorizationOutcome outcome) : IRequestAuthorization<TestRequest>
    {
        public int CallCount { get; private set; }
        public ValueTask<AuthorizationOutcome> AuthorizeAsync(TestRequest request, IAuthorizationContext context, CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(outcome);
        }
    }

    private class ThrowingCheck(Exception exception) : IRequestAuthorization<TestRequest>
    {
        public ValueTask<AuthorizationOutcome> AuthorizeAsync(TestRequest request, IAuthorizationContext context, CancellationToken cancellationToken)
        {
            throw exception;
        }
    }

    private class FakeContextAccessor : IAuthorizationContextAccessor
    {
        public IAuthorizationContext Current { get; } = new AuthorizationContext(null, false, CallerKind.Unknown);
    }

    private class ThrowingContextAccessor : IAuthorizationContextAccessor
    {
        public IAuthorizationContext Current => throw new InvalidOperationException("Should not be accessed");
    }
}
