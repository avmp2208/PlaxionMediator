using System.Collections.Concurrent;
using System.Linq;
using PlaxionMediator.Abstractions;
using PlaxionMediator.AspNetCore;
using PlaxionMediator.Core;
using PlaxionMediator;
using PlaxionMediator.MinimalApis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlaxionMediator();
builder.Services.AddSingleton<ItemStore>();

var app = builder.Build();

// Must be registered before routing/endpoints so handler exceptions become problem+json.
app.UsePlaxionMediatorExceptionHandling();

app.MapGet("/", () => "PlaxionMediator WebApi sample is running.");

app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items")
    .WithName("CreateItem")
    .Produces<ItemDto>(StatusCodes.Status200OK);

app.MapPlaxionMediatorGet<GetItemRequest, ItemDto>("/items/{id:guid}")
    .WithName("GetItem")
    .Produces<ItemDto>(StatusCodes.Status200OK);

// GetItemsRequest deliberately has no route/query-bindable members (it lists all items),
// so PlaxionMediator005 (missing bindable surface) is intentionally suppressed here.
#pragma warning disable PlaxionMediator005
app.MapPlaxionMediatorGet<GetItemsRequest, IReadOnlyList<ItemDto>>("/items")
    .WithName("GetItems")
    .Produces<IReadOnlyList<ItemDto>>(StatusCodes.Status200OK);
#pragma warning restore PlaxionMediator005

// PUT binds TRequest from JSON body (Id + Name). Route id is not merged into body-bound requests.
app.MapPlaxionMediatorPut<UpdateItemRequest, ItemDto>("/items")
    .WithName("UpdateItem")
    .Produces<ItemDto>(StatusCodes.Status200OK);

// PATCH binds TRequest from JSON body, same as PUT. PlaxionMediator does not implement
// JSON Merge Patch/JSON Patch semantics; RenameItemRequest here still carries the full desired name.
app.MapPlaxionMediatorPatch<RenameItemRequest, ItemDto>("/items/rename")
    .WithName("RenameItem")
    .Produces<ItemDto>(StatusCodes.Status200OK);

app.MapPlaxionMediatorDelete<DeleteItemRequest, DeleteItemResponse>("/items/{id:guid}")
    .WithName("DeleteItem")
    .Produces<DeleteItemResponse>(StatusCodes.Status200OK);

// Forces HandlerNotFoundException so integration tests can assert problem+json mapping.
// Thrown directly: introducing an IRequest without a handler would fail the build (PlaxionMediator001).
app.MapGet("/boom/handler-not-found", () =>
{
    throw new PlaxionMediator.Core.HandlerNotFoundException(typeof(ItemDto));
});

// Forces PipelineExecutionException so integration tests can assert problem+json mapping.
app.MapGet("/boom/pipeline", () =>
{
    throw new PlaxionMediator.Core.PipelineExecutionException(
        "Pipeline stage failed.",
        new InvalidOperationException("Simulated behavior fault"),
        "SampleBehavior");
});

app.MapPost("/notify", async (string message, IPublisher publisher, CancellationToken ct) =>
{
    await publisher.Publish(new ItemCreatedNotification(message), ct);
    return Results.Accepted();
});

app.MapPost("/notify-parallel", async (string message, IPublisher publisher, CancellationToken ct) =>
{
    await publisher.Publish(new ItemUpdatedNotification(message), ct);
    return Results.Accepted(value: new { strategy = "Parallel" });
});

app.MapGet("/stream/items", async (int? count, ISender sender, CancellationToken ct) =>
{
    int n = count is > 0 ? count.Value : 3;
    var names = new List<string>();
    await foreach (string name in sender.CreateStream(new ListItemNamesRequest(n), ct))
    {
        names.Add(name);
    }

    return Results.Ok(names);
});

app.MapGet("/stream/ticks", (int? count, int? intervalMs, ISender sender, CancellationToken ct) =>
{
    return sender.CreateStream(new StreamTicksRequest(count ?? 5, intervalMs ?? 1000), ct);
});

app.Run();

// --- Models ---

public sealed record ItemDto(Guid Id, string Name);

// --- Store ---

public sealed class ItemStore
{
    private readonly ConcurrentDictionary<Guid, ItemDto> _items = new();

    public ItemDto Add(string name)
    {
        var item = new ItemDto(Guid.NewGuid(), name);
        if (!_items.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException("Failed to add item.");
        }

        return item;
    }

    public bool TryGet(Guid id, out ItemDto? item) => _items.TryGetValue(id, out item);

    public bool TryUpdate(Guid id, string name, out ItemDto? item)
    {
        if (!_items.TryGetValue(id, out ItemDto? existing))
        {
            item = null;
            return false;
        }

        var updated = existing with { Name = name };
        if (!_items.TryUpdate(id, updated, existing))
        {
            item = null;
            return false;
        }

        item = updated;
        return true;
    }

    public bool TryRemove(Guid id, out ItemDto? item) => _items.TryRemove(id, out item);

    public IReadOnlyList<ItemDto> GetAll() => _items.Values.ToList();
}

// --- Requests & Handlers ---

public sealed record CreateItemRequest(string Name) : IRequest<ItemDto>;

public sealed class CreateItemHandler : IRequestHandler<CreateItemRequest, ItemDto>
{
    private readonly ItemStore _store;

    public CreateItemHandler(ItemStore store) => _store = store;

    public ValueTask<ItemDto> Handle(CreateItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        return ValueTask.FromResult(_store.Add(request.Name.Trim()));
    }
}

public sealed record GetItemRequest(Guid Id) : IRequest<ItemDto>;

public sealed class GetItemHandler : IRequestHandler<GetItemRequest, ItemDto>
{
    private readonly ItemStore _store;

    public GetItemHandler(ItemStore store) => _store = store;

    public ValueTask<ItemDto> Handle(GetItemRequest request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.Id, out ItemDto? item) || item is null)
        {
            throw new KeyNotFoundException($"Item '{request.Id}' was not found.");
        }

        return ValueTask.FromResult(item);
    }
}

public sealed record GetItemsRequest : IRequest<IReadOnlyList<ItemDto>>;

public sealed class GetItemsHandler : IRequestHandler<GetItemsRequest, IReadOnlyList<ItemDto>>
{
    private readonly ItemStore _store;

    public GetItemsHandler(ItemStore store) => _store = store;

    public ValueTask<IReadOnlyList<ItemDto>> Handle(GetItemsRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(_store.GetAll());
}

public sealed record UpdateItemRequest(Guid Id, string Name) : IRequest<ItemDto>;

public sealed class UpdateItemHandler : IRequestHandler<UpdateItemRequest, ItemDto>
{
    private readonly ItemStore _store;

    public UpdateItemHandler(ItemStore store) => _store = store;

    public ValueTask<ItemDto> Handle(UpdateItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (!_store.TryUpdate(request.Id, request.Name.Trim(), out ItemDto? item) || item is null)
        {
            throw new KeyNotFoundException($"Item '{request.Id}' was not found.");
        }

        return ValueTask.FromResult(item);
    }
}

public sealed record RenameItemRequest(Guid Id, string Name) : IRequest<ItemDto>;

public sealed class RenameItemHandler : IRequestHandler<RenameItemRequest, ItemDto>
{
    private readonly ItemStore _store;

    public RenameItemHandler(ItemStore store) => _store = store;

    public ValueTask<ItemDto> Handle(RenameItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.", nameof(request));
        }

        if (!_store.TryUpdate(request.Id, request.Name.Trim(), out ItemDto? item) || item is null)
        {
            throw new KeyNotFoundException($"Item '{request.Id}' was not found.");
        }

        return ValueTask.FromResult(item);
    }
}

public sealed record DeleteItemRequest(Guid Id) : IRequest<DeleteItemResponse>;

public sealed record DeleteItemResponse(Guid Id, bool Deleted);

public sealed class DeleteItemHandler : IRequestHandler<DeleteItemRequest, DeleteItemResponse>
{
    private readonly ItemStore _store;

    public DeleteItemHandler(ItemStore store) => _store = store;

    public ValueTask<DeleteItemResponse> Handle(DeleteItemRequest request, CancellationToken cancellationToken)
    {
        bool deleted = _store.TryRemove(request.Id, out _);
        return ValueTask.FromResult(new DeleteItemResponse(request.Id, deleted));
    }
}

// --- Notifications & streaming ---

public sealed record ItemCreatedNotification(string Name) : INotification;

public sealed class ItemCreatedLogHandler : INotificationHandler<ItemCreatedNotification>
{
    public ValueTask Handle(ItemCreatedNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

public sealed class ItemCreatedAuditHandler : INotificationHandler<ItemCreatedNotification>
{
    public ValueTask Handle(ItemCreatedNotification notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

[NotificationPublishStrategy(PublishStrategy.Parallel)]
public sealed record ItemUpdatedNotification(string Name) : INotification;

public sealed class ItemUpdatedLogHandler : INotificationHandler<ItemUpdatedNotification>
{
    public async ValueTask Handle(ItemUpdatedNotification notification, CancellationToken cancellationToken)
    {
        await Task.Yield();
    }
}

public sealed class ItemUpdatedMetricsHandler : INotificationHandler<ItemUpdatedNotification>
{
    public async ValueTask Handle(ItemUpdatedNotification notification, CancellationToken cancellationToken)
    {
        await Task.Yield();
    }
}

public sealed record ListItemNamesRequest(int Count) : IStreamRequest<string>;

public sealed class ListItemNamesHandler : IStreamRequestHandler<ListItemNamesRequest, string>
{
    public async IAsyncEnumerable<string> Handle(
        ListItemNamesRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return $"item-{i}";
            await Task.Yield();
        }
    }
}

public sealed record StreamTicksRequest(int Count, int IntervalMs) : IStreamRequest<DateTime>;

public sealed class StreamTicksHandler : IStreamRequestHandler<StreamTicksRequest, DateTime>
{
    public async IAsyncEnumerable<DateTime> Handle(
        StreamTicksRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            // Observable delay to showcase real-time streaming and cancellation
            await Task.Delay(request.IntervalMs, cancellationToken);
            yield return DateTime.UtcNow;
        }
    }
}

public partial class Program { }
