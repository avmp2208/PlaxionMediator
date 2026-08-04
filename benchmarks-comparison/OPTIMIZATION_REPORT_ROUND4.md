# Optimization Report — Round 4 (Investigation-first: why Mediator remains faster)

**Date:** 2026-08-03  
**Scope:** Evidence-first architectural investigation of residual latency vs martinothamar **Mediator**, with one measured internal experiment.  
**Public API:** Unchanged (`ISender`, `IPublisher`, `IRequest<>`, `INotification`, `IPipelineBehavior<,>`, `RequestHandlerDelegate<>`, `AddPlaxionMediator()`, adapters, benchmark classes).  
**Tests:** `dotnet test PlaxionMediator.sln -c Release` — **all green** after the only attempted change and after full revert.  
**Code kept:** **None.** Round 4 leaves `src/PlaxionMediator*` identical to the Round 3 end state.

---

## Executive conclusion

The remaining gap vs Mediator is **architectural**, not a missing micro-optimization under the current public contracts.

| Gap | Approx. residual (this session BDN) | Root cause (evidence) |
|-----|--------------------------------------|------------------------|
| **Pipeline Send latency** (alloc parity already) | Send5 ~513 vs ~350 ns; Send20 ~2103 vs ~1501 ns | Mediator **pre-composes** a `MessageHandlerDelegate` chain **once at Init**; Plaxion’s public `RequestHandlerDelegate<TResponse>` is **parameterless**, so request/CT must live in a **per-call** pooled `PipelineRunner` trampoline |
| **TypeVariety50 latency** (0 B both) | ~2142 vs ~967 ns (~2.2×) | Mediator generic path is `FrozenDictionary<Type, wrapper>` → monomorphic `IRequestHandlerBase<TResponse>.Handle` on a **pre-built wrapper**; Plaxion is `Dictionary → id switch → SendCore_* → CastOrAdapt` with more branches/indirections |

**Closing either gap further without a public API change is not justified by evidence.** A `FrozenDictionary` Type→id swap was tried and **REVERTED** (no ≥2–3% TypeVariety win).

---

# Phase 1 — Investigation findings (the WHY)

## 1. What Mediator’s generated code actually looks like

Inspected generated source from `Comparison.MediatorAdapter` after Release build with `EmitCompilerGeneratedFiles` (`Mediator.SourceGenerator` → `Mediator.g.cs`).

### 1.1 Delegate shape (public Mediator API)

Mediator’s next-delegate **carries the message and cancellation token**:

```csharp
// Mediator.Abstractions
public delegate ValueTask<TResponse> MessageHandlerDelegate<TMessage, TResponse>(
    TMessage message,
    CancellationToken cancellationToken);
```

Behaviors are invoked as:

```csharp
behavior.Handle(message, next /* MessageHandlerDelegate */, cancellationToken);
// next is invoked as: next(message, cancellationToken)
```

### 1.2 Per-request wrapper + compose-once `Init`

For each known request type the generator emits a **sealed wrapper** (e.g. `RequestHandlerWrapper_For_…_MediatorPipelineRequest`) held on a singleton `ContainerMetadata`:

- Resolved once from DI at container init / first use.
- `Init()` builds a nested chain of `MessageHandlerDelegate` closures **once**:

```csharp
// Conceptual shape of generated Init (pipeline with N behaviors):
// handlerN = concreteHandler.Handle;                    // method group
// handlerN-1 = (msg, ct) => behaviorN-1.Handle(msg, handlerN, ct);
// ...
// handler0 = (msg, ct) => behavior0.Handle(msg, handler1, ct);
// _rootHandler = handler0;
```

- Hot path `Handle` is essentially: cast request → `_rootHandler(request, ct)`.
- **No per-call composition**, no per-call runner object for the chain structure.
- Concurrent Sends are safe because **request + CT flow as parameters** through the pre-built delegates.

### 1.3 Dispatch surfaces

1. **Concrete monomorphized** `Send(ConcreteRequest request)` overloads on the generated `Mediator` class → direct wrapper call (fastest when the compile-time type is concrete **and** the call site binds that overload).
2. **Generic** `Send<TResponse>(IRequest<TResponse> request)` (what benchmarks use via `Mediator.IMediator`):
   - `GetRequestHandler(request)` → `FrozenDictionary<Type, object>` of wrappers
   - `isinst IRequestHandlerBase<TResponse>` + `Unsafe.As`
   - `handler.Handle(request, ct)` returning `ValueTask<TResponse>` **directly** (no separate CastOrAdapt helper)

### 1.4 Lifetime model

Mediator itself is typically **Singleton**; wrappers and composed chains are long-lived. Transient pipeline behaviors are effectively resolved at composition time (known tradeoff). Plaxion remains **Scoped** and re-resolves Transient behaviors per call when needed — a correctness property that blocks blind “compose once forever” caching.

---

## 2. Diff: Plaxion generated path vs Mediator

### 2.1 Plaxion public next-delegate (cannot change in this round)

```csharp
// src/PlaxionMediator.Abstractions/RequestHandlerDelegate.cs
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();
```

```csharp
// IPipelineBehavior.Handle
ValueTask<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,  // no message parameter
    CancellationToken cancellationToken);
```

**Implication:** `next()` has nowhere to pass the request. Any pre-composed chain must either:

1. Capture `request` in a **per-call closure** (allocates), or  
2. Read request from **mutable per-invocation state** (today’s pooled `PipelineRunner`).

Mediator’s design avoids both on the hot path.

### 2.2 Plaxion generated `Send` (large-N, comparison adapter has **51** request handlers)

`Comparison.PlaxionAdapter` registers pipeline + 50 variety handlers in **one** assembly → always takes the Round 3 hybrid **Dictionary + jump-table** path (threshold 16):

```text
Send<TResponse>(IRequest<TResponse>):
  null check
  request.GetType()
  s_requestTypeMap.TryGetValue → requestId
  switch (requestId):
    case N: return CastOrAdapt<TActual,TResponse>(SendCore_N((TN)(object)request, ct))
```

```text
SendCore_N(TN request, CT ct):
  resolve/cache IRequestHandler<TN,TActual>
  if HasNoPipelineBehaviors() → handler.Handle
  else GetBehaviors → if empty → Handle else PipelineComposer.ExecuteAsync(handler)
```

```text
PipelineComposer (behaviors > 0):
  Rent pooled PipelineRunner (TLS / ConcurrentBag)
  Runner.Run → Next trampoline per behavior
  IValueTaskSource completion for incomplete async
```

### 2.3 Concrete extra costs on Plaxion (not vague)

| Cost | Plaxion | Mediator (generic Send) |
|------|---------|-------------------------|
| Type lookup | `Dictionary<Type,int>` | `FrozenDictionary<Type, object wrapper>` |
| After lookup | integer switch + cast + **call SendCore** | interface call on **ready wrapper** |
| Response adapt | `CastOrAdapt<TActual,TResponse>` (typeof fold + `Unsafe.As` when equal) | often unnecessary — wrapper already returns `ValueTask<TResponse>` |
| Empty pipeline | still `SendCore` prolog (null handler field, HasNoPipeline check) | wrapper → `_rootHandler` / handler method group |
| Non-empty pipeline | **compose structure per call** via runner index trampoline | **pre-composed** nested delegates |
| Next shape | `() => …` bound once on runner | `(msg, ct) => …` closed over behaviors once at Init |
| DI lifetime | Scoped sender; Transient-safe behavior resolve | Singleton mediator; compose-once |

### 2.4 Benchmark call-site note (fairness)

Pipeline and TypeVariety benches call:

- Plaxion: `ISender.Send(...)` → **always** generic generated `Send`
- Mediator: `Mediator.IMediator.Send(...)` → **generic** `Send<TResponse>(IRequest<TResponse>)` (not the concrete monomorphized overloads on the concrete class)

So the comparison is **generic-to-generic**. Mediator still wins because of wrapper + compose-once, not only because of concrete overloads.

---

## 3. Profiler evidence (`dotnet-trace` / harness)

Harness: `benchmarks-comparison/src/Plaxion.BenchMarks.Comparison.Profiling`  
Driver: `scripts/run-profiling.ps1`  
Artifacts (gitignored): `benchmarks-comparison/profiling-results/round4/`

### 3.1 Relative throughput under profiler (informational; BDN is authoritative for ns)

| Scenario | Plaxion ops/s | Mediator ops/s | Plaxion/Mediator |
|----------|--------------:|---------------:|-----------------:|
| Send0 | ~high | ~higher | ~0.4× order (Send0 gap largest relatively) |
| Send5 | ~1.54M | ~1.86M | ~0.83× |
| Send20 | ~0.37M | ~0.44M | ~0.84× |
| TypeVariety50 | ~0.37M | ~0.82M | ~0.45× |

### 3.2 Call-tree / topN shape

**Plaxion TypeVariety50 (managed exclusive, idle frames ignored):**

- `PlaxionMediatorSender.Send` present
- `Dictionary.FindValue` present (map lookup) — small exclusive % (~0.07% band), **not** the whole 2× gap
- `SendCore_*` still on path (resolve + empty-pipeline branch + `Handle`)
- No `IsInstanceOfClass` storm (Round 3 G1-A still holding)

**Mediator TypeVariety50:**

- `FrozenDictionary` get / `GetValueRefOrNullRefCore`
- Wrapper `Handle` → root handler / handler `Handle`
- Fewer framework frames between `IMediator.Send` and handler body

**Plaxion Send5/Send20:**

- `PipelineRunner.Next` trampoline + behavior `MoveNext` dominate useful time
- Pooled runner / `IValueTaskSource` appears on incomplete paths; sync-complete behaviors stay on `Next` without full async machinery
- DI `GetServices` **not** on hot path (Round 1/2 still holding)

**Interpretation:** TypeVariety residual is **path shape** (more steps after type id), not dictionary lookup alone. Pipeline residual is **per-call trampoline vs pre-composed chain**, not allocation (alloc already matches).

---

## 4. JIT / disassembly notes

Attempted:

- BDN `DisassemblyDiagnoser` via temporary filtered runs (not committed into shipped `Program.cs` / benchmark classes).
- `DOTNET_JitDisasm` on the profiling harness for `*PlaxionMediatorSender:Send*`.

**Reliable takeaways (without fabricating full asm listings):**

1. Plaxion `Send` for N=51 is large (map + 51-case jump table + `CastOrAdapt` calls). Round 3 already showed **bloating `Send` further** (G1-B hoisting empty-pipeline bodies) **regressed TypeVariety ~75%** — strong evidence the JIT size/complexity heuristic is binding.
2. `SendCore_*` is `[AggressiveInlining]`; empty path can fold, but **51 distinct cores** still create more code than Mediator’s single wrapper `Handle` + shared `_rootHandler` invoke.
3. Mediator’s generic Send is a **short** method: lookup → interface dispatch → return. Better inlining/locality story for TypeVariety.
4. Pipeline: Mediator’s nested delegates are stable call targets; Plaxion’s `Next` is one shared trampoline with index/branch per hop — harder to fully inline a deep chain.

Full machine-code dumps were not durable enough to ship as primary evidence; **generated-source + profiler call trees + BDN** are the authoritative triad for this round.

---

## 5. Exact latency attribution

### 5.1 `Send` path (behaviors)

```text
Plaxion:
  ISender.Send → type map → SendCore → (behaviors?)
    → PipelineComposer.Rent/Run → Next×N → handler.Handle
    → optional IValueTaskSource wrap

Mediator:
  IMediator.Send → FrozenDict wrapper → wrapper.Handle
    → _rootHandler(request, ct)   // pre-built N-deep delegate chain
    → handler.Handle
```

**Where the extra latency lives (evidence):**

1. **Structural:** per-call runner + `Next` vs compose-once chain (dominant story once alloc matched).
2. **Entry:** map + `SendCore` prolog vs wrapper interface call (visible on Send0 / shallow depths).
3. **Not primary:** DI resolution, behavior array alloc, method-group `Func` (already fixed R1–R2).
4. **Not primary:** exception-wrapping helpers (`NoInlining`) on success path.

### 5.2 TypeVariety path

```text
Plaxion: GetType → Dictionary → switch(id) → cast → SendCore → CastOrAdapt → Handle
Mediator: GetType → FrozenDict → wrapper.Handle → (cast) → Handle / _rootHandler
```

**Can `SendCore` disappear?** Only by inlining its body into `Send` or into wrappers. Round 3 G1-B proved bulk inline into `Send` **hurts**. Mediator-style **wrappers** could remove `SendCore` without bloating `Send` — but that is a large codegen redesign; empty-pipeline wrappers alone still leave pipeline API-bound.

**Can `CastOrAdapt` disappear?** For monomorphic `TResponse` matching the handler (TypeVariety always `string`), JIT should fold it. Residual cost is mostly the **call boundary / generic instantiation**, not a true copy. Mediator avoids a separate helper by returning `ValueTask<TResponse>` from the typed wrapper interface.

**Would wrapper-object dispatch win?** Plausible for TypeVariety (closer to Mediator). Round 4 did **not** implement full wrappers (high complexity / risk); the smaller FrozenDictionary step was measured first and did not move the needle.

---

## 6. Pipeline allocation–latency divergence

| Fact | Source |
|------|--------|
| Allocations **identical** to Mediator at 1/5/10/20 behaviors (128/640/1280/2560 B) | BDN Round 2–4 |
| Latency still higher | BDN + profiler |
| Cause of residual latency | **Not** missing pool; **not** extra 192 B class runner (fixed R2) |
| Remaining mechanism | Index trampoline + virtual `IPipelineBehavior.Handle` + inability to pre-build message-carrying chain under `RequestHandlerDelegate` |

Mediator’s pre-built chain still pays virtual/interface behavior calls and async state machines **per behavior depth** (hence alloc parity). Plaxion pays an extra **control-plane** cost organizing those calls each Send.

---

## 7. Why Mediator’s wrapper architecture cannot be fully exploited today

| Mediator advantage | Why Plaxion cannot match it under current public API |
|--------------------|------------------------------------------------------|
| `MessageHandlerDelegate(message, ct)` | Plaxion `RequestHandlerDelegate` is `()` — **public** |
| Compose once at Init | Parameterless `next` requires per-call request storage for concurrency safety |
| Singleton long-lived wrappers | Plaxion Scoped + Transient-correct behavior resolve is a product choice |
| Wrapper returns `ValueTask<TResponse>` | Plaxion monomorphizes via `SendCore` + `CastOrAdapt` |
| Optional concrete `Send(TRequest)` | Public surface is `ISender.Send<TResponse>(IRequest<TResponse>)` only |

**This is the core architectural reason** for the remaining pipeline latency gap, stated explicitly:

> Mediator is a **compose-once, invoke-many** system whose next-delegate **threads the message**. Plaxion is a **compose-each-call (structure) / resolve-from-IServiceProvider** system whose next-delegate is **MediatR-shaped** (`Func<ValueTask<T>>`), which preserves flexible lifetimes and a familiar API at the cost of a runner trampoline.

---

# Phase 2 — Optimization attempts (the WHAT WAS TRIED)

## Attempt R4-A — `Dictionary<Type,int>` → `FrozenDictionary<Type,int>` — **REVERTED**

### Hypothesis

Mediator’s TypeVariety path uses `FrozenDictionary`. Plaxion TypeVariety topN shows `Dictionary.FindValue`. Swapping the static map to `FrozenDictionary` might recover a few percent on large-N dispatch without API changes.

### Implementation (temporary)

`SourceEmitter.EmitSender`: emit `FrozenDictionary<Type,int>` + `map.ToFrozenDictionary()` when handler count > 16. Conditional `using System.Collections.Frozen` only on that path.

### Validation

1. `dotnet test PlaxionMediator.sln -c Release` — green (with temporary generator-test TPA tweak while experiment was live).  
2. Real BDN TypeVariety (default job): Plaxion **~2674 ns** vs Round 3 baseline **~2614 ns** — **no improvement** (slightly worse / noise). Mediator same session ~1144 ns.  
3. Pipeline (default job, experiment live): Send0 ~43 ns (noisy vs ~53), Send5 ~516, Send20 ~2224 — mixed, not a clean ≥2–3% story.  
4. Profiler: dictionary exclusive was already tiny; Frozen cannot erase SendCore/CastOrAdapt/wrapper gap.

### Decision: **REVERTED completely**

Does not meet keep bar. Restored `Dictionary<Type,int>` emission and test helper. Tree matches Round 3 generator behavior.

### Why it failed

Lookup was never the dominant residual. Mediator wins on **what the dictionary stores** (ready wrappers + typed Handle), not on Frozen vs Dictionary alone.

---

## Attempts considered but not implemented (evidence said no)

| Idea | Why skipped |
|------|-------------|
| Hoist empty-pipeline into `Send` again | Round 3 G1-B: **−75%** TypeVariety |
| Shallow 1–2 runner specialization | Round 3 G2-A: &lt;2–3%, complexity |
| Drop null guards | Round 3 G2-B: no win |
| Full Mediator-style wrapper table codegen | Large rewrite; empty-pipeline only would leave pipeline gap untouched; pipeline wrappers still blocked by `RequestHandlerDelegate` shape without internal dual-API (high risk) |
| Change `RequestHandlerDelegate` / behavior interface | **Public API break** — out of scope |
| Pre-compose capturing request | Allocates or races under concurrency |
| Speculative `MethodImpl` / null-check thrash | Forbidden without exact bottleneck proof |

---

# Final numbers (Round 4 — no KEPT code changes)

Authoritative full suite: real BenchmarkDotNet, `WarmupCount=3`, `IterationCount=10`, `LaunchCount=1`, Windows 11, i7-12700K, .NET 9.0.7.  
See `RESULTS.md` Round 4 section for full tables.

### Headline comparisons vs Round 3

| Metric | Round 3 | Round 4 (this session) | Notes |
|--------|--------:|-----------------------:|-------|
| TypeVariety Plaxion | 2614 ns | **2142 ns** | Session variance; **no code change** — do not claim a Round 4 optimization win |
| TypeVariety Mediator | 1144 ns | **967 ns** | Same |
| TypeVariety ratio P/M | ~2.3× | **~2.2×** | Structurally unchanged |
| Pipeline allocs | Mediator parity | **Mediator parity** | 128/640/1280/2560 B |
| Send0 Plaxion | ~53 ns | **~51 ns** | Unchanged structure |
| Send5 Plaxion | ~537 ns | **~513 ns** | Unchanged structure |
| Send20 Plaxion | ~2094 ns | **~2103 ns** | High StdDev this run |

### Confirmations

- Public API **100% identical**.  
- All main-repo tests **pass**.  
- No KEPT changes to `src/PlaxionMediator*`.  
- Adapters / Shared / four benchmark classes / comparison `Program.cs` **untouched**.

---

# What would be required to close the gap (future, out of scope)

1. **Pipeline parity path:** evolve public (or additive) API so `next` can carry message+CT *or* expose a source-generated sealed pipeline with known behavior types — enabling true compose-once.  
2. **TypeVariety parity path:** generate Mediator-like wrapper objects implementing a typed `Handle(IRequest<TResponse>)` and dispatch via `FrozenDictionary<Type, wrapper>` from generic `Send`, without bloating `Send` itself.  
3. **Optional:** concrete generated `Send(TRequest)` overloads for callers that bind concrete types (does not help interface-typed benches).

Until (1), pipeline latency will remain slightly behind Mediator at equal allocations. Until (2), TypeVariety will remain ~2× class.

---

## Files touched this round

| File | Role |
|------|------|
| `benchmarks-comparison/OPTIMIZATION_REPORT_ROUND4.md` | This report |
| `benchmarks-comparison/RESULTS.md` | Round 4 BDN snapshot appended |
| `benchmarks-comparison/PROFILING_REPORT_ROUND2.md` | Round 4 investigation pointer appended |
| `src/PlaxionMediator.SourceGenerators/SourceEmitter.cs` | Temporary Frozen experiment **fully reverted** |
| `test/.../GeneratorTestHelper.cs` | Temporary Frozen ref **fully reverted** |

Ephemeral logs under `benchmarks-comparison/profiling-results/` (gitignored) are not deliverables.
