# PlaxionMediator.OpenTelemetry

Opt-in OpenTelemetry tracing and metrics instrumentation for `PlaxionMediator`'s `ISender.Send`
and `IPublisher.Publish` dispatch, built on the `IPipelineObserver` (ADR-0008) and
`INotificationObserver` (ADR-0009) instrumentation seams. It adds no dependency on OpenTelemetry
SDK packages to `PlaxionMediator.Core`/`PlaxionMediator.Pipeline` — this package is entirely
opt-in and is never bundled transitively into core `PlaxionMediator`.

## Install

```bash
dotnet add package PlaxionMediator.OpenTelemetry
```

## Usage

```csharp
using PlaxionMediator.OpenTelemetry;

services.AddPlaxionMediator();
services.AddPlaxionMediatorOpenTelemetry();
```

`AddPlaxionMediatorOpenTelemetry()` registers a single `OpenTelemetryPipelineObserver` instance
and subscribes it to both `PipelineObserverHub` (`Send`) and `NotificationObserverHub`
(`Publish`). Wire it into your own OpenTelemetry SDK setup the same way you would for any other
instrumented library:

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("PlaxionMediator"))
    .WithMetrics(metrics => metrics.AddMeter("PlaxionMediator"));
```

No PlaxionMediator-specific exporter configuration is required.

## What's emitted

### ActivitySource

- Name: `PlaxionMediator`
- One `Activity` per `Send` call (kind `Internal`) and one per `Publish` call (kind `Producer`).
- Tags:
  - `plaxionmediator.request.type`
  - `plaxionmediator.response.type`
  - `plaxionmediator.handler.type`
  - `plaxionmediator.notification.type`
  - `plaxionmediator.behavior.count`
- On fault, `Activity.Status` is set to `Error` with the exception message, and an
  `exception.type` tag is added.

### Meter

- Name: `PlaxionMediator`
- Instruments:
  - `plaxionmediator.request.duration` (`Histogram<double>`, milliseconds)
  - `plaxionmediator.request.count` (`Counter<long>`)
  - `plaxionmediator.handler_not_found.count` (`Counter<long>`) — incremented specifically for
    `HandlerNotFoundException`
  - `plaxionmediator.pipeline_exception.count` (`Counter<long>`) — incremented for all other
    faults

## Zero overhead when unused

Both `Send` and `Publish` source-generator emitted paths check `PipelineObserverHub.HasObservers`
/ `NotificationObserverHub.HasObservers` before doing any observer work. When
`PlaxionMediator.OpenTelemetry` isn't referenced or `AddPlaxionMediatorOpenTelemetry()` isn't
called, this is a single volatile-read length check with no allocations — the hot path is
unchanged from pre-`v0.7.0` behavior.

## Learn more

See [`docs/wiki/Observability.md`](https://github.com/avmp2208/PlaxionMediator/blob/master/docs/wiki/Observability.md)
for the full write-up, including the sample WebApi wiring in
[`samples/PlaxionMediator.Sample.WebApi`](https://github.com/avmp2208/PlaxionMediator/tree/master/samples/PlaxionMediator.Sample.WebApi).
