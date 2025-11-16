# Hartonomous Quickstart Guide

Get Hartonomous running locally in **5 minutes**.

---

## Prerequisites

### Required

- **SQL Server 2019+** (Developer or Enterprise edition)
  - Download: https://www.microsoft.com/sql-server/sql-server-downloads
  - Enable CLR integration: `sp_configure 'clr enabled', 1; RECONFIGURE;`

- **.NET 8 SDK**
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0

- **Visual Studio 2022** (with SQL Server Data Tools)
  - Or: VS Code + SQL Server extension

### Optional

- **Neo4j 5.x** (for provenance tracking)
  - Download: https://neo4j.com/download/
  - Can run without Neo4j initially (workers will log warnings)

- **Git** (for cloning repository)

---

## Step 1: Clone Repository

```bash
git clone https://github.com/yourusername/Hartonomous.git
cd Hartonomous
```

---

## Step 2: Build SQL CLR Project

**Important**: SQL CLR requires **.NET Framework 4.8.1**, not .NET 8.

```bash
# Navigate to CLR project
cd src/Hartonomous.Database

# Build in Visual Studio 2022 (recommended)
# Or use MSBuild directly:
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" Hartonomous.Database.sqlproj /p:Configuration=Release /t:Build

# This produces: bin/Release/Hartonomous.Database.dll
```

**Verify**: Check that `bin/Release/Hartonomous.Database.dll` exists.

---

## Step 3: Deploy Database

### Option A: Using sqlpackage (Recommended)

```bash
# Build DACPAC
cd src/Hartonomous.Database
dotnet build -c Release

# Deploy to SQL Server
sqlpackage /Action:Publish \
    /SourceFile:bin/Release/Hartonomous.Database.dacpac \
    /TargetConnectionString:"Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;"
```

### Option B: Using Visual Studio

1. Open `Hartonomous.sln` in Visual Studio 2022
2. Right-click `Hartonomous.Database` project
3. Select **Publish...**
4. Configure connection to `localhost`
5. Database name: `Hartonomous`
6. Click **Publish**

### Verify Database Deployment

```sql
-- Connect to SQL Server
USE Hartonomous;

-- Check core tables exist
SELECT name FROM sys.tables WHERE name IN ('Atoms', 'AtomEmbeddings', 'TensorAtoms');

-- Check CLR functions deployed
SELECT name, type_desc FROM sys.objects WHERE type IN ('FN', 'FS', 'FT', 'PC') AND is_ms_shipped = 0;

-- Check spatial indexes
SELECT name FROM sys.indexes WHERE type_desc = 'SPATIAL';
```

**Expected**: Tables exist, CLR functions registered, spatial indexes created.

---

## Step 4: Configure Application Settings

Create `appsettings.Development.json` in each project:

### src/Hartonomous.Api/appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;",
    "Neo4j": "bolt://localhost:7687"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "your-password-here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### src/Hartonomous.Workers.*/appsettings.Development.json

Same configuration for each worker project.

---

## Step 5: Run Workers

Open **4 terminal windows**:

### Terminal 1: Ingestion Worker

```bash
cd src/Hartonomous.Workers.Ingestion
dotnet run
```

**Expected output**:
```
info: Hartonomous.Workers.Ingestion[0]
      Ingestion worker started
info: Hartonomous.Workers.Ingestion[0]
      Listening for atomization requests...
```

### Terminal 2: Embedding Generator Worker

```bash
cd src/Hartonomous.Workers.EmbeddingGenerator
dotnet run
```

### Terminal 3: Spatial Projector Worker

```bash
cd src/Hartonomous.Workers.SpatialProjector
dotnet run
```

### Terminal 4: Neo4j Sync Worker (Optional)

```bash
cd src/Hartonomous.Workers.Neo4jSync
dotnet run
```

**Note**: If Neo4j not running, this worker will log warnings but won't crash.

---

## Step 6: Run API (Optional)

```bash
cd src/Hartonomous.Api
dotnet run
```

**Expected output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

Navigate to: `https://localhost:5001/swagger`

---

## Step 7: Verify System Health

### Test 1: Atomize Sample Text

```sql
USE Hartonomous;

DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

-- Atomize text
EXEC dbo.sp_AtomizeText_Governed
    @InputText = 'Hello, Hartonomous!',
    @SessionId = @SessionId,
    @SourceId = 1;

-- Check atoms created
SELECT TOP 10 * FROM dbo.Atoms ORDER BY CreatedAt DESC;
```

**Expected**: New atoms in Atoms table.

### Test 2: Generate Embedding and Project to 3D

```sql
-- Check if embedding worker generated embeddings
SELECT TOP 5 AtomId, EmbeddingVector, SpatialGeometry
FROM dbo.AtomEmbeddings
ORDER BY CreatedAt DESC;
```

**Expected**: SpatialGeometry populated (GEOMETRY type, not NULL).

### Test 3: Spatial Query

```sql
-- Create query point
DECLARE @QueryGeometry GEOMETRY = geometry::Point(10, 20, 5, 0);

-- Spatial search (O(log N))
SELECT TOP 10
    ae.AtomId,
    a.ContentType,
    ae.SpatialGeometry.STDistance(@QueryGeometry) AS Distance
FROM dbo.AtomEmbeddings ae
INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
WHERE ae.SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(50)) = 1
ORDER BY ae.SpatialGeometry.STDistance(@QueryGeometry);
```

**Expected**: Results returned in <100ms.

### Test 4: Reasoning Framework

```sql
DECLARE @SessionId UNIQUEIDENTIFIER = NEWID();

-- Chain of Thought reasoning
EXEC dbo.sp_ChainOfThoughtReasoning
    @Prompt = 'Solve: If x + 5 = 12, what is x?',
    @MaxSteps = 5,
    @SessionId = @SessionId;

-- Check reasoning chain
SELECT * FROM dbo.ReasoningChains WHERE SessionId = @SessionId;
SELECT * FROM dbo.ReasoningSteps WHERE ChainId IN (SELECT ChainId FROM dbo.ReasoningChains WHERE SessionId = @SessionId);
```

**Expected**: Reasoning chain stored with steps.

---

## Step 8: Enable OODA Loop (Autonomous Self-Improvement)

### Start Service Broker

```sql
USE Hartonomous;

-- Enable Service Broker
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Verify queues and services
SELECT name, is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
SELECT name FROM sys.service_queues WHERE is_ms_shipped = 0;
```

### Trigger Initial OODA Cycle

```sql
-- Send initial message to start loop
DECLARE @handle UNIQUEIDENTIFIER;

BEGIN DIALOG CONVERSATION @handle
    FROM SERVICE [AnalyzeService]
    TO SERVICE 'AnalyzeService'
    ON CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
    WITH ENCRYPTION = OFF;

SEND ON CONVERSATION @handle
    MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
    (N'{"trigger": "manual", "timestamp": "' + CONVERT(NVARCHAR(50), GETUTCDATE(), 127) + '"}');
```

**Expected**: OODA loop starts running every 15-60 minutes.

### Monitor OODA Loop

```sql
-- Check OODA execution history
SELECT TOP 20 *
FROM dbo.OODALoopMetrics
ORDER BY ExecutedAt DESC;

-- Check generated hypotheses
SELECT TOP 10 *
FROM dbo.PendingActions
ORDER BY CreatedAt DESC;
```

---

## Common Issues

### Issue 1: CLR Not Enabled

**Error**: `CLR integration is disabled`

**Fix**:
```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

### Issue 2: Assembly Loading Fails

**Error**: `Could not load assembly 'Hartonomous.Database'`

**Fix**:
```sql
-- Check if assembly registered
SELECT * FROM sys.assemblies WHERE name = 'Hartonomous.Database';

-- If missing, register manually:
CREATE ASSEMBLY [Hartonomous.Database]
FROM 'C:\path\to\Hartonomous.Database.dll'
WITH PERMISSION_SET = UNSAFE;
```

### Issue 3: Spatial Index Build Fails

**Error**: `Cannot create spatial index - bounding box invalid`

**Fix**: Check GEOMETRY values are within bounding box (-1000, -1000, 1000, 1000)

### Issue 4: Workers Can't Connect

**Error**: `Cannot connect to SQL Server`

**Fix**: Update connection string in `appsettings.Development.json`:
```json
"DefaultConnection": "Server=localhost,1433;Database=Hartonomous;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

### Issue 5: Neo4j Warnings

**Warning**: `Neo4j connection failed`

**Fix**: Either:
- Start Neo4j: `neo4j console`
- Or: Ignore warnings if not using provenance tracking yet

---

## Next Steps

**For Developers**:
1. Read [Architecture](./ARCHITECTURE.md) to understand the system
2. Review [Rewrite Guide](./docs/rewrite-guide/) for complete technical details
3. Explore [API Reference](./docs/api/) for available endpoints
4. Check [Setup Guides](./docs/setup/) for advanced configuration

**For Operators**:
1. Review [Operations Runbooks](./docs/operations/) for monitoring and troubleshooting
2. Set up production deployment following [Deployment Guide](./docs/setup/deployment.md)
3. Configure backups and high availability

**For Users**:
1. Try [Cross-Modal Examples](./docs/rewrite-guide/22-Cross-Modal-Generation-Examples.md)
2. Explore [User Guides](./docs/guides/) for tutorials
3. Experiment with reasoning frameworks and agent tools

---

## Verify Complete Setup

Run this verification script:

```sql
USE Hartonomous;

-- 1. Tables exist
SELECT 'Tables' AS Component, COUNT(*) AS Count FROM sys.tables;

-- 2. CLR functions registered
SELECT 'CLR Functions' AS Component, COUNT(*) AS Count
FROM sys.objects WHERE type IN ('FN', 'FS', 'FT', 'PC') AND is_ms_shipped = 0;

-- 3. Spatial indexes created
SELECT 'Spatial Indexes' AS Component, COUNT(*) AS Count
FROM sys.indexes WHERE type_desc = 'SPATIAL';

-- 4. Service Broker enabled
SELECT 'Service Broker' AS Component,
       CASE WHEN is_broker_enabled = 1 THEN 'Enabled' ELSE 'Disabled' END AS Status
FROM sys.databases WHERE name = 'Hartonomous';

-- 5. Atoms ingested
SELECT 'Atoms' AS Component, COUNT(*) AS Count FROM dbo.Atoms;

-- 6. Embeddings generated
SELECT 'Embeddings' AS Component, COUNT(*) AS Count FROM dbo.AtomEmbeddings WHERE SpatialGeometry IS NOT NULL;

-- 7. Reasoning chains
SELECT 'Reasoning Chains' AS Component, COUNT(*) AS Count FROM dbo.ReasoningChains;
```

**Expected output** (after running test queries):
```
Component           Count/Status
------------------  ------------
Tables              50+
CLR Functions       20+
Spatial Indexes     10+
Service Broker      Enabled
Atoms               10+
Embeddings          10+
Reasoning Chains    1+
```

---

## Success! 🚀

You now have a fully functional Hartonomous system:
- ✅ Database deployed with spatial indexes
- ✅ CLR functions registered
- ✅ Workers running (ingestion, embedding, spatial projection)
- ✅ OODA loop enabled (autonomous self-improvement)
- ✅ Reasoning frameworks operational

**Start exploring:**
- Run spatial queries (O(log N) performance)
- Use reasoning frameworks (Chain of Thought, Tree of Thought, Reflexion)
- Try cross-modal synthesis (image → audio, video → text, etc.)
- Watch the OODA loop optimize itself automatically

For questions or issues, see:
- [Architecture Documentation](./ARCHITECTURE.md)
- [Complete Rewrite Guide](./docs/rewrite-guide/)
- [Operations Runbooks](./docs/operations/)
