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

# Run a single class with the full, reproducible job
dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison -- --filter *PipelineBehavior*
```

> **Note:** There is intentionally no "Dry"/single-iteration job wired into the runner. Every run
> uses the same explicit, reproducible job (see below) so results are comparable across machines
> and across releases. If you only want to sanity-check that the code compiles and executes
> without collecting real numbers, pass `--job Short` on the CLI for a quick local check — just
> don't use those numbers for anything you intend to publish or compare.

> **Note:** Build/run the benchmark **project** (`src/Plaxion.BenchMarks.Comparison`) rather than relying solely on the `.sln` for Release runs. Project references into the main repo's PlaxionMediator sources are outside the comparison solution, and a solution-level Release build may still resolve those dependencies as Debug. Building the project with `-c Release` pulls optimized Plaxion assemblies correctly.

## Exported Results

The runner configures BenchmarkDotNet with:

- An explicit, reproducible measurement job (`Job.Default` with `WarmupCount = 3`,
  `IterationCount = 10`, `LaunchCount = 1`) — no `Dry`/single-iteration job is used, so numbers
  are suitable for publishing and for comparing across releases.
- `MemoryDiagnoser` (`Allocated`, `Gen0`, ... columns)
- `RankColumn.Arabic` (relative `Rank` of each benchmark within its class)
- `BaselineRatioColumn.RatioMean` (the `Ratio` column, relative to each class's `[Benchmark(Baseline = true)]` method — the Plaxion 0-behavior/1-caller/1-handler/50-types variant in each class)
- `StatisticColumn.Mean` / `StatisticColumn.StdDev` / `StatisticColumn.Error`
- JSON exporter (`JsonExporter.Full` → `*-report-full.json`)
- CSV exporter (`CsvExporter.Default` → `*-report.csv`)
- Markdown exporter (`MarkdownExporter.GitHub` → `*-report-github.md`)

Each benchmark class isolates its own `ServiceProvider`(s) in `[GlobalSetup]`/`[GlobalCleanup]` and
does not share mutable state with other classes, so classes can be run independently or together
with identical, deterministic results run-to-run.

Results are written under the benchmark project folder:

```text
src/Plaxion.BenchMarks.Comparison/BenchmarkDotNet.Artifacts/results/
```

See [`RESULTS.md`](RESULTS.md) for a human-readable snapshot of the latest full-suite run
(Mean/Ratio/Rank/Allocated per scenario, plus a summary comparing PlaxionMediator, MediatR, and
Mediator). Regenerate that file after re-running the suite so it stays in sync with the raw
artifacts above.

## License & Dependencies

- **MediatR** is pinned to version **12.5.0**. This is the last version released under the MIT license before the switch to a commercial dual-license model in MediatR 13. By pinning to this version, this comparison suite remains fully open-source and redistributable without requiring a MediatR.io license key.
