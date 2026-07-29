# 09 — Dependency Injection

## The Registration Experience

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConduit(options =>
{
    options.DefaultHandlerLifetime = ServiceLifetime.Scoped;
    options.GlobalBehaviors.Add(typeof(LoggingBehavior<,>));
    options.GlobalBehaviors.Add(typeof(ValidationBehavior<,>));
});
```

A developer writes **one line** (`AddConduit()`), and every `IRequestHandler<,>`, `INotificationHandler<>`, and `IPipelineBehavior<,>` implementation *in the current compilation* is registered — with zero calls to `Assembly.GetTypes()`, zero `typeof(x).GetInterfaces()`, and zero runtime scanning of any kind.

## How Compile-Time Registration Works

1. During compilation of the consuming project, `Conduit.SourceGenerators` (an Incremental Generator, see [Source Generator Architecture](10-source-generator-architecture.md)) inspects the Roslyn `Compilation` for every type implementing `IRequestHandler<,>`, `INotificationHandler<>`, `IStreamRequestHandler<,>`, or `IPipelineBehavior<,>`.
2. It emits a partial method body for `AddConduit()` (declared as `partial` in `Conduit.DependencyInjection` and implemented by the generator in the consuming assembly) that calls `services.AddScoped<IRequestHandler<CreateOrderCommand, OrderId>, CreateOrderHandler>()` — and one such line per discovered handler — using the exact, compiler-known types.
3. Because this happens once per build, not once per process start, the "scanning cost" traditional frameworks pay at every cold start is paid exactly zero times at runtime — it's fully amortized into the build.

```csharp
// Illustrative generated code
namespace MyApp.Generated;

partial class ConduitRegistration
{
    public static partial void RegisterHandlers(IServiceCollection services, ConduitOptions options)
    {
        services.Add(new ServiceDescriptor(typeof(IRequestHandler<CreateOrderCommand, OrderId>), typeof(CreateOrderHandler), options.DefaultHandlerLifetime));
        services.Add(new ServiceDescriptor(typeof(INotificationHandler<OrderShippedEvent>), typeof(SendShippingEmailHandler), options.DefaultHandlerLifetime));
        services.AddScoped<ISender, ConduitSender>();
    }
}
```

## No-Reflection Guarantees

| Guarantee | How it's enforced |
|---|---|
| No `Assembly.GetTypes()` / `GetExportedTypes()` in generated or hand-written runtime code | Code review + a CI analyzer rule (`CONDUIT-INTERNAL-001`) forbidding these APIs outside `Conduit.SourceGenerators` (which runs in the compiler process, not the app). |
| No `Type.GetInterfaces()` / `IsAssignableFrom` at runtime for handler matching | Handler-to-request matching is resolved by Roslyn `SemanticModel` symbol comparison at compile time only. |
| No `Activator.CreateInstance` | All instantiation goes through `IServiceProvider`, which itself is reflection-free for AOT-compiled constructors when using the `Microsoft.Extensions.DependencyInjection` AOT-friendly resolution path. |
| No `MakeGenericType` / `MakeGenericMethod` at runtime | Every closed generic type (e.g., `IRequestHandler<CreateOrderCommand, OrderId>`) is written as a literal in generated source — the JIT/AOT compiler sees a concrete type, not a runtime-constructed one. |
| Native AOT trim-safe | Because every registration is a literal `typeof(ConcreteType)` in generated source, the trimmer can statically see and preserve exactly the types actually used — no `[DynamicallyAccessedMembers]` annotations needed. |

## Multi-Project / Multi-Assembly Composition

Each assembly that references `Conduit.SourceGenerators` gets its own generated `AddConduit()` partial implementation scoped to that assembly's handlers. A composition root (e.g., the Web API host project) calls `AddConduit()` once; if handlers live in a separate class library, that library also participates by exposing its own generated registration, invoked transitively via a generated `AddConduit<TMarker>()` overload that the generator wires per referenced assembly containing handlers — still zero reflection, because each assembly's set of handlers is known to that assembly's own compilation pass.

## Interaction with `IServiceCollection` Validation

Because handler registration is fully static, `ServiceProviderOptions.ValidateOnBuild = true` (the recommended default for Conduit apps) can catch constructor-dependency misconfiguration at host startup — combined with compile-time handler-existence guarantees, this means the only remaining failure category is "a handler's *own* dependency is unregistered," which is exactly the kind of error `ValidateOnBuild` is designed to catch immediately on startup rather than on first request.
