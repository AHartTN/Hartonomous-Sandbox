# Hartonomous

> **Operational AI platform for multimodal content, explainable inference, and usage-based billing**

Hartonomous is an opinionated stack for teams that want SQL Server 2025, Neo4j, and .NET 10 working in concert.  It ingests and deduplicates atoms of text, audio, image, and video, tracks provenance across every inference, and now meters usage with first-class billing support.  The platform ships with a Service Broker message pipeline, Neo4j projection workers, and a growing surface of admin tooling.

[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2025-red)](https://www.microsoft.com/sql-server)
[![Neo4j](https://img.shields.io/badge/Neo4j-5.x-green)](https://neo4j.com/)

---

## Why Hartonomous?

- **Multimodal atoms** with deduplication, vector embeddings, and spatial context stored directly in SQL Server.
- **Event-driven graph projection** that mirrors SQL changes into Neo4j for full provenance and lineage analysis.
- **Usage-based billing** baked into EF Core with rate plans, multipliers, and ledger tracking.
- **Service Broker messaging** wrapped in resilience policies, dead-letter handling, and telemetry hooks.
- **.NET-first development** experience with clean repositories, DI-friendly services, and integration points for UI/CLI clients.

---

## Getting Started

### Prerequisites

- SQL Server 2025 with vector and spatial features enabled (`VECTOR`, `GEOMETRY`, SQL Service Broker)
- .NET 10 SDK
- Neo4j 5.x
- PowerShell 7+ (for scripts)
- Optional: Azure Event Hubs (or another CloudEvents-compatible broker)

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

> **Tip:** The Service Broker queue must be enabled on your SQL Server instance.  See [Deployment & Operations](docs/deployment-and-operations.md) for broker scripts and feature flags.

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
 │  Admin UI (WIP)  │      │  Thin Clients (CLI)  │
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
| `Hartonomous.Core` | Domain entities, interfaces, security models |
| `Hartonomous.Data` | EF Core DbContext, billing migrations, configuration maps |
| `Hartonomous.Infrastructure` | Billing, messaging, access policy, throttling, repositories |
| `CesConsumer` | Translates SQL CDC into CloudEvents on the Service Broker queue |
| `Neo4jSync` | Dispatches messages, enforces policy, bills usage, updates Neo4j |
| `ModelIngestion` | CLI import/export for AI models and embeddings |
| `SqlClr` | SQL CLR helpers for vector and spatial operations |

---

## Working Agreements

- **Code-first schema.** All changes originate in EF Core migrations; raw SQL DDL scripts are removed.
- **Telemetry-first.** Messaging, billing, and graph operations emit activity traces; keep traces intact when extending services.
- **Test debt acknowledged.** The current codebase is green for compilation but lacks automated coverage.  Add unit/integration/e2e tests alongside new features.

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
