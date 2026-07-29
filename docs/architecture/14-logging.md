# 14 — Logging

## Foundation: `Microsoft.Extensions.Logging`

Conduit does not invent a logging abstraction — `ILogger<T>` is already the correct, ecosystem-standard primitive with structured logging, provider model, and filtering built in. `LoggingBehavior<TRequest,TResponse>` (in `Conduit.Telemetry`... note: shipped alongside instrumentation since logging and tracing share the same lifecycle hooks) depends on `ILogger<LoggingBehavior<TRequest,TResponse>>`.

## Structured Logging

```csharp
_logger.LogInformation(
    "Handling {RequestType} -> {ResponseType}",
    typeof(TRequest).Name, typeof(TResponse).Name);
```

All log messages use message templates with named placeholders — never string interpolation — so structured logging providers (Application Insights, Seq, Elasticsearch) can index `RequestType`/`ResponseType`/`DurationMs` as first-class fields rather than parsing free text.

## Scopes

```csharp
using (_logger.BeginScope(new Dictionary<string, object?>
{
    ["ConduitRequestType"] = typeof(TRequest).Name,
    ["ConduitCorrelationId"] = correlationId
}))
{
    return await next();
}
```

`LoggingBehavior` opens one `ILogger.BeginScope` per request, so every log line emitted by the handler or downstream behaviors — including ones written by application code, not just Conduit — automatically carries the request type and correlation ID, without every log call site needing to know about Conduit.

## Correlation IDs

A `ConduitCorrelationId` is generated (or propagated from an inbound `Activity.Current?.Id`/`traceparent` header when `Conduit.AspNetCore` is used) once per `Send`/`Publish` call and flows through the logging scope and the `Activity` baggage — the same ID appears in logs and traces, which is what makes cross-signal correlation (log → trace → metric) possible in an observability backend.

## Sensitive Data Masking

Requests may implement `ISensitiveDataMasker` to control what appears in logs/traces:

```csharp
public interface ISensitiveDataMasker
{
    object ToLoggableRepresentation(); // Returns a redacted projection safe to log.
}

public sealed record CreateUserCommand(string Email, string Password) : IRequest<UserId>, ISensitiveDataMasker
{
    public object ToLoggableRepresentation() => new { Email, Password = "***" };
}
```

`LoggingBehavior` calls `ToLoggableRepresentation()` when present instead of logging the raw request; when absent, payload logging (below) is opt-in and off by default specifically to avoid accidentally logging PII/secrets — a safe-by-default posture over MediatR-style loggers that often log entire request objects unconditionally.

## Log Levels

| Event | Level |
|---|---|
| Request start/complete (success) | `Debug` (avoids log volume explosion in production by default) |
| Request complete (duration above configurable threshold) | `Information` |
| Validation failure | `Information` (expected, client-caused) |
| Unhandled handler exception | `Error` |
| Behavior misconfiguration detected at runtime (defensive checks) | `Critical` |

## Sampling

For high-throughput requests, `LoggingBehaviorOptions.SamplingRate` (0.0–1.0, default `1.0`) probabilistically skips the `Debug`-level start/complete pair while always logging failures — sampling never suppresses `Error`/`Critical` logs, only high-volume success telemetry.

## Payload Logging

`LoggingBehaviorOptions.LogRequestPayload` / `LogResponsePayload` (both `false` by default) opt into logging the (masked) request/response body at `Debug` level — intended for local development and short-lived diagnostic sessions, never recommended as an always-on production setting given payload size and residual PII risk even with masking.
