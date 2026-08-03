# ASP.NET Core & Minimal APIs (`v0.2.0+`)

`PlaxionMediator.AspNetCore` and `PlaxionMediator.MinimalApis` let you build a CRUD API with **zero hand-written mediator glue code**.

## Install

```bash
dotnet add package PlaxionMediator.AspNetCore
dotnet add package PlaxionMediator.MinimalApis
```

`PlaxionMediator.MinimalApis` references `PlaxionMediator.AspNetCore` (shared `ProblemDetails`/exception mapping), so adding it is enough to pull both in.

## Exception handling middleware

```csharp
var app = builder.Build();

// Must be registered BEFORE routing/endpoints so it can catch exceptions thrown by handlers.
app.UsePlaxionMediatorExceptionHandling();

app.MapGet("/", () => "ok"); // routes go after
```

`UsePlaxionMediatorExceptionHandling()` catches `PlaxionMediatorException` subtypes and converts them into RFC 7807 `ProblemDetails` (`application/problem+json`):

| Exception | Status | Notes |
|---|---|---|
| `HandlerNotFoundException` | 500 | Indicates a build-time invariant was violated at runtime; `Extensions["requestType"]` names the offending request. |
| `PipelineExecutionException` | 500 | `Extensions["stageName"]` + a safe inner-exception summary (message + type, no stack trace). |
| Any other exception (including unrelated app exceptions) | — | **Not caught** — rethrown untouched, so this middleware never swallows unrelated failures. |

## Route mapping

```csharp
app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items");
app.MapPlaxionMediatorGet<GetItemRequest, ItemDto>("/items/{id}");
app.MapPlaxionMediatorPut<UpdateItemRequest, ItemDto>("/items");
app.MapPlaxionMediatorPatch<RenameItemRequest, ItemDto>("/items/rename");
app.MapPlaxionMediatorDelete<DeleteItemRequest, ItemDto>("/items/{id}");
```

| Method | Binding | Notes |
|---|---|---|
| `MapPlaxionMediatorPost` | JSON body | `TRequest` deserialized from the request body |
| `MapPlaxionMediatorPut` | JSON body | Full-replace semantics — send the complete desired state |
| `MapPlaxionMediatorPatch` | JSON body | Same binding as `Put`; PlaxionMediator does **not** implement JSON Merge Patch/JSON Patch semantics — `TRequest` still carries whatever full/partial shape you define |
| `MapPlaxionMediatorGet` | Route values / query string (`[AsParameters]`) | `TRequest`'s primary-constructor parameter names must match route/query names |
| `MapPlaxionMediatorDelete` | Route values / query string (`[AsParameters]`) | Same as `Get` |

Each method returns a `RouteHandlerBuilder`, so you can chain `.WithName()`, `.Produces<T>()`, etc.

```csharp
app.MapPlaxionMediatorGet<GetItemRequest, ItemDto>("/items/{id}")
   .WithName("GetItem")
   .Produces<ItemDto>(StatusCodes.Status200OK);
```

## Analyzer safety nets

- **`PlaxionMediator005`** warns when a `TRequest` passed to `MapPlaxionMediatorGet`/`Delete` has no bindable route/query members — usually a sign you forgot an `Id` parameter. If it's intentional (e.g. a parameterless "list all" request), suppress locally with a documented `#pragma warning disable PlaxionMediator005` / `restore` pair around the call site.
- **`PlaxionMediator006`** warns on `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` inside a handler's `Handle` method — a sync-over-async anti-pattern that risks deadlocks/thread-pool starvation.

## Full example

See [`samples/PlaxionMediator.Sample.WebApi`](https://github.com/avmp2208/PlaxionMediator/tree/master/samples/PlaxionMediator.Sample.WebApi) for a complete `Item` CRUD sample (Create/Get/GetAll/Update/Rename/Delete + error-mapping demo endpoints), and [`docs/postman-tests`](https://github.com/avmp2208/PlaxionMediator/tree/master/docs/postman-tests) for a ready-to-run Postman collection covering every endpoint.
