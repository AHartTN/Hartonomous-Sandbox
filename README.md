# Hartonomous

## Database-First AI Platform for SQL Server 2025

Hartonomous is an enterprise-grade AI platform built on SQL Server 2025 that implements a **"Periodic Table of Knowledge"** architecture. Everything—text, images, audio, video, and even AI model weights—is decomposed into fundamental, deduplicated atoms stored as primitive data types. By leveraging SQL Server's vector search, CLR integration, and content-addressable storage (CAS), Hartonomous eliminates redundancy while providing full provenance tracking and regulatory compliance—all within your existing database infrastructure.

## Why Hartonomous?

- **Atomic Knowledge Decomposition**: Break all content down to fundamental units (pixels, samples, weights, characters) with content-addressable deduplication—the "Periodic Table of Knowledge"
- **Database-Native AI**: Run inference, embedding generation, and vector search directly in SQL Server—no separate vector databases, no FILESTREAM blobs required
- **Spatial Intelligence**: Exploit GEOMETRY/GEOGRAPHY types for multi-dimensional indexing beyond geospatial—RGB color space, audio waveforms, tensor positions, embedding projections
- **Full Audit Trail**: Complete provenance tracking with temporal tables, graph relationships, and Neo4j integration for regulatory compliance and explainability
- **Enterprise Ready**: Built on SQL Server 2025 with .NET 10, designed for production workloads with resilience patterns, rate limiting, and comprehensive monitoring
- **Autonomous Operations**: OODA loop (Observe → Orient → Decide → Act) implemented via Service Broker for self-optimizing systems

## Key Features

### T-SQL AI Interface

Call AI capabilities directly from SQL queries:

```sql
-- Generate embeddings
DECLARE @embedding VECTOR(1998) = dbo.fn_ComputeEmbedding('search text', 'text-embedding-3-large');

-- Run inference
EXEC dbo.clr_RunInference @modelName = 'gpt-4-vision', @inputJson = '{...}', @outputJson = @result OUTPUT;

-- Trigger autonomous analysis
EXEC dbo.sp_Analyze @observationJson = '{"metricName": "ResponseTime", "value": 1250}';
```

### Database-First Architecture

- **Schema Ownership**: SQL Server Database Project controls all tables, procedures, functions, and CLR bindings
- **EF Core as ORM**: Used only for data access—no migrations, schema defined in SQL scripts
- **Thin Services**: .NET APIs orchestrate database intelligence, not duplicate business logic

### CLR Intelligence Layer

- **14 Deployed Assemblies**: CPU SIMD vector operations (AVX2/SSE4), transformer attention, anomaly detection
- **.NET Framework 4.8.1**: Compiled CLR functions callable from T-SQL
- **Security Model**: `SAFE` assemblies where possible; `UNSAFE` assemblies use trusted assembly registration (no `TRUSTWORTHY` flag)

### Comprehensive Provenance

- **Temporal Tables**: System-versioned history for point-in-time compliance queries
- **SQL Graph**: Causal relationships tracked in `graph.AtomGraphNodes` and `graph.AtomGraphEdges`
- **Neo4j Integration**: Inference traces, model evolution, and reasoning paths mirrored for explainability
- **Full Lineage**: Track every AI decision with evidence, alternatives, confidence scores, and model versions

### Atomic Decomposition ("Periodic Table of Knowledge")

- **Radical Atomization**: Break all content into fundamental units—pixels (RGB triplets), audio samples (int16), model weights (float32), characters (UTF-8)
- **Content-Addressable Storage**: SHA-256 deduplication means identical pixels/weights/samples stored exactly once
- **No FILESTREAM**: Eliminate large blob storage entirely—10GB model becomes millions of deduplicated 4-byte float atoms
- **Spatial Indexing**: Use GEOMETRY for multi-dimensional data—RGB color space, tensor positions, audio waveforms, time-series coordinates
- **Cross-Modal Reuse**: Same RGB value shared across thousands of images, same weight values across model checkpoints

See [docs/architecture/atomic-decomposition.md](docs/architecture/atomic-decomposition.md) for complete philosophy and implementation details.

## Prerequisites

- **SQL Server 2025** with CLR and Service Broker enabled
- **.NET 10 SDK** for building solutions
- **PowerShell 7+** for deployment automation
- **Neo4j 5.x** (Community or Enterprise) for provenance graph and audit trail

**Optional**:

- Azure AD tenant for authentication
- Azure Storage for blob ingestion (pre-atomization)
- OpenTelemetry endpoint for distributed tracing

**Note**: FILESTREAM is NOT required—atomic decomposition eliminates need for large blob storage.

## Quick Start

### 1. Clone and Restore

```powershell
git clone https://github.com/AHartTN/Hartonomous-Sandbox.git Hartonomous
cd Hartonomous
dotnet restore Hartonomous.sln
```

### 2. Build Solutions

```powershell
# Build all projects
dotnet build Hartonomous.sln -c Release

# Build test suite
dotnet build Hartonomous.Tests.sln -c Debug
```

### 3. Provision Database

Run the unified deployment script to create schema, deploy CLR, and configure Service Broker:

```powershell
./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"
```

**Options**:

- `-SkipClr`: Skip CLR assembly deployment
- `-DryRun`: Show SQL commands without executing

This script:

- Enables CLR integration and Service Broker (FILESTREAM not required)
- Executes schema scripts from `sql/tables`, `sql/procedures`, `sql/functions`
- Deploys CLR assemblies and registers functions/aggregates
- Sets up Service Broker message types, contracts, queues, and services
- Creates spatial indexes for atomic decomposition queries

### 4. Start Neo4j

```powershell
# Docker (recommended for development)
docker run -d `
  --name neo4j `
  -p 7474:7474 -p 7687:7687 `
  -e NEO4J_AUTH=neo4j/your-password `
  neo4j:5.28-community
```

Open Neo4j Browser at <http://localhost:7474> and execute `neo4j/schemas/CoreSchema.cypher` to initialize the provenance schema.

### 5. Run Services

**API Server** (port 5000 by default):

```powershell
cd src/Hartonomous.Api
dotnet run
```

**Background Workers**:

```powershell
# CES consumer (CDC ingestion)
cd src/Hartonomous.Workers.CesConsumer
dotnet run

# Neo4j sync worker (Service Broker → Neo4j)
cd src/Hartonomous.Workers.Neo4jSync
dotnet run
```

Update connection strings in `appsettings.json` for each service.

### 6. Run Tests

```powershell
# Run all tests
dotnet test Hartonomous.Tests.sln

# Unit tests only
dotnet test tests/Hartonomous.UnitTests
```

**Note**: Integration tests require additional configuration. See [docs/development/testing-guide.md](docs/development/testing-guide.md) for details.

## Project Structure

- **`src/Hartonomous.Api`**: ASP.NET Core REST API with 18 controller endpoints
- **`src/Hartonomous.Admin`**: Blazor Server admin portal with telemetry dashboards
- **`src/Hartonomous.Workers.*`**: Background services (CDC consumer, Neo4j sync)
- **`src/Hartonomous.Core`**: Domain entities, value objects, interfaces
- **`src/Hartonomous.Data`**: EF Core DbContext and entity configurations
- **`src/Hartonomous.Infrastructure`**: Service implementations, pipelines, messaging
- **`src/SqlClr`**: SQL CLR implementation (.NET Framework 4.8.1)
- **`sql/`**: Database schema scripts (tables, procedures, functions)
- **`scripts/`**: PowerShell deployment automation
- **`tests/`**: Unit, integration, and end-to-end test suites

## REST API

Hartonomous provides a comprehensive REST API for all operations:

```bash
# Ingest content
curl -X POST http://localhost:5000/api/ingestion/ingest \
  -H "Content-Type: application/json" \
  -d '{"sourceUri": "file:///documents/report.txt", "modality": "text"}'

# Generate embeddings
curl -X POST http://localhost:5000/api/embeddings/generate \
  -H "Content-Type: application/json" \
  -d '{"text": "sample text", "modelName": "text-embedding-3-large"}'

# Semantic search
curl -X POST http://localhost:5000/api/search/semantic \
  -H "Content-Type: application/json" \
  -d '{"query": "financial reports", "topK": 10}'

# Query provenance
curl -X GET http://localhost:5000/api/provenance/trace/atom/{atomId}
```

See [docs/api/rest-api.md](docs/api/rest-api.md) for complete API documentation.

## Documentation

- **[Architecture Overview](docs/ARCHITECTURE.md)** - Database-first design, CLR layer, OODA loop, provenance tracking
- **[Atomic Decomposition](docs/architecture/atomic-decomposition.md)** - "Periodic Table of Knowledge" philosophy, radical atomization, spatial indexing
- **[REST API Reference](docs/api/rest-api.md)** - Complete endpoint documentation with examples
- **[Deployment Guide](docs/deployment/deployment-guide.md)** - Production deployment and configuration
- **[Database Schema](docs/development/database-schema.md)** - Complete schema reference and design patterns
- **[CLR Security](docs/security/clr-security.md)** - Security model and best practices
- **[Testing Guide](docs/development/testing-guide.md)** - Running tests and contributing quality code

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines, coding standards, and pull request process.

## License

MIT License - see [LICENSE](LICENSE) for details.

