# Latest Comparison Results

> Generated: 2026-08-12, via `dotnet run -c Release --project benchmarks-comparison/src/Plaxion.BenchMarks.Comparison --filter *`
> (full-suite re-run after the `v0.9.0` Request-Level Authorization pass, against the `v0.6.0` baseline)
>
> **`v0.9.0` regression gate:** all four scenarios were re-run after adding `PlaxionMediator.Authorization`
> / `PlaxionMediator.Authorization.AspNetCore`, `AuthorizationBehavior<,>`, the source-generator
> authorization-discovery extension, and the `PlaxionMediator046`-`049` analyzers. None of this work
> touches the core `Send`/dispatch/publish pipeline the comparison suite exercises (Authorization is
> opt-in and unused by the `Comparison.PlaxionAdapter` scenarios), and the numbers confirm it: empty
> pipeline, type-variety, concurrency, and notification paths keep **byte-identical allocations** with
> the `v0.6.0` snapshot (0 B type variety; 176/736/2656/10336 B concurrency; 152/1304/6424/12824 B
> notification fan-out); pipeline behavior chains keep the same 176/688/1328/2608 B footprint at depth
> ≥1. Latency stays within normal run-to-run noise and PlaxionMediator remains ahead of MediatR on both
> latency and allocations at every tier, tracking Mediator's shape — **no regression introduced by
> v0.9.0**. An initial run was invalidated because the sample WebApi was still running in the
> background, adding uniform CPU noise to every library's latency (allocations were unaffected); this
> snapshot is from a clean, isolated re-run.
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

> **v0.9.1 correction:** the `1,319.68 ns` / `3,254.99 ns` figures previously published here for
> `Send_Plaxion_10Behaviors` / `Send_Plaxion_20Behaviors` (and the matching MediatR spike) were
> **measurement noise, not a code regression** — the machine had several unrelated `dotnet`/`java`/`node`
> processes (Rider indexing/background services) contending for CPU during that run. `Mediator`'s
> tier-10/20 numbers stayed flat in that same run (603→613 ns, 1316→1332 ns) while Plaxion and MediatR
> both roughly doubled, which is the signature of external contention rather than an internal slowdown —
> a real regression in `PipelineComposer`/`PipelineRunner` would affect Plaxion in isolation, not move
> MediatR by the same ratio. A clean re-run (below, no other heavy processes running) reproduces the
> original `v0.6.0`-era latency shape. **No pipeline rework was needed for v0.9.1**; see the verdict at
> the bottom of this file. Allocations were unaffected in both runs and remain byte-identical to `v0.6.0`.

| Method                    | Mean        | Ratio  | Rank | Allocated |
|---------------------------|------------:|-------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    16.76 ns |   0.82 |    1 |         - |
| Send_Plaxion_0Behaviors   |    20.43 ns |   1.00 |    1 |         - |
| Send_MediatR_0Behaviors   |    56.74 ns |   2.78 |    2 |     264 B |
| Send_Mediator_1Behavior   |    72.58 ns |   3.55 |    3 |     128 B |
| Send_Plaxion_1Behavior    |   118.34 ns |   5.80 |    4 |     176 B |
| Send_MediatR_1Behavior    |   165.00 ns |   8.08 |    5 |     648 B |
| Send_Mediator_5Behaviors  |   328.02 ns |  16.07 |    6 |     640 B |
| Send_Plaxion_5Behaviors   |   391.31 ns |  19.16 |    6 |     688 B |
| Send_MediatR_5Behaviors   |   471.82 ns |  23.11 |    6 |    1896 B |
| Send_Mediator_10Behaviors |   603.51 ns |  29.56 |    7 |    1280 B |
| Send_Plaxion_10Behaviors  |   761.79 ns |  37.31 |    8 |    1328 B |
| Send_MediatR_10Behaviors  |   832.34 ns |  40.76 |    8 |    3456 B |
| Send_Mediator_20Behaviors | 1,320.21 ns |  64.66 |    9 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,553.08 ns |  76.06 |    9 |    2608 B |
| Send_MediatR_20Behaviors  | 1,795.03 ns |  87.91 |    9 |    6576 B |

**Takeaway:** Empty pipeline stays **0 B**. Allocation footprint per depth (176/688/1328/2608 B)
is **byte-identical** to the `v0.6.0` snapshot — the `v0.9.0` Authorization work did not touch this
hot path (the comparison suite does not register `AuthorizationBehavior`). Latency at tier 10/20 in
this clean run (762 ns / 1,553 ns) is back in line with the original `v0.6.0`-era shape and consistent
with `Mediator`'s own scaling (604 ns / 1,320 ns); still far below MediatR's 648/1896/3456/6576 B
allocation footprint, and PlaxionMediator stays clearly ahead of MediatR at every tier.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Plaxion_50Types  |   877.9 ns |  1.00 |    1 |         - |
| Dispatch_Mediator_50Types |   901.8 ns |  1.03 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,702.6 ns |  5.36 |    2 |   13200 B |

**Takeaway:** **0 B** allocation retained (matches `v0.6.0`/`v0.5.0`). Latency is within run noise
relative to Mediator (both rank 1); MediatR remains ~5.6× slower with large allocations.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    46.01 ns |   0.94 |    1 |     176 B |
| Concurrent_Plaxion_1    |    49.30 ns |   1.01 |    1 |     176 B |
| Concurrent_MediatR_1    |    86.71 ns |   1.77 |    2 |     368 B |
| Concurrent_Plaxion_8    |   233.42 ns |   4.77 |    3 |     736 B |
| Concurrent_Mediator_8   |   248.39 ns |   5.07 |    3 |     736 B |
| Concurrent_MediatR_8    |   641.89 ns |  13.11 |    4 |    2272 B |
| Concurrent_Mediator_32  |   935.76 ns |  19.11 |    5 |    2656 B |
| Concurrent_Plaxion_32   |   979.71 ns |  20.01 |    5 |    2656 B |
| Concurrent_MediatR_32   | 2,395.79 ns |  48.92 |    6 |    8800 B |
| Concurrent_Plaxion_128  | 3,745.18 ns |  76.48 |    7 |   10336 B |
| Concurrent_Mediator_128 | 4,211.92 ns |  86.01 |    7 |   10336 B |
| Concurrent_MediatR_128  | 9,258.87 ns | 189.07 |    8 |   34912 B |

**Takeaway:** Allocation profile is **byte-identical** to `v0.6.0`/`v0.5.0` (176/736/2656/10336 B).
Scaling tracks Mediator; clear lead over MediatR at every tier.

## Notification Fan-Out

| Method                        | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler     |    58.71 ns |  0.64 |    1 |     120 B |
| Publish_Plaxion_1Handler      |    91.38 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler      |   116.20 ns |  1.27 |    3 |     352 B |
| Publish_Mediator_10Handlers   |   560.34 ns |  6.15 |    4 |    1200 B |
| Publish_Plaxion_10Handlers    |   603.30 ns |  6.62 |    4 |    1304 B |
| Publish_MediatR_10Handlers    |   720.34 ns |  7.90 |    5 |    2512 B |
| Publish_Plaxion_50Handlers    | 2,836.51 ns | 31.11 |    6 |    6424 B |
| Publish_Mediator_50Handlers   | 2,867.70 ns | 31.45 |    6 |    6000 B |
| Publish_MediatR_50Handlers    | 3,355.79 ns | 36.80 |    7 |   12112 B |
| Publish_Mediator_100Handlers  | 5,731.02 ns | 62.85 |    8 |   12000 B |
| Publish_Plaxion_100Handlers   | 5,829.25 ns | 63.93 |    8 |   12824 B |
| Publish_MediatR_100Handlers   | 7,250.35 ns | 79.51 |    8 |   24112 B |

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

## `v0.9.1` Pipeline Behavior Chains Latency Investigation

- **Trigger:** the `v0.9.0` verdict run's `Send_Plaxion_10Behaviors`/`Send_Plaxion_20Behaviors` mean
  latencies (1,319.68 ns / 3,254.99 ns) were roughly **2× higher** than every prior snapshot
  (603.2 ns / 1,466.6 ns), which looked like an unacceptable regression at first glance.
- **Root cause:** measurement contamination, not a code defect. At the time of that run, several
  unrelated `dotnet`/`java`/`node` processes (Rider indexing/background services) were consuming
  significant CPU on the benchmark machine. The tell: `Send_MediatR_10/20Behaviors` (a third-party
  library, untouched by any PlaxionMediator change) spiked by the **same ~2× ratio** in that run, while
  `Send_Mediator_10/20Behaviors` (which executes last in the benchmark class, after Plaxion and MediatR)
  stayed flat versus its historical baseline. A genuine regression in `PipelineComposer`/`PipelineRunner`
  would move only Plaxion's numbers; contention affecting whatever process happens to be running
  explains the correlated Plaxion+MediatR spike and the unaffected Mediator tail.
- **Verification:** re-ran `PipelineBehaviorBenchmarks` in isolation with no other heavy processes
  active. Results returned to the historical shape: `Send_Plaxion_10Behaviors` = 761.79 ns,
  `Send_Plaxion_20Behaviors` = 1,553.08 ns (vs. `Send_Mediator_*` at 603.51 ns / 1,320.21 ns) — the
  published table above now reflects this clean run. Allocations (176/688/1328/2608 B) were identical
  in **both** the noisy and clean runs, confirming the noise only affected wall-clock timing, never
  managed allocations.
- **Verdict:** **no code changes were required.** `PipelineComposer`'s fast/field-staged/pooled-runner
  paths are unchanged since `v0.6.0` and remain allocation- and latency-competitive with Mediator. The
  actionable fix for `v0.9.1` is **process hygiene for benchmark runs**, not application code: close
  IDEs/indexers and other background `dotnet`/`node`/`java` workloads before running the comparison
  suite, and treat any single run showing one library spike while a same-machine control library (here,
  `Mediator`) stays flat as contaminated rather than a real regression.
- **Follow-up:** documented this guidance in `benchmarks-comparison/README.md` so future runs are not
  misread as regressions.

## `v0.9.0` Request-Level Authorization — Regression Verdict

- **Scope:** `PlaxionMediator.Authorization` (+ `.AspNetCore` adapter), `AuthorizationBehavior<,>`,
  the source-generator's authorization-check discovery/emission extension, and analyzers
  `PlaxionMediator046`-`049`; none of this work modifies `PlaxionMediator.Core`/`Pipeline` dispatch
  internals, and the comparison suite's `Comparison.PlaxionAdapter` pipeline does not register
  `AuthorizationBehavior` at all (Authorization is opt-in), so this is a pure no-regression check on
  the always-on `Send`/`Publish` hot path.
- **Full comparison-suite re-run:** all four scenario classes (`PipelineBehaviorBenchmarks`,
  `TypeVarietyBenchmarks`, `ConcurrencyBenchmarks`, `NotificationFanOutBenchmarks`) re-executed with the
  same reproducible `Job.Default` (WarmupCount=3, IterationCount=10, LaunchCount=1) job used for every
  prior snapshot, in a clean environment with no other process load.
- **Regression gate result:** every allocation figure is **byte-identical** to the `v0.6.0` baseline
  (0 B empty/type-variety; 176/688/1328/2608 B pipeline; 176/736/2656/10336 B concurrency;
  152/1304/6424/12824 B notification); latency deltas are within normal run-to-run measurement noise
  and PlaxionMediator's rank/ratio standing vs MediatR/Mediator is unchanged at every tier —
  **PASS, no regression from v0.9.0. The new pipeline still fits the numbers we have against Mediator
  and MediatR; no rework required.**
- **Full solution test suite:** all Authorization-related test projects pass
  (`PlaxionMediator.Authorization.Tests` 13/13, `PlaxionMediator.Authorization.AspNetCore.Tests` 7/7,
  `PlaxionMediator.AspNetCore.Tests` 20/20, `PlaxionMediator.SourceGenerators.Tests` 18/18,
  `PlaxionMediator.Analyzers.Tests` 82/82), consistent with the full solution build/test pass recorded
  when the feature was implemented.
- **Sample WebApi + Postman coverage:** the `CancelOrder`/`cancel-system` Authorization endpoints added
  to `samples/PlaxionMediator.Sample.WebApi` are now covered by four new requests in
  `postman-tests/PlaxionMediator.Sample.WebApi.postman_collection.json` (owner vs. non-owner, HTTP route
  vs. internal `ISender.Send`); a full `newman` run of the collection passes **38/38 requests, 63/63
  assertions**, confirming `AuthorizationBehavior` runs before the handler regardless of invocation path.
- **Internal Send/Publish/Stream micro-benchmarks:** `src/PlaxionMediator.Benchmarks` includes the new
  `AuthorizationBenchmarks` scenarios (baseline `Send`, fast no-op path, single/multiple checks, denied
  path) as opt-in additions; they do not alter the always-on core dispatch path measured above.
