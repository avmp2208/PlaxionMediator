# 22 — Benchmark Strategy

## Philosophy

Benchmarks exist to answer "will Conduit feel fast in a real application," not to win a synthetic leaderboard. Every benchmark scenario below is modeled after a realistic application shape (a web API handling commands/queries against a database, not an empty no-op handler in a tight loop) — per the explicit non-negotiable: *do not optimize for synthetic benchmarks*.

## Comparison Baselines

Conduit is benchmarked against **representative categories** of .NET request-pipeline implementations available in the ecosystem (named generically here since Conduit does not target compatibility or direct feature parity with any of them):

- A reflection-based, assembly-scanning open-generic mediator (representative of the dominant current pattern).
- A hand-rolled, no-framework baseline (direct method calls, no pipeline abstraction) — the theoretical performance ceiling.
- A source-generator-based mediator alternative, if one exists in the ecosystem at benchmark time, to compare compile-time-first approaches against each other fairly.

## Benchmark Suite Structure (`Conduit.Benchmarks`, BenchmarkDotNet)

| Benchmark | What It Measures | Why It Matters |
|---|---|---|
| `SimpleCommandDispatch` | Latency/allocations of `Send` for a request with zero pipeline behaviors | Isolates pure dispatch overhead from behavior composition cost. |
| `FullPipelineDispatch` | Latency/allocations of `Send` through the recommended default pipeline (logging, validation, authorization, caching miss, handler) | Reflects the realistic "typical production request" shape, not a stripped-down best case. |
| `NotificationFanOut` | Latency/allocations of `Publish` to N handlers (N = 1, 5, 20) | Validates the `ArrayPool`-based fan-out strategy scales sub-linearly in allocations as N grows. |
| `StreamingThroughput` | Items/sec and allocations/item for `CreateStream` over 100k items | Validates `IAsyncEnumerable<T>`-based streaming has no per-item dispatch tax. |
| `ConcurrencyScalability` | Throughput (requests/sec) at 1, 8, 32, 128 concurrent callers against a `Scoped` handler with simulated I/O (`Task.Delay(1ms)`) | Confirms no contention/lock introduced by `ConduitSender`'s stateless design under load (see [Internal Architecture](07-internal-architecture.md#concurrency--synchronization)). |
| `ColdStartRegistration` | Wall-clock time for `AddConduit()` + `BuildServiceProvider()` for 10 / 100 / 1000 handlers | Validates registration is O(handler count) with no scanning overhead, directly relevant to serverless/CLI tool cold start. |
| `NativeAotColdStart` | Process start-to-first-response time for a `PublishAot=true` minimal API using Conduit vs. baselines | The metric that matters most for AOT-targeted deployment scenarios (Lambda, Aspire microservices). |
| `NativeAotBinarySize` | Published binary size delta attributable to Conduit's packages | Validates the "pay only for what you use" packaging promise translates into actual binary size savings when only `Core`+`Pipeline`+`DI` are referenced. |
| `MemoryUnderSustainedLoad` | Gen0/Gen1/Gen2 collection counts and peak working set over a 60-second sustained load run | Detects allocation-driven GC pressure that per-call microbenchmarks alone might miss. |

## Why Each Metric Is Meaningful

- **Latency & Throughput**: the two numbers application teams actually care about when evaluating "will this framework add meaningful overhead to my request budget."
- **Allocations**: the leading indicator of GC pressure at scale — a framework with low per-call latency but high allocations degrades disproportionately under sustained production load, which single-shot latency benchmarks hide.
- **Cold Start / Native AOT**: increasingly decisive for serverless and container-orchestrated workloads where instance lifetime can be seconds to minutes; a framework with runtime-scanning startup cost is disqualified outright in these environments regardless of steady-state performance.
- **DI Registration Time**: directly measures whether the "compile-time-first" architectural bet pays off in practice versus reflection-based registration, which is the central performance claim of the entire framework.
- **Concurrency Scalability**: validates the stateless-dispatcher design claim from [Internal Architecture](07-internal-architecture.md) under realistic multi-threaded load, not just single-threaded micro-benchmarks.

## Benchmark Governance

- All benchmark results are published alongside each release in `docs/benchmarks/vX.Y.Z.md`, including the exact BenchmarkDotNet environment (CPU, .NET SDK version, OS) for reproducibility.
- Benchmarks run in CI on every PR touching `Conduit.Core`/`Conduit.Pipeline`/`Conduit.SourceGenerators`, with a regression gate (fail PR if `FullPipelineDispatch` p95 latency regresses >10% or allocations increase >15% without an accompanying justification in the PR description).
- Synthetic "zero-behavior, zero-IO" numbers are reported for transparency but are explicitly labeled as a ceiling, not a claim about real application performance — avoiding the classic "our no-op benchmark is 10x faster" marketing trap the non-negotiables explicitly warn against.
