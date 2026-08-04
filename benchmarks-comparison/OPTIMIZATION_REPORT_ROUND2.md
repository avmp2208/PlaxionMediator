# PlaxionMediator Optimization Report — Round 2

> Generated: 2026-08-03  
> Scope: residual-gap optimizations driven by `PROFILING_REPORT_ROUND2.md` (R1 → R3 → R2)  
> Baseline: post-round-1 state in `OPTIMIZATION_REPORT.md` / `RESULTS.md` and round-2 analysis  
> Environment: Windows 11, 12th Gen Intel Core i7-12700K, .NET 9.0.7, BenchmarkDotNet v0.14.0  
> Job: WarmupCount=3, IterationCount=10, LaunchCount=1 (same as prior RESULTS entries)  
> Public API: **unchanged**. Full `dotnet test PlaxionMediator.sln -c Release`: **0 failures** after every KEPT step.

This report documents the round-2 optimization loop. Each opportunity was implemented **one at a time**, validated with full main-repo tests, real BenchmarkDotNet runs, and `dotnet-trace` profiling before moving on.

---

## Executive summary

| ID | Opportunity | Verdict | Headline measured effect |
|----|-------------|---------|--------------------------|
| **R1** | Eliminate ~192 B/call pipeline entry alloc (pool runner + drop method-group `Func`) | **KEPT** | Pipeline allocs match Mediator at every depth (1/5/10/20); `+192 B` residual closed |
| **R3** | Collapse `ExecuteAsync` → `ExecuteCore` → runner entry frames | **KEPT** | Removed `ExecuteCore` hop; Send5 ~2–3% faster vs R1-only; Send0 improved |
| **R2** | TypeVariety dispatch overhead | **KEPT** (low-complexity only) | TypeVariety50 **4121 → ~3410 ns** (~17% better); residual gap vs Mediator still ~3.8× |
| R4 | Source-gen fixed behavior chains | **NOT ATTEMPTED** | R1+R3 sufficient for alloc parity; R4 complexity out of scope |
| R5 / R6 | Further DI / CastOrAdapt | **NOT TOUCHED** | Explicitly out of scope per task |

**Final Plaxion vs baselines (full suite, 2026-08-03 post-round-2):**

| Scenario | Plaxion | Mediator | MediatR | Notes |
|----------|--------:|---------:|--------:|-------|
| Send 0 | 30.4 ns / **0 B** | 17.0 ns / 0 B | 53.0 ns / 264 B | Still ahead of MediatR |
| Send 1 | 127.8 ns / **128 B** | 68.5 ns / 128 B | 161.3 ns / 648 B | **Alloc parity with Mediator** |
| Send 5 | 396.0 ns / **640 B** | 305.8 ns / 640 B | 452.6 ns / 1896 B | **Alloc parity**; ~+90 ns residual latency |
| Send 10 | 722.4 ns / **1280 B** | 604.3 ns / 1280 B | 814.9 ns / 3456 B | Alloc parity |
| Send 20 | 1548.8 ns / **2560 B** | 1298.4 ns / 2560 B | 1680.4 ns / 6576 B | Alloc parity (Send20 mean noisy ±71 ns) |
| TypeVariety50 | **3409.9 ns / 0 B** | 901.5 ns / 0 B | 4741.2 ns / 13200 B | Improved from 4121 ns; still ~3.8× Mediator |
| Concurrent_128 | 5792 ns / 10336 B | 3649 ns / 10336 B | 8336 ns / 34912 B | Alloc parity with Mediator |
| Publish_100 | 5413 ns / 12824 B | 5780 ns / 12000 B | 7944 ns / 24112 B | Still competitive / often ahead of Mediator |

---

## R1 — Eliminate constant ~192 B/call pipeline entry allocation

### Why

Round-2 profiling pinned a **constant +192 B/call** vs Mediator on every Send with ≥1 behavior to:

1. `new PipelineRunner<,>(...)` heap instance per call  
2. Cached-but-first-time `RequestHandlerDelegate` (`_next ??= Next`) allocated with the runner  
3. Generated `handler.Handle` method-group → `Func<TRequest, CT, ValueTask<TResponse>>` per call  

Struct/stack runners were rejected (unsafe across `await` in async behaviors).

### What changed

| File | Change |
|------|--------|
| `src/PlaxionMediator.Pipeline/PipelineComposer.cs` | Pooled `PipelineRunner` (TLS + `ConcurrentBag`, max 64); `_next` and completion continuation bound once in ctor; async completion via `IValueTaskSource` + `ManualResetValueTaskSourceCore` so pool return does not need an extra async state machine; new `ExecuteAsync(..., IRequestHandler<,>, ...)` overload for generated code |
| `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs` | Emit `PipelineComposer.ExecuteAsync(request, behaviors, handler, ct)` (handler instance) instead of `handler.Handle` method-group |

Semantics preserved: behavior order, CT propagation, `PipelineExecutionException` wrapping (OCE / `PlaxionMediatorException` still unwrap).

### BenchmarkDotNet (PipelineBehaviorBenchmarks)

| Scenario | Before (round-1 RESULTS) | After R1 (isolated run) | Δ Mean | Δ Alloc |
|----------|-------------------------:|------------------------:|-------:|--------:|
| Send_Plaxion_0Behaviors | 30.05 ns / 0 B | 35.86 ns / 0 B | noise | 0 |
| Send_Plaxion_1Behavior | 134.02 ns / **320 B** | 126.01 ns / **128 B** | ~−6% | **−192 B** |
| Send_Plaxion_5Behaviors | 403.45 ns / **832 B** | 403.85 ns / **640 B** | ~0% | **−192 B** |
| Send_Plaxion_10Behaviors | 736.41 ns / **1472 B** | 728.11 ns / **1280 B** | ~−1% | **−192 B** |
| Send_Plaxion_20Behaviors | 1500.17 ns / **2752 B** | 1488.92 ns / **2560 B** | ~−1% | **−192 B** |

Mediator comparison after R1: **identical Allocated** at 1/5/10/20 behaviors (128 / 640 / 1280 / 2560 B).

### Profiling (real captures, `profiling-results/round2-r1`)

| Scenario | Ops/s under profiler (5s) | Observation vs round-2 baseline |
|----------|--------------------------:|----------------------------------|
| Send5 | **2,044,482** | Round-2 baseline under heavier profile was 1,422,560; bare-path throughput improved |
| Send20 | **516,861** | Round-2 baseline 354,153 under heavier profile |

`dotnet-trace report topN` (Send5/Send20):

- `PipelineRunner.Next` exclusive cost collapsed from **~1.8%** (round-2 Send5 topN) to **≲0.03%**  
- No per-call `new PipelineRunner` / method-group `Func` signal on the hot path after warmup  
- Behavior `MoveNext` frames remain the dominant non-idle work (expected; same async SM model as Mediator)

### Tests

`dotnet test PlaxionMediator.sln -c Release` — **all passed** (0 failed).

### Verdict

**KEPT** — closed the entire constant **+192 B/call** residual vs Mediator with clear BDN evidence at every pipeline depth.

---

## R3 — Reduce framework call layers (`ExecuteAsync` → `ExecuteCore` → runner)

### Why

Round-2 call trees showed a fixed Plaxion-only frame chain (`PipelineComposer.ExecuteAsync` → `ExecuteCore` → runner entry) that Mediator’s flatter generated path does not have. R1 already absorbed some of the runner-entry cost via pooling; R3 removes the remaining hop.

### What changed

| File | Change |
|------|--------|
| `src/PlaxionMediator.Pipeline/PipelineComposer.cs` | Removed private `ExecuteCore`; both `ExecuteAsync` overloads contain the empty-check + `Rent(...).Run()` body directly; `[MethodImpl(AggressiveInlining)]` on both overloads; `Compose` delegates to `ExecuteAsync` |

No public signature changes. Generated call site still `PipelineComposer.ExecuteAsync(...)`.

### BenchmarkDotNet (after R3, vs R1-only)

| Scenario | After R1 | After R3 | Δ |
|----------|---------:|---------:|--:|
| Send_Plaxion_0Behaviors | 35.86 ns | **28.85 ns** | **−19.5%** |
| Send_Plaxion_1Behavior | 126.01 ns / 128 B | 128.34 ns / 128 B | ~noise |
| Send_Plaxion_5Behaviors | 403.85 ns / 640 B | **393.48 ns** / 640 B | **−2.6%** |
| Send_Plaxion_10Behaviors | 728.11 ns / 1280 B | 740.30 ns / 1280 B | ~noise |
| Send_Plaxion_20Behaviors | 1488.92 ns / 2560 B | 1483.97 ns / 2560 B | ~0% |

### Profiling (`profiling-results/round2-r3`)

| Scenario | Ops/s (5s) |
|----------|-----------:|
| Send5 | **2,068,111** |
| Send20 | **515,933** |

TopN: `ExecuteCore` frame gone; thin `ExecuteAsync` remains (~1.5% inclusive / ~0.01% exclusive on Send5). `Next` exclusive still ~0.02%.

### Tests

Full Release test suite — **all passed**.

### Verdict

**KEPT** — measurable Send5 (~2.6%) and Send0 improvements, cleaner entry path, no alloc regression. Meets keep threshold on the primary residual-latency scenario.

---

## R2 — TypeVariety dispatch overhead

### Why

TypeVariety50 was ~4.6× slower than Mediator (4121 vs 894 ns) with **0 B** on both sides. Round-2 attributed this to many distinct `SendCore_*` methods / type-check dispatch shape, not allocations. Full Mediator-parity dispatch tables were judged high complexity.

### What changed (low-complexity only)

| File | Change |
|------|--------|
| `src/PlaxionMediator.Pipeline/PipelineBehaviorResolver.cs` | `HasNoPipelineBehaviors()` — process-wide policy (none vs any `IPipelineBehavior<,>` registration) so generated code can skip per-type `GetBehaviors` when the app has zero pipeline behaviors |
| `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs` | `[MethodImpl(AggressiveInlining)]` on each `SendCore_*`; early `if (PipelineBehaviorResolver.HasNoPipelineBehaviors()) return handler.Handle(...)` |

No public API change. When any behavior *is* registered (pipeline benchmarks), the fast path is a single volatile read then the existing `GetBehaviors` path.

**Not attempted:** Mediator-style compact message/lookup tables, rewriting the `switch (request)` shape into dictionary/dispatch tables, or R4 fixed chains. Remaining ~3.8× gap is structural codegen shape; closing it would need disproportionate complexity.

### BenchmarkDotNet (TypeVarietyBenchmarks)

| Method | Before (round-1 RESULTS) | After R2 (isolated) | After full suite | vs before |
|--------|-------------------------:|--------------------:|-----------------:|----------:|
| Dispatch_Plaxion_50Types | 4120.6 ns / 0 B | **3359.4 ns** / 0 B | **3409.9 ns** / 0 B | **~−17%** |
| Dispatch_Mediator_50Types | 894.3 ns / 0 B | 899.4 ns / 0 B | 901.5 ns / 0 B | (baseline) |
| Dispatch_MediatR_50Types | 5105.4 ns / 13200 B | 4903.8 ns / 13200 B | 4741.2 ns / 13200 B | — |

Plaxion/Mediator ratio: **~4.6× → ~3.8×**.

### Profiling (`profiling-results/round2-r2`)

| Scenario | Ops/s (5s) | Round-2 baseline ops/s |
|----------|-----------:|-----------------------:|
| TypeVariety50 | **243,837** | 169,799 |

Relative throughput under profiler improved ~**+44%** vs the round-2 analysis capture (absolute ops/s still profiler-depressed vs BDN).

### Pipeline non-regression (full suite after R2)

| Scenario | Final Plaxion | Alloc vs Mediator |
|----------|--------------:|-------------------|
| Send5 | 396.04 ns / 640 B | **parity** |
| Send20 | 1548.83 ns / 2560 B (±71 ns noisy) | **parity** |

No alloc regression from the extra `HasNoPipelineBehaviors` check on behavior-heavy paths.

### Tests

Full Release test suite — **all passed**.

### Verdict

**KEPT** — clear ~17% TypeVariety win from a safe, low-complexity prolog/fast-path change. Full Mediator-parity dispatch **not** pursued (documented residual).

---

## R4 — Source-generated fixed behavior chains

**NOT ATTEMPTED.**

R1 already achieved **Mediator alloc parity** on pipeline Sends. R3 + R2 delivered incremental latency wins without fixed-chain codegen. R4 remains a documented high-complexity follow-up if residual ~90–250 ns pipeline latency vs Mediator becomes a product priority.

---

## Overall summary

### Verdicts

| Item | Status |
|------|--------|
| R1 pool runner + handler-instance ExecuteAsync | **KEPT** |
| R3 collapse ExecuteCore | **KEPT** |
| R2 HasNoPipelineBehaviors + SendCore inlining | **KEPT** |
| R4 fixed chains | **NOT ATTEMPTED** |
| R5 / R6 | **NOT TOUCHED** (per scope) |

### Latency / allocation deltas (Plaxion, round-1 RESULTS → round-2 final full suite)

| Scenario | Mean before → after | Alloc before → after |
|----------|---------------------|----------------------|
| Send0 | 30.05 → 30.39 ns | 0 → 0 B |
| Send1 | 134 → 128 ns | **320 → 128 B** |
| Send5 | 403 → 396 ns | **832 → 640 B** |
| Send10 | 736 → 722 ns | **1472 → 1280 B** |
| Send20 | 1500 → 1549 ns* | **2752 → 2560 B** |
| TypeVariety50 | 4121 → **3410 ns** | 0 → 0 B |

\*Send20 final full-suite mean had elevated StdDev (71 ns); earlier isolated R1/R3 runs were ~1484–1500 ns. Alloc figure is stable and authoritative.

### Residual gap vs Mediator

1. **Pipeline latency** (~+60–250 ns depending on depth) with **identical allocations** — remaining cost is structural (index trampoline + exception-wrapping async path vs Mediator’s flatter generated chain). R4 is the main remaining lever.  
2. **TypeVariety** (~3.8×) — still dominated by multi-type switch/`SendCore_*` shape vs Mediator’s compact dispatch; needs larger codegen investment.  
3. **Notifications** — still a Plaxion strength (often ≤ Mediator at high fan-out).

### Constraints confirmation

- **No public API changes** (`ISender`/`IPublisher`/`AddPlaxionMediator`/behavior interfaces unchanged; only internal + `EditorBrowsable(Never)` helpers).  
- **All main-repo tests pass** after every KEPT optimization.  
- Comparison adapters / benchmark class files **not** modified.  
- Work order R1 → R3 → R2 respected; R5/R6 untouched.

### Files modified / added

**Main library / generators**

- `src/PlaxionMediator.Pipeline/PipelineComposer.cs` (rewrite: pool + IValueTaskSource + handler overload; ExecuteCore removed)  
- `src/PlaxionMediator.Pipeline/PipelineBehaviorResolver.cs` (`HasNoPipelineBehaviors` policy)  
- `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs` (handler-instance ExecuteAsync; AggressiveInlining; no-behaviors fast path)

**Reports / results**

- `benchmarks-comparison/OPTIMIZATION_REPORT_ROUND2.md` (this file)  
- `benchmarks-comparison/RESULTS.md` (appended 2026-08-03 round-2 section)  
- `benchmarks-comparison/PROFILING_REPORT_ROUND2.md` (appended post-optimization section)

**Ephemeral profiling (gitignored)**

- `benchmarks-comparison/profiling-results/round2-r1/` (Send5, Send20 after R1)  
- `benchmarks-comparison/profiling-results/round2-r3/` (Send5, Send20 after R3)  
- `benchmarks-comparison/profiling-results/round2-r2/` (TypeVariety50 after R2)
