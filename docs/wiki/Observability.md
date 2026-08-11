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
- On fault, `Activity.Status` is set to `Error` with the exception message, and an `exception.type` tag is added.

#### Meter

- Name: `PlaxionMediator`
- Instruments:
  - `plaxionmediator.request.duration` (`Histogram<double>`, milliseconds)
  - `plaxionmediator.request.count` (`Counter<long>`)
  - `plaxionmediator.handler_not_found.count` (`Counter<long>`) — incremented specifically for `HandlerNotFoundException`
  - `plaxionmediator.pipeline_exception.count` (`Counter<long>`) — incremented for all other faults

#### Zero overhead when unused

Both `Send` and `Publish` source-generator emitted paths check `PipelineObserverHub.HasObservers` /
`NotificationObserverHub.HasObservers` before doing any observer work. When
`PlaxionMediator.OpenTelemetry` isn't referenced or `AddPlaxionMediatorOpenTelemetry()` isn't called, this
is a single volatile-read length check with no allocations — the hot path is unchanged from pre-v0.7.0
behavior.

#### Sample

`samples/PlaxionMediator.Sample.WebApi` demonstrates the package end-to-end.

> **Note:** A full `.NET Aspire` AppHost/dashboard sample was considered for this release but was
> descoped due to sandbox/tooling constraints (workload availability). The WebApi sample above is a
> lighter-weight, still-real substitute. A dedicated Aspire sample remains a good follow-up for a future
> release.
