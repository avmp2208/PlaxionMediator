# Conduit — Architecture Documentation

Conduit is a next-generation Request Pipeline framework for .NET, designed from first principles for .NET 10, Native AOT, and a source-generator-first compile-time model. This documentation set is the complete architectural design for the framework. No implementation code exists yet — this is a design-only deliverable.

## Reading Order

1. [Executive Summary](01-executive-summary.md)
2. [Product Vision](02-product-vision.md)
3. [High-Level Architecture](03-high-level-architecture.md)
4. [Solution Structure](04-solution-structure.md)
5. [Core Architecture](05-core-architecture.md)
6. [Public API](06-public-api.md)
7. [Internal Architecture](07-internal-architecture.md)
8. [Package Architecture](08-package-architecture.md)
9. [Dependency Injection](09-dependency-injection.md)
10. [Source Generator Architecture](10-source-generator-architecture.md)
11. [Roslyn Analyzer Architecture](11-roslyn-analyzer-architecture.md)
12. [Pipeline Architecture](12-pipeline-architecture.md)
13. [Diagnostics](13-diagnostics.md)
14. [Logging](14-logging.md)
15. [OpenTelemetry](15-opentelemetry.md)
16. [Validation](16-validation.md)
17. [Authorization](17-authorization.md)
18. [Caching](18-caching.md)
19. [Retry](19-retry.md)
20. [Transactions](20-transactions.md)
21. [Performance](21-performance.md)
22. [Benchmark Strategy](22-benchmark-strategy.md)
23. [Extensibility](23-extensibility.md)
24. [Versioning Strategy](24-versioning-strategy.md)
25. [Roadmap](25-roadmap.md)
26. [Risks](26-risks.md)
27. [Architecture Decision Records](adr/)

## Naming Conventions

- Framework name: **Conduit**
- Root namespace: `Conduit.*`
- OSS packages: `Conduit.Core`, `Conduit.Abstractions`, `Conduit.DependencyInjection`, `Conduit.Pipeline`, `Conduit.SourceGenerators`, `Conduit.Analyzers`, `Conduit.AspNetCore`, `Conduit.MinimalApis`, `Conduit.Aspire`, `Conduit.Testing`, `Conduit.Benchmarks`, `Conduit.Templates`
- Commercial (open-core) packages: `Conduit.Diagnostics.Pro`, `Conduit.Visualizer`, `Conduit.Analytics`, `Conduit.Enterprise`, `Conduit.Azure`, `Conduit.Observability`, `Conduit.PolicyEngine`
