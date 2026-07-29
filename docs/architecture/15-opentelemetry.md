# 15 — OpenTelemetry

## Design Principle

Conduit instruments itself using raw `System.Diagnostics.ActivitySource`/`Meter` — the .NET-native OpenTelemetry primitives — rather than depending on the `OpenTelemetry` NuGet package directly. This keeps `Conduit.Telemetry` dependency-light (it works whether or not the consumer has wired up OpenTelemetry exporters at all) while being 100% compatible with `OpenTelemetry.Extensions.Hosting`'s auto-discovery of named sources.

## Activities

```csharp
internal static class ConduitTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Conduit", ConduitVersion.Value);
}
```

One `Activity` per `Send`/`Publish` call, named `Conduit.Send.{RequestTypeName}` / `Conduit.Publish.{NotificationTypeName}`; one child `Activity` per pipeline behavior (`Conduit.Behavior.{BehaviorTypeName}`) and one for the handler (`Conduit.Handle.{HandlerTypeName}`) — mirroring the generated pipeline chain 1:1, so a trace waterfall visually matches the pipeline diagram from [Pipeline Architecture](12-pipeline-architecture.md).

## Metrics

```csharp
internal static class ConduitMetrics
{
    public static readonly Meter Meter = new("Conduit", ConduitVersion.Value);
    public static readonly Counter<long> RequestsHandled = Meter.CreateCounter<long>("conduit.requests.handled");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("conduit.request.duration", unit: "ms");
    public static readonly Counter<long> RequestsFailed = Meter.CreateCounter<long>("conduit.requests.failed");
}
```

Metric names follow the OpenTelemetry semantic-convention style (`conduit.<noun>.<verb/measure>`) so dashboards built for other `conduit.*`-instrumented systems compose naturally.

## Tags

Every `Activity`/measurement carries: `conduit.request.type`, `conduit.response.type`, `conduit.handler.type`, and (when available) `conduit.correlation_id`. Tags never include the raw request payload (that is `Conduit.Diagnostics.Pro`'s job, opt-in, with redaction) — telemetry tags are metadata, not data.

## Correlation

`Activity.Current` is respected as the parent of the `Conduit.Send.*` activity, so a Conduit-dispatched request that originates from an ASP.NET Core HTTP request automatically nests under that request's `Activity`, giving end-to-end distributed traces without any manual propagation code.

## Distributed Tracing

Because `ActivitySource`-based activities automatically participate in the W3C Trace Context (`traceparent`/`tracestate`) propagation that ASP.NET Core, `HttpClient`, and gRPC already implement, a request that fans out via `Conduit.Azure`'s Service Bus transport (commercial) carries trace context across the message boundary using standard OpenTelemetry messaging semantic conventions — no bespoke correlation protocol invented.

## Enterprise Readiness (`Conduit.Observability`, commercial)

| Feature | OSS (`Conduit.Telemetry`) | Commercial (`Conduit.Observability`) |
|---|---|---|
| `ActivitySource`/`Meter` instrumentation | ✅ | ✅ (inherited) |
| Standard OTLP exporter compatibility | ✅ (via consumer's own `OpenTelemetry.Exporter.OpenTelemetryProtocol` setup) | ✅ |
| Adaptive/tail-based sampling profiles | — | ✅ (samples based on latency/error outcome, not just head-based probability) |
| Vendor-specific dashboard templates (Datadog, Grafana, App Insights) | — | ✅ |
| Multi-region trace aggregation guidance | — | ✅ |

The OSS tier is a complete, correct OpenTelemetry citizen on its own — `Conduit.Observability` adds convenience and advanced sampling strategies for organizations running OTel at scale, not baseline correctness.
