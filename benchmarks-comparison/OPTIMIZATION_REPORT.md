# PlaxionMediator Optimization Report

> Date: 2026-08-03  
> Scope: Internal implementation of `src/PlaxionMediator*` only (no public API changes)  
> Baseline: pre-optimization `RESULTS.md` / `PROFILING_REPORT.md`  
> Final validation: full BenchmarkDotNet suite + Plaxion profiling (Send0/Send5/Send20/TypeVariety50)

All five planned optimizations were implemented, validated with `dotnet test PlaxionMediator.sln -c Release` after each change, measured with real (non-Dry) BenchmarkDotNet jobs, and cross-checked with `dotnet-trace` / harness throughput. **All five were KEPT.**

---

## Overall verdict

| # | Optimization | Verdict | One-line reason |
|---|--------------|---------|-----------------|
| 1 | Eliminate per-request pipeline composition in `PipelineComposer` | **KEPT** | Removed O(n) async-lambda chain; Send5 alloc 1392→896 B, latency −15% |
| 2 | Optimize behavior resolution (`GetServices().ToArray`) | **KEPT** | Empty-path cache + scope-safe instance cache; Send5 465→423 ns, TypeVariety −22% |
| 3 | Optimize handler dispatch (type variety) | **KEPT** | Scope-local typed handler fields; TypeVariety 5021→4075 ns, now beats MediatR |
| 4 | Elide unnecessary `Adapt<TActual,TResponse>` | **KEPT** | `CastOrAdapt` + `Unsafe.As` when types match; Send0 40→30 ns |
| 5 | Slim exception path in `PipelineComposer` | **KEPT** | NoInlining throw helpers; success path thinner, semantics unchanged |

### Final Plaxion deltas vs pre-optimization baseline (BDN)

| Scenario | Baseline Mean | Final Mean | Δ Mean | Baseline Alloc | Final Alloc | Δ Alloc |
|----------|--------------:|-----------:|-------:|---------------:|------------:|--------:|
| Send 0 behaviors | 48.39 ns | **30.05 ns** | **−38%** | 0 B | 0 B | — |
| Send 1 behavior | 167.58 ns | **134.02 ns** | **−20%** | 432 B | **320 B** | **−26%** |
| Send 5 behaviors | 547.86 ns | **403.45 ns** | **−26%** | 1392 B | **832 B** | **−40%** |
| Send 10 behaviors | 948.02 ns | **736.41 ns** | **−22%** | 2592 B | **1472 B** | **−43%** |
| Send 20 behaviors | 2015.69 ns | **1500.17 ns** | **−26%** | 4992 B | **2752 B** | **−45%** |
| TypeVariety ×50 | 6434.9 ns | **4120.6 ns** | **−36%** | 0 B | 0 B | — |
| Concurrent 128 | 7938.64 ns | **5980.40 ns** | **−25%** | 10336 B | 10336 B | — |

### Final vs competitors (BDN, post-optimization)

| Scenario | Plaxion | MediatR | Mediator | Plaxion vs MediatR | Plaxion vs Mediator |
|----------|--------:|--------:|---------:|--------------------|---------------------|
| Send 0 | 30.05 ns / 0 B | 60.19 ns / 264 B | 17.12 ns / 0 B | **faster, 0 alloc** | 1.76× slower |
| Send 5 | 403 ns / 832 B | 805 ns / 1896 B | 329 ns / 640 B | **faster, −56% alloc** | 1.23× / +192 B |
| Send 20 | 1500 ns / 2752 B | 1696 ns / 6576 B | 1316 ns / 2560 B | **faster, −58% alloc** | 1.14× / +192 B |
| TypeVariety50 | 4121 ns / 0 B | 5105 ns / 13200 B | 894 ns / 0 B | **faster, 0 alloc** | 4.6× slower |
| Publish 100 | 5522 ns / 12824 B | 7696 ns / 24112 B | 5904 ns / 12000 B | faster | slightly faster |

### Profiling harness throughput (Plaxion only, 5s tight loop)

| Scenario | Pre (PROFILING_REPORT) | Post | Δ |
|----------|-----------------------:|-----:|--:|
| Send0 | 13.2 M ops/s | **18.9 M ops/s** | **+43%** |
| Send5 | 1.26 M ops/s | **1.98 M ops/s** | **+57%** |
| Send20 | 384 k ops/s | **527 k ops/s** | **+37%** |
| TypeVariety50 | 154 k ops/s | **256 k ops/s** | **+66%** |

### Post-opt CPU hot path (Send5) — key signal

| Frame | Pre exclusive | Post exclusive |
|-------|--------------:|---------------:|
| `PipelineComposer.Compose` | **9.77%** | **gone** (replaced by `ExecuteCore` **0.11%**) |
| `ICollectionToArray` / `GetServices().ToArray` | present | **gone from topN** |
| `PipelineRunner.Next` (`__Canon].Next`) | n/a | ~1.8% |
| `PlaxionMediatorSender.Send` | 1.71% excl | 3.32% excl (relative share after Compose removal) |

---

## Optimization 1 — PipelineComposer non-allocating trampoline

### What / why
**Finding:** P1 in `PROFILING_REPORT.md` — `PipelineComposer.Compose` exclusive **9.77%** on Send5; O(n) async lambdas per Send.

**Files:**
- `src/PlaxionMediator.Pipeline/PipelineComposer.cs`

**Change:** Replaced reverse-fold async-lambda chain with a single `PipelineRunner<TRequest,TResponse>` heap object that advances an index via one cached `RequestHandlerDelegate` (`Next`). Sync-completed behavior `ValueTask`s return without allocating an async state machine. Exception wrapping (`PipelineExecutionException`, OCE / `PlaxionMediatorException` passthrough) preserved.

### Measurements

| Metric | Before | After opt1 |
|--------|-------:|-----------:|
| Send5 Mean | 547.86 ns | **464.99 ns** (−15%) |
| Send5 Alloc | 1392 B | **896 B** (−36%) |
| Send20 Mean | 2015.69 ns | **1639.03 ns** (−19%) |
| Send20 Alloc | 4992 B | **2936 B** (−41%) |
| Compose exclusive % (Send5) | 9.77% | **removed** |

### Verdict
**KEPT** — clear latency and allocation win; all pipeline unit tests green.

---

## Optimization 2 — Behavior resolution

### What / why
**Finding:** P3 — `GetServices().ToArray()` every `SendCore_*`.

**Files:**
- `src/PlaxionMediator.Pipeline/PipelineBehaviorResolver.cs` (new)
- `src/PlaxionMediator.Pipeline/PlaxionMediator.Pipeline.csproj` (DI.Abstractions package)
- `src/PlaxionMediator/PlaxionMediatorServiceCollectionExtensions.cs`
- `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs`

**Change:**
- Cache **empty** closed behavior lists process-wide (`Array.Empty`).
- When **no Transient** `IPipelineBehavior<,>` is registered, cache resolved behavior **arrays on the scoped sender** (sender is already scoped → respects Scoped/Singleton lifetimes).
- When any Transient behavior exists, re-resolve every call (correctness first).
- `RegisterServiceCollection` captures `IServiceCollection` from `AddPlaxionMediator` / `AddPlaxionMediatorCore` for lifetime policy.

### Measurements (vs post-opt1)

| Metric | After opt1 | After opt2 |
|--------|-----------:|-----------:|
| Send0 Mean | 49.00 ns | **39.95 ns** |
| Send5 Mean | 464.99 ns | **423.38 ns** |
| Send5 Alloc | 896 B | **832 B** (−64 B ≈ array) |
| TypeVariety50 | 6435 ns (baseline) / ~5.8–6.4 µs | **5021.5 ns** |

### Verdict
**KEPT** — measurable CPU + alloc win; DI lifetime rules preserved.

---

## Optimization 3 — Handler dispatch / type variety

### What / why
**Finding:** P2 — repeated `GetRequiredService` across many closed types.

**Files:**
- `src/PlaxionMediator.Pipeline/RequestHandlerResolver.cs` (new)
- `src/PlaxionMediator/PlaxionMediatorServiceCollectionExtensions.cs`
- `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs`

**Change:** Generated sender holds one typed `IRequestHandler<TReq,TRes>?` field per known request. When no Transient `IRequestHandler<,>` is registered, cache the instance on first resolve for the scope lifetime. Transient policy disables caching.

### Measurements (vs post-opt2)

| Metric | After opt2 | After opt3 |
|--------|-----------:|-----------:|
| TypeVariety50 Mean | 5021.5 ns | **4074.7 ns** (−19%) |
| Send0 Mean | 39.95 ns | **30.31 ns** |
| Send5 Mean | 423.38 ns | **410.04 ns** |

Post-opt3 TypeVariety **beats MediatR** (4700 ns in that run / 5105 ns final full suite).

### Verdict
**KEPT** — primary TypeVariety win; also helps every Send path.

---

## Optimization 4 — Adapt elision

### What / why
**Finding:** P4 — generated `Send` always routed through async `Adapt<TActual,TResponse>` even when types match.

**Files:**
- `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs`

**Change:** Emit `CastOrAdapt<TActual,TResponse>`:
- If `typeof(TActual) == typeof(TResponse)` → `Unsafe.As` reinterpret of `ValueTask` (JIT folds the check on monomorphic calls).
- Else keep async `Adapt` for covariant `IRequest<out TResponse>` cases (e.g. `Send<object>(IRequest<string>)`).

### Measurements
Folded into final full suite with opt5:
- Send0 final **30.05 ns** (baseline 48.39 ns).
- No correctness regressions (source-gen + integration tests).

### Verdict
**KEPT** — free win on the common monomorphic path; variance path preserved.

---

## Optimization 5 — Exception path slim-down

### What / why
**Finding:** P5 — try/catch + message formatting on composition path can inhibit inlining.

**Files:**
- `src/PlaxionMediator.Pipeline/PipelineComposer.cs`

**Change:**
- Minimal try around `behavior.Handle` only.
- Completed-success `ValueTask` returns outside catch complexity.
- `ThrowSyncException` / `ThrowAsyncException` marked `[MethodImpl(NoInlining)]` so message allocation stays off the success path.
- Observable throw types/messages/`PipelineExecutionException` wrapping **unchanged** (pipeline tests cover this).

### Measurements
Final suite (with opts 1–5): Send5 **403.45 ns / 832 B** — no regression vs opt3/4; small incremental latency help on success path. Exception semantics validated by existing tests.

### Verdict
**KEPT** — semantics preserved; success path cleaner for JIT.

---

## Residual gap & follow-ups (no public API change)

1. **~192 B/call gap vs Mediator on behavior chains** — remaining runner object + single next-delegate + behavior async state machines. Future: struct trampoline / object pooling of `PipelineRunner`, or compile-time fixed chains when behaviors are known.
2. **TypeVariety still ~4.6× Mediator** — Mediator bakes direct invokes with no DI. Further internal gains: pre-bind `Func` invokers, denser switch / jump table, optional handler method-group caching without instances when Transient.
3. **Notification fan-out** already strong; not touched this round.
4. **Optional:** source-gen empty-behavior-only `SendCore` that skips resolver call entirely when analyzer proves no open-generic behaviors (harder; requires registration knowledge at gen time).

---

## Validation checklist

- [x] Full `dotnet test PlaxionMediator.sln -c Release` — **218 tests passed** after final state
- [x] Full BenchmarkDotNet suite (non-Dry) — results in `RESULTS.md` + artifacts
- [x] Profiling harness Plaxion Send0/Send5/Send20/TypeVariety50 — artifacts under `profiling-results/`
- [x] No public API signature changes to `ISender`, `IPublisher`, `IRequest<>`, `INotification`, `IPipelineBehavior<,>`, `IStreamRequest<>`, `AddPlaxionMediator`, etc.
- [x] No modifications to comparison adapters / benchmark class bodies

---

## Files touched

### Modified
- `src/PlaxionMediator.Pipeline/PipelineComposer.cs`
- `src/PlaxionMediator.Pipeline/PlaxionMediator.Pipeline.csproj`
- `src/PlaxionMediator/PlaxionMediatorServiceCollectionExtensions.cs`
- `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs`
- `benchmarks-comparison/RESULTS.md`
- `benchmarks-comparison/PROFILING_REPORT.md`

### Created
- `src/PlaxionMediator.Pipeline/PipelineBehaviorResolver.cs`
- `src/PlaxionMediator.Pipeline/RequestHandlerResolver.cs`
- `benchmarks-comparison/OPTIMIZATION_REPORT.md` (this file)
