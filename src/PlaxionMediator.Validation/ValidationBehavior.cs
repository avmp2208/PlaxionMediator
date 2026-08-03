using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Validation;

/// <summary>
/// Pipeline behavior that runs all registered <see cref="IPlaxionMediatorValidator{TRequest}"/> instances
/// for the current request, aggregates failures, and throws <see cref="PlaxionMediatorValidationException"/>
/// when validation fails.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IPlaxionMediatorValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators registered for <typeparamref name="TRequest"/>.</param>
    public ValidationBehavior(IEnumerable<IPlaxionMediatorValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        // Fast path: no validators registered for this request type.
        // Avoid allocating a list when the DI enumerable is already empty.
        if (_validators is ICollection<IPlaxionMediatorValidator<TRequest>> collection)
        {
            if (collection.Count == 0)
            {
                return await next().ConfigureAwait(false);
            }
        }
        else if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        List<PlaxionMediatorValidationFailure>? failures = null;

        foreach (IPlaxionMediatorValidator<TRequest> validator in _validators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (validator is null)
            {
                continue;
            }

            PlaxionMediatorValidationResult result = await validator
                .ValidateAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (result is null || result.IsValid)
            {
                continue;
            }

            failures ??= new List<PlaxionMediatorValidationFailure>();
            failures.AddRange(result.Failures);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (failures is { Count: > 0 })
        {
            throw new PlaxionMediatorValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
