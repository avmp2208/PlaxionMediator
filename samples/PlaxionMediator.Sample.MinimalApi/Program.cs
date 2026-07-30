using System.Reflection.PortableExecutable;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Add PlaxionMediator (registers handlers discovered at compile time)
builder.Services.AddPlaxionMediator();

// 2. Register behaviors manually (MVP requires manual behavior registration for now)
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Registration order matters for behaviors
builder.Services.AddScoped<FlowTracker>();
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FlowBehaviorA<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FlowBehaviorB<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CacheBehavior<,>));

var app = builder.Build();

app.MapGet("/", () => "PlaxionMediator sample is running.");

app.MapGet("/ping", async (string? message, ISender sender, CancellationToken ct) =>
{
    string result = await sender.Send(new Ping(message ?? "from-api"), ct);
    return Results.Ok(new { result });
});

app.MapPost("/echo", async (Echo body, ISender sender, CancellationToken ct) =>
{
    string result = await sender.Send(new EchoRequest(body.Message), ct);
    return Results.Ok(new { result });
});

app.MapPost("/echo-class", async (Echo body, ISender sender, CancellationToken ct) =>
{
    // Testing non-record class request
    var request = new EchoClassRequest { Message = body.Message };
    string result = await sender.Send(request, ct);
    return Results.Ok(new { result });
});

app.MapPost("/notify", async (string message, IPublisher publisher, CancellationToken ct) =>
{
    // Testing notification fan-out
    await publisher.Publish(new PingNotification(message), ct);
    return Results.Accepted();
});

//Create a min api for the TestClass tes below
app.MapPost("/test-class", async (TestClass body, ISender sender, CancellationToken ct) =>
{
    string result = await sender.Send(body, ct);
    return Results.Ok(new { result });
});

app.MapGet("/fail", async (ISender sender, CancellationToken ct) =>
{
    await sender.Send(new FailRequest(), ct);
    return Results.Ok();
});

app.MapGet("/flow", async (ISender sender, FlowTracker tracker, CancellationToken ct) =>
{
    var result = await sender.Send(new FlowRequest(), ct);
    return Results.Ok(new { result, steps = tracker.Steps });
});

app.MapGet("/nested", async (ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new NestedRequest("hello"), ct);
    return Results.Ok(new { result });
});

app.Run();

// --- Requests & Handlers ---

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Pong: {request.Message}");
}

public sealed record EchoRequest(string Message) : IRequest<string>;

public sealed class EchoHandler : IRequestHandler<EchoRequest, string>
{
    public ValueTask<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Message);
}

public sealed record Echo(string Message);

// Testing a standard class request (non-record)
public sealed class EchoClassRequest : IRequest<string>
{
    public string Message { get; init; } = string.Empty;
}

public sealed class EchoClassHandler : IRequestHandler<EchoClassRequest, string>
{
    public ValueTask<string> Handle(EchoClassRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"ClassEcho: {request.Message}");
}

public class TestClass : IRequest<string>
{
    public string Type { get; init; } = string.Empty;
}

public class TestClassHandler : IRequestHandler<TestClass, string>
{
    private readonly ILogger<TestClassHandler> _logger;

    public TestClassHandler(ILogger<TestClassHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<string> Handle(TestClass request, CancellationToken cancellationToken)
    { 
        _logger.LogInformation("TestClassHandler received: {Type}", request.Type);
       return ValueTask.FromResult($"TestClass: {request.Type}");
    } 
}

public sealed record FailRequest : IRequest<Unit>;

public sealed class FailHandler : IRequestHandler<FailRequest, Unit>
{
    public ValueTask<Unit> Handle(FailRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Intentional failure");
    }
}

public sealed record FlowRequest : IRequest<string>;

public sealed class FlowHandler : IRequestHandler<FlowRequest, string>
{
    private readonly FlowTracker _tracker;
    public FlowHandler(FlowTracker tracker) => _tracker = tracker;

    public ValueTask<string> Handle(FlowRequest request, CancellationToken cancellationToken)
    {
        _tracker.Steps.Add("Handler");
        return ValueTask.FromResult("FlowDone");
    }
}

public sealed record NestedRequest(string Message) : IRequest<string>;

public sealed class NestedHandler : IRequestHandler<NestedRequest, string>
{
    private readonly ISender _sender;
    public NestedHandler(ISender sender) => _sender = sender;

    public async ValueTask<string> Handle(NestedRequest request, CancellationToken cancellationToken)
    {
        // Call another request from within a handler
        return await _sender.Send(new EchoRequest($"Nested: {request.Message}"), cancellationToken);
    }
}

// --- Notifications ---

public sealed record PingNotification(string Message) : INotification;

public sealed class PingNotificationHandler1 : INotificationHandler<PingNotification>
{
    private readonly ILogger<PingNotificationHandler1> _logger;
    public PingNotificationHandler1(ILogger<PingNotificationHandler1> logger) => _logger = logger;

    public ValueTask Handle(PingNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification Handler 1 received: {Message}", notification.Message);
        return ValueTask.CompletedTask;
    }
}

public sealed class PingNotificationHandler2 : INotificationHandler<PingNotification>
{
    private readonly ILogger<PingNotificationHandler2> _logger;
    public PingNotificationHandler2(ILogger<PingNotificationHandler2> logger) => _logger = logger;

    public ValueTask Handle(PingNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification Handler 2 received: {Message}", notification.Message);
        return ValueTask.CompletedTask;
    }
}

// --- Behaviors ---

public sealed class FlowTracker
{
    public List<string> Steps { get; } = new();
}

public sealed class FlowBehaviorA<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly FlowTracker _tracker;
    public FlowBehaviorA(FlowTracker tracker) => _tracker = tracker;

    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _tracker.Steps.Add("BehaviorA-Start");
        var response = await next();
        _tracker.Steps.Add("BehaviorA-End");
        return response;
    }
}

public sealed class FlowBehaviorB<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly FlowTracker _tracker;
    public FlowBehaviorB(FlowTracker tracker) => _tracker = tracker;

    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _tracker.Steps.Add("BehaviorB-Start");
        var response = await next();
        _tracker.Steps.Add("BehaviorB-End");
        return response;
    }
}

public sealed class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is Ping { Message: "cached" })
        {
            // Short-circuit: do NOT call next()
            return ValueTask.FromResult((TResponse)(object)"CachedResult");
        }
        return next();
    }
}

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting request {RequestName}", typeof(TRequest).Name);
        try
        {
            var response = await next();
            _logger.LogInformation("Finished request {RequestName}", typeof(TRequest).Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request {RequestName} failed", typeof(TRequest).Name);
            throw;
        }
    }
}

public partial class Program { }
