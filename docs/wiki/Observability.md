### Observability

`PlaxionMediator.OpenTelemetry` is an opt-in package that adds `System.Diagnostics.Activity` tracing and
`System.Diagnostics.Metrics` metrics for `Send` and `Publish` calls, without adding any dependency on
OpenTelemetry SDK packages to Core/Pipeline (ADR-0008/ADR-0009 observer seam).

#### Enabling it

```
dotnet add package PlaxionMediator.OpenTelemetry
```

```csharp
services.AddPlaxionMediator(...);
services.AddPlaxionMediatorOpenTelemetry();
```

`AddPlaxionMediatorOpenTelemetry()` registers a single `OpenTelemetryPipelineObserver` instance and
subscribes it to both `PipelineObserverHub` (Send) and `NotificationObserverHub` (Publish). Wire it into
your own OpenTelemetry SDK setup (`AddSource("PlaxionMediator")` / `AddMeter("PlaxionMediator")`) the same
way you would for `ActivitySource`/`Meter` from any other library — no PlaxionMediator-specific exporter
configuration is required.

#### ActivitySource

- Name: `PlaxionMediator`
- One `Activity` per `Send` call (kind `Internal`) and one per `Publish` call (kind `Producer`).
- Tags:
  - `plaxionmediator.request.type`
  - `plaxionmediator.response.type`
  - `plaxionmediator.handler.type`
  - `plaxionmediator.notification.type`
  - `plaxionmediator.behavior.count`
  - `plaxionmediator.correlation_id` — a caller-supplied `correlation.id` `Activity.Baggage` item, or the
    activity's own W3C `TraceId` when no baggage item is set (see [Correlation ID](#correlation-id) below).
- On fault, `Activity.Status` is set to `Error` with the exception message, and an `exception.type` tag is added.

#### Meter

- Name: `PlaxionMediator`
- Instruments:
  - `plaxionmediator.request.duration` (`Histogram<double>`, milliseconds)
  - `plaxionmediator.request.count` (`Counter<long>`)
  - `plaxionmediator.handler_not_found.count` (`Counter<long>`) — incremented specifically for `HandlerNotFoundException`
  - `plaxionmediator.pipeline_exception.count` (`Counter<long>`) — incremented for all other faults

#### Correlation ID

Every `Send`/`Publish` `Activity` gets a `plaxionmediator.correlation_id` tag for free:

- If the caller sets an `Activity.Current?.SetBaggage("correlation.id", myCorrelationId)` baggage item
  before calling `Send`/`Publish` (e.g. from your own ASP.NET Core middleware), that business-level id is
  used as-is — PlaxionMediator never inspects or requires any specific format for it.
- Otherwise, it falls back to the started `Activity`'s own W3C `TraceId`, which is already the ambient
  trace context (e.g. inherited from the incoming HTTP request's `Activity.Current` in ASP.NET Core), so
  every span/metric is correlatable across a request out of the box with zero setup.

```csharp
// e.g. in a custom middleware, before calling ISender.Send/IPublisher.Publish
Activity.Current?.SetBaggage("correlation.id", httpContext.TraceIdentifier);
```

##### Pitfall: ASP.NET Core hosts need `AddAspNetCoreInstrumentation()` too

If you host PlaxionMediator inside ASP.NET Core, ASP.NET Core itself always starts an internal
`Activity` for every incoming request, whether or not you instrument it. If your `TracerProviderBuilder`
only calls `.AddSource("PlaxionMediator")` (no ASP.NET Core instrumentation registered at all), that
request `Activity` is created but never marked as sampled/recorded. Because the default sampler is
`ParentBased`, PlaxionMediator's own child `Activity` then silently inherits that "not recorded"
decision — `ActivitySource.StartActivity` returns `null`, no span is created, no `plaxionmediator.correlation_id`
tag exists, and nothing is exported. Metrics still work fine in this situation (they don't depend on
sampling), which is why this failure mode is easy to miss: you see request-duration/count metrics in the
console but no trace/span output at all, and no error anywhere.

The fix is to add the `OpenTelemetry.Instrumentation.AspNetCore` package and call
`.AddAspNetCoreInstrumentation()` alongside `.AddSource("PlaxionMediator")`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()          // required: samples/records the incoming request Activity
        .AddSource(PlaxionMediatorActivitySource.Name)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(PlaxionMediatorMeter.Name)
        .AddConsoleExporter());
```

This is exactly what `samples/PlaxionMediator.Sample.WebApi` does (see below).

#### Zero overhead when unused

Both `Send` and `Publish` source-generator emitted paths check `PipelineObserverHub.HasObservers` /
`NotificationObserverHub.HasObservers` before doing any observer work. When
`PlaxionMediator.OpenTelemetry` isn't referenced or `AddPlaxionMediatorOpenTelemetry()` isn't called, this
is a single volatile-read length check with no allocations — the hot path is unchanged from pre-v0.7.0
behavior.

#### Sample

`samples/PlaxionMediator.Sample.WebApi` demonstrates the package end-to-end, including
`.AddAspNetCoreInstrumentation()` (see the pitfall above), so it prints the correlation id via three
different, mutually-corroborating places in the console for every `POST /items` (or any other
mediator-backed endpoint) call — run the sample and look for, in this order:

1. **A plain log line** from the `PlaxionMediator.Observability` category, right next to the familiar
   `Microsoft.AspNetCore.Hosting.Diagnostics` `Request finished ...` line:
   ```
   info: PlaxionMediator.Observability[0]
         PlaxionMediator correlation id for POST /items: d9d0cdfc4822692e12c2fda7a055d65a
   ```
2. **A full trace `Activity.` block** for the PlaxionMediator span itself, printed by the console trace
   exporter as soon as the `Send` call completes (before the "Request finished" log line) — look for
   `Instrumentation scope (ActivitySource): Name: PlaxionMediator` and, inside its `Activity.TagObjects`,
   the `plaxionmediator.correlation_id` tag.
3. **A second `Activity.` block** for the ASP.NET Core request itself (`Instrumentation scope
   (ActivitySource): Name: Microsoft.AspNetCore`) whose `Activity.TraceId` is identical to the one from
   step 2 — this is the actual proof of end-to-end correlation between the HTTP request and the mediator
   call that served it.

> **Note:** A full `.NET Aspire` AppHost/dashboard sample was considered for this release but was
> descoped due to sandbox/tooling constraints (workload availability). The WebApi sample above is a
> lighter-weight, still-real substitute. A dedicated Aspire sample remains a good follow-up for a future
> release.
