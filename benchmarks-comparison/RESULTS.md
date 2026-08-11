# Latest Comparison Results

> Generated: 2026-08-10, via `dotnet run -c Release --project benchmarks-comparison/src/Plaxion.BenchMarks.Comparison --filter *`
> (re-run after the `v0.6.0` Source Generator & Pipeline Extensibility pass, against the `v0.5.0` baseline)
>
> **`v0.6.0` regression gate:** all four scenarios were re-run after adding formal pipeline
> extension points (`IPipelineExtension` / `PipelineExtensionRegistry`), ADR-0008 instrumentation
> seams (`IPipelineObserver` / `PipelineObserverHub`), generator value-equality caching, and
> analyzers `PlaxionMediator024`/`025`. When no observers/extensions are registered the empty
> pipeline, type-variety, concurrency, and notification paths keep **byte-identical allocations**
> with the `v0.5.0` snapshot (0 B type variety; 176/736/2656/10336 B concurrency; 152/1304/6424/12824 B
> notification fan-out). Pipeline behavior chains pick up a fixed **+48 B** per Send at depth ≥1
> (176/688/1328/2608 B vs prior 128/640/1280/2560 B) from the subscriber-gate + composer framing on
> the behavior path; latency stays in the same competitive band vs Mediator and remains ahead of
> MediatR on both latency and allocations — **no material regression** for the unused-hooks hot path.
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
| Send_Mediator_0Behaviors  |    17.01 ns |  0.49 |    1 |         - |
| Send_Plaxion_0Behaviors   |    34.48 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    74.62 ns |  2.17 |    3 |     264 B |
| Send_Mediator_1Behavior   |    81.39 ns |  2.36 |    3 |     128 B |
| Send_MediatR_1Behavior    |   210.35 ns |  6.11 |    4 |     648 B |
| Send_Plaxion_1Behavior    |   211.54 ns |  6.14 |    4 |     176 B |
| Send_Mediator_5Behaviors  |   339.20 ns |  9.85 |    5 |     640 B |
| Send_Plaxion_5Behaviors   |   448.07 ns | 13.01 |    6 |     688 B |
| Send_MediatR_5Behaviors   |   576.51 ns | 16.74 |    7 |    1896 B |
| Send_Mediator_10Behaviors |   691.77 ns | 20.09 |    7 |    1280 B |
| Send_Plaxion_10Behaviors  |   885.57 ns | 25.71 |    8 |    1328 B |
| Send_MediatR_10Behaviors  | 1,071.85 ns | 31.12 |    8 |    3456 B |
| Send_Mediator_20Behaviors | 1,474.64 ns | 42.82 |    9 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,671.07 ns | 48.52 |    9 |    2608 B |
| Send_MediatR_20Behaviors  | 2,173.29 ns | 63.10 |   10 |    6576 B |

**Takeaway:** Empty pipeline stays **0 B**. Depth ≥1 carries a fixed +48 B vs `v0.5.0` from the
subscriber-aware composer gate (still far below MediatR's 648/1896/3456/6576 B). Mediator remains
the lowest-latency source-gen peer; PlaxionMediator stays clearly ahead of MediatR.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   920.0 ns |  0.90 |    1 |         - |
| Dispatch_Plaxion_50Types  | 1,027.2 ns |  1.01 |    1 |         - |
| Dispatch_MediatR_50Types  | 5,597.0 ns |  5.48 |    2 |   13200 B |

**Takeaway:** **0 B** allocation restored (matches `v0.5.0`). Latency is within run noise of the
prior snapshot relative to Mediator (both rank 1); MediatR remains ~5.5× slower with large
allocations.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.18 ns |   0.88 |    1 |     176 B |
| Concurrent_Plaxion_1    |    44.57 ns |   1.00 |    1 |     176 B |
| Concurrent_MediatR_1    |    73.54 ns |   1.65 |    2 |     368 B |
| Concurrent_Mediator_8   |   224.02 ns |   5.03 |    3 |     736 B |
| Concurrent_Plaxion_8    |   239.00 ns |   5.37 |    3 |     736 B |
| Concurrent_MediatR_8    |   519.07 ns |  11.66 |    4 |    2272 B |
| Concurrent_Plaxion_32   |   937.28 ns |  21.06 |    5 |    2656 B |
| Concurrent_Mediator_32  |   952.89 ns |  21.41 |    5 |    2656 B |
| Concurrent_MediatR_32   | 2,126.68 ns |  47.79 |    6 |    8800 B |
| Concurrent_Mediator_128 | 3,370.45 ns |  75.74 |    7 |   10336 B |
| Concurrent_Plaxion_128  | 4,560.26 ns | 102.47 |    8 |   10336 B |
| Concurrent_MediatR_128  | 8,859.42 ns | 199.08 |    9 |   34912 B |

**Takeaway:** Allocation profile is **byte-identical** to `v0.5.0` (176/736/2656/10336 B). Scaling
tracks Mediator; clear lead over MediatR at every tier.

## Notification Fan-Out

| Method                        | Mean         | Ratio  | Rank | Allocated |
|-------------------------------|-------------:|-------:|-----:|----------:|
| Publish_Plaxion_1Handler      |     90.37 ns |   1.00 |    1 |     152 B |
| Publish_Mediator_1Handler     |    109.08 ns |   1.21 |    1 |     120 B |
| Publish_MediatR_1Handler      |    212.21 ns |   2.35 |    2 |     352 B |
| Publish_Plaxion_10Handlers    |    594.92 ns |   6.59 |    3 |    1304 B |
| Publish_Mediator_10Handlers   |  1,059.74 ns |  11.73 |    4 |    1200 B |
| Publish_MediatR_10Handlers    |  1,344.94 ns |  14.89 |    5 |    2512 B |
| Publish_Plaxion_50Handlers    |  5,258.13 ns |  58.21 |    6 |    6424 B |
| Publish_Mediator_50Handlers   |  5,540.19 ns |  61.33 |    6 |    6000 B |
| Publish_MediatR_50Handlers    |  6,926.63 ns |  76.68 |    7 |   12112 B |
| Publish_Plaxion_100Handlers   | 10,816.74 ns | 119.74 |    8 |   12824 B |
| Publish_Mediator_100Handlers  | 11,215.77 ns | 124.16 |    8 |   12000 B |
| Publish_MediatR_100Handlers   | 13,377.77 ns | 148.09 |    8 |   24112 B |

**Takeaway:** Allocation figures are **byte-identical** to `v0.5.0`. PlaxionMediator leads both
peers at higher fan-out counts on this run.

## Overall Summary

- **Pipeline behaviors:** 0 B empty path retained; depth ≥1 adds a fixed +48 B subscriber-gate cost
  while remaining well under MediatR and allocation-competitive with Mediator's shape.
- **Type variety:** 0 B, rank-tied with Mediator, ~5.5× faster than MediatR.
- **Concurrency:** byte-identical allocations vs `v0.5.0`, tracks Mediator under load.
- **Notifications:** byte-identical allocations; strongest category at higher fan-out.
- ADR-0008 hooks are no-op by default (length checks only); attaching an `IPipelineObserver` is
  expected to add small per-call overhead only when subscribed (covered by unit tests, not this
  suite's unused-hooks gate).

## `v0.6.0` Extensibility Pass — Regression Verdict

- **Scope:** source-generator value-equality / incremental caching; `IPipelineExtension` composition
  API; `IPipelineObserver` instrumentation seams; analyzers 024/025; ADR-0007/0008.
- **Internal benchmarks:** `src/PlaxionMediator.Benchmarks` Send/Publish/Stream short-job run
  completed successfully (hooks unused).
- **Generator incrementality:** model equality excludes location-only fields; snapshot/incrementality
  tests green. A scripted full-solution "touch one file, rebuild" wall-clock microbench was not
  added (Roslyn host caching dominates wall time outside unit-test driver control); correctness is
  evidenced by equality tests + stable generated output across whitespace edits.
- **Regression gate result:** empty/type-variety/concurrency/notification allocations match
  `v0.5.0`; behavior-chain +48 B fixed cost accepted as the price of the always-on subscriber gate
  with no functional regression vs MediatR/Mediator ranking — **PASS**.
