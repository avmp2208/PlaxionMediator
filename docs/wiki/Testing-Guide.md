# Testing Guide

## Unit testing consumers of `ISender`

`PlaxionMediator.Testing` ships `FakeSender`, a hand-written test double implementing `ISender` — no mocking library required.

```csharp
var fakeSender = new FakeSender();
fakeSender.When<Ping, string>(request => $"Pong: {request.Message}");

var result = await fakeSender.Send(new Ping("hi"));
Assert.Equal("Pong: hi", result);
```

### Stubbing streaming requests

`FakeSender` also supports streaming requests (via `CreateStream`) with `WhenStream` overloads (added in `v0.5.0`):

```csharp
var fakeSender = new FakeSender();

// Stub with IEnumerable
fakeSender.WhenStream<StreamPing, int>(request => Enumerable.Range(0, request.Count));

// Stub with IAsyncEnumerable
fakeSender.WhenStream<StreamPing, int>(async request => 
{
    for (int i = 0; i < request.Count; i++)
    {
        yield return i;
        await Task.Yield();
    }
});

// Using the stream
await foreach (int item in fakeSender.CreateStream(new StreamPing(3)))
{
    // observed: 0, 1, 2
}

// All assertion helpers work for streaming requests too
fakeSender.AssertCallCount<StreamPing>(1);
```

### Asserting call count, captured arguments, and ordering

`FakeSender` also tracks every request it observes (`SentRequests`), and exposes assertion helpers (added in `v0.5.0`) so you don't need hand-rolled bookkeeping to verify `ISender`-dependent code:

```csharp
var fakeSender = new FakeSender();
fakeSender.When<Ping, string>(request => $"Pong: {request.Message}");
fakeSender.When<CountRequest, int>(_ => 1);

await fakeSender.Send(new Ping("a"));
await fakeSender.Send(new Ping("b"));
await fakeSender.Send(new CountRequest());

// Call-count assertion
fakeSender.AssertCallCount<Ping>(2);

// Argument capture — inspect every request of a given type, in call order
IReadOnlyList<Ping> pings = fakeSender.GetSent<Ping>();
Assert.Equal(new[] { "a", "b" }, pings.Select(p => p.Message));

// Ordering assertion across multiple Send calls (by request type)
fakeSender.AssertSentInOrder(typeof(Ping), typeof(Ping), typeof(CountRequest));
```

All assertion helpers throw `FakeSenderAssertionException` (a plain `Exception`, not a `PlaxionMediatorException`) on mismatch, with a message describing the expected vs. actual state.

## Unit testing handlers directly

Handlers are plain classes — instantiate and call `Handle` directly, no DI container needed:

```csharp
var handler = new GetItemHandler(store);
var result = await handler.Handle(new GetItemRequest(id), CancellationToken.None);
```

## Integration testing ASP.NET Core / Minimal API apps

Use `WebApplicationFactory<Program>` against your sample/app project (requires the entry point to be visible, e.g. via `public partial class Program` in `Program.cs`):

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Create_Then_Get_Roundtrips()
    {
        var create = await _client.PostAsJsonAsync("/items", new CreateItemRequest("widget"));
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<ItemDto>();

        var get = await _client.GetAsync($"/items/{created!.Id}");
        get.EnsureSuccessStatusCode();
    }
}
```

This mirrors the pattern used in [`test/PlaxionMediator.Sample.MinimalApi.Tests`](https://github.com/avmp2208/PlaxionMediator/tree/master/test/PlaxionMediator.Sample.MinimalApi.Tests) and [`test/PlaxionMediator.Sample.WebApi.Tests`](https://github.com/avmp2208/PlaxionMediator/tree/master/test/PlaxionMediator.Sample.WebApi.Tests).

## Manual/exploratory testing with Postman

Ready-to-run Postman collections for both sample apps (with a shared environment defaulting to `http://localhost:5000`) live in [`docs/postman-tests`](https://github.com/avmp2208/PlaxionMediator/tree/master/docs/postman-tests):
- `PlaxionMediator.Sample.WebApi.postman_collection.json` — full CRUD + `ProblemDetails` error-mapping demo requests
- `PlaxionMediator.Sample.MinimalApi.postman_collection.json` — the MVP sample's endpoints
- `PlaxionMediator.postman_environment.json` — shared `baseUrl`/variables environment

Run them from the CLI with [Newman](https://github.com/postmanlabs/newman):

```bash
newman run docs/postman-tests/PlaxionMediator.Sample.WebApi.postman_collection.json \
  -e docs/postman-tests/PlaxionMediator.postman_environment.json
```
