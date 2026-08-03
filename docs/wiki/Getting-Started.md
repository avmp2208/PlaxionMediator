# Getting Started

## Install

```bash
dotnet add package PlaxionMediator
```

This single package transitively brings in:
- `PlaxionMediator.Abstractions` (contracts)
- `PlaxionMediator.Core` (`ISender`/`IPublisher`, exceptions)
- `PlaxionMediator.Pipeline` (behavior chain primitives)
- `PlaxionMediator.SourceGenerators` (build-time, packed as an analyzer)
- `PlaxionMediator.Analyzers` (build-time, packed as an analyzer)
- `PlaxionMediator.Testing` (`FakeSender` for tests)

## Define a request and handler

```csharp
using PlaxionMediator.Abstractions;

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Pong: {request.Message}");
}
```

Requests are `sealed record`s by convention — immutable, and every request must have **exactly one** handler or the build fails (`PlaxionMediator001`/`PlaxionMediator002`).

## Register and dispatch

```csharp
using PlaxionMediator;

var services = new ServiceCollection();
services.AddPlaxionMediator(); // generated at compile time — no reflection scanning

await using var sp = services.BuildServiceProvider();
var sender = sp.GetRequiredService<ISender>();

var result = await sender.Send(new Ping("hello"));
Console.WriteLine(result); // Pong: hello
```

## Next steps

- Looking for more examples? See the [Full Usage Guide](Full-Usage-Guide) for Minimal APIs, Validation, and more.
- Building a web API? See [ASP.NET Core & Minimal APIs](ASPNET-Core-and-Minimal-APIs).
- Want to add request validation? See the new `PlaxionMediator.Validation` package and `ValidationBehavior<,>`.
- Want to optimize performance? See `PlaxionMediator.Caching` for request caching.
- Want to add resilience? See `PlaxionMediator.Retry` for request retries with backoff strategies.
- Want to unit test handlers/behaviors without a real DI container? See [Testing Guide](Testing-Guide).
- Curious what each package is for? See [Packages Overview](Packages-Overview).
