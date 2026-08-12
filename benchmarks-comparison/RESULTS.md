# Latest Comparison Results

> Generated: 2026-08-11, via `dotnet run -c Release --project benchmarks-comparison/src/Plaxion.BenchMarks.Comparison --filter *`
> (full-suite re-run after the `v0.8.0` Transactions & Analyzer Hardening pass, against the `v0.6.0` baseline)
>
> **`v0.8.0` regression gate:** all four scenarios were re-run after adding
> `PlaxionMediator.Transactions` / `PlaxionMediator.Transactions.EntityFrameworkCore` and the
> `PlaxionMediator042`-`045` analyzers. None of this work touches the core `Send`/dispatch/publish
> pipeline, and the numbers confirm it: empty pipeline, type-variety, concurrency, and notification
> paths keep **byte-identical allocations** with the `v0.6.0` snapshot (0 B type variety;
> 176/736/2656/10336 B concurrency; 152/1304/6424/12824 B notification fan-out); pipeline behavior
> chains keep the same 176/688/1328/2608 B footprint at depth ≥1. Latency stays within normal run-to-run
> noise and PlaxionMediator remains ahead of MediatR on both latency and allocations at every tier,
> tracking Mediator's shape — **no regression introduced by v0.8.0**.
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

## Pipeline Behavior Chains

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    18.90 ns |  0.92 |    1 |         - |
| Send_Plaxion_0Behaviors   |    20.50 ns |  1.00 |    1 |         - |
| Send_MediatR_0Behaviors   |    64.33 ns |  3.14 |    2 |     264 B |
| Send_Mediator_1Behavior   |    73.44 ns |  3.58 |    2 |     128 B |
| Send_Plaxion_1Behavior    |   124.02 ns |  6.05 |    3 |     176 B |
| Send_MediatR_1Behavior    |   163.83 ns |  7.99 |    4 |     648 B |
| Send_Mediator_5Behaviors  |   328.86 ns | 16.05 |    5 |     640 B |
| Send_Plaxion_5Behaviors   |   397.49 ns | 19.40 |    5 |     688 B |
| Send_MediatR_5Behaviors   |   475.24 ns | 23.19 |    5 |    1896 B |
| Send_Mediator_10Behaviors |   625.71 ns | 30.54 |    6 |    1280 B |
| Send_Plaxion_10Behaviors  |   793.18 ns | 38.71 |    7 |    1328 B |
| Send_MediatR_10Behaviors  |   852.77 ns | 41.62 |    7 |    3456 B |
| Send_Mediator_20Behaviors | 1,351.27 ns | 65.94 |    8 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,569.23 ns | 76.58 |    9 |    2608 B |
| Send_MediatR_20Behaviors  | 1,831.37 ns | 89.37 |    9 |    6576 B |

**Takeaway:** Empty pipeline stays **0 B**. Allocation footprint per depth (176/688/1328/2608 B)
is **byte-identical** to the `v0.6.0` snapshot — the `v0.8.0` Transactions/analyzer work did not
touch this hot path. Still far below MediatR's 648/1896/3456/6576 B; PlaxionMediator stays clearly
ahead of MediatR at every tier and tracks Mediator's shape.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   921.0 ns |  0.99 |    1 |         - |
| Dispatch_Plaxion_50Types  |   935.0 ns |  1.00 |    1 |         - |
| Dispatch_MediatR_50Types  | 5,275.9 ns |  5.64 |    2 |   13200 B |

**Takeaway:** **0 B** allocation retained (matches `v0.6.0`/`v0.5.0`). Latency is within run noise
relative to Mediator (both rank 1); MediatR remains ~5.6× slower with large allocations.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    40.89 ns |   0.86 |    1 |     176 B |
| Concurrent_Plaxion_1    |    47.63 ns |   1.00 |    1 |     176 B |
| Concurrent_MediatR_1    |    80.58 ns |   1.69 |    2 |     368 B |
| Concurrent_Mediator_8   |   242.93 ns |   5.11 |    3 |     736 B |
| Concurrent_Plaxion_8    |   252.74 ns |   5.31 |    3 |     736 B |
| Concurrent_MediatR_8    |   534.41 ns |  11.24 |    4 |    2272 B |
| Concurrent_Mediator_32  |   941.73 ns |  19.80 |    5 |    2656 B |
| Concurrent_Plaxion_32   | 1,060.69 ns |  22.30 |    5 |    2656 B |
| Concurrent_MediatR_32   | 2,247.53 ns |  47.26 |    6 |    8800 B |
| Concurrent_Mediator_128 | 3,827.53 ns |  80.48 |    7 |   10336 B |
| Concurrent_Plaxion_128  | 4,107.76 ns |  86.37 |    7 |   10336 B |
| Concurrent_MediatR_128  | 8,772.46 ns | 184.46 |    8 |   34912 B |

**Takeaway:** Allocation profile is **byte-identical** to `v0.6.0`/`v0.5.0` (176/736/2656/10336 B).
Scaling tracks Mediator; clear lead over MediatR at every tier.

## Notification Fan-Out

| Method                        | Mean         | Ratio | Rank | Allocated |
|-------------------------------|-------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler     |     63.37 ns |  0.66 |    1 |     120 B |
| Publish_Plaxion_1Handler      |     95.81 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler      |    117.90 ns |  1.23 |    2 |     352 B |
| Publish_Plaxion_10Handlers    |    615.52 ns |  6.43 |    3 |    1304 B |
| Publish_Mediator_10Handlers   |    616.08 ns |  6.44 |    3 |    1200 B |
| Publish_MediatR_10Handlers    |    877.39 ns |  9.17 |    4 |    2512 B |
| Publish_Plaxion_50Handlers    |  2,812.16 ns | 29.39 |    5 |    6424 B |
| Publish_Mediator_50Handlers   |  3,022.01 ns | 31.58 |    5 |    6000 B |
| Publish_MediatR_50Handlers    |  3,826.04 ns | 39.98 |    6 |   12112 B |
| Publish_Plaxion_100Handlers   |  5,764.50 ns | 60.24 |    7 |   12824 B |
| Publish_Mediator_100Handlers  |  6,301.49 ns | 65.85 |    7 |   12000 B |
| Publish_MediatR_100Handlers   |  8,189.91 ns | 85.59 |    8 |   24112 B |

**Takeaway:** Allocation figures are **byte-identical** to `v0.6.0`/`v0.5.0`. PlaxionMediator
remains competitive with Mediator and leads MediatR at every fan-out tier on this run.

## Overall Summary

- **Pipeline behaviors:** 0 B empty path retained; per-depth allocation footprint (176/688/1328/2608 B)
  unchanged since `v0.6.0`, still well under MediatR and allocation-competitive with Mediator's shape.
- **Type variety:** 0 B, rank-tied with Mediator, ~5.6× faster than MediatR.
- **Concurrency:** byte-identical allocations vs `v0.6.0`/`v0.5.0`, tracks Mediator under load.
- **Notifications:** byte-identical allocations; competitive with Mediator, ahead of MediatR at every fan-out tier.
- ADR-0008 hooks are no-op by default (length checks only); attaching an `IPipelineObserver` is
  expected to add small per-call overhead only when subscribed (covered by unit tests, not this
  suite's unused-hooks gate).

## `v0.8.0` Transactions & Analyzer Hardening — Regression Verdict

- **Scope:** `PlaxionMediator.Transactions` (+ `.EntityFrameworkCore` adapter), `TransactionBehavior<,>`,
  analyzers `PlaxionMediator042`-`045`; none of this work modifies `PlaxionMediator.Core`/`Pipeline`
  dispatch internals.
- **Full solution test suite:** `dotnet test PlaxionMediator.sln -c Release` — **187/187 tests passed**,
  0 failed, 0 skipped, across all packages including the new `PlaxionMediator.Transactions.Tests` (22)
  and `PlaxionMediator.Transactions.EntityFrameworkCore.Tests` (5).
- **Full comparison-suite re-run:** all four scenario classes (`PipelineBehaviorBenchmarks`,
  `TypeVarietyBenchmarks`, `ConcurrencyBenchmarks`, `NotificationFanOutBenchmarks`) re-executed with the
  same reproducible `Job.Default` (WarmupCount=3, IterationCount=10, LaunchCount=1) job used for every
  prior snapshot.
- **Regression gate result:** every allocation figure is **byte-identical** to the `v0.6.0` baseline
  (0 B empty/type-variety; 176/688/1328/2608 B pipeline; 176/736/2656/10336 B concurrency;
  152/1304/6424/12824 B notification); latency deltas are within normal run-to-run measurement noise
  and PlaxionMediator's rank/ratio standing vs MediatR/Mediator is unchanged at every tier —
  **PASS, no regression from v0.8.0**.
- **Internal Send/Publish/Stream micro-benchmarks:** `src/PlaxionMediator.Benchmarks` includes the new
  Transactions scenarios (no-transaction fast path, non-transactional request, no-op manager commit,
  no-op manager rollback) as opt-in additions; they do not alter the always-on core dispatch path
  measured above.
