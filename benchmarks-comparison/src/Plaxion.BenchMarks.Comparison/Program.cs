using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;

// BenchmarkSwitcher natively supports "--list flat"/"--list tree"/"--help" for CI/build
// verification without running the (long) benchmarks themselves.
//
// CreateMinimumViable keeps default columns + console logger without the stock exporters,
// so we can add MemoryDiagnoser / JSON / CSV / Markdown exactly once (no duplicate-exporter warnings).
var config = ManualConfig
    .CreateMinimumViable()
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(JsonExporter.Full)
    .AddExporter(CsvExporter.Default)
    .AddExporter(MarkdownExporter.GitHub)
    .WithArtifactsPath(ResolveArtifactsPath());

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

// Prefer a stable artifacts root next to this project (…/Plaxion.BenchMarks.Comparison/BenchmarkDotNet.Artifacts),
// independent of the caller's working directory. Fall back to CWD when the project folder can't be located.
static string ResolveArtifactsPath()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Plaxion.BenchMarks.Comparison.csproj")))
            return Path.Combine(dir.FullName, "BenchmarkDotNet.Artifacts");
    }

    return Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts");
}

public partial class Program;
