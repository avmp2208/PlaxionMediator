# Conduit

**Conduit** is a next-generation .NET request pipeline framework — a from-scratch, Native AOT-safe alternative to MediatR built on zero-reflection, source-generator-first architecture.

Define immutable requests, write a single handler, call `AddConduit()`, and dispatch with `ISender.Send`. Missing handlers are compile-time errors, not runtime surprises.

## Install

```bash
dotnet add package Conduit.DependencyInjection
```

`Conduit.DependencyInjection` brings in the core runtime packages and the source generator transitively.

## Quickstart

```csharp
using Conduit.Abstractions;
using Conduit.Core;
using Conduit.DependencyInjection;

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
services.AddConduit();
await using var sp = services.BuildServiceProvider();

// 4. Dispatch
var sender = sp.GetRequiredService<ISender>();
var result = await sender.Send(new Ping("hello"));
Console.WriteLine(result); // Pong: hello
```

### ASP.NET Core Minimal API

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConduit();

var app = builder.Build();

app.MapGet("/ping", async (ISender sender, CancellationToken ct) =>
    await sender.Send(new Ping("from-api"), ct));

app.Run();
```

## Why Conduit?

- **Zero reflection** at runtime — dispatch and DI registration are generated
- **Native AOT / trim safe** by construction
- **Compile-time safety** — missing or duplicate handlers fail the build (`CONDUIT001` / `CONDUIT002`)
- **Immutable-by-default** requests (`sealed record`)
- **Split `ISender` / `IPublisher`** contracts with clear failure semantics

## Packages

| Package | Role |
|---------|------|
| `Conduit.Abstractions` | Contracts (`IRequest<>`, handlers, behaviors, notifications) |
| `Conduit.Core` | `ISender`, `IPublisher`, exceptions |
| `Conduit.Pipeline` | Delegate-chain pipeline primitives |
| `Conduit.DependencyInjection` | `AddConduit()` + generator integration |
| `Conduit.SourceGenerators` | Incremental generator (analyzer package) |
| `Conduit.Analyzers` | Roslyn analyzers (missing handler, mutable request, …) |
| `Conduit.Testing` | `FakeSender` and test helpers |

## Documentation

Full architecture design lives in [`docs/architecture/`](docs/architecture/). MVP scope and release process are documented under [`ReleaseProcess/`](ReleaseProcess/).

## License

MIT — see [LICENSE](LICENSE).
