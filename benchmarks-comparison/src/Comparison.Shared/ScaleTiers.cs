namespace Comparison.Shared;

/// <summary>
/// Defines the scaling parameters for the benchmarks.
/// </summary>
public static class ScaleTiers
{
    public static readonly int[] BehaviorCounts = [0, 1, 5, 10, 20];
    public static readonly int[] NotificationHandlerCounts = [1, 10, 50, 100];
    public static readonly int[] ConcurrencyLevels = [1, 8, 32, 128];
    public const int TypeVarietyCount = 50;
}
