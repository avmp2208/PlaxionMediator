using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.MinimalApis;
using PlaxionMediator.Testing;

namespace PlaxionMediator.MinimalApis.Tests;

public sealed class EndpointMappingTests
{
    [Fact]
    public async Task MapPost_Binds_Body_Sends_Request_Returns_Ok()
    {
        await using TestHost host = await TestHost.StartAsync(sender =>
        {
            sender.When<CreateThingRequest, ThingDto>(r =>
                new ThingDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), r.Name));
        });

        HttpResponseMessage response = await host.Client.PostAsJsonAsync("/things", new { Name = "alpha" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ThingDto? body = await response.Content.ReadFromJsonAsync<ThingDto>();
        Assert.NotNull(body);
        Assert.Equal("alpha", body.Name);

        Assert.Single(host.Sender.SentRequests);
        CreateThingRequest sent = Assert.IsType<CreateThingRequest>(host.Sender.SentRequests[0]);
        Assert.Equal("alpha", sent.Name);
    }

    [Fact]
    public async Task MapPut_Binds_Body_Sends_Request_Returns_Ok()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await using TestHost host = await TestHost.StartAsync(sender =>
        {
            sender.When<UpdateThingRequest, ThingDto>(r => new ThingDto(r.Id, r.Name));
        });

        HttpResponseMessage response = await host.Client.PutAsJsonAsync("/things", new { Id = id, Name = "beta" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ThingDto? body = await response.Content.ReadFromJsonAsync<ThingDto>();
        Assert.NotNull(body);
        Assert.Equal(id, body.Id);
        Assert.Equal("beta", body.Name);

        UpdateThingRequest sent = Assert.IsType<UpdateThingRequest>(Assert.Single(host.Sender.SentRequests));
        Assert.Equal(id, sent.Id);
        Assert.Equal("beta", sent.Name);
    }

    [Fact]
    public async Task MapPatch_Binds_Body_Sends_Request_Returns_Ok()
    {
        var id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        await using TestHost host = await TestHost.StartAsync(sender =>
        {
            sender.When<PatchThingRequest, ThingDto>(r => new ThingDto(r.Id, r.Name));
        });

        HttpResponseMessage response = await host.Client.PatchAsJsonAsync("/things", new { Id = id, Name = "delta" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ThingDto? body = await response.Content.ReadFromJsonAsync<ThingDto>();
        Assert.NotNull(body);
        Assert.Equal(id, body.Id);
        Assert.Equal("delta", body.Name);

        PatchThingRequest sent = Assert.IsType<PatchThingRequest>(Assert.Single(host.Sender.SentRequests));
        Assert.Equal(id, sent.Id);
        Assert.Equal("delta", sent.Name);
    }

    [Fact]
    public async Task MapGet_Binds_Route_Sends_Request_Returns_Ok()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await using TestHost host = await TestHost.StartAsync(sender =>
        {
            sender.When<GetThingRequest, ThingDto>(r => new ThingDto(r.Id, "gamma"));
        });

        HttpResponseMessage response = await host.Client.GetAsync($"/things/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ThingDto? body = await response.Content.ReadFromJsonAsync<ThingDto>();
        Assert.NotNull(body);
        Assert.Equal(id, body.Id);
        Assert.Equal("gamma", body.Name);

        GetThingRequest sent = Assert.IsType<GetThingRequest>(Assert.Single(host.Sender.SentRequests));
        Assert.Equal(id, sent.Id);
    }

    [Fact]
    public async Task MapDelete_Binds_Route_Sends_Request_Returns_Ok()
    {
        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using TestHost host = await TestHost.StartAsync(sender =>
        {
            sender.When<DeleteThingRequest, DeleteThingResponse>(r => new DeleteThingResponse(r.Id, true));
        });

        HttpResponseMessage response = await host.Client.DeleteAsync($"/things/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DeleteThingResponse? body = await response.Content.ReadFromJsonAsync<DeleteThingResponse>();
        Assert.NotNull(body);
        Assert.Equal(id, body.Id);
        Assert.True(body.Deleted);

        DeleteThingRequest sent = Assert.IsType<DeleteThingRequest>(Assert.Single(host.Sender.SentRequests));
        Assert.Equal(id, sent.Id);
    }

    public sealed record ThingDto(Guid Id, string Name);
    public sealed record CreateThingRequest(string Name) : IRequest<ThingDto>;
    public sealed record UpdateThingRequest(Guid Id, string Name) : IRequest<ThingDto>;
    public sealed record PatchThingRequest(Guid Id, string Name) : IRequest<ThingDto>;
    public sealed record GetThingRequest(Guid Id) : IRequest<ThingDto>;
    public sealed record DeleteThingRequest(Guid Id) : IRequest<DeleteThingResponse>;
    public sealed record DeleteThingResponse(Guid Id, bool Deleted);

    private sealed class TestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private TestHost(WebApplication app, FakeSender sender, HttpClient client)
        {
            _app = app;
            Sender = sender;
            Client = client;
        }

        public FakeSender Sender { get; }
        public HttpClient Client { get; }

        public static async Task<TestHost> StartAsync(Action<FakeSender> configureSender)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            FakeSender sender = new();
            configureSender(sender);
            builder.Services.AddSingleton<ISender>(sender);

            WebApplication app = builder.Build();
            app.MapPlaxionMediatorPost<CreateThingRequest, ThingDto>("/things");
            app.MapPlaxionMediatorPut<UpdateThingRequest, ThingDto>("/things");
            app.MapPlaxionMediatorPatch<PatchThingRequest, ThingDto>("/things");
            app.MapPlaxionMediatorGet<GetThingRequest, ThingDto>("/things/{id:guid}");
            app.MapPlaxionMediatorDelete<DeleteThingRequest, DeleteThingResponse>("/things/{id:guid}");

            await app.StartAsync();
            HttpClient client = app.GetTestClient();
            return new TestHost(app, sender, client);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
        }
    }
}
