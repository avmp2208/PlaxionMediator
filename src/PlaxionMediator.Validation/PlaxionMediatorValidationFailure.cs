namespace PlaxionMediator.Validation;

/// <summary>
/// A single validation failure for a request property.
/// </summary>
public sealed class PlaxionMediatorValidationFailure
{
    /// <summary>
    /// Initializes a new validation failure.
    /// </summary>
    /// <param name="propertyName">The property that failed validation. May be empty for model-level errors.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    public PlaxionMediatorValidationFailure(string propertyName, string errorMessage)
    {
        PropertyName = propertyName ?? string.Empty;
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }

    /// <summary>
    /// The property that failed validation. Empty for model-level errors.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// The human-readable error message.
    /// </summary>
    public string ErrorMessage { get; }
}
