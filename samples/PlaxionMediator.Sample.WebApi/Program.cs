using System.Collections.Concurrent;
using System.Linq;
using FluentValidation;
using PlaxionMediator.Abstractions;
using PlaxionMediator.AspNetCore;
using PlaxionMediator.Caching;
using PlaxionMediator.Core;
using PlaxionMediator;
using PlaxionMediator.MinimalApis;
using PlaxionMediator.Retry;
using PlaxionMediator.Validation;
using PlaxionMediator.Validation.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Global behavior order (outermost → innermost → handler):
// Validation → Caching → CircuitBreaker → Retry → Handler
// Validation fails fast; caching short-circuits before circuit breaker/retry/handler on hit;
// circuit breaker is registered outside retry so an open circuit fails fast before any retry
// attempts are made; retry wraps the handler only.
builder.Services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorValidationBehavior();
    o.UsePlaxionMediatorCachingBehavior();
    o.UsePlaxionMediatorCircuitBreakerBehavior();
    o.UsePlaxionMediatorRetryBehavior();
});
builder.Services.AddPlaxionMediatorFluentValidation(typeof(Program).Assembly);
builder.Services.AddPlaxionMediatorCaching(o =>
{
    o.DefaultCacheDuration = TimeSpan.FromMinutes(5);
});
builder.Services.AddPlaxionMediatorRetry(o =>
{
    // Keep sample/integration tests fast; production apps typically use larger delays.
    o.MaxRetryAttempts = 5;
    o.BaseDelay = TimeSpan.FromMilliseconds(1);
    o.BackoffStrategy = RetryBackoffStrategy.Exponential;
    // Looser coupling: Retry does not reference Validation; sample wires the exception type explicitly.
    o.NonRetryableExceptionTypes.Add(typeof(PlaxionMediatorValidationException));
});
builder.Services.AddPlaxionMediatorCircuitBreaker(o =>
{
    // Keep sample/integration tests fast and deterministic; production apps typically use larger windows.
    o.FailureRatio = 0.5;
    o.MinimumThroughput = 2;
    o.SamplingDuration = TimeSpan.FromSeconds(10);
    o.BreakDuration = TimeSpan.FromMilliseconds(500);
});
builder.Services.AddSingleton<ItemStore>();
builder.Services.AddSingleton<GetItemInvocationCounter>();
builder.Services.AddSingleton<TransientFailureSimulator>();
builder.Services.AddSingleton<FlakyDownstreamSimulator>();

var app = builder.Build();

// Must be registered before routing/endpoints so handler exceptions become problem+json.
app.UsePlaxionMediatorExceptionHandling();

app.MapGet("/", () => "PlaxionMediator WebApi sample is running.");

app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items")
    .WithName("CreateItem")
    .Produces<ItemDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

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
    .Produces<ItemDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

// PATCH binds TRequest from JSON body, same as PUT. PlaxionMediator does not implement
// JSON Merge Patch/JSON Patch semantics; RenameItemRequest here still carries the full desired name.
app.MapPlaxionMediatorPatch<RenameItemRequest, ItemDto>("/items/rename")
    .WithName("RenameItem")
    .Produces<ItemDto>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest);

app.MapPlaxionMediatorDelete<DeleteItemRequest, DeleteItemResponse>("/items/{id:guid}")
    .WithName("DeleteItem")
    .Produces<DeleteItemResponse>(StatusCodes.Status200OK);

// Demo: retryable request that fails a configurable number of times before succeeding.
app.MapPlaxionMediatorPost<UnstableOperationRequest, UnstableOperationResponse>("/demo/unstable")
    .WithName("UnstableOperation")
    .Produces<UnstableOperationResponse>(StatusCodes.Status200OK);

// Demo: circuit-breaker-guarded request. Registered outside retry, so once the breaker opens,
// calls fail fast with BrokenCircuitException before any retry attempt or the handler runs.
app.MapPlaxionMediatorPost<FlakyDownstreamRequest, FlakyDownstreamResponse>("/demo/circuit-breaker")
    .WithName("FlakyDownstream")
    .Produces<FlakyDownstreamResponse>(StatusCodes.Status200OK);

// Diagnostics: how many times GetItemHandler ran (proves cache hits skip the handler).
app.MapGet("/demo/cache/get-item-invocations", (GetItemInvocationCounter counter) =>
    Results.Ok(new InvocationCountDto(counter.Count)));

// Diagnostics: configure/reset the transient failure simulator used by /demo/unstable.
app.MapPost("/demo/unstable/configure", (ConfigureUnstableDto body, TransientFailureSimulator simulator) =>
{
    simulator.Configure(body.FailuresBeforeSuccess);
    return Results.Ok(new UnstableConfigDto(simulator.FailuresBeforeSuccess, simulator.Attempts));
});

app.MapGet("/demo/unstable/status", (TransientFailureSimulator simulator) =>
    Results.Ok(new UnstableConfigDto(simulator.FailuresBeforeSuccess, simulator.Attempts)));

// Diagnostics: configure/reset the circuit breaker demo simulator used by /demo/circuit-breaker.
app.MapPost("/demo/circuit-breaker/configure", (ConfigureFlakyDownstreamDto body, FlakyDownstreamSimulator simulator) =>
{
    simulator.Configure(body.AlwaysFail);
    return Results.Ok(new FlakyDownstreamStatusDto(simulator.AlwaysFail, simulator.Attempts));
});

app.MapGet("/demo/circuit-breaker/status", (FlakyDownstreamSimulator simulator) =>
    Results.Ok(new FlakyDownstreamStatusDto(simulator.AlwaysFail, simulator.Attempts)));

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

public sealed record InvocationCountDto(int Count);

public sealed record ConfigureUnstableDto(int FailuresBeforeSuccess);

public sealed record UnstableConfigDto(int FailuresBeforeSuccess, int Attempts);

public sealed record UnstableOperationResponse(string Payload, int Attempts);

public sealed record ConfigureFlakyDownstreamDto(bool AlwaysFail);

public sealed record FlakyDownstreamStatusDto(bool AlwaysFail, int Attempts);

public sealed record FlakyDownstreamResponse(string Payload, int Attempts);

// --- Store & demo services ---

/// <summary>
/// Counts GetItemHandler executions so cache-hit integration tests can assert the handler was skipped.
/// </summary>
public sealed class GetItemInvocationCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);

    public void Reset() => Interlocked.Exchange(ref _count, 0);
}

/// <summary>
/// Simulates a flaky dependency: fails <see cref="FailuresBeforeSuccess"/> times, then succeeds.
/// </summary>
public sealed class TransientFailureSimulator
{
    private int _failuresBeforeSuccess;
    private int _attempts;

    public int FailuresBeforeSuccess => Volatile.Read(ref _failuresBeforeSuccess);

    public int Attempts => Volatile.Read(ref _attempts);

    public void Configure(int failuresBeforeSuccess)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(failuresBeforeSuccess);
        Interlocked.Exchange(ref _failuresBeforeSuccess, failuresBeforeSuccess);
        Interlocked.Exchange(ref _attempts, 0);
    }

    public void ThrowIfShouldFail()
    {
        int attempt = Interlocked.Increment(ref _attempts);
        int remainingBudget = Volatile.Read(ref _failuresBeforeSuccess);
        if (attempt <= remainingBudget)
        {
            throw new InvalidOperationException($"Simulated transient failure (attempt {attempt}/{remainingBudget}).");
        }
    }
}

/// <summary>
/// Simulates a persistently failing downstream dependency for the circuit breaker demo:
/// fails every call while <see cref="AlwaysFail"/> is <see langword="true"/>, otherwise succeeds.
/// </summary>
public sealed class FlakyDownstreamSimulator
{
    private int _alwaysFail = 1;
    private int _attempts;

    public bool AlwaysFail => Volatile.Read(ref _alwaysFail) != 0;

    public int Attempts => Volatile.Read(ref _attempts);

    public void Configure(bool alwaysFail)
    {
        Interlocked.Exchange(ref _alwaysFail, alwaysFail ? 1 : 0);
        Interlocked.Exchange(ref _attempts, 0);
    }

    public void ThrowIfShouldFail()
    {
        Interlocked.Increment(ref _attempts);
        if (AlwaysFail)
        {
            throw new InvalidOperationException("Simulated persistent downstream failure.");
        }
    }
}

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

    public int Count => _items.Count;
}

// --- Requests & Handlers ---

public sealed record CreateItemRequest(string Name) : IRequest<ItemDto>;

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class CreateItemHandler : IRequestHandler<CreateItemRequest, ItemDto>
{
    private readonly ItemStore _store;

    public CreateItemHandler(ItemStore store) => _store = store;

    public ValueTask<ItemDto> Handle(CreateItemRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_store.Add(request.Name.Trim()));
    }
}

public sealed record GetItemRequest(Guid Id) : IRequest<ItemDto>, ICacheableRequest<ItemDto>
{
    public string CacheKey => $"item:{Id}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class GetItemHandler : IRequestHandler<GetItemRequest, ItemDto>
{
    private readonly ItemStore _store;
    private readonly GetItemInvocationCounter _counter;

    public GetItemHandler(ItemStore store, GetItemInvocationCounter counter)
    {
        _store = store;
        _counter = counter;
    }

    public ValueTask<ItemDto> Handle(GetItemRequest request, CancellationToken cancellationToken)
    {
        _counter.Increment();

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

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class UpdateItemHandler : IRequestHandler<UpdateItemRequest, ItemDto>
{
    private readonly ItemStore _store;
    private readonly IPlaxionMediatorCacheInvalidator _cacheInvalidator;

    public UpdateItemHandler(ItemStore store, IPlaxionMediatorCacheInvalidator cacheInvalidator)
    {
        _store = store;
        _cacheInvalidator = cacheInvalidator;
    }

    public ValueTask<ItemDto> Handle(UpdateItemRequest request, CancellationToken cancellationToken)
    {
        if (!_store.TryUpdate(request.Id, request.Name.Trim(), out ItemDto? item) || item is null)
        {
            throw new KeyNotFoundException($"Item '{request.Id}' was not found.");
        }

        _cacheInvalidator.Remove($"item:{request.Id}");
        return ValueTask.FromResult(item);
    }
}

public sealed record RenameItemRequest(Guid Id, string Name) : IRequest<ItemDto>;

public sealed class RenameItemRequestValidator : AbstractValidator<RenameItemRequest>
{
    public RenameItemRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class RenameItemHandler : IRequestHandler<RenameItemRequest, ItemDto>
{
    private readonly ItemStore _store;
    private readonly IPlaxionMediatorCacheInvalidator _cacheInvalidator;

    public RenameItemHandler(ItemStore store, IPlaxionMediatorCacheInvalidator cacheInvalidator)
    {
        _store = store;
        _cacheInvalidator = cacheInvalidator;
    }

    public ValueTask<ItemDto> Handle(RenameItemRequest request, CancellationToken cancellationToken)
    {
        if (!_store.TryUpdate(request.Id, request.Name.Trim(), out ItemDto? item) || item is null)
        {
            throw new KeyNotFoundException($"Item '{request.Id}' was not found.");
        }

        _cacheInvalidator.Remove($"item:{request.Id}");
        return ValueTask.FromResult(item);
    }
}

public sealed record DeleteItemRequest(Guid Id) : IRequest<DeleteItemResponse>;

public sealed record DeleteItemResponse(Guid Id, bool Deleted);

public sealed class DeleteItemHandler : IRequestHandler<DeleteItemRequest, DeleteItemResponse>
{
    private readonly ItemStore _store;
    private readonly IPlaxionMediatorCacheInvalidator _cacheInvalidator;

    public DeleteItemHandler(ItemStore store, IPlaxionMediatorCacheInvalidator cacheInvalidator)
    {
        _store = store;
        _cacheInvalidator = cacheInvalidator;
    }

    public ValueTask<DeleteItemResponse> Handle(DeleteItemRequest request, CancellationToken cancellationToken)
    {
        bool deleted = _store.TryRemove(request.Id, out _);
        if (deleted)
        {
            _cacheInvalidator.Remove($"item:{request.Id}");
        }

        return ValueTask.FromResult(new DeleteItemResponse(request.Id, deleted));
    }
}

// --- Retry demo ---

public sealed record UnstableOperationRequest(string Payload) : IRequest<UnstableOperationResponse>, IRetryableRequest
{
    // Per-request overrides keep the demo deterministic and fast.
    public int? MaxRetryAttempts => 5;

    public TimeSpan? BaseDelay => TimeSpan.FromMilliseconds(1);
}

public sealed class UnstableOperationHandler : IRequestHandler<UnstableOperationRequest, UnstableOperationResponse>
{
    private readonly TransientFailureSimulator _simulator;

    public UnstableOperationHandler(TransientFailureSimulator simulator) => _simulator = simulator;

    public ValueTask<UnstableOperationResponse> Handle(
        UnstableOperationRequest request,
        CancellationToken cancellationToken)
    {
        _simulator.ThrowIfShouldFail();
        return ValueTask.FromResult(new UnstableOperationResponse(request.Payload, _simulator.Attempts));
    }
}

// --- Circuit breaker demo ---

public sealed record FlakyDownstreamRequest(string Payload) : IRequest<FlakyDownstreamResponse>, ICircuitBreakerRequest;

public sealed class FlakyDownstreamHandler : IRequestHandler<FlakyDownstreamRequest, FlakyDownstreamResponse>
{
    private readonly FlakyDownstreamSimulator _simulator;

    public FlakyDownstreamHandler(FlakyDownstreamSimulator simulator) => _simulator = simulator;

    public ValueTask<FlakyDownstreamResponse> Handle(
        FlakyDownstreamRequest request,
        CancellationToken cancellationToken)
    {
        _simulator.ThrowIfShouldFail();
        return ValueTask.FromResult(new FlakyDownstreamResponse(request.Payload, _simulator.Attempts));
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
