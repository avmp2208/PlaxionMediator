using System.Text;
using PlaxionMediator.Core;

namespace PlaxionMediator.Validation;

/// <summary>
/// Thrown by <see cref="ValidationBehavior{TRequest,TResponse}"/> when one or more validators report failures.
/// </summary>
public sealed class PlaxionMediatorValidationException : PlaxionMediatorException
{
    /// <summary>
    /// Initializes a new instance with the given validation failures.
    /// </summary>
    /// <param name="failures">The validation failures that caused the exception.</param>
    public PlaxionMediatorValidationException(IEnumerable<PlaxionMediatorValidationFailure> failures)
        : base(BuildMessage(failures))
    {
        ArgumentNullException.ThrowIfNull(failures);

        PlaxionMediatorValidationFailure[] materialised = failures as PlaxionMediatorValidationFailure[]
            ?? failures.ToArray();

        if (materialised.Length == 0)
        {
            throw new ArgumentException("At least one validation failure is required.", nameof(failures));
        }

        Failures = materialised;
    }

    /// <summary>
    /// Initializes a new instance with a custom message and the given validation failures.
    /// </summary>
    public PlaxionMediatorValidationException(string message, IEnumerable<PlaxionMediatorValidationFailure> failures)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(failures);

        PlaxionMediatorValidationFailure[] materialised = failures as PlaxionMediatorValidationFailure[]
            ?? failures.ToArray();

        if (materialised.Length == 0)
        {
            throw new ArgumentException("At least one validation failure is required.", nameof(failures));
        }

        Failures = materialised;
    }

    /// <summary>
    /// The validation failures associated with this exception.
    /// </summary>
    public IReadOnlyList<PlaxionMediatorValidationFailure> Failures { get; }

    private static string BuildMessage(IEnumerable<PlaxionMediatorValidationFailure>? failures)
    {
        if (failures is null)
        {
            return "Validation failed.";
        }

        PlaxionMediatorValidationFailure[] materialised = failures as PlaxionMediatorValidationFailure[]
            ?? failures.ToArray();

        if (materialised.Length == 0)
        {
            return "Validation failed.";
        }

        StringBuilder builder = new("Validation failed: ");
        for (int i = 0; i < materialised.Length; i++)
        {
            if (i > 0)
            {
                builder.Append("; ");
            }

            PlaxionMediatorValidationFailure failure = materialised[i];
            if (!string.IsNullOrEmpty(failure.PropertyName))
            {
                builder.Append(failure.PropertyName).Append(": ");
            }

            builder.Append(failure.ErrorMessage);
        }

        return builder.ToString();
    }
}
