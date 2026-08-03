# Testing Guide

## Unit testing consumers of `ISender`

`PlaxionMediator.Testing` ships `FakeSender`, a hand-written test double implementing `ISender` — no mocking library required.

```csharp
var fakeSender = new FakeSender();
fakeSender.Setup<Ping, string>(request => $"Pong: {request.Message}");

var result = await fakeSender.Send(new Ping("hi"));
Assert.Equal("Pong: hi", result);
```

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
