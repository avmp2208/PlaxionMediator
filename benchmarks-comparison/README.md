# Plaxion Benchmarks Comparison

This standalone solution is used for stress-testing and comparing **PlaxionMediator** against other popular mediator implementations in .NET:

- **MediatR**: The original and most widely used mediator library.
- **Mediator (by Martin Othamar)**: A high-performance implementation using source generators.

## Purpose

The suite focuses on heavy scenarios to identify performance characteristics and bottlenecks:
- Pipeline behavior chains (varying lengths)
- Concurrency and contention under high load
- Type variety (handling many different message types)
- Notification fan-out (one-to-many messaging)

## Running the Benchmarks

From the root of this sub-folder (`benchmarks-comparison/`):

```bash
dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison
```

Useful CLI options (passed after `--`):

```bash
# List all discovered benchmarks
dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison -- --list flat

# Smoke-test the entire suite with BenchmarkDotNet's Dry job (single iteration)
dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison -- --filter * --job Dry

# Run a single class
dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison -- --filter *PipelineBehavior*
```

> **Note:** Build/run the benchmark **project** (`src/Plaxion.BenchMarks.Comparison`) rather than relying solely on the `.sln` for Release runs. Project references into the main repo's PlaxionMediator sources are outside the comparison solution, and a solution-level Release build may still resolve those dependencies as Debug. Building the project with `-c Release` pulls optimized Plaxion assemblies correctly.

## Exported Results

The runner configures BenchmarkDotNet with:

- `MemoryDiagnoser`
- JSON exporter (`JsonExporter.Full` → `*-report-full.json`)
- CSV exporter (`CsvExporter.Default` → `*-report.csv`)
- Markdown exporter (`MarkdownExporter.GitHub` → `*-report-github.md`)

Results are written under the benchmark project folder:

```text
src/Plaxion.BenchMarks.Comparison/BenchmarkDotNet.Artifacts/results/
```

## License & Dependencies

- **MediatR** is pinned to version **12.5.0**. This is the last version released under the MIT license before the switch to a commercial dual-license model in MediatR 13. By pinning to this version, this comparison suite remains fully open-source and redistributable without requiring a MediatR.io license key.
