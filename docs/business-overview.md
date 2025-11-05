# Business Overview

Hartonomous packages the heavy lifting of multimodal AI infrastructure into a platform that enterprise data teams can operate themselves.  It emphasizes explainable inference, full provenance, and consumption-based billing so that downstream product teams can innovate without guessing where costs come from.

## Value Proposition

- **Unified multimodal store.** Text, image, audio, video, and tensor representations live in the same SQL Server tenant with deduplication and vector search.
- **Explainability through provenance.** Every inference, decision, and derived artifact can be traced back through Neo4j lineage graphs.
- **Operational transparency.** Usage-based billing and rate plans expose true costs per tenant, feature, and handler.
- **Enterprise integration.** Designed for Microsoft-first stacks (SQL Server 2025, .NET 10, Neo4j) with Service Broker messaging and audited security policies.

## Target Personas

| Persona | Needs | Platform Hooks |
| --- | --- | --- |
| Head of AI/ML | Scalable experimentation, compliance, explainability | Neo4j provenance, model ingestion pipeline |
| Platform Engineer | Stable infrastructure, operability, automation | Service Broker, EF Core migrations, deployment scripts |
| Finance / RevOps | Transparent chargebacks, contract alignment | Billing rate plans, multiplier catalog, usage ledger |
| Product Manager | Feature velocity with guardrails | ModelIngestion CLI, Blazor Admin UI, throttling & policy engine |

## Packaging & Monetisation

Hartonomous itself is an internal platform.  The new billing tables support several commercial motions:

1. **Consumption plans.** Default rate per operation with multipliers for modality, complexity, content type, grounding, guarantee tier, and provenance requirements.
2. **Plan subscriptions.** Each plan carries a code, named tier, monthly platform fee, unit price per DCU, and bundled storage/seat entitlements while enforcing private data access flags.
3. **Enterprise commitments.** Provisioning multiple rate plans per tenant allows negotiated pricing, contract variance, and experiment tracking.
4. **Internal chargeback.** Ledger data aggregates per tenant/operation and can be exported to ERP tooling.

Finance teams can extend the ledger by:

- Adding additional dimensions to the `BillingMultipliers` table (lookups via EF Core configuration and configuration defaults)
- Connecting external invoicing by streaming ledger entries into analytics pipelines
- Building dashboards on top of the admin UI's billing workspace

