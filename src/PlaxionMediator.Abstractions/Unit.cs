namespace PlaxionMediator.Abstractions;

/// <summary>
/// Allocation-free "no response" type used when a request has no meaningful result.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The singleton <see cref="Unit"/> value.
    /// </summary>
    public static readonly Unit Value = default;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>Always equal.</summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>Never unequal.</summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
