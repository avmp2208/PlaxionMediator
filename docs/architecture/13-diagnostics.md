# 13 — Diagnostics

## OSS Tier: `Conduit.Diagnostics`

Because the pipeline shape is fully known at compile time ([Source Generator Architecture](10-source-generator-architecture.md#generated-diagnostics--metadata)), diagnostics tooling starts from **static, generated metadata** rather than runtime reflection over live objects.

### Pipeline Shape Introspection

```csharp
public interface IConduitPipelineInspector
{
    IReadOnlyList<PipelineDescriptor> GetPipelines(); // One entry per request type: handler + ordered behavior chain.
}

public sealed record PipelineDescriptor(Type RequestType, Type ResponseType, Type HandlerType, IReadOnlyList<Type> Behaviors);
```

`IConduitPipelineInspector` is generated (backed by the same `ConduitPipelineMetadata` the generator emits), so listing "what does the pipeline for `CreateOrderCommand` look like" costs one array lookup, not a reflection walk.

### Execution Tracing (Basic)

`Conduit.Diagnostics` emits one `Activity` per `Send`/`Publish` call (start/stop, `Ok`/`Error` status) via a shared `ActivitySource("Conduit")`, and one child `Activity` per behavior/handler invocation — this is the basic tracing tier; full payload capture is a Pro feature (below).

### Request Graphs

A "request graph" is a static, generator-produced graph of which requests are sent by which handlers (detected via generator analysis of `ISender.Send` call sites inside handler bodies) — useful for spotting unintended coupling/cycles between commands. Exposed as `IConduitPipelineInspector.GetRequestGraph()`, rendered as DOT/Mermaid text by a `conduit diagnostics graph` CLI command (below).

### Performance Reports (Basic)

`Conduit.Diagnostics` records per-request-type `Meter` counters (count, duration histogram) using `System.Diagnostics.Metrics`, exportable to any OpenTelemetry-compatible backend via `Conduit.Telemetry`. The OSS tier ships raw metrics only — aggregation/dashboards are a Pro/Analytics concern.

### CLI Diagnostics

`dotnet conduit` is a `dotnet` tool (shipped from `Conduit.Templates`) offering:
- `dotnet conduit graph` — prints the compile-time pipeline/request graph (from generated metadata embedded as an assembly resource) without running the application.
- `dotnet conduit validate` — runs the generator's validation stage against a project without a full build, for fast CI pre-checks.

### Health Checks

`Conduit.Diagnostics` provides `AddConduitHealthCheck()`, an `IHealthCheck` verifying that `ISender`/`IPublisher` resolve successfully and that the DI container can construct every registered handler (cheap `IServiceScope`-based construction check) — surfacing "a handler's dependency was removed" issues via `/health` rather than the first real request.

## Commercial Tier

| Feature | Package | Description |
|---|---|---|
| Full-payload execution tracing | `Conduit.Diagnostics.Pro` | Captures serialized request/response payloads per `Activity` (with configurable redaction), enabling full request replay. |
| Pipeline replay / time-travel debugging | `Conduit.Diagnostics.Pro` | Re-executes a captured request against the current handler implementation in an isolated scope, for post-incident debugging. |
| Live pipeline graph UI | `Conduit.Visualizer` | Web UI rendering `IConduitPipelineInspector`'s static graph overlaid with live `Activity` execution data (latency per node, error highlighting) in real time. |
| Historical performance dashboards | `Conduit.Analytics` | Ingests `Conduit.Telemetry` metrics into a time-series store with SLA/regression dashboards (see [OpenTelemetry](15-opentelemetry.md)). |

## Design Rationale

The OSS/Pro split here follows the same rule as [Package Architecture](08-package-architecture.md): "know what's happening" (introspection, basic tracing, health checks) is free because it's required for anyone to trust and debug the framework; "replay, visualize, and analyze at scale" is commercial because it requires storage/UI infrastructure with ongoing operational cost.
