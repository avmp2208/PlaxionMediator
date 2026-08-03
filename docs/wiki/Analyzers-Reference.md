# Analyzers Reference

All PlaxionMediator analyzers ship in `PlaxionMediator.Analyzers` (transitively referenced by `PlaxionMediator.DependencyInjection`) and report as build **warnings**, not errors, except where noted.

| Id | Name | Fires when | Severity |
|---|---|---|---|
| `PlaxionMediator001` | Missing Handler | An `IRequest<T>`/`INotification` has zero registered handlers | Error |
| `PlaxionMediator002` | Multiple Handlers | An `IRequest<T>` has more than one registered handler (ambiguous dispatch) | Error |
| `PlaxionMediator003` | Mutable Request | A request type is not an immutable `sealed record` (found mutable public setters) | Error |
| `PlaxionMediator004` | Missing CancellationToken | A handler's `Handle` method doesn't accept/forward a `CancellationToken` | Warning |
| `PlaxionMediator005` | Missing Request Binding Attribute | `MapPlaxionMediatorGet`/`MapPlaxionMediatorDelete<TRequest,TResponse>` called with a `TRequest` that has no bindable route/query members | Warning |
| `PlaxionMediator006` | Handler Blocking Call | `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` used inside an `IRequestHandler<,>`/`INotificationHandler<>` `Handle` implementation | Warning |
| `PlaxionMediator011` | Non-Sealed Handler | A handler class is not sealed, allowing accidental subclassing that bypasses DI-registered behavior | Warning |
| `PlaxionMediator020` | Invalid Behavior Registration | `PipelineBuilder.Use<T>()` called with a type that does not implement `IPipelineBehavior<,>` | Error |
| `PlaxionMediator021` | Duplicate Registration | The same behavior type is registered more than once for the same pipeline | Warning |
| `PlaxionMediator022` | Incorrect Lifetime | A Singleton handler/behavior captures a Scoped or Transient dependency | Warning |
| `PlaxionMediator031` | Missing CancellationToken Propagation | A handler/behavior receives a `CancellationToken` but doesn't pass it to an awaited async call | Warning |
| `PlaxionMediator032` | CancellationToken.None Usage | `CancellationToken.None` used inside a handler where an ambient token is available | Info |
| `PlaxionMediator040` | Async Void Handler | A handler or behavior method is declared `async void`, preventing proper exception observation | Error |
| `PlaxionMediator041` | Handler Self-Send | A handler sends a request of its own type, risking infinite recursion | Warning |
| `PlaxionMediator080` | Unnecessary Behavior on Hot Path | A `[HighFrequency]` request has more than N behaviors (default: 3) in its chain | Info |
| `PlaxionMediator081` | Synchronous-Only Handler | A handler has no `await`; suggests using `ValueTask.FromResult` for optimized completion | Info |
| `PlaxionMediator082` | Behavior Allocates in Hot Path | A behavior allocates a new closure/collection per call | Info |
| `PlaxionMediator083` | Stream Handler Buffers Sequence | A stream handler materializes the entire sequence before yielding, defeating streaming | Warning |
| `PlaxionMediator090` | Fail-Fast Notification Handler | A notification handler uses fail-fast throw patterns incompatible with fan-out semantics | Warning |

## Suppressing a false positive

Best practice for a legitimate, intentional exception (e.g. an empty "list all" request with `PlaxionMediator005`): a narrowly-scoped, documented pragma pair around the single call site.

```csharp
// GetItemsRequest deliberately has no route/query-bindable members (it lists all items),
// so PlaxionMediator005 (missing bindable surface) is intentionally suppressed here.
#pragma warning disable PlaxionMediator005
app.MapPlaxionMediatorGet<GetItemsRequest, IReadOnlyList<ItemDto>>("/items");
#pragma warning restore PlaxionMediator005
```

Avoid disabling a diagnostic project-wide via `.editorconfig` unless you're certain it should never fire anywhere in that project — that defeats the analyzer's purpose of catching genuinely accidental mistakes.
