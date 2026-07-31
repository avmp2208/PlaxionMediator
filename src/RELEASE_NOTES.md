# Release Notes

All notable changes to `PlaxionMediator` and its companion packages are documented in this file.

## v0.2.0

### Added
- New package **`PlaxionMediator.AspNetCore`**: `UsePlaxionMediatorExceptionHandling()` middleware that translates `PlaxionMediatorException` subtypes (`HandlerNotFoundException`, `PipelineExecutionException`) into RFC 7807 `application/problem+json` responses via `PlaxionMediatorProblemDetailsFactory`. Any other exception type is rethrown untouched, so unrelated application errors are never swallowed.
- New package **`PlaxionMediator.MinimalApis`**: `MapPlaxionMediatorPost/Get/Put/Delete/Patch<TRequest, TResponse>()` extension methods on `IEndpointRouteBuilder` for low-boilerplate Minimal API route mapping — `Post`/`Put`/`Patch` bind `TRequest` from the JSON body, `Get`/`Delete` bind from route/query values via `[AsParameters]`, and all of them call `ISender.Send` and return `TypedResults.Ok(response)`.
- Both new packages are **opt-in** (`dotnet add package PlaxionMediator.AspNetCore` / `PlaxionMediator.MinimalApis`) and are **not** pulled in transitively by `PlaxionMediator.DependencyInjection`, so console/worker apps that only need the DI bundle aren't forced to reference ASP.NET Core.
- Two new Roslyn analyzers in `PlaxionMediator.Analyzers`: `PlaxionMediator005` (warns when a `MapPlaxionMediatorGet`/`MapPlaxionMediatorDelete` request type has no bindable route/query members) and `PlaxionMediator006` (warns on blocking `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` calls inside request/notification handlers).
- New sample app `samples/PlaxionMediator.Sample.WebApi`: a full `Item` CRUD API (`POST`/`GET` single/list/`PUT`/`PATCH`/`DELETE`) demonstrating both new packages end-to-end, with `UsePlaxionMediatorExceptionHandling()` wired in and `PublishAot` support.
- New test projects `PlaxionMediator.AspNetCore.Tests`, `PlaxionMediator.MinimalApis.Tests`, and `PlaxionMediator.Sample.WebApi.Tests` (unit + `WebApplicationFactory`-based integration tests), plus new analyzer snapshot tests for `PlaxionMediator005`/`006`.
- Postman collections and a shared environment added under `docs/postman-tests` for manually exercising both sample apps (`PlaxionMediator.Sample.MinimalApi` and `PlaxionMediator.Sample.WebApi`).
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
- Initial public release of **PlaxionMediator** — a from-scratch, Native AOT-safe alternative to MediatR, built on a zero-reflection, source-generator-first architecture.
- Core packages: `PlaxionMediator.Abstractions`, `PlaxionMediator.Core`, `PlaxionMediator.Pipeline`, `PlaxionMediator.SourceGenerators`, `PlaxionMediator.Analyzers`, and `PlaxionMediator.DependencyInjection`.
- `AddPlaxionMediator()` DI extension method for compile-time handler discovery and registration — no runtime reflection.
- Support for immutable requests/handlers (`IRequest<T>` / `IRequestHandler<T, TResponse>`), notifications/events, and pipeline behaviors.
- Compile-time diagnostics: missing or duplicate handlers are reported as build errors instead of runtime failures.
- Full Native AOT and trimming compatibility across all packages.
- GitHub Actions workflow (`publish.yml`) to automate NuGet package publishing.
- Migrated the project from the original "Conduit" framework name/namespaces to `PlaxionMediator` across the codebase and tests.
