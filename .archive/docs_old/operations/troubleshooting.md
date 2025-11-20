# Troubleshooting

**Last Updated**: November 19, 2025  
**Status**: Production Ready

## Overview

Common issues in Hartonomous production deployments: slow spatial queries, OODA loop failures, CLR assembly errors, Neo4j sync lag, and Service Broker stalls.

## Slow Spatial Queries

### Symptom

Queries using `STIntersects`, `STDistance`, or `STBuffer` taking >50ms.

### Diagnosis

**Step 1: Check execution plan**
```sql
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- Example slow query
DECLARE @QueryGeometry GEOMETRY = GEOMETRY::Point(10, 20, 0);
SELECT TOP 10 AtomId, Content
FROM dbo.Atoms a
INNER JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
WHERE ae.SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

**Look for**:
- Scan instead of Seek on spatial index
- High logical reads (>10000)
- Execution time >50ms

**Step 2: Check spatial index fragmentation**
```sql
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.AtomEmbeddings'), NULL, NULL, 'DETAILED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE i.name = 'IX_AtomEmbeddings_SpatialGeometry';
```

**Problem if**: Fragmentation >30%

### Solution

**1. Rebuild spatial index**
```sql
ALTER INDEX IX_AtomEmbeddings_SpatialGeometry 
ON dbo.AtomEmbeddings 
REBUILD WITH (
    MAXDOP = 4,
    ONLINE = ON,
    SORT_IN_TEMPDB = ON
);
```

**2. Update statistics**
```sql
UPDATE STATISTICS dbo.AtomEmbeddings WITH FULLSCAN;
```

**3. Force index hint if planner ignores spatial index**
```sql
SELECT TOP 10 AtomId, Content
FROM dbo.Atoms a
INNER JOIN dbo.AtomEmbeddings ae WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
    ON a.AtomId = ae.AtomId
WHERE ae.SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1;
```

**4. Reduce semantic radius if too large**
```sql
-- Check candidate pool size
SELECT COUNT(*) AS CandidateCount
FROM dbo.AtomEmbeddings
WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1;

-- If CandidateCount >100K, reduce radius to 20 or 15
```

**5. Consider columnstore for large scans**
```sql
CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_AtomEmbeddings
ON dbo.AtomEmbeddings (AtomId, EmbeddingVector, SpatialGeometry);
```

## OODA Loop Failures

### Symptom

OODA phases timing out or returning errors (>5% error rate).

### Diagnosis

**Step 1: Check OODA logs**
```sql
SELECT TOP 100
    Phase,
    SessionId,
    ErrorMessage,
    DATEDIFF(MILLISECOND, StartTime, EndTime) AS DurationMs,
    StartTime
FROM dbo.OODALogs
WHERE ErrorMessage IS NOT NULL
ORDER BY StartTime DESC;
```

**Step 2: Identify failing phase**
```sql
SELECT 
    Phase,
    COUNT(*) AS ErrorCount,
    AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgDurationMs
FROM dbo.OODALogs
WHERE ErrorMessage IS NOT NULL
    AND StartTime >= DATEADD(HOUR, -1, SYSDATETIME())
GROUP BY Phase
ORDER BY ErrorCount DESC;
```

### Solution

**Problem: Orient phase timing out (>300ms)**

**Cause**: Hypothesis generation calling slow external LLM

**Fix 1**: Increase timeout
```csharp
// In CesConsumer or API
var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
```

**Fix 2**: Use async/await for LLM calls
```csharp
var response = await httpClient.PostAsync(llmEndpoint, content);
```

**Fix 3**: Cache frequent hypotheses
```sql
CREATE TABLE dbo.HypothesisCache (
    InputHash VARBINARY(32) PRIMARY KEY,
    Hypothesis NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT SYSDATETIME()
);

-- Check cache before calling LLM
DECLARE @InputHash VARBINARY(32) = HASHBYTES('SHA2_256', @Input);
DECLARE @CachedHypothesis NVARCHAR(MAX) = (
    SELECT Hypothesis 
    FROM dbo.HypothesisCache 
    WHERE InputHash = @InputHash 
        AND CreatedAt >= DATEADD(HOUR, -24, SYSDATETIME())
);

IF @CachedHypothesis IS NULL
    EXEC dbo.sp_InvokeExternalLLM @Input, @CachedHypothesis OUTPUT;
```

**Problem: Act phase failing (CLR errors)**

**Diagnosis**:
```sql
SELECT 
    ErrorMessage,
    COUNT(*) AS Occurrences
FROM dbo.OODALogs
WHERE Phase = 'Act'
    AND ErrorMessage IS NOT NULL
GROUP BY ErrorMessage
ORDER BY Occurrences DESC;
```

**Fix**: See "CLR Assembly Errors" section below.

## CLR Assembly Errors

### Symptom

CLR functions/aggregates throwing errors: "Could not load file or assembly", "Security exception", "Invalid serialization".

### Diagnosis

**Step 1: Check CLR assembly status**
```sql
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc,
    a.is_visible,
    a.create_date
FROM sys.assemblies a
WHERE a.name LIKE 'Hartonomous.Clr%';
```

**Step 2: Check for missing dependencies**
```sql
-- List assembly dependencies
SELECT 
    a.name AS AssemblyName,
    af.name AS DependentFile
FROM sys.assemblies a
CROSS APPLY sys.assembly_files af
WHERE a.name = 'Hartonomous.Clr';
```

### Solution

**Problem: "Could not load file or assembly 'System.Numerics.Tensors'"**

**Cause**: Missing dependency in SQL Server CLR

**Fix 1**: Deploy dependency assembly
```sql
CREATE ASSEMBLY [System.Numerics.Tensors]
FROM 'D:\Assemblies\System.Numerics.Tensors.dll'
WITH PERMISSION_SET = SAFE;
```

**Fix 2**: Use ILMerge to bundle dependencies
```powershell
# Merge all dependencies into single DLL
ilmerge /target:library /out:Hartonomous.Clr.Merged.dll `
    Hartonomous.Clr.dll `
    System.Numerics.Tensors.dll `
    System.Memory.dll
```

**Problem: "Security exception: UNSAFE assembly disabled"**

**Cause**: SQL Server 2025 requires explicit UNSAFE permission

**Fix**:
```sql
-- Enable CLR strict security (SQL Server 2025)
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;

-- Drop and recreate assembly with UNSAFE
DROP ASSEMBLY [Hartonomous.Clr];

CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Assemblies\Hartonomous.Clr.dll'
WITH PERMISSION_SET = UNSAFE;

-- Grant UNSAFE assembly permission
EXEC sp_add_trusted_assembly 
    @hash = 0x<SHA512 hash of DLL>,
    @description = 'Hartonomous CLR functions';
```

**Get assembly hash**:
```powershell
$bytes = [System.IO.File]::ReadAllBytes("D:\Assemblies\Hartonomous.Clr.dll")
$hash = [System.Security.Cryptography.SHA512]::Create().ComputeHash($bytes)
$hashString = [System.BitConverter]::ToString($hash).Replace("-", "")
Write-Host "0x$hashString"
```

**Problem: "Invalid serialization data in IBinarySerialize.Read()"**

**Cause**: CLR aggregate deserialization failing (version mismatch or corrupt data)

**Diagnosis**:
```sql
-- Try to invoke aggregate on small dataset
SELECT dbo.clr_ChainOfThoughtCoherence(SpatialGeometry)
FROM (
    SELECT TOP 10 SpatialGeometry 
    FROM dbo.ReasoningSteps
) AS test;
```

**Fix 1**: Drop and recreate CLR objects
```sql
-- Drop all CLR functions/aggregates
DROP AGGREGATE dbo.clr_ChainOfThoughtCoherence;
DROP AGGREGATE dbo.clr_SelfConsistency;
DROP FUNCTION dbo.clr_CosineSimilarity;

-- Drop assembly
DROP ASSEMBLY [Hartonomous.Clr];

-- Redeploy
CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Assemblies\Hartonomous.Clr.dll'
WITH PERMISSION_SET = UNSAFE;

-- Recreate functions/aggregates
-- (Run CLR deployment script)
```

**Fix 2**: Clear serialization cache
```sql
-- Truncate intermediate results using aggregate
DELETE FROM dbo.ReasoningSteps WHERE StepType = 'IntermediateResult';
```

## Neo4j Sync Lag

### Symptom

Neo4j provenance graph lagging behind SQL Server by >60 seconds.

### Diagnosis

**Step 1: Check sync queue depth**
```sql
SELECT 
    EntityType,
    COUNT(*) AS PendingItems,
    MIN(CreatedAt) AS OldestPending,
    DATEDIFF(SECOND, MIN(CreatedAt), SYSDATETIME()) AS LagSeconds
FROM dbo.Neo4jSyncQueue
WHERE IsSynced = 0
    AND RetryCount < 5
GROUP BY EntityType
ORDER BY LagSeconds DESC;
```

**Step 2: Check failed syncs**
```sql
SELECT TOP 100
    EntityType,
    EntityId,
    Operation,
    RetryCount,
    LastError,
    CreatedAt
FROM dbo.Neo4jSyncQueue
WHERE IsSynced = 0
    AND RetryCount >= 5
ORDER BY CreatedAt DESC;
```

### Solution

**Problem: Queue depth >10K items**

**Cause**: Neo4jSyncWorker not keeping up with ingestion rate

**Fix 1**: Increase worker parallelism
```csharp
// In Hartonomous.Neo4jSync/Program.cs
services.AddHostedService<Neo4jSyncWorker>(sp =>
{
    return new Neo4jSyncWorker(
        sp.GetRequiredService<ILogger<Neo4jSyncWorker>>(),
        sp.GetRequiredService<IConfiguration>(),
        parallelism: 8  // Increased from 4
    );
});
```

**Fix 2**: Batch sync operations
```csharp
// Sync in batches of 100 instead of 1-by-1
var batch = await GetNextBatch(batchSize: 100);
await neo4jSession.RunAsync(@"
    UNWIND $batch AS item
    MERGE (a:Atom {atomId: item.atomId})
    SET a.contentType = item.contentType
", new { batch });
```

**Fix 3**: Optimize Neo4j indexes
```cypher
// Ensure indexes exist on frequently queried properties
CREATE INDEX atom_id IF NOT EXISTS FOR (a:Atom) ON (a.atomId);
CREATE INDEX session_id IF NOT EXISTS FOR (s:Session) ON (s.sessionId);
CREATE INDEX edge_type IF NOT EXISTS FOR ()-[r:DERIVED_FROM]-() ON (type(r));
```

**Problem: Failed syncs (RetryCount >=5)**

**Cause**: Neo4j connection errors or malformed Cypher

**Fix 1**: Check Neo4j connectivity
```powershell
# Test Neo4j HTTP API
curl -u neo4j:password http://neo4j-server:7474/db/data/

# Test Bolt protocol
dotnet run --project Hartonomous.Neo4jSync -- test-connection
```

**Fix 2**: Inspect malformed Cypher
```sql
-- Extract failed Cypher query
SELECT TOP 1 MetadataJson, LastError
FROM dbo.Neo4jSyncQueue
WHERE RetryCount >= 5
ORDER BY CreatedAt DESC;
```

**Fix 3**: Requeue failed items
```sql
-- Reset retry count for specific entity type
UPDATE dbo.Neo4jSyncQueue
SET RetryCount = 0, LastError = NULL
WHERE EntityType = 'Atom'
    AND RetryCount >= 5;
```

## Service Broker Stalled

### Symptom

Ingestion queue not processing, models stuck in "ingesting" state.

### Diagnosis

**Step 1: Check queue activation**
```sql
SELECT 
    sq.name AS QueueName,
    sq.is_receive_enabled,
    sq.is_enqueue_enabled,
    sq.activation_procedure,
    sqe.queuing_order,
    sqe.conversation_handle
FROM sys.service_queues sq
LEFT JOIN sys.service_queue_events sqe ON sq.object_id = sqe.queue_id
WHERE sq.name = 'IngestionQueue';
```

**Step 2: Check for poison messages**
```sql
-- Query queue directly
SELECT TOP 10
    message_type_name,
    CAST(message_body AS NVARCHAR(MAX)) AS MessageBody,
    queuing_order
FROM dbo.IngestionQueue;
```

**Step 3: Check transmission errors**
```sql
SELECT 
    from_service_name,
    to_service_name,
    transmission_status,
    CAST(message_body AS NVARCHAR(MAX)) AS MessageBody
FROM sys.transmission_queue
WHERE transmission_status <> '';
```

### Solution

**Problem: Queue disabled**

**Fix**:
```sql
-- Re-enable queue
ALTER QUEUE dbo.IngestionQueue WITH STATUS = ON;
ALTER QUEUE dbo.IngestionQueue WITH ACTIVATION (STATUS = ON);
```

**Problem: Activation procedure crashing**

**Diagnosis**:
```sql
-- Check SQL Server error log
EXEC xp_readerrorlog 0, 1, N'IngestionQueue';
```

**Fix**: Identify crashing SQL in activation stored procedure
```sql
-- Add error handling to activation procedure
ALTER PROCEDURE dbo.sp_ProcessIngestionQueue
AS
BEGIN
    BEGIN TRY
        -- Existing logic
    END TRY
    BEGIN CATCH
        -- Log error
        INSERT INTO dbo.ServiceBrokerErrors (QueueName, ErrorMessage, ErrorTime)
        VALUES ('IngestionQueue', ERROR_MESSAGE(), SYSDATETIME());
    END CATCH;
END;
```

**Problem: Poison messages**

**Fix**: Manually remove and log
```sql
-- Dequeue poison message
DECLARE @ConversationHandle UNIQUEIDENTIFIER;
RECEIVE TOP 1 
    @ConversationHandle = conversation_handle
FROM dbo.IngestionQueue;

-- End conversation
END CONVERSATION @ConversationHandle WITH CLEANUP;

-- Log for investigation
INSERT INTO dbo.PoisonMessages (QueueName, MessageBody, DetectedAt)
VALUES ('IngestionQueue', '<message body>', SYSDATETIME());
```

## Spatial Projection Failures

### Symptom

Atoms ingested but `SpatialGeometry` is NULL, breaking spatial queries.

### Diagnosis

**Step 1: Check for NULL geometries**
```sql
SELECT 
    COUNT(*) AS TotalAtoms,
    SUM(CASE WHEN ae.SpatialGeometry IS NULL THEN 1 ELSE 0 END) AS MissingGeometry,
    CAST(SUM(CASE WHEN ae.SpatialGeometry IS NULL THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS MissingRate
FROM dbo.Atoms a
LEFT JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
WHERE a.IngestTimestamp >= DATEADD(HOUR, -1, SYSDATETIME());
```

**Step 2: Check landmark validity**
```sql
-- Verify landmarks exist and are non-zero
SELECT 
    AxisAssignment,
    LEN(CAST(Vector AS VARBINARY(MAX))) AS VectorLength
FROM dbo.SpatialLandmarks
WHERE AxisAssignment IN ('X', 'Y', 'Z');
```

**Expected**: 3 rows, VectorLength = 6144 (1536 dims × 4 bytes/float)

### Solution

**Problem: Landmarks missing or corrupt**

**Fix**: Regenerate landmarks
```sql
-- Delete existing landmarks
DELETE FROM dbo.SpatialLandmarks;

-- Regenerate via bootstrap procedure
EXEC dbo.sp_BootstrapCognitiveKernel_Epoch1_Axioms;
```

**Problem: CLR projection function failing**

**Diagnosis**:
```sql
-- Test projection manually
DECLARE @TestEmbedding VARBINARY(MAX) = (
    SELECT TOP 1 EmbeddingVector 
    FROM dbo.AtomEmbeddings 
    WHERE SpatialGeometry IS NOT NULL
);

SELECT dbo.clr_LandmarkProjection_ProjectTo3D(
    @TestEmbedding,
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'X'),
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'Y'),
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'Z'),
    42
);
```

**If NULL returned**: CLR function error (see "CLR Assembly Errors")

**Fix**: Reproject all atoms
```sql
-- Reproject missing geometries
UPDATE ae
SET ae.SpatialGeometry = dbo.clr_LandmarkProjection_ProjectTo3D(
    ae.EmbeddingVector,
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'X'),
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'Y'),
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'Z'),
    42
)
FROM dbo.AtomEmbeddings ae
WHERE ae.SpatialGeometry IS NULL;
```

## Performance Degradation

### Symptom

General slowdown across all operations (queries, ingestion, OODA).

### Diagnosis

**Step 1: Check database size**
```sql
EXEC sp_spaceused;

-- Check specific tables
EXEC sp_spaceused 'dbo.Atoms';
EXEC sp_spaceused 'dbo.AtomEmbeddings';
```

**Step 2: Check memory pressure**
```sql
SELECT 
    total_physical_memory_kb / 1024 AS TotalMemoryMB,
    available_physical_memory_kb / 1024 AS AvailableMemoryMB,
    system_memory_state_desc
FROM sys.dm_os_sys_memory;
```

**Step 3: Check CPU pressure**
```sql
SELECT 
    scheduler_id,
    cpu_id,
    status,
    current_tasks_count,
    runnable_tasks_count,
    work_queue_count
FROM sys.dm_os_schedulers
WHERE scheduler_id < 255;
```

### Solution

**Problem: Database >1TB, queries slowing down**

**Fix 1**: Archive old atoms
```sql
-- Move atoms older than 6 months to archive table
SELECT * INTO dbo.AtomsArchive
FROM dbo.Atoms
WHERE IngestTimestamp < DATEADD(MONTH, -6, SYSDATETIME());

-- Delete from main table
DELETE FROM dbo.Atoms
WHERE IngestTimestamp < DATEADD(MONTH, -6, SYSDATETIME());

-- Rebuild indexes
ALTER INDEX ALL ON dbo.Atoms REBUILD;
```

**Fix 2**: Partition large tables
```sql
-- Partition Atoms by IngestTimestamp
CREATE PARTITION FUNCTION pf_IngestMonth (DATETIME2)
AS RANGE RIGHT FOR VALUES (
    '2025-01-01', '2025-02-01', '2025-03-01', ...
);

CREATE PARTITION SCHEME ps_IngestMonth
AS PARTITION pf_IngestMonth ALL TO ([PRIMARY]);

-- Recreate table on partition scheme
CREATE TABLE dbo.Atoms_New (
    AtomId BIGINT IDENTITY PRIMARY KEY,
    Content VARBINARY(MAX),
    ContentType NVARCHAR(100),
    IngestTimestamp DATETIME2 DEFAULT SYSDATETIME()
) ON ps_IngestMonth(IngestTimestamp);
```

**Problem: Memory pressure (AvailableMemoryMB <10% of total)**

**Fix**: Increase SQL Server max memory
```sql
EXEC sp_configure 'max server memory (MB)', 65536;  -- 64GB
RECONFIGURE;
```

**Problem: High CPU (runnable_tasks_count >10)**

**Fix 1**: Limit MAXDOP for expensive queries
```sql
-- Force queries to use fewer CPUs
ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 4;
```

**Fix 2**: Enable Query Store auto-tuning
```sql
ALTER DATABASE Hartonomous
SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    QUERY_CAPTURE_MODE = AUTO,
    MAX_PLANS_PER_QUERY = 200
);

ALTER DATABASE SCOPED CONFIGURATION 
SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
```

## Best Practices

1. **Monitor Proactively**: Use Grafana dashboards (see monitoring.md)
2. **Index Maintenance**: Rebuild spatial indexes weekly
3. **OODA Error Rate**: Alert if >5%
4. **Neo4j Sync**: Keep lag <60 seconds
5. **Service Broker**: Check queue depth daily
6. **CLR Assemblies**: Version assemblies, track hashes
7. **Spatial Projection**: Validate landmarks after bootstrap

## Summary

Common troubleshooting scenarios:

- **Slow queries**: Rebuild indexes, reduce semantic radius
- **OODA failures**: Cache hypotheses, increase LLM timeout
- **CLR errors**: Deploy dependencies, use UNSAFE permission
- **Neo4j lag**: Increase parallelism, batch operations
- **Service Broker stalls**: Re-enable queues, remove poison messages
- **Spatial projection failures**: Regenerate landmarks, reproject atoms
- **Performance degradation**: Archive old data, partition tables, tune memory/CPU

All issues diagnosed via SQL views, DMVs, and error logs.
