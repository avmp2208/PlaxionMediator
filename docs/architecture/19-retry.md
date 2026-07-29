# 19 — Retry

## Foundation: `Microsoft.Extensions.Resilience`

Conduit does not implement its own retry/backoff/circuit-breaker algorithms — `Microsoft.Extensions.Resilience` (built on Polly) is already the .NET-standard resilience library, ships first-class `Microsoft.Extensions.DependencyInjection` integration, and is what ASP.NET Core/`HttpClientFactory` itself uses. `Conduit.Retry` is a thin pipeline-behavior adapter over it, not a competing implementation.

## Policy Provider

```csharp
public interface IRetryPolicyProvider<in TRequest>
{
    ResiliencePipeline GetPipeline(TRequest request); // Microsoft.Extensions.Resilience's ResiliencePipeline
}

public sealed class DefaultRetryPolicyProvider<TRequest> : IRetryPolicyProvider<TRequest>
{
    public ResiliencePipeline GetPipeline(TRequest request) => _namedPipelineProvider.GetPipeline("conduit-default");
}
```

Retry policies are registered through the standard `AddResiliencePipeline("name", builder => ...)` API — Conduit's contribution is only `IRetryPolicyProvider<TRequest>`, which maps a request type to a named pipeline, so different request types can use different resilience strategies without each behavior instance hardcoding policy details.

## The Retry Behavior

```csharp
public sealed class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var pipeline = _policyProvider.GetPipeline(request);
        return pipeline.ExecuteAsync(async token => await next(), ct);
    }
}
```

Per [Pipeline Architecture](12-pipeline-architecture.md#recommended-default-global-order), `RetryBehavior` is registered **innermost**, immediately wrapping the handler call — retrying re-executes only the handler, not upstream logging/validation/authorization/caching behaviors, avoiding duplicate side effects (e.g., re-validating or re-authorizing on every retry attempt) and duplicate telemetry spans per attempt (attempts are recorded as nested spans by `Microsoft.Extensions.Resilience`'s own OpenTelemetry integration instead).

## Transient Failure Handling

`Conduit.Retry` ships a `PredicateBuilder`-based default classification of "transient" (timeout, `HttpRequestException`, `SqlException` with transient error numbers, `DbUpdateConcurrencyException` when explicitly opted in) — but requires requests that should be retried to implement `IRetryableRequest` (opt-in), because blindly retrying non-idempotent commands (e.g., "charge credit card") is a correctness bug waiting to happen, not a resilience feature.

```csharp
public interface IRetryableRequest
{
    bool IsIdempotent { get; } // Must explicitly assert idempotency before RetryBehavior applies.
}
```

## Backoff Strategies

Exponential backoff with jitter is the default (`Microsoft.Extensions.Resilience`'s `RetryStrategyOptions.BackoffType = DelayBackoffType.Exponential`, `UseJitter = true`) — jitter is non-negotiable in the default template to avoid retry storms/thundering herd against a struggling downstream dependency, a well-established distributed-systems first principle.

## Circuit Breaker Integration

`Conduit.Retry` does not implement its own circuit breaker — it composes `Microsoft.Extensions.Resilience`'s `CircuitBreakerStrategyOptions` into the same named `ResiliencePipeline` used for retry, so a request type can have both retry-with-backoff *and* a circuit breaker (trip after N consecutive failures, half-open probing) configured in one place:

```csharp
services.AddResiliencePipeline("conduit-default", builder => builder
    .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3, BackoffType = DelayBackoffType.Exponential, UseJitter = true })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions { FailureRatio = 0.5, MinimumThroughput = 10 }));
```

## Design Rationale Summary

| Decision | Why |
|---|---|
| Adapt `Microsoft.Extensions.Resilience`, don't reinvent | Avoids duplicating a mature, ecosystem-standard, already-OpenTelemetry-integrated resilience engine. |
| Retry opt-in via `IRetryableRequest.IsIdempotent` | Prevents silent, dangerous automatic retries of non-idempotent commands. |
| Innermost pipeline position | Avoids duplicate side effects/telemetry from upstream behaviors on each retry attempt. |
| Jitter mandatory by default | Prevents retry-storm cascading failures against already-degraded dependencies. |
