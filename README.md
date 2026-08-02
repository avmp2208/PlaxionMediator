# PlaxionMediator

<p align="center">
  <img src="assets/plaxionlogo.png" alt="PlaxionMediator Banner" width="400" />
</p>

<p align="center">
  <a href="https://plaxion.dev">plaxion.dev</a>
</p>

**PlaxionMediator** is a next-generation .NET request pipeline platform for developers — a from-scratch, Native AOT-safe framework built on zero-reflection, source-generator-first architecture.

Define immutable requests, write a single handler, call `AddPlaxionMediator()`, and dispatch with `ISender.Send`. Missing handlers are compile-time errors, not runtime surprises.

## Install

```bash
dotnet add package PlaxionMediator
```

`PlaxionMediator` brings in the core runtime packages and the source generator transitively.

Building an ASP.NET Core / Minimal API web app? Add the opt-in web packages as well (they are **not** pulled in transitively, so console/worker apps aren't forced to reference ASP.NET Core):

```bash
dotnet add package PlaxionMediator.AspNetCore
dotnet add package PlaxionMediator.MinimalApis
```

## Quickstart

```csharp
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator;

// 1. Define an immutable request
public sealed record Ping(string Message) : IRequest<string>;

// 2. Implement exactly one handler
public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Pong: {request.Message}");
}

// 3. Register (handlers discovered at compile time — zero reflection)
var services = new ServiceCollection();
services.AddPlaxionMediator();
await using var sp = services.BuildServiceProvider();

// 4. Dispatch
var sender = sp.GetRequiredService<ISender>();
var result = await sender.Send(new Ping("hello"));
Console.WriteLine(result); // Pong: hello
```

### ASP.NET Core Minimal API

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPlaxionMediator();

var app = builder.Build();

app.MapGet("/ping", async (ISender sender, CancellationToken ct) =>
    await sender.Send(new Ping("from-api"), ct));

app.Run();
```

### `PlaxionMediator.AspNetCore` + `PlaxionMediator.MinimalApis` (v0.2.0+)

Skip hand-written mediator glue entirely: `MapPlaxionMediatorPost/Get/Put/Delete/Patch` bind the request, call `ISender.Send`, and return `TypedResults.Ok(...)`; `UsePlaxionMediatorExceptionHandling()` turns `PlaxionMediatorException` subtypes into RFC 7807 `ProblemDetails` responses.

```csharp
using PlaxionMediator.AspNetCore;
using PlaxionMediator.MinimalApis;

public sealed record CreateItemRequest(string Name) : IRequest<ItemDto>;
public sealed record GetItemRequest(Guid Id) : IRequest<ItemDto>;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPlaxionMediator();

var app = builder.Build();

// Register BEFORE routing/endpoints so handler exceptions are caught.
app.UsePlaxionMediatorExceptionHandling();

app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items");
app.MapPlaxionMediatorGet<GetItemRequest, ItemDto>("/items/{id}");

app.Run();
```

See the full CRUD walkthrough (`POST`/`GET`/`PUT`/`PATCH`/`DELETE` + error mapping) in [`samples/PlaxionMediator.Sample.WebApi`](samples/PlaxionMediator.Sample.WebApi), and the Postman collections in [`postman-tests`](postman-tests) for ready-to-run request examples against both sample apps.

## Why PlaxionMediator?

- **Zero reflection** at runtime — dispatch and DI registration are generated
- **Native AOT / trim safe** by construction
- **Compile-time safety** — missing or duplicate handlers fail the build (`PlaxionMediator001` / `PlaxionMediator002`)
- **Immutable-by-default** requests (`sealed record`)
- **Split `ISender` / `IPublisher`** contracts with clear failure semantics

## Packages

| Package | Role |
|---------|------|
| `PlaxionMediator.Abstractions` | Contracts (`IRequest<>`, handlers, behaviors, notifications) |
| `PlaxionMediator.Core` | `ISender`, `IPublisher`, exceptions |
| `PlaxionMediator.Pipeline` | Delegate-chain pipeline primitives |
| `PlaxionMediator` | `AddPlaxionMediator()` + generator integration |
| `PlaxionMediator.SourceGenerators` | Incremental generator (analyzer package) |
| `PlaxionMediator.Analyzers` | Roslyn analyzers (missing handler, mutable request, …) |
| `PlaxionMediator.Testing` | `FakeSender` and test helpers |
| `PlaxionMediator.AspNetCore` | Exception→`ProblemDetails` middleware (`UsePlaxionMediatorExceptionHandling`) |
| `PlaxionMediator.MinimalApis` | `MapPlaxionMediatorPost/Get/Put/Delete/Patch` Minimal API endpoint helpers |

> `PlaxionMediator.AspNetCore`/`PlaxionMediator.MinimalApis` are **separate opt-in packages** — they are not referenced transitively by `PlaxionMediator`, so plain console/worker apps never pull in the ASP.NET Core framework surface.

## License

MIT — see [LICENSE](LICENSE).
