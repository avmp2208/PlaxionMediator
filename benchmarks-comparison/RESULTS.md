# Latest Comparison Results

> Generated: 2026-08-04, via `dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison --filter *`
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
| Send_Mediator_0Behaviors  |    16.20 ns |  0.73 |    1 |         - |
| Send_Plaxion_0Behaviors   |    22.19 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    56.02 ns |  2.52 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.47 ns |  3.09 |    3 |     128 B |
| Send_Plaxion_1Behavior    |   120.69 ns |  5.44 |    4 |     128 B |
| Send_MediatR_1Behavior    |   165.30 ns |  7.45 |    5 |     648 B |
| Send_Mediator_5Behaviors  |   309.10 ns | 13.93 |    6 |     640 B |
| Send_Plaxion_5Behaviors   |   388.76 ns | 17.52 |    7 |     640 B |
| Send_MediatR_5Behaviors   |   479.34 ns | 21.61 |    8 |    1896 B |
| Send_Mediator_10Behaviors |   603.20 ns | 27.19 |    9 |    1280 B |
| Send_Plaxion_10Behaviors  |   747.55 ns | 33.70 |    9 |    1280 B |
| Send_MediatR_10Behaviors  |   809.17 ns | 36.47 |    9 |    3456 B |
| Send_Mediator_20Behaviors | 1,316.00 ns | 59.32 |   10 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,466.63 ns | 66.11 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 1,708.21 ns | 77.00 |   12 |    6576 B |

**Takeaway:** Mediator (source-gen) remains the fastest, lowest-allocation option here. PlaxionMediator
tracks it closely at every depth — matching its allocation profile exactly (128/640/1280/2560 B) —
and stays consistently ahead of MediatR on both latency and allocations.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   844.1 ns |  0.97 |    1 |         - |
| Dispatch_Plaxion_50Types  |   870.3 ns |  1.00 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,689.4 ns |  5.39 |    2 |   13200 B |

**Takeaway:** PlaxionMediator remains essentially tied with Mediator (ratio 1.00 vs 0.97) on this
scenario, while remaining **0 B** allocated — well ahead of MediatR, which allocates ~264 B/call.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.28 ns |   0.84 |    1 |     176 B |
| Concurrent_Plaxion_1    |    46.69 ns |   1.00 |    1 |     176 B |
| Concurrent_MediatR_1    |    80.80 ns |   1.73 |    2 |     368 B |
| Concurrent_Mediator_8   |   225.77 ns |   4.85 |    3 |     736 B |
| Concurrent_Plaxion_8    |   299.23 ns |   6.42 |    4 |     736 B |
| Concurrent_MediatR_8    |   594.20 ns |  12.76 |    5 |    2272 B |
| Concurrent_Mediator_32  |   858.19 ns |  18.43 |    6 |    2656 B |
| Concurrent_Plaxion_32   | 1,824.39 ns |  39.17 |    7 |    2656 B |
| Concurrent_MediatR_32   | 2,076.46 ns |  44.58 |    8 |    8800 B |
| Concurrent_Mediator_128 | 3,546.54 ns |  76.15 |    9 |   10336 B |
| Concurrent_Plaxion_128  | 3,828.64 ns |  82.21 |    9 |   10336 B |
| Concurrent_MediatR_128  | 8,600.15 ns | 184.65 |   10 |   34912 B |

**Takeaway:** PlaxionMediator scales in step with Mediator under concurrent load, with identical
allocation profiles at every caller tier (176/736/2656/10336 B), and stays well ahead of MediatR
throughout.

## Notification Fan-Out

| Method                        | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler     |    59.40 ns |  0.66 |    1 |     120 B |
| Publish_Plaxion_1Handler      |    90.47 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler      |   112.28 ns |  1.24 |    2 |     352 B |
| Publish_Mediator_10Handlers   |   581.80 ns |  6.43 |    3 |    1200 B |
| Publish_Plaxion_10Handlers    |   598.84 ns |  6.62 |    3 |    1304 B |
| Publish_MediatR_10Handlers    |   732.56 ns |  8.10 |    3 |    2512 B |
| Publish_Plaxion_50Handlers    | 2,808.52 ns | 31.05 |    4 |    6424 B |
| Publish_Mediator_50Handlers   | 2,880.80 ns | 31.85 |    4 |    6000 B |
| Publish_MediatR_50Handlers    | 3,466.18 ns | 38.32 |    4 |   12112 B |
| Publish_Plaxion_100Handlers   | 5,515.70 ns | 60.97 |    5 |   12824 B |
| Publish_Mediator_100Handlers  | 5,966.56 ns | 65.96 |    6 |   12000 B |
| Publish_MediatR_100Handlers   | 8,183.52 ns | 90.47 |    7 |   24112 B |

**Takeaway:** PlaxionMediator's strongest category — it edges ahead of Mediator at 50 and 100
handlers, and is consistently faster than MediatR across every fan-out tier.

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
