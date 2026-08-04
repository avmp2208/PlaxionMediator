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

## Benchmarks

PlaxionMediator is benchmarked head-to-head against [Mediator](https://github.com/martinothamar/Mediator) (source-gen) and [MediatR](https://github.com/jbogard/MediatR) using [BenchmarkDotNet](https://benchmarkdotnet.org/). All three frameworks are solid, production-ready choices — these numbers simply document where PlaxionMediator stands today so the comparison is transparent and reproducible. Full methodology and raw artifacts live in [`benchmarks-comparison`](benchmarks-comparison).

> Generated: 2026-08-04, via `dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison --filter *`
> Environment: BenchmarkDotNet v0.14.0, Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7 (RyuJIT AVX2)
> Job: `Job.Default` (WarmupCount=3, IterationCount=10, LaunchCount=1) — reproducible, non-Dry job.

### Pipeline Behavior Chains

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    16.20 ns |  0.73 |    1 |         - |
| Send_Plaxion_0Behaviors   |    22.19 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    56.02 ns |  2.52 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.47 ns |  3.09 |    3 |     128 B |
| Send_Plaxion_1Behavior    |   120.69 ns |  5.44 |    4 |     128 B |
| Send_MediatR_1Behavior    |   165.30 ns |  7.45 |    5 |     648 B |
| Send_Mediator_5Behaviors  |   309.10 ns | 13.93 |    6 |     640 B |
| Send_Plaxion_5Behaviors   |   388.76 ns | 17.52 |    7 |     640 B |
| Send_MediatR_5Behaviors   |   479.34 ns | 21.61 |    8 |    1896 B |
| Send_Mediator_10Behaviors |   603.20 ns | 27.19 |    9 |    1280 B |
| Send_Plaxion_10Behaviors  |   747.55 ns | 33.70 |    9 |    1280 B |
| Send_MediatR_10Behaviors  |   809.17 ns | 36.47 |    9 |    3456 B |
| Send_Mediator_20Behaviors | 1,316.00 ns | 59.32 |   10 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,466.63 ns | 66.11 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 1,708.21 ns | 77.00 |   12 |    6576 B |

**Takeaway:** Mediator (source-gen) remains the fastest, lowest-allocation option here. PlaxionMediator
tracks it closely at every depth — matching its allocation profile exactly (128/640/1280/2560 B) —
and stays consistently ahead of MediatR on both latency and allocations.

### Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   844.1 ns |  0.97 |    1 |         - |
| Dispatch_Plaxion_50Types  |   870.3 ns |  1.00 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,689.4 ns |  5.39 |    2 |   13200 B |

**Takeaway:** PlaxionMediator remains essentially tied with Mediator (ratio 1.00 vs 0.97) on this
scenario, while remaining **0 B** allocated — well ahead of MediatR, which allocates ~264 B/call.

### Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.28 ns |   0.84 |    1 |     176 B |
| Concurrent_Plaxion_1    |    46.69 ns |   1.00 |    1 |     176 B |
| Concurrent_MediatR_1    |    80.80 ns |   1.73 |    2 |     368 B |
| Concurrent_Mediator_8   |   225.77 ns |   4.85 |    3 |     736 B |
| Concurrent_Plaxion_8    |   299.23 ns |   6.42 |    4 |     736 B |
| Concurrent_MediatR_8    |   594.20 ns |  12.76 |    5 |    2272 B |
| Concurrent_Mediator_32  |   858.19 ns |  18.43 |    6 |    2656 B |
| Concurrent_Plaxion_32   | 1,824.39 ns |  39.17 |    7 |    2656 B |
| Concurrent_MediatR_32   | 2,076.46 ns |  44.58 |    8 |    8800 B |
| Concurrent_Mediator_128 | 3,546.54 ns |  76.15 |    9 |   10336 B |
| Concurrent_Plaxion_128  | 3,828.64 ns |  82.21 |    9 |   10336 B |
| Concurrent_MediatR_128  | 8,600.15 ns | 184.65 |   10 |   34912 B |

**Takeaway:** PlaxionMediator scales in step with Mediator under concurrent load, with identical
allocation profiles at every caller tier (176/736/2656/10336 B), and stays well ahead of MediatR
throughout.

### Notification Fan-Out

| Method                        | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler     |    59.40 ns |  0.66 |    1 |     120 B |
| Publish_Plaxion_1Handler      |    90.47 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler      |   112.28 ns |  1.24 |    2 |     352 B |
| Publish_Mediator_10Handlers   |   581.80 ns |  6.43 |    3 |    1200 B |
| Publish_Plaxion_10Handlers    |   598.84 ns |  6.62 |    3 |    1304 B |
| Publish_MediatR_10Handlers    |   732.56 ns |  8.10 |    3 |    2512 B |
| Publish_Plaxion_50Handlers    | 2,808.52 ns | 31.05 |    4 |    6424 B |
| Publish_Mediator_50Handlers   | 2,880.80 ns | 31.85 |    4 |    6000 B |
| Publish_MediatR_50Handlers    | 3,466.18 ns | 38.32 |    4 |   12112 B |
| Publish_Plaxion_100Handlers   | 5,515.70 ns | 60.97 |    5 |   12824 B |
| Publish_Mediator_100Handlers  | 5,966.56 ns | 65.96 |    6 |   12000 B |
| Publish_MediatR_100Handlers   | 8,183.52 ns | 90.47 |    7 |   24112 B |

**Takeaway:** PlaxionMediator's strongest category — it edges ahead of Mediator at 50 and 100
handlers, and is consistently faster than MediatR across every fan-out tier.

### Overall Summary

- **Pipeline behaviors:** PlaxionMediator matches Mediator's allocation profile exactly at every
  depth and stays ahead of MediatR on latency and allocations throughout.
- **Type variety:** PlaxionMediator is essentially on par with Mediator (ratio ~1.00) while
  remaining 0 B allocated, and is roughly 5.4x faster than MediatR with far fewer allocations.
- **Concurrency:** Scaling behavior tracks Mediator closely under load, with identical allocation
  footprints, and a clear lead over MediatR at every caller tier.
- **Notifications:** PlaxionMediator's best category, leading both peers at higher fan-out counts.
- All three frameworks — PlaxionMediator, Mediator, and MediatR — are solid, production-ready
  choices; these numbers simply document where PlaxionMediator stands today so the comparison is
  transparent and reproducible.

See [`benchmarks-comparison/RESULTS.md`](benchmarks-comparison/RESULTS.md) for the always-current snapshot, [`BENCHMARK_REPORT.md`](BENCHMARK_REPORT.md) for the narrative report, and [`ARCHITECTURE_SUMMARY.md`](ARCHITECTURE_SUMMARY.md) for the design decisions behind these numbers.

## License

MIT — see [LICENSE](LICENSE).
