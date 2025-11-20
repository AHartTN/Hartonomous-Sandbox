# Performance Tuning

**Last Updated**: November 19, 2025  
**Status**: Production Ready

## Overview

Hartonomous performance optimization focuses on spatial query acceleration, OODA loop throughput, ingestion pipeline tuning, and CLR function efficiency.

## Spatial Query Optimization

### Index Configuration

**Primary spatial index** (R-Tree):
```sql
CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
ON dbo.AtomEmbeddings(SpatialGeometry)
WITH (
    BOUNDING_BOX = (-200, -200, -200, 200, 200, 200),
    GRIDS = (
        LEVEL_1 = MEDIUM,
        LEVEL_2 = MEDIUM,
        LEVEL_3 = MEDIUM,
        LEVEL_4 = MEDIUM
    ),
    CELLS_PER_OBJECT = 16,
    PAD_INDEX = OFF,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = ON,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
```

**Key parameters**:
- `BOUNDING_BOX`: Matches 3D coordinate space (typically ±200 for landmark projection)
- `GRIDS`: MEDIUM density for 3.5B atom scale
- `CELLS_PER_OBJECT`: 16 (default) for balanced tree depth

### Covering Index for Refinement

After spatial pre-filter, cosine refinement needs embedding vectors:
```sql
CREATE NONCLUSTERED INDEX IX_AtomEmbeddings_Covering
ON dbo.AtomEmbeddings(AtomId)
INCLUDE (EmbeddingVector, SpatialGeometry)
WITH (FILLFACTOR = 90, ONLINE = ON);
```

**Benefit**: Avoids key lookup during O(K) refinement phase.

### Query Optimization Patterns

**Pattern 1: Force spatial index hint**
```sql
-- Planner may choose scan for small radii
SELECT TOP 10 AtomId
FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1;
```

**Pattern 2: Use CTE to materialize candidates**
```sql
-- Separate pre-filter from refinement
WITH SpatialCandidates AS (
    SELECT AtomId, EmbeddingVector, SpatialGeometry
    FROM dbo.AtomEmbeddings WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
    WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(30.0)) = 1
)
SELECT TOP 10 
    AtomId,
    dbo.clr_CosineSimilarity(@QueryEmbedding, EmbeddingVector) AS Score
FROM SpatialCandidates
ORDER BY Score DESC;
```

**Pattern 3: Adjust semantic radius dynamically**
```sql
-- Adaptive radius: start small, increase if insufficient results
DECLARE @Radius FLOAT = 15.0;
DECLARE @ResultCount INT = 0;

WHILE @ResultCount < 10 AND @Radius < 50.0
BEGIN
    SELECT @ResultCount = COUNT(*)
    FROM dbo.AtomEmbeddings
    WHERE SpatialGeometry.STIntersects(@QueryGeometry.STBuffer(@Radius)) = 1;
    
    IF @ResultCount < 10
        SET @Radius = @Radius * 1.5;  -- Increase by 50%
END;
```

## OODA Loop Throughput

### Async Orient Phase

**Problem**: LLM calls block OODA loop (150-300ms per hypothesis)

**Solution**: Parallelize hypothesis generation
```csharp
// CesConsumer.cs
var hypothesisTasks = new List<Task<string>>();
for (int i = 0; i < 3; i++)  // Generate 3 hypotheses in parallel
{
    hypothesisTasks.Add(GenerateHypothesisAsync(input, sessionId));
}
var hypotheses = await Task.WhenAll(hypothesisTasks);
```

### Hypothesis Caching

Cache frequent Orient results to avoid repeated LLM calls:
```sql
CREATE TABLE dbo.HypothesisCache (
    InputHash VARBINARY(32) PRIMARY KEY NONCLUSTERED,
    Hypothesis NVARCHAR(MAX),
    Frequency INT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSDATETIME(),
    LastAccessedAt DATETIME2 DEFAULT SYSDATETIME(),
    INDEX IX_HypothesisCache_LastAccessed NONCLUSTERED (LastAccessedAt)
);

-- Check cache before Orient phase
DECLARE @InputHash VARBINARY(32) = HASHBYTES('SHA2_256', @Input);
DECLARE @CachedHypothesis NVARCHAR(MAX);

SELECT @CachedHypothesis = Hypothesis
FROM dbo.HypothesisCache
WHERE InputHash = @InputHash
    AND LastAccessedAt >= DATEADD(HOUR, -24, SYSDATETIME());

IF @CachedHypothesis IS NOT NULL
BEGIN
    -- Use cache
    UPDATE dbo.HypothesisCache
    SET Frequency = Frequency + 1, LastAccessedAt = SYSDATETIME()
    WHERE InputHash = @InputHash;
END
ELSE
BEGIN
    -- Generate new hypothesis
    EXEC dbo.sp_InvokeExternalLLM @Input, @CachedHypothesis OUTPUT;
    
    -- Insert into cache
    INSERT INTO dbo.HypothesisCache (InputHash, Hypothesis)
    VALUES (@InputHash, @CachedHypothesis);
END;
```

**Eviction policy**: Delete entries not accessed in 7 days
```sql
DELETE FROM dbo.HypothesisCache
WHERE LastAccessedAt < DATEADD(DAY, -7, SYSDATETIME());
```

### Decide Phase Optimization

**Problem**: Decision logic queries many atoms (slow aggregation)

**Solution**: Pre-compute decision metrics
```sql
-- Materialized view for decision support
CREATE TABLE dbo.DecisionMetrics (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY,
    TotalHypotheses INT,
    AvgHypothesisScore FLOAT,
    MaxHypothesisScore FLOAT,
    TopHypothesisId BIGINT,
    ComputedAt DATETIME2 DEFAULT SYSDATETIME(),
    INDEX IX_DecisionMetrics_ComputedAt NONCLUSTERED (ComputedAt)
);

-- Update after Orient phase
MERGE INTO dbo.DecisionMetrics AS target
USING (
    SELECT 
        @SessionId AS SessionId,
        COUNT(*) AS TotalHypotheses,
        AVG(Score) AS AvgHypothesisScore,
        MAX(Score) AS MaxHypothesisScore,
        (SELECT TOP 1 HypothesisId FROM dbo.Hypotheses WHERE SessionId = @SessionId ORDER BY Score DESC) AS TopHypothesisId
    FROM dbo.Hypotheses
    WHERE SessionId = @SessionId
) AS source
ON target.SessionId = source.SessionId
WHEN MATCHED THEN
    UPDATE SET 
        TotalHypotheses = source.TotalHypotheses,
        AvgHypothesisScore = source.AvgHypothesisScore,
        MaxHypothesisScore = source.MaxHypothesisScore,
        TopHypothesisId = source.TopHypothesisId,
        ComputedAt = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SessionId, TotalHypotheses, AvgHypothesisScore, MaxHypothesisScore, TopHypothesisId)
    VALUES (source.SessionId, source.TotalHypotheses, source.AvgHypothesisScore, source.MaxHypothesisScore, source.TopHypothesisId);
```

## Ingestion Pipeline Tuning

### Atomizer Parallelism

**Default**: 4 workers per tenant

**Tuning**:
```csharp
// Hartonomous.ModelIngestion/AtomizerWorker.cs
var workerCount = Environment.ProcessorCount / 2;  // Use 50% of CPU cores
var tasks = new List<Task>();

for (int i = 0; i < workerCount; i++)
{
    tasks.Add(Task.Run(() => ProcessAtomizerQueue(tenantId, modelId)));
}

await Task.WhenAll(tasks);
```

**Monitor throughput**:
```sql
SELECT 
    DATEADD(MINUTE, DATEDIFF(MINUTE, 0, IngestTimestamp) / 5 * 5, 0) AS TimeBucket,
    COUNT(*) AS AtomsIngested,
    COUNT(*) * 1.0 / 300 AS AtomsPerSecond  -- 5-minute bucket = 300 seconds
FROM dbo.Atoms
WHERE IngestTimestamp >= DATEADD(HOUR, -1, SYSDATETIME())
GROUP BY DATEADD(MINUTE, DATEDIFF(MINUTE, 0, IngestTimestamp) / 5 * 5, 0)
ORDER BY TimeBucket DESC;
```

**Target**: >100 atoms/second

### Batch Inserts

**Problem**: Individual atom inserts (1-by-1) slow

**Solution**: Batch via table-valued parameters
```sql
-- Table type for batch inserts
CREATE TYPE dbo.AtomTableType AS TABLE (
    Content VARBINARY(MAX),
    ContentType NVARCHAR(100),
    EmbeddingVector VARBINARY(MAX),
    SpatialGeometry GEOMETRY
);
GO

-- Batch insert procedure
CREATE PROCEDURE dbo.sp_BulkInsertAtoms
    @Atoms dbo.AtomTableType READONLY
AS
BEGIN
    -- Dual-database insert
    INSERT INTO dbo.Atoms (Content, ContentType, IngestTimestamp)
    OUTPUT inserted.AtomId, inserted.Content, inserted.ContentType
    SELECT Content, ContentType, SYSDATETIME()
    FROM @Atoms;

    -- Insert embeddings (separate transaction for failure isolation)
    INSERT INTO dbo.AtomEmbeddings (AtomId, EmbeddingVector, SpatialGeometry)
    SELECT a.AtomId, atoms.EmbeddingVector, atoms.SpatialGeometry
    FROM @Atoms atoms
    INNER JOIN dbo.Atoms a 
        ON a.Content = atoms.Content 
        AND a.ContentType = atoms.ContentType
    WHERE a.IngestTimestamp >= DATEADD(SECOND, -10, SYSDATETIME());
END;
```

**C# usage**:
```csharp
var dataTable = new DataTable();
dataTable.Columns.Add("Content", typeof(byte[]));
dataTable.Columns.Add("ContentType", typeof(string));
dataTable.Columns.Add("EmbeddingVector", typeof(byte[]));
dataTable.Columns.Add("SpatialGeometry", typeof(SqlGeometry));

foreach (var atom in atomBatch)
{
    dataTable.Rows.Add(atom.Content, atom.ContentType, atom.Embedding, atom.Geometry);
}

using var cmd = new SqlCommand("dbo.sp_BulkInsertAtoms", connection);
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.AddWithValue("@Atoms", dataTable);
await cmd.ExecuteNonQueryAsync();
```

**Benchmark**: 1000 atoms insert in ~800ms (vs 8-10 seconds for 1-by-1)

### Service Broker Throughput

**Problem**: Queue processing bottleneck

**Solution 1**: Increase activation parallelism
```sql
ALTER QUEUE dbo.IngestionQueue
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ProcessIngestionQueue,
    MAX_QUEUE_READERS = 8,  -- Increased from 4
    EXECUTE AS OWNER
);
```

**Solution 2**: Batch receive messages
```sql
CREATE PROCEDURE dbo.sp_ProcessIngestionQueue
AS
BEGIN
    DECLARE @MessageBatch TABLE (
        ConversationHandle UNIQUEIDENTIFIER,
        MessageBody VARBINARY(MAX)
    );

    -- Receive batch of 100 messages
    RECEIVE TOP (100)
        conversation_handle,
        message_body
    INTO @MessageBatch
    FROM dbo.IngestionQueue;

    -- Process batch (insert into staging table, then bulk process)
    INSERT INTO dbo.IngestionStaging (TenantId, ModelId, AtomData)
    SELECT 
        JSON_VALUE(CAST(MessageBody AS NVARCHAR(MAX)), '$.tenantId'),
        JSON_VALUE(CAST(MessageBody AS NVARCHAR(MAX)), '$.modelId'),
        JSON_QUERY(CAST(MessageBody AS NVARCHAR(MAX)), '$.atomData')
    FROM @MessageBatch;

    -- End conversations
    DECLARE @ConversationHandle UNIQUEIDENTIFIER;
    DECLARE conversation_cursor CURSOR FOR 
        SELECT ConversationHandle FROM @MessageBatch;
    OPEN conversation_cursor;
    FETCH NEXT FROM conversation_cursor INTO @ConversationHandle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        END CONVERSATION @ConversationHandle;
        FETCH NEXT FROM conversation_cursor INTO @ConversationHandle;
    END;
    CLOSE conversation_cursor;
    DEALLOCATE conversation_cursor;
END;
```

## CLR Function Optimization

### CosineSimilarity Performance

**Problem**: Cosine similarity called millions of times (O(K) refinement)

**Solution 1**: Use SIMD (System.Numerics.Vectors)
```csharp
public static SqlDouble clr_CosineSimilarity(SqlBytes vectorA, SqlBytes vectorB)
{
    var a = MemoryMarshal.Cast<byte, float>(vectorA.Value);
    var b = MemoryMarshal.Cast<byte, float>(vectorB.Value);

    float dot = 0, magA = 0, magB = 0;
    int vecSize = Vector<float>.Count;
    int i = 0;

    // SIMD loop (process 4-8 floats per iteration)
    for (; i <= a.Length - vecSize; i += vecSize)
    {
        var va = new Vector<float>(a.Slice(i, vecSize));
        var vb = new Vector<float>(b.Slice(i, vecSize));
        dot += Vector.Dot(va, vb);
        magA += Vector.Dot(va, va);
        magB += Vector.Dot(vb, vb);
    }

    // Scalar remainder
    for (; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magA += a[i] * a[i];
        magB += b[i] * b[i];
    }

    return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
}
```

**Benchmark**: ~2-3× faster than scalar loop for 1536-dim vectors

**Solution 2**: Cache frequently queried embeddings
```sql
-- In-memory cache for hot embeddings
CREATE TABLE dbo.EmbeddingCache (
    AtomId BIGINT PRIMARY KEY NONCLUSTERED,
    EmbeddingVector VARBINARY(MAX),
    AccessCount INT DEFAULT 1,
    LastAccessedAt DATETIME2 DEFAULT SYSDATETIME()
) WITH (MEMORY_OPTIMIZED = ON);

-- Check cache before reading from disk
DECLARE @CachedEmbedding VARBINARY(MAX) = (
    SELECT EmbeddingVector 
    FROM dbo.EmbeddingCache 
    WHERE AtomId = @QueryAtomId
);

IF @CachedEmbedding IS NULL
BEGIN
    SELECT @CachedEmbedding = EmbeddingVector
    FROM dbo.AtomEmbeddings
    WHERE AtomId = @QueryAtomId;
    
    -- Add to cache
    INSERT INTO dbo.EmbeddingCache (AtomId, EmbeddingVector)
    VALUES (@QueryAtomId, @CachedEmbedding);
END;
```

### LandmarkProjection Optimization

**Problem**: ProjectTo3D called during ingestion for every atom

**Solution**: Precompute landmark inner products
```csharp
// Cache landmark magnitudes (computed once on assembly load)
private static float[] _landmarkX, _landmarkY, _landmarkZ;
private static float _magX, _magY, _magZ;

static LandmarkProjection()
{
    // Load landmarks from SQL Server
    using var conn = new SqlConnection("context connection=true");
    using var cmd = new SqlCommand(@"
        SELECT AxisAssignment, Vector 
        FROM dbo.SpatialLandmarks 
        WHERE AxisAssignment IN ('X', 'Y', 'Z')", conn);
    conn.Open();
    using var reader = cmd.ExecuteReader();
    
    while (reader.Read())
    {
        var axis = reader.GetString(0);
        var vector = MemoryMarshal.Cast<byte, float>(reader.GetSqlBytes(1).Value);
        
        if (axis == "X")
        {
            _landmarkX = vector.ToArray();
            _magX = (float)Math.Sqrt(vector.ToArray().Sum(v => v * v));
        }
        else if (axis == "Y")
        {
            _landmarkY = vector.ToArray();
            _magY = (float)Math.Sqrt(vector.ToArray().Sum(v => v * v));
        }
        else if (axis == "Z")
        {
            _landmarkZ = vector.ToArray();
            _magZ = (float)Math.Sqrt(vector.ToArray().Sum(v => v * v));
        }
    }
}

public static SqlGeometry clr_LandmarkProjection_ProjectTo3D(
    SqlBytes embedding, ...)
{
    var emb = MemoryMarshal.Cast<byte, float>(embedding.Value);
    
    // Use cached landmarks
    float x = DotProduct(emb, _landmarkX) / _magX;
    float y = DotProduct(emb, _landmarkY) / _magY;
    float z = DotProduct(emb, _landmarkZ) / _magZ;
    
    return SqlGeometry.Point(x, y, z, 0);
}
```

## Query Store Auto-Tuning

Enable automatic plan regression detection:
```sql
ALTER DATABASE Hartonomous
SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    QUERY_CAPTURE_MODE = AUTO,
    SIZE_BASED_CLEANUP_MODE = AUTO,
    MAX_STORAGE_SIZE_MB = 2048,
    INTERVAL_LENGTH_MINUTES = 5
);

-- Enable automatic tuning
ALTER DATABASE SCOPED CONFIGURATION 
SET AUTOMATIC_TUNING (FORCE_LAST_GOOD_PLAN = ON);
```

**Monitor regressed queries**:
```sql
SELECT 
    qsq.query_id,
    qsqt.query_sql_text,
    qsrs.avg_duration / 1000 AS AvgDurationMs,
    qsrs.count_executions,
    qsp.plan_id,
    qsp.is_forced_plan
FROM sys.query_store_query qsq
INNER JOIN sys.query_store_query_text qsqt ON qsq.query_text_id = qsqt.query_text_id
INNER JOIN sys.query_store_plan qsp ON qsq.query_id = qsp.query_id
INNER JOIN sys.query_store_runtime_stats qsrs ON qsp.plan_id = qsrs.plan_id
WHERE qsrs.avg_duration > 50000  -- >50ms
    AND qsrs.last_execution_time >= DATEADD(HOUR, -1, SYSDATETIME())
ORDER BY qsrs.avg_duration DESC;
```

## Index Maintenance

**Weekly rebuild schedule**:
```sql
-- Rebuild all spatial indexes
DECLARE @TableName NVARCHAR(255);
DECLARE @IndexName NVARCHAR(255);
DECLARE @SQL NVARCHAR(MAX);

DECLARE index_cursor CURSOR FOR
    SELECT 
        OBJECT_NAME(i.object_id) AS TableName,
        i.name AS IndexName
    FROM sys.indexes i
    WHERE i.type_desc = 'SPATIAL';

OPEN index_cursor;
FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'ALTER INDEX ' + @IndexName + ' ON ' + @TableName + ' REBUILD WITH (ONLINE = ON, MAXDOP = 4)';
    PRINT @SQL;
    EXEC sp_executesql @SQL;
    FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;
END;

CLOSE index_cursor;
DEALLOCATE index_cursor;

-- Update statistics
EXEC sp_updatestats;
```

**Schedule via SQL Agent Job**:
```sql
EXEC msdb.dbo.sp_add_job @job_name = 'Hartonomous_IndexMaintenance';
EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'Hartonomous_IndexMaintenance',
    @step_name = 'RebuildIndexes',
    @subsystem = 'TSQL',
    @command = '-- (SQL from above)';
EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'WeeklySunday',
    @freq_type = 8,  -- Weekly
    @freq_interval = 1,  -- Sunday
    @active_start_time = 20000;  -- 2:00 AM
EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'Hartonomous_IndexMaintenance',
    @schedule_name = 'WeeklySunday';
```

## Monitoring Performance Gains

**Before/after comparison**:
```sql
-- Baseline (before tuning)
SELECT AVG(DurationMs) AS AvgDurationMs
FROM dbo.QueryPerformanceLog
WHERE QueryText LIKE '%STIntersects%'
    AND ExecutedAt BETWEEN '2025-11-01' AND '2025-11-15';

-- After tuning (compare to baseline)
SELECT AVG(DurationMs) AS AvgDurationMs
FROM dbo.QueryPerformanceLog
WHERE QueryText LIKE '%STIntersects%'
    AND ExecutedAt >= '2025-11-16';
```

**Expected improvements**:
- Spatial queries: 50ms → 20-30ms (40-60% faster)
- OODA loop: 250ms → 120-150ms (40-50% faster)
- Ingestion: 50 atoms/sec → 150 atoms/sec (3× throughput)

## Best Practices

1. **Spatial Indexes**: Rebuild weekly, check fragmentation daily
2. **OODA Caching**: Cache Orient hypotheses for 24 hours
3. **Batch Operations**: Batch inserts (100-1000 atoms), batch Service Broker receives (100 messages)
4. **CLR SIMD**: Use System.Numerics.Vectors for vector math
5. **Query Store**: Enable auto-tuning for plan regression detection
6. **In-Memory Tables**: Use for hot data (caches, metrics)
7. **Monitor Throughput**: Alert if atoms/sec <100 or OODA loop >200ms

## Summary

Hartonomous performance tuning:

- **Spatial Queries**: R-Tree indexes + covering indexes + adaptive radius
- **OODA Loop**: Async Orient, hypothesis caching, pre-computed metrics
- **Ingestion**: Parallel workers, batch inserts, Service Broker tuning
- **CLR Functions**: SIMD vectorization, landmark caching
- **Maintenance**: Weekly index rebuilds, Query Store auto-tuning

Target performance: <30ms spatial queries, <150ms OODA loop, >150 atoms/sec ingestion.
