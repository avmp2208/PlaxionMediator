# Monetization Strategy — Conduit Commercial Track

> Scope note: this document is **separate** from `docs/architecture/` and from the other `ReleaseProcess/` documents (`01-mvp-development-phases.md`, `02-nuget-org-publishing-process.md`, `03-full-development-phases-oss.md`, `04-full-development-phases-enterprise.md`). Those documents answer "what do we build and in what order." This document answers **"how does this actually make money"** — pricing, packaging, distribution, sales motion, and financial/legal mechanics for the commercial modules. It exists purely as a reference for later; nothing here needs to be acted on before the OSS track reaches `v1.0.0` (see the hard gate in `04-full-development-phases-enterprise.md`).

## 1. Monetization Philosophy

- **Open-core, never bait-and-switch.** The pipeline itself (`Conduit.Core`, `Conduit.Pipeline`, `Conduit.SourceGenerators`, `Conduit.DependencyInjection`, `Conduit.AspNetCore`, and every module classified OSS in `03-full-development-phases-oss.md`) stays free and MIT/Apache-2.0-licensed **forever**. Nothing that already shipped free is ever pulled behind a paywall in a later version — that single act destroys OSS trust permanently and is far more costly than any short-term revenue gain.
- **Sell to organizations, not individuals.** Every priced module (see the table in `04-full-development-phases-enterprise.md`) is valuable primarily at organizational scale (production debugging, fleet-wide policy, compliance, hosted analytics). Individual developers and small projects should never need to pay to be productive with Conduit.
- **The OSS project is the top of the funnel, not a loss leader to abandon.** Continued OSS investment (bug fixes, new analyzers, ecosystem integrations) after `v1.0.0` is a marketing/trust expense, not a cost center to minimize — it is what keeps the funnel filled with future commercial leads.

## 2. Packaging Model

| Tier | Contains | Distribution | Target buyer |
|---|---|---|---|
| **OSS (free)** | Everything in `03-full-development-phases-oss.md` | NuGet.org, MIT/Apache-2.0 | Any developer/team |
| **Pro Bundle** | `Conduit.Diagnostics.Pro`, `Conduit.Visualizer`, `Conduit.Observability`, `Conduit.Azure` | Private feed + license key | Teams running Conduit in production wanting better debugging/telemetry |
| **Platform Bundle** | `Conduit.PolicyEngine` | Private feed + license key | Platform/infra teams standardizing policy across many services |
| **Enterprise** | `Conduit.Enterprise` (multi-tenant policy, multi-datastore transactions, support SLA), plus everything in Pro + Platform bundled | Private feed + license key + signed contract | Large organizations with procurement, compliance, and support requirements |
| **Analytics (SaaS)** | `Conduit.Analytics` hosted service | Hosted web app, usage-tiered | Any paying customer wanting historical/SLA reporting |

Rationale for bundling instead of à la carte pricing: reduces buyer decision fatigue, matches how comparable OSS-core vendors package (Sentry, Datadog agent + paid features, HashiCorp OSS + Enterprise), and simplifies license-key entitlement checks to "bundle flags" rather than per-package SKUs.

## 3. Pricing Model (recommendations, to validate with real design partners before finalizing)

| Product | Pricing shape | Reasoning |
|---|---|---|
| Pro Bundle | Flat **per-organization annual subscription**, tiered by number of production deployments/environments (e.g., Small: ≤3 environments, Medium: ≤10, Large: unlimited) | Avoids per-seat friction that discourages whole-team adoption once one developer champions the purchase; environment count roughly tracks the value delivered (debugging complexity scales with deployment surface, not headcount). |
| Platform Bundle (`PolicyEngine`) | Flat **per-organization annual subscription**, tiered by number of services/policies managed | Value scales with the number of services a platform team is standardizing, not with individual developer count. |
| Enterprise | **Custom annual contract**, negotiated, includes support SLA terms | Enterprise procurement expects negotiated contracts, not self-service pricing; also the only tier that includes a services/support component, which isn't a fixed-cost SKU. |
| Analytics (SaaS) | **Usage-tiered subscription** (e.g., priced by ingested trace volume/retention period, monthly bands) | Has genuine variable hosting cost — usage-based pricing is the standard SaaS shape and keeps margins predictable as usage grows. |

- Offer a **free trial** (30–60 days, full-featured, self-service license key) for the Pro and Platform bundles — lowers the barrier for a developer to convince their organization, without a sales call. Enterprise and Analytics remain sales-assisted given their higher price point and negotiation/hosting needs.
- Publish **list pricing** for Pro/Platform bundles publicly (transparent, self-service, matches expectations set by the OSS positioning); keep Enterprise pricing "contact us" as is standard for negotiated contracts.

## 4. Distribution & License Enforcement

- Commercial packages are distributed via a **private NuGet feed** (Azure Artifacts or GitHub Packages), never NuGet.org — keeps the public feed 100% OSS and avoids any perception of "OSS project secretly gating features."
- **License key validation** happens at DI registration time (e.g., `AddConduitDiagnosticsPro(licenseKey)`), fails fast and loud with an actionable error message on missing/invalid/expired keys, and performs **no per-request network calls** — validation is local (signed license blob, checked against an embedded public key) with only a periodic (e.g., daily) background revalidation ping, consistent with Conduit's zero-hidden-runtime-cost principle even in paid packages.
- License keys are organization-scoped (not machine-scoped), so scaling out additional instances/environments within an entitled tier never requires a support ticket.

## 5. Sales & Go-to-Market Motion

- **Phase 1 (Pro/Platform bundles): product-led growth.** Self-service trial → self-service purchase (Stripe or Paddle for payment/tax handling) → automated license key issuance. No sales team needed at this stage; the OSS project itself is the primary marketing channel (README, docs site, `Conduit.Visualizer` demo video called out in `04-full-development-phases-enterprise.md` Phase 2).
- **Phase 2 (Enterprise/Analytics): sales-assisted.** Once there are Pro/Platform customers, identify the largest/most engaged ones as Enterprise sales targets. Enterprise deals require a real conversation (support SLA terms, procurement/security questionnaires) — do not attempt to self-service this tier.
- **Design partners before general availability**: for every new commercial module (Phases 1–7 in `04-full-development-phases-enterprise.md`), recruit 1–3 active OSS users as free/discounted design partners before public pricing goes live — validates willingness to pay and pricing assumptions with real usage data before committing to a price sheet.
- **Marketing channel priority**: technical content (blog posts on the source-generator architecture, benchmark results vs. alternatives) and conference talks are higher-leverage than paid ads for a developer-tool audience; budget for paid acquisition only after organic/content channels are exhausted.

## 6. Financial & Legal Mechanics

- **Legal entity**: required before the first invoice — see the prerequisite already noted in `04-full-development-phases-enterprise.md`. Revisit entity type (LLC vs. C-Corp) if outside investment or a co-founder structure becomes relevant later; an LLC is sufficient for self-funded early revenue.
- **Payment processing**: use a merchant-of-record platform (e.g., Stripe Billing + Stripe Tax, or Paddle/LemonSqueezy if a full merchant-of-record — handling international VAT/sales tax automatically — is preferred over the extra tax-compliance burden of using Stripe alone) for the self-service Pro/Platform tiers; direct invoicing for negotiated Enterprise contracts.
- **Revenue recognition**: annual subscriptions should be recognized ratably over the contract term (standard SaaS/subscription accounting), not upfront — plan basic bookkeeping/accounting support (or software like an accountant-recommended tool) once contracts exist, this is a compliance requirement, not optional.
- **EULA**: draft a commercial End User License Agreement (distinct from the OSS MIT/Apache-2.0 license) covering license scope (per-organization, non-transferable), audit rights, liability limitations, and support terms — have this reviewed by a lawyer before the first paid contract, not after.
- **Trademark**: as already noted in `04-full-development-phases-enterprise.md`, file for the `Conduit` trademark once OSS traction is visible and before commercial sales begin in earnest — protects both the free and paid brand simultaneously.

## 7. Success Metrics to Track (from day one of the commercial track)

| Metric | Why it matters |
|---|---|
| OSS → trial conversion rate | Validates that the funnel from free users to paid trial is working before investing further in commercial features. |
| Trial → paid conversion rate | Validates pricing/packaging fit; a low rate here signals a pricing or feature-completeness problem, not a marketing problem. |
| Net revenue retention (NRR) | For a subscription business, expansion revenue (tier upgrades) mattering more than new-logo growth is the standard sign of durable SaaS economics. |
| Support ticket volume per paying customer | Enterprise support SLA commitments (Phase 7) must be sized against real support burden data gathered from the Pro/Platform tiers first. |
| Design-partner-to-GA time per module | Tracks whether the phased rollout described in `04-full-development-phases-enterprise.md` is actually de-risking each module launch as intended. |

---

This document intentionally contains no engineering task list — for the module-by-module build order, see `04-full-development-phases-enterprise.md`. This document should be revisited once real trial/pricing data exists from Phase 1 of the commercial track, since every number above is a starting hypothesis, not a commitment.
