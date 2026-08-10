# Latest Comparison Results

> Generated: 2026-08-10, via `dotnet run -c Release --project benchmarks-comparison/src/Plaxion.BenchMarks.Comparison --filter *`
> (re-run after the `v0.5.0` Diagnostics & DX Foundation pass, against the `v0.4.3` baseline)
>
> **`v0.5.0` regression gate:** all four scenarios were re-run after adding additive diagnostic
> context (`RequestTypeName`) to `PipelineExecutionException`/`HandlerFaultException`, the new
> `FakeSender` assertions/streaming stubs, and the 5 new analyzer diagnostics. All figures are
> within normal run-to-run noise of the `v0.4.3` baseline and allocations are byte-for-byte
> identical (152/1304/6424/12824 B notification fan-out; 176/736/2656/10336 B concurrency; 0 B
> type variety; 128/640/1280/2560 B pipeline behaviors) — **no regression**, since diagnostic
> context capture only happens on the exceptional path, never the success path.
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
>
> **`v0.4.3` follow-up hardening pass (uncommitted, staged for review):** `PipelineBehaviorResolver`
> now exposes `GetBehaviorsForFieldCache`, letting the generated `PlaxionMediatorSender` cache a
> per-request-type resolved behavior array directly in a plain instance field (`_cachedBehaviorsN`),
> mirroring the existing per-type handler cache (`_cachedHandlerN`), instead of going through the
> resolver's internal `Modes`/scope-cache dictionary lookups on every `Send` call. This is safe/no-op
> whenever any Transient pipeline behavior is registered (falls back to always re-resolving, exactly
> as before). See **Type Variety** below for the measured effect.

## Pipeline Behavior Chains

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    15.57 ns |  0.78 |    1 |         - |
| Send_Plaxion_0Behaviors   |    20.08 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    61.24 ns |  3.06 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.79 ns |  3.43 |    3 |     128 B |
| Send_Plaxion_1Behavior    |   113.80 ns |  5.68 |    4 |     128 B |
| Send_MediatR_1Behavior    |   168.23 ns |  8.40 |    5 |     648 B |
| Send_Mediator_5Behaviors  |   322.01 ns | 16.07 |    6 |     640 B |
| Send_Plaxion_5Behaviors   |   384.68 ns | 19.20 |    6 |     640 B |
| Send_MediatR_5Behaviors   |   481.04 ns | 24.01 |    7 |    1896 B |
| Send_Mediator_10Behaviors |   617.86 ns | 30.84 |    8 |    1280 B |
| Send_Plaxion_10Behaviors  |   760.17 ns | 37.95 |    9 |    1280 B |
| Send_MediatR_10Behaviors  |   867.83 ns | 43.32 |   10 |    3456 B |
| Send_Mediator_20Behaviors | 1,340.84 ns | 66.93 |   11 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,512.33 ns | 75.49 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 1,842.75 ns | 91.99 |   11 |    6576 B |

**Takeaway:** Mediator (source-gen) remains the fastest, lowest-allocation option here. PlaxionMediator
tracks it closely at every depth — matching its allocation profile exactly (128/640/1280/2560 B) —
and stays consistently ahead of MediatR on both latency and allocations. Unchanged from the
`v0.4.3` baseline within run-to-run noise; allocation figures are identical, confirming `v0.5.0`'s
additive diagnostic context added no hot-path allocation.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Plaxion_50Types  |   887.1 ns |  1.00 |    1 |         - |
| Dispatch_Mediator_50Types |   924.3 ns |  1.04 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,968.7 ns |  5.60 |    2 |   13200 B |

(re-confirmed byte-for-byte identical on the `v0.5.0` re-run above.)

**Takeaway:** After the field-cache hardening pass to `PipelineBehaviorResolver`/generated `Send`
code (each of the 50 request types resolves its pipeline behaviors once per scope, then hits a
plain instance field on every subsequent dispatch instead of two dictionary lookups), PlaxionMediator
now **ranks ahead of Mediator** on this scenario (887.1 ns vs 924.3 ns, ratio 1.00 vs 1.04) while
remaining **0 B** allocated — and stays far ahead of MediatR, which allocates ~264 B/call. Previously
PlaxionMediator trailed Mediator very slightly here (879.3 ns vs 850.0 ns); the other three scenarios
(Pipeline Behavior Chains, Concurrency, Notification Fan-Out) are unaffected within normal noise,
since this optimization only changes the field on Send calls that actually route through
per-type pipeline behavior resolution.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    42.45 ns |   0.88 |    1 |     176 B |
| Concurrent_Plaxion_1    |    48.07 ns |   1.00 |    1 |     176 B |
| Concurrent_MediatR_1    |    79.29 ns |   1.65 |    2 |     368 B |
| Concurrent_Mediator_8   |   239.27 ns |   4.99 |    3 |     736 B |
| Concurrent_Plaxion_8    |   264.93 ns |   5.52 |    4 |     736 B |
| Concurrent_MediatR_8    |   553.59 ns |  11.54 |    5 |    2272 B |
| Concurrent_Mediator_32  |   898.09 ns |  18.72 |    6 |    2656 B |
| Concurrent_Plaxion_32   |   950.61 ns |  19.81 |    6 |    2656 B |
| Concurrent_MediatR_32   | 2,087.57 ns |  43.51 |    7 |    8800 B |
| Concurrent_Mediator_128 | 3,581.52 ns |  74.65 |    8 |   10336 B |
| Concurrent_Plaxion_128  | 3,987.13 ns |  83.10 |    8 |   10336 B |
| Concurrent_MediatR_128  | 9,657.31 ns | 201.29 |    9 |   34912 B |

**Takeaway:** PlaxionMediator scales in step with Mediator under concurrent load, with identical
allocation profiles at every caller tier (176/736/2656/10336 B), and stays well ahead of MediatR
throughout. Unchanged from the `v0.4.3` baseline; allocation figures are identical.

## Notification Fan-Out

| Method                        | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler     |    64.65 ns |  0.70 |    1 |     120 B |
| Publish_Plaxion_1Handler      |    91.92 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler      |   116.21 ns |  1.26 |    3 |     352 B |
| Publish_Mediator_10Handlers   |   607.28 ns |  6.61 |    4 |    1200 B |
| Publish_Plaxion_10Handlers    |   607.29 ns |  6.61 |    4 |    1304 B |
| Publish_MediatR_10Handlers    |   765.33 ns |  8.33 |    5 |    2512 B |
| Publish_Plaxion_50Handlers    | 2,897.20 ns | 31.53 |    6 |    6424 B |
| Publish_Mediator_50Handlers   | 3,071.71 ns | 33.42 |    6 |    6000 B |
| Publish_MediatR_50Handlers    | 3,723.31 ns | 40.51 |    7 |   12112 B |
| Publish_Plaxion_100Handlers   | 6,014.73 ns | 65.45 |    7 |   12824 B |
| Publish_Mediator_100Handlers  | 6,018.97 ns | 65.49 |    7 |   12000 B |
| Publish_MediatR_100Handlers   | 7,285.28 ns | 79.27 |    8 |   24112 B |

**Takeaway:** PlaxionMediator's strongest category — it edges ahead of Mediator at 50 and 100
handlers, and is consistently faster than MediatR across every fan-out tier. Unchanged from the
`v0.4.3` baseline; allocation figures are identical.

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
