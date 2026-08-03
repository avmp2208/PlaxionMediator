# Full Usage Guide

This guide provides a comprehensive set of examples for building applications with **PlaxionMediator**, covering core concepts, web API integration, and cross-cutting concerns like validation.

For a broader overview of why the framework exists and its core concepts, see the [Design Overview](Design-Overview).

---

## Core Usage: Request/Response

The fundamental building block is the **Request**. Requests are immutable `sealed record`s that define a contract for an operation.

### 1. Define a Request
```csharp
using PlaxionMediator.Abstractions;

// A request that returns a string result
public sealed record Ping(string Message) : IRequest<string>;
```

### 2. Implement a Handler
Every request must have exactly one handler. If you forget to implement one or implement multiple, the PlaxionMediator analyzer will fail your build (`PlaxionMediator001`/`PlaxionMediator002`).

```csharp
public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult($"Pong: {request.Message}");
    }
}
```

### 3. Registration and Dispatch
Register your handlers at startup. PlaxionMediator uses a source generator to discover handlers at compile-time, so there is **zero reflection** at runtime.

```csharp
var services = new ServiceCollection();

// Discovers all handlers in the current assembly automatically
services.AddPlaxionMediator();

var provider = services.BuildServiceProvider();
var sender = provider.GetRequiredService<ISender>();

// Dispatch the request
var result = await sender.Send(new Ping("Hello World"));
```

---

## ASP.NET Core & Minimal APIs

PlaxionMediator provides first-class support for Minimal APIs through the `PlaxionMediator.MinimalApis` package, allowing you to map requests directly to routes.

### Setup
Ensure you have the following packages installed:
```bash
dotnet add package PlaxionMediator.AspNetCore
dotnet add package PlaxionMediator.MinimalApis
```

### Exception Handling
Register the exception handling middleware to automatically convert PlaxionMediator exceptions into RFC 7807 `ProblemDetails` responses.

```csharp
var app = builder.Build();

// Register before routing/endpoints
app.UsePlaxionMediatorExceptionHandling();
```

### Mapping Routes
Use the `MapPlaxionMediatorX` extension methods to bind routes directly to requests.

```csharp
// POST: Binds TRequest from JSON body
app.MapPlaxionMediatorPost<CreateItemRequest, ItemDto>("/items");

// GET: Binds TRequest from Route/Query parameters ([AsParameters])
app.MapPlaxionMediatorGet<GetItemRequest, ItemDto>("/items/{id}");

// PUT/PATCH/DELETE are also supported
app.MapPlaxionMediatorPut<UpdateItemRequest, ItemDto>("/items");
app.MapPlaxionMediatorDelete<DeleteItemRequest, DeleteItemResponse>("/items/{id}");
```

---

## Validation & FluentValidation

Validation is an opt-in cross-cutting concern provided by the `PlaxionMediator.Validation` package.

### Installation
```bash
dotnet add package PlaxionMediator.Validation
dotnet add package PlaxionMediator.Validation.FluentValidation // For FluentValidation support
```

### 1. Enable Global Validation
Call `UsePlaxionMediatorValidationBehavior()` on the mediator options to add `ValidationBehavior<,>` to your global behavior list. This behavior will automatically run all registered validators for a request before the handler executes. You never need to reference `ValidationBehavior<,>` directly.

```csharp
builder.Services.AddPlaxionMediator(options =>
{
    options.UsePlaxionMediatorValidationBehavior();
});
```

### 2. Register FluentValidation Validators
`UsePlaxionMediatorValidationBehavior()` only wires the pipeline *behavior* — it does not know about your `AbstractValidator<T>` classes. You still need to register them in the DI container so the behavior can resolve `IPlaxionMediatorValidator<TRequest>` for each request type. `AddPlaxionMediatorFluentValidation(typeof(Program).Assembly)` scans the given assembly for every `FluentValidation.IValidator<T>` implementation and registers a `FluentValidationAdapter<T>` (which implements `IPlaxionMediatorValidator<T>`) for it, so both calls are required together: one enables the behavior in the pipeline, the other registers the concrete validators the behavior will run.

```csharp
builder.Services.AddPlaxionMediatorFluentValidation(typeof(Program).Assembly);
```

### 3. Define a Validator
```csharp
public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

If validation fails, the behavior throws a `PlaxionMediatorValidationException`, which is caught by the `PlaxionMediator.AspNetCore` middleware and returned as a `400 Bad Request` with validation details.

---

## Caching & Retry

Caching and Retry are opt-in cross-cutting concerns provided by the `PlaxionMediator.Caching` and `PlaxionMediator.Retry` packages.

### Installation
```bash
dotnet add package PlaxionMediator.Caching
dotnet add package PlaxionMediator.Retry
```

### 1. Enable Behaviors
Call the `UsePlaxionMediatorXBehavior()` extension methods to add the behaviors to your global behavior list; you never need to reference `CachingBehavior<,>`/`RetryBehavior<,>` directly. The recommended order is **Validation → Caching → Retry → Handler**.

```csharp
builder.Services.AddPlaxionMediator(options =>
{
    options.UsePlaxionMediatorValidationBehavior();
    options.UsePlaxionMediatorCachingBehavior();
    options.UsePlaxionMediatorRetryBehavior();
});

// Configure Caching
builder.Services.AddPlaxionMediatorCaching(options =>
{
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
});

// Configure Retry
builder.Services.AddPlaxionMediatorRetry(options =>
{
    options.MaxRetryAttempts = 3;
    options.BaseDelay = TimeSpan.FromMilliseconds(200);
    options.BackoffStrategy = RetryBackoffStrategy.Exponential;
});
```

### 2. Caching: `ICacheableRequest`
To enable caching for a request, implement the `ICacheableRequest<TResponse>` interface.

```csharp
public sealed record GetItemRequest(Guid Id) : IRequest<ItemDto>, ICacheableRequest<ItemDto>
{
    public string CacheKey => $"item:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10); // Optional override
}
```

You can manually invalidate cache entries using `IPlaxionMediatorCacheInvalidator`.

```csharp
public sealed class UpdateItemHandler : IRequestHandler<UpdateItemRequest, ItemDto>
{
    private readonly IPlaxionMediatorCacheInvalidator _cache;
    public UpdateItemHandler(IPlaxionMediatorCacheInvalidator cache) => _cache = cache;

    public async ValueTask<ItemDto> Handle(UpdateItemRequest request, CancellationToken ct)
    {
        // ... perform update ...
        _cache.Remove($"item:{request.Id}");
        return result;
    }
}
```

### 3. Retry: `IRetryableRequest`
To enable retries for a request, implement the `IRetryableRequest` interface.

```csharp
public sealed record UnstableRequest(string Data) : IRequest<string>, IRetryableRequest
{
    public int? MaxRetryAttempts => 5; // Optional override
    public TimeSpan? BaseDelay => TimeSpan.FromSeconds(1); // Optional override
}
```

The `RetryBehavior` will automatically retry the operation if a transient exception occurs, following the configured backoff strategy.

---

## Real-World Examples

The repository contains several sample projects that demonstrate full CRUD implementations, advanced validation scenarios, and streaming:

- **[PlaxionMediator.Sample.WebApi](https://github.com/avmp2208/PlaxionMediator/tree/master/samples/PlaxionMediator.Sample.WebApi)**: A comprehensive Web API project using `PlaxionMediator.AspNetCore`, `MinimalApis`, and `Validation`.
- **[PlaxionMediator.Sample.MinimalApi](https://github.com/avmp2208/PlaxionMediator/tree/master/samples/PlaxionMediator.Sample.MinimalApi)**: A lightweight example focused on Minimal API integration.

You can also find **Postman collections** in the [`postman-tests`](https://github.com/avmp2208/PlaxionMediator/tree/master/postman-tests) folder to test the sample APIs locally.
