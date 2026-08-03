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

Building an ASP.NET Core / Minimal API web app? Add the opt-in companion packages as well (they are **not** pulled in transitively, so console/worker apps aren't forced to reference ASP.NET Core or validation dependencies):

```bash
dotnet add package PlaxionMediator.AspNetCore
dotnet add package PlaxionMediator.MinimalApis
dotnet add package PlaxionMediator.Validation
dotnet add package PlaxionMediator.Validation.FluentValidation
dotnet add package PlaxionMediator.Caching
dotnet add package PlaxionMediator.Retry
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

### Validation (v0.4.0+)

Enable global request validation by adding `ValidationBehavior<,>` to the pipeline and registering your validators.

```csharp
using PlaxionMediator.Validation;
using PlaxionMediator.Validation.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlaxionMediator(o =>
{
    // 1. Add validation as a global behavior
    o.UsePlaxionMediatorValidationBehavior();
});

// 2. Register FluentValidation validators from an assembly
builder.Services.AddPlaxionMediatorFluentValidation(typeof(Program).Assembly);

var app = builder.Build();

// 3. Validation failures now return 400 ProblemDetails automatically
app.UsePlaxionMediatorExceptionHandling();
app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items");

app.Run();
```

### Caching & Retry (v0.4.0+)

Optimize performance with `CachingBehavior<,>` and resilience with `RetryBehavior<,>`.

```csharp
using PlaxionMediator.Caching;
using PlaxionMediator.Retry;

builder.Services.AddPlaxionMediator(o =>
{
    // Recommended order: Validation → Caching → Retry → Handler
    o.UsePlaxionMediatorValidationBehavior();
    o.UsePlaxionMediatorCachingBehavior();
    o.UsePlaxionMediatorRetryBehavior();
});

builder.Services.AddPlaxionMediatorCaching(o => o.DefaultCacheDuration = TimeSpan.FromMinutes(5));
builder.Services.AddPlaxionMediatorRetry(o => 
{
    o.MaxRetryAttempts = 3;
    o.BackoffStrategy = RetryBackoffStrategy.Exponential;
});
```

Define a cacheable request:

```csharp
public sealed record GetItemRequest(Guid Id) : IRequest<ItemDto>, ICacheableRequest<ItemDto>
{
    public string CacheKey => $"item:{Id}";
}
```

Define a retryable request:

```csharp
public sealed record UnstableRequest(string Data) : IRequest<string>, IRetryableRequest
{
    public int? MaxRetryAttempts => 5; // Per-request override
}
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
| `PlaxionMediator.Validation` | `IPlaxionMediatorValidator<>` and `ValidationBehavior<,>` |
| `PlaxionMediator.Validation.FluentValidation` | `FluentValidation` adapter and DI scanning |
| `PlaxionMediator.Caching` | `ICacheableRequest<>` and `CachingBehavior<,>` |
| `PlaxionMediator.Retry` | `IRetryableRequest` and `RetryBehavior<,>` |

> `PlaxionMediator.AspNetCore`/`PlaxionMediator.MinimalApis`/`PlaxionMediator.Validation`/`PlaxionMediator.Caching`/`PlaxionMediator.Retry` are **separate opt-in packages** — they are not referenced transitively by `PlaxionMediator`, so plain console/worker apps never pull in extra dependencies.

## License

MIT — see [LICENSE](LICENSE).
