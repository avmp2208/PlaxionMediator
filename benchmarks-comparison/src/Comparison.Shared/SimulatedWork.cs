using System.Security.Cryptography;
using System.Text;

namespace Comparison.Shared;

/// <summary>
/// A concrete implementation of simulated work that performs non-trivial CPU operations.
/// </summary>
public sealed class SimulatedValidationWork : ISimulatedWork
{
    private readonly Dictionary<string, int> _cache = new();
    private int _counter;

    public void Do(ScenarioPayload payload)
    {
        // 1. Compute a simple hash-like value from Data
        var bytes = Encoding.UTF8.GetBytes(payload.Data);
        var hash = 0;
        foreach (var b in bytes)
        {
            hash = (hash * 31) + b;
        }

        var key = $"{payload.Id}_{hash}";

        // 2. Dictionary lookup/insert
        if (!_cache.TryGetValue(key, out var val))
        {
            val = hash;
            _cache[key] = val;
        }

        // 3. Increment counter based on the value
        _counter += val;

        // Keep the cache small to avoid memory pressure being the bottleneck
        if (_cache.Count > 1000)
        {
            _cache.Clear();
        }
    }
}
