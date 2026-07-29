# Contributing to Conduit

Thanks for your interest in contributing to Conduit!

## Prerequisites

- .NET 9 SDK or later
- A GitHub account

## Getting started

1. Fork and clone the repository.
2. Open `Conduit.sln` in your IDE of choice (Rider, Visual Studio, or VS Code + C# Dev Kit).
3. Restore and build:

```bash
dotnet restore Conduit.sln
dotnet build Conduit.sln
dotnet test Conduit.sln
```

## Project layout

| Path | Purpose |
|------|---------|
| `src/` | Product libraries (Abstractions, Core, Pipeline, DI, Generators, Analyzers, Testing) |
| `test/` | Unit tests mirrored 1:1 with `src/` |
| `samples/` | Runnable sample apps |
| `docs/architecture/` | Full long-term design documentation |
| `ReleaseProcess/` | MVP and release process docs |

## Coding standards

- File-scoped namespaces
- Prefer `sealed` classes/records unless inheritance is intentional
- Requests should be `sealed record` types implementing `IRequest<TResponse>`
- Nullable reference types are enabled; treat warnings as errors in `src/`
- Follow existing patterns in neighboring files

## Pull requests

1. Create a feature branch from `main`.
2. Keep changes focused and covered by tests.
3. Ensure `dotnet build` and `dotnet test` pass.
4. Open a PR with a clear description of *why* the change exists.

## Reporting issues

Please include:

- Conduit package version(s)
- Target framework
- Minimal reproduction (ideally a small project or snippet)
- Expected vs. actual behavior

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). By participating, you agree to uphold it.
