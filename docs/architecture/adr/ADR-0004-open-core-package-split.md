# ADR-0004: Open-Core Package Split

## Status

Accepted

## Context

Conduit needs a sustainable long-term maintenance and development model. A purely OSS project relies on volunteer time or corporate sponsorship with no direct revenue tie to usage; a purely commercial product undermines the trust and adoption velocity that "de facto standard" status requires, especially for a compile-time-first framework that developers need to trust deeply (it changes their build).

## Decision

Split the framework into a permanently free, MIT-licensed OSS core (dispatch, pipeline, DI, source generators, analyzers, core cross-cutting behaviors) and a set of commercial "Pro/Enterprise" packages (`Conduit.Diagnostics.Pro`, `Conduit.Visualizer`, `Conduit.Analytics`, `Conduit.Enterprise`, `Conduit.Azure`, `Conduit.Observability`, `Conduit.PolicyEngine`) that extend — never gate — the core functionality. The dependency direction is structurally enforced: commercial packages depend on OSS packages, never the reverse.

## Alternatives Considered

1. **Fully OSS, no commercial tier** — rejected: no sustainable funding model for long-term maintenance at the scope this framework requires (26+ documented modules, ongoing Native AOT/analyzer maintenance as .NET evolves).
2. **Fully commercial (paid framework)** — rejected: directly undermines the "become the de facto standard" goal; developers overwhelmingly reject paying for foundational infrastructure they can't fully evaluate/trust first, especially one that generates code into their build.
3. **Open-core split with commercial value concentrated in scale-oriented tooling (chosen)** — the pipeline itself (what every user needs to function) stays free forever; commercial value is created in tooling primarily useful to organizations operating at scale (visualization, analytics, governance, cloud-vendor integration).

## Tradeoffs

- Requires ongoing discipline to keep the OSS core genuinely complete and uncrippled — a "crippled OSS core" strategy (common failure mode of open-core models) would undermine trust and adoption; this ADR commits to the OSS core being a fully production-viable framework on its own.
- Commercial packages must maintain their own compatibility promises against an independently-versioned OSS core ([Versioning Strategy](../24-versioning-strategy.md)), adding cross-repository coordination overhead the team must budget for.
- Some feature-placement decisions (e.g., is "basic tracing" OSS or Pro?) require ongoing judgment calls as the product evolves — this ADR does not eliminate that ambiguity, only establishes the guiding principle ("infrastructure is free, insight-at-scale is paid") used to resolve it consistently (documented per-module in [Package Architecture](../08-package-architecture.md)).

## Consequences

- Structural CI enforcement (dependency-graph check) prevents accidental coupling of OSS packages to commercial ones, protecting the "free forever" promise from being silently violated.
- The OSS core's roadmap (Phases 1–3) is prioritized ahead of the commercial tier (Phase 4) specifically to build adoption/trust before monetization, per [Roadmap](../25-roadmap.md).
- Licensing enforcement (`IConduitLicense`) is designed as a one-time, startup-only check to avoid any runtime performance tax on the commercial tier, preserving the framework's core performance promise even for paying customers.
