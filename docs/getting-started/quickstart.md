# Quickstart Guide

**Get started with Hartonomous in 10 minutes**

This guide walks you through ingesting your first model and running semantic queries using the O(log N) + O(K) pattern.

## Prerequisites

- ✅ Installation completed (see `installation.md`)
- ✅ SQL Server running with Hartonomous database
- ✅ Neo4j running
- ✅ Worker services running

## Step 1: Verify System Health (1 minute)

```sql
USE Hartonomous;
GO

-- Check CLR functions available
SELECT COUNT(*) AS TotalClrFunctions
FROM sys.objects
WHERE type = 'FS';  -- CLR scalar function
-- Expected: 49

-- Check Service Broker enabled
SELECT is_broker_enabled 
FROM sys.databases 
WHERE name = 'Hartonomous';
-- Expected: 1

-- Check OODA loop job
SELECT TOP 1 
    j.name AS JobName,
    ja.run_status,
    ja.run_date,
    ja.run_time
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobactivity ja ON j.job_id = ja.job_id
WHERE j.name = 'OodaCycle_15min'
ORDER BY ja.run_date DESC, ja.run_time DESC;
-- Expected: run_status = 1 (Success)
```

## Step 2: Create Your Tenant (1 minute)

```sql
-- Create tenant
INSERT INTO dbo.Tenants (TenantName, AtomQuota, CreatedAt)
VALUES ('MyTenant', 1000000000, SYSUTCDATETIME());  -- 1B atom quota

DECLARE @TenantId INT = SCOPE_IDENTITY();
SELECT @TenantId AS YourTenantId;  -- Save this for later

-- Create user
INSERT INTO dbo.Users (Username, Email, TenantId, Role, CreatedAt)
VALUES ('you@example.com', 'you@example.com', @TenantId, 'admin', SYSUTCDATETIME());

DECLARE @UserId INT = SCOPE_IDENTITY();
SELECT @UserId AS YourUserId;  -- Save this too
```

## Step 3: Ingest a Small Model (3-5 minutes)

### Option A: Download Sample Model

```powershell
# Download small GGUF model (~3.5GB)
$modelUrl = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
$modelPath = "C:\Temp\tinyllama-1.1b.gguf"

Invoke-WebRequest -Uri $modelUrl -OutFile $modelPath
```

### Option B: Use Existing Model

If you already have a GGUF, SafeTensors, or ONNX model:

```sql
-- Just reference the path
DECLARE @ModelPath NVARCHAR(4000) = N'C:\Models\YourModel.gguf';
```

### Ingest the Model

```sql
-- Replace @TenantId with your tenant ID from Step 2
DECLARE @TenantId INT = 1;
DECLARE @ModelPath NVARCHAR(4000) = N'C:\Temp\tinyllama-1.1b.gguf';

-- Ingest model (creates atoms, generates embeddings, projects to 3D)
EXEC dbo.sp_IngestModel 
    @ModelPath = @ModelPath,
    @ModelName = 'TinyLlama-1.1B-Chat',
    @TenantId = @TenantId,
    @SourceType = 'file',
    @GenerateEmbeddings = 1,
    @ProjectToSpatial = 1;

-- Monitor progress
SELECT 
    JobId,
    JobType,
    Status,
    StartedAt,
    CompletedAt,
    DATEDIFF(SECOND, StartedAt, ISNULL(CompletedAt, SYSUTCDATETIME())) AS DurationSec,
    ErrorMessage
FROM dbo.IngestionJobs
WHERE TenantId = @TenantId
ORDER BY StartedAt DESC;
```

**Expected Duration**: 
- Parsing: 30-60 seconds
- Atomization: 1-2 minutes
- Spatialization: 1-2 minutes
- **Total**: 3-5 minutes for 1.1B parameter model

**Monitor Atoms Created**:

```sql
-- Check atoms created
SELECT COUNT(*) AS TotalAtoms
FROM dbo.TensorAtoms
WHERE TenantId = @TenantId;
-- Expected: ~500,000 atoms for TinyLlama-1.1B

-- Check spatial projections completed
SELECT 
    COUNT(*) AS TotalAtoms,
    SUM(CASE WHEN SpatialKey IS NOT NULL THEN 1 ELSE 0 END) AS ProjectedAtoms,
    SUM(CASE WHEN HilbertIndex IS NOT NULL THEN 1 ELSE 0 END) AS HilbertIndexed
FROM dbo.TensorAtoms
WHERE TenantId = @TenantId;
```

## Step 4: Run Your First Semantic Query (2 minutes)

### Query 1: Find Similar Weights

```sql
-- Find tensor atoms similar to a specific weight vector
DECLARE @TenantId INT = 1;
DECLARE @QueryAtomId BIGINT = (
    SELECT TOP 1 TensorAtomId 
    FROM dbo.TensorAtoms 
    WHERE TenantId = @TenantId 
      AND TensorName LIKE '%attention%'
    ORDER BY NEWID()  -- Random attention weight
);

-- Stage 1: Landmark projection (already done during ingestion)
-- Stage 2: R-Tree spatial query (O(log N))
-- Stage 3: STIntersects pre-filter
-- Stage 4: CLR cosine similarity refinement (O(K))

DECLARE @QuerySpatialKey GEOMETRY = (
    SELECT SpatialKey 
    FROM dbo.TensorAtoms 
    WHERE TensorAtomId = @QueryAtomId
);

SELECT TOP 20
    ta.TensorAtomId,
    ta.TensorName,
    ta.ModelId,
    m.ModelName,
    -- O(K) CLR refinement
    dbo.clr_CosineSimilarity(
        (SELECT EmbeddingVector FROM dbo.TensorAtoms WHERE TensorAtomId = @QueryAtomId),
        ta.EmbeddingVector
    ) AS CosineSimilarity,
    -- Spatial distance (3D)
    @QuerySpatialKey.STDistance(ta.SpatialKey) AS SpatialDistance
FROM dbo.TensorAtoms ta
INNER JOIN dbo.Models m ON ta.ModelId = m.ModelId
WHERE ta.TenantId = @TenantId
  AND ta.TensorAtomId != @QueryAtomId
  -- O(log N) spatial pre-filter
  AND ta.SpatialKey.STIntersects(@QuerySpatialKey.STBuffer(5.0)) = 1
ORDER BY CosineSimilarity DESC;
```

**Expected Results**:
- **Query Time**: 15-30ms for 500K atoms
- **Breakdown**: 
  - O(log N) R-Tree lookup: ~5ms
  - STIntersects filter: ~3ms
  - O(K) CLR refinement (K=20): ~10ms

### Query 2: Cross-Model Weight Comparison

```sql
-- Find which models share similar attention mechanisms
DECLARE @TenantId INT = 1;

SELECT 
    m1.ModelName AS Model1,
    m2.ModelName AS Model2,
    COUNT(*) AS SharedSimilarWeights,
    AVG(dbo.clr_CosineSimilarity(ta1.EmbeddingVector, ta2.EmbeddingVector)) AS AvgSimilarity
FROM dbo.TensorAtoms ta1
INNER JOIN dbo.TensorAtoms ta2 ON 
    ta1.SpatialKey.STDistance(ta2.SpatialKey) < 1.0  -- Close in 3D space
    AND ta1.TensorAtomId < ta2.TensorAtomId  -- Avoid duplicates
    AND ta1.TensorName LIKE '%attention%'
    AND ta2.TensorName LIKE '%attention%'
INNER JOIN dbo.Models m1 ON ta1.ModelId = m1.ModelId
INNER JOIN dbo.Models m2 ON ta2.ModelId = m2.ModelId
WHERE ta1.TenantId = @TenantId
  AND ta2.TenantId = @TenantId
  AND m1.ModelId != m2.ModelId  -- Different models
GROUP BY m1.ModelName, m2.ModelName
HAVING COUNT(*) > 5  -- At least 5 similar weights
ORDER BY AvgSimilarity DESC;
```

**Expected Results**: Shows which models have similar architectures based on weight similarity.

### Query 3: Provenance Tracking

```sql
-- Query Neo4j for model provenance
-- (Connect to Neo4j Browser: http://localhost:7474)
```

```cypher
// Find all atoms from the ingested model
MATCH (m:Model {name: 'TinyLlama-1.1B-Chat'})-[:INGESTED_FROM]->(s:Source)
RETURN m.name AS Model, 
       s.identifier AS SourceFile, 
       s.ingestedAt AS IngestedAt;

// Find ingestion job details
MATCH (a:Atom)-[:CREATED_BY_JOB]->(j:IngestionJob)-[:PROCESSED_SOURCE]->(s:Source)
WHERE s.identifier CONTAINS 'tinyllama'
RETURN j.jobType AS JobType,
       j.status AS Status,
       j.algorithmVersion AS AlgorithmVersion,
       j.atomsCreated AS AtomsCreated,
       j.completedAt AS CompletedAt;
```

## Step 5: Test OODA Loop (2 minutes)

### Trigger Manual Analysis

```sql
-- Manually trigger OODA cycle (don't wait for 15-min schedule)
DECLARE @handle UNIQUEIDENTIFIER;

BEGIN DIALOG CONVERSATION @handle
    FROM SERVICE [//Hartonomous/InitiatorService]
    TO SERVICE '//Hartonomous/AnalyzeService'
    ON CONTRACT [//Hartonomous/OodaContract];

SEND ON CONVERSATION @handle
    MESSAGE TYPE [//Hartonomous/Analyze] ('');

-- Wait 10-30 seconds for cycle to complete
WAITFOR DELAY '00:00:30';

-- Check execution log
SELECT TOP 10
    HypothesisId,
    HypothesisType,
    ActionSQL,
    EstimatedImpact,
    RiskLevel,
    Status,
    ExecutedAt,
    ErrorMessage
FROM dbo.OodaExecutionLog
ORDER BY ExecutedAt DESC;
```

**Expected Results**:
- **HypothesisType**: IndexOptimization, ConceptDiscovery, StatisticsUpdate
- **RiskLevel**: Low (auto-executed), Medium (queued for approval)
- **Status**: Success (if auto-executed)

### View Hypothesis Weights

```sql
-- See which hypotheses are performing best
SELECT 
    HypothesisType,
    SuccessCount,
    FailureCount,
    ConfidenceScore,
    AvgImpact,
    TotalExecutions,
    LastUpdated
FROM dbo.HypothesisWeights
ORDER BY ConfidenceScore DESC;
```

## Step 6: Explore Advanced Features (Optional)

### A* Pathfinding Between Concepts

```sql
-- Find semantic path from "attention" to "embedding"
DECLARE @StartAtomId BIGINT = (
    SELECT TOP 1 TensorAtomId 
    FROM dbo.TensorAtoms 
    WHERE TensorName LIKE '%attention%' 
    ORDER BY NEWID()
);

DECLARE @EndAtomId BIGINT = (
    SELECT TOP 1 TensorAtomId 
    FROM dbo.TensorAtoms 
    WHERE TensorName LIKE '%embedding%' 
    ORDER BY NEWID()
);

EXEC dbo.sp_GenerateOptimalPath 
    @StartAtomId = @StartAtomId,
    @EndAtomId = @EndAtomId,
    @TenantId = 1,
    @MaxHops = 5;
```

### DBSCAN Clustering

```sql
-- Discover semantic clusters in recent atoms
DECLARE @ClustersJson NVARCHAR(MAX) = dbo.clr_DbscanClustering_JSON(
    @minPts = 10,
    @epsilon = 0.15,
    @timeWindowHours = 24
);

-- Parse results
SELECT 
    JSON_VALUE(value, '$.clusterId') AS ClusterId,
    JSON_VALUE(value, '$.memberCount') AS Members,
    JSON_VALUE(value, '$.centroid') AS Centroid
FROM OPENJSON(@ClustersJson, '$.clusters');
```

### Temporal Queries (Laplace's Demon)

```sql
-- Query system state at a specific point in time
DECLARE @Timestamp DATETIME2 = '2025-11-15 10:30:00';

-- How many atoms existed at that time?
SELECT COUNT(*) AS AtomsAtTime
FROM dbo.Atoms
FOR SYSTEM_TIME AS OF @Timestamp;

-- What was the average query latency at that time?
SELECT AVG(TotalDurationMs) AS AvgLatencyMs
FROM dbo.InferenceRequests
WHERE RequestTimestamp BETWEEN DATEADD(HOUR, -1, @Timestamp) AND @Timestamp;
```

## Benchmarking Performance

### Measure O(log N) + O(K) Speedup

```sql
-- Baseline: Brute-force O(N) cosine similarity
SET STATISTICS TIME ON;

DECLARE @QueryVector VARBINARY(MAX) = (
    SELECT TOP 1 EmbeddingVector 
    FROM dbo.TensorAtoms 
    ORDER BY NEWID()
);

-- O(N) brute force (SLOW - don't run on large datasets!)
-- Commented out for safety
/*
SELECT TOP 20 TensorAtomId, dbo.clr_CosineSimilarity(@QueryVector, EmbeddingVector) AS Similarity
FROM dbo.TensorAtoms
ORDER BY Similarity DESC;
*/
-- Expected: ~5 seconds for 500K atoms (N = 500,000 comparisons)

-- O(log N) + O(K) optimized (FAST)
DECLARE @QuerySpatialKey GEOMETRY = (
    SELECT TOP 1 SpatialKey 
    FROM dbo.TensorAtoms 
    WHERE EmbeddingVector = @QueryVector
);

SELECT TOP 20
    ta.TensorAtomId,
    dbo.clr_CosineSimilarity(@QueryVector, ta.EmbeddingVector) AS Similarity
FROM dbo.TensorAtoms ta
WHERE ta.SpatialKey.STIntersects(@QuerySpatialKey.STBuffer(5.0)) = 1  -- O(log N)
ORDER BY Similarity DESC;  -- O(K log K)

SET STATISTICS TIME OFF;
-- Expected: 15-30ms (166× speedup)
-- With 3.5B atoms: ~3.6M× speedup (as proven in architecture)
```

## Troubleshooting Common Issues

### Issue: Ingestion Hangs

**Symptom**: `sp_IngestModel` runs for >10 minutes

**Solution**:
```sql
-- Check ingestion job status
SELECT * FROM dbo.IngestionJobs WHERE Status = 'InProgress';

-- Check worker service logs
-- (In PowerShell terminal where worker is running)
```

### Issue: Spatial Queries Return 0 Results

**Symptom**: Queries with `STIntersects` return no results

**Solution**:
```sql
-- Verify spatial projections completed
SELECT COUNT(*) AS TotalAtoms,
       SUM(CASE WHEN SpatialKey IS NOT NULL THEN 1 ELSE 0 END) AS ProjectedAtoms
FROM dbo.TensorAtoms;

-- If ProjectedAtoms = 0, run spatial projection worker manually
-- (Or wait for worker service to process queue)
```

### Issue: CLR Functions Not Found

**Symptom**: "Could not find stored procedure 'dbo.clr_CosineSimilarity'"

**Solution**:
```sql
-- Verify CLR assembly deployed
SELECT * FROM sys.assemblies WHERE name = 'HartonomousClr';

-- If not found, redeploy CLR assembly (see installation.md)
```

## Next Steps

**Congratulations!** 🎉 You've successfully:

✅ Ingested your first model (~500K atoms)  
✅ Executed semantic queries with O(log N) + O(K) pattern  
✅ Tracked provenance in Neo4j Merkle DAG  
✅ Triggered OODA autonomous optimization loop  

**Continue Learning**:

1. **Architecture**: Read `docs/architecture/semantic-first.md` to understand O(log N) + O(K) in depth
2. **Model Formats**: Read `docs/architecture/model-atomization.md` for 6 supported formats
3. **API Reference**: Read `docs/api/sql-procedures.md` for all stored procedures
4. **OODA Loop**: Read `docs/architecture/ooda-loop.md` for autonomous optimization details
5. **Production**: Read `docs/operations/clr-deployment.md` for production deployment

**Try Advanced Features**:
- Cross-modal queries (text → audio → image)
- Adversarial modeling (red team attacks)
- Multi-tenant isolation
- SVD compression (159:1 ratio)
- Behavioral geometry (user session analysis)

**Get Help**:
- GitHub Issues: https://github.com/AHartTN/Hartonomous-Sandbox/issues
- Documentation: `docs/README.md`
- Examples: `docs/examples/`

---

**Time Completed**: ~10 minutes ⏱️  
**Atoms Ingested**: ~500,000 🧬  
**Queries Executed**: 3-5 ✅  
**Performance**: O(log N) + O(K) = 166× faster than O(N) 🚀
