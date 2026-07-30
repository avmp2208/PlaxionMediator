using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PlaxionMediator.Sample.MinimalApi.Tests;

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
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("PlaxionMediator sample is running.", content);
    }

    [Fact]
    public async Task GetPing_ReturnsPong()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/ping");
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(data);
        Assert.Equal("Pong: from-api", data.Result);
    }

    [Fact]
    public async Task PostEcho_ReturnsEcho()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/echo", new { Message = "hello world" });
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(data);
        Assert.Equal("hello world", data.Result);
    }

    [Fact]
    public async Task PostEchoClass_ReturnsClassEcho()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/echo-class", new { Message = "hello class" });
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(data);
        Assert.Equal("ClassEcho: hello class", data.Result);
    }

    [Fact]
    public async Task PostNotify_ReturnsAccepted()
    {
        var client = _factory.CreateClient();
        // The notify endpoint takes a string message. Minimal API might expect it from body if not specified.
        // Actually it's app.MapPost("/notify", async (string message, IPublisher publisher, CancellationToken ct) => ...)
        // By default, string parameter in MapPost without attribute might come from query or body.
        // Let's try query first, or just pass it as a plain string in body.
        var response = await client.PostAsJsonAsync("/notify?message=test-notification", "");
        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostTestClass_ReturnsTestClass()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/test-class", new { Type = "IntegrationTest" });
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(data);
        Assert.Equal("TestClass: IntegrationTest", data.Result);
    }

    [Fact]
    public async Task GetFail_ReturnsInternalServerError()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/fail");
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetFlow_VerifiesBehaviorOrder()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/flow");
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<FlowResponse>();
        
        Assert.NotNull(data);
        Assert.Equal("FlowDone", data.Result);
        
        // Expected order based on registration:
        // 1. LoggingBehavior
        // 2. FlowBehaviorA
        // 3. FlowBehaviorB
        // 4. CacheBehavior
        // 5. Handler
        var expectedSteps = new[] 
        { 
            "BehaviorA-Start", 
            "BehaviorB-Start", 
            "Handler", 
            "BehaviorB-End", 
            "BehaviorA-End" 
        };
        
        Assert.Equal(expectedSteps, data.Steps);
    }

    [Fact]
    public async Task GetPingCached_VerifiesShortCircuit()
    {
        var client = _factory.CreateClient();
        // Trigger short-circuit in CacheBehavior
        var response = await client.GetAsync("/ping?message=cached");
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(data);
        Assert.Equal("CachedResult", data.Result);
    }

    [Fact]
    public async Task GetNested_VerifiesNestedDispatch()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/nested");
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(data);
        Assert.Equal("Nested: hello", data.Result);
    }

    private record PingResponse(string Result);
    private record FlowResponse(string Result, string[] Steps);
}
