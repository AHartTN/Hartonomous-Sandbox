# Hartonomous

**Autonomous AGI-in-SQL-Server Platform**

Hartonomous is a database-first autonomous AI platform where SQL Server 2025 is the runtime, storage, and intelligence substrate. T-SQL stored procedures execute the full OODA loop (Observe → Orient → Decide → Act), CLR functions provide SIMD-accelerated inference and embedding generation, and .NET 10 services orchestrate ingestion, API access, and background workers. The database stores atoms (multimodal data units), embeddings (vector representations), tensor coefficients (model weights), and provenance graphs (dual-ledger temporal + Neo4j lineage).

## Platform Highlights

- **T-SQL as AI Interface**: Call `sp_Analyze`, `sp_Hypothesize`, `sp_Act`, `sp_Learn` directly from SQL to execute autonomous reasoning loops. Invoke CLR functions like `clr_RunInference`, `fn_ComputeEmbedding`, or `fn_GenerateText` for inference without leaving the database.
- **Database-First Architecture**: Core runtime is SQL Server 2025. Atoms, embeddings, tensor atoms, and model coefficients live in SQL tables with geometry columns (`SpatialKey`, `SpatialSignature`), temporal history (system-versioned tables), and graph relationships (SQL graph tables + Neo4j sync).
- **CLR Intelligence Layer**: `src/SqlClr` compiles to .NET Framework 4.8.1 assembly deployed in SQL Server. Provides SIMD vector aggregates, transformer attention, anomaly detection, multimodal generation, and stream orchestration called from stored procedures.
- **Autonomous OODA Loop**: Service Broker queues (`AnalyzeQueue`, `HypothesizeQueue`, `ActQueue`, `LearnQueue`) coordinate the four-phase loop. Stored procedures publish/consume messages, CLR helpers aggregate telemetry, and results are recorded in `AutonomousImprovementHistory`.
- **Dual Embedding Paths**: C# `EmbeddingService` (`src/Hartonomous.Infrastructure/AI/Embeddings/EmbeddingService.cs`) for HTTP ingestion. CLR `fn_ComputeEmbedding` for T-SQL embedding generation. Both persist to `dbo.AtomEmbeddings` with `VECTOR(1998)` columns and spatial geometry projections.
- **Unified Substrate**: Single `dbo.Atoms` table with modality metadata, JSON descriptors, geometry `SpatialKey`, and temporal columns. Embeddings, tensor atoms, and provenance link to atoms. No separate vector store—database is the vector store, graph store, and model repository.

## Prerequisites

- **SQL Server 2025** with CLR, FILESTREAM, and Service Broker enabled
- **.NET 10 SDK** for building `Hartonomous.sln` and `Hartonomous.Tests.sln`
- **PowerShell 7+** for running deployment scripts (`scripts/deploy-database-unified.ps1`)
- **Optional**: Neo4j 5.x for provenance graph sync, Azure AD tenant for authentication, Azure Storage for blob ingestion, OpenTelemetry endpoint for telemetry export

## Quick Start

### 1. Clone and Restore

```pwsh
git clone <repository-url> Hartonomous
cd Hartonomous
dotnet restore Hartonomous.sln
dotnet restore Hartonomous.Tests.sln
```

### 2. Build Solutions

```pwsh
# Build all projects (Debug)
dotnet build Hartonomous.sln -c Debug

# Build CLR assembly (targets .NET Framework 4.8.1)
dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release

# Build test suite
dotnet build Hartonomous.Tests.sln -c Debug
```

### 3. Provision Database

Run the unified deployment script to create schema, deploy CLR, and configure Service Broker:

```pwsh
./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"
```

**Options**:

- `-SkipFilestream`: Skip FILESTREAM setup
- `-SkipClr`: Skip CLR assembly deployment
- `-DryRun`: Show SQL commands without executing
- `-ConnectionString`: Override default connection string

This script:

- Enables CLR integration, FILESTREAM, and Service Broker
- Executes schema scripts from `sql/tables`, `sql/procedures`, `sql/functions`
- Deploys `SqlClrFunctions.dll` and registers CLR functions/aggregates/types
- Sets up Service Broker message types, contracts, queues, and services
- Runs verification scripts to validate installation

### 4. Run Services Locally

**API Server** (port 5000 by default):

```pwsh
cd src/Hartonomous.Api
dotnet run
```

**Admin Portal** (Blazor, port 5001 by default):

```pwsh
cd src/Hartonomous.Admin
dotnet run
```

**Background Workers**:

```pwsh
# CES consumer (CDC ingestion)
cd src/Hartonomous.Workers.CesConsumer
dotnet run

# Neo4j sync worker (Service Broker → Neo4j)
cd src/Hartonomous.Workers.Neo4jSync
dotnet run
```

**Configuration**: Each service reads connection strings and configuration from `appsettings.json` or environment variables. Default SQL connection: `Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;`

### 5. Run EF Core Migrations (Alternative)

If using EF Core migrations instead of raw SQL scripts:

```pwsh
dotnet ef database update --project src/Hartonomous.Data/Hartonomous.Data.csproj --connection "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"
```

### 6. Run Tests

```pwsh
# All tests
dotnet test Hartonomous.Tests.sln

# Unit tests only
dotnet test tests/Hartonomous.UnitTests

# Integration tests
dotnet test tests/Hartonomous.IntegrationTests

# Database validation tests
dotnet test tests/Hartonomous.DatabaseTests
```

## Project Structure

- **`src/Hartonomous.Api`**: ASP.NET Core REST API. Controllers expose ingestion, inference, search, provenance, and operations endpoints. Configured with Azure AD auth, rate limiting, OpenTelemetry.
- **`src/Hartonomous.Admin`**: Blazor Server admin portal with telemetry dashboards, atom browser, model management.
- **`src/Hartonomous.Workers.*`**: Background services (CES consumer for CDC, Neo4j sync for Service Broker message pump).
- **`src/Hartonomous.Core`**: Domain entities (`Atom`, `AtomEmbedding`, `TensorAtom`, `TensorAtomCoefficient`, `Model`, `ModelLayer`), value objects, interfaces.
- **`src/Hartonomous.Shared.Contracts`**: DTOs, request/response models shared across services.
- **`src/Hartonomous.Data`**: EF Core `DbContext`, entity configurations, migrations. Maps geometry columns, JSON columns, temporal tables, and relationships.
- **`src/Hartonomous.Infrastructure`**: Service registrations (`DependencyInjection.cs`), pipelines (ingestion, inference, provenance), messaging (event bus, Service Broker client), resilience policies, billing, security services.
- **`src/Hartonomous.Core.Performance`**: ILGPU harnesses and BenchmarkDotNet tests for SIMD validation.
- **`src/Hartonomous.Database.Clr`**: Packaging project for CLR assembly deployment.
- **`src/SqlClr`**: SQL CLR implementation (vector aggregates, transformer helpers, anomaly detection, multimodal processing, stream UDTs, Service Broker orchestrators). Compiles to `SqlClrFunctions.dll` targeting .NET Framework 4.8.1.
- **`sql/`**: Database schema scripts (`tables/`, `procedures/`, `functions/`), verification utilities, setup scripts (FILESTREAM, vector indexes, Service Broker).
- **`scripts/`**: PowerShell automation (database deployment, CLR refresh, dependency analysis).
- **`deploy/`**: Production deployment artifacts (systemd units, bootstrap script).
- **`docs/`**: Architecture, deployment, CLR guide, schema reference, Gödel Engine documentation.
- **`tests/`**: Unit, integration, database validation, end-to-end test suites.

## Example: T-SQL Inference

```sql
-- Generate embedding from text (CLR function)
DECLARE @embedding VECTOR(1998);
SET @embedding = dbo.fn_ComputeEmbedding('The quick brown fox jumps over the lazy dog', 'text-embedding-3-large');

-- Search for similar atoms (CLR aggregate + spatial index)
SELECT TOP 10 a.AtomId, a.Modality, a.ContentJson, 
       dbo.clr_VectorDistance(@embedding, e.EmbeddingVector) AS Distance
FROM dbo.Atoms a
INNER JOIN dbo.AtomEmbeddings e ON a.AtomId = e.AtomId
WHERE e.EmbeddingType = 'text-embedding-3-large'
ORDER BY Distance ASC;

-- Run multimodal inference (CLR stored procedure)
EXEC dbo.clr_RunInference 
    @modelName = 'gpt-4-vision',
    @inputJson = '{"text": "Describe this image", "imageUrl": "https://..."}',
    @outputJson = @result OUTPUT;

-- Trigger autonomous analysis (OODA loop entry point)
EXEC dbo.sp_Analyze 
    @observationJson = '{"metricName": "ResponseTime", "value": 1250, "threshold": 1000}';
```

## Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)**: Database-first design, CLR intelligence layer, OODA loop, dual embedding paths, schema overview, .NET 10 orchestration.
- **[DEPLOYMENT.md](DEPLOYMENT.md)**: Comprehensive deployment guide (prerequisites, database provisioning, CLR deployment, service deployment, systemd setup, verification).
- **[API.md](API.md)**: REST API endpoint reference (ingestion, inference, search, provenance, operations).
- **[docs/CLR_GUIDE.md](docs/CLR_GUIDE.md)**: CLR function reference, .NET Framework 4.8.1 constraints, SAFE vs UNSAFE deployment, security best practices, troubleshooting.
- **[docs/DATABASE_SCHEMA.md](docs/DATABASE_SCHEMA.md)**: Comprehensive schema reference (atoms, embeddings, tensor atoms, temporal tables, graph tables, provenance).
- **[docs/GODEL_ENGINE.md](docs/GODEL_ENGINE.md)**: Autonomous compute via Service Broker (`sp_StartPrimeSearch`, `clr_FindPrimes`, `AutonomousComputeJobs`).


## License

See [LICENSE](LICENSE) for details.

