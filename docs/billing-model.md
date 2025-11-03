# Billing Model

The November 2025 release introduces native billing support across the Hartonomous platform.  This document explains the schema, EF Core configuration, and runtime flow so product and finance teams can rely on accurate usage data.

## Schema Overview

```text
BillingRatePlans (RatePlanId, TenantId, Name, DefaultRate, IsActive, CreatedUtc, UpdatedUtc)
  ├─ BillingOperationRates (OperationRateId, RatePlanId, Operation, Rate, IsActive, CreatedUtc, UpdatedUtc)
  └─ BillingMultipliers (MultiplierId, RatePlanId, Dimension, Key, Multiplier, IsActive, CreatedUtc, UpdatedUtc)

BillingUsageLedger (LedgerId, TenantId, PrincipalId, Operation, MessageType, Handler, Units, BaseRate, Multiplier, TotalCost, MetadataJson, TimestampUtc)
```

All tables are managed by the EF Core migration `20251103040827_AddBillingTables`.  They live in the default schema (`dbo`) and should only be modified through migrations.

## EF Core Configuration

- **Entities.** `BillingRatePlan`, `BillingOperationRate`, and `BillingMultiplier` live in `Hartonomous.Core.Entities`.
- **DbContext.** `HartonomousDbContext` exposes `DbSet` properties for each entity.
- **Configurations.** Fluent mappings reside under `Hartonomous.Data/Configurations` to enforce defaults (`SYSUTCDATETIME()` timestamps, uniqueness filters on active rows, etc.).
- **Ledger Writes.** `BillingUsageRecord` objects are written via `SqlBillingUsageSink`.  Decimal columns use precision `(18,6)`.

## Runtime Flow

1. `EventDispatcher` receives a Service Broker message.
2. `AccessPolicyEngine` and `InMemoryThrottleEvaluator` validate tenant and rate limits.
3. `UsageBillingMeter` pulls the effective rate plan for the tenant using `SqlBillingConfigurationProvider`.
   - Falls back to default plan when a tenant-specific plan is missing.
   - Applies multipliers for generation type, complexity, and content type based on CloudEvent metadata.
4. A `BillingUsageRecord` is produced and persisted to `BillingUsageLedger` with computed `TotalCost`.
5. Ledger entries can be exported or aggregated for invoicing/chargeback workflows.

## Configuration

Sample `appsettings.json` snippet:

```json
"Billing": {
  "DefaultRate": 0.0105,
  "OperationRates": {
    "neo4j_sync.model_updated": 0.025,
    "neo4j_sync.inference_completed": 0.04,
    "neo4j_sync.ingest_completed": 0.018
  },
  "GenerationTypeMultipliers": {
    "text": 1.0,
    "image": 3.0,
    "audio": 2.2,
    "video": 3.8
  },
  "ComplexityMultipliers": {
    "standard": 1.0,
    "premium": 1.5,
    "enterprise": 2.0
  },
  "ContentTypeMultipliers": {
    "knowledge_graph": 1.2,
    "time_series": 1.4,
    "spatial": 1.6
  }
}
```

> **Note:** These configuration values seed the in-memory cache but the authoritative data is in SQL tables.  Keep them in sync during deployments or provide admin tooling to edit rate plans.

## Extending the Model

- **New operations.** Add rows to `BillingOperationRates` for each operation (`operation` corresponds to `AccessPolicyContext.Operation`).
- **Additional dimensions.** Extend `BillingMultiplier.Dimension` enumeration (string) and update `UsageBillingMeter.ResolveMultiplier` to interpret new metadata keys.
- **Promotions / discounts.** Consider adding validity windows or priority flags to the multiplier table; use filtered indexes to keep active rows unique.

## Reporting & Analytics

- Build SQL views aggregating totals per tenant/principal/operation.
- Export ledger rows to a data warehouse daily; include metadata JSON for downstream enrichment.
- Consider integrating Power BI or Grafana dashboards for finance teams.

## Data Retention

- Ledger growth can be significant.  Implement an archival job (SQL Agent, Azure Function) that moves data older than 12 months into cold storage.
- Ensure retention jobs maintain referential integrity (rate plans referenced in ledger entries should not be deleted).

Keep this document aligned with any schema or multiplier changes.  Update the diagrams and runtime description whenever the billing pipeline evolves.
