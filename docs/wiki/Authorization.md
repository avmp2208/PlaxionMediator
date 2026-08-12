# Authorization

Fine-grained, application-level authorization boundaries for PlaxionMediator (`v0.9.0`, ADR-0011).

> "ASP.NET Core authorization protects the endpoint. PlaxionMediator.Authorization protects the application operation."

## Problem

Application operations often require authorization checks that are more granular than what simple endpoint-level policies can provide, or need to apply even when the operation is triggered from background tasks, message queue consumers, or internal service calls where no HTTP context exists.

## Non-goals

- **Authentication:** (JWT, Identity, Cookies, etc.) - use standard ASP.NET Core authentication.
- **Token validation:** Refresh tokens, etc.
- **UI-level visibility:** Though claims can be reused.

## Packages

| Package | Role |
|---------|------|
| `PlaxionMediator.Authorization` | `IRequestAuthorization<TRequest>`, `AuthorizationBehavior`, `IAuthorizationContext` |
| `PlaxionMediator.Authorization.AspNetCore` | `HttpAuthorizationContextAccessor`, `MicrosoftAuthorizationPolicyCheck<TRequest>` adapter |

Core depends only on `PlaxionMediator.Abstractions`. Both packages are AOT-compatible.

## Quick start

### 1. Registration

```csharp
services.AddPlaxionMediator(o =>
{
    // Recommended global order:
    o.UsePlaxionMediatorValidationBehavior();
    o.UsePlaxionMediatorAuthorizationBehavior(); // after Validation
    o.UsePlaxionMediatorRetryBehavior();
    o.UsePlaxionMediatorTransactionBehavior();
});

services.AddPlaxionMediatorAuthorization();

// Optional: ASP.NET Core integration (overrides default context accessor)
services.AddPlaxionMediatorAuthorizationAspNetCore();
```

### 2. Request authorization

Implement `IRequestAuthorization<TRequest>` for your request type. The source generator automatically registers these into DI at compile time.

```csharp
public sealed record CancelOrderRequest(Guid OrderId) : IRequest<OrderDto>;

public sealed class CancelOrderAuthorization : IRequestAuthorization<CancelOrderRequest>
{
    public ValueTask<AuthorizationOutcome> AuthorizeAsync(
        CancelOrderRequest request, 
        IAuthorizationContext context, 
        CancellationToken ct)
    {
        if (!context.IsAuthenticated) 
            return ValueTask.FromResult(AuthorizationOutcome.Unauthenticated);
        
        // Resource-based check: user must own the order or be an Admin
        if (context.Principal?.IsInRole("Admin") == true) 
            return ValueTask.FromResult(AuthorizationOutcome.Authorized);
        
        var ownerClaim = context.Principal?.FindFirst("OwnerOf")?.Value;
        if (ownerClaim == request.OrderId.ToString()) 
            return ValueTask.FromResult(AuthorizationOutcome.Authorized);
        
        return ValueTask.FromResult(AuthorizationOutcome.Forbidden);
    }
}
```

## Internal and Background calls

By default, `AddPlaxionMediatorAuthorization()` registers a `SystemAuthorizationContextAccessor` which provides a default `Background` caller context (unauthenticated). 

When using `AddPlaxionMediatorAuthorizationAspNetCore()`, this is replaced by an `HttpAuthorizationContextAccessor` that pulls the current user from `HttpContext.User`. 

If `Send` is called outside an active HTTP request (e.g. from a `BackgroundService`), the HTTP accessor gracefully falls back to an unauthenticated context with `CallerKind.Unknown`.

## Failure semantics

When an authorization check fails, `AuthorizationBehavior` throws one of two exceptions:

| Outcome | Exception | ASP.NET Core Mapping |
|---------|-----------|----------------------|
| `Unauthenticated` | `PlaxionMediatorUnauthenticatedException` | 401 Unauthorized |
| `Forbidden` | `PlaxionMediatorForbiddenException` | 403 Forbidden |

When using `app.UsePlaxionMediatorExceptionHandling()` from `PlaxionMediator.AspNetCore`, these are automatically mapped to the appropriate HTTP status codes.

## Multiple checks

If multiple `IRequestAuthorization<TRequest>` implementations are registered for the same request type, they use **AND** semantics: all checks must return `Authorized` for the request to proceed. Evaluation short-circuits on the first failure.

## Pipeline ordering

**Recommended (outer → inner):**

`Validation → Authorization → Retry → Transaction → Handler`

- **Validation first:** Ensures the request is well-formed before checking permissions.
- **Authorization before Retry/Transaction:** Prevents unnecessary resource usage (retries, database transactions) if the user is not allowed to perform the action.

> **Security Note:** If you use `PlaxionMediator.Caching`, ensure `AuthorizationBehavior` is **outer** to `CachingBehavior`. If caching is outer to authorization, a cached response for a protected resource might be served to an unauthorized caller (flagged by `PlaxionMediator046`).

## ASP.NET Core Integration

### Named Policy Reuse

`PlaxionMediator.Authorization.AspNetCore` allows you to reuse existing Microsoft Authorization policies within your mediator pipeline:

```csharp
services.AddPlaxionMediatorAuthorization<MyRequest, MicrosoftAuthorizationPolicyCheck<MyRequest>>(
    o => o.PolicyName = "MustBeVip");
```

## Telemetry

The authorization package emits low-cardinality metrics under the `PlaxionMediator.Authorization` meter:

- `plaxionmediator.authorization.allowed` (Counter)
- `plaxionmediator.authorization.denied` (Counter)
- `plaxionmediator.authorization.duration` (Histogram, ms)

All metrics are tagged with `plaxionmediator.request.type`.

## Analyzers

| ID | Severity | Meaning |
|----|----------|---------|
| `PlaxionMediator046` | Error | `AuthorizationBehavior` is registered inner to `CachingBehavior` (security risk). |
| `PlaxionMediator047` | Error | `IRequestAuthorization<T>` exists but `AuthorizationBehavior` is not registered. |
| `PlaxionMediator048` | Warning | `AuthorizationBehavior` is registered inner to `RetryBehavior` or `TransactionBehavior`. |
| `PlaxionMediator049` | Warning | Same `IRequestAuthorization` check type is registered twice for one request. |

See [Analyzers-Reference.md](Analyzers-Reference.md).

## Sample

`samples/PlaxionMediator.Sample.WebApi` demonstrates `CancelOrderRequest` with resource-based ownership authorization, internal call failure semantics, and OpenTelemetry integration.
