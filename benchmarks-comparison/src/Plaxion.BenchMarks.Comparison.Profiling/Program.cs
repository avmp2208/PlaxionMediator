using System.Diagnostics;
using Comparison.MediatorAdapter;
using Comparison.MediatRAdapter;
using Comparison.PlaxionAdapter;
using Comparison.Shared;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Core;

namespace Plaxion.BenchMarks.Comparison.Profiling;

/// <summary>
/// Single-scenario profiling harness. Runs ONE framework+scenario combination in a tight loop
/// so an external tool (dotnet-trace / dotnet-gcdump) can attach and capture an isolated snapshot.
/// </summary>
internal static class Program
{
    private static readonly string[] Frameworks = ["Plaxion", "MediatR", "Mediator"];
    private static readonly string[] Scenarios = ["Send0", "Send5", "Send10", "Send20", "TypeVariety50"];

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || HasFlag(args, "--help") || HasFlag(args, "-h"))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        if (HasFlag(args, "--list"))
        {
            PrintCombinations();
            return 0;
        }

        string? framework = GetOption(args, "--framework");
        string? scenario = GetOption(args, "--scenario");
        double durationSeconds = GetDoubleOption(args, "--duration-seconds", defaultValue: 5.0);
        int? fixedIterations = GetNullableIntOption(args, "--iterations");
        bool readySignal = HasFlag(args, "--ready-signal");

        if (string.IsNullOrWhiteSpace(framework) || string.IsNullOrWhiteSpace(scenario))
        {
            Console.Error.WriteLine("ERROR: --framework and --scenario are required.");
            PrintUsage();
            return 2;
        }

        framework = NormalizeFramework(framework);
        scenario = NormalizeScenario(scenario);

        if (!Frameworks.Contains(framework, StringComparer.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"ERROR: Unknown framework '{framework}'. Valid: {string.Join(", ", Frameworks)}");
            return 2;
        }

        if (!Scenarios.Contains(scenario, StringComparer.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"ERROR: Unknown scenario '{scenario}'. Valid: {string.Join(", ", Scenarios)}");
            return 2;
        }

        if (durationSeconds <= 0 && fixedIterations is null or <= 0)
        {
            Console.Error.WriteLine("ERROR: Provide a positive --duration-seconds and/or --iterations.");
            return 2;
        }

        Console.WriteLine($"PID={Environment.ProcessId}");
        Console.WriteLine($"Framework={framework}");
        Console.WriteLine($"Scenario={scenario}");
        Console.WriteLine(fixedIterations is > 0
            ? $"Mode=Iterations({fixedIterations.Value})"
            : $"Mode=Duration({durationSeconds:0.###}s)");

        await using ScenarioRunner runner = ScenarioRunner.Create(framework, scenario);

        // Warmup so first-JIT / DI cold paths are outside the profiled window when possible.
        await runner.WarmupAsync(iterations: 1_000).ConfigureAwait(false);

        if (readySignal)
        {
            Console.WriteLine("READY");
            // Give external collectors a brief window to attach after READY.
            await Task.Delay(250).ConfigureAwait(false);
        }

        long iterations;
        Stopwatch sw = Stopwatch.StartNew();

        if (fixedIterations is > 0)
        {
            iterations = await runner.RunFixedAsync(fixedIterations.Value).ConfigureAwait(false);
        }
        else
        {
            iterations = await runner.RunForDurationAsync(TimeSpan.FromSeconds(durationSeconds)).ConfigureAwait(false);
        }

        sw.Stop();
        double opsPerSec = iterations / Math.Max(sw.Elapsed.TotalSeconds, 1e-9);

        Console.WriteLine($"DONE iterations={iterations} elapsed_ms={sw.Elapsed.TotalMilliseconds:F1} ops_per_sec={opsPerSec:F0}");
        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            Plaxion.BenchMarks.Comparison.Profiling — single-scenario profiler harness

            Usage:
              dotnet run -c Release -- --framework <Plaxion|MediatR|Mediator> --scenario <Send0|Send5|Send10|Send20|TypeVariety50> [options]

            Options:
              --framework <name>         Required. Target mediator framework.
              --scenario <name>          Required. Scenario to run in a tight loop.
              --duration-seconds <n>     Loop duration (default: 5). Ignored if --iterations is set.
              --iterations <n>           Fixed iteration count instead of timed loop.
              --ready-signal             Print READY after warmup (for external attach scripts).
              --list                     List all 15 framework x scenario combinations.
              --help, -h                 Show this help.

            Example (external capture):
              dotnet-trace collect -o cpu.nettrace --profile cpu-sampling -- \
                dotnet exec Plaxion.BenchMarks.Comparison.Profiling.dll --framework Plaxion --scenario Send5 --duration-seconds 5
            """);
    }

    private static void PrintCombinations()
    {
        foreach (string framework in Frameworks)
        {
            foreach (string scenario in Scenarios)
            {
                Console.WriteLine($"{framework}/{scenario}");
            }
        }
    }

    private static bool HasFlag(string[] args, string name)
        => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    private static string? GetOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static double GetDoubleOption(string[] args, string name, double defaultValue)
    {
        string? raw = GetOption(args, name);
        return double.TryParse(raw, out double value) ? value : defaultValue;
    }

    private static int? GetNullableIntOption(string[] args, string name)
    {
        string? raw = GetOption(args, name);
        return int.TryParse(raw, out int value) ? value : null;
    }

    private static string NormalizeFramework(string value) => value.Trim() switch
    {
        var v when v.Equals("Plaxion", StringComparison.OrdinalIgnoreCase) => "Plaxion",
        var v when v.Equals("PlaxionMediator", StringComparison.OrdinalIgnoreCase) => "Plaxion",
        var v when v.Equals("MediatR", StringComparison.OrdinalIgnoreCase) => "MediatR",
        var v when v.Equals("Mediator", StringComparison.OrdinalIgnoreCase) => "Mediator",
        _ => value.Trim()
    };

    private static string NormalizeScenario(string value) => value.Trim() switch
    {
        var v when v.Equals("Send0", StringComparison.OrdinalIgnoreCase) => "Send0",
        var v when v.Equals("Send5", StringComparison.OrdinalIgnoreCase) => "Send5",
        var v when v.Equals("Send10", StringComparison.OrdinalIgnoreCase) => "Send10",
        var v when v.Equals("Send20", StringComparison.OrdinalIgnoreCase) => "Send20",
        var v when v.Equals("TypeVariety50", StringComparison.OrdinalIgnoreCase) => "TypeVariety50",
        var v when v.Equals("TypeVariety", StringComparison.OrdinalIgnoreCase) => "TypeVariety50",
        _ => value.Trim()
    };
}

/// <summary>
/// Builds one framework/scenario DI graph and runs its dispatch operation in a tight loop.
/// </summary>
internal sealed class ScenarioRunner : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly Func<ValueTask> _dispatch;

    private ScenarioRunner(ServiceProvider provider, Func<ValueTask> dispatch)
    {
        _provider = provider;
        _dispatch = dispatch;
    }

    public static ScenarioRunner Create(string framework, string scenario)
    {
        return (framework, scenario) switch
        {
            ("Plaxion", "Send0") => CreatePlaxionSend(0),
            ("Plaxion", "Send5") => CreatePlaxionSend(5),
            ("Plaxion", "Send10") => CreatePlaxionSend(10),
            ("Plaxion", "Send20") => CreatePlaxionSend(20),
            ("Plaxion", "TypeVariety50") => CreatePlaxionTypeVariety(),

            ("MediatR", "Send0") => CreateMediatRSend(0),
            ("MediatR", "Send5") => CreateMediatRSend(5),
            ("MediatR", "Send10") => CreateMediatRSend(10),
            ("MediatR", "Send20") => CreateMediatRSend(20),
            ("MediatR", "TypeVariety50") => CreateMediatRTypeVariety(),

            ("Mediator", "Send0") => CreateMediatorSend(0),
            ("Mediator", "Send5") => CreateMediatorSend(5),
            ("Mediator", "Send10") => CreateMediatorSend(10),
            ("Mediator", "Send20") => CreateMediatorSend(20),
            ("Mediator", "TypeVariety50") => CreateMediatorTypeVariety(),

            _ => throw new ArgumentOutOfRangeException(nameof(scenario), $"Unsupported combo {framework}/{scenario}")
        };
    }

    public async Task WarmupAsync(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            await _dispatch().ConfigureAwait(false);
        }
    }

    public async Task<long> RunForDurationAsync(TimeSpan duration)
    {
        long iterations = 0;
        long deadline = Stopwatch.GetTimestamp() + (long)(duration.TotalSeconds * Stopwatch.Frequency);

        while (Stopwatch.GetTimestamp() < deadline)
        {
            await _dispatch().ConfigureAwait(false);
            iterations++;
        }

        return iterations;
    }

    public async Task<long> RunFixedAsync(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            await _dispatch().ConfigureAwait(false);
        }

        return iterations;
    }

    public ValueTask DisposeAsync()
    {
        _provider.Dispose();
        return ValueTask.CompletedTask;
    }

    private static ScenarioRunner CreatePlaxionSend(int behaviors)
    {
        ServiceProvider provider = PlaxionAdapterFactory.BuildServiceProviderForBehaviors(behaviors);
        ISender sender = provider.GetRequiredService<ISender>();
        PlaxionPipelineRequest request = new(new ScenarioPayload("pipeline", "profiling"));
        return new ScenarioRunner(provider, async () => { await sender.Send(request).ConfigureAwait(false); });
    }

    private static ScenarioRunner CreatePlaxionTypeVariety()
    {
        ServiceProvider provider = PlaxionAdapterFactory.BuildServiceProviderForTypeVariety();
        ISender sender = provider.GetRequiredService<ISender>();
        PlaxionMediator.Abstractions.IRequest<string>[] requests =
            PlaxionTypeVarietyRegistrar.GetRequests(new ScenarioPayload("type-variety", "profiling"));

        return new ScenarioRunner(provider, async () =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                await sender.Send(requests[i]).ConfigureAwait(false);
            }
        });
    }

    private static ScenarioRunner CreateMediatRSend(int behaviors)
    {
        ServiceProvider provider = MediatRAdapterFactory.BuildServiceProviderForBehaviors(behaviors);
        MediatR.IMediator mediator = provider.GetRequiredService<MediatR.IMediator>();
        MediatRPipelineRequest request = new(new ScenarioPayload("pipeline", "profiling"));
        return new ScenarioRunner(provider, async () => { await mediator.Send(request).ConfigureAwait(false); });
    }

    private static ScenarioRunner CreateMediatRTypeVariety()
    {
        ServiceProvider provider = MediatRAdapterFactory.BuildServiceProviderForTypeVariety();
        MediatR.IMediator mediator = provider.GetRequiredService<MediatR.IMediator>();
        MediatR.IRequest<string>[] requests =
            MediatRTypeVarietyRegistrar.GetRequests(new ScenarioPayload("type-variety", "profiling"));

        return new ScenarioRunner(provider, async () =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                await mediator.Send(requests[i]).ConfigureAwait(false);
            }
        });
    }

    private static ScenarioRunner CreateMediatorSend(int behaviors)
    {
        ServiceProvider provider = MediatorAdapterFactory.BuildServiceProviderForBehaviors(behaviors);
        Mediator.IMediator mediator = provider.GetRequiredService<Mediator.IMediator>();
        MediatorPipelineRequest request = new(new ScenarioPayload("pipeline", "profiling"));
        return new ScenarioRunner(provider, async () => { await mediator.Send(request).ConfigureAwait(false); });
    }

    private static ScenarioRunner CreateMediatorTypeVariety()
    {
        ServiceProvider provider = MediatorAdapterFactory.BuildServiceProviderForTypeVariety();
        Mediator.IMediator mediator = provider.GetRequiredService<Mediator.IMediator>();
        Mediator.IRequest<string>[] requests =
            MediatorTypeVarietyRegistrar.GetRequests(new ScenarioPayload("type-variety", "profiling"));

        return new ScenarioRunner(provider, async () =>
        {
            for (int i = 0; i < requests.Length; i++)
            {
                await mediator.Send(requests[i]).ConfigureAwait(false);
            }
        });
    }
}
