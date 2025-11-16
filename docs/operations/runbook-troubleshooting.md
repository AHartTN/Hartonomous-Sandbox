# Troubleshooting Runbook

## Common Issues and Solutions

### 1. Slow Query Performance

**Symptom**: Queries taking longer than expected (>100ms for spatial queries)

**Diagnosis**:
```sql
-- Check query execution plan
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

EXEC sp_SpatialNextToken @context_atom_ids = '1,2', @top_k = 10;

-- Check if spatial index is being used
SELECT 
    i.name,
    ius.user_seeks,
    ius.user_scans,
    ius.last_user_seek
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius 
    ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE i.type_desc = 'SPATIAL'
    AND OBJECT_NAME(i.object_id) = 'AtomEmbeddings';
```

**Solutions**:

A. Rebuild spatial indexes:
```sql
ALTER INDEX IX_AtomEmbeddings_SpatialGeometry 
ON dbo.AtomEmbeddings REBUILD;

ALTER INDEX IX_AtomEmbeddings_SpatialCoarse 
ON dbo.AtomEmbeddings REBUILD;
```

B. Update statistics:
```sql
UPDATE STATISTICS dbo.AtomEmbeddings;
```

C. Force index hint in query:
```sql
-- Modify sp_SpatialNextToken to include index hint
SELECT TOP (@candidatePool)
    ae.AtomId
FROM dbo.AtomEmbeddings ae WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE ae.SpatialGeometry.STIntersects(@queryGeometry.STBuffer(10.0)) = 1
ORDER BY ae.SpatialGeometry.STDistance(@queryGeometry);
```

### 2. OODA Loop Not Running

**Symptom**: No activity in OODA loop procedures, queues appear stuck

**Diagnosis**:
```sql
-- Check Service Broker status
SELECT is_broker_enabled FROM sys.databases WHERE name = DB_NAME();

-- Check queue status
SELECT name, is_activation_enabled, is_receive_enabled
FROM sys.service_queues
WHERE name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');

-- Check for messages
SELECT COUNT(*) FROM AnalyzeQueue;
SELECT COUNT(*) FROM HypothesizeQueue;
```

**Solutions**:

A. Enable Service Broker:
```sql
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
```

B. Activate queues:
```sql
ALTER QUEUE AnalyzeQueue WITH STATUS = ON, ACTIVATION (STATUS = ON);
ALTER QUEUE HypothesizeQueue WITH STATUS = ON, ACTIVATION (STATUS = ON);
ALTER QUEUE ActQueue WITH STATUS = ON, ACTIVATION (STATUS = ON);
ALTER QUEUE LearnQueue WITH STATUS = ON, ACTIVATION (STATUS = ON);
```

C. Manually trigger OODA loop:
```sql
EXEC sp_Analyze @TenantId = 0;
```

D. Check for errors in transmission queue:
```sql
SELECT * FROM sys.transmission_queue;
```

### 3. CLR Assembly Errors

**Symptom**: CLR functions return NULL or throw "Assembly not found" errors

**Diagnosis**:
```sql
-- Check assembly status
SELECT name, permission_set_desc, is_visible, clr_name
FROM sys.assemblies
WHERE name LIKE 'Hartonomous%';

-- Check CLR configuration
EXEC sp_configure 'clr enabled';
EXEC sp_configure 'clr strict security';
```

**Solutions**:

A. Enable CLR:
```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

B. Configure CLR strict security (SQL Server 2017+):
```sql
-- Create asymmetric key from DLL
CREATE ASYMMETRIC KEY Hartonomous_CLR_Key
FROM EXECUTABLE FILE = 'D:\Path\To\Hartonomous.Database.dll';

CREATE LOGIN Hartonomous_CLR_Login FROM ASYMMETRIC KEY Hartonomous_CLR_Key;
GRANT UNSAFE ASSEMBLY TO Hartonomous_CLR_Login;
```

C. Drop and recreate assembly:
```sql
DROP ASSEMBLY [Hartonomous.Clr];
-- Redeploy DACPAC
```

### 4. Spatial Index Not Being Used

**Symptom**: Queries use table scan instead of spatial index

**Diagnosis**:
```sql
-- Check execution plan
SET SHOWPLAN_XML ON;
EXEC sp_SpatialNextToken @context_atom_ids = '1,2', @top_k = 10;
SET SHOWPLAN_XML OFF;

-- Check index fragmentation
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    ps.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.AtomEmbeddings'), NULL, NULL, 'DETAILED') ps
INNER JOIN sys.indexes i ON ps.object_id = i.object_id AND ps.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL';
```

**Solutions**:

A. Verify BOUNDING_BOX encompasses data:
```sql
-- Check data bounds
SELECT 
    MIN(SpatialGeometry.STX) AS MinX,
    MAX(SpatialGeometry.STX) AS MaxX,
    MIN(SpatialGeometry.STY) AS MinY,
    MAX(SpatialGeometry.STY) AS MaxY,
    MIN(SpatialGeometry.STZ) AS MinZ,
    MAX(SpatialGeometry.STZ) AS MaxZ
FROM dbo.AtomEmbeddings
WHERE SpatialGeometry IS NOT NULL;

-- If data exceeds BOUNDING_BOX, rebuild index with larger box
DROP INDEX IX_AtomEmbeddings_SpatialGeometry ON dbo.AtomEmbeddings;

CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
ON dbo.AtomEmbeddings (SpatialGeometry)
WITH (BOUNDING_BOX = (-2000, -2000, 2000, 2000));
```

B. Use index hint:
```sql
-- Force spatial index usage
SELECT * FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE SpatialGeometry.STDistance(@point) < 10;
```

### 5. Memory Pressure

**Symptom**: SQL Server running out of memory, queries slowing down

**Diagnosis**:
```sql
-- Check memory usage
SELECT 
    physical_memory_in_use_kb / 1024 AS physical_memory_MB,
    locked_page_allocations_kb / 1024 AS locked_pages_MB,
    total_virtual_address_space_kb / 1024 AS virtual_memory_MB
FROM sys.dm_os_process_memory;

-- Check buffer pool usage
SELECT 
    COUNT(*) * 8 / 1024 AS buffer_pool_MB
FROM sys.dm_os_buffer_descriptors;
```

**Solutions**:

A. Configure max server memory:
```sql
-- Leave 20% for OS
EXEC sp_configure 'max server memory (MB)', 81920;  -- 80GB example
RECONFIGURE;
```

B. Clear procedure cache if bloated:
```sql
DBCC FREEPROCCACHE;
```

C. Clear buffer pool (last resort, causes performance hit):
```sql
CHECKPOINT;
DBCC DROPCLEANBUFFERS;
```

### 6. Neo4j Sync Failures

**Symptom**: Provenance data not appearing in Neo4j

**Diagnosis**:
```sql
-- Check sync queue
SELECT COUNT(*) FROM dbo.Neo4jSyncQueue WHERE IsSynced = 0;

-- Check for errors
SELECT TOP 10 * FROM dbo.Neo4jSyncQueue 
WHERE ErrorMessage IS NOT NULL
ORDER BY CreatedAt DESC;
```

**Solutions**:

A. Verify Neo4j connection:
```powershell
# Test Neo4j connectivity
Invoke-RestMethod -Uri "http://neo4j-server:7474/db/data/"
```

B. Restart sync worker:
```powershell
Restart-Service Hartonomous.Workers.Neo4jSync
```

C. Manual resync:
```sql
-- Reset failed items
UPDATE dbo.Neo4jSyncQueue 
SET IsSynced = 0, ErrorMessage = NULL
WHERE ErrorMessage IS NOT NULL;
```

## Performance Tuning

### Optimize Spatial Queries

```sql
-- Create covering index for common queries
CREATE NONCLUSTERED INDEX IX_AtomEmbeddings_Covering
ON dbo.AtomEmbeddings (AtomId)
INCLUDE (SpatialGeometry, HilbertValue, EmbeddingVector);
```

### OODA Loop Tuning

```sql
-- Adjust OODA loop frequency
-- Modify sp_Analyze to run every N minutes instead of continuously
```

### Query Store Configuration

```sql
-- Enable Query Store for automatic tuning
ALTER DATABASE Hartonomous SET QUERY_STORE = ON;
ALTER DATABASE Hartonomous SET QUERY_STORE (
    OPERATION_MODE = READ_WRITE,
    MAX_STORAGE_SIZE_MB = 10240,
    INTERVAL_LENGTH_MINUTES = 5,
    QUERY_CAPTURE_MODE = AUTO
);

-- Enable automatic plan correction
ALTER DATABASE Hartonomous SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
```

## Health Checks

### Daily Checks

```sql
-- 1. Check OODA loop activity
SELECT Phase, COUNT(*) AS executions_today
FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= CAST(GETDATE() AS DATE)
GROUP BY Phase;

-- 2. Check query performance
SELECT * FROM dbo.vw_QueryPerformanceMetrics
WHERE last_execution_time >= DATEADD(HOUR, -24, GETUTCDATE())
    AND avg_duration_ms > 100
ORDER BY avg_duration_ms DESC;

-- 3. Check index usage
SELECT 
    i.name,
    ius.user_seeks,
    ius.user_scans,
    ius.last_user_seek
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius
    ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE OBJECT_NAME(i.object_id) = 'AtomEmbeddings';
```

### Weekly Checks

```sql
-- 1. Index maintenance
EXEC sp_updatestats;
ALTER INDEX ALL ON dbo.AtomEmbeddings REBUILD;

-- 2. Check database size
EXEC sp_spaceused;

-- 3. Backup verification
SELECT TOP 5 database_name, backup_finish_date, type
FROM msdb.dbo.backupset
WHERE database_name = 'Hartonomous'
ORDER BY backup_finish_date DESC;
```

## Contact Information

**For Production Issues**:
- On-call: [Contact Info]
- Escalation: [Contact Info]
- Slack: #hartonomous-ops

**Known Issues**:
- [Link to issue tracker]

**Monitoring Dashboards**:
- Grafana: [URL]
- Application Insights: [URL]
