# Quickstart Guide

Get Hartonomous running in 5 minutes and ingest your first AI model.

## Prerequisites Checklist

- ✅ **Windows Server 2022+** or **Windows 11**
- ✅ **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- ✅ **SQL Server 2025** (or 2022) - [Download Express](https://www.microsoft.com/sql-server/sql-server-downloads)
- ✅ **Neo4j 5.x** - [Download Community](https://neo4j.com/download/)
- ✅ **PowerShell 7+** - [Download](https://github.com/PowerShell/PowerShell)
- ✅ **Git** - [Download](https://git-scm.com/downloads)

## Step 1: Clone and Configure

```powershell
# Clone repository
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous

# Create configuration from template
Copy-Item src/Hartonomous.Api/appsettings.json.template src/Hartonomous.Api/appsettings.json

# Edit connection strings (use your favorite editor)
code src/Hartonomous.Api/appsettings.json
```

### Connection Strings Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;",
    "Neo4j": "neo4j://localhost:7687"
  },
  "Neo4jCredentials": {
    "Username": "neo4j",
    "Password": "your-neo4j-password"
  }
}
```

## Step 2: Deploy Database

```powershell
# Deploy SQL Server database schema and CLR assemblies
.\scripts\Deploy-Database.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
```

This script:
- Creates `Hartonomous` database
- Deploys schema (40+ tables, indices, temporal tables)
- Registers CLR assemblies (49 SIMD-optimized functions)
- Creates spatial indices (R-tree for GEOMETRY columns)
- Sets up Service Broker queues (OODA loop)

**Expected output:**
```
✅ Database created
✅ Schema deployed
✅ CLR assemblies registered
✅ Spatial indices created
✅ Service Broker enabled
```

## Step 3: Seed Cognitive Kernel (Optional but Recommended)

For testing and validation, seed the database with orthogonal basis vectors:

```powershell
# Run cognitive kernel seeding script
.\scripts\seed-cognitive-kernel.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
```

This creates:
- **EPOCH 1**: Spatial landmarks (X/Y/Z axis basis vectors)
- **EPOCH 2**: Test atoms (A* pathfinding golden paths)
- **EPOCH 3**: Embeddings with predictable 3D coordinates
- **EPOCH 4**: Operational history (OODA validation data)

## Step 4: Start Neo4j

```bash
# Start Neo4j (Windows)
neo4j console

# Or as Windows service
neo4j start
```

Navigate to http://localhost:7474 and set initial password (use same password as in `appsettings.json`).

## Step 5: Run API

```powershell
# Build and run
dotnet build
dotnet run --project src/Hartonomous.Api
```

API starts at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000
- Swagger UI: https://localhost:5001/swagger

## Step 6: Ingest Your First Model

### Option A: Ollama Model (Fastest)

Requires [Ollama](https://ollama.ai) running locally:

```bash
# 1. Pull model in Ollama
ollama pull llama3.2

# 2. Ingest into Hartonomous
curl -X POST https://localhost:5001/api/ingest/ollama \
  -H "Content-Type: application/json" \
  -d '{
    "modelIdentifier": "llama3.2",
    "source": {
      "name": "Llama 3.2 from Ollama",
      "metadata": "{\"ollamaEndpoint\":\"http://localhost:11434\"}"
    }
  }'
```

**Response:**
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "atomsCreated": 847,
  "status": "Completed",
  "modelMetadata": {
    "parameterSize": "3B",
    "quantization": "Q4_0",
    "modelFamily": "llama"
  }
}
```

### Option B: HuggingFace Model

```bash
curl -X POST https://localhost:5001/api/ingest/huggingface \
  -H "Content-Type: application/json" \
  -d '{
    "modelIdentifier": "meta-llama/Llama-3.2-1B",
    "source": {
      "name": "Llama 3.2 1B from HuggingFace"
    }
  }'
```

### Option C: Local Model File

```bash
# Upload GGUF, ONNX, SafeTensors, or PyTorch file
curl -X POST https://localhost:5001/api/ingest/file \
  -F "file=@D:\Models\llama-2-7b.Q4_K_M.gguf" \
  -F "tenantId=1"
```

## Step 7: Query the Semantic Space

```bash
# Semantic search for "machine learning optimization techniques"
curl -X POST https://localhost:5001/api/query/semantic \
  -H "Content-Type: application/json" \
  -d '{
    "query": "machine learning optimization techniques",
    "topK": 10
  }'
```

**Response:**
```json
{
  "results": [
    {
      "atomId": 12345,
      "content": "Adam optimizer combines momentum with adaptive learning rates...",
      "distance": 0.0234,
      "sourceType": "ModelMetadata"
    },
    ...
  ]
}
```

## Verify Installation

### Check Database

```sql
-- Connect to SQL Server
USE Hartonomous;

-- Verify atoms created
SELECT COUNT(*) AS TotalAtoms FROM Atom;
SELECT COUNT(*) AS AtomsWithEmbeddings FROM AtomEmbedding;

-- Verify spatial index
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('AtomEmbedding')
AND i.name LIKE '%Spatial%';
```

### Check Neo4j

```cypher
// Open Neo4j Browser at http://localhost:7474

// Count nodes
MATCH (n) RETURN count(n) AS TotalNodes;

// View atom nodes
MATCH (a:Atom) RETURN a LIMIT 10;

// View relationships
MATCH (a)-[r]->(b) RETURN a, r, b LIMIT 25;
```

### Check API Health

```bash
# Health check endpoint
curl https://localhost:5001/health

# Response:
# {
#   "status": "Healthy",
#   "checks": {
#     "database": "Healthy",
#     "neo4j": "Healthy",
#     "spatial_indices": "Healthy"
#   }
# }
```

## What's Next?

- **[First Ingestion Guide](first-ingestion.md)** - Ingest documents, images, videos
- **[Configuration Guide](configuration.md)** - Azure services, Entra ID, multi-tenancy
- **[Architecture Overview](../architecture/semantic-first.md)** - Understand how it works
- **[API Reference](../api/ingestion.md)** - Explore all endpoints

## Troubleshooting

### SQL Server Connection Failed

**Error:** `Cannot connect to SQL Server`

**Solutions:**
1. Verify SQL Server is running: `Get-Service MSSQLSERVER`
2. Check firewall: Enable TCP/IP in SQL Server Configuration Manager
3. Test connection: `sqlcmd -S localhost -E`

### CLR Assembly Registration Failed

**Error:** `CLR integration is not enabled`

**Solution:**
```sql
-- Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Enable CLR strict security (SQL Server 2017+)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
```

### Neo4j Connection Failed

**Error:** `Failed to connect to Neo4j`

**Solutions:**
1. Verify Neo4j is running: `neo4j status`
2. Check connection string: `neo4j://localhost:7687` (note: `neo4j://` not `bolt://`)
3. Verify credentials match `appsettings.json`

### Spatial Index Not Created

**Error:** `Spatial index IX_AtomEmbedding_Spatial does not exist`

**Solution:**
```sql
-- Manually create spatial index
CREATE SPATIAL INDEX IX_AtomEmbedding_Spatial
ON AtomEmbedding(SpatialKey)
USING GEOMETRY_AUTO_GRID
WITH (BOUNDING_BOX = (-100, -100, 100, 100));
```

### GGUF Parsing Error

**Error:** `Unsupported GGUF version`

**Solution:** Ensure model is GGUF v3 format (Ollama uses v3). Re-export older models:
```bash
ollama pull llama3.2  # Always uses latest GGUF format
```

## Additional Resources

- **[Installation Guide](installation.md)** - Detailed installation steps
- **[Configuration Reference](configuration.md)** - All configuration options
- **[API Documentation](../api/ingestion.md)** - Complete API reference
- **[Troubleshooting Guide](../operations/troubleshooting.md)** - Common issues and solutions
