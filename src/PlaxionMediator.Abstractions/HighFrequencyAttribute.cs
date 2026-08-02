namespace PlaxionMediator.Abstractions;

/// <summary>
/// Marks a request type as high-frequency / hot-path.
/// Analyzers may warn when too many pipeline behaviors are attached to such requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class HighFrequencyAttribute : Attribute;
