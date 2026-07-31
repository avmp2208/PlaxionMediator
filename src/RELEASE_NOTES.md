# Release Notes

All notable changes to `PlaxionMediator` and its companion packages are documented in this file.

## v0.1.4 (Unreleased)

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
