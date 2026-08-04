# Release Notes

All notable changes to `PlaxionMediator` and its companion packages are documented in this file.

## v0.4.1

### Fixed
- **Unmapped exception handling**: Resolved a regression where exceptions surfacing from a terminal handler were incorrectly wrapped into `PipelineExecutionException`. Introduced `HandlerFaultException` and updated `PipelineComposer` to correctly attribute and unwrap raw handler faults, ensuring transparency in error reporting (e.g., returning raw 500s instead of problem+json for unmapped errors).

### Changed
- **Major Performance Optimization**: Rewrote the core pipeline execution engine using a field-staged `PipelineExecutor` (for up to 4 behaviors) and a pooled `PipelineRunner` fallback. This new architecture utilizes hybrid dictionary + jump-table dispatch and aggressive pooling, allowing PlaxionMediator to match or exceed the performance and allocation profile of Mediator and MediatR across pipeline, concurrency, and type-variety scenarios.
- **Documentation & Benchmarks Refresh**:
  - Added a comprehensive `benchmarks-comparison/` suite with detailed `RESULTS.md`.
  - Updated root `README.md` and `READMEpackage.md` with the latest competitive performance data.

## v0.4.0

### Added
- **New validation framework**: `PlaxionMediator.Validation` package provides a new `IPlaxionMediatorValidator<TRequest>` interface and `ValidationBehavior<TRequest, TResponse>` pipeline behavior. Validation is performed before the request reaches the handler, and failures throw a `PlaxionMediatorValidationException`.
- **FluentValidation adapter**: `PlaxionMediator.Validation.FluentValidation` package allows using existing `FluentValidation` validators seamlessly within the PlaxionMediator pipeline via `FluentValidationAdapter<TRequest>`.
- **ProblemDetails integration**: Validation failures now automatically surface as RFC 7807 `application/problem+json` responses (HTTP 400 Bad Request) when using `PlaxionMediator.AspNetCore`. The response body includes a structured list of validation errors with `propertyName` and `errorMessage` fields.
- **Sample WebApi updates**: `samples/PlaxionMediator.Sample.WebApi` now demonstrates global validation behavior and FluentValidation integration for its CRUD endpoints.
- **New Caching and Retry packages**:
  - **`PlaxionMediator.Caching`**: Introduces `ICacheableRequest<TResponse>` and `CachingBehavior<TRequest, TResponse>`. Provides an `IPlaxionMediatorCacheInvalidator` for manual invalidation. Uses `Microsoft.Extensions.Caching.Memory` for the default implementation.
  - **`PlaxionMediator.Retry`**: Introduces `IRetryableRequest` and `RetryBehavior<TRequest, TResponse>` with support for `Constant` and `Exponential` backoff strategies.
- **Extensive test coverage**: 93 new tests added across the validation, caching, and retry ecosystem:
  - 23 unit tests for the core `PlaxionMediator.Validation` logic.
  - 13 unit tests for the `PlaxionMediator.Validation.FluentValidation` adapter.
  - 23 unit tests for `PlaxionMediator.Caching`.
  - 21 unit tests for `PlaxionMediator.Retry`.
  - 13 new integration tests in `PlaxionMediator.Sample.WebApi.Tests` (10 for validation, 2 for caching, 1 for retry).

### Changed
- **Simplified Pipeline API**: New extension methods `UsePlaxionMediatorValidationBehavior()`, `UsePlaxionMediatorCachingBehavior()`, and `UsePlaxionMediatorRetryBehavior()` on `PlaxionMediatorOptions` allow enabling global behaviors without referencing their internal open-generic types directly.
- All new packages (Validation, Caching, Retry) are **opt-in** and are **not** bundled transitively into the core `PlaxionMediator` package, following the project's zero-bloat philosophy.

## v0.3.1

### Changed
- **Renamed core dependency injection package**: `PlaxionMediator.DependencyInjection` is now simply **`PlaxionMediator`**. This change simplifies the package identity on NuGet.org and reduces boilerplate in `using` directives.
- Updated project structure, solution files, and all internal references to reflect the new `PlaxionMediator` naming convention.
- Migrated all source code namespaces, sample applications, and benchmarks to the new `PlaxionMediator` root namespace.
- Refreshed all documentation (READMEs, Wiki, architecture guides) with the updated package ID and code snippets.

## v0.3.0

### Added
- **Explicit notification publish strategy**: new `PublishStrategy` enum (`Sequential`, `Parallel`) and `[NotificationPublishStrategyAttribute(PublishStrategy...)]` in `PlaxionMediator.Abstractions`, letting each `INotification` type declare whether its handlers run one after another or concurrently. The source generator emits the matching dispatch code per notification type at compile time — there is no hidden runtime default, and omitting the attribute falls back to `Sequential`.
- **Streaming support hardened end-to-end**: new `IStreamRequest<TResponse>` / `IStreamRequestHandler<TRequest, TResponse>` abstractions and `ISender.CreateStream<TResponse>(...)`, with the source generator emitting stream dispatch code that propagates `CancellationToken` through to the handler and the returned `IAsyncEnumerable<T>` for backpressure-friendly, incremental consumption. `PlaxionMediator.Testing`'s `FakeSender` now supports stubbing streaming requests as well as `Send`.
- **New Roslyn analyzers** expanding the catalog (IDs `PlaxionMediator011`–`090`, alongside the existing `PlaxionMediator001`–`006`):
  - `PlaxionMediator011` — non-sealed handler (should be sealed to prevent subclassing that bypasses DI-registered behavior).
  - `PlaxionMediator020` — invalid behavior registration (type doesn't implement `IPipelineBehavior<,>`).
  - `PlaxionMediator021` — duplicate behavior registration for the same pipeline.
  - `PlaxionMediator022` — incorrect lifetime (a Singleton handler/behavior captures a Scoped/Transient dependency).
  - `PlaxionMediator031` — missing `CancellationToken` propagation to an awaited call that accepts one.
  - `PlaxionMediator032` — `CancellationToken.None` used where an ambient token is available.
  - `PlaxionMediator040` — `async void` handler/behavior method.
  - `PlaxionMediator041` — handler depends on `ISender` and sends a request of its own type (risk of infinite recursion).
  - `PlaxionMediator080` — new `[HighFrequencyAttribute]` marks a request as hot-path; too many pipeline behaviors attached triggers this diagnostic.
  - `PlaxionMediator081` — synchronous-only handler with no `await` (suggests `ValueTask.FromResult`).
  - `PlaxionMediator082` — behavior allocates a closure/collection per call in a hot path.
  - `PlaxionMediator083` — stream handler materializes the entire sequence (`List`/array/`ToList`/`ToArray`) before yielding, defeating `IAsyncEnumerable` streaming.
  - `PlaxionMediator090` — notification handler uses a fail-fast throw pattern incompatible with fan-out semantics.
- New `PlaxionMediator.Benchmarks` project (BenchmarkDotNet) covering `Send`, `Publish`, and stream dispatch paths, wired into the solution and given a build/list smoke step in CI.
- Sample apps (`PlaxionMediator.Sample.WebApi`, `PlaxionMediator.Sample.MinimalApi`) extended with sequential and parallel notification examples and a streaming endpoint, exercised by new integration tests.
- Generator snapshot tests covering both publish strategies and stream dispatch, and unit tests for every new analyzer (44 analyzer tests total).

### Changed
- `.github/workflows/ci.yml` updated to build/smoke-test the new `PlaxionMediator.Benchmarks` project alongside the existing test suite.

## v0.2.0

### Added
- New package **`PlaxionMediator.AspNetCore`**: `UsePlaxionMediatorExceptionHandling()` middleware that translates `PlaxionMediatorException` subtypes (`HandlerNotFoundException`, `PipelineExecutionException`) into RFC 7807 `application/problem+json` responses via `PlaxionMediatorProblemDetailsFactory`. Any other exception type is rethrown untouched, so unrelated application errors are never swallowed.
- New package **`PlaxionMediator.MinimalApis`**: `MapPlaxionMediatorPost/Get/Put/Delete/Patch<TRequest, TResponse>()` extension methods on `IEndpointRouteBuilder` for low-boilerplate Minimal API route mapping — `Post`/`Put`/`Patch` bind `TRequest` from the JSON body, `Get`/`Delete` bind from route/query values via `[AsParameters]`, and all of them call `ISender.Send` and return `TypedResults.Ok(response)`.
- Both new packages are **opt-in** (`dotnet add package PlaxionMediator.AspNetCore` / `PlaxionMediator.MinimalApis`) and are **not** pulled in transitively by `PlaxionMediator`, so console/worker apps that only need the DI bundle aren't forced to reference ASP.NET Core.
- Two new Roslyn analyzers in `PlaxionMediator.Analyzers`: `PlaxionMediator005` (warns when a `MapPlaxionMediatorGet`/`MapPlaxionMediatorDelete` request type has no bindable route/query members) and `PlaxionMediator006` (warns on blocking `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` calls inside request/notification handlers).
- New sample app `samples/PlaxionMediator.Sample.WebApi`: a full `Item` CRUD API (`POST`/`GET` single/list/`PUT`/`PATCH`/`DELETE`) demonstrating both new packages end-to-end, with `UsePlaxionMediatorExceptionHandling()` wired in and `PublishAot` support.
- New test projects `PlaxionMediator.AspNetCore.Tests`, `PlaxionMediator.MinimalApis.Tests`, and `PlaxionMediator.Sample.WebApi.Tests` (unit + `WebApplicationFactory`-based integration tests), plus new analyzer snapshot tests for `PlaxionMediator005`/`006`.
- Postman collections and a shared environment added under `postman-tests` for manually exercising both sample apps (`PlaxionMediator.Sample.MinimalApi` and `PlaxionMediator.Sample.WebApi`).
- Image-free `READMEpackage.md` (packed as each package's NuGet `README.md`) since NuGet.org doesn't render the logo embedded in the main GitHub `README.md`; a GitHub Wiki page set was added under `docs/wiki`.

### Changed
- `publish.yml` now allows pre-release/preview tags (e.g. `v0.2.0-preview.1`) to be published to NuGet from any branch, while stable (non-hyphenated) tags still require the tagged commit to be on `master`.

## v0.1.4

### Added
- `PlaxionMediator.Testing` moved from `test/` to `src/` and is now shipped as its own NuGet package, referenced transitively by `PlaxionMediator` — installing the DI package now automatically pulls in the testing helpers (e.g. `FakeSender`) for consumers.
- Added a `FakeSender` implementation to `PlaxionMediator.Testing` to make it easier to unit test code that depends on `ISender`.
- Official project website `https://plaxion.dev` added as the `PackageProjectUrl` for all NuGet packages (GitHub remains the `RepositoryUrl`).
- Branded logo/icon added to all NuGet packages (`PackageIcon`) and to the main `README.md`.
- Added a dedicated `README.md` for `PlaxionMediator`, packed into the NuGet package (`PackageReadmeFile`), describing installation, usage, and related packages.

### Changed
- Solution structure updated so `PlaxionMediator.Testing` lives alongside the other shippable packages under `src/` instead of `test/`.

## v0.1.3

### Fixed
- CI/CD: `publish.yml` GitHub Actions workflow now verifies that the git tag being published exists on the `master` branch before publishing to NuGet, preventing accidental releases from other branches.

## v0.1.2

### Fixed
- Removed unnecessary analyzer-specific project reference metadata (`OutputItemType`/`ReferenceOutputAssembly`) from `PlaxionMediator`'s references to the source generator and analyzer projects, relying on `PrivateAssets="all"` alone for correct packaging.

## v0.1.1

### Added
- Initial public release of **PlaxionMediator** — a from-scratch, Native AOT-safe request pipeline platform for developers, built on a zero-reflection, source-generator-first architecture.
- Core packages: `PlaxionMediator.Abstractions`, `PlaxionMediator.Core`, `PlaxionMediator.Pipeline`, `PlaxionMediator.SourceGenerators`, `PlaxionMediator.Analyzers`, and `PlaxionMediator`.
- `AddPlaxionMediator()` DI extension method for compile-time handler discovery and registration — no runtime reflection.
- Support for immutable requests/handlers (`IRequest<T>` / `IRequestHandler<T, TResponse>`), notifications/events, and pipeline behaviors.
- Compile-time diagnostics: missing or duplicate handlers are reported as build errors instead of runtime failures.
- Full Native AOT and trimming compatibility across all packages.
- GitHub Actions workflow (`publish.yml`) to automate NuGet package publishing.
- Migrated the project from the original "Conduit" framework name/namespaces to `PlaxionMediator` across the codebase and tests.
