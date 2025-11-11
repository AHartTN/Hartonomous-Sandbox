# Hartonomous

**Autonomous AGI-in-SQL-Server Platform**

Hartonomous is a database-first autonomous AI platform where SQL Server 2025 is the runtime, storage, and intelligence substrate. T-SQL stored procedures execute the full OODA loop (Observe → Orient → Decide → Act), CLR functions provide SIMD-accelerated inference and embedding generation, and .NET 10 services orchestrate ingestion, API access, and background workers. The database stores atoms (multimodal data units), embeddings (vector representations), tensor coefficients (model weights), and provenance graphs (dual-ledger temporal + Neo4j lineage).

## Platform Highlights

- **T-SQL as AI Interface**: Call `sp_Analyze`, `sp_Hypothesize`, `sp_Act`, `sp_Learn` directly from SQL to execute autonomous reasoning loops. Invoke CLR functions like `clr_RunInference`, `fn_ComputeEmbedding`, or `fn_GenerateText` for inference without leaving the database.
- **Database-First Architecture**: SQL Server Database Project owns all schema (tables, procedures, functions, CLR bindings). EF Core used only as ORM for data access—no migrations control schema. Applications are thin clients orchestrating database intelligence.
- **CLR Intelligence Layer**: `src/SqlClr` compiles to .NET Framework 4.8.1 assembly deployed in SQL Server. Provides SIMD vector aggregates, transformer attention, anomaly detection, multimodal generation, and stream orchestration called from stored procedures. **Security**: CLR assemblies deployed as `SAFE` where possible; `UNSAFE` assemblies (SIMD intrinsics, file I/O) use trusted assembly registration without enabling `TRUSTWORTHY` database option.
- **Autonomous OODA Loop**: Service Broker queues (`AnalyzeQueue`, `HypothesizeQueue`, `ActQueue`, `LearnQueue`) coordinate the four-phase loop. Stored procedures publish/consume messages, CLR helpers aggregate telemetry, and results are recorded in `AutonomousImprovementHistory`.
- **Dual-Ledger Provenance** (**CRITICAL FOR AUDITABILITY**):
  - **SQL Temporal Tables**: System-versioned history on critical tables (`TensorAtomCoefficients`, `Models`, etc.) for point-in-time compliance queries
  - **SQL Graph Tables**: `graph.AtomGraphNodes`, `graph.AtomGraphEdges` capture causal relationships using SQL Server graph syntax
  - **Neo4j Provenance Graph**: Service Broker→Neo4j worker (`Hartonomous.Workers.Neo4jSync`) mirrors inference traces, model evolution, reasoning paths to Neo4j for explainability queries (see `neo4j/schemas/CoreSchema.cypher`)
  - **Full Audit Trail**: Every inference decision tracked with evidence, alternatives considered, models used, reasoning modes, confidence scores—required for regulatory compliance and AI explainability
- **Dual Embedding Paths**: C# `EmbeddingService` (`src/Hartonomous.Infrastructure/AI/Embeddings/EmbeddingService.cs`) for HTTP ingestion. CLR `fn_ComputeEmbedding` for T-SQL embedding generation. Both persist to `dbo.AtomEmbeddings` with `VECTOR(1998)` columns and spatial geometry projections.
- **Unified Substrate**: Single `dbo.Atoms` table with modality metadata, JSON descriptors, geometry `SpatialKey`, and temporal columns. Embeddings, tensor atoms, and provenance link to atoms. No separate vector store—database is the vector store, graph store, and model repository.

## Prerequisites

- **SQL Server 2025** with CLR, FILESTREAM, and Service Broker enabled
- **.NET 10 SDK** for building `Hartonomous.sln` and `Hartonomous.Tests.sln`
- **PowerShell 7+** for running deployment scripts (`scripts/deploy-database-unified.ps1`)
- **Neo4j 5.x** (Community or Enterprise) for provenance graph sync and audit trail - **CRITICAL for compliance/explainability**
- **Optional**: Azure AD tenant for authentication, Azure Storage for blob ingestion, OpenTelemetry endpoint for telemetry export

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

**Neo4j** (required for provenance/auditability - run BEFORE services):

```pwsh
# Option 1: Docker (recommended for development)
docker run -d `
  --name neo4j `
  -p 7474:7474 -p 7687:7687 `
  -e NEO4J_AUTH=neo4j/your-password `
  -e NEO4J_apoc_export_file_enabled=true `
  -e NEO4J_apoc_import_file_enabled=true `
  neo4j:5.28-community

# Option 2: Neo4j Desktop (Windows/Mac)
# Download from https://neo4j.com/download/
# Create a database, set password, start

# Initialize schema
# Open Neo4j Browser at http://localhost:7474
# Execute: neo4j/schemas/CoreSchema.cypher
```

**Configuration**: Update connection strings in `appsettings.json`:

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password"
  }
}
```

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

### 5. Run EF Core Migrations (Alternative - Not Recommended)

**Note**: In database-first architecture, SQL Server Database Project owns schema. EF Core migrations are preserved for backward compatibility but **should not** be used to modify schema. Use SQL scripts in `sql/` instead.

```pwsh
dotnet ef database update --project src/Hartonomous.Data/Hartonomous.Data.csproj --connection "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"
```

**Database-First Workflow**:

1. Modify schema in `sql/tables/*.sql`, `sql/procedures/*.sql`, etc.
2. Deploy via `deploy-database-unified.ps1`
3. Scaffold EF Core entities if needed: `dotnet ef dbcontext scaffold`
4. Manually update EF Core configurations in `src/Hartonomous.Data/Configurations/` to match SQL schema

### 6. Run Tests

**Current Test Status** (as of November 2024):

- **Unit Tests**: ✅ 110/110 passing (stubs, ~30-40% code coverage)
- **Integration Tests**: ⚠️ 2/28 passing (25 failures due to missing `appsettings.Testing.json` configuration)
- **End-to-End Tests**: ✅ 2/2 passing (minimal smoke tests)
- **Database Tests**: ❌ Not yet executed
- **Coverage Goal**: 100% (see [TESTING_AUDIT_AND_COVERAGE_PLAN.md](TESTING_AUDIT_AND_COVERAGE_PLAN.md))

```pwsh
# All tests (will show failures without test infrastructure)
dotnet test Hartonomous.Tests.sln

# Unit tests only (all passing)
dotnet test tests/Hartonomous.UnitTests

# Integration tests (requires appsettings.Testing.json, test database, Neo4j)
dotnet test tests/Hartonomous.IntegrationTests

# Database validation tests (not yet implemented)
dotnet test tests/Hartonomous.DatabaseTests
```

**To Fix Integration Test Failures**:

1. Create `tests/Hartonomous.IntegrationTests/appsettings.Testing.json` with connection strings
2. Deploy test database: `./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous_Test"`
3. Start Neo4j container for graph tests
4. Configure External ID test tokens

See [TESTING_AUDIT_AND_COVERAGE_PLAN.md](TESTING_AUDIT_AND_COVERAGE_PLAN.md) for complete testing roadmap.

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
- **[DEPLOYMENT_ARCHITECTURE_PLAN.md](DEPLOYMENT_ARCHITECTURE_PLAN.md)**: Hybrid Azure Arc SQL deployment strategy, production infrastructure plan.
- **[DATABASE_DEPLOYMENT_GUIDE.md](DATABASE_DEPLOYMENT_GUIDE.md)**: Step-by-step database provisioning guide.
- **[API.md](API.md)**: REST API endpoint reference (ingestion, inference, search, provenance, operations).
- **[TESTING_AUDIT_AND_COVERAGE_PLAN.md](TESTING_AUDIT_AND_COVERAGE_PLAN.md)**: Testing status, coverage gaps, 184-item plan to 100% coverage.
- **[IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)**: 226-item sequential action plan (database-first architecture).
- **[VERSION_AND_COMPATIBILITY_AUDIT.md](VERSION_AND_COMPATIBILITY_AUDIT.md)**: Package versions, SQL Server 2025 requirements, compatibility matrix.
- **[DATABASE_AND_DEPLOYMENT_AUDIT.md](DATABASE_AND_DEPLOYMENT_AUDIT.md)**: Schema inventory, CLR UNSAFE security documentation.
- **[CODE_REFACTORING_AUDIT.md](CODE_REFACTORING_AUDIT.md)**: 400+ code quality issues cataloged (P0-P4 priority levels).
- **[docs/CLR_DEPLOYMENT.md](docs/CLR_DEPLOYMENT.md)**: CLR function reference, .NET Framework 4.8.1 constraints, SAFE vs UNSAFE deployment, security best practices.
- **[docs/GODEL_ENGINE.md](docs/GODEL_ENGINE.md)**: Autonomous compute via Service Broker (`sp_StartPrimeSearch`, `clr_FindPrimes`, `AutonomousComputeJobs`).
- **[scripts/CLR_SECURITY_ANALYSIS.md](scripts/CLR_SECURITY_ANALYSIS.md)**: CLR security surface analysis, UNSAFE assembly justification.


## License

See [LICENSE](LICENSE) for details.

