# Technical Architecture

This document captures the current state of the Hartonomous platform as of November 2025.  It focuses on services, data stores, messaging contracts, and cross-cutting concerns.

## High-Level View

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
| `Hartonomous.Data` | EF Core `HartonomousDbContext`, entity configuration, and migrations (`AddBillingTables`, etc.) |
| `Hartonomous.Infrastructure` | Implementations for billing, messaging, security, throttling, repositories, and SQL graph sync |
| `CesConsumer` | Listens to SQL CDC, enriches change events, publishes CloudEvents to Service Broker |
| `Neo4jSync` | Consumes broker messages, enforces policy/throttle, records billing, projects data into Neo4j |
| `ModelIngestion` | CLI for downloading, normalising, and registering AI models |
| `SqlClr` | CLR functions for vector/spatial interop and tensor processing |

## Data Stores

### SQL Server 2025

- **Multimodal atoms.** Tables such as `Atoms`, `AtomEmbeddings`, `AtomicAudioSamples` store deduplicated content and vector data (`VECTOR`, `GEOMETRY`).
- **Billing schema.** New tables `BillingRatePlans`, `BillingOperationRates`, `BillingMultipliers`, `BillingUsageLedger` are managed via EF Core migrations.  Seeding and updates must go through the DbContext.
- **Messaging.** Service Broker queue (`HartonomousQueue` by default) is the backbone for domain events handled by downstream services.

### Neo4j 5.x

- **Provenance graph.** Nodes for models, inferences, knowledge documents, and generic events maintained by `ProvenanceGraphBuilder`.
- **Explainability.** Relationships encode which atoms, models, or operations contributed to an inference.

## Messaging Flow

1. **Change capture.** SQL Server CDC emits row changes; `CesConsumer` packages them into CloudEvents.
2. **Service Broker publish.** `SqlMessageBroker` encapsulates send/receive logic with automatic conversation management, retry, and dead-letter routing via `SqlMessageDeadLetterSink`.
3. **Resilience.** `ServiceBrokerResilienceStrategy` wraps publish/receive operations with retry policies and circuit breaker semantics.
4. **Dispatch.** `EventDispatcher` in `Neo4jSync` routes messages to specific handlers (model, inference, knowledge, generic) after evaluating access policies and throttling.
5. **Billing.** `UsageBillingMeter` constructs `BillingUsageRecord` objects written to the ledger through `SqlBillingUsageSink`.

## Security & Governance

- **Access Policies.** `AccessPolicyEngine` evaluates ordered `IAccessPolicyRule` implementations (e.g., `TenantAccessPolicyRule`) and can deny processing before any handler runs.
- **Throttling.** `InMemoryThrottleEvaluator` enforces rate limits defined in configuration (`SecurityOptions.RateLimits`).
- **Auditing.** All handler executions and billing records emit structured logs.  Extend the logging scopes rather than logging ad-hoc strings.

## Graph Projection

- **AtomGraphWriter.** Updates SQL graph node/edge tables keeping relational and graph views consistent.  Retries sync via stored procedure if writes fail.
- **Neo4j Handlers.** Each handler (`ModelEventHandler`, `InferenceEventHandler`, etc.) transforms CloudEvents into Neo4j nodes/relationships through `ProvenanceGraphBuilder`.

## Extensibility Points

- **New content modalities.** Add EF Core entities + configuration, update `HartonomousDbContext`, and create migration.  Ensure `UsageBillingMeter` understands new multiplier dimensions.
- **Additional policy rules.** Implement `IAccessPolicyRule` and register it in DI ahead of or behind existing rules as needed.
- **Alternative messaging transports.** `IMessageBroker` abstraction allows substituting Service Broker if necessary, but supporting resiliency policies will require equivalent implementations.

## Known Gaps

- Automated testing is minimal; integration tests for billing, messaging, and Neo4j projections must be authored.
- Admin UI and CLI clients have limited coverage; expect to flesh them out in upcoming sprints.
- Observability dashboards reference telemetry but the dashboards themselves are not bundled.

This document should evolve as architecture changes.  Keep diagrams updated and prune sections that no longer reflect the deployed system.
