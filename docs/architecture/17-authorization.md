# 17 — Authorization

## Abstraction

```csharp
public interface IConduitAuthorizationHandler<in TRequest>
{
    ValueTask<AuthorizationResult> Authorize(TRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);
}

public sealed record AuthorizationResult(bool Succeeded, string? FailureReason = null)
{
    public static readonly AuthorizationResult Success = new(true);
    public static AuthorizationResult Fail(string reason) => new(false, reason);
}
```

**Rationale**: authorization is expressed against `ClaimsPrincipal` — the .NET-standard identity representation — so `Conduit.Authorization` works identically inside ASP.NET Core, a console host, or a background worker, without requiring `HttpContext`. This is deliberately broader than `Microsoft.AspNetCore.Authorization`, which is HTTP-context-bound.

## Policies, Permissions, Claims, Roles

```csharp
public sealed class PolicyAuthorizationHandler<TRequest> : IConduitAuthorizationHandler<TRequest>
{
    private readonly IAuthorizationService _authorizationService; // Microsoft.AspNetCore.Authorization abstraction, reused not reinvented
    private readonly string _policyName;

    public async ValueTask<AuthorizationResult> Authorize(TRequest request, ClaimsPrincipal principal, CancellationToken ct)
    {
        var result = await _authorizationService.AuthorizeAsync(principal, request, _policyName);
        return result.Succeeded ? AuthorizationResult.Success : AuthorizationResult.Fail($"Policy '{_policyName}' failed");
    }
}
```

Rather than reinventing policy evaluation, `Conduit.Authorization` **wraps** `Microsoft.AspNetCore.Authorization`'s `IAuthorizationService`/`IAuthorizationRequirement` model when available — this reuses a mature, well-understood policy engine (claims/roles/requirement composition) instead of building a parallel one, honoring "don't reinvent what the ecosystem already does well."

## Resource Authorization

```csharp
public sealed record AuthorizeResource<TRequest, TResource>(TRequest Request, TResource Resource);
```

For requests that authorize against a specific loaded resource (e.g., "can this user edit *this* order"), the handler itself performs resource-scoped checks via `IAuthorizationService.AuthorizeAsync(principal, resource, policy)` inside the behavior — Conduit does not attempt a generic "resource-fetching" abstraction, since fetching the resource is inherently domain-specific and belongs in the handler/behavior composition, not the framework.

## The Authorization Behavior

```csharp
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        foreach (var handler in _authorizationHandlers)
        {
            var result = await handler.Authorize(request, _principalAccessor.Principal, ct);
            if (!result.Succeeded) throw new ConduitAuthorizationException(result.FailureReason);
        }
        return await next();
    }
}
```

`IConduitPrincipalAccessor` (an abstraction over `IHttpContextAccessor.HttpContext?.User` or a custom principal source in non-HTTP hosts) decouples authorization from any specific hosting model — same reasoning as splitting `ISender`/`IPublisher` for testability and hosting independence.

## Feeding `Conduit.PolicyEngine` (Commercial)

`Conduit.PolicyEngine` extends this model with a declarative policy DSL (YAML/JSON, or a fluent C# builder compiled at startup) that generates `IConduitAuthorizationHandler<TRequest>` implementations without hand-written C#, and supports **hot-reloading** policy definitions without redeploying the application — valuable for enterprises whose authorization rules change independently of application release cycles (see [Package Architecture](08-package-architecture.md) for the OSS/commercial rationale).

## Design Decisions Summary

| Decision | Alternative Considered | Why Rejected |
|---|---|---|
| `ClaimsPrincipal`-based, HTTP-agnostic | `HttpContext`-bound authorization only | Would make authorization untestable/unusable outside ASP.NET Core (workers, console apps, gRPC). |
| Wrap `IAuthorizationService` | Build a bespoke policy engine | Reinvents a well-tested ecosystem primitive for no first-principles benefit; violates "don't copy but don't reinvent needlessly" balance — reuse infra, innovate on the pipeline model. |
| Authorization as a pipeline behavior | Attribute-based (`[Authorize(Policy="x")]`) declared on handler | Attributes require either reflection (rejected) or generator-based attribute reading; a behavior with explicit per-request registration keeps the "explicit over implicit" principle and avoids inventing an attribute-processing subsystem just for this one concern. |
