namespace Comparison.Shared;

/// <summary>
/// Interface for simulating work during benchmark execution to avoid JIT dead-code elimination.
/// </summary>
public interface ISimulatedWork
{
    void Do(ScenarioPayload payload);
}
