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

// Resilience & Caching (v0.4.0+)
builder.Services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorCachingBehavior();
    o.UsePlaxionMediatorCircuitBreakerBehavior();
    o.UsePlaxionMediatorRetryBehavior();
});
builder.Services.AddPlaxionMediatorCaching();
builder.Services.AddPlaxionMediatorRetry();
builder.Services.AddPlaxionMediatorCircuitBreaker();

// ... failures return 400 ProblemDetails automatically
app.UsePlaxionMediatorExceptionHandling();
```

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

## Benchmarks

Benchmarked head-to-head against [Mediator](https://github.com/martinothamar/Mediator) (source-gen) and [MediatR](https://github.com/jbogard/MediatR) via BenchmarkDotNet. All three are solid, production-ready choices — PlaxionMediator matches Mediator's allocation profile exactly on pipeline behaviors and concurrency, is essentially on par with it on type-variety dispatch (~1.00 ratio, 0 B allocated), and edges ahead of both on notification fan-out at higher handler counts — while staying consistently ahead of MediatR on latency and allocations across every scenario.

See the [full README on GitHub](https://github.com/avmp2208/PlaxionMediator#benchmarks) for the complete results tables.

## License

MIT — see [LICENSE](https://github.com/avmp2208/PlaxionMediator/blob/master/LICENSE).
