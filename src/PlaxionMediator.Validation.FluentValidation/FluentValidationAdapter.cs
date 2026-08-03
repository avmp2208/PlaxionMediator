using FluentValidation;
using FluentValidation.Results;

namespace PlaxionMediator.Validation.FluentValidation;

/// <summary>
/// Adapts one or more FluentValidation <see cref="IValidator{T}"/> instances
/// into an <see cref="IPlaxionMediatorValidator{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">The request type being validated.</typeparam>
public sealed class FluentValidationAdapter<TRequest> : IPlaxionMediatorValidator<TRequest>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance that aggregates the given FluentValidation validators.
    /// </summary>
    /// <param name="validators">The FluentValidation validators registered for <typeparamref name="TRequest"/>.</param>
    public FluentValidationAdapter(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    /// <inheritdoc />
    public async ValueTask<PlaxionMediatorValidationResult> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        List<PlaxionMediatorValidationFailure>? failures = null;

        foreach (IValidator<TRequest> validator in _validators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (validator is null)
            {
                continue;
            }

            ValidationResult result = await validator
                .ValidateAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (result is null || result.IsValid)
            {
                continue;
            }

            failures ??= new List<PlaxionMediatorValidationFailure>(result.Errors.Count);
            foreach (ValidationFailure error in result.Errors)
            {
                if (error is null)
                {
                    continue;
                }

                failures.Add(new PlaxionMediatorValidationFailure(
                    error.PropertyName ?? string.Empty,
                    error.ErrorMessage ?? string.Empty));
            }
        }

        if (failures is { Count: > 0 })
        {
            return PlaxionMediatorValidationResult.Failed(failures);
        }

        return PlaxionMediatorValidationResult.Success;
    }
}
