# Latest Comparison Results

> Generated: 2026-08-06, via `dotnet run -c Release --project benchmarks-comparison/src/Plaxion.BenchMarks.Comparison --filter *`
> (re-run as part of the `v0.4.3` stabilization pass, against the `v0.4.2` baseline captured on 2026-08-04)
> Environment: BenchmarkDotNet v0.14.0, Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7 (RyuJIT AVX2)
> Job: `Job.Default` (WarmupCount=3, IterationCount=10, LaunchCount=1) — reproducible, non-Dry job.
>
> Raw exported files (JSON/CSV/GitHub-Markdown) live under
> `src/Plaxion.BenchMarks.Comparison/BenchmarkDotNet.Artifacts/results/`. This file is a
> human-readable snapshot of those results for quick reference; regenerate it whenever the suite
> is re-run so the numbers here stay in sync with the artifacts on disk.
>
> See the root-level `BENCHMARK_REPORT.md` for a narrative summary and
> `ARCHITECTURE_SUMMARY.md` for the design decisions behind these numbers.
>
> **`v0.4.3` regression gate:** all four scenarios below were re-run unchanged against the
> `v0.4.2` baseline (no `PipelineExecutor`/`PipelineRunner` internal tuning was made this release
> — the Core/Pipeline audit found no correctness or contention issue justifying a change, see
> `RELEASE_NOTES.md`). Every mean/allocation figure is within normal run-to-run noise (≤~5%) of
> the prior snapshot, and all `Allocated` figures are byte-for-byte identical — **no regression**
> versus `v0.4.2`, satisfying the Benchmark Strategy's regression gate.

## Pipeline Behavior Chains

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    15.64 ns |  0.67 |    1 |         - |
| Send_Plaxion_0Behaviors   |    23.45 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    52.15 ns |  2.22 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.21 ns |  2.91 |    4 |     128 B |
| Send_Plaxion_1Behavior    |   121.88 ns |  5.20 |    5 |     128 B |
| Send_MediatR_1Behavior    |   173.32 ns |  7.39 |    6 |     648 B |
| Send_Mediator_5Behaviors  |   307.58 ns | 13.11 |    7 |     640 B |
| Send_Plaxion_5Behaviors   |   392.78 ns | 16.75 |    8 |     640 B |
| Send_MediatR_5Behaviors   |   447.27 ns | 19.07 |    8 |    1896 B |
| Send_Mediator_10Behaviors |   596.48 ns | 25.43 |    9 |    1280 B |
| Send_Plaxion_10Behaviors  |   742.15 ns | 31.64 |    9 |    1280 B |
| Send_MediatR_10Behaviors  |   820.02 ns | 34.96 |    9 |    3456 B |
| Send_Mediator_20Behaviors | 1,315.22 ns | 56.08 |   10 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,502.94 ns | 64.08 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 1,644.02 ns | 70.10 |   11 |    6576 B |

**Takeaway:** Mediator (source-gen) remains the fastest, lowest-allocation option here. PlaxionMediator
tracks it closely at every depth — matching its allocation profile exactly (128/640/1280/2560 B) —
and stays consistently ahead of MediatR on both latency and allocations. Unchanged from the
`v0.4.2` baseline within run-to-run noise; allocation figures are identical.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   850.0 ns |  0.97 |    1 |         - |
| Dispatch_Plaxion_50Types  |   879.3 ns |  1.00 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,799.5 ns |  5.46 |    2 |   13200 B |

**Takeaway:** PlaxionMediator remains essentially tied with Mediator (ratio 1.00 vs 0.97) on this
scenario, while remaining **0 B** allocated — well ahead of MediatR, which allocates ~264 B/call.
Unchanged from the `v0.4.2` baseline.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.42 ns |   0.89 |    1 |     176 B |
| Concurrent_Plaxion_1    |    44.38 ns |   1.00 |    2 |     176 B |
| Concurrent_MediatR_1    |    74.44 ns |   1.68 |    3 |     368 B |
| Concurrent_Mediator_8   |   224.88 ns |   5.07 |    4 |     736 B |
| Concurrent_Plaxion_8    |   249.73 ns |   5.63 |    4 |     736 B |
| Concurrent_MediatR_8    |   550.49 ns |  12.41 |    5 |    2272 B |
| Concurrent_Mediator_32  |   872.18 ns |  19.66 |    6 |    2656 B |
| Concurrent_Plaxion_32   |   884.17 ns |  19.93 |    6 |    2656 B |
| Concurrent_MediatR_32   | 1,988.73 ns |  44.83 |    7 |    8800 B |
| Concurrent_Mediator_128 | 3,351.72 ns |  75.56 |    8 |   10336 B |
| Concurrent_Plaxion_128  | 3,610.68 ns |  81.39 |    8 |   10336 B |
| Concurrent_MediatR_128  | 8,253.81 ns | 186.06 |    9 |   34912 B |

**Takeaway:** PlaxionMediator scales in step with Mediator under concurrent load, with identical
allocation profiles at every caller tier (176/736/2656/10336 B), and stays well ahead of MediatR
throughout. Unchanged from the `v0.4.2` baseline; allocation figures are identical.

## Notification Fan-Out

| Method                        | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler     |    59.10 ns |  0.66 |    1 |     120 B |
| Publish_Plaxion_1Handler      |    89.16 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler      |   121.67 ns |  1.37 |    3 |     352 B |
| Publish_Mediator_10Handlers   |   584.04 ns |  6.55 |    4 |    1200 B |
| Publish_Plaxion_10Handlers    |   586.86 ns |  6.58 |    4 |    1304 B |
| Publish_MediatR_10Handlers    |   738.66 ns |  8.29 |    5 |    2512 B |
| Publish_Plaxion_50Handlers    | 2,808.66 ns | 31.51 |    6 |    6424 B |
| Publish_Mediator_50Handlers   | 2,991.50 ns | 33.56 |    6 |    6000 B |
| Publish_MediatR_50Handlers    | 3,565.68 ns | 40.00 |    7 |   12112 B |
| Publish_Plaxion_100Handlers   | 5,573.20 ns | 62.53 |    8 |   12824 B |
| Publish_Mediator_100Handlers  | 5,854.28 ns | 65.68 |    8 |   12000 B |
| Publish_MediatR_100Handlers   | 6,864.30 ns | 77.01 |    9 |   24112 B |

**Takeaway:** PlaxionMediator's strongest category — it edges ahead of Mediator at 50 and 100
handlers, and is consistently faster than MediatR across every fan-out tier. Unchanged from the
`v0.4.2` baseline; allocation figures are identical.

## Overall Summary

- **Pipeline behaviors:** PlaxionMediator matches Mediator's allocation profile exactly at every
  depth and stays ahead of MediatR on latency and allocations throughout.
- **Type variety:** PlaxionMediator is essentially on par with Mediator (ratio ~1.00) while
  remaining 0 B allocated, and is roughly 5.4x faster than MediatR with far fewer allocations.
- **Concurrency:** Scaling behavior tracks Mediator closely under load, with identical allocation
  footprints, and a clear lead over MediatR at every caller tier.
- **Notifications:** PlaxionMediator's best category, leading both peers at higher fan-out counts.
- All three frameworks — PlaxionMediator, Mediator, and MediatR — are solid, production-ready
  choices; these numbers simply document where PlaxionMediator stands today so the comparison is
  transparent and reproducible.

## `v0.4.3` Stabilization Pass — Regression Verdict

- **Scope re-confirmed:** the `v0.4.3` Core/Pipeline workstream profiled `PipelineExecutor`
  (field-staged, ≤5 behaviors) and the pooled `PipelineRunner` fallback (>5 behaviors) and found
  no correctness or contention issue serious enough to justify changing pool sizing or the
  staged-field threshold — see `src/RELEASE_NOTES.md` for the explicit "no tuning needed"
  rationale. Consequently no source changes were made to `PipelineComposer.cs` this release.
- **New Circuit-Breaker-inclusive coverage:** `src/PlaxionMediator.Benchmarks` gained
  `ResiliencePipelineBenchmarks` (`Send_CircuitBreakerOnly`, `Send_FullChain_CacheMiss`,
  `Send_FullChain_CacheHit`, covering `Validation -> Caching -> CircuitBreaker -> Retry`), closing
  the coverage gap called out in the `v0.4.2` plan. This project builds cleanly in Release; a full
  BenchmarkDotNet run of it was intentionally skipped here (too slow for this pass) since it has
  no `v0.4.2` baseline to regress against — it is new coverage, not a comparison point.
  See `src/PlaxionMediator.Benchmarks/BenchmarkDotNet.Artifacts` locally if a full run is desired.
- **Regression gate result:** re-running this `benchmarks-comparison/` suite (the actual
  before/after reference point for `PipelineExecutor`/`PipelineRunner`, per the `v0.4.3` plan's
  Benchmark Strategy) shows every scenario within normal noise of the `v0.4.2` baseline and
  byte-identical allocations — **PASS, no regression**.
