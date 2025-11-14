# Getting Started with Hartonomous

**Last Updated**: 2025-11-13

Welcome to Hartonomous! This guide will help you get the platform running in your environment.

## Prerequisites

### Required
- **SQL Server 2025** (or compatible version with VECTOR type support)
  - CLR enabled
  - Service Broker enabled
  - Mixed authentication mode
- **.NET 10 SDK** (RC2 or later)
- **PowerShell 7+**
- **Windows Server 2019+** or **Windows 10/11**

### Optional
- **Neo4j 5.x** (Community or Enterprise) - for provenance graph
- **Docker Desktop** - for containerized Neo4j

**Note**: Embedding generation should use native CLR transformer implementation. External services may be used temporarily during development but should be replaced with enterprise-grade native implementation.

### System Requirements
- **RAM**: 16GB minimum, 32GB+ recommended
- **Storage**: 100GB+ for database (depends on data volume)
- **CPU**: 8+ cores recommended (for CLR SIMD operations)

---

## Quick Start (30 minutes)

### Step 1: Clone Repository

```powershell
git clone https://github.com/AHartTN/Hartonomous-Sandbox.git Hartonomous
cd Hartonomous
```

### Step 2: Build Solution

```powershell
# Restore NuGet packages
dotnet restore Hartonomous.sln

# Build all projects
dotnet build Hartonomous.sln -c Release
```

### Step 3: Deploy Database

```powershell
# Build DACPAC
cd src\Hartonomous.Database
msbuild Hartonomous.Database.sqlproj /t:Build /p:Configuration=Release
cd ..\..

# Deploy schema
.\scripts\deploy-dacpac.ps1 -Server "localhost" -Database "Hartonomous"
```

**Expected output**:
```
Deploying DACPAC to localhost...
Creating database Hartonomous...
Creating schemas: dbo, provenance, graph, Billing...
Creating tables (99)...
Creating procedures (107)...
Creating functions (18)...
Creating Service Broker components...
Deployment successful!
```

### Step 4: Deploy CLR Assemblies

```powershell
.\scripts\deploy-clr-secure.ps1 -Server "localhost" -Database "Hartonomous"
```

**Expected output**:
```
Deploying CLR assemblies...
Tier 1: Foundation assemblies (3)...
Tier 2: Memory assemblies (2)...
Tier 3: Language assemblies (2)...
Tier 4: System assemblies (4)...
Tier 5: Libraries (2)...
Tier 6: Application (1)...
CLR deployment successful! 14 assemblies deployed.
```

### Step 5: Verify Installation

```sql
-- Connect to database
USE Hartonomous;

-- Check tables
SELECT COUNT(*) AS TableCount FROM sys.tables;
-- Expected: 99

-- Check procedures
SELECT COUNT(*) AS ProcedureCount FROM sys.procedures;
-- Expected: 107

-- Check CLR functions
SELECT COUNT(*) AS ClrFunctionCount FROM sys.objects
WHERE type IN ('FS', 'FT', 'AF');
-- Expected: 60+

-- Test CLR function
SELECT dbo.clr_BinaryToFloat(0x3F800000) AS Result;
-- Expected: 1.0
```

### Step 6: Run First Ingestion

```sql
-- Ingest sample text
DECLARE @AtomId BIGINT;
EXEC dbo.sp_IngestAtom_Atomic
    @Modality = 'text',
    @Content = 'Hello Hartonomous!',
    @Metadata = '{"source": "getting-started"}',
    @ParentAtomId = @AtomId OUTPUT;

SELECT @AtomId AS NewAtomId;

-- Verify atom created
SELECT * FROM dbo.Atoms WHERE AtomId = @AtomId;
```

**Success!** You've deployed Hartonomous and ingested your first atom.

---

## Next Steps

### Configure Neo4j (Optional - Provenance Graph)

```powershell
# Run Neo4j in Docker
docker run -d `
    --name neo4j `
    -p 7474:7474 -p 7687:7687 `
    -e NEO4J_AUTH=neo4j/password `
    neo4j:5.28

# Apply Hartonomous schema
cat neo4j\schemas\CoreSchema.cypher | docker exec -i neo4j cypher-shell -u neo4j -p password
```

### Configure Embedding Generation

**Enterprise Approach** (Recommended):
Implement native transformer-based embedding generation in CLR:
- Load pre-trained model weights (BERT, RoBERTa, etc.) from ingested atoms
- Implement tokenization (WordPiece/BPE) via CLR functions
- Forward pass through transformer encoder using MathNet.Numerics
- Layer normalization and pooling in SQL CLR
- Store embeddings as VECTOR(1998) directly

**Development Temporary Workaround** (Azure OpenAI):
For rapid prototyping only - should be replaced:

1. Create Azure OpenAI resource
2. Deploy `text-embedding-3-large` model
3. Update `appsettings.json` in Hartonomous.Api:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-key",
    "EmbeddingDeployment": "text-embedding-3-large"
  }
}
```

### Run API

```powershell
cd src\Hartonomous.Api
dotnet run
```

Navigate to `https://localhost:5001/swagger` for API documentation.

### Trigger OODA Loop

```sql
-- Manually trigger analysis phase
EXEC dbo.sp_Analyze @LookbackHours = 24;

-- Check queue status
SELECT * FROM dbo.AnalyzeQueue WITH (NOLOCK);
```

---

## Troubleshooting

### "CLR not enabled"

```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

### "Service Broker not enabled"

```sql
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
```

### "Assembly binding redirect error"

See [SQL Server Binding Redirects](../reference/sqlserver-binding-redirects.md)

### "VECTOR type not supported"

You need SQL Server 2025 or compatible version with VECTOR type support.

---

## What's Next?

- [Architecture Overview](../architecture/PHILOSOPHY.md) - Understand the WHY
- [Database Reference](../database/procedures-reference.md) - Explore all procedures
- [API Documentation](../api/rest-api.md) - Learn the REST API
- [Operations Guide](../operations/monitoring.md) - Monitor and maintain

---

## Support

- **Documentation**: [docs/README.md](../README.md)
- **Issues**: [GitHub Issues](https://github.com/AHartTN/Hartonomous-Sandbox/issues)
- **Discussions**: [GitHub Discussions](https://github.com/AHartTN/Hartonomous-Sandbox/discussions)

---

**Welcome to the Periodic Table of Knowledge!**
