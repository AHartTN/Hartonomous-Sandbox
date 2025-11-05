# Hartonomous

**Database-native AI inference platform with SQL Server 2025 vector search, multimodal embeddings, and graph provenance**

Hartonomous treats SQL Server 2025 as a first-class AI inference engine. Models decompose into queryable rows, embeddings leverage native VECTOR types with spatial hybrid search, and provenance flows through Service Broker into Neo4j for full lineage tracking. The platform provides atomic content deduplication, CLR-accelerated vector operations, usage-based billing, and distributed worker services.

[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2025%20RC1-red)](https://www.microsoft.com/sql-server)
[![Neo4j](https://img.shields.io/badge/Neo4j-5.x-green)](https://neo4j.com/)

---

## Core Capabilities

- **Database-native inference**: AI models stored as queryable SQL rows; inference executes in T-SQL using native VECTOR operations
- **Hybrid vector search**: Spatial indexes (GEOMETRY) filter candidates with O(log n) complexity, exact vector distance reranks results
- **Atomic content storage**: Content-addressable atoms (SHA-256) with reference counting across text, image, audio, video modalities
- **CLR acceleration**: Custom UDTs (AtomicStream, ComponentStream) and aggregate functions (VectorAvg, CosineSimilarityAvg) for vector operations
- **Event-driven architecture**: SQL Service Broker messaging with Neo4j graph projection for provenance tracking

---

## Getting Started

### Prerequisites

- SQL Server 2025 with vector and spatial features enabled (`VECTOR`, `GEOMETRY`, SQL Service Broker)
- .NET 10 SDK
- Neo4j 5.x
- PowerShell 7+ (for deployment scripts)

### First Run

```powershell
# Clone and enter the repo
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous

# Restore tools and build
dotnet restore Hartonomous.sln
dotnet build Hartonomous.sln

# Apply database schema (includes billing tables)
dotnet ef database update --project src/Hartonomous.Data --startup-project src/Hartonomous.Infrastructure

# Seed foundational data (atoms, models, rate plans)
./scripts/deploy-database.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -TrustedConnection $true -Seed $true

# Run the Neo4j projection worker
cd src/Neo4jSync
dotnet run
```

See [Deployment & Operations](docs/deployment-and-operations.md) for Service Broker configuration and database setup.

---

## Documentation

| Area | Purpose |
| --- | --- |
| **[docs/README.md](docs/README.md)** | Documentation index and quick navigation |
| **[Business Overview](docs/business-overview.md)** | Product positioning, personas, and billing strategy |
| **[Technical Architecture](docs/technical-architecture.md)** | Services, data flow, messaging, and storage deep dive |
| **[Deployment & Operations](docs/deployment-and-operations.md)** | Environment setup, migrations, broker configuration, health checks |
| **[Development Handbook](docs/development-handbook.md)** | Local environment, coding guidelines, and testing strategy |
| **[Billing Model](docs/billing-model.md)** | Rate plans, multipliers, EF Core schema, and ledger semantics |

---

## Platform Map

```text
 ┌──────────────────┐      ┌──────────────────────┐
 │ Blazor Admin UI  │      │  ModelIngestion CLI  │
 └────────┬─────────┘      └─────────┬────────────┘
          │                          │
          ▼                          ▼
 ┌────────────────────────────────────────────┐
 │          Hartonomous.Infrastructure        │
 │  • Billing services & ledger               │
 │  • Service Broker messaging                │
 │  • Access policy + throttling              │
 │  • Atom graph writer + repositories        │
 └────────┬──────────────────────────┬────────┘
          │                          │
          ▼                          ▼
 ┌─────────────────────┐      ┌─────────────────────┐
 │ SQL Server 2025     │      │ Neo4j 5.x           │
 │ • Multimodal atoms  │      │ • Provenance graph  │
 │ • EF Core migrations│ <>   │ • Explanation paths │
 │ • Service Broker    │      │ • Usage analytics   │
 └─────────────────────┘      └─────────────────────┘
          ▲                          ▲
          │                          │
 ┌────────┴────────┐      ┌──────────┴────────┐
 │ CesConsumer      │      │ Neo4jSync Worker │
 │ (CDC → Broker)   │      │ (Broker → Graph) │
 └──────────────────┘      └──────────────────┘
```

### Key Services

| Project | Role |
| --- | --- |
| `Hartonomous.Core` | 32 domain entities (Atom, AtomEmbedding, Model, TensorAtom, etc.), 66 interfaces, value objects |
| `Hartonomous.Data` | EF Core DbContext with 31 entity configurations, billing schema migrations, SQL graph integration |
| `Hartonomous.Infrastructure` | 23 repositories, 12 services (AtomIngestion, ModelIngestion, InferenceOrchestrator), security, messaging |
| `CesConsumer` | CDC consumer converting SQL Server change streams into CloudEvents on Service Broker queues |
| `Neo4jSync` | Event dispatcher with policy enforcement, usage billing, provenance graph builder for Neo4j |
| `ModelIngestion` | CLI tool with Safetensors/ONNX/PyTorch/GGUF readers, model decomposition, atomic weight storage |
| `SqlClr` | CLR UDTs (AtomicStream, ComponentStream), vector aggregates (VectorAvg, VectorWeightedAvg), spatial/audio/image functions |
| `Hartonomous.Admin` | Blazor Server UI for model browsing, student extraction, operations monitoring |

## Feature Set

### Database-Native AI Inference
- SQL Server 2025 VECTOR(1998) with EF Core 10 integration
- Hybrid search: Spatial GEOMETRY indexes filter candidates, VECTOR_DISTANCE reranks results
- Multi-resolution search: SpatialCoarse → SpatialGeometry → exact vector
- 30+ stored procedures for ensemble inference, semantic search, spatial generation, deduplication
- CLR aggregate functions: VectorAvg, VectorSum, VectorMedian, VectorWeightedAvg, VectorStdDev, CosineSimilarityAvg
- SQL graph tables (dbo.AtomNodes, dbo.AtomEdges) with AtomGraphWriter sync

### Content-Addressable Storage
- Atomic deduplication: SHA-256 content hashing with reference counting
- Multimodal atoms: Text, Image, Audio, Video, SCADA with unified embedding storage
- CLR UDTs: AtomicStream (generation provenance), ComponentStream (bill-of-materials)
- Deduplication policies: Semantic similarity thresholds, hash-based exact match

### Model Decomposition
- Models-as-rows: Transformer layers, attention weights, tensor atoms queryable via SQL
- Model ingestion: Safetensors, ONNX, PyTorch (.pt, .pth, .bin), GGUF formats
- Student model extraction: Query-based subsets by importance score
- Weight storage: TensorAtoms with SpatialSignature (GEOMETRY) for cross-model similarity

### Event-Driven Architecture
- Service Broker messaging: HartonomousQueue with conversation-scoped messages
- CDC to CloudEvents: CesConsumer enriches change streams, publishes to Service Broker
- Neo4j projection: ModelEventHandler, InferenceEventHandler, KnowledgeEventHandler, GenericEventHandler
- Provenance graph: Complete lineage from source atoms → embeddings → inferences → outputs
- Resilience patterns: Retry policies, circuit breaker, dead-letter routing

### Security & Governance
- Access policies: TenantAccessPolicyRule with ordered evaluation, deny-first semantics
- Throttling: Configurable rate limits per tenant/operation
- Usage billing: BillingRatePlans, BillingMultipliers (modality, complexity, content type, grounding, guarantee, provenance)
- Ledger tracking: BillingUsageLedger with operation metadata, DCU calculations

### Operational Tooling
- Blazor Admin UI: Model browser, student extraction, telemetry dashboard
- ModelIngestion CLI: Batch import with progress tracking
- Health monitoring: TelemetryHub with SignalR real-time updates
- Deployment automation: PowerShell scripts for schema versioning, index creation, seeding

---

## Contributing

1. Create a feature branch (`git checkout -b feature/billing-dashboard`)
2. Keep commits focused and include migrations or tests that demonstrate behaviour
3. Run the lint/test suite (`dotnet test`) before pushing
4. Open a pull request with context and screenshots/logs where applicable

See the [Development Handbook](docs/development-handbook.md) for coding standards and review checklist.

---

## Support & Contact

- File bugs or proposals on the GitHub issue tracker
- Join the team chat channel (`#hartonomous-core`) for day-to-day help
- Email the maintainers at `platform@hartonomous.ai`

---

© 2025 Hartonomous. All rights reserved. Built with ❤️ on .NET 10, SQL Server 2025, and Neo4j 5.x.
