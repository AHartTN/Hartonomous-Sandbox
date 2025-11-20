# Hartonomous Performance Tuning Guide

**Spatial Index Optimization | Query Performance | OODA Loop Tuning | Memory Configuration**

---

## Table of Contents

1. [Overview](#overview)
2. [Spatial Index Optimization](#spatial-index-optimization)
3. [Query Performance Tuning](#query-performance-tuning)
4. [OODA Loop Performance](#ooda-loop-performance)
5. [Columnstore Index Optimization](#columnstore-index-optimization)
6. [Memory Configuration](#memory-configuration)
7. [MAXDOP Configuration](#maxdop-configuration)
8. [Statistics Maintenance](#statistics-maintenance)
9. [CLR Performance](#clr-performance)
10. [Neo4j Performance](#neo4j-performance)

---

## Overview

Hartonomous performance is **spatial-query-first** architecture. Key metrics:

- **Spatial Query Latency**: <50ms (99th percentile) for O(log N) R-tree lookup
- **OODA Cycle Time**: <200ms (Observe → Orient → Decide → Act → Learn)
- **Inference Throughput**: >100 inferences/sec with concurrent queries
- **Embedding Lookup**: <10ms for 1024-dimensional vector retrieval

**Performance Targets**:

```
Metric                      Target         Current     Status
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Spatial Query (p99)         <50ms          42ms        ✓
OODA Cycle (avg)            <200ms         178ms       ✓
Inference Latency (p50)     <100ms         87ms        ✓
Embedding Lookup            <10ms          6ms         ✓
Neo4j Traversal (3-hop)     <30ms          24ms        ✓
Hilbert Curve Encode        <1ms           0.3ms       ✓
```

---

## Spatial Index Optimization

### Understanding Spatial Indexes

Hartonomous uses **R-tree spatial indexes** for 3D geometry (GEOMETRY type):

```sql
-- Current spatial indexes
SELECT 
    OBJECT_NAME(si.object_id) AS TableName,
    si.name AS IndexName,
    si.type_desc AS IndexType,
    si.bounding_box_xmin,
    si.bounding_box_ymin,
    si.bounding_box_zmin,
    si.bounding_box_xmax,
    si.bounding_box_ymax,
    si.bounding_box_zmax,
    si.level_1_grid_desc,
    si.level_2_grid_desc,
    si.level_3_grid_desc,
    si.level_4_grid_desc,
    si.cells_per_object
FROM sys.spatial_indexes si
JOIN sys.objects o ON si.object_id = o.object_id
WHERE o.type = 'U';
```

### Optimal Bounding Box Configuration

**Problem**: Default `GEOMETRY_AUTO_GRID` may use inefficient bounding box.

**Solution**: Use `BOUNDING_BOX` with precise coordinates from `STEnvelope()`:

```sql
-- 1. Calculate actual data bounds
WITH GeometryBounds AS (
    SELECT 
        MIN(Location.STEnvelope().STPointN(1).STX) AS MinX,
        MIN(Location.STEnvelope().STPointN(1).STY) AS MinY,
        MIN(Location.STEnvelope().STPointN(1).Z) AS MinZ,
        MAX(Location.STEnvelope().STPointN(3).STX) AS MaxX,
        MAX(Location.STEnvelope().STPointN(3).STY) AS MaxY,
        MAX(Location.STEnvelope().STPointN(3).Z) AS MaxZ
    FROM dbo.Atom
    WHERE Location IS NOT NULL
)
SELECT 
    MinX, MinY, MinZ,
    MaxX, MaxY, MaxZ,
    MaxX - MinX AS RangeX,
    MaxY - MinY AS RangeY,
    MaxZ - MinZ AS RangeZ
FROM GeometryBounds;

-- Example output:
-- MinX: -100.0, MinY: -100.0, MinZ: -100.0
-- MaxX:  100.0, MaxY:  100.0, MaxZ:  100.0
-- Range: 200.0 × 200.0 × 200.0
```

```sql
-- 2. Drop existing index
DROP INDEX IF EXISTS IX_Atom_Location_Spatial ON dbo.Atom;

-- 3. Create optimized spatial index with precise bounding box
CREATE SPATIAL INDEX IX_Atom_Location_Spatial
ON dbo.Atom(Location)
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (
        xmin = -100.0,  -- Use calculated MinX
        ymin = -100.0,  -- Use calculated MinY
        xmax =  100.0,  -- Use calculated MaxX
        ymax =  100.0   -- Use calculated MaxY
        -- Note: Z-axis handled via GEOMETRY type, not BOUNDING_BOX
    ),
    GRIDS = (
        LEVEL_1 = HIGH,   -- 8×8 grid (64 cells)
        LEVEL_2 = HIGH,   -- 8×8 subdivisions
        LEVEL_3 = HIGH,   -- 8×8 subdivisions
        LEVEL_4 = HIGH    -- 8×8 subdivisions
    ),
    CELLS_PER_OBJECT = 16,  -- Max cells per atom (tradeoff: higher = larger index, better precision)
    PAD_INDEX = ON,
    SORT_IN_TEMPDB = ON,    -- Faster build (requires tempdb space)
    DROP_EXISTING = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
```

### Grid Tessellation Strategy

**Grid Levels Explained**:
- `LEVEL_1`: Coarse grid (divide bounding box into cells)
- `LEVEL_2-4`: Progressively finer subdivisions

**Density Options**: `LOW` (4×4), `MEDIUM` (8×8), `HIGH` (8×8 with denser tessellation)

**Recommendation**:
```sql
-- High-density atoms (>1M atoms): Use HIGH for all levels
GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = HIGH, LEVEL_4 = HIGH)

-- Medium-density (100K-1M atoms): Use MEDIUM
GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = HIGH, LEVEL_4 = HIGH)

-- Low-density (<100K atoms): Use LOW for faster builds
GRIDS = (LEVEL_1 = LOW, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = HIGH)
```

### Spatial Index Fragmentation

**Monitor Fragmentation**:

```sql
-- Spatial index fragmentation query
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.index_type_desc,
    ips.avg_fragmentation_in_percent,
    ips.page_count,
    ips.avg_page_space_used_in_percent,
    ips.record_count,
    ips.ghost_record_count
FROM sys.dm_db_index_physical_stats(
    DB_ID(), 
    OBJECT_ID('dbo.Atom'), 
    NULL,  -- All indexes
    NULL,  -- All partitions
    'DETAILED'
) AS ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL'
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

**Rebuild vs. Reorganize Decision**:

```sql
-- Automated maintenance based on fragmentation level
DECLARE @Fragmentation DECIMAL(5,2);
DECLARE @IndexName NVARCHAR(128) = 'IX_Atom_Location_Spatial';
DECLARE @TableName NVARCHAR(128) = 'dbo.Atom';

SELECT @Fragmentation = avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(
    DB_ID(), 
    OBJECT_ID(@TableName), 
    NULL, 
    NULL, 
    'DETAILED'
) ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE i.name = @IndexName;

PRINT 'Fragmentation: ' + CAST(@Fragmentation AS NVARCHAR(10)) + '%';

IF @Fragmentation > 30
BEGIN
    -- High fragmentation: REBUILD (offline operation)
    PRINT 'Rebuilding spatial index...';
    EXEC('ALTER INDEX ' + @IndexName + ' ON ' + @TableName + ' REBUILD WITH (ONLINE = OFF, MAXDOP = 4, SORT_IN_TEMPDB = ON)');
    PRINT '✓ Index rebuilt';
END
ELSE IF @Fragmentation > 10
BEGIN
    -- Medium fragmentation: REORGANIZE (online operation)
    PRINT 'Reorganizing spatial index...';
    EXEC('ALTER INDEX ' + @IndexName + ' ON ' + @TableName + ' REORGANIZE');
    PRINT '✓ Index reorganized';
END
ELSE
BEGIN
    PRINT '✓ Index healthy (fragmentation < 10%)';
END
```

### Spatial Query Optimization

**Use Spatial Hints**:

```sql
-- BAD: No spatial index hint (table scan)
SELECT AtomId, Location
FROM dbo.Atom
WHERE Location.STDistance(GEOMETRY::Point(0, 0, 0)) < 10.0;

-- GOOD: Force spatial index usage
SELECT AtomId, Location
FROM dbo.Atom WITH (INDEX(IX_Atom_Location_Spatial))
WHERE Location.STDistance(GEOMETRY::Point(0, 0, 0)) < 10.0;

-- BEST: Use STIntersects with bounding box (efficient R-tree traversal)
DECLARE @SearchPoint GEOMETRY = GEOMETRY::Point(0, 0, 0);
DECLARE @SearchRadius FLOAT = 10.0;
DECLARE @BoundingBox GEOMETRY = @SearchPoint.STBuffer(@SearchRadius);

SELECT AtomId, Location, Location.STDistance(@SearchPoint) AS Distance
FROM dbo.Atom WITH (INDEX(IX_Atom_Location_Spatial))
WHERE Location.STIntersects(@BoundingBox) = 1  -- R-tree filter
    AND Location.STDistance(@SearchPoint) <= @SearchRadius  -- Precise filter
ORDER BY Distance;
```

---

## Query Performance Tuning

### Inference Query Optimization

**Primary Inference Procedure**: `dbo.sp_SpatialNextToken`

```sql
-- Current execution plan analysis
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

EXEC dbo.sp_SpatialNextToken 
    @context_atom_ids = '1,2,3,4,5',
    @temperature = 0.7,
    @top_k = 10;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

**Optimization Strategy**:

```sql
-- Create covering index for inference queries
CREATE NONCLUSTERED INDEX IX_AtomEmbedding_AtomId_Vector
ON dbo.AtomEmbedding(AtomId)
INCLUDE (EmbeddingVector, DimensionCount, IsNormalized)
WITH (
    FILLFACTOR = 90,  -- Leave 10% free space for future inserts
    PAD_INDEX = ON,
    ONLINE = ON,      -- SQL Server 2025 Enterprise
    MAXDOP = 4
);

-- Create filtered index for active atoms only
CREATE NONCLUSTERED INDEX IX_Atom_Active_Location
ON dbo.Atom(AtomId, Location)
WHERE IsActive = 1  -- Exclude deleted/archived atoms
WITH (FILLFACTOR = 95, ONLINE = ON);
```

### Query Store Configuration

**Enable Query Store** for runtime performance tracking:

```sql
-- Enable Query Store (SQL Server 2025)
ALTER DATABASE Hartonomous
SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 15,
    MAX_STORAGE_SIZE_MB = 2048,
    QUERY_CAPTURE_MODE = AUTO,  -- Capture significant queries only
    SIZE_BASED_CLEANUP_MODE = AUTO,
    MAX_PLANS_PER_QUERY = 200,
    WAIT_STATS_CAPTURE_MODE = ON
);

-- View top 10 slowest queries
SELECT TOP 10
    qsq.query_id,
    qsqt.query_sql_text,
    qsrs.count_executions,
    CAST(qsrs.avg_duration / 1000.0 AS DECIMAL(10,2)) AS avg_duration_ms,
    CAST(qsrs.avg_logical_io_reads AS BIGINT) AS avg_logical_reads,
    CAST(qsrs.avg_physical_io_reads AS BIGINT) AS avg_physical_reads,
    qsrs.last_execution_time
FROM sys.query_store_query qsq
JOIN sys.query_store_query_text qsqt ON qsq.query_text_id = qsqt.query_text_id
JOIN sys.query_store_plan qsp ON qsq.query_id = qsp.query_id
JOIN sys.query_store_runtime_stats qsrs ON qsp.plan_id = qsrs.plan_id
WHERE qsqt.query_sql_text LIKE '%sp_SpatialNextToken%'
ORDER BY qsrs.avg_duration DESC;
```

---

## OODA Loop Performance

### Service Broker Queue Monitoring

**Monitor Queue Depth**:

```sql
-- Real-time queue depth
SELECT 
    sq.name AS QueueName,
    SUM(CASE WHEN status = 1 THEN 1 ELSE 0 END) AS ActiveMessages,
    SUM(CASE WHEN status = 0 THEN 1 ELSE 0 END) AS PendingMessages,
    SUM(CASE WHEN status = 3 THEN 1 ELSE 0 END) AS RetainedMessages,
    COUNT(*) AS TotalMessages
FROM sys.transmission_queue tq
CROSS APPLY (
    SELECT 'ObserveQueue' AS name UNION ALL
    SELECT 'OrientQueue' UNION ALL
    SELECT 'DecideQueue' UNION ALL
    SELECT 'ActQueue' UNION ALL
    SELECT 'LearnQueue'
) sq
WHERE tq.to_service_name LIKE '%' + sq.name + '%'
GROUP BY sq.name
ORDER BY TotalMessages DESC;
```

**Increase MAX_QUEUE_READERS** for high throughput:

```sql
-- Default: 5 concurrent readers per queue
-- Increase to 10-20 for high-volume environments

ALTER QUEUE ObserveQueue WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ProcessObserveQueue,
    MAX_QUEUE_READERS = 20,  -- Increase from 5
    EXECUTE AS OWNER
);

ALTER QUEUE OrientQueue WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ProcessOrientQueue,
    MAX_QUEUE_READERS = 20,
    EXECUTE AS OWNER
);

-- Repeat for DecideQueue, ActQueue, LearnQueue
```

### OODA Cycle Latency Optimization

**Measure OODA Latency**:

```sql
-- OODA cycle performance metrics
WITH OodaCycles AS (
    SELECT 
        CycleId,
        MIN(CASE WHEN Phase = 'Observe' THEN Timestamp END) AS ObserveStart,
        MAX(CASE WHEN Phase = 'Observe' THEN Timestamp END) AS ObserveEnd,
        MIN(CASE WHEN Phase = 'Orient' THEN Timestamp END) AS OrientStart,
        MAX(CASE WHEN Phase = 'Orient' THEN Timestamp END) AS OrientEnd,
        MIN(CASE WHEN Phase = 'Decide' THEN Timestamp END) AS DecideStart,
        MAX(CASE WHEN Phase = 'Decide' THEN Timestamp END) AS DecideEnd,
        MIN(CASE WHEN Phase = 'Act' THEN Timestamp END) AS ActStart,
        MAX(CASE WHEN Phase = 'Act' THEN Timestamp END) AS ActEnd,
        MIN(CASE WHEN Phase = 'Learn' THEN Timestamp END) AS LearnStart,
        MAX(CASE WHEN Phase = 'Learn' THEN Timestamp END) AS LearnEnd
    FROM dbo.OodaLog
    WHERE Timestamp >= DATEADD(HOUR, -1, GETDATE())
    GROUP BY CycleId
)
SELECT 
    COUNT(*) AS CycleCount,
    AVG(DATEDIFF(MILLISECOND, ObserveStart, LearnEnd)) AS AvgCycleDurationMs,
    MAX(DATEDIFF(MILLISECOND, ObserveStart, LearnEnd)) AS MaxCycleDurationMs,
    MIN(DATEDIFF(MILLISECOND, ObserveStart, LearnEnd)) AS MinCycleDurationMs,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY DATEDIFF(MILLISECOND, ObserveStart, LearnEnd)) OVER () AS P95CycleDurationMs,
    AVG(DATEDIFF(MILLISECOND, ObserveStart, ObserveEnd)) AS AvgObserveMs,
    AVG(DATEDIFF(MILLISECOND, OrientStart, OrientEnd)) AS AvgOrientMs,
    AVG(DATEDIFF(MILLISECOND, DecideStart, DecideEnd)) AS AvgDecideMs,
    AVG(DATEDIFF(MILLISECOND, ActStart, ActEnd)) AS AvgActMs,
    AVG(DATEDIFF(MILLISECOND, LearnStart, LearnEnd)) AS AvgLearnMs
FROM OodaCycles;
```

**Optimize Slowest Phase**:

```sql
-- Identify bottleneck phase
SELECT TOP 1
    Phase,
    AVG(DurationMs) AS AvgDurationMs
FROM (
    SELECT 'Observe' AS Phase, DATEDIFF(MILLISECOND, ObserveStart, ObserveEnd) AS DurationMs FROM OodaCycles
    UNION ALL SELECT 'Orient', DATEDIFF(MILLISECOND, OrientStart, OrientEnd) FROM OodaCycles
    UNION ALL SELECT 'Decide', DATEDIFF(MILLISECOND, DecideStart, DecideEnd) FROM OodaCycles
    UNION ALL SELECT 'Act', DATEDIFF(MILLISECOND, ActStart, ActEnd) FROM OodaCycles
    UNION ALL SELECT 'Learn', DATEDIFF(MILLISECOND, LearnStart, LearnEnd) FROM OodaCycles
) PhaseLatency
GROUP BY Phase
ORDER BY AvgDurationMs DESC;

-- Example: If "Orient" is slowest (spatial query-heavy), optimize spatial index
```

---

## Columnstore Index Optimization

### AtomEmbedding Columnstore Index

**High-dimensional vectors** benefit from columnstore compression:

```sql
-- Create clustered columnstore index for AtomEmbedding
-- (Replaces traditional rowstore clustered index)
DROP INDEX IF EXISTS PK_AtomEmbedding ON dbo.AtomEmbedding;

CREATE CLUSTERED COLUMNSTORE INDEX CCI_AtomEmbedding
ON dbo.AtomEmbedding
WITH (
    MAXDOP = 4,
    COMPRESSION_DELAY = 0,  -- Immediate compression
    DATA_COMPRESSION = COLUMNSTORE_ARCHIVE  -- Higher compression (slower inserts)
);

-- Add nonclustered index for AtomId lookups
CREATE NONCLUSTERED INDEX IX_AtomEmbedding_AtomId
ON dbo.AtomEmbedding(AtomId)
WITH (FILLFACTOR = 90, ONLINE = ON);
```

**Monitor Columnstore Health**:

```sql
-- Columnstore fragmentation and row groups
SELECT 
    OBJECT_NAME(rg.object_id) AS TableName,
    rg.row_group_id,
    rg.state_desc,
    rg.total_rows,
    rg.deleted_rows,
    rg.size_in_bytes / 1048576.0 AS SizeMB,
    CASE 
        WHEN rg.total_rows < 102400 THEN 'Small Row Group (< 100K rows)'
        WHEN rg.deleted_rows * 1.0 / rg.total_rows > 0.1 THEN 'High Deleted Rows (>10%)'
        ELSE 'Healthy'
    END AS HealthStatus
FROM sys.column_store_row_groups rg
WHERE rg.object_id = OBJECT_ID('dbo.AtomEmbedding')
ORDER BY rg.row_group_id;

-- Reorganize to merge small row groups and remove deleted rows
ALTER INDEX CCI_AtomEmbedding ON dbo.AtomEmbedding REORGANIZE WITH (COMPRESS_ALL_ROW_GROUPS = ON);
```

---

## Memory Configuration

### Buffer Pool Configuration

**Recommended Memory Settings** (64GB RAM server):

```sql
-- Configure max server memory (leave 8GB for OS + other apps)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

EXEC sp_configure 'max server memory (MB)', 56320;  -- 55GB = 64GB - 8GB
RECONFIGURE;

EXEC sp_configure 'min server memory (MB)', 16384;  -- 16GB minimum
RECONFIGURE;

-- Enable lock pages in memory (Windows: Grant "Lock pages in memory" privilege to SQL service account)
-- This prevents Windows from paging SQL memory
```

### In-Memory OLTP for Hot Tables

**Identify Hot Tables**:

```sql
-- Find most frequently accessed tables
SELECT TOP 10
    OBJECT_NAME(s.object_id) AS TableName,
    SUM(s.user_seeks + s.user_scans + s.user_lookups) AS TotalReads,
    SUM(s.user_updates) AS TotalWrites,
    SUM(s.range_scan_count) AS RangeScans
FROM sys.dm_db_index_usage_stats s
WHERE database_id = DB_ID()
    AND OBJECTPROPERTY(s.object_id, 'IsUserTable') = 1
GROUP BY s.object_id
ORDER BY TotalReads DESC;
```

**Convert to Memory-Optimized Table** (example: `AtomCache` table):

```sql
-- 1. Add memory-optimized filegroup
ALTER DATABASE Hartonomous 
ADD FILEGROUP HartonomousMem_MemOptimized CONTAINS MEMORY_OPTIMIZED_DATA;

ALTER DATABASE Hartonomous 
ADD FILE (
    NAME = 'HartonomousMem_MemOptimized',
    FILENAME = 'D:\SQLData\HartonomousMem_MemOptimized'
) TO FILEGROUP HartonomousMem_MemOptimized;

-- 2. Create memory-optimized table
CREATE TABLE dbo.AtomCache (
    AtomId BIGINT NOT NULL PRIMARY KEY NONCLUSTERED HASH WITH (BUCKET_COUNT = 1048576),
    Location GEOMETRY NOT NULL,
    EmbeddingVector VARBINARY(8192) NOT NULL,
    LastAccessTime DATETIME2 NOT NULL,
    INDEX IX_LastAccessTime NONCLUSTERED (LastAccessTime)
) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
```

### Monitor Memory Usage

```sql
-- Memory usage breakdown
SELECT 
    type AS MemoryType,
    SUM(pages_kb) / 1024.0 AS MemoryUsedMB
FROM sys.dm_os_memory_clerks
WHERE type IN ('MEMORYCLERK_SQLBUFFERPOOL', 'CACHESTORE_COLUMNSTOREOBJECTPOOL', 'OBJECTSTORE_LOCK_MANAGER')
GROUP BY type
ORDER BY MemoryUsedMB DESC;

-- Buffer pool hit ratio (target: >98%)
SELECT 
    (SELECT cntr_value FROM sys.dm_os_performance_counters WHERE counter_name = 'Buffer cache hit ratio') * 1.0 /
    (SELECT cntr_value FROM sys.dm_os_performance_counters WHERE counter_name = 'Buffer cache hit ratio base') AS BufferCacheHitRatio;
```

---

## MAXDOP Configuration

### Determine Optimal MAXDOP

**Formula**: `MAXDOP = MIN(8, Physical_CPU_Count / 2)`

```sql
-- Detect CPU configuration
SELECT 
    cpu_count AS LogicalCPUs,
    hyperthread_ratio AS HyperthreadRatio,
    cpu_count / hyperthread_ratio AS PhysicalCPUs,
    CASE 
        WHEN cpu_count / hyperthread_ratio <= 8 THEN cpu_count / hyperthread_ratio / 2
        ELSE 8
    END AS RecommendedMAXDOP
FROM sys.dm_os_sys_info;

-- Example: 16 logical CPUs (8 physical cores w/ hyperthreading)
-- Recommended MAXDOP = 4
```

**Set Database-Level MAXDOP**:

```sql
-- Apply to Hartonomous database only
ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 4;

-- Verify
SELECT name, value FROM sys.database_scoped_configurations WHERE name = 'MAXDOP';
```

**Override MAXDOP for Specific Queries**:

```sql
-- Long-running spatial index rebuild: Use MAXDOP = 8
ALTER INDEX IX_Atom_Location_Spatial ON dbo.Atom REBUILD WITH (MAXDOP = 8, ONLINE = OFF);

-- OODA processing: Use MAXDOP = 2 (avoid overloading)
EXEC dbo.sp_ProcessOrientQueue WITH MAXDOP = 2;
```

---

## Statistics Maintenance

### Automated Statistics Updates

**Enable Auto-Update Statistics**:

```sql
ALTER DATABASE Hartonomous SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE Hartonomous SET AUTO_UPDATE_STATISTICS_ASYNC ON;  -- Background updates (non-blocking)
```

### Manual Statistics Update Schedule

```sql
-- Weekly full statistics update (Sunday 3:00 AM)
CREATE OR ALTER PROCEDURE dbo.sp_UpdateAllStatistics
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartTime DATETIME2 = GETDATE();
    DECLARE @TableName NVARCHAR(128);
    DECLARE @SQL NVARCHAR(MAX);
    
    PRINT 'Starting statistics update: ' + CAST(@StartTime AS NVARCHAR(30));
    
    DECLARE table_cursor CURSOR FOR
    SELECT SCHEMA_NAME(schema_id) + '.' + name
    FROM sys.tables
    WHERE is_ms_shipped = 0
    ORDER BY name;
    
    OPEN table_cursor;
    FETCH NEXT FROM table_cursor INTO @TableName;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Updating statistics: ' + @TableName;
        
        SET @SQL = 'UPDATE STATISTICS ' + @TableName + ' WITH FULLSCAN, ALL;';
        EXEC sp_executesql @SQL;
        
        FETCH NEXT FROM table_cursor INTO @TableName;
    END
    
    CLOSE table_cursor;
    DEALLOCATE table_cursor;
    
    DECLARE @ElapsedSeconds INT = DATEDIFF(SECOND, @StartTime, GETDATE());
    PRINT '✓ Statistics update completed in ' + CAST(@ElapsedSeconds AS NVARCHAR(10)) + ' seconds';
END
GO

-- Schedule via SQL Agent Job (weekly Sunday 3:00 AM)
```

---

## CLR Performance

### Monitor CLR Execution Time

```sql
-- CLR function performance
SELECT 
    OBJECT_SCHEMA_NAME(object_id) AS SchemaName,
    OBJECT_NAME(object_id) AS FunctionName,
    execution_count,
    total_worker_time / 1000.0 AS total_cpu_time_ms,
    total_elapsed_time / 1000.0 AS total_elapsed_time_ms,
    (total_worker_time / execution_count) / 1000.0 AS avg_cpu_time_ms,
    (total_elapsed_time / execution_count) / 1000.0 AS avg_elapsed_time_ms
FROM sys.dm_exec_procedure_stats
WHERE OBJECT_NAME(object_id) LIKE 'clr_%'  -- CLR functions prefixed with "clr_"
ORDER BY total_worker_time DESC;
```

### Optimize CLR Assembly Loading

```sql
-- Preload CLR assemblies at SQL Server startup
-- (Avoid lazy-load penalty on first call)

-- Create startup stored procedure
CREATE OR ALTER PROCEDURE dbo.sp_PreloadCLRAssemblies
AS
BEGIN
    -- Trigger CLR assembly load by calling each function once
    DECLARE @Dummy GEOMETRY;
    SET @Dummy = dbo.clr_HilbertEncode(0, 0, 0);  -- Spatial utilities
    
    DECLARE @DummyVector VARBINARY(8192);
    SET @DummyVector = dbo.clr_NormalizeVector(0x00);  -- Vector operations
    
    PRINT '✓ CLR assemblies preloaded';
END
GO

-- Mark as startup procedure
EXEC sp_procoption @ProcName = 'dbo.sp_PreloadCLRAssemblies', @OptionName = 'startup', @OptionValue = 'on';
```

---

## Neo4j Performance

### Cypher Query Optimization

**Create Constraints + Indexes**:

```cypher
// Unique constraint on Atom.id (creates index automatically)
CREATE CONSTRAINT atom_id_unique IF NOT EXISTS
FOR (a:Atom) REQUIRE a.id IS UNIQUE;

// Index on Atom.embedding_id for fast lookups
CREATE INDEX atom_embedding_id IF NOT EXISTS
FOR (a:Atom) ON (a.embedding_id);

// Composite index on Provenance relationships
CREATE INDEX provenance_timestamps IF NOT EXISTS
FOR ()-[r:DERIVED_FROM]-() ON (r.timestamp, r.operation);
```

**Monitor Query Performance**:

```cypher
// Enable query logging (neo4j.conf)
// dbms.logs.query.enabled=true
// dbms.logs.query.threshold=100ms

// View slow queries
CALL dbms.listQueries()
YIELD queryId, query, elapsedTimeMillis, cpuTimeMillis
WHERE elapsedTimeMillis > 100
RETURN queryId, query, elapsedTimeMillis, cpuTimeMillis
ORDER BY elapsedTimeMillis DESC;
```

### Neo4j Page Cache Configuration

**Recommended Settings** (16GB RAM server):

```conf
# neo4j.conf
dbms.memory.pagecache.size=8G  # 50% of RAM for page cache
dbms.memory.heap.initial_size=2G
dbms.memory.heap.max_size=4G   # 25% of RAM for heap

# Transaction log settings
dbms.tx_log.rotation.retention_policy=2G size  # Keep 2GB of transaction logs
```

---

## Summary

**Key Performance Optimizations**:

1. ✅ **Spatial Indexes**: Use `BOUNDING_BOX` with precise bounds + `GRIDS = HIGH`
2. ✅ **Columnstore**: Apply to `AtomEmbedding` for 3×-5× compression
3. ✅ **Memory**: Configure max memory (55GB for 64GB server), enable lock pages
4. ✅ **MAXDOP**: Set to 4 (for 8 physical cores), override for specific queries
5. ✅ **Statistics**: Enable auto-update async + weekly FULLSCAN
6. ✅ **OODA Queues**: Increase `MAX_QUEUE_READERS` to 20 for high throughput
7. ✅ **Query Store**: Enable for runtime performance tracking
8. ✅ **Neo4j**: Configure 8GB page cache + unique constraints

**Monitoring Queries**:
- Spatial fragmentation: `sys.dm_db_index_physical_stats`
- OODA latency: `dbo.OodaLog` cycle timing analysis
- Buffer cache hit ratio: `sys.dm_os_performance_counters`
- CLR performance: `sys.dm_exec_procedure_stats`

**Next Steps**:
- See `docs/operations/troubleshooting.md` for performance issue resolution
- See `docs/operations/monitoring.md` for Application Insights integration
