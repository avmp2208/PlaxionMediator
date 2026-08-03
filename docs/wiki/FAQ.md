# FAQ

## Behavior & Middleware

**Why do I need to call both `UsePlaxionMediatorValidationBehavior()` and `AddPlaxionMediatorFluentValidation()`?**
They solve two different problems: `UsePlaxionMediatorValidationBehavior()` enables the *logic* in the mediator pipeline to look for validators and run them. `AddPlaxionMediatorFluentValidation()` is what actually *finds* your FluentValidation classes in your assembly and registers them so the behavior can find them. Without both, the pipeline will either not look for validators at all, or it will find an empty list and do nothing.

**What is the recommended order for global behaviors?**
The recommended order is **Validation → Caching → Retry**.
1. **Validation**: Fail fast before any other processing happens.
2. **Caching**: If we have a cached result, return it immediately without triggering retry logic or the handler.
3. **Retry**: Wrap the handler execution to catch transient failures and retry them.

**Why doesn't the Retry behavior retry validation exceptions by default?**
Validation failures (like a missing required field) are not transient — retrying the exact same invalid request will result in the exact same failure. Retrying them would just waste CPU cycles and logs. `PlaxionMediator.Retry` allows you to configure `NonRetryableExceptionTypes` to exclude these types of errors.

## Caching & Persistence

**Does `PlaxionMediator.Caching` support Redis or other distributed caches?**
Currently, `PlaxionMediator.Caching` uses `IMemoryCache` for maximum performance and zero-dependency setup. Support for `IDistributedCache` (Redis, etc.) is on the roadmap. If you need it now, you can implement a custom `IPipelineBehavior<TRequest, TResponse>` that uses your preferred provider.

**How do I invalidate a cache entry when data changes?**
Inject `IPlaxionMediatorCacheInvalidator` into your command handler. You can then call `Remove("your-cache-key")` or `RemoveByRequestType<GetItemRequest>()` to ensure stale data is cleared after an update or delete operation.

## Design & Compatibility

**Can I use multiple validators for the same request?**
Yes. The `ValidationBehavior` resolves an `IEnumerable<IPlaxionMediatorValidator<TRequest>>`. It will run every registered validator for that request type and aggregate all errors into a single `PlaxionMediatorValidationException`.

**Can I use `PlaxionMediator.Retry` with Polly?**
`PlaxionMediator.Retry` is a lightweight implementation designed for Native AOT compatibility without pulling in large external dependencies. If you need advanced Polly features (Circuit Breaker, Bulkhead, etc.), we recommend creating a custom `IPipelineBehavior` that wraps your calls in a Polly policy.

**Why use PlaxionMediator instead of MediatR or Mediator?**
Frameworks like MediatR and Mediator are excellent tools that have served the .NET community for years. PlaxionMediator is an independent implementation—not a dependency of or built upon these existing libraries—designed to address specific modern requirements like **Native AOT compatibility** and **zero-reflection performance**. By leveraging Roslyn Source Generators to discover and register handlers at compile-time, it eliminates the need for runtime reflection, resulting in faster startup times and a smaller memory footprint, which is ideal for high-performance cloud-native and serverless environments.


## Contribution

**Where do I report bugs / request features?**
Open an issue on [GitHub](https://github.com/avmp2208/PlaxionMediator/issues); see [`CONTRIBUTING.md`](https://github.com/avmp2208/PlaxionMediator/blob/master/CONTRIBUTING.md) for guidelines.
