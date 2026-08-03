namespace PlaxionMediator.Validation;

/// <summary>
/// The outcome of validating a request.
/// </summary>
public sealed class PlaxionMediatorValidationResult
{
    private static readonly PlaxionMediatorValidationFailure[] EmptyFailures = [];

    /// <summary>
    /// A shared successful validation result with no failures.
    /// </summary>
    public static PlaxionMediatorValidationResult Success { get; } = new(EmptyFailures);

    private PlaxionMediatorValidationResult(IReadOnlyList<PlaxionMediatorValidationFailure> failures)
    {
        Failures = failures;
    }

    /// <summary>
    /// <see langword="true"/> when <see cref="Failures"/> is empty.
    /// </summary>
    public bool IsValid => Failures.Count == 0;

    /// <summary>
    /// The validation failures, if any.
    /// </summary>
    public IReadOnlyList<PlaxionMediatorValidationFailure> Failures { get; }

    /// <summary>
    /// Creates a failed result from the given failures.
    /// </summary>
    /// <param name="failures">One or more validation failures.</param>
    /// <returns>A result containing the supplied failures.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="failures"/> is empty.</exception>
    public static PlaxionMediatorValidationResult Failed(IEnumerable<PlaxionMediatorValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        PlaxionMediatorValidationFailure[] materialised = failures as PlaxionMediatorValidationFailure[]
            ?? failures.ToArray();

        if (materialised.Length == 0)
        {
            throw new ArgumentException("At least one validation failure is required.", nameof(failures));
        }

        for (int i = 0; i < materialised.Length; i++)
        {
            if (materialised[i] is null)
            {
                throw new ArgumentException("Validation failures cannot contain null entries.", nameof(failures));
            }
        }

        return new PlaxionMediatorValidationResult(materialised);
    }

    /// <summary>
    /// Creates a failed result from the given failures.
    /// </summary>
    public static PlaxionMediatorValidationResult Failed(params PlaxionMediatorValidationFailure[] failures)
        => Failed((IEnumerable<PlaxionMediatorValidationFailure>)failures);
}
