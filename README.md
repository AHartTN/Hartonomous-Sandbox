# Hartonomous

Hartonomous is a SQL Server-native autonomous AI platform. The system stores embeddings, model weights, and provenance data directly in SQL Server 2025, couples them with CLR-implemented intelligence, and exposes the platform through ASP.NET Core services, background workers, and Blazor administration tools.

## Platform Highlights
- SQL-first substrate: Hartonomous.Core models atoms, embeddings, tensor coefficients, and provenance, with NetTopologySuite geometry columns (see src/Hartonomous.Data/Configurations/AtomConfiguration.cs).
- In-database intelligence: the SqlClrFunctions assembly (src/SqlClr) delivers vector math, dimensionality reduction, attention mechanisms, anomaly detection, and multimedia processing compiled for .NET Framework 4.8.1.
- Autonomous operations: Service Broker queues (scripts/setup-service-broker.sql) drive the Analyze → Hypothesize → Act → Learn loop backed by stored procedures such as sql/procedures/dbo.sp_Analyze.sql and sql/procedures/dbo.sp_Learn.sql.
- Provenance and governance: temporal tables (sql/tables/TensorAtomCoefficients_Temporal.sql) capture history, graph tables (sql/tables/graph.*) map causal relationships, and CLR types like AtomicStream persist auditable event streams.
- End-to-end services: .NET 10 projects provide the REST API (src/Hartonomous.Api), Blazor admin surface (src/Hartonomous.Admin), and workers for change-data capture and Neo4j sync (src/Hartonomous.Workers.*).

## Repository Layout
- src/Hartonomous.Api: ASP.NET Core 10 gateway exposing ingestion, search, and orchestration endpoints.
- src/Hartonomous.Admin: Blazor Server admin portal with telemetry and operational tooling.
- src/Hartonomous.Core and src/Hartonomous.Shared.Contracts: domain models, value objects, and shared contracts.
- src/Hartonomous.Data: Entity Framework Core 10 DbContext and model configuration (includes geometry, temporal, and FILESTREAM mappings).
- src/Hartonomous.Infrastructure: integrations (Service Bus, Neo4j, Azure storage), orchestration services, and performance tooling.
- src/Hartonomous.Core.Performance: ILGPU and BenchmarkDotNet harnesses for SIMD and accelerator testing.
- src/SqlClr: SQL Server CLR assembly source (SqlClrFunctions.csproj).
- src/Hartonomous.Database.Clr: packaging project for deploying CLR assemblies from the solution build.
- src/Hartonomous.Workers.CesConsumer and src/Hartonomous.Workers.Neo4jSync: background services for CDC ingestion and provenance sync; src/Neo4jSync retains the legacy console runner.
- sql/: schema, types, procedures, and verification scripts (vector search, provenance, inference orchestration, temporal tables).
- scripts/: PowerShell automation including deploy-database-unified.ps1 for one-click provisioning.
- deploy/: systemd unit files and server bootstrap scripts.
- tests/: unit, integration, database, and end-to-end suites tracked by Hartonomous.Tests.sln.
- docs/: architecture, deployment, performance, SIMD, and CLR research notes.

## Database and AI Substrate
- **Atoms and embeddings**: sql/tables/dbo.Atoms.sql and sql/tables/dbo.AtomEmbeddings.sql create the unified semantic store. Spatial indexes are generated via sql/procedures/Common.CreateSpatialIndexes.sql.
- **Tensor storage**: sql/tables/dbo.ModelStructure.sql and temporal TensorAtomCoefficients track weights with automatic history.
- **Autonomous loop**: Service Broker is enabled by scripts/setup-service-broker.sql; orchestration logic lives in procedures under sql/procedures/Autonomy.*, sql/procedures/dbo.sp_Act.sql, and related files.
- **Provenance**: Graph structures (graph.AtomGraphNodes.sql, graph.AtomGraphEdges.sql) and CLR types (src/SqlClr/AtomicStream.cs, src/SqlClr/ComponentStream.cs) capture lineage and replayable event streams.

## CLR Assembly
The CLR package (SqlClrFunctions) compiles under .NET Framework 4.8.1 and delivers:
- Vector math, aggregates, and SIMD-aware operations (VectorAggregates.cs, VectorOperations.cs, AdvancedVectorAggregates.cs).
- Dimensionality reduction and analytics (DimensionalityReductionAggregates.cs, TimeSeriesVectorAggregates.cs, Analysis/QueryStoreAnalyzer.cs).
- Transformer and attention helpers (TensorOperations/TransformerInference.cs, AttentionGeneration.cs).
- Multimedia and multimodal pipelines (AudioProcessing.cs, ImageProcessing.cs, MultiModalGeneration.cs).
- Spatial projection helpers (SpatialOperations.cs, Core/LandmarkProjection.cs) exposing high-dimensional embeddings as geometry.

Build with Visual Studio or dotnet (dotnet build src/SqlClr/SqlClrFunctions.csproj). Deploy assemblies via the unified script or sql/procedures/Common.ClrBindings.sql.

## Getting Started

### Prerequisites
- SQL Server 2025 Developer/Enterprise with CLR enabled, FILESTREAM configured, and graph features available.
- .NET 10 SDK and PowerShell 7+.
- Optional: Neo4j 5.x for graph synchronization, Azure resources for Event Hubs and Blob storage.

### Local Setup (PowerShell)
`pwsh
# Clone
git clone https://github.com/AHartTN/Hartonomous-Sandbox.git
cd Hartonomous

# Restore and build
dotnet restore Hartonomous.sln
dotnet build Hartonomous.sln -c Debug

# Provision database (creates schemas, deploys CLR, runs procedures)
./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"

# Apply EF Core migrations only (optional)
dotnet ef database update --project src/Hartonomous.Data/Hartonomous.Data.csproj --connection "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"
`
The deployment script is idempotent. Use -SkipFilestream, -SkipCLR, or -DryRun when needed.

### Run Services
`pwsh
# API
cd src/Hartonomous.Api
dotnet run

# Admin portal
cd ../Hartonomous.Admin
dotnet run

# Background workers (separate terminals)
cd ../Hartonomous.Workers.CesConsumer
dotnet run
cd ../Hartonomous.Workers.Neo4jSync
dotnet run
`

## Testing
`pwsh
# Full test matrix
dotnet test Hartonomous.Tests.sln

# Targeted suite
dotnet test tests/Hartonomous.UnitTests
`

## Documentation
- ARCHITECTURE.md: system and component overview.
- DATABASE_DEPLOYMENT_GUIDE.md: SQL Server configuration, CLR permissions, and fallback procedures.
- DEVELOPMENT.md: environment setup, build, run, and debugging workflows.
- API.md: REST surface area.
- docs/CLR_DEPLOYMENT.md: assembly deployment strategy and permission guidance.
- docs/DEPLOYMENT.md: infrastructure rollout playbooks (Linux and Windows).
- docs/PERFORMANCE_ARCHITECTURE_AUDIT.md: SIMD optimization findings and backlog.
- docs/SIMD_RESTORATION_STATUS.md: status of vector hot paths.
- docs/SQL_CLR_RESEARCH_FINDINGS.md: research notes backing CLR feature design.
- docs/EMERGENT_CAPABILITIES.md: OODA reflex catalogue and autonomous behaviors.
- docs/AZURE_ARC_MANAGED_IDENTITY.md: identity strategy for Arc-enabled SQL.

Refer to docs/document-list.txt for historical and archived material.

## Deployment Automation
- scripts/deploy-database-unified.ps1: single-entry deployment covering prerequisites, EF migrations, CLR assembly upload, Service Broker, and verification.
- scripts/deploy/*: specialized routines for CLR-only refresh, dependency mapping, and temporal verification.
- deploy/*.service and deploy/setup-hart-server.sh: production systemd units and server bootstrap instructions.

## License and Support
This repository is proprietary. See LICENSE for usage restrictions. For licensing or support inquiries, contact support@hartonomous.dev.
