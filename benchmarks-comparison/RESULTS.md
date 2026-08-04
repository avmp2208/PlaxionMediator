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
| Send_Mediator_0Behaviors  |    15.46 ns |  0.68 |    1 |         - |
| Send_Plaxion_0Behaviors   |    22.70 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    52.16 ns |  2.30 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.53 ns |  3.02 |    4 |     128 B |
| Send_Plaxion_1Behavior    |   119.52 ns |  5.27 |    5 |     128 B |
| Send_MediatR_1Behavior    |   162.58 ns |  7.16 |    6 |     648 B |
| Send_Mediator_5Behaviors  |   308.06 ns | 13.57 |    7 |     640 B |
| Send_Plaxion_5Behaviors   |   391.69 ns | 17.26 |    8 |     640 B |
| Send_MediatR_5Behaviors   |   450.51 ns | 19.85 |    8 |    1896 B |
| Send_Mediator_10Behaviors |   604.65 ns | 26.64 |    9 |    1280 B |
| Send_Plaxion_10Behaviors  |   716.20 ns | 31.55 |    9 |    1280 B |
| Send_MediatR_10Behaviors  |   845.41 ns | 37.25 |   10 |    3456 B |
| Send_Mediator_20Behaviors | 1,335.62 ns | 58.84 |   11 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,493.24 ns | 65.79 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 1,687.25 ns | 74.33 |   11 |    6576 B |

**Takeaway:** Mediator (source-gen) remains the fastest, lowest-allocation option here. PlaxionMediator
tracks it closely at every depth — matching its allocation profile exactly (128/640/1280/2560 B) —
and stays consistently ahead of MediatR on both latency and allocations.

## Type Variety (50 distinct request/handler pairs, dispatched once per iteration)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   851.2 ns |  0.99 |    1 |         - |
| Dispatch_Plaxion_50Types  |   860.4 ns |  1.00 |    1 |         - |
| Dispatch_MediatR_50Types  | 4,703.1 ns |  5.47 |    2 |   13200 B |

**Takeaway:** PlaxionMediator is now essentially tied with Mediator (ratio 1.00 vs 0.99) on this
scenario, while remaining **0 B** allocated — well ahead of MediatR, which allocates ~264 B/call.

## Concurrency (Task.WhenAll, shared ServiceProvider)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.64 ns |   0.88 |    1 |     176 B |
| Concurrent_Plaxion_1    |    45.02 ns |   1.00 |    2 |     176 B |
| Concurrent_MediatR_1    |    78.19 ns |   1.74 |    3 |     368 B |
| Concurrent_Mediator_8   |   235.42 ns |   5.24 |    4 |     736 B |
| Concurrent_Plaxion_8    |   246.61 ns |   5.48 |    4 |     736 B |
| Concurrent_MediatR_8    |   520.90 ns |  11.59 |    5 |    2272 B |
| Concurrent_Mediator_32  |   870.55 ns |  19.36 |    6 |    2656 B |
| Concurrent_Plaxion_32   |   911.44 ns |  20.27 |    6 |    2656 B |
| Concurrent_MediatR_32   | 2,171.30 ns |  48.29 |    7 |    8800 B |
| Concurrent_Mediator_128 | 3,575.40 ns |  79.52 |    8 |   10336 B |
| Concurrent_Plaxion_128  | 3,684.04 ns |  81.94 |    8 |   10336 B |
| Concurrent_MediatR_128  | 8,469.29 ns | 188.36 |    9 |   34912 B |

**Takeaway:** PlaxionMediator scales in step with Mediator under concurrent load, with identical
allocation profiles at every caller tier (176/736/2656/10336 B), and stays well ahead of MediatR
throughout.

## Notification Fan-Out

| Method                       | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler    |    58.92 ns |  0.66 |    1 |     120 B |
| Publish_Plaxion_1Handler     |    89.22 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler     |   115.47 ns |  1.29 |    3 |     352 B |
| Publish_Mediator_10Handlers  |   565.40 ns |  6.34 |    4 |    1200 B |
| Publish_Plaxion_10Handlers   |   604.36 ns |  6.77 |    4 |    1304 B |
| Publish_MediatR_10Handlers   |   743.79 ns |  8.34 |    4 |    2512 B |
| Publish_Plaxion_50Handlers   | 2,752.46 ns | 30.85 |    5 |    6424 B |
| Publish_Mediator_50Handlers  | 2,938.96 ns | 32.94 |    5 |    6000 B |
| Publish_MediatR_50Handlers   | 3,579.49 ns | 40.12 |    5 |   12112 B |
| Publish_Plaxion_100Handlers  | 5,459.38 ns | 61.20 |    6 |   12824 B |
| Publish_Mediator_100Handlers | 5,895.82 ns | 66.09 |    6 |   12000 B |
| Publish_MediatR_100Handlers  | 6,744.40 ns | 75.60 |    6 |   24112 B |

**Takeaway:** PlaxionMediator's strongest category — it edges ahead of Mediator at 50 and 100
handlers, and is consistently faster than MediatR across every fan-out tier.

## Overall Summary

- **Pipeline behaviors:** PlaxionMediator matches Mediator's allocation profile exactly at every
  depth and stays ahead of MediatR on latency and allocations throughout.
- **Type variety:** PlaxionMediator is now essentially on par with Mediator (ratio ~1.00) while
  remaining 0 B allocated, and is roughly 5.5x faster than MediatR with far fewer allocations.
- **Concurrency:** Scaling behavior tracks Mediator closely under load, with identical allocation
  footprints, and a clear lead over MediatR at every caller tier.
- **Notifications:** PlaxionMediator's best category, leading both peers at higher fan-out counts.
- All three frameworks — PlaxionMediator, Mediator, and MediatR — are solid, production-ready
  choices; these numbers simply document where PlaxionMediator stands today so the comparison is
  transparent and reproducible.
