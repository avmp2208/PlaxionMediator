# Analyzers Reference

All PlaxionMediator analyzers ship in `PlaxionMediator.Analyzers` (transitively referenced by `PlaxionMediator.DependencyInjection`) and report as build **warnings**, not errors, except where noted.

| Id | Name | Fires when | Severity |
|---|---|---|---|
| `PlaxionMediator001` | Missing Handler | An `IRequest<T>`/`INotification` has zero registered handlers | Error |
| `PlaxionMediator002` | Multiple Handlers | An `IRequest<T>` has more than one registered handler (ambiguous dispatch) | Error |
| `PlaxionMediator003` | Mutable Request | A request type is not an immutable `sealed record` (found mutable public setters) | Error |
| `PlaxionMediator004` | Missing CancellationToken | A handler's `Handle` method doesn't accept/forward a `CancellationToken` | Warning |
| `PlaxionMediator005` | Missing Request Binding Attribute | `MapPlaxionMediatorGet`/`MapPlaxionMediatorDelete<TRequest,TResponse>` called with a `TRequest` that has no bindable route/query members | Warning |
| `PlaxionMediator006` | Handler Blocking Call | `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` used inside an `IRequestHandler<,>`/`INotificationHandler<>` `Handle` implementation | Warning |
| `PlaxionMediator007`–`010` | _Reserved_ | Intentionally reserved; no high-value diagnostic identified | — |
| `PlaxionMediator011` | Non-Sealed Handler | A handler class is not sealed, allowing accidental subclassing that bypasses DI-registered behavior | Warning |
| `PlaxionMediator012` | Multiple Public Constructors | A handler class declares more than one public constructor | Warning |
| `PlaxionMediator013`–`019` | _Reserved_ | Intentionally reserved; no high-value diagnostic identified | — |
| `PlaxionMediator020` | Invalid Behavior Registration | `PipelineBuilder.Use<T>()` called with a type that does not implement `IPipelineBehavior<,>` | Error |
| `PlaxionMediator021` | Duplicate Registration | The same behavior type is registered more than once for the same pipeline | Warning |
| `PlaxionMediator022` | Incorrect Lifetime | A Singleton handler/behavior captures a Scoped or Transient dependency | Warning |
| `PlaxionMediator023` | Mutable State in Behavior | A pipeline behavior maintains mutable instance state (field/property writes) in its Handle method | Warning |
| `PlaxionMediator024` | Invalid Extension Registration | `PipelineExtensionBuilder.Use<T>()` called with a type that does not implement `IPipelineExtension` | Error |
| `PlaxionMediator025` | Duplicate Extension Registration | The same extension type is registered more than once on the same `PipelineExtensionBuilder` chain | Warning |
| `PlaxionMediator026`–`030` | _Reserved_ | Intentionally reserved; no high-value diagnostic identified | — |
| `PlaxionMediator031` | Missing CancellationToken Propagation | A handler/behavior receives a `CancellationToken` but doesn't pass it to an awaited async call | Warning |
| `PlaxionMediator032` | CancellationToken.None Usage | `CancellationToken.None` used inside a handler where an ambient token is available | Info |
| `PlaxionMediator033`–`039` | _Reserved_ | Intentionally reserved; no high-value diagnostic identified | — |
| `PlaxionMediator040` | Async Void Handler | A handler or behavior method is declared `async void`, preventing proper exception observation | Error |
| `PlaxionMediator041` | Handler Self-Send | A handler sends a request of its own type, risking infinite recursion | Warning |
| `PlaxionMediator042` | Missing Transaction Behavior | `ITransactionalRequest` used but `TransactionBehavior` not registered | Error |
| `PlaxionMediator043` | Invalid Transaction Ordering | `TransactionBehavior` registered in unsafe order relative to `RetryBehavior` | Error |
| `PlaxionMediator044` | Ambiguous Transaction Manager | Multiple `ITransactionManager` implementations registered without resolution | Error |
| `PlaxionMediator045` | Unsupported Isolation Level | `ITransactionalRequest.IsolationLevel` unsupported by registered provider | Warning |
| `PlaxionMediator050` | LINQ in High-Frequency Handler | A `[HighFrequency]` request handler uses LINQ extension methods | Info |
| `PlaxionMediator051` | Closure Capture in Hot Path | A lambda or local function inside a handler/behavior Handle method captures local state | Info |
| `PlaxionMediator070` | Fire-and-Forget Task.Run | `Task.Run(...)` invoked inside a Handle method without being awaited or returned | Warning |
| `PlaxionMediator080` | Unnecessary Behavior on Hot Path | A `[HighFrequency]` request has more than N behaviors (default: 3) in its chain | Info |
| `PlaxionMediator081` | Synchronous-Only Handler | A handler has no `await`; suggests using `ValueTask.FromResult` for optimized completion | Info |
| `PlaxionMediator082` | Behavior Allocates in Hot Path | A behavior allocates a new closure/collection per call | Info |
| `PlaxionMediator083` | Stream Handler Buffers Sequence | A stream handler materializes the entire sequence before yielding, defeating streaming | Warning |
| `PlaxionMediator084`–`089` | _Reserved_ | Intentionally reserved; no high-value diagnostic identified | — |
| `PlaxionMediator090` | Fail-Fast Notification Handler | A notification handler uses fail-fast throw patterns incompatible with fan-out semantics | Warning |

## Suppressing a false positive

Best practice for a legitimate, intentional exception (e.g. an empty "list all" request with `PlaxionMediator005`): a narrowly-scoped, documented pragma pair around the single call site.

```csharp
// GetItemsRequest deliberately has no route/query-bindable members (it lists all items),
// so PlaxionMediator005 (missing bindable surface) is intentionally suppressed here.
#pragma warning disable PlaxionMediator005
app.MapPlaxionMediatorGet<GetItemsRequest, IReadOnlyList<ItemDto>>("/items");
#pragma warning restore PlaxionMediator005
```

Avoid disabling a diagnostic project-wide via `.editorconfig` unless you're certain it should never fire anywhere in that project — that defeats the analyzer's purpose of catching genuinely accidental mistakes.

## Transaction-Specific Diagnostics (v0.8.0)

### PlaxionMediator042: Missing Transaction Behavior
- **Title:** Transactional request used without transaction behavior
- **Category:** Correctness
- **Severity:** Error
- **Trigger:** A request implements `ITransactionalRequest` but `TransactionBehavior<,>` is not registered in the pipeline (neither via global behaviors nor `PipelineBuilder`).
- **Non-Trigger:** Request implements `ITransactionalRequest` and `TransactionBehavior` is registered.
- **Rationale:** Declaring a request as transactional implies an expectation of atomicity. If the behavior is missing, the request executes without a transaction boundary, risking partial data updates.
- **Code-Fix:** Add `.AddTransactions()` to `AddPlaxionMediator()` or register `TransactionBehavior` manually.
- **False-Positive Risk:** None (deterministic marker interface check).
- **Tests:** `TransactionalRequest_MissingBehavior_ReportsError`, `TransactionalRequest_WithBehavior_NoDiagnostic`.

### PlaxionMediator043: Invalid Transaction Ordering
- **Title:** Unsafe transaction behavior ordering
- **Category:** Reliability
- **Severity:** Error
- **Trigger:** Both `TransactionBehavior` and `RetryBehavior` are registered, but `TransactionBehavior` is inner to `RetryBehavior` (i.e., `RetryBehavior` wraps `TransactionBehavior`).
- **Non-Trigger:** `TransactionBehavior` is outer to `RetryBehavior` (recommended default).
- **Rationale:** If a transaction wraps retries, a single transaction spans all attempts. If one attempt fails but a later one succeeds, the transaction might be in an inconsistent state or stay open too long. The recommended pattern is a fresh transaction per retry.
- **Code-Fix:** Reorder registrations so `TransactionBehavior` is outer to `RetryBehavior`.
- **False-Positive Risk:** Low; requires static analysis of registration order.
- **Tests:** `RetryInsideTransaction_ReportsError`, `TransactionInsideRetry_NoDiagnostic`.

### PlaxionMediator044: Ambiguous Transaction Manager
- **Title:** Multiple transaction managers registered
- **Category:** Configuration
- **Severity:** Error
- **Trigger:** Multiple implementations of `ITransactionManager` are registered in DI without a specific implementation being selected for the transactional behavior.
- **Non-Trigger:** Exactly one `ITransactionManager` is registered, or keyed registration is used to resolve the conflict.
- **Rationale:** `TransactionBehavior` needs a single manager to begin transactions. Ambiguity leads to runtime resolution failures.
- **Code-Fix:** Remove duplicate registrations or use keyed resolution.
- **False-Positive Risk:** None.
- **Tests:** `MultipleManagers_ReportsError`, `SingleManager_NoDiagnostic`.

### PlaxionMediator045: Unsupported Isolation Level
- **Title:** Unsupported transaction isolation level
- **Category:** Provider-Specific
- **Severity:** Warning
- **Trigger:** `ITransactionalRequest.IsolationLevel` is set to a value known to be unsupported by the registered `ITransactionManager` (e.g., `Snapshot` isolation on a provider that doesn't support it).
- **Non-Trigger:** Isolation level is supported or set to `null` (default).
- **Rationale:** Prevents runtime exceptions when the database provider rejects the requested isolation level.
- **Code-Fix:** Change `IsolationLevel` to a supported value.
- **False-Positive Risk:** Medium; depends on the analyzer's knowledge of provider capabilities.
- **Tests:** `UnsupportedIsolation_ReportsWarning`, `SupportedIsolation_NoDiagnostic`.
