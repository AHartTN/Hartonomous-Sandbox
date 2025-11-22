# Quick Start Guide

**Get Hartonomous running in 10 minutes**

## Prerequisites

### Required Software
- **Windows Server 2022** or **Windows 11** (SQL Server dependency)
- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **SQL Server 2025** or **SQL Server 2022** ([Download Express](https://www.microsoft.com/sql-server/sql-server-downloads))
- **Neo4j 5.x** Community or Enterprise ([Download](https://neo4j.com/download/))
- **PowerShell 7+** ([Download](https://github.com/PowerShell/PowerShell))

### Optional (For Full Features)
- **Azure subscription** (for Entra ID, Key Vault, Application Insights)
- **Ollama** (for local model testing) ([Download](https://ollama.ai))
- **Git** (for repository atomization)

### Hardware Recommendations
- **Minimum**: 8 CPU cores, 16GB RAM, 100GB SSD
- **Recommended**: 16+ CPU cores, 64GB RAM, 500GB NVMe SSD
- **Production**: 32+ CPU cores, 128GB RAM, 2TB NVMe SSD

## Installation Steps

### 1. Clone Repository

```powershell
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous
```

### 2. Configure SQL Server

**Enable CLR Integration** (Required for spatial functions):
```sql
-- Run in SQL Server Management Studio (SSMS)
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
GO
```

**Create Database**:
```sql
CREATE DATABASE Hartonomous;
GO
```

### 3. Configure Connection Strings

**Copy template**:
```powershell
cp src/Hartonomous.Api/appsettings.json.template src/Hartonomous.Api/appsettings.json
```

**Edit** `src/Hartonomous.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;",
    "Neo4jConnection": "bolt://localhost:7687"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password-here",
    "Database": "neo4j"
  },
  "AzureAd": {
    "Enabled": false  // Set true if using Entra ID
  }
}
```

### 4. Deploy Database Schema

**Option A: Using PowerShell Script** (Recommended):
```powershell
.\scripts\deploy\Deploy-Local.ps1 -Force
```

**Option B: Using DACPAC**:
```powershell
# Build DACPAC
.\scripts\build-dacpac.ps1 -ProjectPath "src/Hartonomous.Database/Hartonomous.Database.sqlproj" `
                            -OutputDir "src/Hartonomous.Database/bin/Release/" `
                            -Configuration Release

# Deploy DACPAC
sqlpackage /Action:Publish `
           /SourceFile:"src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac" `
           /TargetServerName:"localhost" `
           /TargetDatabaseName:"Hartonomous" `
           /p:BlockOnPossibleDataLoss=false
```

**Verify Deployment**:
```sql
-- Check table count (should be 93+)
SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo';

-- Check stored procedures (should be 77+)
SELECT COUNT(*) FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE';

-- Check CLR functions (should be 93+)
SELECT COUNT(*) FROM sys.objects WHERE type = 'FS';
```

### 5. Start Neo4j

**Windows**:
```powershell
# Navigate to Neo4j installation
cd "C:\Program Files\Neo4j\bin"

# Set initial password
.\neo4j-admin set-initial-password your-password-here

# Start Neo4j
.\neo4j console
```

**Verify**: Open http://localhost:7474 in browser, login with `neo4j` / `your-password-here`

### 6. Build and Run API

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run API
dotnet run --project src/Hartonomous.Api
```

**Expected Output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### 7. Verify Installation

**Open Swagger UI**: https://localhost:5001/swagger

**Test Health Endpoint**:
```powershell
curl https://localhost:5001/health
```

**Expected Response**:
```json
{
  "status": "Healthy",
  "results": {
    "database": "Healthy",
    "neo4j": "Healthy"
  }
}
```

## First Ingestion

### Ingest a Text File

**Prepare test file**:
```powershell
echo "Hello Hartonomous! This is a test document for atomization." > test.txt
```

**Upload via API**:
```powershell
curl -X POST https://localhost:5001/api/v1/ingestion/file `
  -F "file=@test.txt" `
  -F "tenantId=1"
```

**Response**:
```json
{
  "jobId": "abc-123-def",
  "status": "completed",
  "totalAtoms": 12,
  "uniqueAtoms": 11,
  "durationMs": 145,
  "atomizerType": "TextAtomizer"
}
```

### Ingest an Ollama Model (Optional)

**Prerequisites**: Ollama running locally with a model pulled

```powershell
# Pull model
ollama pull llama3.2

# Ingest via API
curl -X POST https://localhost:5001/api/v1/ingestion/ollama `
  -H "Content-Type: application/json" `
  -d '{
    "modelIdentifier": "llama3.2",
    "tenantId": 1,
    "source": {
      "name": "Llama 3.2 from Ollama",
      "metadata": "{\"ollamaEndpoint\":\"http://localhost:11434\"}"
    }
  }'
```

## First Query

### Semantic Search

```powershell
curl -X POST https://localhost:5001/api/search/semantic `
  -H "Content-Type: application/json" `
  -d '{
    "query": "test document",
    "topK": 10,
    "tenantId": 1
  }'
```

**Response**:
```json
{
  "results": [
    {
      "atomId": 123,
      "canonicalText": "test",
      "distance": 0.12,
      "modality": "text"
    },
    {
      "atomId": 124,
      "canonicalText": "document",
      "distance": 0.18,
      "modality": "text"
    }
  ],
  "totalResults": 10,
  "queryTimeMs": 24
}
```

## Running Worker Services

### Start CES Consumer (Service Broker Queue Processor)

```powershell
dotnet run --project src/Hartonomous.Workers.CesConsumer
```

### Start Embedding Generator

```powershell
dotnet run --project src/Hartonomous.Workers.EmbeddingGenerator
```

### Start Neo4j Sync Worker

```powershell
dotnet run --project src/Hartonomous.Workers.Neo4jSync
```

## Troubleshooting

### CLR Functions Not Working

**Error**: "CLR function 'clr_ComputeHilbertValue' not found"

**Solution**:
```sql
-- Check CLR assembly is deployed
SELECT * FROM sys.assemblies WHERE name = 'Hartonomous.Clr';

-- If missing, manually deploy
CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Clr\bin\Release\net10.0\Hartonomous.Clr.dll'
WITH PERMISSION_SET = SAFE;
```

### Neo4j Connection Fails

**Error**: "Neo.ClientError.Security.Unauthorized"

**Solution**: Verify password in appsettings.json matches Neo4j password

### Swagger UI Shows 401 Unauthorized

**Cause**: Entra ID authentication enabled but not configured

**Solution**: Set `"AzureAd:Enabled": false` in appsettings.json for local development

### Database Migration Fails

**Error**: "Cannot drop table 'dbo.Atom' because it is being referenced by a FOREIGN KEY constraint"

**Solution**: Use `/p:BlockOnPossibleDataLoss=false` flag in sqlpackage deploy

## Next Steps

- [Configuration Guide](configuration.md) - Azure services, secrets, multi-tenancy
- [API Reference](../api/README.md) - All endpoints with examples
- [Architecture Overview](../architecture/README.md) - Understand the system design
- [Operations Guide](../operations/README.md) - Production deployment

---

**Document Version**: 2.0
**Last Updated**: January 2025
