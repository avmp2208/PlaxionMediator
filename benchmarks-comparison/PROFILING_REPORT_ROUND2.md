# PlaxionMediator Profiling Report — Round 2 (Residual Gap)

> Generated: 2026-08-03  
> Scope: **analysis only** (no `src/PlaxionMediator*` or comparison adapter/benchmark code changes)  
> Baseline: post-optimization state documented in `OPTIMIZATION_REPORT.md` / `RESULTS.md`  
> Prior round: `PROFILING_REPORT.md`  
> Environment: Windows 11, .NET 9, `dotnet-trace` 9.0.661903 (`dotnet-sampled-thread-time` + `gc-verbose`), `dotnet-gcdump` 9.0.661903  
> Machine: `DESKTOP-SRHMIC9`

This report explains the **remaining** gap vs Mediator (martinothamar) after the five KEPT optimizations, using fresh captures and full call-tree walks (not Top-N alone).

---

## Methodology

### What was captured (real artifacts only)

| Framework | Scenario | CPU `.nettrace` | Speedscope JSON | `.gcdump` | `topN` | Call-tree analysis | Captured? |
|-----------|----------|:---------------:|:---------------:|:---------:|:------:|:------------------:|:---------:|
| Plaxion | Send5 | Y | Y | Y | Y | Y | **YES** |
| Plaxion | Send20 | Y | Y | Y | Y | Y | **YES** |
| Plaxion | TypeVariety50 | Y | Y | Y | Y | Y | **YES** |
| Mediator | Send5 | Y | Y | Y | Y | Y | **YES** |
| Mediator | Send20 | Y | Y | Y | Y | Y | **YES** |
| Mediator | TypeVariety50 | Y | Y | Y | Y | Y | **YES** |
| MediatR | Send5 | Y | Y | Y | Y | Y | **YES** |
| MediatR | Send20 | Y | Y | Y | Y | Y | **YES** |
| MediatR | TypeVariety50 | Y | Y | Y | Y | Y | **YES** |

**9 / 9** requested primary combos captured. No Send0/Send10 in this round (not required for residual-gap focus).

### Tooling / settings

- Harness: `benchmarks-comparison/src/Plaxion.BenchMarks.Comparison.Profiling` (Release `net9.0`)
- Driver: `benchmarks-comparison/scripts/run-profiling.ps1`
  - `-Frameworks Plaxion,Mediator,MediatR`
  - `-Scenarios Send5,Send20,TypeVariety50`
  - `-DurationSeconds 6`
  - `-ResultsRoot .../profiling-results/round2`
- Per combo: `dotnet-trace collect --profile dotnet-sampled-thread-time,gc-verbose --format Speedscope`, mid-run `dotnet-gcdump collect`
- Post-process:
  - `dotnet-trace report <nettrace> topN -n 80` → `*_topN.txt`
  - `dotnet-gcdump report <gcdump>` → `*_gcdump_report.txt`
  - `scripts/analyze-calltrees.py` walks **evented** speedscope O/C events on the busiest thread, rebuilds stacks, attributes wall time between events, emits call chains / flame offsets / DI·interface·pipeline frame counts

### Artifact locations (ephemeral / gitignored)

```text
benchmarks-comparison/profiling-results/round2/
  manifest.json
  <Framework>/<Scenario>/
    <Framework>_<Scenario>_cpu.nettrace
    <Framework>_<Scenario>_speedscope.json
    <Framework>_<Scenario>.gcdump
    <Framework>_<Scenario>_topN.txt
    <Framework>_<Scenario>_gcdump_report.txt
    <Framework>_<Scenario>_meta.json
    <Framework>_<Scenario>_stdout.txt
  analysis/calltree-analysis.{json,md}          # Send5 + TypeVariety50
  analysis_send20/calltree-analysis.{json,md}   # Send20
```

### Throughput under profiler (informational only)

Profiler + mid-run gcdump depress absolute ops/s vs bare harness / BDN. Use for **relative** ranking only.

| Scenario | Plaxion ops/s | Mediator ops/s | MediatR ops/s | Plaxion/Mediator |
|----------|--------------:|---------------:|--------------:|-----------------:|
| Send5 | 1,422,560 | 1,816,328 | 1,017,348 | 0.78× |
| Send20 | 354,153 | 372,807 | 275,054 | 0.95× |
| TypeVariety50 | 169,799 | 623,895 | 99,596 | 0.27× |

### Trusted absolute latency / alloc (BenchmarkDotNet, `RESULTS.md`)

| Scenario | Plaxion | Mediator | MediatR | Residual vs Mediator |
|----------|--------:|---------:|--------:|----------------------|
| Send 5 | 403 ns / **832 B** | 329 ns / **640 B** | 805 ns / 1896 B | **+74 ns / +192 B** |
| Send 20 | 1500 ns / **2752 B** | 1316 ns / **2560 B** | 1696 ns / 6576 B | **+184 ns / +192 B** |
| TypeVariety50 | 4121 ns / **0 B** | 894 ns / **0 B** | 5105 ns / 13200 B | **+3227 ns / 0 B** (~4.6×) |

**Critical BDN observation:** the alloc gap vs Mediator is a **constant +192 B/call** at 1/5/10/20 behaviors (320−128, 832−640, 1472−1280, 2752−2560). It does **not** scale with behavior count. That strongly pins residual alloc to **fixed per-Send objects**, not per-behavior async state machines (those track Mediator ~1:1 at ~128 B/behavior).

### Current implementation under test (source, not modified)

- `PipelineComposer.ExecuteCore` allocates `new PipelineRunner<,>(...)` then `runner.Next()` when `behaviors.Count > 0`
- `PipelineRunner` is a **`private sealed class`** holding request, behaviors list, `Func<,>` handler, CT, cached `RequestHandlerDelegate` (`_next ??= Next`), and index
- Generated `SendCore_*` calls `PipelineBehaviorResolver.GetBehaviors` + `PipelineComposer.ExecuteAsync(..., handler.Handle, ...)` (method-group → `Func`)
- Scope-local handler fields + behavior array cache already active when no Transient registrations

### Caveats

- Unfiltered exclusive % is dominated by idle waits (`WaitHandle` ~49%, `Monitor.Wait` ~25%). Prefer frames under `PlaxionMediatorSender.Send` / inclusive carrier stacks.
- Speedscope from this `dotnet-trace` build is **evented** (open/close), not sampled arrays; call-tree script reconstructs stacks from O/C events.
- Mid-run `.gcdump` shows **retained** heap; short-lived per-call objects are often already collected. BDN MemoryDiagnoser remains authoritative for alloc/op.
- Evented self-time on tiny helpers often rounds to ~0% of wall clock; exclusive % from `topN` is cited for those.

---

## Investigation findings (8 points)

### 1. Complete CPU call trees (Plaxion vs Mediator)

#### Plaxion / Send5 — representative dispatch chain (evented speedscope, highest-weight interesting stack)

Framework-relevant frames (harness prefix omitted in commentary):

1. `ScenarioRunner` await loop / `<>c__DisplayClass8_0 <<CreatePlaxionSend>b__0`  
2. **`PlaxionMediatorSender.Send`**
3. **`PipelineRunner.Next`**
4. **`PlaxionBehavior01.Handle` → `<Handle>d__1.MoveNext`**
5. **`PipelineRunner.Next`**
6. **`PlaxionBehavior02.Handle` → MoveNext**
7. **`PipelineRunner.Next`**
8. **`PlaxionBehavior03.Handle` → MoveNext**
9. **`PipelineRunner.Next`**
10. **`PlaxionBehavior04.Handle` → MoveNext**
11. … (behavior 05 + terminal handler / `SimulatedValidationWork.Do`)

Entry frames also observed on stacks with non-zero total time:

- `PipelineComposer.ExecuteAsync` — **4.818%** total (busiest thread attribution)
- `PipelineComposer.ExecuteCore` — **4.818%** total (same band; called from ExecuteAsync)
- `PlaxionMediatorSender.SendCore_0` — **5.196%** total

`topN` exclusive (same capture):

| Frame | Inclusive | Exclusive |
|-------|----------:|----------:|
| `PlaxionMediatorSender.Send` | 23.73% | **3.36%** |
| `PipelineRunner.Next` (`__Canon].Next`) | 20.18% | **1.82%** |
| Behavior `MoveNext` (×5 distinct) | 3.7–18.4% | **~0.88–0.93% each** |
| `PipelineComposer.ExecuteCore` | 1.18% | **0.09%** |
| `SendCore_0` | 1.29% | **0.09%** |
| `PipelineComposer.ExecuteAsync` | 1.19% | **0.01%** |
| `ServiceProvider.GetService` | 0.04% | **0.01%** (startup / noise) |

#### Mediator / Send5 — shape

No `PipelineComposer` / `PipelineRunner` / `ExecuteCore` frames.

`topN` highlights:

| Frame | Inclusive | Exclusive |
|-------|----------:|----------:|
| Behavior `MoveNext` (×5) | 4.7–23.7% | **~1.14–1.18% each** |
| `Mediator.Send` | 23.79% | ~0% (carrier) |
| Generated `Handle(IRequest, CT)` | 23.72% | ~0% |
| Behavior `Handle(..., MessageHandlerDelegate, ...)` | 9.41% | ~0% |
| `SimulatedValidationWork.Do` | 17.87% | ~0% |

Call tree is **Send → generated handler wrapper → fixed behavior.Handle chain → terminal handler**, without an index trampoline object.

#### Plaxion / Send20

Same structure as Send5 scaled in depth: `Next` + behavior `Handle`/`MoveNext` repeated; `ExecuteCore`/`ExecuteAsync` still present as thin entry. Harness gap vs Mediator shrinks (0.95×) because behavior body work dominates.

#### TypeVariety50

**Plaxion `topN`:** many distinct `SendCore_N` exclusive leaves (e.g. `SendCore_47` 0.14%, `SendCore_31` 0.07%, `SendCore_7` 0.07%, …), plus `PlaxionMediatorSender.Send` 1.6% incl / 0.09% excl, `CastHelpers.IsInstanceOfClass` 0.94% excl.  
**Mediator `topN`:** `Handle(IRequest, CT)` 0.67% excl, `IsInstanceOfInterface` 0.62% excl, `Mediator.Send` 2.41% incl / 0.38% excl, `GetValueRefOrNullRefCore` 0.67% excl — compact lookup/dispatch, not 50 separate `SendCore_*` methods.

Speedscope files for TypeVariety are tiny (sync-heavy path → few O/C transitions); **`topN` is the stronger TypeVariety CPU evidence**.

---

### 2. Flame-graph shape (depth / width)

#### Plaxion Send5 (under `PlaxionMediatorSender.Send`)

Flame is a **deep, repeating sandwich**:

| Offset from sender | Dominant width |
|--------------------|----------------|
| +0 | `PlaxionMediatorSender.Send` (~98% of sender-rooted time) |
| +1 | `PipelineRunner.Next` (~83% total process attribution includes this carrier) |
| +2.. | `PlaxionBehavior0k.Handle` + `<Handle>d__1.MoveNext` alternating with **another `Next`** |
| Leaf band | `SimulatedValidationWork.Do` (~57% total on busy thread — shared workload) |

Shape summary: **wide recursive trampoline spine** (`Next` appears at many depths) rather than a single pre-expanded call ladder.

Interesting-frame depth on attributed intervals: **mean 15.1, median 15, p90 23, max 29**.  
Full stack depth (incl. runtime): **mean 34.4, median 35, p90 44**.

#### Mediator Send5

Flame under `Mediator.Send` is a **monotone descent** through generated `Handle` → behavior `MoveNext` frames **without** a shared `Next` spine frame. Behavior `MoveNext` exclusive is slightly higher per frame (~1.15% vs Plaxion ~0.9%), but there is no extra composer/runner entry band.

#### Send20

Same shapes, deeper. Plaxion `Next` remains the structural spine; Mediator remains a straight generated chain. Relative framework overhead shrinks vs validation work.

#### TypeVariety50

Plaxion flame is **wide at the switch/SendCore layer** (many sibling `SendCore_*` leaves). Mediator flame is **narrow**: one `Send` + lookup/`Handle` path.

---

### 3. Remaining `IServiceProvider` / `GetService` / `GetServices` in the hot path

| Combo | DI-related frames per attributed interval (mean / max) | DI frames in call-tree list |
|-------|--------------------------------------------------------|----------------------------|
| Plaxion Send5 | **0.0 / 0** | **none** on steady-state stacks |
| Plaxion Send20 | **0.0 / 0** | **none** |
| Plaxion TypeVariety50 | **0** on steady samples | `GetRequiredService` only at **0.06% incl / 0.01% excl** in topN (warmup / first resolve) |
| Mediator Send5/20 | 0 in hot path | setup-only CallSite frames |
| MediatR Send5 | low but present | `dynamicClass.ResolveService` ~0.28% excl band (prior round pattern; still visible vs Plaxion) |

**Conclusion:** Round-1 P2/P3 caching worked. **DI resolution is no longer a meaningful residual hot-path cost** for the profiled non-Transient benchmark registrations. Further DI micro-opts are **not** justified by this round’s evidence.

---

### 4. Remaining virtual / interface call boundaries

#### Counts from call-tree (frames matching Handle / delegate / interface patterns per interval)

| Combo | Interface-ish frames mean | p90 | max |
|-------|--------------------------:|----:|----:|
| Plaxion Send5 | **2.27** | 5 | 7 |
| Plaxion Send20 | higher (more behaviors on stack when sampled mid-chain) | — | — |
| Mediator Send5 | similar order (behavior `Handle` + generated wrappers) | — | — |

These are **stack occupancy** counts (how many such frames are live when sampled), not a perfect “boundaries per request” counter. Structural per-request boundaries from source + trees:

#### Plaxion multi-behavior Send (structural)

| # | Boundary | Notes |
|---|----------|-------|
| 1 | `ISender.Send` → generated `PlaxionMediatorSender.Send` | interface entry |
| 2 | `Send` → `SendCore_*` | direct after type switch |
| 3 | `SendCore` → `PipelineComposer.ExecuteAsync` | static call (still a frame) |
| 4 | `ExecuteAsync` → `ExecuteCore` | static; AggressiveInlining but **still appears** |
| 5 | `ExecuteCore` → `PipelineRunner.Next` | instance call on **new** runner |
| 6.. | each `IPipelineBehavior.Handle` | interface dispatch × N |
| 6b.. | each `RequestHandlerDelegate` (`Next`) invoke | delegate invoke × N |
| last | `Func` → `IRequestHandler.Handle` | delegate + interface |

#### Mediator multi-behavior Send (structural)

| # | Boundary | Notes |
|---|----------|-------|
| 1 | `Mediator.Send` | |
| 2 | generated message handler `Handle` | |
| 3.. | behavior `Handle` × N | interface/virtual as emitted |
| last | terminal handler | |

**Plaxion pays extra fixed boundaries** for `ExecuteAsync` / `ExecuteCore` / `PipelineRunner.Next` / `RequestHandlerDelegate` that Mediator folds into a source-generated chain. Per-behavior interface `Handle` cost is shared by both libraries.

---

### 5. Methods that do **not** appear inlined by the JIT

Cross-check of `[MethodImpl(AggressiveInlining)]` in current source vs frames that still appear:

| Method | Attribute in source | Appears as distinct frame? | Evidence |
|--------|---------------------|----------------------------|----------|
| `PipelineComposer.ExecuteCore` | **AggressiveInlining** | **YES** | Send5 total **4.818%**, excl **0.09%**; listed in non-inlined candidates |
| `PipelineBehaviorResolver.GetBehaviors` | AggressiveInlining | **NO** (hot path) | Not in Send5/Send20 interesting frames after cache hit |
| `RequestHandlerResolver.CanCacheHandlersPerScope` | AggressiveInlining | **NO** (hot path) | Constructor-time only |
| Generated `CastOrAdapt` | AggressiveInlining | **NO** | Not in Send5 hot frames (type-equal path elided) |

Also appear without AggressiveInlining (expected, but still call overhead):

| Method | Hit/total % (Plaxion Send5) | Notes |
|--------|----------------------------:|-------|
| `PipelineRunner.Next` | **83.4%** total / **1.82%** excl | Hot trampoline; too large/complex to inline into all callers |
| `PipelineComposer.ExecuteAsync` | **4.82%** total | Thin public entry; not marked AggressiveInlining |
| `SendCore_0` | **5.20%** total / **0.09%** excl | Per-type private method; visible |

**Takeaway:** `ExecuteCore`’s AggressiveInlining is **not fully effective** in this profile (still a separate frame). `Next` cannot reasonably inline through async behavior boundaries. `CastOrAdapt` / `GetBehaviors` inlining goals from round 1 **are** met on the hot path.

---

### 6. Delegates, closures, async state machines (gcdump + BDN + trees)

#### BDN (authoritative per-call)

- Residual **+192 B/call** vs Mediator at every behavior depth ≥ 1 (constant).
- Both libraries still pay ~128 B × behavior count for async behavior state machines (shared structural cost).

#### Source-level fixed allocators on Plaxion path (when `behaviors.Count > 0`)

```csharp
// ExecuteCore
PipelineRunner<TRequest, TResponse> runner = new(...);  // class → heap
return runner.Next();
// inside Next:
RequestHandlerDelegate<TResponse> next = _next ??= Next; // one delegate
// from generated SendCore:
PipelineComposer.ExecuteAsync(..., handler.Handle, ...); // method-group → Func<...>
```

Likely composition of the **192 B** constant (x64, approximate):

| Object | Role | Rough size |
|--------|------|------------:|
| `PipelineRunner<,>` instance | trampoline state | ~48–72 B |
| `RequestHandlerDelegate<TResponse>` | cached `Next` | ~64–88 B |
| `Func<TRequest,CT,ValueTask<TResponse>>` from `handler.Handle` | terminal invoker | ~64 B |
| **Sum** | | **~176–224 B ≈ 192 B** |

#### gcdump mid-run (retained; short-lived often missing)

Plaxion Send5 retained heap ~374 KB total; **no live `PipelineRunner` instances** at snapshot (consistent with gen0 collection of per-call runners). Retained items include sender, DI tables, behavior arrays — not the per-call spike.

MediatR gcdump still shows retained `RequestHandlerDelegate<string>` and wrapper caches; trees still show `Enumerable.Aggregate` / `<>c__DisplayClass` wrapper patterns on MediatR Send paths.

#### Trees: async state machines

Plaxion and Mediator both show per-behavior `<Handle>d__1.MoveNext` frames — **expected** for async behaviors in the harness. Plaxion no longer shows O(n) composition DisplayClasses from the old `Compose` lambda fold (addressed in round 1).

**No evidence** of a remaining O(n) Plaxion composition closure alloc. Residual is **O(1) per Send** (runner + delegates) plus shared O(n) behavior SMs.

---

### 7. `PipelineRunner` / `ExecuteCore` overhead — can it be allocation-free / stack-based?

#### Measured overhead (Plaxion Send5)

| Signal | Value |
|--------|------:|
| `Next` exclusive (`topN`) | **1.82%** |
| `Next` total (call-tree) | **83.4%** (inclusive carrier of whole pipeline) |
| `ExecuteCore` exclusive | **0.09%** |
| `ExecuteCore` total | **4.82%** |
| `ExecuteAsync` exclusive | **0.01%** |
| Alloc attributable (BDN constant gap) | **~192 B/call** with runner + delegates |

#### Implementation facts

- `PipelineRunner<,>` is a **heap class**, not a struct.
- It must outlive the first `await` inside any async behavior that calls `next()` after await points → **a pure stack struct is incorrect** unless the entire chain completes synchronously **or** the state is carefully lifted only when pending.
- Current design already avoids O(n) lambda allocs; single runner + one `Next` delegate is the remaining O(1) cost.

#### Evidence-based assessment

| Approach | Alloc-free? | Fits evidence? | Notes |
|----------|:-----------:|----------------|-------|
| Struct runner always on stack | Only if never pending | **Unsafe** for async behaviors in this harness (they are async) | Would corrupt state after await |
| Struct runner + box only when pending | Often 0 B on sync path | Partially | Harness behaviors are async → still boxes often here |
| Object pool of `PipelineRunner` | Amortized ~0 B | Matches 192 B target | Needs reset/`_index`/`_next` care; pool churn under concurrency |
| Cache `Func` for `handler.Handle` on sender field | Removes ~64 B | Strong | Low risk with scope-cached handler instance |
| Source-gen fixed chain (Mediator-style) | Can remove runner entirely | Highest ceiling | Needs behavior registration knowledge at gen time; complexity high |
| Keep as-is | No | Already competitive on Send20 latency | +192 B remains |

**Verdict on “stack-based enumerator”:** not supported as a blanket fix by the async call trees. **Pooling or eliminating the runner via codegen**, plus **pre-binding the handler `Func`**, are the evidence-backed alloc paths.

---

### 8. Full dispatch chain depth comparison (frame-by-frame)

#### Plaxion Send5 — logical chain (source + profile)

```text
ISender.Send / PlaxionMediatorSender.Send          [switch on request type]
  └─ CastOrAdapt<TActual,TResponse>                 [INLINED / absent in profile]
       └─ SendCore_0
            ├─ (cached) IRequestHandler field
            ├─ PipelineBehaviorResolver.GetBehaviors [cache hit; no DI frames]
            └─ PipelineComposer.ExecuteAsync         [VISIBLE frame]
                 └─ ExecuteCore                      [VISIBLE despite AggressiveInlining]
                      └─ new PipelineRunner + Next   [VISIBLE; ~1.8% excl]
                           ├─ Behavior01.Handle      [interface]
                           │    └─ <Handle>d__1.MoveNext
                           │         └─ next() → Next
                           ├─ Behavior02.Handle → MoveNext → Next
                           ├─ Behavior03.Handle → MoveNext → Next
                           ├─ Behavior04.Handle → MoveNext → Next
                           ├─ Behavior05.Handle → MoveNext → Next
                           └─ Func → handler.Handle  [terminal]
```

Interesting depth median **15**; pipeline frames/interval mean **2.35** (multiple `Next`/Execute* live when sampled).

#### Mediator Send5 — logical chain

```text
Mediator.Send
  └─ generated Handle(IRequest, CT)
       ├─ Behavior01.Handle → MoveNext
       ├─ Behavior02.Handle → MoveNext
       ├─ …
       └─ terminal handler.Handle
```

No `ExecuteAsync` / `ExecuteCore` / `PipelineRunner` / separate `RequestHandlerDelegate` spine in the profile.

#### Depth summary

| Metric | Plaxion Send5 | Mediator Send5 |
|--------|--------------:|---------------:|
| Interesting-frame depth (median) | **15** | lower (no runner/execute layers; same behavior MoveNext bulk) |
| Extra fixed framework layers vs Mediator | **ExecuteAsync + ExecuteCore + Runner.Next + delegate Next** | baseline |
| Per-behavior layers | Handle + MoveNext (+ Next spine) | Handle + MoveNext |
| TypeVariety dispatch | `Send` + **50× `SendCore_*`** methods | `Send` + compact generated lookup/`Handle` |

---

## Ranked optimization opportunities

Ordered by expected impact on the **residual gap** (Mediator-first), grounded only in round-2 evidence + BDN constants.

### R1 — Remove the constant ~192 B/call pipeline entry alloc (runner + delegates)

- **(a) Evidence:** BDN constant **+192 B** vs Mediator at 1/5/10/20 behaviors; source allocates `new PipelineRunner`, `_next ??= Next`, and `handler.Handle` method-group `Func` each Send with behaviors; `Next` **1.82%** excl; no O(n) composition left.
- **(b) Expected benefit:** **~192 B/call** on all behavior depths ≥ 1; latency ballpark **~20–60 ns** on Send5 if alloc/GC pressure drops (part of the 74 ns mean gap; not all of it). Send20 already near Mediator latency (1.14×) — alloc win still valuable under load.
- **(c) Complexity:** **Medium** — pool runner and/or cache `Func`+`RequestHandlerDelegate` on sender; struct-only path is High complexity due to async.
- **(d) Risks:** DI lifetime OK if pooling only runner state; must reset `_index` and not leak request refs across rents; concurrency needs concurrent bag/pool; correctness of exception wrapping must stay identical.
- **(e) Recommendation:** **Worth implementing** (start with caching `handler.Handle` `Func` on the typed sender field + pooling `PipelineRunner`, measure BDN Send1/5/20 alloc).

### R2 — TypeVariety dispatch: close the ~4.6× gap vs Mediator

- **(a) Evidence:** BDN 4121 vs 894 ns; harness 170k vs 624k ops/s under profiler; Plaxion topN shows **many `SendCore_*` exclusive frames** + `IsInstanceOfClass`; Mediator shows compact `Send`/`Handle`/`GetValueRefOrNullRefCore`; both **0 B**.
- **(b) Expected benefit:** Large latency-only win. Even a **20–40%** TypeVariety cut would be hundreds of ns; matching Mediator fully likely needs Mediator-like baked invoke tables (**up to ~3–4×** theoretical, high effort).
- **(c) Complexity:** **High** for Mediator-parity codegen; **Medium** for incremental denser switch / shared invoker stubs / eliminating per-core prolog.
- **(d) Risks:** Generator complexity, debuggability, binary size; must preserve covariance `CastOrAdapt` slow path; no public API change required if kept internal.
- **(e) Recommendation:** **Worth implementing only if** TypeVariety/multi-type apps are a product priority. Not the best ROI if the product focus is pipeline-heavy APIs (where Plaxion is already near Mediator).

### R3 — Collapse fixed call layers (`ExecuteAsync` / `ExecuteCore` / visible `Next` entry)

- **(a) Evidence:** `ExecuteCore` still a frame despite AggressiveInlining (4.82% total / 0.09% excl); `ExecuteAsync` 4.82% total; structural depth exceeds Mediator by several fixed frames.
- **(b) Expected benefit:** Small–moderate CPU (**tens of ns** scale on Send5), secondary to R1. Helps TypeVariety empty-behavior path slightly when behaviors resolve empty and skip runner (already fast path to `handler.Handle`).
- **(c) Complexity:** **Low–Medium** — emit `ExecuteCore` body into generated `SendCore`, mark/eliminate `ExecuteAsync` hop, aggressive JIT-friendly shape.
- **(d) Risks:** Low correctness risk if semantics preserved; maintainability if logic duplicated into generator.
- **(e) Recommendation:** **Worth implementing** as a low-risk follow-on after or with R1, but **do not expect** it alone to erase the Mediator gap.

### R4 — Source-generate fixed behavior chains when registrations are known (Mediator-style)

- **(a) Evidence:** Mediator flame has **no trampoline spine**; Plaxion `Next` is 83% inclusive carrier / 1.82% excl; depth and delegate invokes are the structural difference on multi-behavior Send.
- **(b) Expected benefit:** Can remove runner **and** reduce virtual/delegate boundaries; potential to approach Mediator Send5 (329 ns / 640 B). Upside **medium–high** on deep pipelines; overlaps R1 alloc win.
- **(c) Complexity:** **High** — needs compile-time knowledge of closed behavior order/lifetimes; DI open-generics and runtime registration order are hard; fallback path still required.
- **(d) Risks:** Wrong order/lifetime = correctness bugs; large generator surface; harder to reason about vs runtime `IPipelineBehavior` enumeration.
- **(e) Recommendation:** **Worth implementing only if** R1’s residual is still insufficient **and** the project accepts generator/DI-policy complexity. Not the first move.

### R5 — Further DI / behavior-array work

- **(a) Evidence:** DI frames/interval **mean 0** on Plaxion Send5/Send20; `GetServices`/`ICollectionToArray` absent from hot topN.
- **(b) Expected benefit:** **~0** on current benchmarks.
- **(c) Complexity:** n/a  
- **(d) Risks:** Easy to break Transient lifetime correctness for negligible gain.  
- **(e) Recommendation:** **Not worth implementing** based on round-2 evidence.

### R6 — Chase `CastOrAdapt` / empty-path micro-opts

- **(a) Evidence:** `CastOrAdapt` already absent from hot frames; TypeVariety/Send0 already 0 B; Send0 30 ns vs Mediator 17 ns is a separate small gap not instrumented as a round-2 primary residual with multi-behavior focus.
- **(b) Expected benefit:** Minimal on Send5/20/TypeVariety residual story.  
- **(e) Recommendation:** **Not worth implementing** as a residual-gap project now.

---

## Summary table (residual story)

| Residual | Magnitude | Primary evidence | Best next step |
|----------|-----------|------------------|----------------|
| Alloc on behavior Send | **+192 B/call** constant | BDN 1/5/10/20; class `PipelineRunner` + delegates | **R1** |
| Latency Send5 | +74 ns (1.23×) | BDN; `Next` 1.82% excl; extra frames | R1 + R3 |
| Latency Send20 | +184 ns (1.14×) | BDN; behavior-dominated | R1 (alloc), optional R4 |
| TypeVariety | **~4.6×** slower, 0 B | BDN; many `SendCore_*` frames | **R2** if product-priority |
| DI in hot path | **~gone** | 0 DI frames/interval | no work |

---

## Bottom line

Round 1 killed the big P1/P3 problems (compose lambdas, per-call `GetServices().ToArray`). Round 2 shows the **honest remaining story**:

1. **Multi-behavior Send** is already in Mediator’s neighborhood on latency (especially Send20). The clean, evidence-backed leftover is a **fixed ~192 B/call** entry cost from the **class-based `PipelineRunner` + `Next` delegate + `handler.Handle` Func**, not DI and not O(n) composition.
2. **TypeVariety** is still the large **latency-only** gap (~4.6×), driven by dispatch shape (many `SendCore_*` / type tests vs Mediator’s compact generated path), not allocations.
3. **Do not** spend effort on further DI caching for this benchmark shape — profiles show **zero** hot-path DI frames.

No optimizations were implemented in this round (analysis/report only).

---

## Round 2 Post-Optimization (2026-08-03)

> Appended after implementing R1 then R3 then R2 from this report. Analysis sections above are unchanged.  
> Full write-up: `OPTIMIZATION_REPORT_ROUND2.md`. Fresh BDN numbers: `RESULTS.md` (Round 2 section).  
> Real profiling artifacts (gitignored): `profiling-results/round2-r1/`, `round2-r3/`, `round2-r2/`.

### What was KEPT

| ID | Change | Validation |
|----|--------|------------|
| **R1 KEPT** | TLS+ConcurrentBag `PipelineRunner`; bind `Next` once; async complete via `IValueTaskSource`; generated code passes `IRequestHandler` (no method-group `Func`) | BDN: constant **-192 B/call** at 1/5/10/20; alloc **parity with Mediator**. Trace topN: `Next` exclusive ~1.8% to ~0.03%. Tests green. |
| **R3 KEPT** | Removed `ExecuteCore`; single inlined `ExecuteAsync` to `Rent().Run()` entry | BDN Send5 ~403 to ~393 ns (~2.6%) vs R1-only; `ExecuteCore` frame gone. Tests green. |
| **R2 KEPT** | `HasNoPipelineBehaviors()` global fast path + `AggressiveInlining` on `SendCore_*` | BDN TypeVariety50 **4121 to ~3410 ns** (~17%); profiler ops/s 170k to 244k. Tests green. Full Mediator-parity dispatch tables **not** attempted. |

R4 fixed chains **NOT ATTEMPTED** (R1 already closed alloc residual). R5/R6 **not touched**.

### After-state BDN (authoritative latency/alloc)

| Scenario | Plaxion (round-1) | Plaxion (round-2 final) | Mediator (round-2 final) |
|----------|------------------:|------------------------:|-------------------------:|
| Send5 | 403 ns / 832 B | **396 ns / 640 B** | 306 ns / 640 B |
| Send20 | 1500 ns / 2752 B | **1549 ns* / 2560 B** | 1298 ns / 2560 B |
| TypeVariety50 | 4121 ns / 0 B | **3410 ns / 0 B** | 902 ns / 0 B |

\*Send20 full-suite mean had high StdDev (71 ns); isolated R1/R3 runs were ~1484-1500 ns. **Allocated** is stable and matches Mediator.

### After-state profiling throughput (5s, Plaxion only, no mid-run gcdump)

| Capture | Scenario | Ops/s |
|---------|----------|------:|
| `round2-r1` | Send5 | 2,044,482 |
| `round2-r1` | Send20 | 516,861 |
| `round2-r3` | Send5 | 2,068,111 |
| `round2-r3` | Send20 | 515,933 |
| `round2-r2` | TypeVariety50 | 243,837 |

Compare to round-2 analysis table (heavier profile + gcdump): Send5 1.42M, Send20 354k, TypeVariety50 170k — directionally improved; absolute ops/s not comparable 1:1 across profiler configs.

### Residual after round 2

1. **Pipeline latency only** vs Mediator (~+90 ns Send5) with **identical allocations** — index trampoline / exception-wrap path vs Mediator flatter generated chain. Primary remaining lever: R4 fixed chains (high complexity).
2. **TypeVariety ~3.8x** (was ~4.6x) — still multi-type switch/`SendCore_*` shape; needs larger codegen investment for Mediator parity.
3. **Notifications** remain a Plaxion strength.

### Constraint checks

- Public API unchanged.
- Full `dotnet test PlaxionMediator.sln -c Release` passed after every KEPT step.
- Comparison adapters / benchmark class sources not modified.
