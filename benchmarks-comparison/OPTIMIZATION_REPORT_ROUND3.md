# Optimization Report — Round 3 (TypeVariety dispatch + pipeline flattening)

**Date:** 2026-08-03  
**Scope:** Architectural changes only in `src/PlaxionMediator*` (source generator dispatch shape + pipeline execution).  
**Public API:** Unchanged (`ISender`, `IPublisher`, `IRequest<>`, `IPipelineBehavior<,>`, `AddPlaxionMediator()`, etc.).  
**Tests:** `dotnet test PlaxionMediator.sln -c Release` — **all green** after every KEPT change.

Round 2 left two residual gaps vs martinothamar **Mediator**:

| Area | Round 2 Plaxion | Mediator | Notes |
|------|----------------:|---------:|-------|
| TypeVariety50 | ~3410 ns / 0 B | ~900 ns / 0 B | ~3.8×; dominated by N-way type-pattern `IsInst` in generated `Send` |
| Send5 | ~396–505 ns / 640 B | ~306–415 ns / 640 B | alloc parity; latency residual is call-chain shape |

This round focuses on **Goal 1 (TypeVariety dispatch redesign)** and **Goal 2 (pipeline path flattening)**. DI/`GetServices`/`CastOrAdapt` micro-opts (R5/R6) were explicitly skipped.

---

## Goal 1 — TypeVariety dispatch redesign

### Context (how Mediator differs)

martinothamar Mediator monomorphizes concrete `Send(TRequest)` and, for object/generic entry, uses a compact static lookup of pre-built handler wrappers (`MessageHandlerDelegate` / wrapper objects) rather than a large type-pattern switch. Plaxion’s public generic surface is `Send<TResponse>(IRequest<TResponse>)`, so concrete monomorphization of the public entry is not available without an API change. The internal shape, however, can still move from an N-way `IsInst` chain to an O(1) Type→id map + jump table.

### Strategy G1-A — Static `Dictionary<Type,int>` + integer jump-table switch — **KEPT** (hybrid)

**Rationale:** Profiling (R2) showed `CastHelpers.IsInstanceOfClass` as a top exclusive frame on TypeVariety (~11–13% of managed exclusive). Replacing `switch (request) { case T1: … case T50: }` with `request.GetType()` → `Dictionary.TryGetValue` → `switch (requestId)` removes the linear-ish type-test chain and gives the JIT a dense integer jump table into monomorphized `SendCore_*`.

**Implementation (`SourceEmitter.EmitSend` / `EmitSender`):**

- Emit static `s_requestTypeMap` (`Dictionary<Type, int>`) when handler count **> 16**.
- Large-N `Send`:
  - `Type requestType = request.GetType();`
  - `s_requestTypeMap.TryGetValue` → `HandlerNotFoundException` on miss (same behavior as before).
  - `switch (requestId)` with `case N: return CastOrAdapt(SendCore_N((TN)(object)request, ct));`
- Small-N (≤ 16): keep classic type-pattern `case TN r:` (faster for the common 1-request app; avoids Dictionary tax on Send0).

**Files:** `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs`, `test/PlaxionMediator.SourceGenerators.Tests/PlaxionMediatorGeneratorTests.cs`.

**Benchmarks (real BDN, non-Dry):**

| Metric | Round 2 baseline | G1-A (always map) | G1-A hybrid (final) |
|--------|-----------------:|------------------:|--------------------:|
| TypeVariety50 Plaxion Mean | 3410 ns | ~2578–2723 ns | **2614 ns** |
| TypeVariety50 Mediator | ~902 ns | ~1130 ns* | **1144 ns*** |
| Allocated | 0 B | 0 B | **0 B** |

\*Mediator absolute numbers vary by run; ratio is the useful comparison.

**vs Round 2 Plaxion:** **3410 → 2614 ns ≈ −23%** (clearly ≥ 2–3%).  
**vs Mediator:** ~3.8× → ~**2.3×** residual.

**Always-map side effect (measured then fixed by hybrid):** forcing the Dictionary path for N=1 raised Send0 ~44→~51 ns. Hybrid threshold restores type-pattern for small assemblies (pipeline bench N=1).

**Profiling (real `dotnet-trace`, Plaxion / TypeVariety50, 5s):**

| Observation | Round 2 post | Round 3 post-G1-A |
|-------------|--------------|-------------------|
| `CastHelpers.IsInstanceOfClass` | Top managed exclusive (~11–13%) | **Gone from top-20 exclusive** |
| New hot lookup | n/a | `Dictionary.FindValue` appears in top exclusive managed frames |
| `PlaxionMediatorSender.Send` | Present | Present (now map + jump table) |
| Ops/sec (harness) | ~256k (R2 post) | ~360k this capture (machine-noise band) |

**Verdict: KEPT** — largest TypeVariety win of the round; hybrid form avoids regressing small-N Send0.

---

### Strategy G1-B — Hoist `HasNoPipelineBehaviors` + inline resolve/Handle in `Send` — **REVERTED**

**Rationale:** TypeVariety never has behaviors; hoisting the global empty-pipeline check into `Send` and inlining handler resolve + `Handle` (skipping `SendCore_*`) should remove one call + one branch per type.

**Implementation (attempted):** Dual switch in generated `Send` — fast path with per-case resolve/Handle, slow path still calling `SendCore_*`.

**Benchmarks:**

| Metric | G1-A alone | G1-A + G1-B |
|--------|-----------:|------------:|
| TypeVariety50 Plaxion | ~2650–2720 ns | **~4780 ns** |

**Verdict: REVERTED** — ~75% regression. Duplicating large switch bodies / bloating `Send` hurt JIT code quality more than the removed hop helped. Restored G1-A-only (then hybrid).

---

## Goal 2 — Flatten pipeline execution path

### Strategy G2-A — `ShallowPipelineRunner` for 1–2 behaviors — **REVERTED**

**Rationale:** Round 2 already removed `ExecuteCore` and pooled `PipelineRunner`. Remaining indirection is the indexed `Next()` trampoline + virtual `IPipelineBehavior.Handle`. Specializing count∈{1,2} with field-staged behaviors (no list index) should cut bookkeeping on shallow pipelines (Send1).

**Implementation (attempted):** `PipelineComposer.ExecuteAsync(handler)` branched to a second pooled `IValueTaskSource` runner with `_b0`/`_b1`/`_stage` instead of `_behaviors[_index]`.

**Benchmarks (same session as G1-A):**

| Method | Round 2-ish prior | With G2-A | Δ |
|--------|------------------:|----------:|--:|
| Send_Plaxion_1Behavior | ~165 ns | ~161 ns | ~−2% (borderline) |
| Send_Plaxion_5Behaviors | ~505 ns | ~485 ns | noise (still generic path) |
| Send_Plaxion_10Behaviors | ~912 ns | ~942 ns | mild worse / noise |

**Verdict: REVERTED** — no reliable ≥2–3% win; extra ~250 LOC of duplicated pool/VTS machinery not justified. Deeper residual vs Mediator is structural: Mediator pre-composes `MessageHandlerDelegate` chains once (next takes message+ct); Plaxion’s public `RequestHandlerDelegate` is `Func<ValueTask<TResponse>>`, so request state must live in a per-call runner unless the **public** behavior API changes (out of scope).

---

### Strategy G2-B — Drop null guards on generated `ExecuteAsync` overload — **REVERTED**

**Rationale:** Generated callers always pass non-null; three `ThrowIfNull` branches on every non-empty pipeline Send.

**Verdict: REVERTED** — no measurable ≥2–3% improvement (session noise dominated). Left public/`Func` overload guards intact.

---

## Final numbers (Round 3 KEPT = G1-A hybrid only)

### TypeVariety50

| Method | Mean | Allocated |
|--------|-----:|----------:|
| Dispatch_Mediator_50Types | 1.144 µs | 0 B |
| **Dispatch_Plaxion_50Types** | **2.614 µs** | **0 B** |
| Dispatch_MediatR_50Types | 7.534 µs | 13200 B |

- Plaxion vs R2: **3410 → 2614 ns (−23%)**  
- Plaxion / Mediator: **~2.3×** (was ~3.8×)

### Pipeline Send (same machine/job; absolute ns vary run-to-run — allocs stable)

| Method | Mean | Allocated |
|--------|-----:|----------:|
| Send_Mediator_0Behaviors | 19.8 ns | 0 B |
| **Send_Plaxion_0Behaviors** | **~50–53 ns** | **0 B** |
| Send_MediatR_0Behaviors | ~76–83 ns | 264 B |
| Send_Mediator_1Behavior | ~94–104 ns | 128 B |
| **Send_Plaxion_1Behavior** | **~150–181 ns** | **128 B** |
| Send_MediatR_1Behavior | ~220–249 ns | 648 B |
| Send_Mediator_5Behaviors | ~413–462 ns | 640 B |
| **Send_Plaxion_5Behaviors** | **~537–539 ns** | **640 B** |
| Send_MediatR_5Behaviors | ~696–756 ns | 1896 B |
| Send_Mediator_10Behaviors | ~843–952 ns | 1280 B |
| **Send_Plaxion_10Behaviors** | **~969–1003 ns** | **1280 B** |
| Send_MediatR_10Behaviors | ~1303–1425 ns | 3456 B |
| Send_Mediator_20Behaviors | ~1921–2029 ns | 2560 B |
| **Send_Plaxion_20Behaviors** | **~2094–2128 ns** | **2560 B** |
| Send_MediatR_20Behaviors | ~2693–2920 ns | 6576 B |

Pipeline **allocation parity with Mediator retained** at every depth. No Goal 2 change kept; latency residual vs Mediator remains (call-chain / pre-composition model).

### Notifications / concurrency

No intentional changes; prior-session full-suite snapshots retained in `RESULTS.md` Round 3 section (alloc parity with Mediator on concurrent Send; notifications still a Plaxion strength area).

---

## Overall summary

| Strategy | Goal | Verdict | Why |
|----------|------|---------|-----|
| G1-A hybrid Type→id map + jump table (N>16) | 1 | **KEPT** | TypeVariety −23%; IsInst off hot path; 0 B retained |
| G1-B hoist empty-pipeline inline Send | 1 | **REVERTED** | TypeVariety +75% regression |
| G2-A ShallowPipelineRunner 1–2 | 2 | **REVERTED** | &lt;2–3% / noisy; complexity |
| G2-B remove ExecuteAsync null guards | 2 | **REVERTED** | no measurable win |

**Residual gap vs Mediator:**

1. **TypeVariety (~2.3×):** Mediator’s wrapper table + monomorphized concrete `Send` still tighter than Dictionary+`SendCore_*`+`CastOrAdapt`. Further gains likely need wrapper objects closer to Mediator (still without public API breaks) or concrete overloads as an opt-in generated surface.
2. **Pipeline latency:** Mediator pre-composes next delegates once (`MessageHandlerDelegate` carries message). Plaxion’s `RequestHandlerDelegate` shape forces a runner/trampoline; true flatten needs either API evolution or a different internal adapter boundary.

**Confirmations:**

- Public API surface unchanged.
- All main-repo tests pass (`dotnet test PlaxionMediator.sln -c Release`).
- Only internal generator emission + docs under `benchmarks-comparison/` changed for KEPT work (`PipelineComposer` unchanged in final tree).

---

## Files touched

| File | Role |
|------|------|
| `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs` | G1-A hybrid dispatch emission (**KEPT**) |
| `test/PlaxionMediator.SourceGenerators.Tests/PlaxionMediatorGeneratorTests.cs` | Assert small-N type-pattern shape |
| `benchmarks-comparison/OPTIMIZATION_REPORT_ROUND3.md` | This report |
| `benchmarks-comparison/RESULTS.md` | Round 3 snapshot appended |
| `benchmarks-comparison/PROFILING_REPORT_ROUND2.md` | Round 3 pointer section appended |

Temporary benchmark logs under `benchmarks-comparison/round3-*.txt` may be deleted; they are not part of the deliverable.
