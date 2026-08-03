# Latest Comparison Results

> Generated: 2026-08-03 (post-optimization), via `dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison --filter *`  
> Environment: BenchmarkDotNet v0.14.0, Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7 (RyuJIT AVX2)  
> Job: `Job.Default` (WarmupCount=3, IterationCount=10, LaunchCount=1) — reproducible, non-Dry job.  
>
> Raw exported files (JSON/CSV/GitHub-Markdown) live under  
> `src/Plaxion.BenchMarks.Comparison/BenchmarkDotNet.Artifacts/results/`. This file is a  
> human-readable snapshot of those results for quick reference; regenerate it whenever the suite  
> is re-run so the numbers here stay in sync with the artifacts on disk.  
>
> See also: `OPTIMIZATION_REPORT.md` for the internal optimization series that produced these numbers.

## Pipeline Behavior Chains

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    17.12 ns |  0.57 |    1 |         - |
| Send_Plaxion_0Behaviors   |    30.05 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    60.19 ns |  2.00 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.46 ns |  2.28 |    4 |     128 B |
| Send_Plaxion_1Behavior    |   134.02 ns |  4.46 |    5 |     320 B |
| Send_MediatR_1Behavior    |   161.41 ns |  5.37 |    5 |     648 B |
| Send_Mediator_5Behaviors  |   329.28 ns | 10.96 |    6 |     640 B |
| Send_Plaxion_5Behaviors   |   403.45 ns | 13.42 |    7 |     832 B |
| Send_Mediator_10Behaviors |   613.21 ns | 20.40 |    8 |    1280 B |
| Send_Plaxion_10Behaviors  |   736.41 ns | 24.50 |    8 |    1472 B |
| Send_MediatR_5Behaviors   |   805.43 ns | 26.80 |    8 |    1896 B |
| Send_MediatR_10Behaviors  | 1,267.24 ns | 42.17 |    9 |    3456 B |
| Send_Mediator_20Behaviors | 1,316.11 ns | 43.79 |    9 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,500.17 ns | 49.92 |    9 |    2752 B |
| Send_MediatR_20Behaviors  | 1,696.07 ns | 56.43 |   10 |    6576 B |

**Takeaway:** Mediator (source-gen) remains fastest/lowest-alloc. Post-optimization, **PlaxionMediator
beats MediatR at every pipeline depth** on both latency and allocations, and closes most of the gap
to Mediator on deep chains (Send20: 1500 ns / 2752 B vs Mediator 1316 ns / 2560 B).

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   894.3 ns |  0.22 |    1 |         - |
| Dispatch_Plaxion_50Types  | 4,120.6 ns |  1.00 |    2 |         - |
| Dispatch_MediatR_50Types  | 5,105.4 ns |  1.24 |    2 |   13200 B |

**Takeaway:** Mediator’s compile-time dispatch table still wins by a wide margin. PlaxionMediator is
now **faster than MediatR** here while remaining **0 B** allocated (MediatR ~264 B/call × 50).

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.34 ns |   0.61 |    1 |     176 B |
| Concurrent_Plaxion_1    |    64.08 ns |   1.00 |    2 |     176 B |
| Concurrent_MediatR_1    |    78.90 ns |   1.23 |    3 |     368 B |
| Concurrent_Mediator_8   |   242.42 ns |   3.78 |    4 |     736 B |
| Concurrent_Plaxion_8    |   372.95 ns |   5.82 |    5 |     736 B |
| Concurrent_MediatR_8    |   649.61 ns |  10.14 |    6 |    2272 B |
| Concurrent_Mediator_32  |   895.48 ns |  13.97 |    7 |    2656 B |
| Concurrent_Plaxion_32   | 1,539.19 ns |  24.02 |    8 |    2656 B |
| Concurrent_MediatR_32   | 2,006.58 ns |  31.31 |    9 |    8800 B |
| Concurrent_Mediator_128 | 3,691.25 ns |  57.61 |   10 |   10336 B |
| Concurrent_Plaxion_128  | 5,980.40 ns |  93.33 |   11 |   10336 B |
| Concurrent_MediatR_128  | 8,907.46 ns | 139.01 |   12 |   34912 B |

**Takeaway:** Mediator scales best under concurrency. PlaxionMediator stays ahead of MediatR at every
tier with the same low allocation profile as Mediator at 8/32/128 callers.

## Notification Fan-Out

| Method                       | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler    |    58.73 ns |  0.65 |    1 |     120 B |
| Publish_Plaxion_1Handler     |    90.31 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler     |   118.90 ns |  1.32 |    3 |     352 B |
| Publish_Mediator_10Handlers  |   576.10 ns |  6.38 |    4 |    1200 B |
| Publish_Plaxion_10Handlers   |   591.19 ns |  6.55 |    4 |    1304 B |
| Publish_MediatR_10Handlers   |   727.27 ns |  8.05 |    4 |    2512 B |
| Publish_Plaxion_50Handlers   | 2,775.33 ns | 30.73 |    5 |    6424 B |
| Publish_Mediator_50Handlers  | 2,920.65 ns | 32.34 |    5 |    6000 B |
| Publish_MediatR_50Handlers   | 3,418.66 ns | 37.86 |    6 |   12112 B |
| Publish_Plaxion_100Handlers  | 5,522.10 ns | 61.15 |    7 |   12824 B |
| Publish_Mediator_100Handlers | 5,903.95 ns | 65.38 |    8 |   12000 B |
| Publish_MediatR_100Handlers  | 7,695.78 ns | 85.22 |    9 |   24112 B |

**Takeaway:** Still PlaxionMediator’s strongest category — ahead of or tied with Mediator at higher
fan-out tiers, and clearly faster than MediatR throughout.

## Overall Summary

- **Mediator (source-gen)** remains the fastest overall for baseline dispatch and type variety.
- **PlaxionMediator (post-optimization)** now **beats MediatR** on pipeline Send latency/allocs and
  type-variety latency, keeps Mediator-like 0 B Send0/TypeVariety, and remains strongest at
  notification fan-out.
- Biggest remaining gap vs Mediator: raw type-variety dispatch (~4.6×) and a residual ~192 B/call
  on behavior pipelines (runner + next delegate). See `OPTIMIZATION_REPORT.md` for follow-ups.
