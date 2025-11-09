# Hartonomous Database Deployment Guide

## Executive Summary

This guide documents the **unified enterprise-grade database deployment solution** for Hartonomous, resolving architectural conflicts between EF Core migrations and SQL scripts while supporting both Windows and Linux SQL Server 2025 environments.

## Architecture Resolution

### Problem Identified

During comprehensive analysis, a critical architectural conflict was discovered:

- **EF Core migrations** create domain tables: `Atoms`, `AtomEmbeddings`, `Models`, `ModelLayers`, `InferenceRequests`, etc.
- **SQL scripts** ALSO create the same tables: `dbo.Atoms.sql`, `dbo.AtomEmbeddings.sql`, `dbo.ModelStructure.sql`, etc.
- **Result**: Deployment conflicts, duplicate object creation attempts, unpredictable behavior

### Solution Architecture

**Clear ownership boundaries established:**

| Component | Owner | Purpose |
|-----------|-------|---------|
| **Domain Tables** | EF Core Migrations | CRUD operations, standard columns, FK relationships |
| **Advanced Features** | SQL Scripts | VECTOR types, GEOMETRY spatial, CLR UDTs, SQL Graph, Temporal tables, FILESTREAM |
| **CLR Assemblies** | Manual Deployment | UNSAFE assemblies for vector/geometry operations, SIMD optimizations |
| **Service Broker** | SQL Scripts | OODA loop messaging infrastructure |
| **Stored Procedures** | SQL Scripts | Business logic, inference, generation, autonomous improvement |

### Deployment Order (Dependency Graph)

```
1. Prerequisites
   ├─ Database creation
   ├─ FILESTREAM filegroup
   └─ Schemas (dbo, graph, provenance)
   
2. EF Core Migrations
   ├─ Domain tables (Atoms, Models, etc.)
   ├─ Standard indexes
   └─ Foreign key relationships
   
3. CLR Assemblies (UNSAFE)
   ├─ System.Numerics.Vectors.dll
   ├─ MathNet.Numerics.dll
   ├─ Newtonsoft.Json.dll
   ├─ System.ServiceModel.Internals.dll
   ├─ SMDiagnostics.dll
   ├─ System.Runtime.Serialization.dll
   └─ SqlClrFunctions.dll (PRIMARY)
   
4. Advanced Tables
   ├─ Attention generation tables
   ├─ Reasoning framework tables
   ├─ Stream orchestration tables
   ├─ Provenance tracking tables
   ├─ Spatial landmarks
   ├─ Inference cache (In-Memory OLTP)
   ├─ Billing ledger (memory-optimized)
   ├─ SQL Graph (AtomGraphNodes, AtomGraphEdges)
   └─ Temporal tables (weight history)
   
5. Service Broker
   ├─ Message types
   ├─ Contracts
   ├─ Queues (AnalyzeQueue, HypothesizeQueue, ActQueue, LearnQueue)
   └─ Services (OODA loop infrastructure)
   
6. CLR Bindings
   ├─ Vector operations (dot product, cosine similarity, softmax)
   ├─ Image processing (point cloud generation, patches)
   ├─ Audio processing (waveform, harmonic generation)
   ├─ Concept discovery (DBSCAN clustering)
   ├─ File system operations (autonomous deployment)
   └─ Shell command execution
   
7. Stored Procedures
   ├─ OODA loop (sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn)
   ├─ Atom ingestion (sp_IngestAtom, atomization)
   ├─ Semantic search (sp_SemanticSearch - hybrid spatial+vector)
   ├─ Text generation (sp_GenerateText)
   ├─ Ensemble inference (sp_EnsembleInference)
   ├─ Autonomous improvement (sp_AutonomousImprovement)
   ├─ Weight rollback (temporal table operations)
   └─ Helper functions (fn_SelectModelsForTask, fn_EnsembleAtomScores)
   
8. Verification
   ├─ Table count validation
   ├─ CLR assembly verification
   ├─ Service Broker status check
   └─ Vector operation smoke test
```

## Deployment Scripts

### Unified Deployment Script

**Primary deployment tool:**

```powershell
.\scripts\deploy-database-unified.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -FilestreamPath "D:\SQLDATA\Filestream" `
    -Verbose
```

**Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Server` | string | `localhost` | SQL Server instance name |
| `-Database` | string | `Hartonomous` | Target database name |
| `-Credential` | PSCredential | (Windows Auth) | SQL authentication credentials |
| `-FilestreamPath` | string | `D:\SQLDATA\Filestream` | FILESTREAM filegroup path |
| `-SkipFilestream` | switch | `false` | Skip FILESTREAM configuration |
| `-SkipCLR` | switch | `false` | Skip CLR assembly deployment |
| `-SkipEFMigrations` | switch | `false` | Skip EF Core migrations |
| `-DryRun` | switch | `false` | Simulate deployment without changes |
| `-Verbose` | switch | `false` | Detailed logging |
| `-Force` | switch | `false` | Continue on non-fatal errors |

### Platform-Specific Examples

#### Windows (HART-DESKTOP)

```powershell
# Full deployment with FILESTREAM
.\scripts\deploy-database-unified.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -FilestreamPath "D:\SQLDATA\Filestream" `
    -Verbose

# Dry run to preview changes
.\scripts\deploy-database-unified.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -DryRun `
    -Verbose
```

#### Linux (HART-SERVER via Azure Arc)

```powershell
# Deploy to remote Linux SQL Server
$cred = Get-Credential -Message "SQL Server credentials"

.\scripts\deploy-database-unified.ps1 `
    -Server "hart-server.local" `
    -Database "Hartonomous" `
    -Credential $cred `
    -FilestreamPath "/var/opt/mssql/filestream" `
    -Verbose

# Or via Azure DevOps over SSH (use scripts/deploy/ modular approach)
# See scripts/deploy/README.md for Azure Arc deployment
```

## Database Schema Overview

### Core Domain Tables (EF Core)

**40+ tables created by InitialCreate migration:**

- **`Atoms`**: Content-addressable atomic storage (SHA-256 deduplication)
- **`AtomEmbeddings`**: VECTOR(1998) embeddings with spatial projection (x,y,z)
- **`AtomPayloadStore`**: FILESTREAM binary blob storage
- **`Models`**: Model metadata and configuration
- **`ModelLayers`**: Layer structure and tensor shapes
- **`TensorAtomCoefficients`**: Neural network weights
- **`InferenceRequests`**: Inference tracking and metrics
- **`GenerationStreams`**: Provenance lineage for generated content
- **`BillingRatePlans`**: Multi-tenant billing infrastructure
- **`BillingUsageLedger`**: Usage tracking ledger
- **`AutonomousImprovementHistory`**: Self-modification provenance

### Advanced Features (SQL Scripts)

**Attention & Reasoning:**

- `Attention.AttentionGenerationLog`
- `Reasoning.ReasoningChains`
- `Reasoning.SelfConsistencyResults`
- `Reasoning.MultiPathReasoning`

**Spatial Projection:**

- `dbo.SpatialLandmarks`: VECTOR(1998) landmark anchors for projection system
- `dbo.AtomEmbeddings.SpatialGeometry`: GEOMETRY point cloud representation

**SQL Graph:**

- `graph.AtomGraphNodes`: NODE table with AtomId FK
- `graph.AtomGraphEdges`: EDGE table with typed relationships (DerivedFrom, ComponentOf, SimilarTo, etc.)

**Temporal Tables:**

- `dbo.TensorAtomCoefficients` (SYSTEM_VERSIONING enabled)
- `dbo.Weights` (temporal weight history tracking)

**In-Memory OLTP:**

- `dbo.BillingUsageLedger_InMemory`: HASH indexes, latch-free inserts for high-frequency billing

**Provenance:**

- `provenance.Concepts`: Unsupervised concept discovery via clustering
- `provenance.AtomConcepts`: Multi-label concept bindings
- `provenance.ConceptEvolution`: Temporal concept tracking
- `provenance.GenerationStreams`: AtomicStream CLR UDT for lineage

## CLR Assembly Functions

### Vector Operations

```sql
-- Dot product
SELECT dbo.clr_VectorDotProduct(@v1, @v2);

-- Cosine similarity
SELECT dbo.clr_VectorCosineSimilarity(@v1, @v2);

-- Normalize vector
SELECT dbo.clr_VectorNormalize(@vector);

-- Softmax activation
SELECT dbo.clr_VectorSoftmax(@logits);
```

### Image Processing

```sql
-- Convert image to spatial point cloud
SELECT dbo.clr_ImageToPointCloud(@image, @width, @height, @sampleStep);

-- Deconstruct into patches (TVF)
SELECT * FROM dbo.clr_DeconstructImageToPatches(
    @rawImage, @width, @height, @patchSize, @strideSize
);

-- Generate guided image patches
SELECT * FROM dbo.clr_GenerateImagePatches(
    @width, @height, @patchSize, @steps, 
    @guidanceScale, @guideX, @guideY, @guideZ, @seed
);
```

### Audio Processing

```sql
-- Audio to waveform geometry
SELECT dbo.clr_AudioToWaveform(@audio, @channels, @sampleRate, @maxPoints);

-- Generate harmonic tone
SELECT dbo.clr_GenerateHarmonicTone(
    @fundamentalHz, @durationMs, @sampleRate, 
    @channelCount, @amplitude, @secondHarmonic, @thirdHarmonic
);
```

### Concept Discovery (CLR Table-Valued Functions)

```sql
-- Discover emergent concepts via DBSCAN clustering
SELECT * FROM dbo.fn_DiscoverConcepts(
    @MinClusterSize = 5,
    @CoherenceThreshold = 0.7,
    @MaxConcepts = 100,
    @TenantId = 0
);

-- Bind atoms to concepts
SELECT * FROM dbo.fn_BindConcepts(
    @AtomId = 12345,
    @SimilarityThreshold = 0.8,
    @MaxConceptsPerAtom = 5,
    @TenantId = 0
);
```

### File System Operations (Autonomous Deployment)

```sql
-- Write file
SELECT dbo.clr_WriteFileText(@FilePath, @Content);

-- Execute shell command (security: ArgumentList to prevent injection)
SELECT * FROM dbo.clr_ExecuteShellCommand(
    @Executable = 'git',
    @Arguments = '["commit", "-m", "Autonomous improvement"]',
    @WorkingDirectory = 'D:\Repositories\Hartonomous',
    @TimeoutSeconds = 30
);
```

## Key Stored Procedures

### OODA Loop (Autonomous Improvement)

```sql
-- Phase 1: Observe & Analyze
EXEC dbo.sp_Analyze 
    @TenantId = 0, 
    @AnalysisScope = 'full', 
    @LookbackHours = 24;

-- Phase 2: Orient & Hypothesize
EXEC dbo.sp_Hypothesize @TenantId = 0;

-- Phase 3: Decide & Act
EXEC dbo.sp_Act @TenantId = 0, @AutoApproveThreshold = 3;

-- Phase 4: Learn & Adapt
EXEC dbo.sp_Learn @TenantId = 0;

-- Full autonomous improvement cycle
EXEC dbo.sp_AutonomousImprovement
    @DryRun = 1,
    @MaxChangesPerRun = 1,
    @RequireHumanApproval = 1,
    @TargetArea = 'performance',
    @Debug = 1;
```

### Atom Ingestion

```sql
DECLARE @AtomId BIGINT;

-- Ingest content-addressable atom
EXEC dbo.sp_IngestAtom
    @Content = @binaryData,
    @ContentType = 'image/png',
    @Metadata = '{"source": "user_upload"}',
    @TenantId = 0,
    @GenerateEmbedding = 1,
    @ModelId = NULL,
    @AtomId = @AtomId OUTPUT;
```

### Semantic Search (Hybrid Spatial + Vector)

```sql
-- Hybrid search (O(log n) spatial filter + exact vector rerank)
EXEC dbo.sp_SemanticSearch
    @query_embedding = '[0.123, 0.456, ...]',
    @query_dimension = 768,
    @top_k = 10,
    @category = 'text',
    @use_hybrid = 1;
```

### Text Generation

```sql
-- Multi-model ensemble text generation
EXEC dbo.sp_GenerateText
    @prompt = 'Explain quantum entanglement',
    @max_tokens = 256,
    @temperature = 0.7,
    @ModelIds = '1,2,3',
    @top_k = 5;
```

### Temporal Weight Operations

```sql
-- Create snapshot before risky experiment
EXEC dbo.sp_CreateWeightSnapshot
    @SnapshotName = 'BeforeExperiment',
    @ModelId = NULL,
    @Description = 'Baseline weights before RL training';

-- Rollback to specific timestamp (dry run)
EXEC dbo.sp_RollbackWeightsToTimestamp
    @TargetDateTime = '2025-11-08 10:00:00',
    @ModelId = NULL,
    @DryRun = 1;

-- Restore from snapshot
EXEC dbo.sp_RestoreWeightSnapshot
    @SnapshotName = 'BeforeExperiment',
    @DryRun = 0;
```

## Service Broker (OODA Loop Messaging)

### Architecture

```
AnalyzeService → HypothesizeMessage → HypothesizeQueue → sp_Hypothesize (activation)
    ↑                                                              ↓
    |                                                         ActMessage
    |                                                              ↓
LearnQueue ← LearnMessage ← ActQueue ← (activation) sp_Act
    ↓
sp_Learn (activation) → AnalyzeMessage → AnalyzeQueue → sp_Analyze
```

### Message Flow

1. **sp_Analyze** sends `HypothesizeMessage` to `HypothesizeQueue`
2. **sp_Hypothesize** activated, processes observations, sends `ActMessage`
3. **sp_Act** activated, executes safe actions, queues dangerous ones, sends `LearnMessage`
4. **sp_Learn** activated, measures performance delta, updates weights, restarts loop

### Monitoring

```sql
-- Check queue depths
SELECT 
    name AS QueueName,
    (SELECT COUNT(*) FROM sys.transmission_queue WHERE transmission_status = name) AS Messages
FROM sys.service_queues
WHERE name LIKE '%Queue';

-- View active conversations
SELECT * FROM sys.conversation_endpoints;
```

## Troubleshooting

### Common Issues

#### 1. CLR Assembly Deployment Fails

**Error:** `CREATE ASSEMBLY failed because assembly '<name>' is not authorized for PERMISSION_SET = UNSAFE`

**Solution:**

```sql
-- Option A: Disable CLR strict security (development only)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

-- Option B: Add to trusted assemblies (production)
DECLARE @hash VARBINARY(64) = HASHBYTES('SHA2_512', <assembly_bytes>);
EXEC sys.sp_add_trusted_assembly @hash, N'<AssemblyName>';
```

#### 2. FILESTREAM Not Enabled

**Error:** `Cannot add file to filegroup 'FilestreamGroup'`

**Solution:**

```powershell
# Windows
# 1. Enable FILESTREAM in SQL Configuration Manager
# 2. Restart SQL Server
# 3. Enable at instance level:

sqlcmd -Q "EXEC sp_configure 'filestream access level', 2; RECONFIGURE;"

# Linux
sudo /opt/mssql/bin/mssql-conf set filelevel.filelevel 2
sudo systemctl restart mssql-server
```

#### 3. EF Migration Connection Failure

**Error:** `A network-related or instance-specific error occurred`

**Solution:**

```powershell
# Check SQL Server is running
Get-Service MSSQL*

# Test connectivity
Test-NetConnection -ComputerName localhost -Port 1433

# Check connection string
$env:ConnectionStrings__DefaultConnection
```

#### 4. Temporal Tables Creation Error

**Error:** `Cannot create temporal table with FILESTREAM columns`

**Solution:** Temporal tables don't support FILESTREAM. Use external provenance tracking instead:

```sql
-- Store FILESTREAM metadata, not the stream itself
ALTER TABLE dbo.TensorAtomCoefficients
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));
```

#### 5. Service Broker Not Activating

**Error:** Procedures not executing automatically

**Solution:**

```sql
-- Enable Service Broker
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Check activation status
SELECT name, is_activation_enabled, max_queue_readers
FROM sys.service_queues
WHERE name LIKE '%Queue';

-- Manually activate if needed
ALTER QUEUE HypothesizeQueue WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Hypothesize,
    MAX_QUEUE_READERS = 1,
    EXECUTE AS OWNER
);
```

## Performance Optimization

### Spatial Indexing

```sql
-- Create spatial index on AtomEmbeddings.SpatialGeometry
CREATE SPATIAL INDEX idx_spatial_geometry
ON dbo.AtomEmbeddings (SpatialGeometry)
WITH (
    BOUNDING_BOX = (-1000, -1000, 1000, 1000),
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16
);
```

### Bucket-Based Similarity Search

```sql
-- Pre-filter by spatial bucket before exact vector distance
WITH SpatialCandidates AS (
    SELECT AtomEmbeddingId, SpatialBucketX, SpatialBucketY, SpatialBucketZ
    FROM dbo.AtomEmbeddings
    WHERE SpatialBucketX BETWEEN @x - 1 AND @x + 1
      AND SpatialBucketY BETWEEN @y - 1 AND @y + 1
      AND SpatialBucketZ BETWEEN @z - 1 AND @z + 1
)
SELECT TOP 10
    ae.AtomId,
    VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @queryVector) AS Distance
FROM SpatialCandidates sc
INNER JOIN dbo.AtomEmbeddings ae ON sc.AtomEmbeddingId = ae.AtomEmbeddingId
ORDER BY Distance;
```

### In-Memory OLTP for Billing

```sql
-- Already configured: dbo.BillingUsageLedger_InMemory
-- HASH indexes on TenantId, Operation for latch-free inserts

INSERT INTO dbo.BillingUsageLedger_InMemory (
    TenantId, PrincipalId, Operation, Units, BaseRate, TotalCost
)
VALUES (
    @TenantId, @PrincipalId, @Operation, @Units, @BaseRate, @TotalCost
);
-- No locking, scales to millions of inserts/sec
```

### Query Store Monitoring

```sql
-- Enable Query Store for performance analysis
ALTER DATABASE Hartonomous 
SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 60,
    MAX_STORAGE_SIZE_MB = 1024,
    QUERY_CAPTURE_MODE = AUTO
);

-- Identify slow queries
SELECT TOP 10
    qt.query_sql_text,
    rs.avg_duration / 1000.0 AS avg_duration_ms,
    rs.count_executions,
    (rs.avg_duration * rs.count_executions) / 1000.0 AS total_duration_ms
FROM sys.query_store_query q
INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
INNER JOIN sys.query_store_plan p ON q.query_id = p.query_id
INNER JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
ORDER BY total_duration_ms DESC;
```

## Security Considerations

### CLR Security

**UNSAFE assemblies have unrestricted access to SQL Server process.**

**Development:**
- `clr strict security = 0` acceptable for local development
- Use `PERMISSION_SET = UNSAFE` freely

**Production:**
- Enable `clr strict security = 1`
- Use `sys.sp_add_trusted_assembly` for explicit whitelisting:

```sql
DECLARE @assemblyHash VARBINARY(64);
DECLARE @assemblyBytes VARBINARY(MAX) = <load_from_file>;

SET @assemblyHash = HASHBYTES('SHA2_512', @assemblyBytes);

EXEC sys.sp_add_trusted_assembly 
    @hash = @assemblyHash,
    @description = N'SqlClrFunctions - Vector/geometry operations';

CREATE ASSEMBLY SqlClrFunctions
FROM <assembly_bytes>
WITH PERMISSION_SET = UNSAFE;
```

### Tenant Isolation

```sql
-- TenantSecurityPolicy table controls CLR whitelists per tenant
SELECT * FROM dbo.TenantSecurityPolicy
WHERE TenantId = @TenantId AND IsActive = 1;

-- CLR functions check policy before execution
-- Example: FileSystemFunctions validates paths against whitelist
```

### Row-Level Security (Future)

```sql
-- Implement RLS for multi-tenant data isolation
CREATE SECURITY POLICY TenantSecurityPolicy
ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId)
ON dbo.Atoms,
ADD BLOCK PREDICATE dbo.fn_TenantAccessPredicate(TenantId)
ON dbo.Atoms;
```

## Maintenance

### Regular Tasks

```sql
-- Update statistics weekly
EXEC sp_updatestats;

-- Rebuild fragmented indexes monthly
EXEC dbo.sp_RebuildFragmentedIndexes;

-- Purge old inference history (>90 days)
DELETE FROM dbo.InferenceRequests
WHERE RequestTimestamp < DATEADD(DAY, -90, GETUTCDATE());

-- Compact temporal history tables
ALTER TABLE dbo.TensorAtomCoefficients
SET (HISTORY_RETENTION_PERIOD = 6 MONTHS);
```

### Backup Strategy

```sql
-- Full backup weekly
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Full_<timestamp>.bak'
WITH COMPRESSION, CHECKSUM, STATS = 10;

-- Differential backup daily
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Diff_<timestamp>.bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, STATS = 10;

-- Transaction log backup hourly (FULL recovery model)
BACKUP LOG Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Log_<timestamp>.trn'
WITH COMPRESSION, CHECKSUM, STATS = 10;
```

## Migration from Old Architecture

### If You Have Existing Corrupted deploy-database.ps1

```powershell
# 1. Backup existing database
sqlcmd -Q "BACKUP DATABASE Hartonomous TO DISK = 'D:\Backups\Hartonomous_PreMigration.bak'"

# 2. Drop database (if starting fresh)
sqlcmd -Q "DROP DATABASE IF EXISTS Hartonomous"

# 3. Use new unified deployment
.\scripts\deploy-database-unified.ps1 -Verbose

# 4. Restore data from backup (if needed)
# ... custom data migration scripts ...
```

### Schema Migration

If you have data in old schema structure:

1. Export data to JSON/CSV
2. Deploy new schema with unified script
3. Import data using sp_IngestAtom for atoms
4. Rebuild embeddings and spatial projections
5. Run concept discovery to regenerate clusters

## References

- **EF Core Migration**: `src\Hartonomous.Data\Migrations\20251107210027_InitialCreate.cs`
- **DbContext**: `src\Hartonomous.Data\HartonomousDbContext.cs`
- **CLR Source**: `src\Hartonomous.SqlClr\`
- **SQL Tables**: `sql\tables\`
- **SQL Procedures**: `sql\procedures\`
- **Service Broker Setup**: `scripts\setup-service-broker.sql`
- **Modular Deployment (Azure Arc)**: `scripts\deploy\`

## Support

For deployment issues:

1. Check deployment log: `deployment-log-<timestamp>.json`
2. Review error log: `deployment-error-<timestamp>.json`
3. Run with `-DryRun -Verbose` to preview operations
4. Use `-Force` to continue through non-fatal errors
5. Check SQL Server error log: `SELECT * FROM sys.dm_os_ring_buffers`

---

**Last Updated:** 2025-01-19  
**Schema Version:** EF InitialCreate + Advanced Features v1.0  
**CLR Assembly Version:** SqlClrFunctions 7-assembly configuration
