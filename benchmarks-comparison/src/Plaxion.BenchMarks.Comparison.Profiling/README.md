# Plaxion.BenchMarks.Comparison.Profiling

Single-scenario profiling harness for capturing isolated CPU/memory snapshots of PlaxionMediator, MediatR, and Mediator (martinothamar).

Unlike the BenchmarkDotNet project next door, this app runs **one** framework+scenario combination in a tight loop and exits, so external tools (`dotnet-trace`, `dotnet-gcdump`) can attach without cross-contamination.

## CLI

```bash
dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison.Profiling -- --help

dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison.Profiling -- \
  --framework Plaxion --scenario Send5 --duration-seconds 5 --ready-signal
```

| Argument | Values |
|----------|--------|
| `--framework` | `Plaxion`, `MediatR`, `Mediator` |
| `--scenario` | `Send0`, `Send5`, `Send10`, `Send20`, `TypeVariety50` |
| `--duration-seconds` | Timed loop (default 5) |
| `--iterations` | Fixed iteration count (overrides duration) |
| `--ready-signal` | Print `READY` after warmup |
| `--list` | Print all 15 combinations |

## Automation

From `benchmarks-comparison/`:

```powershell
.\scripts\run-profiling.ps1 -DurationSeconds 4
.\scripts\run-profiling.ps1 -Frameworks Plaxion,MediatR -Scenarios Send5,Send20
.\scripts\analyze-profiling.ps1
```

Results land under `profiling-results/<Framework>/<Scenario>/`.
