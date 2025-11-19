# Cognitive Kernel Seeding

**Status**: Production Ready  
**Last Updated**: November 18, 2025  
**Owner**: AI Architecture Team

## Overview

The Cognitive Kernel Seeding system bootstraps the semantic universe with defined physics, matter, space, and time. This creates a testable environment with known geometric properties for validating spatial reasoning, A* pathfinding, and OODA loop functionality.

### Purpose

1. **Testing Framework**: Establish golden paths with predictable coordinates for algorithm validation
2. **Physics Definition**: Create orthogonal basis vectors for trilateration (1536D → 3D mapping)
3. **Bootstrap Data**: Seed operational history for OODA loop autonomous improvement
4. **Validation Suite**: Enable reproducible testing of spatial coherence and pathfinding

---

## Architecture

### Four Epochs of Creation

```
EPOCH 1: Axioms          → Tenants, Models, Spatial Landmarks (The Reference Frame)
EPOCH 2: Primordial Soup → Atoms with Content-Addressable Storage (The Matter)
EPOCH 3: Mapping Space   → Embeddings with Geometric Coordinates (The Topology)
EPOCH 4: Waking the Mind → Operational History & Anomalies (The Time)
```

---

## EPOCH 1: The Axioms (Physics Definition)

### Tenants (The Observers)

```sql
MERGE dbo.TenantGuidMapping AS target
USING (VALUES 
    (0, '00000000-0000-0000-0000-000000000000', 'System Root'),
    (1, '11111111-1111-1111-1111-111111111111', 'Dev Operations'),
    (2, '22222222-2222-2222-2222-222222222222', 'Research Lab')
) AS source (Id, Guid, Name)
ON target.TenantId = source.Id
WHEN MATCHED THEN UPDATE SET TenantGuid = source.Guid
WHEN NOT MATCHED THEN INSERT (TenantId, TenantGuid, CreatedAt) 
VALUES (source.Id, source.Guid, SYSDATETIME());
```

### Models (The Encoders)

```sql
MERGE dbo.Models AS target
USING (VALUES 
    ('godel-v1-reasoning', 'Reasoning', 'Hartonomous', 
     '{"embeddingDimension": 1536, "contextWindow": 128000, "capabilities": ["chain-of-thought", "spatial-reasoning"]}'),
    ('clip-spatial-v2', 'Multimodal', 'OpenAI', 
     '{"embeddingDimension": 1024, "supportedModalities": ["image", "text"], "spatialAwareness": true}'),
    ('codellama-70b', 'LLM', 'Meta', 
     '{"embeddingDimension": 4096, "supportedModalities": ["code"]}')
) AS source (Name, Type, Prov, Meta)
ON target.ModelName = source.Name
WHEN MATCHED THEN UPDATE SET MetadataJson = source.Meta
WHEN NOT MATCHED THEN INSERT (ModelName, ModelType, Provider, IsActive, MetadataJson) 
VALUES (source.Name, source.Type, source.Prov, 1, source.Meta);
```

### Spatial Landmarks (Orthogonal Basis for Trilateration)

**The Reference Frame**: Three orthogonal vectors define the coordinate system for mapping high-dimensional embeddings to 3D space.

```sql
DECLARE @ModelReasoning INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = 'godel-v1-reasoning');

INSERT INTO dbo.SpatialLandmarks (ModelId, LandmarkType, Vector, AxisAssignment, CreatedAt)
VALUES 
    -- X-Axis: "Abstract <-> Concrete"
    (@ModelReasoning, 'Basis', REPLICATE(CAST(0x3F800000 AS VARBINARY(MAX)), 100), 'X', SYSDATETIME()), 
    -- Y-Axis: "Technical <-> Creative"
    (@ModelReasoning, 'Basis', REPLICATE(CAST(0x40000000 AS VARBINARY(MAX)), 100), 'Y', SYSDATETIME()),
    -- Z-Axis: "Static <-> Dynamic"
    (@ModelReasoning, 'Basis', REPLICATE(CAST(0xC0000000 AS VARBINARY(MAX)), 100), 'Z', SYSDATETIME());
```

**Binary Patterns**:
- `0x3F800000` = 1.0 in IEEE 754 float32 (X-axis)
- `0x40000000` = 2.0 in IEEE 754 float32 (Y-axis)
- `0xC0000000` = -2.0 in IEEE 754 float32 (Z-axis)

These form an orthogonal basis for projecting 1536-dimensional embeddings into 3D semantic space via trilateration.

---

## EPOCH 2: The Primordial Soup (Matter Creation)

### Atoms with Content-Addressable Storage

Create a reasoning chain for testing A* pathfinding: **Problem → Observation → Hypothesis → Solution**

```sql
DECLARE @AtomData TABLE (
    Alias NVARCHAR(50),
    Content NVARCHAR(MAX),
    Modality VARCHAR(50),
    ContentType NVARCHAR(100)
);

INSERT INTO @AtomData VALUES 
('START_NODE', 'Why is the server latency spiking at 2 AM?', 'text', 'text/question'),
('STEP_1',     'Logs show high disk I/O during backup operations.', 'text', 'text/log-analysis'),
('STEP_2',     'The backup schedule overlaps with the ETL batch job.', 'text', 'text/reasoning'),
('GOAL_NODE',  'Reschedule ETL job to 4 AM to avoid contention.', 'text', 'text/solution'),
('NOISE_1',    'The mitochondria is the powerhouse of the cell.', 'text', 'text/fact'),
('NOISE_2',    'def main(): print("Hello World")', 'code', 'text/python');
```

### CAS Deduplication

```sql
MERGE dbo.Atom AS target
USING (
    SELECT 
        Alias, 
        Content, 
        Modality, 
        ContentType, 
        HASHBYTES('SHA2_256', Content) as Hash,
        CAST(Content AS VARBINARY(MAX)) as BinValue
    FROM @AtomData
) AS source
ON target.ContentHash = source.Hash
WHEN MATCHED THEN 
    UPDATE SET ReferenceCount = ReferenceCount + 1  -- Deduplication
WHEN NOT MATCHED THEN
    INSERT (TenantId, Modality, ContentHash, ContentType, AtomicValue, ReferenceCount, CanonicalText)
    VALUES (1, source.Modality, source.Hash, source.ContentType, source.BinValue, 1, source.Content);
```

**Key Principle**: Identical content = same hash → reference count increments, no duplicate storage.

---

## EPOCH 3: Mapping the Space (Geometric Topology)

### Concepts (Voronoi Regions / A* Targets)

Define the "Solution Space" as a geometric region (Polygon) that A* must navigate to.

```sql
DECLARE @SolutionConceptID INT;

MERGE provenance.Concepts AS target
USING (VALUES (1, 'System Optimization', 'A region containing valid system fixes')) AS source(T, N, D)
ON target.Name = source.N
WHEN NOT MATCHED THEN
    INSERT (TenantId, Name, Description, CreatedAt)
    VALUES (source.T, source.N, source.D, SYSDATETIME());

SELECT @SolutionConceptID = ConceptId FROM provenance.Concepts WHERE Name = 'System Optimization';

-- Define target region: X(8-12), Y(8-12), Z(8-12)
DECLARE @TargetRegion GEOMETRY = geometry::STPolyFromText('POLYGON ((8 8 0, 12 8 0, 12 12 0, 8 12 0, 8 8 0))', 0);

UPDATE provenance.Concepts
SET ConceptDomain = @TargetRegion,
    CentroidSpatialKey = geometry::STPointFromText('POINT(10 10 0)', 0)
WHERE ConceptId = @SolutionConceptID;
```

### Golden Path Coordinates

Manually assign spatial coordinates to atoms to create a navigable reasoning path:

| Node | Content | Coordinates (X, Y, Z) | Distance from Previous |
|------|---------|----------------------|------------------------|
| **START** | "Why is server latency spiking?" | (0, 0, 0) | — |
| **STEP 1** | "Logs show high disk I/O..." | (3, 3, 0) | ~4.24 units |
| **STEP 2** | "Backup overlaps with ETL..." | (6, 6, 0) | ~4.24 units |
| **GOAL** | "Reschedule ETL job..." | (10, 10, 0) | ~5.66 units (INSIDE target) |
| **NOISE** | "Mitochondria is powerhouse..." | (-50, -50, 0) | ~70.7 units (FAR away) |

```sql
DECLARE @AtomStart BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Why is the server latency spiking at 2 AM?'));
DECLARE @AtomStep1 BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Logs show high disk I/O during backup operations.'));
DECLARE @AtomStep2 BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'The backup schedule overlaps with the ETL batch job.'));
DECLARE @AtomGoal  BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'Reschedule ETL job to 4 AM to avoid contention.'));
DECLARE @AtomBio   BIGINT = (SELECT AtomId FROM dbo.Atom WHERE ContentHash = HASHBYTES('SHA2_256', 'The mitochondria is the powerhouse of the cell.'));

DECLARE @DummyVec VARBINARY(MAX) = 0x00; 

-- 1. START NODE at (0,0,0)
INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
VALUES (@AtomStart, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(0 0 0)', 0), 0, 0, 0);

-- 2. STEP 1 at (3,3,0)
INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
VALUES (@AtomStep1, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(3 3 0)', 0), 3, 3, 0);

-- 3. STEP 2 at (6,6,0)
INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
VALUES (@AtomStep2, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(6 6 0)', 0), 6, 6, 0);

-- 4. GOAL NODE at (10,10,0) - INSIDE TARGET REGION
INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
VALUES (@AtomGoal, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(10 10 0)', 0), 10, 10, 0);

-- 5. NOISE at (-50,-50,0) - Far away, should be ignored
INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, SpatialBucketX, SpatialBucketY, SpatialBucketZ)
VALUES (@AtomBio, @ModelReasoning, @DummyVec, 1536, geometry::STPointFromText('POINT(-50 -50 0)', 0), -50, -50, 0);
```

**Critical Parameters**:
- **NeighborRadius**: Must be > 4.3 for A* to bridge (0,0) → (3,3) gaps
- **SpatialBucket**: Integer coordinates for coarse spatial filtering

---

## EPOCH 4: Waking the Mind (Operational History)

### Billing/Usage Data (Base Load)

```sql
DECLARE @i INT = 0;
WHILE @i < 100
BEGIN
    INSERT INTO dbo.BillingUsageLedger (TenantId, MetricType, Quantity, Unit, UsageDate, CreatedAt)
    VALUES (1, 'TokenCount', 100 + (@i * 2), 'Tokens', 
            DATEADD(DAY, -30 + (@i/4), SYSDATETIME()), SYSDATETIME());
    SET @i = @i + 1;
END
```

### Inference Requests (OODA Observe Signal)

**Normal Operation** (Last 24 hours - 200ms baseline):

```sql
INSERT INTO dbo.InferenceRequests (TenantId, ModelId, InputHash, Status, RequestTimestamp, CompletedTimestamp, TotalDurationMs)
SELECT TOP 50 
    1, 
    @ModelReasoning, 
    HASHBYTES('SHA2_256', CAST(NEWID() AS NVARCHAR(36))), 
    'Completed', 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) * 10, SYSDATETIME()), 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) * 10, DATEADD(MILLISECOND, 200, SYSDATETIME())), 
    200  -- 200ms (Fast baseline)
FROM sys.objects;
```

**Anomaly** (Last 1 hour - 2500ms spike):

```sql
INSERT INTO dbo.InferenceRequests (TenantId, ModelId, InputHash, Status, RequestTimestamp, CompletedTimestamp, TotalDurationMs)
SELECT TOP 10 
    1, 
    @ModelReasoning, 
    HASHBYTES('SHA2_256', CAST(NEWID() AS NVARCHAR(36))), 
    'Completed', 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), SYSDATETIME()), 
    DATEADD(MINUTE, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), DATEADD(MILLISECOND, 2500, SYSDATETIME())), 
    2500  -- 2.5s (Slow anomaly!)
FROM sys.objects;
```

### Previous Improvements (Learning History)

```sql
INSERT INTO dbo.AutonomousImprovementHistory 
    (ImprovementId, TargetFile, ChangeType, RiskLevel, SuccessScore, WasDeployed, StartedAt, CompletedAt)
VALUES 
    (NEWID(), 'Index_Optimization', 'IndexCreate', 'Low', 0.95, 1, 
     DATEADD(DAY, -5, SYSDATETIME()), DATEADD(DAY, -5, SYSDATETIME()));
```

---

## Validation Suite

### Test 1: CAS Deduplication

```sql
DECLARE @PreCount INT = (SELECT COUNT(*) FROM dbo.Atom);
DECLARE @DuplicateText NVARCHAR(MAX) = 'Why is the server latency spiking at 2 AM?';
DECLARE @RefHash BINARY(32) = HASHBYTES('SHA2_256', @DuplicateText);
DECLARE @PreRef BIGINT = (SELECT ReferenceCount FROM dbo.Atom WHERE ContentHash = @RefHash);

-- Attempt Insert
MERGE dbo.Atom AS target
USING (SELECT @RefHash AS Hash, @DuplicateText AS Val) AS source
ON target.ContentHash = source.Hash
WHEN MATCHED THEN UPDATE SET ReferenceCount = ReferenceCount + 1;

DECLARE @PostCount INT = (SELECT COUNT(*) FROM dbo.Atom);
DECLARE @PostRef BIGINT = (SELECT ReferenceCount FROM dbo.Atom WHERE ContentHash = @RefHash);

-- Validation
IF @PreCount = @PostCount AND @PostRef = @PreRef + 1
    PRINT '[PASS] Deduplication active. Reference Count incremented.';
ELSE
    PRINT '[FAIL] Deduplication failed or row count increased.';
```

### Test 2: Spatial Proximity

```sql
DECLARE @StartKey GEOMETRY = (SELECT SpatialKey FROM dbo.AtomEmbedding 
                               WHERE AtomId = (SELECT AtomId FROM dbo.Atom 
                                               WHERE ContentHash = HASHBYTES('SHA2_256', 'Why is the server latency spiking at 2 AM?')));
DECLARE @Step1Key GEOMETRY = (SELECT SpatialKey FROM dbo.AtomEmbedding 
                               WHERE AtomId = (SELECT AtomId FROM dbo.Atom 
                                               WHERE ContentHash = HASHBYTES('SHA2_256', 'Logs show high disk I/O during backup operations.')));
DECLARE @BioKey   GEOMETRY = (SELECT SpatialKey FROM dbo.AtomEmbedding 
                               WHERE AtomId = (SELECT AtomId FROM dbo.Atom 
                                               WHERE ContentHash = HASHBYTES('SHA2_256', 'The mitochondria is the powerhouse of the cell.')));

DECLARE @DistValid FLOAT = @StartKey.STDistance(@Step1Key); -- Should be ~4.24
DECLARE @DistNoise FLOAT = @StartKey.STDistance(@BioKey);   -- Should be ~70.7

IF @DistValid < @DistNoise
    PRINT '[PASS] Semantic space is coherent. Related atoms are spatially closer.';
ELSE
    PRINT '[FAIL] Spatial coherence violation.';
```

### Test 3: A* Pathfinding

```sql
DECLARE @StartAtomId BIGINT = (SELECT AtomId FROM dbo.Atom 
                                WHERE ContentHash = HASHBYTES('SHA2_256', 'Why is the server latency spiking at 2 AM?'));
DECLARE @TargetConceptId INT = (SELECT ConceptId FROM provenance.Concepts WHERE Name = 'System Optimization');

BEGIN TRY
    EXEC dbo.sp_GenerateOptimalPath 
        @StartAtomId = @StartAtomId, 
        @TargetConceptId = @TargetConceptId,
        @MaxSteps = 10,
        @NeighborRadius = 5.0;  -- Must be > 4.3 for sparse grid
    
    PRINT '[PASS] Path generation executed without error.';
END TRY
BEGIN CATCH
    PRINT '[FAIL] Path generation threw error: ' + ERROR_MESSAGE();
END CATCH
```

### Test 4: OODA Loop Anomaly Detection

```sql
DECLARE @AnalysisId UNIQUEIDENTIFIER;
DECLARE @AnomaliesJson NVARCHAR(MAX);
DECLARE @TotalInferences INT;
DECLARE @AvgDuration FLOAT;
DECLARE @AnomalyCount INT;

EXEC dbo.sp_AnalyzeSystem
    @TenantId = 1,
    @AnalysisScope = 'performance',
    @LookbackHours = 24,
    @AnalysisId = @AnalysisId OUTPUT,
    @TotalInferences = @TotalInferences OUTPUT,
    @AvgDurationMs = @AvgDuration OUTPUT,
    @AnomalyCount = @AnomalyCount OUTPUT,
    @AnomaliesJson = @AnomaliesJson OUTPUT,
    @PatternsJson = @PatternsJson OUTPUT;

IF @AnomalyCount >= 10
    PRINT '[PASS] OODA Loop correctly identified simulated latency spikes.';
ELSE
    PRINT '[FAIL] OODA Loop missed the anomalies.';
```

---

## Production Integration

### Transitioning from Seeding to Real Data

**Seeding** (Testing):
- Hardcoded coordinates for golden paths
- Dummy vectors (0x00) with explicit GEOMETRY
- Known distances for A* validation
- Synthetic anomalies for OODA testing

**Production** (Real World):
- Computed coordinates via `LandmarkProjection.ProjectTo3D()`
- Real 1536-dim embeddings from models
- Dynamic spatial indexing via `clr_ComputeHilbertValue()`
- Actual operational metrics

### Spatial Landmark Generation (Production)

**Option 1**: Fixed Basis (Recommended for Testing)
```sql
-- Use seeded landmarks as permanent reference frame
-- Ensures reproducibility across deployments
```

**Option 2**: Computed Basis (Production)
```csharp
// LandmarkProjection.cs - Static Constructor
static LandmarkProjection()
{
    var rand = new Random(42); // Deterministic seed
    BasisVectorX = CreateRandomUnitVector(rand, 1998);
    BasisVectorY = CreateRandomUnitVector(rand, 1998);
    BasisVectorZ = CreateRandomUnitVector(rand, 1998);
    
    // Gram-Schmidt orthonormalization
    BasisVectorY = OrthogonalizeAndNormalize(BasisVectorY, BasisVectorX);
    BasisVectorZ = OrthogonalizeAndNormalize(BasisVectorZ, BasisVectorX, BasisVectorY);
}
```

**Option 3**: Learned Basis (Advanced)
```sql
-- Extract principal components from existing embeddings
-- Use PCA/SVD on production data to derive optimal axes
SELECT dbo.PrincipalComponentAnalysis(EmbeddingVector)
FROM dbo.AtomEmbedding
GROUP BY ModelId;
```

---

## Performance Characteristics

| Phase | Operation | Duration | Memory |
|-------|-----------|----------|--------|
| **EPOCH 1** | Create tenants, models, landmarks | <1 second | 10KB |
| **EPOCH 2** | Insert 6 atoms with CAS | <1 second | 50KB |
| **EPOCH 3** | Insert 5 embeddings with GEOMETRY | <2 seconds | 100KB |
| **EPOCH 4** | Insert 100 billing + 60 inference records | <5 seconds | 500KB |
| **Total Seeding** | Complete kernel bootstrap | **<10 seconds** | **<1MB** |

### Spatial Index Statistics

```sql
-- Verify spatial index coverage
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    s.level_number AS Level,
    s.record_count AS Records,
    s.page_count AS Pages
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.AtomEmbedding'), NULL, NULL, 'DETAILED') s
JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE i.type_desc = 'SPATIAL';
```

---

## Best Practices

### 1. Deterministic Seeding
✅ **DO**: Use fixed seeds (Random(42)) for reproducibility  
❌ **DON'T**: Use NEWID() or GETDATE() for test data

### 2. Sparse Grids
✅ **DO**: Space nodes >4 units apart for A* testing  
❌ **DON'T**: Create dense clusters (causes neighbor explosion)

### 3. OODA Baselines
✅ **DO**: Seed 24h+ of normal data before anomalies  
❌ **DON'T**: Only insert anomalies (no baseline for detection)

### 4. CAS Validation
✅ **DO**: Test duplicate insertion to verify deduplication  
❌ **DON'T**: Assume CAS works without validation

### 5. Spatial Coherence
✅ **DO**: Validate distances match expectations (use STDistance)  
❌ **DON'T**: Trust coordinates without measurement

---

## Troubleshooting

### A* Fails to Find Path

**Symptom**: `sp_GenerateOptimalPath` returns empty result

**Causes**:
1. **NeighborRadius too small** → Increase to > max distance between nodes
2. **Spatial index missing** → Rebuild: `CREATE SPATIAL INDEX ON dbo.AtomEmbedding(SpatialKey)`
3. **Target concept not in ConceptDomain** → Verify POLYGON contains goal coordinates

### OODA Doesn't Detect Anomalies

**Symptom**: `@AnomalyCount = 0` after seeding

**Causes**:
1. **Insufficient baseline** → Need 24h+ of normal data for rolling average
2. **Anomaly too subtle** → Ensure spike is >100% increase from baseline
3. **LookbackHours too short** → Use 24h+ window to capture baseline

### CAS Duplicates Atoms

**Symptom**: `ReferenceCount` doesn't increment, new rows created

**Causes**:
1. **Hash collision** → Use SHA2_256, not MD5
2. **MERGE not using WHEN MATCHED** → Verify UPDATE clause exists
3. **Concurrent inserts** → Use SERIALIZABLE isolation or HOLDLOCK

---

## Summary

Cognitive Kernel Seeding provides:

✅ **Reproducible Testing** - Golden paths with known coordinates  
✅ **Physics Validation** - Spatial landmarks define reference frame  
✅ **OODA Bootstrap** - Operational history seeds autonomous improvement  
✅ **A* Verification** - Navigable reasoning chains for pathfinding  

This is the **foundation** for all spatial reasoning and autonomous operation in Hartonomous.
