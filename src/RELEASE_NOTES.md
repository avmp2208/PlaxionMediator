# Release Notes

All notable changes to `PlaxionMediator` and its companion packages are documented in this file.

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
- Both new packages are **opt-in** (`dotnet add package PlaxionMediator.AspNetCore` / `PlaxionMediator.MinimalApis`) and are **not** pulled in transitively by `PlaxionMediator.DependencyInjection`, so console/worker apps that only need the DI bundle aren't forced to reference ASP.NET Core.
- Two new Roslyn analyzers in `PlaxionMediator.Analyzers`: `PlaxionMediator005` (warns when a `MapPlaxionMediatorGet`/`MapPlaxionMediatorDelete` request type has no bindable route/query members) and `PlaxionMediator006` (warns on blocking `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` calls inside request/notification handlers).
- New sample app `samples/PlaxionMediator.Sample.WebApi`: a full `Item` CRUD API (`POST`/`GET` single/list/`PUT`/`PATCH`/`DELETE`) demonstrating both new packages end-to-end, with `UsePlaxionMediatorExceptionHandling()` wired in and `PublishAot` support.
- New test projects `PlaxionMediator.AspNetCore.Tests`, `PlaxionMediator.MinimalApis.Tests`, and `PlaxionMediator.Sample.WebApi.Tests` (unit + `WebApplicationFactory`-based integration tests), plus new analyzer snapshot tests for `PlaxionMediator005`/`006`.
- Postman collections and a shared environment added under `postman-tests` for manually exercising both sample apps (`PlaxionMediator.Sample.MinimalApi` and `PlaxionMediator.Sample.WebApi`).
- Image-free `READMEpackage.md` (packed as each package's NuGet `README.md`) since NuGet.org doesn't render the logo embedded in the main GitHub `README.md`; a GitHub Wiki page set was added under `docs/wiki`.

### Changed
- `publish.yml` now allows pre-release/preview tags (e.g. `v0.2.0-preview.1`) to be published to NuGet from any branch, while stable (non-hyphenated) tags still require the tagged commit to be on `master`.

## v0.1.4

### Added
- `PlaxionMediator.Testing` moved from `test/` to `src/` and is now shipped as its own NuGet package, referenced transitively by `PlaxionMediator.DependencyInjection` — installing the DI package now automatically pulls in the testing helpers (e.g. `FakeSender`) for consumers.
- Added a `FakeSender` implementation to `PlaxionMediator.Testing` to make it easier to unit test code that depends on `ISender`.
- Official project website `https://plaxion.dev` added as the `PackageProjectUrl` for all NuGet packages (GitHub remains the `RepositoryUrl`).
- Branded logo/icon added to all NuGet packages (`PackageIcon`) and to the main `README.md`.
- Added a dedicated `README.md` for `PlaxionMediator.DependencyInjection`, packed into the NuGet package (`PackageReadmeFile`), describing installation, usage, and related packages.

### Changed
- Solution structure updated so `PlaxionMediator.Testing` lives alongside the other shippable packages under `src/` instead of `test/`.

## v0.1.3

### Fixed
- CI/CD: `publish.yml` GitHub Actions workflow now verifies that the git tag being published exists on the `master` branch before publishing to NuGet, preventing accidental releases from other branches.

## v0.1.2

### Fixed
- Removed unnecessary analyzer-specific project reference metadata (`OutputItemType`/`ReferenceOutputAssembly`) from `PlaxionMediator.DependencyInjection`'s references to the source generator and analyzer projects, relying on `PrivateAssets="all"` alone for correct packaging.

## v0.1.1

### Added
- Initial public release of **PlaxionMediator** — a from-scratch, Native AOT-safe request pipeline platform for developers, built on a zero-reflection, source-generator-first architecture.
- Core packages: `PlaxionMediator.Abstractions`, `PlaxionMediator.Core`, `PlaxionMediator.Pipeline`, `PlaxionMediator.SourceGenerators`, `PlaxionMediator.Analyzers`, and `PlaxionMediator.DependencyInjection`.
- `AddPlaxionMediator()` DI extension method for compile-time handler discovery and registration — no runtime reflection.
- Support for immutable requests/handlers (`IRequest<T>` / `IRequestHandler<T, TResponse>`), notifications/events, and pipeline behaviors.
- Compile-time diagnostics: missing or duplicate handlers are reported as build errors instead of runtime failures.
- Full Native AOT and trimming compatibility across all packages.
- GitHub Actions workflow (`publish.yml`) to automate NuGet package publishing.
- Migrated the project from the original "Conduit" framework name/namespaces to `PlaxionMediator` across the codebase and tests.
