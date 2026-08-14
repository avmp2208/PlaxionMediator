# Migration Guide: v0.9.x → v1.0.0

This guide accompanies the `v1.0.0` "Stable Contract / Compatibility Baseline" release (see the engineering plan `v1.0.0-engineering-plan-v2.md` and `ADR-0013`). It exists to carry every intentional breaking change discovered during the pre-1.0 final API review.

## How to use this guide

1. Read the [Versioning Policy](Versioning-Policy.md) first to understand what "breaking" means per compatibility dimension.
2. Check the Breaking Change Ledger below for anything affecting the packages you consume.
3. Apply the migration instructions for each affected item.
4. Re-run your test suite against `1.0.0`.

## Breaking Change Ledger

As of this writing, the final pre-1.0 API review (`v1.0.0-engineering-plan-v2.md`, Sections 8–10) has not yet completed a full package-by-package classification pass. This ledger will be populated with one row per intentional breaking change as that review executes. The table structure below is the required, authoritative format — every row must be filled in before the `1.0.0` API/binary baseline is captured (per the Breaking-Change Cleanup sequence in `ADR-0013`/the engineering plan).

| Package | Previous API/behavior | New API/behavior | Reason | Migration instructions | Automated fix available? | Source impact | Binary impact |
|---|---|---|---|---|---|---|---|
| Core | `HandlerFaultException` (public) | `HandlerFaultException` (internal) | Implementation detail; should not be caught by consumers. | Remove any `catch (HandlerFaultException)` blocks. Catch `PlaxionMediatorException` instead if necessary. | No | Breaking | Breaking |
| Validation | `ValidationBehavior<,>` (public) | `ValidationBehavior<,>` (internal) | Only intended for registration via extension methods. | Use `.UsePlaxionMediatorValidationBehavior()` instead of manual registration. | No | Breaking | Breaking |
| Caching | `CachingBehavior<,>` (public) | `CachingBehavior<,>` (internal) | Only intended for registration via extension methods. | Use `.UsePlaxionMediatorCachingBehavior()` instead of manual registration. | No | Breaking | Breaking |
| Retry | `RetryBehavior<,>` (public) | `RetryBehavior<,>` (internal) | Only intended for registration via extension methods. | Use `.UsePlaxionMediatorRetryBehavior()` instead of manual registration. | No | Breaking | Breaking |
| Retry | `CircuitBreakerBehavior<,>` (public) | `CircuitBreakerBehavior<,>` (internal) | Only intended for registration via extension methods. | Use `.UsePlaxionMediatorCircuitBreakerBehavior()` instead of manual registration. | No | Breaking | Breaking |
| Authorization | `AuthorizationBehavior<,>` (public) | `AuthorizationBehavior<,>` (internal) | Only intended for registration via extension methods. | Use `.UsePlaxionMediatorAuthorizationBehavior()` instead of manual registration. | No | Breaking | Breaking |
| Transactions | `TransactionBehavior<,>` (public) | `TransactionBehavior<,>` (internal) | Only intended for registration via extension methods. | Use `.UsePlaxionMediatorTransactionBehavior()` instead of manual registration. | No | Breaking | Breaking |

## Upgrade considerations

If you are upgrading from the latest `0.9.x` release, ensure you:

- Review the ledger above for any internalizations affecting your custom pipeline configurations.

- Reviewing the [Versioning Policy](Versioning-Policy.md) for the new, more precise compatibility guarantees `1.0.0` introduces.
- Confirming your project's target framework appears in the supported platform matrix once published.

## 0.x support after 1.0.0

Per `ADR-0013`, `0.x` releases receive no formal maintenance commitment once `1.0.0` ships. There is no dedicated `v0.9.x` security-backport branch. Please migrate to `1.0.0` (or later `1.x`) to continue receiving fixes.

## Reporting a missed breaking change

If you discover a behavior, signature, package-dependency, diagnostic, generator, or AOT/trimming change between your `0.9.x` version and `1.0.0` that is not listed in the Breaking Change Ledger above, please open an issue — the ledger is the source of truth and must be corrected before `1.0.0` is considered release-qualified.
