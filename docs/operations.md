# Hartonomous Operations Guide

## Table of Contents

- [Model Ingestion](#model-ingestion)
- [Embedding Management](#embedding-management)
- [System Monitoring](#system-monitoring)
- [Maintenance Tasks](#maintenance-tasks)
- [Troubleshooting](#troubleshooting)
- [Backup and Recovery](#backup-and-recovery)
- [Performance Tuning](#performance-tuning)

## Model Ingestion

### Ingesting Individual Models

#### From Local File

```bash
cd publish/ModelIngestion

# ONNX model
.\ModelIngestion.exe ingest-model "C:\Models\bert-base-uncased.onnx"

# Safetensors model
.\ModelIngestion.exe ingest-model "C:\Models\llama-7b\model.safetensors"

# PyTorch model
.\ModelIngestion.exe ingest-model "C:\Models\gpt2\pytorch_model.bin"

# GGUF model (quantized)
.\ModelIngestion.exe ingest-model "C:\Models\llama-7b-q4.gguf"
```

#### From Hugging Face

```bash
# Download and ingest in one step
.\ModelIngestion.exe download-hf \
  --model "bert-base-uncased" \
  --output "C:\Models\HuggingFace"

# Then ingest
.\ModelIngestion.exe ingest-model "C:\Models\HuggingFace\bert-base-uncased"
```

#### From Ollama

```bash
# Export Ollama model to local path
.\ModelIngestion.exe download-ollama \
  --model "llama2:7b" \
  --output "C:\Models\Ollama"

# Ingest the exported GGUF
.\ModelIngestion.exe ingest-model "C:\Models\Ollama\llama2-7b.gguf"
```

### Batch Ingestion

```bash
# Ingest all models in a directory
.\ModelIngestion.exe ingest-models "C:\Models\Batch" --recursive

# With specific extensions
.\ModelIngestion.exe ingest-models "C:\Models\Batch" \
  --extensions ".onnx,.safetensors" \
  --recursive
```

### Monitoring Ingestion Progress

```bash
# Check recent ingestion jobs
sqlcmd -S localhost -d Hartonomous -E -Q "
SELECT TOP 10
    IngestionJobId,
    JobType,
    Status,
    StartTime,
    EndTime,
    DATEDIFF(SECOND, StartTime, COALESCE(EndTime, GETUTCDATE())) AS DurationSec,
    AtomsProcessed,
    ErrorMessage
FROM IngestionJobs
ORDER BY StartTime DESC;
"
```

### Verifying Ingestion

```sql
-- Check model was ingested
SELECT ModelId, ModelName, ParameterCount, IngestionDate
FROM Models
WHERE ModelName = 'bert-base-uncased';

-- Check layer count
SELECT ModelId, COUNT(*) AS LayerCount
FROM ModelLayers
WHERE ModelId = <model_id>
GROUP BY ModelId;

-- Verify weights are stored
SELECT
    ml.LayerIdx,
    ml.LayerName,
    ml.WeightsGeometry.STNumPoints() AS WeightCount,
    ml.ParameterCount
FROM ModelLayers ml
WHERE ml.ModelId = <model_id>
ORDER BY ml.LayerIdx;
```

## Embedding Management

### Generating Embeddings

```bash
# Generate embeddings for text content
.\ModelIngestion.exe ingest-embeddings \
  --input "C:\Content\articles.txt" \
  --model "sentence-transformers/all-MiniLM-L6-v2" \
  --batch-size 32

# Generate embeddings with specific policy
.\ModelIngestion.exe ingest-embeddings \
  --input "C:\Content\documents" \
  --policy "semantic" \
  --threshold 0.95
```

### Querying Embeddings

```bash
# Hybrid search test
.\ModelIngestion.exe query \
  --text "quantum computing applications" \
  --top 10 \
  --use-spatial true
```

### Embedding Statistics

```sql
-- Embedding count by type
SELECT
    EmbeddingType,
    COUNT(*) AS EmbeddingCount,
    AVG(Dimension) AS AvgDimension,
    SUM(CASE WHEN UsesMaxDimensionPadding = 1 THEN 1 ELSE 0 END) AS PaddedCount
FROM AtomEmbeddings
GROUP BY EmbeddingType;

-- Spatial projection coverage
SELECT
    COUNT(*) AS TotalEmbeddings,
    SUM(CASE WHEN SpatialGeometry IS NOT NULL THEN 1 ELSE 0 END) AS WithSpatialProjection,
    CAST(100.0 * SUM(CASE WHEN SpatialGeometry IS NOT NULL THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,2)) AS CoveragePercent
FROM AtomEmbeddings;
```

### Recomputing Spatial Projections

```sql
-- Recompute all spatial projections (expensive!)
EXEC sp_RecomputeAllSpatialProjections;

-- Recompute for specific embedding type
UPDATE ae
SET SpatialGeometry = NULL,
    SpatialCoarse = NULL
FROM AtomEmbeddings ae
WHERE EmbeddingType = 'semantic';

-- Then trigger recomputation via stored procedure
```

## System Monitoring

### Admin Portal Dashboard

Access at `http://localhost:5000`

**Key Metrics**:

- Total atoms, models, embeddings
- Recent ingestion jobs
- Inference request statistics
- Model usage analytics

### SQL Server Metrics

```sql
-- Database size and growth
SELECT
    name AS DatabaseName,
    size * 8.0 / 1024 AS SizeMB,
    max_size * 8.0 / 1024 AS MaxSizeMB
FROM sys.database_files;

-- Table sizes
SELECT
    t.name AS TableName,
    p.rows AS RowCount,
    SUM(a.total_pages) * 8 / 1024 AS TotalSpaceMB,
    SUM(a.used_pages) * 8 / 1024 AS UsedSpaceMB
FROM sys.tables t
JOIN sys.indexes i ON t.object_id = i.object_id
JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0
GROUP BY t.name, p.rows
ORDER BY SUM(a.total_pages) DESC;

-- Index fragmentation
SELECT
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.index_type_desc,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
    AND ips.page_count > 1000
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

### Neo4j Monitoring

```cypher
// Node counts
MATCH (n)
RETURN labels(n) AS Label, count(*) AS Count
ORDER BY Count DESC;

// Relationship counts
MATCH ()-[r]->()
RETURN type(r) AS RelationshipType, count(*) AS Count
ORDER BY Count DESC;

// Database size
CALL dbms.queryJmx('org.neo4j:instance=kernel#0,name=Store file sizes')
YIELD attributes
RETURN attributes;

// Recent inferences
MATCH (i:Inference)
WHERE i.timestamp > datetime() - duration({days: 1})
RETURN count(i) AS RecentInferences;
```

### Event Hub Metrics

```bash
# Using Azure CLI
az eventhubs eventhub show \
  --resource-group hartonomous-rg \
  --namespace-name hartonomous-events \
  --name hartonomous-changes \
  --query "{MessageCount: messageCount, PartitionCount: partitionCount}"

# Check consumer lag
az eventhubs eventhub consumer-group show \
  --resource-group hartonomous-rg \
  --namespace-name hartonomous-events \
  --eventhub-name hartonomous-changes \
  --name neo4j-sync
```

### Application Logs

#### Windows Event Log

```powershell
# CesConsumer logs
Get-EventLog -LogName Application -Source "HartonomousCesConsumer" -Newest 50

# Neo4jSync logs
Get-EventLog -LogName Application -Source "HartonomousNeo4jSync" -Newest 50
```

#### File-Based Logs

```powershell
# Tail logs
Get-Content "C:\Logs\CesConsumer\log.txt" -Wait -Tail 50

# Search for errors
Select-String -Path "C:\Logs\*.txt" -Pattern "ERROR|FATAL" -Context 2, 5
```

## Maintenance Tasks

### Daily Tasks

#### Check System Health

```sql
-- Run system verification
:r sql\verification\SystemVerification.sql
```

#### Monitor Disk Space

```powershell
# Check SQL Server data directory
Get-PSDrive C | Select-Object Used, Free

# Alert if less than 50GB free
$free = (Get-PSDrive C).Free / 1GB
if ($free -lt 50) {
    Write-Warning "Low disk space: $free GB remaining"
}
```

### Weekly Tasks

#### Update Statistics

```sql
-- Update statistics for large tables
UPDATE STATISTICS Atoms WITH FULLSCAN;
UPDATE STATISTICS AtomEmbeddings WITH FULLSCAN;
UPDATE STATISTICS Models WITH FULLSCAN;
UPDATE STATISTICS ModelLayers WITH FULLSCAN;
UPDATE STATISTICS InferenceRequests WITH FULLSCAN;
```

#### Rebuild Fragmented Indexes

```sql
-- Rebuild indexes with >30% fragmentation
DECLARE @tablename NVARCHAR(128);
DECLARE @indexname NVARCHAR(128);

DECLARE index_cursor CURSOR FOR
SELECT
    OBJECT_NAME(ips.object_id),
    i.name
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
    AND ips.page_count > 1000
    AND i.name IS NOT NULL;

OPEN index_cursor;
FETCH NEXT FROM index_cursor INTO @tablename, @indexname;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Rebuilding index: ' + @indexname + ' on table: ' + @tablename;
    EXEC('ALTER INDEX [' + @indexname + '] ON [' + @tablename + '] REBUILD WITH (ONLINE = ON)');
    FETCH NEXT FROM index_cursor INTO @tablename, @indexname;
END

CLOSE index_cursor;
DEALLOCATE index_cursor;
```

#### Cleanup Old Inference Logs

```sql
-- Delete inference requests older than 90 days
DELETE FROM InferenceRequests
WHERE StartTime < DATEADD(day, -90, GETUTCDATE());

-- Archive instead of deleting (recommended)
INSERT INTO InferenceRequests_Archive
SELECT * FROM InferenceRequests
WHERE StartTime < DATEADD(day, -90, GETUTCDATE());

DELETE FROM InferenceRequests
WHERE StartTime < DATEADD(day, -90, GETUTCDATE());
```

### Monthly Tasks

#### Database Backup

See [Backup and Recovery](#backup-and-recovery) section.

#### Neo4j Maintenance

```cypher
// Delete orphaned nodes (no relationships)
MATCH (n)
WHERE NOT (n)--()
DELETE n;

// Compact store (requires restart)
CALL dbms.checkpoint();
```

#### Review and Archive Logs

```powershell
# Archive logs older than 30 days
$archivePath = "D:\Archives\Logs\$(Get-Date -Format 'yyyy-MM')"
New-Item -ItemType Directory -Path $archivePath -Force

Get-ChildItem "C:\Logs\*.txt" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Move-Item -Destination $archivePath

# Compress archived logs
Compress-Archive -Path $archivePath -DestinationPath "$archivePath.zip"
```

## Troubleshooting

### Common Issues

#### Model Ingestion Failures

**Symptom**: Model ingestion fails with "Unsupported format"

**Solution**:

```bash
# Check file is valid
file model.onnx  # Should show "ONNX model"

# Verify with ModelDiscoveryService
.\ModelIngestion.exe detect-format "C:\Models\problematic-model.bin"

# Check logs for detailed error
Get-Content "C:\Logs\ModelIngestion\log.txt" | Select-String "ERROR"
```

**Symptom**: Layer ingestion incomplete

**Solution**:

```sql
-- Check for partial ingestion
SELECT
    m.ModelId,
    m.ModelName,
    COUNT(ml.LayerId) AS LayersIngested,
    m.Config
FROM Models m
LEFT JOIN ModelLayers ml ON m.ModelId = ml.ModelId
WHERE m.IngestionDate > DATEADD(hour, -1, GETUTCDATE())
GROUP BY m.ModelId, m.ModelName, m.Config
HAVING COUNT(ml.LayerId) = 0;

-- Delete and retry
DELETE FROM Models WHERE ModelId = <failed_model_id>;
```

#### Embedding Generation Slow

**Symptom**: Embedding generation takes hours for small datasets

**Solution**:

```sql
-- Check for missing indexes
SELECT
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    is_disabled
FROM sys.indexes
WHERE is_disabled = 1;

-- Enable disabled indexes
ALTER INDEX idx_atom_embeddings_disksann ON AtomEmbeddings REBUILD;

-- Check spatial index statistics
SELECT
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.indexes i
JOIN sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('AtomEmbeddings'), NULL, NULL, 'DETAILED') ips
    ON i.object_id = ips.object_id AND i.index_id = ips.index_id
WHERE i.type_desc = 'SPATIAL';
```

#### CesConsumer Not Publishing Events

**Symptom**: Change events not appearing in Event Hub

**Solution**:

```powershell
# Check service status
sc.exe query HartonomousCesConsumer

# Check CDC tables exist
sqlcmd -S localhost -d Hartonomous -E -Q "
SELECT name
FROM sys.tables
WHERE name LIKE 'cdc_%'
    OR name IN (SELECT OBJECT_NAME(object_id) FROM sys.change_tracking_tables);
"

# Verify Event Hub connection
# Check CesConsumer logs for connection errors
Get-Content "C:\Logs\CesConsumer\log.txt" | Select-String "EventHub|Connection"

# Test Event Hub connectivity manually
# Using Azure SDK test tool or curl
```

#### Neo4jSync Lagging Behind

**Symptom**: Neo4j data is hours behind SQL Server

**Solution**:

```cypher
// Check latest inference timestamp
MATCH (i:Inference)
RETURN max(i.timestamp) AS LatestInference;

// Compare with SQL Server
```

```sql
SELECT MAX(StartTime) AS LatestInference
FROM InferenceRequests;
```

```bash
# Check consumer lag in Event Hub
az eventhubs eventhub partition show \
  --resource-group hartonomous-rg \
  --namespace-name hartonomous-events \
  --eventhub-name hartonomous-changes \
  --partition-id 0 \
  --query "{LastEnqueuedOffset: lastEnqueuedOffset, LastEnqueuedTime: lastEnqueuedTimeUtc}"

# Restart Neo4jSync service
sc.exe stop HartonomousNeo4jSync
sc.exe start HartonomousNeo4jSync
```

### Performance Diagnostics

#### Slow Queries

```sql
-- Find expensive queries
SELECT TOP 20
    qs.execution_count,
    qs.total_elapsed_time / 1000000.0 AS total_elapsed_time_sec,
    qs.total_elapsed_time / qs.execution_count / 1000.0 AS avg_elapsed_time_ms,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY qs.total_elapsed_time DESC;

-- Analyze query plan
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Your slow query here
EXEC sp_SemanticSearch @query_vector = ..., @top_k = 10;
```

#### Memory Pressure

```sql
-- Check buffer pool usage
SELECT
    (physical_memory_in_use_kb / 1024) AS physical_memory_in_use_mb,
    (locked_page_allocations_kb / 1024) AS locked_page_allocations_mb,
    (virtual_address_space_committed_kb / 1024) AS virtual_address_space_committed_mb
FROM sys.dm_os_process_memory;

-- Check plan cache
SELECT
    objtype AS cache_type,
    COUNT(*) AS number_of_plans,
    SUM(size_in_bytes) / 1024 / 1024 AS size_mb
FROM sys.dm_exec_cached_plans
GROUP BY objtype;

-- Clear plan cache if needed (caution: performance impact)
DBCC FREEPROCCACHE;
```

## Backup and Recovery

### SQL Server Backups

#### Full Backup

```sql
-- Full database backup
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Full.bak'
WITH FORMAT, COMPRESSION, STATS = 10;
```

#### Differential Backup

```sql
-- Differential backup (after full backup)
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Diff.bak'
WITH DIFFERENTIAL, COMPRESSION, STATS = 10;
```

#### Transaction Log Backup

```sql
-- For FULL recovery model
ALTER DATABASE Hartonomous SET RECOVERY FULL;

-- Log backup
BACKUP LOG Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Log.trn'
WITH COMPRESSION, STATS = 10;
```

### Neo4j Backups

```bash
# Online backup (Enterprise only)
neo4j-admin database backup --database=neo4j --to-path=D:\Backups\Neo4j

# Or stop database and copy data directory
sc.exe stop Neo4j
robocopy "C:\Neo4j\data" "D:\Backups\Neo4j\data" /MIR
sc.exe start Neo4j
```

### Restore Procedures

#### Restore SQL Server Database

```sql
-- Restore full backup
RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Full.bak'
WITH REPLACE, RECOVERY, STATS = 10;

-- Restore with differential
RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Full.bak'
WITH NORECOVERY, STATS = 10;

RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Diff.bak'
WITH RECOVERY, STATS = 10;
```

#### Restore Neo4j

```bash
# Restore from backup
sc.exe stop Neo4j
robocopy "D:\Backups\Neo4j\data" "C:\Neo4j\data" /MIR
sc.exe start Neo4j
```

## Performance Tuning

### Query Optimization

#### Enable Query Store

```sql
ALTER DATABASE Hartonomous
SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 60,
    MAX_STORAGE_SIZE_MB = 1000,
    QUERY_CAPTURE_MODE = AUTO
);
```

#### Create Covering Indexes

```sql
-- Cover common query patterns
CREATE NONCLUSTERED INDEX IX_InferenceRequests_TaskType_StartTime
ON InferenceRequests (TaskType, StartTime DESC)
INCLUDE (ModelId, DurationMs, Confidence);

CREATE NONCLUSTERED INDEX IX_Atoms_Modality_IsActive
ON Atoms (Modality, IsActive)
INCLUDE (ContentHash, ReferenceCount)
WHERE IsActive = 1;
```

### SQL Server Configuration

```sql
-- Optimize for vector workloads
EXEC sp_configure 'max server memory (MB)', 49152; -- 48GB
EXEC sp_configure 'max degree of parallelism', 8;
EXEC sp_configure 'cost threshold for parallelism', 50;
RECONFIGURE;

-- Enable instant file initialization
-- (Requires 'Perform Volume Maintenance Tasks' privilege)

-- Enable trace flags
DBCC TRACEON(1117, -1); -- Uniform extent allocations
DBCC TRACEON(1118, -1); -- Mixed extent allocations
```

### Neo4j Configuration

Edit `neo4j.conf`:

```properties
# Increase heap size
dbms.memory.heap.initial_size=16g
dbms.memory.heap.max_size=16g

# Page cache for better read performance
dbms.memory.pagecache.size=8g

# Transaction log settings
db.tx_log.rotation.retention_policy=7 days
db.tx_log.rotation.size=250M

# Query timeout
dbms.transaction.timeout=60s
```

---

## Next Steps

- Review [Deployment Guide](deployment.md) for initial setup
- See [Development Guide](development.md) for local development
- Consult [Troubleshooting](#troubleshooting) for common issues

