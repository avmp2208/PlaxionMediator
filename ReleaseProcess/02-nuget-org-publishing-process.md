# Publishing Conduit as an Open Source Project on NuGet.org

> Scope note: this document is **separate** from `docs/architecture/` and from `01-mvp-development-phases.md`. That other document answers "what do I build, in what order". This document answers "once it's built, what are the concrete mechanical/administrative steps to legally and safely publish it as an open source NuGet package under my name/organization". This is a personal operational checklist.

## 1. Pre-Publishing Decisions (do these once, before the first package push)

| Decision | Recommendation | Why |
|---|---|---|
| License | **MIT** (or Apache-2.0 if you want explicit patent grant language) | Both are OSI-approved, permissive, and are what >90% of successful .NET OSS packages use (`Microsoft.Extensions.*`, `Serilog`, `Polly` all use MIT/Apache-2.0). Avoid GPL-family for a library — it scares away commercial adopters, which conflicts with your later open-core plan. |
| Package ID prefix / ownership | Reserve the `Conduit.*` ID prefix on NuGet.org early | NuGet.org supports **package ID prefix reservation** once you've published at least one package matching the prefix and verified you own it — this stops namesquatting on `Conduit.Diagnostics.Pro` etc. before your commercial tier ships. |
| Repository visibility | Public GitHub repo, `main` branch protected | NuGet.org listings link to the repo; a private/missing repo undermines OSS trust. |
| Organization vs personal account | Create a **NuGet.org organization** (e.g. `conduit-dotnet`) rather than publishing under your personal account | Lets you add co-maintainers later, separate personal identity from project identity, and matches how `MediatR`, `Refit`, `FluentValidation` are published. |
| Versioning scheme | Strict SemVer 2.0, starting at `0.1.0` (not `1.0.0`) | Signals "API may still shift" honestly; `docs/architecture/24-versioning-strategy.md` already documents the long-term SemVer policy — this just confirms the starting number. |
| Signing | Not required for MVP; consider strong-naming + NuGet package signing once the project has real adoption | NuGet.org doesn't require signed packages for open publishing; author signing adds friction with limited payoff pre-v1. |

## 2. Account & Access Setup

1. Create/verify a **Microsoft account** (personal or org) to sign in to [nuget.org](https://www.nuget.org).
2. Create the NuGet.org **organization** for the project (Profile → Organizations → Add) and add any co-maintainers as **Owners** or **Package Managers** (Package Managers can push new versions but can't manage ownership/API keys — use this role for outside contributors doing releases).
3. Enable **2FA (Required Two-Factor Authentication)** for the organization — NuGet.org requires this for accounts that own popular packages, and enabling it up front avoids being force-migrated later.
4. In GitHub, connect the repository as the package's **Repository URL** metadata source (used later in the `.csproj`/`Directory.Build.props`).

## 3. API Key Strategy

Never use your NuGet.org password/PAT interactively in CI. Use scoped **API keys**:

| Key | Scope | Where it lives |
|---|---|---|
| CI publish key | `Push new packages and package versions`, glob-scoped to `Conduit.*` | GitHub Actions repository secret (`NUGET_API_KEY`), never committed |
| Personal manual-publish key (optional, for emergencies) | Same scope, shorter expiration (e.g. 90 days) | Local secrets manager, not in the repo |

- Set an **expiration** on every key (NuGet.org allows up to 365 days) and put a calendar reminder to rotate it — expired keys fail CI loudly and safely, which is preferable to keys that never expire.
- Glob-scope keys to `Conduit.*` so a leaked key can't be used to push unrelated packages under your account.

## 4. Required Package Metadata (per `.csproj` / centralized in `Directory.Build.props`)

```xml
<PropertyGroup>
  <PackageId>Conduit.Core</PackageId>
  <Version>0.1.0</Version>
  <Authors>Your Name or Org</Authors>
  <Company>Conduit</Company>
  <Description>One-sentence description of this specific package.</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/&lt;org&gt;/conduit</PackageProjectUrl>
  <RepositoryUrl>https://github.com/&lt;org&gt;/conduit</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageTags>dotnet;request-pipeline;cqrs;source-generator;native-aot</PackageTags>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)'=='true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

- Every `src/*` project that ships publicly needs its own `README.md` (short, package-specific) referenced via `PackageReadmeFile` — NuGet.org renders this on the package page. The repo-root `README.md` can be longer/more general.
- `Conduit.SourceGenerators` and `Conduit.Analyzers` additionally need `<DevelopmentDependency>true</DevelopmentDependency>` and the standard analyzer-packing targets (`<IncludeBuildOutput>false</IncludeBuildOutput>` + explicit `<None Include=... Pack="true" PackagePath="analyzers/dotnet/cs" />`) so consumers don't get a runtime dependency on Roslyn.
- Enable **deterministic builds** and **source link** (`Microsoft.SourceLink.GitHub` package reference) so consumers can step into Conduit's source from their debugger — a strong trust signal for a new OSS library.

## 5. Pre-Publish Verification Checklist (run for every release, not just the first)

- [ ] `dotnet pack -c Release` produces `.nupkg` + `.snupkg` for every package with no warnings.
- [ ] `dotnet nuget verify` / manual inspection of the `.nupkg` contents (`nupkg` is a zip — confirm README, license, and correct `lib/net10.0/` target folder are present).
- [ ] Local smoke test: `dotnet add package Conduit.Core --source <local-feed-folder>` in a throwaway project, confirm it actually works before pushing to nuget.org (nuget.org pushes are **effectively permanent** — you can unlist but not delete).
- [ ] `PublishAot=true` smoke build succeeds against the packed (not project-referenced) NuGet packages, not just the source — packaging bugs (missing analyzer folder targets, wrong TFM) are invisible when testing via `ProjectReference`.
- [ ] Version number bumped correctly per the SemVer policy in `docs/architecture/24-versioning-strategy.md`.
- [ ] `CHANGELOG.md` updated (Keep a Changelog format recommended) and a matching Git tag (`v0.1.0`) created.

## 6. Publishing Steps

1. **Local dry run** (recommended first time): `dotnet nuget push **/*.nupkg --source https://apiint.nugettest.org/v3/index.json --api-key <int-test-key>` against the NuGet.org **integration test feed** to validate metadata rendering without touching production.
2. **CI-driven release** (recommended ongoing process): a GitHub Actions workflow triggered on pushing a `v*` tag that runs `dotnet pack`, then `dotnet nuget push **/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.NUGET_API_KEY }} --skip-duplicate`.
3. Push symbol packages alongside (`.snupkg` is auto-detected and pushed to `nuget.org`'s symbol server when using the same `dotnet nuget push` command with `IncludeSymbols=true`/`SymbolPackageFormat=snupkg`).
4. Verify the listing on `nuget.org/packages/Conduit.Core` — README rendering, license badge, dependency list, target framework (`net10.0`) all correct.
5. Create the corresponding **GitHub Release** from the tag, with release notes copied/linked from `CHANGELOG.md`, and attach a link to the NuGet.org listing.

## 7. Post-Publish: Package ID Reservation & Ownership Hygiene

- Once `Conduit.Core` (or any `Conduit.*` package) is live, go to **nuget.org → Manage Packages → Reserved Namespaces** and request the `Conduit.*` prefix reservation. This is what prevents someone else from publishing `Conduit.Something` later and confusing users — critical given the planned commercial-tier package names (`Conduit.Enterprise`, `Conduit.PolicyEngine`, etc.) that don't exist yet.
- Add at least one **co-owner** (trusted collaborator) to the NuGet.org organization — a single point of failure (only you can publish) is an operational risk for an OSS project expecting community contributions.
- Turn on **NuGet.org package vulnerability/deprecation notifications** for the org so you're alerted if a transitive dependency (e.g., a Resilience/Polly version used by `Conduit.Retry` later) is flagged.

## 8. Ongoing Release Cadence (post-MVP)

- Patch releases (`0.1.x`): bug fixes, non-breaking analyzer additions — can ship as soon as CI is green, no announcement needed beyond the changelog.
- Minor releases (`0.x.0`): new OSS modules/analyzers — batch into a short release note, cross-post to wherever the project is announced (see Phase 3 of `01-mvp-development-phases.md`).
- The `1.0.0` release is a **deliberate milestone**, not an automatic version bump — only cut it once the public API surface in `docs/architecture/06-public-api.md` is considered stable enough to commit to the binary/source compatibility guarantees described in `docs/architecture/24-versioning-strategy.md`.

## 9. Quick Reference: Minimum Commands

```powershell
# One-time: create API key on nuget.org UI first, then store as a GitHub secret.

# Pack every package for release
dotnet pack -c Release -o ./artifacts

# Manual push (normally done by CI instead)
dotnet nuget push .\artifacts\*.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY `
  --skip-duplicate
```
