# Installation Guide

**Complete Setup for Hartonomous Platform**

## Prerequisites

### SQL Server 2022+

**Required Version**: SQL Server 2022 (16.x) or later

**Reason**: Native `VECTOR` type support (introduced in SQL Server 2022)

**Recommended Edition**:
- **Development**: Developer Edition (free, full features)
- **Production**: Enterprise Edition (required for full feature set)
- **Testing**: Evaluation Edition (180-day trial)

**Download**: https://www.microsoft.com/sql-server/sql-server-downloads

**Required Features**:
- Database Engine Services
- Full-Text and Semantic Extractions for Search
- SQL Server Replication (for Service Broker)
- Integration Services (optional, for ETL)

### .NET 8.0 SDK

**Required Version**: .NET 8.0 or later

**Download**: https://dotnet.microsoft.com/download/dotnet/8.0

**Verify Installation**:
```powershell
dotnet --version
# Expected output: 8.0.x
```

### Neo4j 5.x

**Required Version**: Neo4j 5.13 or later

**Recommended Edition**:
- **Development**: Community Edition (free)
- **Production**: Enterprise Edition (clustering, backup, security)

**Download**: https://neo4j.com/download/

**Alternative**: Neo4j Desktop (includes GUI)

**Verify Installation**:
```powershell
neo4j version
# Expected output: neo4j 5.13.0
```

### PowerShell 7+

**Required Version**: PowerShell 7.0 or later

**Download**: https://github.com/PowerShell/PowerShell/releases

**Verify Installation**:
```powershell
$PSVersionTable.PSVersion
# Expected output: 7.x.x
```

### Git

**Download**: https://git-scm.com/downloads

**Verify Installation**:
```powershell
git --version
# Expected output: git version 2.x.x
```

---

## Step 1: Clone Repository

```powershell
# Clone repository
git clone https://github.com/AHartTN/Hartonomous-Sandbox.git
cd Hartonomous-Sandbox

# Verify structure
Get-ChildItem
# Expected: src/, docs/, scripts/, tests/, etc.
```

---

## Step 2: Configure SQL Server

### Enable CLR Integration

```sql
-- Connect to SQL Server instance
USE master;
GO

-- Enable CLR integration (server-level setting)
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Enable CLR strict security (SQL Server 2017+)
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
GO

-- Verify configuration
SELECT name, value, value_in_use 
FROM sys.configurations 
WHERE name IN ('clr enabled', 'clr strict security');
```

**Expected Output**:
| name | value | value_in_use |
|------|-------|--------------|
| clr enabled | 1 | 1 |
| clr strict security | 1 | 1 |

### Enable Service Broker

```sql
-- Create database
CREATE DATABASE Hartonomous;
GO

USE Hartonomous;
GO

-- Enable Service Broker (required for OODA loop)
ALTER DATABASE Hartonomous SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
GO

-- Verify Service Broker enabled
SELECT name, is_broker_enabled 
FROM sys.databases 
WHERE name = 'Hartonomous';
```

**Expected Output**:
| name | is_broker_enabled |
|------|-------------------|
| Hartonomous | 1 |

### Set Database to TRUSTWORTHY (Development Only)

```sql
-- Allow CLR assemblies to access external resources
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
GO
```

⚠️ **Security Warning**: `TRUSTWORTHY ON` allows CLR code to access files, network, etc. Only use in development. For production, use assembly signing (see CLR Deployment guide).

---

## Step 3: Deploy Database Schema

### Option A: Using DACPAC (Recommended)

```powershell
# Navigate to scripts directory
cd scripts

# Install SqlPackage if not present
.\install-sqlpackage.ps1

# Build DACPAC
.\build-dacpac.ps1

# Deploy to local SQL Server
.\deploy-dacpac.ps1 `
    -SqlServer "localhost" `
    -Database "Hartonomous" `
    -DacpacPath "..\src\Hartonomous.Database\bin\Debug\Hartonomous.Database.dacpac"
```

### Option B: Using Migration Scripts

```powershell
# Run migration scripts in order
$server = "localhost"
$database = "Hartonomous"

# Execute scripts
Get-ChildItem "..\src\Hartonomous.Database\Scripts\Migrations" -Filter "*.sql" | 
    Sort-Object Name | 
    ForEach-Object {
        Write-Host "Executing: $($_.Name)" -ForegroundColor Cyan
        Invoke-Sqlcmd -ServerInstance $server -Database $database -InputFile $_.FullName
    }
```

### Verify Schema Deployment

```sql
-- Check tables created
SELECT TABLE_SCHEMA, TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;

-- Expected tables:
-- dbo.Atoms
-- dbo.AtomRelations
-- dbo.AtomEmbeddings
-- dbo.TensorAtoms
-- dbo.Models
-- dbo.Tenants
-- dbo.InferenceRequests
-- dbo.HypothesisWeights
-- dbo.OodaExecutionLog
-- ... (and many more)
```

---

## Step 4: Deploy CLR Assembly

⚠️ **CRITICAL**: Before proceeding, read `docs/operations/clr-deployment.md` for the **System.Collections.Immutable.dll incompatibility issue**.

### Build CLR Assembly

```powershell
# Navigate to repository root
cd D:\Repositories\Hartonomous-Sandbox

# Restore dependencies
dotnet restore Hartonomous.sln

# Build CLR assembly (Release configuration)
dotnet build src\Hartonomous.Clr\Hartonomous.Clr.csproj -c Release
```

**Expected Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

⚠️ **Known Issue**: You may see warnings about `System.Collections.Immutable` not being supported. See CLR Deployment guide for resolution strategies.

### Deploy CLR Assembly

```powershell
# Run deployment script
.\scripts\deploy-clr-assemblies.ps1 `
    -SqlServer "localhost" `
    -Database "Hartonomous" `
    -AssemblyPath "src\Hartonomous.Clr\bin\Release\net8.0\Hartonomous.Clr.dll"
```

**Expected Output**:
```
Deploying CLR assembly...
Assembly 'HartonomousClr' created successfully.
Creating CLR functions...
  - clr_CosineSimilarity
  - clr_EuclideanDistance
  - clr_LandmarkProjection_ProjectTo3D
  - ... (47 more functions)
Deployment completed successfully!
```

### Verify CLR Functions

```sql
-- List all CLR functions
SELECT 
    o.name AS FunctionName,
    a.name AS AssemblyName,
    p.permission_set_desc AS PermissionSet
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
INNER JOIN sys.assemblies p ON a.name = p.name
WHERE o.type = 'FS'  -- CLR scalar function
ORDER BY o.name;

-- Test a function
DECLARE @vec1 VARBINARY(MAX) = 0x3F8000003F0000003E800000;  -- [1.0, 0.5, 0.25]
DECLARE @vec2 VARBINARY(MAX) = 0x3F8000003F0000003E800000;  -- [1.0, 0.5, 0.25]

SELECT dbo.clr_CosineSimilarity(@vec1, @vec2) AS similarity;
-- Expected: 1.0 (identical vectors)
```

---

## Step 5: Configure Neo4j

### Start Neo4j

```powershell
# Start Neo4j service
neo4j start

# Or using Neo4j Desktop: Click "Start" on database
```

### Create Database

```cypher
// Connect to Neo4j (http://localhost:7474)
// Default credentials: neo4j / neo4j (change on first login)

// Create database
CREATE DATABASE hartonomous;

// Switch to database
:use hartonomous;
```

### Create Schema (Indexes & Constraints)

```cypher
// Atom node indexes
CREATE INDEX atom_hash_idx FOR (a:Atom) ON (a.atomHash);
CREATE INDEX atom_tenant_idx FOR (a:Atom) ON (a.tenantId, a.createdAt);

// Source node indexes
CREATE INDEX source_identifier_idx FOR (s:Source) ON (s.identifier);

// IngestionJob node indexes
CREATE INDEX job_status_idx FOR (j:IngestionJob) ON (j.status, j.completedAt);

// User node constraint (unique username)
CREATE CONSTRAINT user_username_unique FOR (u:User) REQUIRE u.username IS UNIQUE;

// Pipeline node indexes
CREATE INDEX pipeline_name_version_idx FOR (p:Pipeline) ON (p.name, p.version);

// Inference node indexes
CREATE INDEX inference_executed_idx FOR (i:Inference) ON (i.executedAt);
CREATE INDEX inference_context_idx FOR (i:Inference) ON (i.contextHash);
```

### Configure Connection String

**File**: `src/Hartonomous.Workers.Neo4jSync/appsettings.json`

```json
{
  "ConnectionStrings": {
    "Neo4j": "neo4j://localhost:7687",
    "SqlServer": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Neo4j": {
    "Username": "neo4j",
    "Password": "your-password-here",
    "Database": "hartonomous"
  }
}
```

---

## Step 6: Deploy Worker Services

### Build Worker Services

```powershell
# Build all worker services
dotnet build src\Hartonomous.Workers.Ingestion\Hartonomous.Workers.Ingestion.csproj -c Release
dotnet build src\Hartonomous.Workers.Neo4jSync\Hartonomous.Workers.Neo4jSync.csproj -c Release
dotnet build src\Hartonomous.Workers.EmbeddingGenerator\Hartonomous.Workers.EmbeddingGenerator.csproj -c Release
dotnet build src\Hartonomous.Workers.SpatialProjector\Hartonomous.Workers.SpatialProjector.csproj -c Release
```

### Configure Connection Strings

Update `appsettings.json` in each worker service:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Run Workers (Development)

**Option A: Run in separate terminals**

```powershell
# Terminal 1: Ingestion Worker
cd src\Hartonomous.Workers.Ingestion
dotnet run

# Terminal 2: Neo4jSync Worker
cd src\Hartonomous.Workers.Neo4jSync
dotnet run

# Terminal 3: EmbeddingGenerator Worker
cd src\Hartonomous.Workers.EmbeddingGenerator
dotnet run

# Terminal 4: SpatialProjector Worker
cd src\Hartonomous.Workers.SpatialProjector
dotnet run
```

**Option B: Install as Windows Services** (Production)

```powershell
# Publish workers
dotnet publish src\Hartonomous.Workers.Ingestion -c Release -o C:\Hartonomous\Workers\Ingestion
dotnet publish src\Hartonomous.Workers.Neo4jSync -c Release -o C:\Hartonomous\Workers\Neo4jSync

# Install as Windows Service (requires admin)
New-Service -Name "HartonomousIngestionWorker" `
    -BinaryPathName "C:\Hartonomous\Workers\Ingestion\Hartonomous.Workers.Ingestion.exe" `
    -DisplayName "Hartonomous Ingestion Worker" `
    -Description "Processes ingestion events from SQL Service Broker" `
    -StartupType Automatic

New-Service -Name "HartonomousNeo4jSyncWorker" `
    -BinaryPathName "C:\Hartonomous\Workers\Neo4jSync\Hartonomous.Workers.Neo4jSync.exe" `
    -DisplayName "Hartonomous Neo4j Sync Worker" `
    -Description "Synchronizes provenance data to Neo4j" `
    -StartupType Automatic

# Start services
Start-Service -Name "HartonomousIngestionWorker"
Start-Service -Name "HartonomousNeo4jSyncWorker"
```

---

## Step 7: Configure OODA Loop

### Create SQL Agent Job (15-min scheduled trigger)

```sql
USE msdb;
GO

-- Create job
EXEC dbo.sp_add_job 
    @job_name = N'OodaCycle_15min',
    @description = N'Autonomous OODA loop execution (Observe-Orient-Decide-Act-Learn)',
    @enabled = 1;

-- Add execution step
EXEC dbo.sp_add_jobstep 
    @job_name = N'OodaCycle_15min',
    @step_name = N'Trigger_Analyze',
    @subsystem = N'TSQL',
    @database_name = N'Hartonomous',
    @command = N'
        DECLARE @handle UNIQUEIDENTIFIER;
        BEGIN DIALOG CONVERSATION @handle
            FROM SERVICE [//Hartonomous/InitiatorService]
            TO SERVICE ''//Hartonomous/AnalyzeService''
            ON CONTRACT [//Hartonomous/OodaContract];
        
        SEND ON CONVERSATION @handle
            MESSAGE TYPE [//Hartonomous/Analyze] ('''');
    ';

-- Create schedule (every 15 minutes)
EXEC dbo.sp_add_schedule 
    @schedule_name = N'Every15Minutes',
    @freq_type = 4,              -- Daily
    @freq_interval = 1,          -- Every day
    @freq_subday_type = 4,       -- Minutes
    @freq_subday_interval = 15;

-- Attach schedule to job
EXEC dbo.sp_attach_schedule 
    @job_name = N'OodaCycle_15min',
    @schedule_name = N'Every15Minutes';

-- Assign to job server
EXEC dbo.sp_add_jobserver 
    @job_name = N'OodaCycle_15min',
    @server_name = N'(local)';

-- Start job
EXEC dbo.sp_start_job @job_name = N'OodaCycle_15min';
GO
```

### Verify OODA Loop Configuration

```sql
-- Check Service Broker queues
SELECT name, is_activation_enabled, activation_procedure
FROM sys.service_queues
WHERE name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');

-- Check SQL Agent job
SELECT 
    j.name AS JobName,
    s.name AS ScheduleName,
    s.freq_subday_interval AS IntervalMinutes,
    j.enabled AS IsEnabled
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
INNER JOIN msdb.dbo.sysschedules s ON js.schedule_id = s.schedule_id
WHERE j.name = 'OodaCycle_15min';
```

---

## Step 8: Create Initial Tenant

```sql
USE Hartonomous;
GO

-- Create default tenant
INSERT INTO dbo.Tenants (TenantName, AtomQuota, CreatedAt)
VALUES ('DefaultTenant', 10000000000, SYSUTCDATETIME());  -- 10B atom quota

-- Get tenant ID
DECLARE @TenantId INT = SCOPE_IDENTITY();
SELECT @TenantId AS TenantId;

-- Create default user
INSERT INTO dbo.Users (Username, Email, TenantId, Role, CreatedAt)
VALUES ('admin', 'admin@hartonomous.local', @TenantId, 'admin', SYSUTCDATETIME());
```

---

## Step 9: Verify Installation

### Run Health Checks

```powershell
# Navigate to scripts directory
cd scripts

# Run preflight check
.\preflight-check.ps1 -SqlServer "localhost" -Database "Hartonomous"
```

**Expected Output**:
```
✓ SQL Server connection successful
✓ Database 'Hartonomous' exists
✓ CLR enabled
✓ Service Broker enabled
✓ CLR assembly 'HartonomousClr' deployed
✓ 49 CLR functions found
✓ Core tables exist (Atoms, AtomRelations, AtomEmbeddings, TensorAtoms)
✓ OODA loop configured (SQL Agent job running)
✓ Neo4j connection successful
✓ Worker services running (4/4)

Installation verified successfully!
```

### Test Basic Query

```sql
-- Test semantic search infrastructure
DECLARE @queryVector VARBINARY(MAX) = dbo.fn_GenerateRandomVector(1536);

SELECT TOP 10
    a.AtomId,
    a.AtomHash,
    ae.SpatialGeometry.STDistance(
        geometry::Point(0, 0, 0, 0)
    ) AS Distance
FROM dbo.Atoms a
INNER JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
WHERE ae.SpatialGeometry.STIntersects(
    geometry::Point(0, 0, 0, 0).STBuffer(10)
) = 1;
```

**Expected**: Query executes without errors (returns 0 rows if no data ingested yet)

---

## Step 10: Ingest Sample Data (Optional)

### Download Sample Model

```powershell
# Download small GGUF model for testing
Invoke-WebRequest -Uri "https://huggingface.co/TheBloke/Llama-2-7B-GGUF/resolve/main/llama-2-7b.Q4_K_M.gguf" `
    -OutFile "C:\Temp\llama-2-7b.Q4_K_M.gguf"
```

### Ingest Model

```sql
-- Ingest GGUF model
DECLARE @ModelPath NVARCHAR(4000) = N'C:\Temp\llama-2-7b.Q4_K_M.gguf';
DECLARE @TenantId INT = 1;  -- Use your tenant ID

EXEC dbo.sp_IngestModel 
    @ModelPath = @ModelPath,
    @ModelName = 'Llama-2-7B-Q4',
    @TenantId = @TenantId;
```

**Expected Duration**: 5-15 minutes for 7B parameter model

**Verify Ingestion**:

```sql
-- Check atoms created
SELECT COUNT(*) AS TotalAtoms
FROM dbo.TensorAtoms
WHERE TenantId = @TenantId;

-- Check spatial projections
SELECT COUNT(*) AS TotalProjected
FROM dbo.TensorAtoms
WHERE SpatialKey IS NOT NULL
  AND TenantId = @TenantId;

-- Check Neo4j provenance
-- (In Neo4j Browser: http://localhost:7474)
MATCH (m:Model {name: 'Llama-2-7B-Q4'})-[:INGESTED_FROM]->(s:Source)
RETURN m, s;
```

---

## Troubleshooting

### SQL Server Connection Failed

**Error**: "Cannot connect to SQL Server"

**Solution**:
```powershell
# Check SQL Server running
Get-Service -Name "MSSQL*"

# Start SQL Server if stopped
Start-Service -Name "MSSQLSERVER"  # Or your instance name
```

### CLR Assembly Deployment Failed

**Error**: "Assembly references assembly 'netstandard, Version=2.0.0.0'"

**Solution**: See `docs/operations/clr-deployment.md` for CRITICAL dependency issue resolution.

### Service Broker Not Enabled

**Error**: "Service Broker is disabled"

**Solution**:
```sql
ALTER DATABASE Hartonomous SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
```

### Neo4j Connection Failed

**Error**: "Unable to connect to Neo4j"

**Solution**:
```powershell
# Check Neo4j running
neo4j status

# Start Neo4j
neo4j start
```

### Worker Services Not Starting

**Error**: "Worker service crashed on startup"

**Solution**:
```powershell
# Check logs
Get-Content "C:\Hartonomous\Workers\Ingestion\logs\app.log" -Tail 50

# Common issues:
# 1. Connection string incorrect (check appsettings.json)
# 2. SQL Server not accessible (check firewall)
# 3. Service Broker queue not found (deploy schema first)
```

---

## Next Steps

1. **Read**: `docs/getting-started/quickstart.md` - 10-minute tutorial
2. **Read**: `docs/architecture/semantic-first.md` - Understand O(log N) + O(K) pattern
3. **Read**: `docs/api/sql-procedures.md` - Learn stored procedures
4. **Try**: Ingest your first model and run semantic queries
5. **Monitor**: OODA loop autonomous improvements (check `dbo.OodaExecutionLog`)

---

## Production Deployment

For production deployment, see:
- `docs/operations/clr-deployment.md` - Assembly signing, TRUSTWORTHY OFF
- `docs/operations/monitoring.md` - Metrics, logging, alerts
- `docs/operations/backup-recovery.md` - Backup strategies, disaster recovery
- `docs/operations/performance-tuning.md` - Index optimization, scaling

---

## Summary

Installation complete! You now have:

✅ SQL Server with CLR enabled and database schema deployed  
✅ 49 CLR functions for O(K) refinement  
✅ Neo4j provenance graph with schema  
✅ 4 worker services processing events  
✅ OODA autonomous loop running every 15 minutes  
✅ Multi-tenant infrastructure ready  

**Time to complete**: ~30-60 minutes (excluding model download)

**System Ready For**:
- Model ingestion (GGUF, SafeTensors, ONNX, PyTorch, TensorFlow, Stable Diffusion)
- Semantic queries (O(log N) + O(K) pattern)
- Provenance tracking (Neo4j Merkle DAG)
- Autonomous optimization (OODA loop)
