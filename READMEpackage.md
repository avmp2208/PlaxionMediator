# PlaxionMediator

> 📖 Full documentation, samples, architecture docs and the project logo are on GitHub: **[avmp2208/PlaxionMediator](https://github.com/avmp2208/PlaxionMediator#readme)**

**PlaxionMediator** is a next-generation .NET request pipeline platform for developers — a from-scratch, Native AOT-safe framework built on zero-reflection, source-generator-first architecture.

Define immutable requests, write a single handler, call `AddPlaxionMediator()`, and dispatch with `ISender.Send`. Missing handlers are compile-time errors, not runtime surprises.

## Install

```bash
dotnet add package PlaxionMediator
```

`PlaxionMediator` brings in the core runtime packages and the source generator transitively.

Building a web API? Also add the opt-in companion packages as needed:

```bash
dotnet add package PlaxionMediator.AspNetCore
dotnet add package PlaxionMediator.MinimalApis
dotnet add package PlaxionMediator.Validation
dotnet add package PlaxionMediator.Validation.FluentValidation
dotnet add package PlaxionMediator.Caching
dotnet add package PlaxionMediator.Retry
dotnet add package PlaxionMediator.Transactions
dotnet add package PlaxionMediator.Transactions.EntityFrameworkCore
dotnet add package PlaxionMediator.OpenTelemetry
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

### Minimal API endpoint (`PlaxionMediator.MinimalApis`)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPlaxionMediator();

var app = builder.Build();
app.UsePlaxionMediatorExceptionHandling(); // RFC 7807 ProblemDetails for mediator exceptions

app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items");
app.MapPlaxionMediatorGet<GetItemRequest, ItemDto>("/items/{id}");

app.Run();
```

### Validation (v0.4.0+)

```csharp
builder.Services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorValidationBehavior();
});
builder.Services.AddPlaxionMediatorFluentValidation(typeof(Program).Assembly);

// Resilience, Caching & Transactions (v0.4.0+ / v0.8.0+)
builder.Services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorCachingBehavior();
    o.UsePlaxionMediatorCircuitBreakerBehavior();
    o.UsePlaxionMediatorRetryBehavior();
    o.UsePlaxionMediatorTransactionBehavior(); // after Retry
});
builder.Services.AddPlaxionMediatorCaching();
builder.Services.AddPlaxionMediatorRetry();
builder.Services.AddPlaxionMediatorCircuitBreaker();
// builder.Services.AddPlaxionMediatorTransactionsEntityFrameworkCore<AppDbContext>();

// ... failures return 400 ProblemDetails automatically
app.UsePlaxionMediatorExceptionHandling();
```

### Telemetry (`PlaxionMediator.OpenTelemetry`, v0.7.0+)

Add tracing (`ActivitySource`) and metrics (`Meter`), both named `"PlaxionMediator"`, for every `ISender.Send` and `IPublisher.Publish` call — zero overhead when the package isn't installed.

```bash
dotnet add package PlaxionMediator.OpenTelemetry
```

```csharp
using PlaxionMediator.OpenTelemetry;

builder.Services.AddPlaxionMediator();
builder.Services.AddPlaxionMediatorOpenTelemetry();

// Wire it into your own OpenTelemetry SDK setup like any other instrumented library
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(PlaxionMediatorActivitySource.Name))
    .WithMetrics(metrics => metrics.AddMeter(PlaxionMediatorMeter.Name));
```

Emits `plaxionmediator.request.duration`, `plaxionmediator.request.count`, `plaxionmediator.handler_not_found.count` and `plaxionmediator.pipeline_exception.count` metrics, plus a span per `Send`/`Publish` call. See the [full write-up on GitHub](https://github.com/avmp2208/PlaxionMediator/blob/master/docs/wiki/Observability.md) for the semantic-convention reference.

## Packages

| Package | Role |
|---------|------|
| `PlaxionMediator.Abstractions` | Contracts (`IRequest<>`, handlers, behaviors, notifications) |
| `PlaxionMediator.Core` | `ISender`, `IPublisher`, exceptions |
| `PlaxionMediator.Pipeline` | Delegate-chain pipeline primitives |
| `PlaxionMediator` | `AddPlaxionMediator()` + generator integration |
| `PlaxionMediator.SourceGenerators` | Incremental generator (analyzer package) |
| `PlaxionMediator.Analyzers` | Roslyn analyzers (missing handler, mutable request, blocking calls, …) |
| `PlaxionMediator.Testing` | `FakeSender` and test helpers |
| `PlaxionMediator.AspNetCore` | Exception→`ProblemDetails` middleware (`UsePlaxionMediatorExceptionHandling`) |
| `PlaxionMediator.MinimalApis` | `MapPlaxionMediatorPost/Get/Put/Delete/Patch` endpoint helpers |
| `PlaxionMediator.Validation` | `IPlaxionMediatorValidator<>` and `ValidationBehavior<,>` |
| `PlaxionMediator.Validation.FluentValidation` | `FluentValidation` adapter and DI scanning |
| `PlaxionMediator.Caching` | `ICacheableRequest<>` and `CachingBehavior<,>` |
| `PlaxionMediator.Retry` | `IRetryableRequest`, `ICircuitBreakerRequest`, `RetryBehavior<,>`, `CircuitBreakerBehavior<,>` |
| `PlaxionMediator.Transactions` | `ITransactionalRequest`, `ITransactionManager`, `TransactionBehavior<,>` (provider-agnostic) |
| `PlaxionMediator.Transactions.EntityFrameworkCore` | `EfCoreTransactionManager<TDbContext>` adapter |
| `PlaxionMediator.OpenTelemetry` | Opt-in OpenTelemetry tracing and metrics instrumentation for request dispatch and notification fan-out. |

## Benchmarks

Benchmarked head-to-head against [Mediator](https://github.com/martinothamar/Mediator) (source-gen) and [MediatR](https://github.com/jbogard/MediatR) via BenchmarkDotNet. All three are solid, production-ready choices — PlaxionMediator matches Mediator's allocation profile exactly on pipeline behaviors and concurrency, is essentially on par with it on type-variety dispatch (~1.00 ratio, 0 B allocated), and edges ahead of both on notification fan-out at higher handler counts — while staying consistently ahead of MediatR on latency and allocations across every scenario.

See the [full README on GitHub](https://github.com/avmp2208/PlaxionMediator#benchmarks) for the complete results tables.

## License

MIT — see [LICENSE](https://github.com/avmp2208/PlaxionMediator/blob/master/LICENSE).
