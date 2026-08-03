using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlaxionMediator.Sample.WebApi.Tests;

public sealed class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoot_ReturnsSuccess()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal("PlaxionMediator WebApi sample is running.", content);
    }

    [Fact]
    public async Task Crud_RoundTrip_Create_Get_Update_Delete()
    {
        HttpClient client = _factory.CreateClient();

        // CREATE
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/items", new { Name = "Widget" });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        ItemDto? created = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(created);
        Assert.Equal("Widget", created.Name);
        Assert.NotEqual(Guid.Empty, created.Id);

        // GET (single)
        HttpResponseMessage getResponse = await client.GetAsync($"/items/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        ItemDto? fetched = await getResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Widget", fetched.Name);

        // GET (list) — created item must be present
        HttpResponseMessage listResponse = await client.GetAsync("/items");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        List<ItemDto>? list = await listResponse.Content.ReadFromJsonAsync<List<ItemDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, i => i.Id == created.Id && i.Name == "Widget");

        // UPDATE (body includes Id — MapPlaxionMediatorPut binds from JSON body)
        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            "/items",
            new { Id = created.Id, Name = "Widget-Updated" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        ItemDto? updated = await updateResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Widget-Updated", updated.Name);

        // GET after update
        HttpResponseMessage getAfterUpdate = await client.GetAsync($"/items/{created.Id}");
        ItemDto? fetchedAfterUpdate = await getAfterUpdate.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(fetchedAfterUpdate);
        Assert.Equal("Widget-Updated", fetchedAfterUpdate.Name);

        // PATCH (body includes Id — MapPlaxionMediatorPatch binds from JSON body)
        HttpRequestMessage patchRequest = new(HttpMethod.Patch, "/items/rename")
        {
            Content = JsonContent.Create(new { Id = created.Id, Name = "Widget-Patched" }),
        };
        HttpResponseMessage patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        ItemDto? patched = await patchResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(patched);
        Assert.Equal(created.Id, patched.Id);
        Assert.Equal("Widget-Patched", patched.Name);

        // GET after patch
        HttpResponseMessage getAfterPatch = await client.GetAsync($"/items/{created.Id}");
        ItemDto? fetchedAfterPatch = await getAfterPatch.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(fetchedAfterPatch);
        Assert.Equal("Widget-Patched", fetchedAfterPatch.Name);

        // DELETE
        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/items/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        DeleteItemResponse? deleted = await deleteResponse.Content.ReadFromJsonAsync<DeleteItemResponse>();
        Assert.NotNull(deleted);
        Assert.Equal(created.Id, deleted.Id);
        Assert.True(deleted.Deleted);

        // GET after delete should surface handler KeyNotFoundException as unhandled 500
        // (not a mapped PlaxionMediator exception) — assert item is gone via delete idempotency.
        HttpResponseMessage deleteAgain = await client.DeleteAsync($"/items/{created.Id}");
        DeleteItemResponse? deletedAgain = await deleteAgain.Content.ReadFromJsonAsync<DeleteItemResponse>();
        Assert.NotNull(deletedAgain);
        Assert.False(deletedAgain.Deleted);
    }

    [Fact]
    public async Task StreamTicks_StreamsCorrectNumberOfItems()
    {
        HttpClient client = _factory.CreateClient();
        // Use a small count and short interval for the test
        int count = 3;
        int intervalMs = 10;
        
        HttpResponseMessage response = await client.GetAsync($"/stream/ticks?count={count}&intervalMs={intervalMs}");
        response.EnsureSuccessStatusCode();

        var ticks = await response.Content.ReadFromJsonAsync<List<DateTime>>();
        Assert.NotNull(ticks);
        Assert.Equal(count, ticks.Count);
    }

    [Fact]
    public async Task HandlerNotFound_Returns_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/boom/handler-not-found");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("https://plaxionmediator.dev/errors/handler-not-found", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task PipelineExecution_Returns_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/boom/pipeline");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("https://plaxionmediator.dev/errors/pipeline-execution", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(500, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("SampleBehavior", doc.RootElement.GetProperty("stageName").GetString());
        Assert.Equal("Simulated behavior fault", doc.RootElement.GetProperty("innerException").GetProperty("message").GetString());
    }

    [Fact]
    public async Task CreateItem_Valid_Succeeds()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/items", new { Name = "Valid-Widget" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ItemDto? created = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(created);
        Assert.Equal("Valid-Widget", created.Name);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task CreateItem_EmptyName_Returns_Validation_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/items", new { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("https://plaxionmediator.dev/errors/validation", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("errors", out JsonElement errors));
        Assert.True(errors.GetArrayLength() >= 1);
        Assert.Contains(
            errors.EnumerateArray(),
            e => e.GetProperty("propertyName").GetString() == "Name");
    }

    [Fact]
    public async Task CreateItem_NameTooLong_Returns_Validation_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        string tooLong = new('x', 201);
        HttpResponseMessage response = await client.PostAsJsonAsync("/items", new { Name = tooLong });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("https://plaxionmediator.dev/errors/validation", doc.RootElement.GetProperty("type").GetString());
        Assert.Contains(
            doc.RootElement.GetProperty("errors").EnumerateArray(),
            e => e.GetProperty("propertyName").GetString() == "Name");
    }

    [Fact]
    public async Task CreateItem_Invalid_Does_Not_Execute_Handler_SideEffects()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage listBefore = await client.GetAsync("/items");
        List<ItemDto>? before = await listBefore.Content.ReadFromJsonAsync<List<ItemDto>>();
        Assert.NotNull(before);
        int countBefore = before.Count;

        HttpResponseMessage invalid = await client.PostAsJsonAsync("/items", new { Name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        HttpResponseMessage listAfter = await client.GetAsync("/items");
        List<ItemDto>? after = await listAfter.Content.ReadFromJsonAsync<List<ItemDto>>();
        Assert.NotNull(after);
        Assert.Equal(countBefore, after.Count);
    }

    [Fact]
    public async Task UpdateItem_Valid_Succeeds()
    {
        HttpClient client = _factory.CreateClient();
        ItemDto created = await CreateItemAsync(client, "Before");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/items",
            new { Id = created.Id, Name = "After" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ItemDto? updated = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("After", updated.Name);
    }

    [Fact]
    public async Task UpdateItem_EmptyGuid_Returns_Validation_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/items",
            new { Id = Guid.Empty, Name = "Name" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("https://plaxionmediator.dev/errors/validation", doc.RootElement.GetProperty("type").GetString());
        Assert.Contains(
            doc.RootElement.GetProperty("errors").EnumerateArray(),
            e => e.GetProperty("propertyName").GetString() == "Id");
    }

    [Fact]
    public async Task UpdateItem_EmptyName_Returns_Validation_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        ItemDto created = await CreateItemAsync(client, "Keep");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/items",
            new { Id = created.Id, Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Contains(
            doc.RootElement.GetProperty("errors").EnumerateArray(),
            e => e.GetProperty("propertyName").GetString() == "Name");

        // Handler must not have mutated the item.
        HttpResponseMessage getResponse = await client.GetAsync($"/items/{created.Id}");
        ItemDto? fetched = await getResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(fetched);
        Assert.Equal("Keep", fetched.Name);
    }

    [Fact]
    public async Task RenameItem_Valid_Succeeds()
    {
        HttpClient client = _factory.CreateClient();
        ItemDto created = await CreateItemAsync(client, "Original");

        HttpRequestMessage patchRequest = new(HttpMethod.Patch, "/items/rename")
        {
            Content = JsonContent.Create(new { Id = created.Id, Name = "Renamed" }),
        };
        HttpResponseMessage response = await client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ItemDto? renamed = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(renamed);
        Assert.Equal("Renamed", renamed.Name);
    }

    [Fact]
    public async Task RenameItem_EmptyGuid_Returns_Validation_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage patchRequest = new(HttpMethod.Patch, "/items/rename")
        {
            Content = JsonContent.Create(new { Id = Guid.Empty, Name = "Name" }),
        };
        HttpResponseMessage response = await client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("https://plaxionmediator.dev/errors/validation", doc.RootElement.GetProperty("type").GetString());
        Assert.Contains(
            doc.RootElement.GetProperty("errors").EnumerateArray(),
            e => e.GetProperty("propertyName").GetString() == "Id");
    }

    [Fact]
    public async Task RenameItem_NameTooLong_Returns_Validation_ProblemJson()
    {
        HttpClient client = _factory.CreateClient();
        ItemDto created = await CreateItemAsync(client, "Original");

        HttpRequestMessage patchRequest = new(HttpMethod.Patch, "/items/rename")
        {
            Content = JsonContent.Create(new { Id = created.Id, Name = new string('y', 201) }),
        };
        HttpResponseMessage response = await client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.Contains(
            doc.RootElement.GetProperty("errors").EnumerateArray(),
            e => e.GetProperty("propertyName").GetString() == "Name");

        HttpResponseMessage getResponse = await client.GetAsync($"/items/{created.Id}");
        ItemDto? fetched = await getResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(fetched);
        Assert.Equal("Original", fetched.Name);
    }

    private static async Task<ItemDto> CreateItemAsync(HttpClient client, string name)
    {
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/items", new { Name = name });
        createResponse.EnsureSuccessStatusCode();
        ItemDto? created = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(created);
        return created;
    }

    private sealed record ItemDto(Guid Id, string Name);
    private sealed record DeleteItemResponse(Guid Id, bool Deleted);
}
