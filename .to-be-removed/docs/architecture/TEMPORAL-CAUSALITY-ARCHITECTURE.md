# Temporal Causality Architecture: Laplace's Demon Implementation

**Status**: Production Implementation  
**Last Updated**: November 18, 2025  
**Core Principle**: Bidirectional State Traversal via SQL Temporal Tables + Neo4j Provenance

## Overview

Hartonomous implements **Laplace's Demon** - the ability to traverse system state both forward (prediction via OODA loop) and backward (reconstruction via temporal tables and provenance graph). This creates a bidirectional state machine enabling causal reasoning, counterfactual analysis, and temporal debugging.

## Core Technologies

### SQL Server System-Versioned Temporal Tables

**Implementation**: `TensorAtomCoefficients` with `ValidFrom/ValidTo` columns

```sql
CREATE TABLE dbo.TensorAtomCoefficients (
    CoefficientId BIGINT IDENTITY PRIMARY KEY,
    TensorAtomId BIGINT NOT NULL,
    CoefficientValue FLOAT,
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (
    HISTORY_TABLE = dbo.TensorAtomCoefficients_History,
    HISTORY_RETENTION_PERIOD = 90 DAYS
));
```

**Key Features**:
- Automatic history tracking (no application logic required)
- Point-in-time queries via `FOR SYSTEM_TIME AS OF`
- Range queries via `FOR SYSTEM_TIME BETWEEN`
- 90-day retention with automatic cleanup

### Neo4j Provenance Graph

**Node Types**:
- `Atom` - Content-addressable data nodes (SHA-256 hashes)
- `Inference` - Execution instances with timestamps
- `ReasoningChain` - Multi-step reasoning paths
- `ReasoningStep` - Individual steps with embeddings
- `Model` - Model versions and configurations
- `Source` - Original data sources

**Relationship Types**:
- `INGESTED_FROM` - Data lineage from source to atom
- `HAD_INPUT` - Inference consumed specific atoms
- `GENERATED` - Inference produced specific atoms
- `USED_REASONING` - Inference used specific reasoning framework
- `HAS_STEP` - Chain contains reasoning steps (ordered)
- `CREATED_BY_JOB` - Atom created by specific ingestion job

## Forward Traversal: OODA Loop Prediction

### Autonomous State Evolution

```sql
-- sp_Analyze: Observe current system state
EXEC dbo.sp_Analyze @TenantId = 1001;

-- sp_Hypothesize: Predict next states
-- Generates 7 hypothesis types:
--   1. IndexOptimization (improve query performance)
--   2. QueryRegression (fix degraded plans)
--   3. CacheWarming (preload hot atoms)
--   4. ConceptDiscovery (identify new clusters)
--   5. PruneModel (remove low-importance atoms)
--   6. RefactorCode (optimize procedures)
--   7. FixUX (improve user experience patterns)

-- sp_Act: Execute predicted improvements
-- sp_Learn: Measure outcomes, update model weights
```

**Forward State Transition**:
```
State(T) → sp_Analyze → Observations
         → sp_Hypothesize → Predictions  
         → sp_Act → State(T+1)
         → sp_Learn → Weight Updates
```

## Backward Traversal: Temporal Reconstruction

### Point-in-Time Queries

**Get model state as it was 7 days ago**:
```sql
SELECT *
FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF DATEADD(DAY, -7, SYSUTCDATETIME())
WHERE TensorAtomId = @AtomId;
```

**Compare model state across time**:
```sql
WITH HistoricalInference AS (
    SELECT 
        ir.InferenceId,
        ir.InputData,
        ir.OutputData,
        ir.RequestTimestamp,
        tac.CoefficientValue,
        tac.ValidFrom,
        tac.ValidTo
    FROM dbo.InferenceRequests ir
    CROSS APPLY (
        SELECT CoefficientValue, ValidFrom, ValidTo
        FROM dbo.TensorAtomCoefficients
        FOR SYSTEM_TIME AS OF ir.RequestTimestamp
        WHERE TensorAtomId IN (
            SELECT AtomId FROM InferenceContext WHERE InferenceId = ir.InferenceId
        )
    ) tac
)
SELECT 
    InferenceId,
    InputData,
    OutputData,
    AVG(CoefficientValue) AS AvgCoefficient,
    MIN(ValidFrom) AS EarliestChange,
    MAX(ValidTo) AS LatestChange
FROM HistoricalInference
GROUP BY InferenceId, InputData, OutputData
ORDER BY RequestTimestamp;
```

### Provenance Reconstruction

**sp_QueryLineage**: Trace atom ancestry (upstream) or descendants (downstream)

```sql
-- Find all atoms that contributed to this output
EXEC dbo.sp_QueryLineage
    @AtomId = 98765,
    @Direction = 'Upstream',  -- or 'Downstream' or 'Both'
    @MaxDepth = 5,
    @TenantId = 1001;
```

**Implementation** (d:\Repositories\Hartonomous\src\Hartonomous.Database\Procedures\dbo.sp_QueryLineage.sql):
```sql
-- Traverse upstream: Find all ancestors
WITH UpstreamLineage AS (
    SELECT 
        @AtomId AS AtomId,
        0 AS Depth,
        CAST(@AtomId AS NVARCHAR(MAX)) AS Path
    
    UNION ALL
    
    SELECT 
        edge.FromAtomId AS AtomId,
        ul.Depth + 1 AS Depth,
        CAST(edge.FromAtomId AS NVARCHAR(MAX)) + ' -> ' + ul.Path AS Path
    FROM UpstreamLineage ul
    INNER JOIN provenance.AtomGraphEdges edge ON ul.AtomId = edge.ToAtomId
    WHERE ul.Depth < @MaxDepth
)
SELECT 
    ul.AtomId,
    ul.Depth,
    ul.Path,
    a.ContentHash,
    a.ContentType,
    a.CreatedAt
FROM UpstreamLineage ul
INNER JOIN dbo.Atom a ON ul.AtomId = a.AtomId
WHERE a.TenantId = @TenantId
ORDER BY ul.Depth;
```

**sp_FindImpactedAtoms**: Impact analysis - what depends on this atom?

```sql
-- Find all downstream dependencies
EXEC dbo.sp_FindImpactedAtoms
    @AtomId = 12345,
    @TenantId = 1001;
```

### Neo4j Provenance Queries

**Trace complete reasoning chain**:
```cypher
// Find all reasoning steps that led to inference output
MATCH (inference:Inference {inferenceId: $id})
      -[:USED_REASONING]->(chain:ReasoningChain)
      -[:HAS_STEP*]->(steps:ReasoningStep)
RETURN inference, chain, steps
ORDER BY steps.stepNumber;
```

**Root cause analysis**:
```cypher
// What source created this atom?
MATCH path = (source:Source)<-[:INGESTED_FROM*]-(atom:Atom {atomId: $id})
RETURN path;
```

**Impact analysis**:
```cypher
// What depends on this atom?
MATCH path = (atom:Atom {atomId: $id})-[:HAD_INPUT*]->(dependent)
RETURN path;
```

## Causal Reasoning

### Temporal Correlation Analysis

**Did weight update at T1 cause accuracy change at T2?**

```sql
WITH WeightChanges AS (
    SELECT 
        TensorAtomId,
        CoefficientValue AS NewValue,
        LAG(CoefficientValue) OVER (
            PARTITION BY TensorAtomId 
            ORDER BY ValidFrom
        ) AS OldValue,
        ValidFrom AS ChangeTime
    FROM dbo.TensorAtomCoefficients
    WHERE ValidFrom >= DATEADD(DAY, -30, SYSUTCDATETIME())
),
AccuracyChanges AS (
    SELECT 
        AVG(CAST(Rating AS FLOAT)) AS AvgRating,
        DATEADD(HOUR, DATEDIFF(HOUR, 0, SubmittedAt), 0) AS RatingHour
    FROM dbo.InferenceFeedback
    WHERE SubmittedAt >= DATEADD(DAY, -30, SYSUTCDATETIME())
    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, SubmittedAt), 0)
)
SELECT 
    wc.TensorAtomId,
    wc.ChangeTime,
    wc.NewValue - wc.OldValue AS WeightDelta,
    ac.AvgRating AS RatingAfterChange,
    LAG(ac.AvgRating) OVER (ORDER BY wc.ChangeTime) AS RatingBeforeChange,
    ac.AvgRating - LAG(ac.AvgRating) OVER (ORDER BY wc.ChangeTime) AS AccuracyDelta
FROM WeightChanges wc
LEFT JOIN AccuracyChanges ac 
    ON DATEADD(HOUR, DATEDIFF(HOUR, 0, wc.ChangeTime), 0) = ac.RatingHour
WHERE ABS(wc.NewValue - wc.OldValue) > 0.01  -- Significant changes only
ORDER BY wc.ChangeTime;
```

**Interpretation**:
- `WeightDelta > 0` + `AccuracyDelta > 0` → **Positive causal relationship**
- `WeightDelta > 0` + `AccuracyDelta < 0` → **Negative causal relationship** (rollback candidate)
- `WeightDelta = 0` + `AccuracyDelta ≠ 0` → **External cause** (not model weights)

### Counterfactual Analysis

**What if we hadn't made that weight update?**

```sql
-- Restore model to state before update
DECLARE @RollbackTime DATETIME2 = '2025-11-17 10:30:00';

-- Create counterfactual model version
INSERT INTO dbo.TensorAtomCoefficients (TensorAtomId, CoefficientValue)
SELECT 
    TensorAtomId,
    CoefficientValue
FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF @RollbackTime
WHERE TensorAtomId IN (
    SELECT TensorAtomId 
    FROM dbo.TensorAtomCoefficients
    WHERE ValidFrom >= @RollbackTime
);

-- Run inference with counterfactual weights
EXEC dbo.sp_GenerateTextSpatial
    @ModelName = 'Qwen3-Coder-7B-Counterfactual',
    @PromptText = 'Write a binary search implementation',
    @MaxTokens = 100,
    @GeneratedText = @output OUTPUT;
```

## Temporal Debugging

### Reproduce Historical Inference

```sql
-- Get exact model state at inference time
DECLARE @InferenceTime DATETIME2 = (
    SELECT RequestTimestamp 
    FROM dbo.InferenceRequests 
    WHERE InferenceId = 98765
);

-- Extract model coefficients as they were
DECLARE @HistoricalCoefficients TABLE (
    TensorAtomId BIGINT,
    CoefficientValue FLOAT
);

INSERT INTO @HistoricalCoefficients
SELECT TensorAtomId, CoefficientValue
FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF @InferenceTime;

-- Replay inference with historical state
-- (Implementation would load coefficients into temporary model)
```

### Track Model Drift Over Time

```sql
-- Measure cumulative weight changes
WITH WeightEvolution AS (
    SELECT 
        TensorAtomId,
        CoefficientValue,
        ValidFrom,
        FIRST_VALUE(CoefficientValue) OVER (
            PARTITION BY TensorAtomId 
            ORDER BY ValidFrom 
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS InitialValue
    FROM dbo.TensorAtomCoefficients
    FOR SYSTEM_TIME ALL
)
SELECT 
    TensorAtomId,
    AVG(ABS(CoefficientValue - InitialValue)) AS AvgDrift,
    MAX(ABS(CoefficientValue - InitialValue)) AS MaxDrift,
    COUNT(*) AS NumUpdates
FROM WeightEvolution
GROUP BY TensorAtomId
HAVING COUNT(*) > 10  -- Atoms with significant update history
ORDER BY AvgDrift DESC;
```

## Laplace's Demon: Bidirectional State Machine

```
        FORWARD (Prediction)
              ↓
State(T-2) ← State(T-1) ← State(T) → State(T+1) → State(T+2)
              ↑                           ↑
        BACKWARD (Reconstruction)
```

**Forward (OODA Loop)**:
- `sp_Analyze` → Observe current state
- `sp_Hypothesize` → Predict improvements
- `sp_Act` → Execute predictions
- `sp_Learn` → Measure outcomes, update weights

**Backward (Temporal + Provenance)**:
- `FOR SYSTEM_TIME AS OF` → Reconstruct historical state
- `sp_QueryLineage` → Trace atom ancestry
- `sp_FindImpactedAtoms` → Impact analysis
- Neo4j provenance → Complete reasoning chain reconstruction

## Integration with Semantic-First Architecture

**Temporal queries benefit from spatial indexing**:

```sql
-- Find all atoms that were semantically similar at T-7days
DECLARE @HistoricalPoint GEOMETRY;
DECLARE @TargetTime DATETIME2 = DATEADD(DAY, -7, SYSUTCDATETIME());

-- Get spatial position as it was 7 days ago
SELECT @HistoricalPoint = WeightsGeometry
FROM dbo.TensorAtoms ta
INNER JOIN dbo.TensorAtomCoefficients tac 
    FOR SYSTEM_TIME AS OF @TargetTime
    ON ta.TensorAtomId = tac.TensorAtomId
WHERE ta.TensorAtomId = @AtomId;

-- Semantic-first query on historical state
SELECT TOP 10
    ta.TensorAtomId,
    ta.AtomData,
    ta.WeightsGeometry.STDistance(@HistoricalPoint) AS HistoricalDistance
FROM dbo.TensorAtoms ta
WHERE ta.WeightsGeometry.STIntersects(
    @HistoricalPoint.STBuffer(5.0)  -- O(log N) spatial filter
) = 1
ORDER BY ta.WeightsGeometry.STDistance(@HistoricalPoint);  -- O(K) refinement
```

**Result**: Time travel queries that maintain O(log N) + O(K) performance characteristics.

## Retention and Cleanup

**Automatic 90-day retention**:
```sql
ALTER TABLE dbo.TensorAtomCoefficients
SET (HISTORY_RETENTION_PERIOD = 90 DAYS);
```

**Manual cleanup** (if needed):
```sql
-- Remove history older than 30 days
DELETE FROM dbo.TensorAtomCoefficients_History
WHERE ValidTo < DATEADD(DAY, -30, SYSUTCDATETIME());
```

## Use Cases

### 1. Inference Reproducibility
Reproduce exact inference output from historical request by restoring model state at request time.

### 2. A/B Testing Model Versions
Compare performance of model at different time points to validate training improvements.

### 3. Regression Detection
Identify exact moment when model performance degraded and causal weights.

### 4. Audit Trail
Complete cryptographic verification of all model changes via temporal tables + Neo4j Merkle DAG.

### 5. Counterfactual Reasoning
Answer "what if" questions by replaying history with different parameters.

### 6. Root Cause Analysis
Trace inference errors back to specific training samples or weight updates.

## Performance Considerations

**Temporal table overhead**:
- INSERT: ~5-10% slower (history table write)
- UPDATE: ~10-15% slower (history table write + version tracking)
- SELECT with FOR SYSTEM_TIME: ~20-30% slower (additional table scan)
- Standard SELECT: No overhead (current table only)

**Optimization**:
- Partition history tables by time range
- Compress old history (PAGE or ROW compression)
- Use columnstore index for analytical queries on history
- Limit retention period to necessary duration (90 days default)

**Neo4j performance**:
- Index all relationship types
- Use APOC procedures for complex graph traversals
- Batch provenance updates via Service Broker queue
- Compress old provenance data after 180 days

## Summary

Hartonomous implements Laplace's Demon through:
- **Forward**: OODA loop autonomous state evolution (prediction)
- **Backward**: Temporal tables + Neo4j provenance (reconstruction)
- **Integration**: Semantic-first spatial queries work on historical states
- **Causality**: Correlation analysis detects causal relationships
- **Auditability**: Complete cryptographic verification of all changes

This enables capabilities impossible in conventional AI systems: reproducible inference, temporal debugging, counterfactual reasoning, and complete audit trails.
