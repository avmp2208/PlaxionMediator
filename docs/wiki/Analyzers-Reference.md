# Analyzers Reference

All PlaxionMediator analyzers ship in `PlaxionMediator.Analyzers` (transitively referenced by `PlaxionMediator.DependencyInjection`) and report as build **warnings**, not errors, except where noted.

| Id | Name | Fires when | Severity |
|---|---|---|---|
| `PlaxionMediator001` | Missing Handler | An `IRequest<T>`/`INotification` has zero registered handlers | Error |
| `PlaxionMediator002` | Multiple Handlers | An `IRequest<T>` has more than one registered handler (ambiguous dispatch) | Error |
| `PlaxionMediator003` | Mutable Request | A request type is not an immutable `sealed record` | Warning |
| `PlaxionMediator004` | Missing CancellationToken | A handler's `Handle` method doesn't accept/forward a `CancellationToken` | Warning |
| `PlaxionMediator005` | Missing Request Binding Attribute | `MapPlaxionMediatorGet`/`MapPlaxionMediatorDelete<TRequest,TResponse>` called with a `TRequest` that has no bindable route/query members | Warning |
| `PlaxionMediator006` | Handler Blocking Call | `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` used inside an `IRequestHandler<,>`/`INotificationHandler<>` `Handle` implementation | Warning |

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
