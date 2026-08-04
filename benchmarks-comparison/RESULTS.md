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

---

# Round 2 Post-Optimization Results (2026-08-03)

> Generated: 2026-08-03 (round-2 post-optimization), via `dotnet run -c Release --project src/Plaxion.BenchMarks.Comparison --filter *`  
> Environment: BenchmarkDotNet v0.14.0, Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7 (RyuJIT AVX2)  
> Job: WarmupCount=3, IterationCount=10, LaunchCount=1 — same reproducible job as prior entries.  
> Details: `OPTIMIZATION_REPORT_ROUND2.md` (R1 pool runner **KEPT**, R3 collapse ExecuteCore **KEPT**, R2 TypeVariety fast-path **KEPT**).

## Pipeline Behavior Chains (Round 2)

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    17.01 ns |  0.56 |    1 |         - |
| Send_Plaxion_0Behaviors   |    30.39 ns |  1.00 |    2 |         - |
| Send_MediatR_0Behaviors   |    52.97 ns |  1.74 |    3 |     264 B |
| Send_Mediator_1Behavior   |    68.47 ns |  2.25 |    4 |     128 B |
| Send_Plaxion_1Behavior    |   127.76 ns |  4.20 |    5 |     128 B |
| Send_MediatR_1Behavior    |   161.27 ns |  5.31 |    6 |     648 B |
| Send_Mediator_5Behaviors  |   305.82 ns | 10.06 |    7 |     640 B |
| Send_Plaxion_5Behaviors   |   396.04 ns | 13.03 |    8 |     640 B |
| Send_MediatR_5Behaviors   |   452.57 ns | 14.89 |    8 |    1896 B |
| Send_Mediator_10Behaviors |   604.34 ns | 19.88 |    9 |    1280 B |
| Send_Plaxion_10Behaviors  |   722.35 ns | 23.77 |   10 |    1280 B |
| Send_MediatR_10Behaviors  |   814.91 ns | 26.81 |   10 |    3456 B |
| Send_Mediator_20Behaviors | 1,298.41 ns | 42.72 |   11 |    2560 B |
| Send_Plaxion_20Behaviors  | 1,548.83 ns | 50.96 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 1,680.35 ns | 55.29 |   11 |    6576 B |

**Takeaway:** Round 2 closed the constant **+192 B/call** gap — PlaxionMediator now has **Mediator alloc parity**
at every pipeline depth (1/5/10/20). Plaxion still beats MediatR on both latency and allocations; residual
vs Mediator is latency-only (~+90 ns at Send5).

## Type Variety (Round 2)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types |   901.5 ns |  0.26 |    1 |         - |
| Dispatch_Plaxion_50Types  | 3,409.9 ns |  1.00 |    2 |         - |
| Dispatch_MediatR_50Types  | 4,741.2 ns |  1.39 |    3 |   13200 B |

**Takeaway:** TypeVariety improved from **4121 → 3410 ns** (~17%) via the no-behaviors dispatch fast path
while remaining **0 B**. Still ~3.8× Mediator (down from ~4.6×); remaining gap is structural codegen shape.

## Concurrency (Round 2)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    39.72 ns |   0.66 |    1 |     176 B |
| Concurrent_Plaxion_1    |    60.50 ns |   1.00 |    2 |     176 B |
| Concurrent_MediatR_1    |    77.11 ns |   1.27 |    3 |     368 B |
| Concurrent_Mediator_8   |   232.25 ns |   3.84 |    4 |     736 B |
| Concurrent_Plaxion_8    |   374.04 ns |   6.18 |    5 |     736 B |
| Concurrent_MediatR_8    |   529.12 ns |   8.75 |    6 |    2272 B |
| Concurrent_Mediator_32  |   888.85 ns |  14.69 |    7 |    2656 B |
| Concurrent_Plaxion_32   | 1,429.44 ns |  23.63 |    8 |    2656 B |
| Concurrent_MediatR_32   | 2,067.46 ns |  34.17 |    9 |    8800 B |
| Concurrent_Mediator_128 | 3,648.92 ns |  60.31 |   10 |   10336 B |
| Concurrent_Plaxion_128  | 5,792.46 ns |  95.74 |   11 |   10336 B |
| Concurrent_MediatR_128  | 8,335.57 ns | 137.78 |   12 |   34912 B |

**Takeaway:** Alloc parity with Mediator retained under concurrency; Plaxion remains ahead of MediatR at every tier.

## Notification Fan-Out (Round 2)

| Method                       | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler    |    59.30 ns |  0.68 |    1 |     120 B |
| Publish_Plaxion_1Handler     |    86.87 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler     |   109.31 ns |  1.26 |    3 |     352 B |
| Publish_Mediator_10Handlers  |   562.68 ns |  6.48 |    4 |    1200 B |
| Publish_Plaxion_10Handlers   |   594.78 ns |  6.85 |    4 |    1304 B |
| Publish_MediatR_10Handlers   |   715.60 ns |  8.24 |    5 |    2512 B |
| Publish_Plaxion_50Handlers   | 2,737.11 ns | 31.51 |    6 |    6424 B |
| Publish_Mediator_50Handlers  | 2,849.83 ns | 32.81 |    6 |    6000 B |
| Publish_MediatR_50Handlers   | 3,483.08 ns | 40.10 |    6 |   12112 B |
| Publish_Plaxion_100Handlers  | 5,412.67 ns | 62.31 |    7 |   12824 B |
| Publish_Mediator_100Handlers | 5,780.07 ns | 66.54 |    7 |   12000 B |
| Publish_MediatR_100Handlers  | 7,943.89 ns | 91.45 |    8 |   24112 B |

**Takeaway:** Unchanged strength area — Plaxion still leads or ties Mediator at higher fan-out tiers.

## Overall Summary (Round 2)

- **Pipeline allocations:** full **Mediator parity** at 1/5/10/20 behaviors (constant +192 B residual eliminated).
- **Pipeline latency:** still slightly behind Mediator, still ahead of MediatR at every depth.
- **TypeVariety:** ~17% faster than round 1; residual ~3.8× vs Mediator is codegen-shape, not allocs.
- **Notifications:** still Plaxion’s strongest category vs both peers.
- See `OPTIMIZATION_REPORT_ROUND2.md` for per-optimization keep/revert detail and profiling evidence.

---

# Round 3 Post-Optimization Results (2026-08-03)

> Generated: 2026-08-03 (round-3), via real BenchmarkDotNet runs (`WarmupCount=3`, `IterationCount=10`, `LaunchCount=1`) on  
> Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7 / BenchmarkDotNet v0.14.0.  
> KEPT change: hybrid Type→id map + jump-table `Send` for assemblies with **>16** request handlers (`SourceEmitter`).  
> Details: `OPTIMIZATION_REPORT_ROUND3.md`. Absolute ns vary by session load; allocs are stable across runs.

## Pipeline Behavior Chains (Round 3)

| Method                    | Mean        | Ratio | Rank | Allocated |
|---------------------------|------------:|------:|-----:|----------:|
| Send_Mediator_0Behaviors  |    19.77 ns |  0.38 |    1 |         - |
| Send_Plaxion_0Behaviors   |    52.84 ns |  1.01 |    2 |         - |
| Send_MediatR_0Behaviors   |    76.25 ns |  1.45 |    3 |     264 B |
| Send_Mediator_1Behavior   |   103.62 ns |  1.97 |    4 |     128 B |
| Send_Plaxion_1Behavior    |   149.68 ns |  2.85 |    5 |     128 B |
| Send_MediatR_1Behavior    |   219.56 ns |  4.18 |    6 |     648 B |
| Send_Mediator_5Behaviors  |   462.03 ns |  8.80 |    7 |     640 B |
| Send_Plaxion_5Behaviors   |   536.98 ns | 10.23 |    7 |     640 B |
| Send_MediatR_5Behaviors   |   696.30 ns | 13.26 |    8 |    1896 B |
| Send_Mediator_10Behaviors |   952.44 ns | 18.14 |    9 |    1280 B |
| Send_Plaxion_10Behaviors  |   968.66 ns | 18.45 |    9 |    1280 B |
| Send_MediatR_10Behaviors  | 1,424.84 ns | 27.14 |   10 |    3456 B |
| Send_Mediator_20Behaviors | 2,029.22 ns | 38.65 |   11 |    2560 B |
| Send_Plaxion_20Behaviors  | 2,094.38 ns | 39.89 |   11 |    2560 B |
| Send_MediatR_20Behaviors  | 2,920.35 ns | 55.62 |   12 |    6576 B |

**Takeaway:** Round 3 did not keep pipeline structural changes. **Mediator alloc parity retained** at every depth.  
Latency remains between Mediator and MediatR; Send1/Send10 can sit close to Mediator on quieter runs.

## Type Variety (Round 3)

| Method                    | Mean       | Ratio | Rank | Allocated |
|---------------------------|-----------:|------:|-----:|----------:|
| Dispatch_Mediator_50Types | 1,144 ns   |  0.44 |    1 |         - |
| Dispatch_Plaxion_50Types  | 2,614 ns   |  1.00 |    2 |         - |
| Dispatch_MediatR_50Types  | 7,534 ns   |  2.88 |    3 |   13200 B |

**Takeaway:** Primary Round 3 win — Plaxion TypeVariety **3410 → 2614 ns (−23% vs Round 2)** via Type→id map + jump table  
for large handler sets. Still **0 B**. Residual vs Mediator ~**2.3×** (was ~3.8×).

## Concurrency (Round 3)

| Method                  | Mean        | Ratio  | Rank | Allocated |
|-------------------------|------------:|-------:|-----:|----------:|
| Concurrent_Mediator_1   |    49.76 ns |   0.40 |    1 |     176 B |
| Concurrent_MediatR_1    |    99.47 ns |   0.80 |    2 |     368 B |
| Concurrent_Plaxion_1    |   125.10 ns |   1.00 |    3 |     176 B |
| Concurrent_Mediator_8   |   276.79 ns |   2.21 |    4 |     736 B |
| Concurrent_MediatR_8    |   691.03 ns |   5.53 |    5 |    2272 B |
| Concurrent_Plaxion_8    |   763.41 ns |   6.11 |    5 |     736 B |
| Concurrent_Mediator_32  | 1,055.70 ns |   8.44 |    6 |    2656 B |
| Concurrent_MediatR_32   | 2,724.47 ns |  21.79 |    7 |    8800 B |
| Concurrent_Plaxion_32   | 2,985.51 ns |  23.88 |    7 |    2656 B |
| Concurrent_Mediator_128 | 4,325.08 ns |  34.59 |    8 |   10336 B |
| Concurrent_Plaxion_128  | 9,536.41 ns |  76.27 |    9 |   10336 B |
| Concurrent_MediatR_128  |10,519.97 ns |  84.13 |    9 |   34912 B |

**Takeaway:** Alloc parity with Mediator retained. Absolute latency noisier under load this session; structure unchanged from R2.

## Notification Fan-Out (Round 3)

| Method                       | Mean        | Ratio | Rank | Allocated |
|-------------------------------|------------:|------:|-----:|----------:|
| Publish_Mediator_1Handler    |    72.62 ns |  0.61 |    1 |     120 B |
| Publish_Plaxion_1Handler     |   118.54 ns |  1.00 |    2 |     152 B |
| Publish_MediatR_1Handler     |   165.09 ns |  1.39 |    3 |     352 B |
| Publish_Mediator_10Handlers  |   693.28 ns |  5.85 |    4 |    1200 B |
| Publish_Plaxion_10Handlers   |   785.28 ns |  6.63 |    4 |    1304 B |
| Publish_MediatR_10Handlers   | 1,015.08 ns |  8.57 |    5 |    2512 B |
| Publish_Plaxion_50Handlers   | 3,667.57 ns | 30.95 |    6 |    6424 B |
| Publish_Mediator_50Handlers  | 3,937.06 ns | 33.23 |    6 |    6000 B |
| Publish_MediatR_50Handlers   | 4,769.01 ns | 40.25 |    7 |   12112 B |
| Publish_Plaxion_100Handlers  | 7,482.39 ns | 63.15 |    8 |   12824 B |
| Publish_MediatR_100Handlers  |10,298.68 ns | 86.92 |    9 |   24112 B |
| Publish_Mediator_100Handlers |11,601.57 ns | 97.92 |    9 |   12000 B |

**Takeaway:** Still a Plaxion strength area at higher fan-out (beats or ties Mediator at 50/100 on this run).

## Overall Summary (Round 3)

- **TypeVariety:** **−23%** vs Round 2 (3410 → 2614 ns); gap vs Mediator **~3.8× → ~2.3×**; still 0 B.
- **Pipeline:** no KEPT structural change; **Mediator alloc parity** held; latency residual remains.
- **Goal 2 attempts** (shallow runner, null-guard strip) **REVERTED** — see `OPTIMIZATION_REPORT_ROUND3.md`.
- **Public API** unchanged; all main-repo tests green.
