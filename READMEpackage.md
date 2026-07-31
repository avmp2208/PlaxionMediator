# PlaxionMediator

> 📖 Full documentation, samples, architecture docs and the project logo are on GitHub: **[avmp2208/PlaxionMediator](https://github.com/avmp2208/PlaxionMediator#readme)**

**PlaxionMediator** is a next-generation .NET request pipeline framework — a from-scratch, Native AOT-safe alternative to MediatR built on zero-reflection, source-generator-first architecture.

Define immutable requests, write a single handler, call `AddPlaxionMediator()`, and dispatch with `ISender.Send`. Missing handlers are compile-time errors, not runtime surprises.

## Install

```bash
dotnet add package PlaxionMediator.DependencyInjection
```

`PlaxionMediator.DependencyInjection` brings in the core runtime packages and the source generator transitively.

Building a web API? Also add:

```bash
dotnet add package PlaxionMediator.AspNetCore
dotnet add package PlaxionMediator.MinimalApis
```

## Quickstart

```csharp
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.DependencyInjection;

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

## Packages

| Package | Role |
|---------|------|
| `PlaxionMediator.Abstractions` | Contracts (`IRequest<>`, handlers, behaviors, notifications) |
| `PlaxionMediator.Core` | `ISender`, `IPublisher`, exceptions |
| `PlaxionMediator.Pipeline` | Delegate-chain pipeline primitives |
| `PlaxionMediator.DependencyInjection` | `AddPlaxionMediator()` + generator integration |
| `PlaxionMediator.SourceGenerators` | Incremental generator (analyzer package) |
| `PlaxionMediator.Analyzers` | Roslyn analyzers (missing handler, mutable request, blocking calls, …) |
| `PlaxionMediator.Testing` | `FakeSender` and test helpers |
| `PlaxionMediator.AspNetCore` | Exception→`ProblemDetails` middleware (`UsePlaxionMediatorExceptionHandling`) |
| `PlaxionMediator.MinimalApis` | `MapPlaxionMediatorPost/Get/Put/Delete/Patch` endpoint helpers |

## License

MIT — see [LICENSE](https://github.com/avmp2208/PlaxionMediator/blob/master/LICENSE).
