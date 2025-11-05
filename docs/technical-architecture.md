# Technical Architecture

Hartonomous platform architecture: services, data stores, messaging contracts, and cross-cutting concerns.

## System Overview

```text
SQL Server CDC ──► CesConsumer ──► Service Broker Queue ──► Neo4jSync Worker ──► Neo4j
       ▲                 │                   │                     │                 
       │                 │                   └──────► Billing + Ledger ──────────────┘
       │                 │
 Admin UI / CLI ◄────────┴──────── Hartonomous.Infrastructure APIs
```

### Core Projects

| Project | Responsibility |
| --- | --- |
| `Hartonomous.Core` | Domain primitives: atoms, embeddings, billing entities, access policy contracts |
| `Hartonomous.Data` | EF Core `HartonomousDbContext`, entity configuration, and migrations (`AddBillingTables`, `EnrichBillingPlans`, etc.) |
| `Hartonomous.Infrastructure` | Implementations for billing, messaging, security, throttling, repositories, and SQL graph sync |
| `CesConsumer` | Listens to SQL CDC, enriches change events, publishes CloudEvents to Service Broker |
| `Neo4jSync` | Consumes broker messages, enforces policy/throttle, records billing, projects data into Neo4j |
| `ModelIngestion` | CLI for downloading, normalising, and registering AI models |
| `SqlClr` | CLR functions for vector/spatial interop and tensor processing |

## Data Stores

### SQL Server 2025

- **Multimodal atoms**: `Atoms`, `AtomEmbeddings`, `AtomicAudioSamples` store deduplicated content with vector data (`VECTOR`, `GEOMETRY`)
- **Billing schema**: `BillingRatePlans`, `BillingOperationRates`, `BillingMultipliers`, `BillingUsageLedger` managed via EF Core migrations
- **Messaging**: Service Broker queue (`HartonomousQueue`) handles internal domain events
- **Query Store**: Performance monitoring and analysis
- **CDC (Change Data Capture)**: Table change tracking consumed by `CesConsumer`

### Neo4j 5.x

- **Provenance graph**: Model, inference, knowledge, and event nodes maintained by `ProvenanceGraphBuilder`
- **Explainability**: Relationships encode atom, model, and operation contributions to inference results

## Messaging Architecture

**SQL Service Broker**

1. **Change capture**: SQL Server CDC emits row changes; `CdcRepository` in `CesConsumer` reads CDC tables and `CdcEventProcessor` packages them into CloudEvents-compatible `BaseEvent` objects
2. **Service Broker publish**: `SqlMessageBroker` publishes events to queue (`HartonomousQueue`) with conversation management, retry, and dead-letter routing via `SqlMessageDeadLetterSink`
3. **Resilience**: `ServiceBrokerResilienceStrategy` provides retry policies and circuit breaker semantics
4. **Dispatch**: `ServiceBrokerMessagePump` in `Neo4jSync` consumes messages and routes to handlers (model, inference, knowledge, generic) after policy and throttling evaluation
5. **Billing**: `UsageBillingMeter` resolves tenant rate plans via `SqlBillingConfigurationProvider`, applies multipliers, and persists `BillingUsageRecord` through `SqlBillingUsageSink`

## Security & Governance

- **Access Policies**: `AccessPolicyEngine` evaluates ordered `IAccessPolicyRule` implementations (e.g., `TenantAccessPolicyRule`) before handler execution
- **Throttling**: `InMemoryThrottleEvaluator` enforces rate limits defined in configuration (`SecurityOptions.RateLimits`)
- **Auditing**: Handler executions and billing records emit structured logs

## Graph Projection

- **AtomGraphWriter**: Updates SQL graph node/edge tables maintaining relational and graph view consistency with retry logic
- **Neo4j Handlers**: Each handler (`ModelEventHandler`, `InferenceEventHandler`, etc.) transforms CloudEvents into Neo4j nodes/relationships via `ProvenanceGraphBuilder`

## Extensibility

- **New content modalities**: Add EF Core entities + configuration, update `HartonomousDbContext`, create migration, extend `UsageBillingMeter` for new multiplier dimensions
- **Additional policy rules**: Implement `IAccessPolicyRule` and register in DI with appropriate ordering
- **Alternative messaging**: `IMessageBroker` abstraction allows transport substitution (requires equivalent resiliency implementation)

---

**See also**: [Deployment & Operations](deployment-and-operations.md), [API Reference](api-implementation-complete.md), [Billing Model](billing-model.md)

- **Messaging**: Limited to SQL Service Broker (no Azure Event Hubs)
- **CLR Deployment**: CLR code exists but assembly deployment status unknown
- **FILESTREAM**: Requires manual SQL Server instance configuration
- **In-Memory OLTP**: Requires manual filegroup configuration
- **Test Coverage**: Integration test status needs review (some tests failing)
- **Documentation**: See `docs/CURRENT_STATE.md` for comprehensive gaps between docs and implementation

## Known Gaps

- Automated testing is minimal; integration tests for billing, messaging, and Neo4j projections must be authored.
- Admin UI and CLI clients have limited coverage; expect to flesh them out in upcoming sprints.
- Observability dashboards reference telemetry but the dashboards themselves are not bundled.

This document should evolve as architecture changes.  Keep diagrams updated and prune sections that no longer reflect the deployed system.
