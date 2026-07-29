# 21 — Performance

## Allocation Strategy

| Hot Path | Allocation Avoided | How |
|---|---|---|
| Dispatch (`Send`) | Boxing of struct requests, delegate allocation for type lookup | Monomorphic generated methods per request type (see [Internal Architecture](07-internal-architecture.md)); no `Dictionary<Type, Delegate>` lookup. |
| Synchronously-completing handlers | `Task<T>` heap allocation | `ValueTask<T>` everywhere on the hot path; handlers returning already-computed values (cache hits) allocate nothing. |
| "No response" requests | Boxed `void`/`object` sentinel | `Unit` is a zero-size `readonly struct` singleton. |
| Notification fan-out | `List<Task>` growable-list allocation | `ArrayPool<Task>` rental sized to the known (compile-time-counted) handler count for that notification type. |
| Pipeline chain construction | Closures per call is the accepted, documented tradeoff | See "Closures vs. Struct Continuations" below. |

## Pooling

- `ArrayPool<T>` for any transient array needed during notification fan-out or streaming buffering.
- `ObjectPool<StringBuilder>` (via `Microsoft.Extensions.ObjectPool`) inside `Conduit.Diagnostics`' basic tracing tag-building code, since building `Activity` tags involves short-lived string construction on every request.
- Handlers/behaviors themselves are **not** pooled — they are ordinary DI-scoped services; pooling instances that also close over per-request state is a correctness hazard not worth the marginal GC benefit for typical (non-hyperscale) request volumes.

## Inlining & Generic Specialization

Every generic type in the public API (`IRequestHandler<TRequest,TResponse>`, `IPipelineBehavior<TRequest,TResponse>`) is a genuine generic, not `object`-based — the JIT (or AOT compiler) specializes/monomorphizes each closed generic instantiation, meaning a `IRequestHandler<CreateOrderCommand, OrderId>` call site compiles down to a direct, potentially inlinable call with no `object` boxing for value-type responses. `MethodImplOptions.AggressiveInlining` is applied to the handful of trivial one-line generated forwarding methods (e.g., `Send<TRequest,TResponse>(TRequest)` calling the monomorphic `SendXxx`).

## `ValueTask` / `Span` / `Memory`

- `ValueTask<T>` is the return type for every `Handle`/`Send`/`Authorize`/`Validate` method — chosen over `Task<T>` because handlers frequently complete synchronously (validation short-circuits, cache hits, in-memory computations), and `ValueTask<T>` avoids the `Task<T>` allocation in that common case. The well-known `ValueTask` constraint (don't await twice, don't access `.Result`) is enforced by analyzer `CONDUIT033`.
- `Span<T>`/`ReadOnlySpan<T>` are used internally in `Conduit.Caching`'s key-building code to avoid intermediate string allocations when composing multi-field cache keys, and in `Conduit.Diagnostics`' `Activity` tag formatting.
- `Memory<T>` (not `Span<T>`) is used at any `async` boundary (streaming responses in `Conduit.Core`'s `IAsyncEnumerable<T>` buffering) since `Span<T>` cannot cross `await` points.

## Closures vs. Struct Continuations

The generated pipeline chain (per [Internal Architecture](07-internal-architecture.md#execution-strategy)) allocates one small closure per behavior per `Send` call to build the `RequestHandlerDelegate<TResponse>` continuation chain. An alternative **struct-based continuation** design was evaluated:

| Approach | Allocations | Readability of Generated Code | Verdict |
|---|---|---|---|
| Closure-based `RequestHandlerDelegate<TResponse>` (chosen) | ~24 bytes/behavior/call | High — matches ASP.NET Core's own `RequestDelegate` idiom, easy to debug | **Adopted for v1.** The allocation is small, Gen0, and short-lived; benchmarks (see [Benchmark Strategy](22-benchmark-strategy.md)) show it is not the dominant cost versus handler/IO work in realistic scenarios. |
| Struct-based pipeline cursor (`ref struct PipelineState` advancing an index into a static behavior array) | Zero allocation | Lower — generated code becomes a state machine, harder to read/debug, cannot easily capture behavior-specific closures (e.g., a `UseWhen` predicate result) | Documented as a **v2+ opt-in "AllocationFreeMode"** candidate ([Roadmap](25-roadmap.md)) for extreme-throughput scenarios, not the default — first principles favor readability/debuggability unless profiling proves the allocation is a real bottleneck for a given workload. |

## Native AOT Considerations

- Zero reflection anywhere in the runtime path (guaranteed structurally, see [Dependency Injection](09-dependency-injection.md#no-reflection-guarantees)) means zero AOT trimming warnings by construction — this is verified by a CI gate that runs `dotnet publish -p:PublishAot=true` on the sample apps and fails the build on any `IL2026`/`IL3050`-class warning.
- All public types avoid `dynamic`, `Reflection.Emit`, and expression-tree compilation (`Expression.Compile()`) — even convenience helpers that might reach for `Expression<Func<T>>` for property-name extraction use `nameof()` or source-generated equivalents instead.
- `Conduit.SourceGenerators`/`Conduit.Analyzers` run entirely inside the Roslyn compiler process at build time and contribute zero runtime dependencies to the published AOT binary — they are referenced with `<PrivateAssets>all</PrivateAssets>` and `ReferenceOutputAssembly="false"` semantics equivalent to a standard analyzer package.
- Startup time is dominated by DI container construction, not Conduit-specific work — since `AddConduit()` emits literal `ServiceDescriptor` additions with no scanning phase, its contribution to cold start is O(handler count) `List.Add` calls, effectively negligible even for large applications (validated in [Benchmark Strategy](22-benchmark-strategy.md)).

## Benchmark-Driven, Not Micro-Optimization-Driven

Every optimization described above is justified by an expected real-world scenario (handler-heavy web API, high-throughput streaming, Native AOT cold start), not by winning a synthetic micro-benchmark in isolation — consistent with the [Benchmark Strategy](22-benchmark-strategy.md) principle of prioritizing realistic application scenarios over synthetic numbers.
