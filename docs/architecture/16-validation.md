# 16 — Validation

## Abstraction

```csharp
public interface IConduitValidator<in TRequest>
{
    ValueTask<ValidationResult> Validate(TRequest request, CancellationToken cancellationToken);
}

public sealed record ValidationResult(bool IsValid, IReadOnlyList<ValidationFailure> Failures)
{
    public static readonly ValidationResult Success = new(true, Array.Empty<ValidationFailure>());
}

public sealed record ValidationFailure(string PropertyName, string ErrorMessage, string? ErrorCode = null);
```

**Rationale**: Conduit defines its own minimal `IConduitValidator<TRequest>` rather than taking a hard dependency on FluentValidation's `IValidator<T>`, so consumers who use DataAnnotations, a custom validation engine, or no external library at all still get first-class pipeline integration. `Conduit.Validation` (OSS) ships the pipeline behavior; adapters bridge specific libraries.

## The Validation Behavior

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IConduitValidator<TRequest>> _validators;

    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.Validate(request, ct);
            if (!result.IsValid) failures.AddRange(result.Failures);
        }
        if (failures.Count > 0) throw new ConduitValidationException(failures);
        return await next();
    }
}
```

Multiple validators per request type are supported (composable, single-responsibility validators) rather than one monolithic validator class — consistent with the "compose, don't inherit" principle from the non-negotiables.

## FluentValidation Integration

`Conduit.Validation.FluentValidation` (a thin adapter package) provides:

```csharp
public sealed class FluentValidationAdapter<TRequest> : IConduitValidator<TRequest>
{
    private readonly IValidator<TRequest> _validator;

    public async ValueTask<ValidationResult> Validate(TRequest request, CancellationToken ct)
    {
        var result = await _validator.ValidateAsync(request, ct);
        return result.IsValid
            ? ValidationResult.Success
            : new ValidationResult(false, result.Errors.Select(e => new ValidationFailure(e.PropertyName, e.ErrorMessage, e.ErrorCode)).ToArray());
    }
}

public static IServiceCollection AddFluentValidationAdapter(this IServiceCollection services) // registers FluentValidationAdapter<> as IConduitValidator<>
```

This keeps `Conduit.Validation`'s core free of any third-party dependency, while making FluentValidation a one-line opt-in for teams who already use it.

## Compile-Time Validation Hooks

Where validation rules are simple and static (non-null, range, regex — the DataAnnotations subset), a **future** `Conduit.Validation.SourceGenerators` addition (see [Roadmap](25-roadmap.md)) could generate an `IConduitValidator<TRequest>` implementation directly from `[Required]`/`[Range]` attributes on the request's record properties at compile time — turning what would otherwise be a reflection-based `Validator.TryValidateObject` call into a generated, allocation-free method. This is documented as a design direction, not committed for the initial release, because the ROI depends on adoption data from the initial FluentValidation-adapter-based release.

## Future Extensibility

Any validation library can be adapted by implementing `IConduitValidator<TRequest>` — there is no closed set of "supported" validators, and no marker attribute required on the request type itself (validators are matched to requests purely through the open-generic DI registration `IConduitValidator<TRequest>`, resolved by the same compile-time-generated wiring as handlers).
