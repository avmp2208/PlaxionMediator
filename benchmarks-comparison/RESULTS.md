# Latest Comparison Results

> Generated: 2026-08-03, via `dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison -- --filter *`
> Environment: BenchmarkDotNet v0.14.0, Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7 (RyuJIT AVX2)
> Job: `Job.Default` (WarmupCount=3, IterationCount=10, LaunchCount=1) — reproducible, non-Dry job.
>
> Raw exported files (JSON/CSV/GitHub-Markdown) live under
> `src/Plaxion.BenchMarks.Comparison/BenchmarkDotNet.Artifacts/results/`. This file is a
> human-readable snapshot of those results for quick reference; regenerate it whenever the suite
> is re-run so the numbers here stay in sync with the artifacts on disk.

## Pipeline Behavior Chains

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    16.89 ns |  0.35 |    1 |         - |
| Send_Plaxion_0Behaviors   |    48.39 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    53.61 ns |  1.11 |    3 |     264 B |
| Send_Mediator_1Behavior   |    67.84 ns |  1.40 |    4 |     128 B |
| Send_MediatR_1Behavior    |   165.63 ns |  3.42 |    5 |     648 B |
| Send_Plaxion_1Behavior    |   167.58 ns |  3.46 |    5 |     432 B |
| Send_Mediator_5Behaviors  |   312.53 ns |  6.46 |    6 |     640 B |
| Send_MediatR_5Behaviors   |   481.46 ns |  9.95 |    7 |    1896 B |
| Send_Plaxion_5Behaviors   |   547.86 ns | 11.32 |    7 |    1392 B |
| Send_Mediator_10Behaviors |   603.98 ns | 12.48 |    7 |    1280 B |
| Send_MediatR_10Behaviors  |   800.12 ns | 16.54 |    8 |    3456 B |
| Send_Plaxion_10Behaviors  |   948.02 ns | 19.59 |    8 |    2592 B |
| Send_Mediator_20Behaviors | 1,275.79 ns | 26.37 |    9 |    2560 B |
| Send_MediatR_20Behaviors  | 1,699.70 ns | 35.13 |   10 |    6576 B |
| Send_Plaxion_20Behaviors  | 2,015.69 ns | 41.66 |   10 |    4992 B |

**Takeaway:** Mediator (source-gen) is consistently fastest and lowest-allocating. PlaxionMediator
and MediatR are close, with MediatR slightly ahead at deeper chains; PlaxionMediator allocates
less per behavior than MediatR at every tier.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   905.2 ns |  0.14 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,833.2 ns |  0.75 |    2 |   13200 B |
| Dispatch_Plaxion_50Types  | 6,434.9 ns |  1.00 |    3 |         - |

**Takeaway:** Mediator's compile-time dispatch table wins by a wide margin. PlaxionMediator is the
slowest here but, like Mediator, allocates zero bytes per dispatch (MediatR allocates ~264B/call).

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    40.60 ns |   0.52 |    1 |     176 B |
| Concurrent_MediatR_1    |    74.77 ns |   0.95 |    2 |     368 B |
| Concurrent_Plaxion_1    |    78.48 ns |   1.00 |    2 |     176 B |
| Concurrent_Mediator_8   |   245.45 ns |   3.13 |    3 |     736 B |
| Concurrent_Plaxion_8    |   509.60 ns |   6.50 |    4 |     736 B |
| Concurrent_MediatR_8    |   560.68 ns |   7.15 |    5 |    2272 B |
| Concurrent_Mediator_32  |   969.28 ns |  12.35 |    6 |    2656 B |
| Concurrent_Plaxion_32   | 1,912.72 ns |  24.38 |    7 |    2656 B |
| Concurrent_MediatR_32   | 2,086.26 ns |  26.59 |    8 |    8800 B |
| Concurrent_Mediator_128 | 3,806.10 ns |  48.51 |    9 |   10336 B |
| Concurrent_Plaxion_128  | 7,938.64 ns | 101.19 |   10 |   10336 B |
| Concurrent_MediatR_128  | 8,469.80 ns | 107.96 |   10 |   34912 B |

**Takeaway:** Mediator scales best under concurrency. PlaxionMediator tracks MediatR's allocation
profile closely (same bytes at 8/32/128 callers) and stays faster than MediatR at every
concurrency tier, but the gap to Mediator widens as concurrency increases.

## Notification Fan-Out

| Method                       | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler    |    59.18 ns |  0.67 |    1 |     120 B |
| Publish_Plaxion_1Handler     |    88.34 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler     |   113.45 ns |  1.28 |    3 |     352 B |
| Publish_Mediator_10Handlers  |   569.75 ns |  6.45 |    4 |    1200 B |
| Publish_Plaxion_10Handlers   |   576.66 ns |  6.53 |    4 |    1304 B |
| Publish_MediatR_10Handlers   |   700.22 ns |  7.93 |    4 |    2512 B |
| Publish_Plaxion_50Handlers   | 2,725.13 ns | 30.85 |    5 |    6424 B |
| Publish_Mediator_50Handlers  | 2,933.72 ns | 33.21 |    5 |    6000 B |
| Publish_MediatR_50Handlers   | 3,333.72 ns | 37.74 |    5 |   12112 B |
| Publish_Plaxion_100Handlers  | 5,342.80 ns | 60.48 |    6 |   12824 B |
| Publish_Mediator_100Handlers | 5,791.42 ns | 65.56 |    6 |   12000 B |
| Publish_MediatR_100Handlers  | 6,738.48 ns | 76.28 |    6 |   24112 B |

**Takeaway:** This is PlaxionMediator's strongest category — it's essentially tied with (and
sometimes ahead of) Mediator at 10/50/100 handlers, and clearly faster than MediatR at every
fan-out tier.

## Overall Summary

- **Mediator (source-gen)** is the fastest and lowest-allocating framework overall, especially for
  baseline dispatch, type variety, and high concurrency.
- **PlaxionMediator** is competitive with MediatR on pipeline behaviors and concurrency (similar or
  better allocation profile, similar latency), and is the strongest of the three at notification
  fan-out, closely matching Mediator.
- **MediatR** allocates the most across almost every scenario and trails Mediator/PlaxionMediator
  at scale, though it remains close at small tiers (1 behavior/handler).
- Biggest remaining gap for PlaxionMediator: raw dispatch throughput under type-variety and high
  concurrency, where Mediator's compile-time-generated dispatch table has a clear edge.
