# PlaxionMediator Profiling Comparison Report

> Generated: 2026-08-03  
> Environment: Windows 11, .NET 9, `dotnet-trace` 9.0.661903 (`dotnet-sampled-thread-time` + `gc-verbose`), `dotnet-gcdump` 9.0.661903  
> Harness: `src/Plaxion.BenchMarks.Comparison.Profiling`  
> Artifacts: `benchmarks-comparison/profiling-results/<Framework>/<Scenario>/`

This report is a **profiler-snapshot comparison** (CPU hot paths + heap snapshots), not a replacement for the BenchmarkDotNet suite in `RESULTS.md`. Throughput numbers below are from the profiling harness tight-loop (informational only). Allocation-per-op numbers cited from `RESULTS.md` (BenchmarkDotNet MemoryDiagnoser) are more precise than point-in-time `.gcdump` heaps for short-lived per-call objects.

---

## Capture matrix (honest status)

| Framework | Scenario | CPU `.nettrace` | Speedscope JSON | `.gcdump` | `topN` report | Captured this session? |
|-----------|----------|:---------------:|:---------------:|:---------:|:-------------:|:----------------------:|
| Plaxion | Send0 | Y | Y | Y | Y | **YES** |
| Plaxion | Send5 | Y | Y | Y | Y | **YES** |
| Plaxion | Send10 | — | — | — | — | **NO** (wired; not executed) |
| Plaxion | Send20 | Y | Y | Y | Y | **YES** |
| Plaxion | TypeVariety50 | Y | Y | Y | Y | **YES** |
| MediatR | Send0 | Y | Y | Y | Y | **YES** |
| MediatR | Send5 | Y | Y | Y | Y | **YES** |
| MediatR | Send10 | — | — | — | — | **NO** (wired; not executed) |
| MediatR | Send20 | Y | Y | Y | Y | **YES** |
| MediatR | TypeVariety50 | Y | Y | Y | Y | **YES** |
| Mediator | Send0 | Y | Y | Y | Y | **YES** |
| Mediator | Send5 | Y | Y | Y | Y | **YES** |
| Mediator | Send10 | — | — | — | — | **NO** (wired; not executed) |
| Mediator | Send20 | Y | Y | Y | Y | **YES** |
| Mediator | TypeVariety50 | Y | Y | Y | Y | **YES** |

**12 / 15** combos have real captured artifacts. The three `Send10` combos are fully wired in the CLI/scripts (`--scenario Send10`) and can be collected with:

```powershell
.\benchmarks-comparison\scripts\run-profiling.ps1 -Scenarios Send10 -DurationSeconds 4
```

Artifact layout example:

```text
profiling-results/Plaxion/Send5/
  Plaxion_Send5_cpu.nettrace
  Plaxion_Send5_speedscope.json
  Plaxion_Send5.gcdump
  Plaxion_Send5_topN.txt
  Plaxion_Send5_meta.json
  Plaxion_Send5_stdout.txt
```

---

## Methodology notes

1. **Isolation**: each framework×scenario runs in its own process via `dotnet-trace collect -- dotnet exec ... --framework X --scenario Y`.
2. **CPU profile**: `dotnet-sampled-thread-time` (~100 Hz stack samples of managed threads). Idle thread-pool waits (`WaitHandle.WaitOne`, `Monitor.Wait`) dominate raw exclusive %; analysis below **filters to mediator/DI/pipeline frames**.
3. **Allocation signals**:
   - Primary quantitative alloc/op: BenchmarkDotNet `RESULTS.md` (trusted).
   - Secondary: mid-run `dotnet-gcdump` heap snapshots (steady-state retained objects; short-lived per-call allocs are often already collected).
   - `gc-verbose` was enabled in the nettrace for future allocation-tick mining; speedscope files for deep behavior chains are very large because of GC event volume.
4. **Analysis tooling**: `dotnet-trace report <file> topN -n 50` → `*_topN.txt`; `scripts/analyze-profiling.ps1` for speedscope JSON (best on smaller traces); harness ops/sec from bare `dotnet exec` runs.

### Harness throughput (informational, 2s tight loop, Release)

| Scenario | Plaxion ops/s | MediatR ops/s | Mediator ops/s |
|----------|--------------:|--------------:|---------------:|
| Send0 | 13,230,045 | 10,796,463 | 28,300,312 |
| Send5 | 1,264,159 | 1,537,487 | 2,516,783 |
| Send20 | 383,955 | 471,181 | 650,458 |
| TypeVariety50 | 153,997 | 195,573 | 1,090,023 |

These track the ranking in `RESULTS.md` (Mediator ≫ others; Plaxion ≈ MediatR with behaviors; Plaxion trails on type variety).

### BenchmarkDotNet allocation reference (`RESULTS.md`)

| Scenario | Plaxion | MediatR | Mediator |
|----------|--------:|--------:|---------:|
| Send 0 behaviors | 0 B | 264 B | 0 B |
| Send 5 behaviors | 1,392 B | 1,896 B | 640 B |
| Send 20 behaviors | 4,992 B | 6,576 B | 2,560 B |
| Type variety ×50 | 0 B | 13,200 B | 0 B |

Approx **~240 B/behavior** for Plaxion once behaviors are present (vs ~320 B/behavior MediatR, ~128 B/behavior Mediator).

---

## CPU hot paths

### Plaxion / Send0 (baseline dispatch, no behaviors)

**Filtered profile highlights** (`Plaxion_Send0_topN.txt`):

| Frame | Inclusive | Exclusive | Role |
|-------|----------:|----------:|------|
| `PlaxionMediatorSender.Send` | 0.76% | 0.49% | Generated type switch + dispatch |
| `DependencyInjection!dynamicClass.ResolveService` | 0.11% | 0.11% | Handler DI resolve |
| `ServiceProvider.GetServices` | 0.04% | 0.01% | Behavior enumeration (empty) |

**Interpretation:** With zero behaviors the generated path is already lean: type-switch → `GetRequiredService<IRequestHandler<,>>` → `handler.Handle`. Exclusive framework time is small; idle waits dominate the unfiltered profile. Plaxion beats MediatR slightly on harness throughput here and matches the 0 B alloc story from BDN.

**Cross-ref MediatR Send0:** `Mediator.Send` ~ inclusive in the low-20% band when behaviors exist; at 0 behaviors MediatR still allocates 264 B/call (wrapper/`Task` machinery) per BDN.

**Cross-ref Mediator Send0:** Fastest by far (~2× Plaxion harness ops/s). Source-generated direct invoke; essentially no DI in the hot path.

---

### Plaxion / Send5 (primary optimization target)

**Filtered profile highlights** (`Plaxion_Send5_topN.txt`):

| Frame | Inclusive | Exclusive | Role |
|-------|----------:|----------:|------|
| **`PipelineComposer.Compose`** | **9.77%** | **9.77%** | **Build async-lambda chain every Send** |
| `PlaxionMediatorSender.Send` | 21.95% | 1.71% | Outer generic Send + `Adapt` |
| `PlaxionMediatorSender.SendCore_0` | 3.23% | 0.16% | Resolve handler + behaviors + execute |
| `Enumerable.ICollectionToArray` | 0.17% | 0.14% | `GetServices(...).ToArray()` |
| Dispatch lambda `MoveNext` | 21.97% | ~0% | Harness await loop (inclusive carrier) |

#### Finding P1 — Per-call pipeline composition (HIGH)

**(1) Why (Plaxion root cause)**  
Generated `SendCore_*` (see `SourceEmitter.EmitSend`) always does:

```csharp
IPipelineBehavior<TReq,TRes>[] behaviors =
    _services.GetServices<IPipelineBehavior<TReq,TRes>>().ToArray();
if (behaviors.Length == 0) return handler.Handle(...);
return PipelineComposer.ExecuteAsync(request, behaviors, handler.Handle, ct);
```

`PipelineComposer.Compose` (`src/PlaxionMediator.Pipeline/PipelineComposer.cs`) then loops behaviors **backwards** and allocates a **new async lambda** (`RequestHandlerDelegate<TResponse>`) per behavior, each capturing `behavior`, `continuation`, `request`, and `cancellationToken`, plus a try/catch that constructs `PipelineExecutionException` messages with `behavior.GetType().Name` on failure paths:

```csharp
next = async () =>
{
    try { return await behavior.Handle(request, continuation, cancellationToken)...; }
    catch ...
};
```

That is **O(n) delegate + async-state-machine allocations and composition CPU on every Send**.

**(2) MediatR equivalent**  
MediatR also builds a pipeline per call (classic `Enumerable.Aggregate` / reverse fold over behaviors). Profile confirms:

| Frame | Inclusive | Exclusive |
|-------|----------:|----------:|
| `Enumerable.Aggregate` | 6.78% | 5.64% |
| `Task.FromResult` | 4.22% | 4.22% |
| `Mediator.Send` | 21.84% | 2.07% |

MediatR’s fold is slightly cheaper in exclusive % than Plaxion’s `Compose`, but MediatR pays extra on `Task.FromResult` and higher alloc/op (1,896 B vs Plaxion 1,392 B at 5 behaviors).

**(3) Mediator equivalent**  
Mediator (martinothamar) source-generates a **fixed pipeline call graph** for known behavior registrations. Exclusive time does **not** show a `Compose`/`Aggregate` hot frame; overhead appears as thin `MoveNext` state machines for async behaviors only. Alloc/op ~640 B at 5 behaviors (less than half of Plaxion).

**(4) Expected gain if optimized**  
Eliminating per-call composition + delegate allocs could realistically reclaim **most of the ~240 B/behavior** and a large fraction of the Compose exclusive time. Ballpark vs current Plaxion Send5:

- Target band: approach Mediator’s 640 B and close a meaningful part of the 1.26 M → 2.52 M ops/s gap.
- Conservative estimate: **15–35% latency improvement** and **50–80% allocation reduction** on behavior-heavy Send if behaviors/handlers are cached and composition is struct/loop based.
- Aggressive (Mediator-like generated chain): **up to ~2×** on deep pipelines, API-permitting internal changes only.

**(5) Safest optimization (no public API change)**  
Internal only:

1. Cache `IPipelineBehavior<TReq,TRes>[]` (or `IReadOnlyList<>`) per closed type inside the generated sender or a scoped/singleton `PipelineCache`, invalidated only if the DI container is rebuilt (normal app lifetime = never).
2. Replace async-lambda composition with either:
   - a **single reusable state machine** / indexed loop invoking `behaviors[i].Handle` with a struct `next` trampoline, or
   - prebuild a chain of **non-allocating** `RequestHandlerDelegate` instances once per closed type when the behavior list is stable.
3. Keep `PipelineComposer` public shape if needed, but add an overload that accepts a precomposed delegate or spans.

Do **not** change `IPipelineBehavior<,>`, `ISender.Send`, or registration APIs.

---

### Plaxion / Send20 (scales the same bug)

| Frame | Inclusive | Exclusive |
|-------|----------:|----------:|
| `PipelineComposer.Compose` | 9.35% | 9.35% |
| `PlaxionMediatorSender.Send` | 20.61% | 0.85% |
| `SendCore_0` | 2.67% | 0.04% |
| `ICollectionToArray` | 0.12% | 0.11% |
| `__Canon].Handle(..., RequestHandlerDelegate)` | 2.74% | 0.02% |

Compose remains ~9% exclusive even at 20 behaviors (composition cost is linear in n but so is behavior body work; relative exclusive % stays high). BDN: 4,992 B/call Plaxion vs 2,560 B Mediator vs 6,576 B MediatR.

MediatR Send20 still shows `Enumerable.Aggregate` as a top exclusive framework frame (~7% band). Mediator Send20 still has no compose-equivalent spike.

---

### Plaxion / TypeVariety50

Harness: Plaxion **154k** ops/s vs MediatR **196k** vs Mediator **1.09M** (~7×).

BDN: Plaxion **0 B**, MediatR **13,200 B**, Mediator **0 B**.

#### Finding P2 — Dispatch / resolution overhead across many request types (HIGH for throughput, not alloc)

**(1) Why**  
Generated `Send` uses a large `switch (request)` with one `case` per known request type, each calling `SendCore_i` which still:

- resolves `IRequestHandler<Ti, R>` via DI every call  
- resolves behaviors via `GetServices().ToArray()` every call (even when empty → empty array path is cheap and 0 B net in BDN, but still virtual dispatch / enumerator work)

With 50 types, switch dispatch is fine for the JIT, but **repeated DI resolution** and the generic `Send` → `Adapt<TActual,TResponse>` layer add overhead Mediator avoids by emitting **direct typed calls** with baked handler references.

**(2) MediatR**  
Runtime wrapper lookup + 264 B/call × 50 = 13,200 B matches BDN. Slightly faster than Plaxion here despite allocations (mature wrapper cache).

**(3) Mediator**  
Compile-time message map; essentially free type dispatch and 0 B.

**(4) Expected gain**  
Caching handler + empty-behavior fast path more aggressively could yield **10–25%** on type-variety. Full Mediator parity needs deeper source-gen (store handler instances or invokers on the sender) — **30–70%** possible without public API changes if generation stays internal.

**(5) Safest optimization**  
In generated `SendCore_*` when no behaviors are registered for that closed type (common case):

- Fast path already exists (`if (behaviors.Length == 0) return handler.Handle`), but still allocates/enumerates services each time before the check.
- Prefer: **once-per-type** cached `bool hasBehaviors` + cached handler invoker (`Func<TReq, CT, ValueTask<TRes>>`) built lazily on first call.
- Optionally emit `Adapt` elision when `TActual` is exactly `TResponse` (direct `return SendCore_i(...)` without async cast helper).

---

## Allocation hot spots

### Per-call (from BDN + source; confirmed by profile frames)

| Allocation source | Plaxion evidence | MediatR | Mediator |
|-------------------|------------------|---------|----------|
| Pipeline continuation delegates | `PipelineComposer` async lambdas each Send; Compose exclusive ~9.7% | `Aggregate` fold + `RequestHandlerDelegate` | Mostly avoided via generated chain |
| Behavior array | `GetServices().ToArray()` → `ICollectionToArray` in topN | Similar service enumeration | Compiled-in list |
| Async state machines | Each async lambda + behavior `async Handle` | `Task` + state machines; `Task.FromResult` 4% excl. | Thin MoveNext only |
| Response adapter | Generated `Adapt<TActual,TResponse>` (async cast) | N/A (Task-based) | Direct typed return |
| Simulated work payloads | Benchmark behaviors allocate `new ScenarioPayload` each call (equal across frameworks; noise for relative compare) | same | same |

### Heap snapshots (`.gcdump` mid-run)

Point-in-time retained heap is small for all three (hundreds of KB) because per-call objects die quickly. Useful observations:

- Plaxion Send0 heap ~408 KB / 5.3k objects — DI descriptors + runtime infrastructure.
- Behavior tiers retain open-generic behavior registrations and DI nodes; not a leak signal.
- MediatR heaps show more `RequestHandlerDelegate` / wrapper related retained metadata under load.
- **Do not treat gcdump totals as alloc/op.** Use BDN for that.

---

## DI resolution costs

| Cost | Plaxion | MediatR | Mediator |
|------|---------|---------|----------|
| Handler resolve | `_services.GetRequiredService<IRequestHandler<,>>()` every SendCore | Wrapper resolves handler via SP | Often direct / generated |
| Behavior resolve | `GetServices<IPipelineBehavior<,>>().ToArray()` every SendCore | Enumerate behaviors each send | Generated fixed list |
| Profile signal | `ResolveService`, `GetService`, `ICollectionToArray` present but smaller than Compose | `ResolveService` + Aggregate dominate framework exclusive | Almost absent from exclusive topN |

#### Finding P3 — Behavior service enumeration every call (MEDIUM–HIGH)

**(1) Why**  
Open-generic `IPipelineBehavior<,>` registrations are correct and flexible, but resolving them on the hot path forces DI to build the closed enumerable repeatedly.

**(2) Others**  
MediatR: same class of cost. Mediator: compile-time.

**(3) Gain**  
Caching the closed behavior array per `(TRequest,TResponse)` should remove `ICollectionToArray` and most DI churn on steady-state Send — estimate **5–15%** CPU and part of the alloc/op once combined with P1.

**(4) Safe fix**  
Lazy static/concurrent cache keyed by `Type` pair inside generated sender (or a singleton `IPipelineBehaviorCache` registered by the generator). Lifetime note: cache instances only when behavior lifetime is Singleton; for Scoped behaviors, cache the *factory* or resolve once per scope via scoped sender (sender is already `TryAddScoped`).

---

## Pipeline construction costs

Covered in P1. Summary:

| | Plaxion | MediatR | Mediator |
|--|---------|---------|----------|
| When | Every Send with n>0 | Every Send with n>0 | Compile time / startup |
| Mechanism | Async lambda wrap loop | LINQ Aggregate fold | Generated method chain |
| Exclusive CPU (Send5) | Compose **9.77%** | Aggregate **5.64%** | (no equivalent spike) |
| Alloc correlation | ~240 B × n | ~320 B × n | ~128 B × n |

---

## Handler resolution costs

| | Plaxion | MediatR | Mediator |
|--|---------|---------|----------|
| Mechanism | DI `GetRequiredService` per SendCore | DI via request handler wrapper | Generated |
| Send0 impact | Small but visible (`ResolveService` 0.11%) | Wrapper + 264 B | Near zero |
| TypeVariety impact | 50× resolve amplifies gap | Wrapper cache helps | Dominates win |

#### Finding P4 — `Adapt<TActual,TResponse>` indirection (MEDIUM)

**(1) Why**  
`EmitSend` always returns `Adapt<TActual, TResponse>(SendCore_i(...))` so a single generic `Send<TResponse>` can host heterogeneous actual response types. Even when `TActual` is `string` and `TResponse` is `string`, the helper is an `async` method that awaits and casts via `(TResponse)(object)result!`.

**(2) Others**  
MediatR uses `Task<T>` naturally. Mediator returns typed `ValueTask<T>` from generated methods without a cast adapter.

**(3) Gain**  
Eliding Adapt when types are identical: small but free win on every Send (**~0–5%**, more on Send0). Avoids an async method frame when the inner `ValueTask` is incomplete (rare in these sync handlers).

**(4) Safe fix**  
Generator emits:

```csharp
case FooRequest r: return SendCore_0(r, ct); // when response type == TResponse
```

and keeps `Adapt` only for true type variance (if ever needed). Public API unchanged.

---

## Finding P5 — Exception wrapping in the hot composition path (LOW–MEDIUM)

`PipelineComposer` wraps every behavior call in try/catch and on failure allocates message strings with `behavior.GetType().Name` / `typeof(TRequest).Name`. Even when exceptions are rare, the try/catch region can inhibit some inlining and forces async lambda shape.

**Safe fix:** move exception wrapping to a single outer boundary, or use a non-async helper with `[MethodImpl(AggressiveInlining)]` for the success path; keep semantic parity for `PipelineExecutionException` and rethrow rules.

---

## Prioritized recommendations

| Priority | ID | Recommendation | Est. impact | API risk |
|----------|----|----------------|-------------|----------|
| **HIGH** | P1 | Stop allocating async lambdas per Send in `PipelineComposer`; precompose or use struct/indexed pipeline | 15–35%+ latency on behavior chains; large alloc cut | None (internal) |
| **HIGH** | P3 | Cache closed `IPipelineBehavior<TReq,TRes>[]` (or invoker) per request type / scope | 5–15% CPU; enables P1 | None |
| **HIGH** | P2 | Lazy per-type handler invoker cache; empty-behavior fast path without `GetServices` | 10–25% type-variety; helps all Sends | None |
| **MEDIUM** | P4 | Elide generated `Adapt<>` when actual response type equals `TResponse` | Small steady win on all Sends | None |
| **MEDIUM** | P5 | Slim exception boundary in composer so success path is thinner | Small | None if semantics preserved |
| **LOW** | — | Consider optional source-gen of fixed behavior chains when behaviors are known at compile time (Mediator-style), behind existing registration model | Large on deep pipelines | Must not change public registration API surface |
| **LOW** | — | Benchmark behaviors allocate `ScenarioPayload` each call — fine for fairness, but real apps should avoid allocs inside behaviors (document / analyzer already exists) | App-level | N/A |

### Suggested implementation order (safest → largest win)

1. **Cache behaviors + empty-behavior short-circuit without enumeration** in generated `SendCore_*` (P3/P2).  
2. **Rewrite `PipelineComposer`** to a non-allocating loop or cached delegate chain (P1).  
3. **Generator polish**: Adapt elision (P4), tighter exception path (P5).  
4. Re-run `scripts/run-profiling.ps1` + BenchmarkDotNet suite; compare Send5/Send20/TypeVariety50.

---

## How to reproduce / extend

```powershell
# Install tools (script also auto-installs)
dotnet tool install --global dotnet-trace
dotnet tool install --global dotnet-gcdump

# Build + profile all 15 (or subsets)
cd benchmarks-comparison
.\scripts\run-profiling.ps1 -DurationSeconds 4
.\scripts\run-profiling.ps1 -Frameworks Plaxion -Scenarios Send10 -DurationSeconds 4

# Single combo manually
dotnet-trace collect --profile dotnet-sampled-thread-time,gc-verbose --format Speedscope `
  -o profiling-results\Plaxion\Send10\Plaxion_Send10_cpu.nettrace -- `
  dotnet exec src\Plaxion.BenchMarks.Comparison.Profiling\bin\Release\net9.0\Plaxion.BenchMarks.Comparison.Profiling.dll `
  --framework Plaxion --scenario Send10 --duration-seconds 4 --ready-signal

# Top-N CPU report
dotnet-trace report profiling-results\Plaxion\Send5\Plaxion_Send5_cpu.nettrace topN -n 50

# Heap summary
dotnet-gcdump report profiling-results\Plaxion\Send5\Plaxion_Send5.gcdump
```

Harness CLI:

```text
--framework Plaxion|MediatR|Mediator
--scenario Send0|Send5|Send10|Send20|TypeVariety50
--duration-seconds <n> | --iterations <n>
--ready-signal
--list
```

---

## Caveats

- Sampled profiles include idle thread waits; always prefer filtered frames or inclusive time under `PlaxionMediatorSender.Send`.
- Speedscope JSON for Send5/Send20 with `gc-verbose` can be tens–hundreds of MB; prefer `dotnet-trace report topN` for automated analysis.
- Harness ops/sec is not BenchmarkDotNet; use it for relative profiling only.
- Send10 tiers were not captured in this session; conclusions for 5 and 20 behaviors are expected to interpolate linearly for Compose/alloc costs.
- No fabricated frame percentages: every % quoted above comes from a real `*_topN.txt` or harness/BDN measurement listed in the capture matrix.

---

## Bottom line

PlaxionMediator’s **zero-behavior and notification** paths are already competitive. The dominant, actionable gap versus Mediator (and the remaining drag versus a tighter MediatR) is **per-Send pipeline construction**: `GetServices().ToArray()` + **`PipelineComposer` async-lambda composition**. Fixing that internally—without touching the public mediator API—is the highest-leverage optimization and is fully justified by the profiles captured in this session.
