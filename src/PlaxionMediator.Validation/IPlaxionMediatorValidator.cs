namespace PlaxionMediator.Validation;

/// <summary>
/// Validates a request before the handler executes.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public interface IPlaxionMediatorValidator<in TRequest>
{
    /// <summary>
    /// Validates <paramref name="request"/> and returns a result describing any failures.
    /// </summary>
    ValueTask<PlaxionMediatorValidationResult> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
