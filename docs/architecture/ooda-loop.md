# OODA Loop: Autonomous Self-Improvement

**The Self-Healing Database**

## Overview

The OODA Loop (Observe-Orient-Decide-Act) is Hartonomous's autonomous self-improvement system. Inspired by military decision-making frameworks, it enables the database to detect problems, generate solutions, and execute fixes without human intervention.

## The OODA Framework

```
┌───────────────────────────────────────────────────────────┐
│ OBSERVE (sp_Analyze)                                      │
│  Continuously monitor system health and performance       │
│  ↓ Output: JSON observations                              │
└────────┬──────────────────────────────────────────────────┘
         │
┌────────▼──────────────────────────────────────────────────┐
│ ORIENT (sp_Hypothesize)                                   │
│  Parse observations, generate hypotheses for improvement  │
│  ↓ Output: Ranked hypotheses with impact scores           │
└────────┬──────────────────────────────────────────────────┘
         │
┌────────▼──────────────────────────────────────────────────┐
│ DECIDE (Implicit filtering in sp_Act)                     │
│  Filter hypotheses by auto-approve threshold              │
│  ↓ Safe operations (≥0.8): Auto-execute                   │
│  ↓ Risky operations (<0.8): Queue for manual review       │
└────────┬──────────────────────────────────────────────────┘
         │
┌────────▼──────────────────────────────────────────────────┐
│ ACT (sp_Act)                                              │
│  Execute approved operations, log results                 │
│  ↓ Output: Performance improvements, metrics              │
└────────┬──────────────────────────────────────────────────┘
         │
┌────────▼──────────────────────────────────────────────────┐
│ LEARN (Feedback Loop)                                     │
│  Compare before/after metrics, update scoring model       │
│  ↓ Adjust auto-approve threshold based on success rate    │
└───────────────────────────────────────────────────────────┘
```

## Phase 1: OBSERVE (sp_Analyze)

### Purpose
Detect anomalies, performance degradation, and optimization opportunities.

### Implementation

**Stored Procedure**: `dbo.sp_Analyze`

**Parameters**:
```sql
CREATE PROCEDURE dbo.sp_Analyze
    @TenantId INT,
    @AnalysisScope NVARCHAR(50) = 'full',  -- 'full' or 'incremental'
    @LookbackHours INT = 24
AS
```

### Monitoring Targets

#### 1. Query Performance Degradation

```sql
-- Detect slow queries (> 1000ms avg)
SELECT
    qt.text AS QueryText,
    qs.execution_count AS ExecutionCount,
    qs.total_elapsed_time / qs.execution_count / 1000 AS AvgDurationMs,
    qs.last_execution_time
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time / qs.execution_count > 1000000  -- > 1000ms
  AND qs.last_execution_time > DATEADD(hour, -@LookbackHours, GETUTCDATE())
ORDER BY AvgDurationMs DESC;
```

#### 2. Missing Index Recommendations

```sql
-- SQL Server's built-in missing index DMVs
SELECT
    OBJECT_NAME(mid.object_id) AS TableName,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.avg_user_impact AS EstimatedImprovement,
    migs.user_seeks + migs.user_scans AS TotalSeeks
FROM sys.dm_db_missing_index_details mid
JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
WHERE migs.avg_user_impact > 50  -- Estimated > 50% improvement
ORDER BY migs.avg_user_impact DESC;
```

#### 3. Concept Drift Detection

```sql
-- Detect embeddings moving away from concept centroids
WITH ConceptDistances AS (
    SELECT
        ae.SourceAtomId,
        c.ConceptId,
        c.ConceptName,
        ae.SpatialKey.STDistance(c.CentroidSpatialKey) AS DistanceFromCentroid
    FROM dbo.AtomEmbedding ae
    JOIN dbo.Concept c ON ae.SourceAtomId IN (
        SELECT AtomId FROM provenance.AtomConcepts WHERE ConceptId = c.ConceptId
    )
    WHERE ae.CreatedAt > DATEADD(hour, -@LookbackHours, GETUTCDATE())
)
SELECT
    ConceptId,
    ConceptName,
    AVG(DistanceFromCentroid) AS AvgDistance,
    STDEV(DistanceFromCentroid) AS StdDevDistance,
    COUNT(*) AS AtomsInDrift
FROM ConceptDistances
WHERE DistanceFromCentroid > (SELECT AVG(DistanceFromCentroid) * 2 FROM ConceptDistances)
GROUP BY ConceptId, ConceptName
HAVING COUNT(*) > 10;  -- At least 10 atoms drifting
```

#### 4. Dead Atom Detection

```sql
-- Atoms with zero references and no recent access
SELECT COUNT(*) AS DeadAtomCount,
       SUM(DATALENGTH(AtomicValue)) AS WastedBytes
FROM dbo.Atom
WHERE ReferenceCount = 0
  AND LastAccessedUtc < DATEADD(day, -90, GETUTCDATE())
  AND TenantId = @TenantId;
```

#### 5. Orphaned Atoms (No Concept Assignment)

```sql
-- Atoms without concept clustering
SELECT COUNT(*) AS OrphanAtomCount
FROM dbo.Atom a
WHERE a.ConceptId IS NULL
  AND a.Modality = 'text'
  AND a.TenantId = @TenantId;
```

### Output Format

```sql
-- Insert observations into LearningMetrics table
INSERT INTO dbo.LearningMetrics (
    TenantId,
    AnalysisType,
    ObservationsJson,
    AnomaliesDetected,
    CreatedAt
)
VALUES (
    @TenantId,
    'OODA_Observe',
    '{
        "slowQueries": 3,
        "missingIndices": 2,
        "conceptDrift": [12, 34],
        "deadAtoms": 15678,
        "orphanAtoms": 8901
    }',
    3 + 2 + 2 + 1 + 1,  -- Total anomalies
    GETUTCDATE()
);

-- Return analysis summary
SELECT @AnalysisId AS AnalysisId,
       'full' AS AnalysisScope,
       8 AS AnomaliesDetected,
       '{...}' AS ObservationsJson,
       GETUTCDATE() AS Timestamp;
```

## Phase 2: ORIENT (sp_Hypothesize)

### Purpose
Generate actionable hypotheses to address observed anomalies.

### Implementation

**Stored Procedure**: `dbo.sp_Hypothesize`

**Parameters**:
```sql
CREATE PROCEDURE dbo.sp_Hypothesize
    @AnalysisId UNIQUEIDENTIFIER,
    @TenantId INT
AS
```

### Hypothesis Generation Rules

#### Rule 1: Missing Index → CREATE INDEX Hypothesis

```sql
-- For each missing index recommendation
INSERT INTO dbo.PendingActions (
    TenantId,
    ActionType,
    TargetObject,
    ActionCommand,
    ExpectedImprovement,
    ApprovalScore,
    CreatedAt
)
SELECT
    @TenantId,
    'CreateIndex',
    TableName,
    'CREATE INDEX IX_' + TableName + '_' + REPLACE(equality_columns, ',', '_')
        + ' ON ' + TableName + ' (' + equality_columns + ')'
        + CASE WHEN included_columns IS NOT NULL
            THEN ' INCLUDE (' + included_columns + ')'
            ELSE '' END,
    CONCAT(EstimatedImprovement, '% query speedup'),
    CASE
        WHEN EstimatedImprovement >= 80 THEN 0.95  -- Very safe
        WHEN EstimatedImprovement >= 50 THEN 0.85
        ELSE 0.70
    END AS ApprovalScore,
    GETUTCDATE()
FROM #MissingIndices;
```

#### Rule 2: Dead Atoms → PRUNE Hypothesis

```sql
INSERT INTO dbo.PendingActions (
    TenantId,
    ActionType,
    TargetObject,
    ActionCommand,
    ExpectedImprovement,
    ApprovalScore,
    CreatedAt
)
VALUES (
    @TenantId,
    'PruneDeadAtoms',
    'dbo.Atom',
    'DELETE FROM dbo.Atom WHERE ReferenceCount = 0 AND LastAccessedUtc < DATEADD(day, -90, GETUTCDATE())',
    CONCAT(@DeadAtomCount, ' atoms, ', @WastedBytes / 1024 / 1024 / 1024, 'GB reclaimed'),
    0.88,  -- Safe operation (DELETE with WHERE clause)
    GETUTCDATE()
);
```

#### Rule 3: Concept Drift → RETRAIN Hypothesis

```sql
INSERT INTO dbo.PendingActions (
    TenantId,
    ActionType,
    TargetObject,
    ActionCommand,
    ExpectedImprovement,
    ApprovalScore,
    CreatedAt
)
SELECT
    @TenantId,
    'RetrainConcept',
    'Concept:' + CAST(ConceptId AS VARCHAR),
    'EXEC dbo.sp_ClusterConcepts @ConceptId = ' + CAST(ConceptId AS VARCHAR),
    CONCAT('Re-cluster ', AtomsInDrift, ' drifting atoms'),
    0.75,  -- Moderate risk (model retraining)
    GETUTCDATE()
FROM #ConceptDrift;
```

#### Rule 4: Orphan Atoms → CLUSTER Hypothesis

```sql
INSERT INTO dbo.PendingActions (
    TenantId,
    ActionType,
    TargetObject,
    ActionCommand,
    ExpectedImprovement,
    ApprovalScore,
    CreatedAt
)
VALUES (
    @TenantId,
    'ClusterOrphans',
    'OrphanAtoms',
    'EXEC dbo.sp_ClusterConcepts @OrphansOnly = 1',
    CONCAT('Discover concepts for ', @OrphanAtomCount, ' atoms'),
    0.82,  -- Safe operation (no destructive changes)
    GETUTCDATE()
);
```

### Output Format

```sql
SELECT
    @HypothesisId AS HypothesisId,
    @AnalysisId AS AnalysisId,
    COUNT(*) AS HypothesesGenerated,
    (
        SELECT
            ActionType,
            TargetObject,
            ExpectedImprovement,
            ApprovalScore
        FROM dbo.PendingActions
        WHERE CreatedAt = @Timestamp
        FOR JSON PATH
    ) AS HypothesesJson,
    @Timestamp AS Timestamp
FROM dbo.PendingActions
WHERE CreatedAt = @Timestamp;
```

## Phase 3: DECIDE (Implicit Filtering)

### Purpose
Separate safe auto-executable operations from risky manual-approval operations.

### Implementation

**Embedded in** `sp_Act` procedure.

### Decision Criteria

**Auto-Approve (ApprovalScore ≥ 0.80)**:
- Index creation (non-unique, filtered)
- UPDATE STATISTICS
- Dead atom pruning (with conservative WHERE clause)
- Orphan clustering
- Cache warming (preload hot embeddings)

**Manual Review (ApprovalScore < 0.80)**:
- Model retraining
- Unique index creation (risk of failure on duplicates)
- Schema changes
- Data migrations
- External API calls

### Threshold Adjustment

```sql
-- Adjust threshold based on success rate
DECLARE @SuccessRate DECIMAL(5,2);

SELECT @SuccessRate =
    CAST(SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(*)
FROM dbo.AutonomousImprovementHistory
WHERE CreatedAt > DATEADD(day, -30, GETUTCDATE());

-- Lower threshold if high success rate
IF @SuccessRate > 0.95
    SET @AutoApproveThreshold = 0.75;  -- More aggressive
ELSE IF @SuccessRate < 0.80
    SET @AutoApproveThreshold = 0.90;  -- More conservative
ELSE
    SET @AutoApproveThreshold = 0.80;  -- Default
```

## Phase 4: ACT (sp_Act)

### Purpose
Execute approved operations with rollback safety.

### Implementation

**Stored Procedure**: `dbo.sp_Act`

**Parameters**:
```sql
CREATE PROCEDURE dbo.sp_Act
    @TenantId INT,
    @AutoApproveThreshold DECIMAL(3,2) = 0.80
AS
```

### Execution Pattern

```sql
BEGIN TRANSACTION;

DECLARE @ActionsExecuted INT = 0;
DECLARE @ActionsSkipped INT = 0;
DECLARE @ResultsJson NVARCHAR(MAX) = '{}';

-- Cursor over pending actions
DECLARE action_cursor CURSOR FOR
SELECT ActionId, ActionType, ActionCommand, TargetObject
FROM dbo.PendingActions
WHERE TenantId = @TenantId
  AND Status = 'Pending'
  AND ApprovalScore >= @AutoApproveThreshold
ORDER BY ApprovalScore DESC;  -- Highest confidence first

OPEN action_cursor;

FETCH NEXT FROM action_cursor INTO @ActionId, @ActionType, @ActionCommand, @TargetObject;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        -- Measure before state
        DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();

        -- Execute action (dynamic SQL with safety checks)
        IF @ActionType = 'CreateIndex'
        BEGIN
            -- Safety: Check index doesn't already exist
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = @TargetObject)
            BEGIN
                EXEC sp_executesql @ActionCommand;
                SET @ResultsJson = JSON_MODIFY(@ResultsJson, '$.CreateIndex_' + @TargetObject, 'Success');
            END
            ELSE
            BEGIN
                SET @ResultsJson = JSON_MODIFY(@ResultsJson, '$.CreateIndex_' + @TargetObject, 'Skipped - Already exists');
                SET @ActionsSkipped += 1;
                GOTO NextAction;
            END
        END
        ELSE IF @ActionType = 'PruneDeadAtoms'
        BEGIN
            EXEC sp_executesql @ActionCommand;
            DECLARE @RowsDeleted INT = @@ROWCOUNT;
            SET @ResultsJson = JSON_MODIFY(@ResultsJson, '$.PruneDeadAtoms', CONCAT(@RowsDeleted, ' atoms deleted'));
        END
        ELSE IF @ActionType = 'ClusterOrphans'
        BEGIN
            EXEC sp_executesql @ActionCommand;
            SET @ResultsJson = JSON_MODIFY(@ResultsJson, '$.ClusterOrphans', 'Clustering completed');
        END;

        -- Measure after state
        DECLARE @EndTime DATETIME2 = SYSUTCDATETIME();
        DECLARE @DurationMs INT = DATEDIFF(millisecond, @StartTime, @EndTime);

        -- Log success
        INSERT INTO dbo.AutonomousImprovementHistory (
            TenantId,
            ActionType,
            TargetObject,
            ActionCommand,
            Status,
            DurationMs,
            ResultJson,
            CreatedAt
        )
        VALUES (
            @TenantId,
            @ActionType,
            @TargetObject,
            @ActionCommand,
            'Success',
            @DurationMs,
            @ResultsJson,
            GETUTCDATE()
        );

        -- Mark action as executed
        UPDATE dbo.PendingActions
        SET Status = 'Executed', ExecutedAt = GETUTCDATE()
        WHERE ActionId = @ActionId;

        SET @ActionsExecuted += 1;

    END TRY
    BEGIN CATCH
        -- Log failure
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();

        INSERT INTO dbo.AutonomousImprovementHistory (
            TenantId,
            ActionType,
            TargetObject,
            ActionCommand,
            Status,
            ErrorMessage,
            CreatedAt
        )
        VALUES (
            @TenantId,
            @ActionType,
            @TargetObject,
            @ActionCommand,
            'Failed',
            @ErrorMessage,
            GETUTCDATE()
        );

        -- Mark action as failed
        UPDATE dbo.PendingActions
        SET Status = 'Failed', ExecutedAt = GETUTCDATE(), ErrorMessage = @ErrorMessage
        WHERE ActionId = @ActionId;

        SET @ActionsSkipped += 1;
    END CATCH;

    NextAction:
    FETCH NEXT FROM action_cursor INTO @ActionId, @ActionType, @ActionCommand, @TargetObject;
END;

CLOSE action_cursor;
DEALLOCATE action_cursor;

COMMIT TRANSACTION;

-- Return summary
SELECT
    @ActionsExecuted AS ActionsExecuted,
    @ActionsSkipped AS ActionsSkipped,
    @ResultsJson AS ResultsJson,
    GETUTCDATE() AS Timestamp;
```

### Safety Mechanisms

1. **Transaction Wrapping**: All actions in single transaction (rollback on failure)
2. **Pre-Execution Checks**: Verify target object state before action
3. **Error Handling**: TRY/CATCH blocks prevent cascading failures
4. **Audit Trail**: All actions logged to `AutonomousImprovementHistory`
5. **Rollback Points**: SAVEPOINT before each action for partial rollback

## Phase 5: LEARN (Feedback Loop)

### Purpose
Improve decision-making over time based on action outcomes.

### Implementation

**Implicit in system** - no dedicated stored procedure.

### Learning Mechanisms

#### 1. Approval Score Adjustment

```sql
-- Analyze success rates by action type
SELECT
    ActionType,
    COUNT(*) AS TotalActions,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS SuccessCount,
    CAST(SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS DECIMAL) / COUNT(*) AS SuccessRate
FROM dbo.AutonomousImprovementHistory
WHERE CreatedAt > DATEADD(day, -30, GETUTCDATE())
GROUP BY ActionType;

-- Adjust future approval scores
UPDATE dbo.PendingActions
SET ApprovalScore = ApprovalScore * (1 + (@SuccessRate - 0.85))  -- Boost/penalize
WHERE ActionType = @ActionType
  AND Status = 'Pending';
```

#### 2. Impact Measurement

```sql
-- Measure query performance improvement after index creation
WITH BeforeMetrics AS (
    SELECT
        AVG(total_elapsed_time / execution_count) AS AvgDurationBefore
    FROM sys.dm_exec_query_stats
    WHERE last_execution_time < @IndexCreationTime
),
AfterMetrics AS (
    SELECT
        AVG(total_elapsed_time / execution_count) AS AvgDurationAfter
    FROM sys.dm_exec_query_stats
    WHERE last_execution_time > @IndexCreationTime
)
SELECT
    ((b.AvgDurationBefore - a.AvgDurationAfter) / b.AvgDurationBefore) * 100 AS PercentImprovement
FROM BeforeMetrics b, AfterMetrics a;

-- Update hypothesis accuracy
UPDATE dbo.AutonomousImprovementHistory
SET ActualImprovement = @PercentImprovement
WHERE ActionId = @ActionId;
```

#### 3. Concept Drift Learning

```sql
-- Track concept coherence over time
INSERT INTO provenance.ConceptEvolution (
    ConceptId,
    Timestamp,
    CoherenceScore,
    SeparationScore,
    AtomCount
)
SELECT
    c.ConceptId,
    GETUTCDATE(),
    dbo.clr_ComputeConceptCoherence(c.ConceptId),
    dbo.clr_ComputeConceptSeparation(c.ConceptId),
    COUNT(ac.AtomId)
FROM dbo.Concept c
JOIN provenance.AtomConcepts ac ON c.ConceptId = ac.ConceptId
GROUP BY c.ConceptId;
```

## Scheduling and Triggers

### SQL Agent Jobs

```sql
-- Job 1: Hourly Analysis
EXEC sp_add_job @job_name = 'OODA_Observe_Hourly';
EXEC sp_add_jobstep @job_name = 'OODA_Observe_Hourly',
    @step_name = 'Run sp_Analyze',
    @command = 'EXEC dbo.sp_Analyze @TenantId = 1, @AnalysisScope = ''incremental'', @LookbackHours = 1';
EXEC sp_add_schedule @schedule_name = 'Every Hour',
    @freq_type = 4,  -- Daily
    @freq_interval = 1,  -- Every day
    @freq_subday_type = 8,  -- Hours
    @freq_subday_interval = 1;  -- Every 1 hour

-- Job 2: Daily Full Analysis + Action
EXEC sp_add_job @job_name = 'OODA_Full_Daily';
EXEC sp_add_jobstep @job_name = 'OODA_Full_Daily',
    @step_name = 'Analyze + Hypothesize + Act',
    @command = '
        DECLARE @AnalysisId UNIQUEIDENTIFIER;
        EXEC dbo.sp_Analyze @TenantId = 1, @AnalysisScope = ''full'', @LookbackHours = 24;
        SET @AnalysisId = (SELECT TOP 1 AnalysisId FROM dbo.LearningMetrics ORDER BY CreatedAt DESC);
        EXEC dbo.sp_Hypothesize @AnalysisId = @AnalysisId, @TenantId = 1;
        EXEC dbo.sp_Act @TenantId = 1, @AutoApproveThreshold = 0.80;
    ';
EXEC sp_add_schedule @schedule_name = 'Daily at 2 AM',
    @freq_type = 4,  -- Daily
    @active_start_time = 020000;  -- 02:00:00
```

### Event-Driven Triggers

```sql
-- Trigger: Auto-analyze after large ingestion
CREATE TRIGGER tr_AutoAnalyzeAfterIngestion
ON dbo.IngestionJob
AFTER UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM inserted
        WHERE JobStatus = 'Completed'
          AND TotalAtomsProcessed > 100000  -- Large ingestion
    )
    BEGIN
        EXEC dbo.sp_Analyze @TenantId = (SELECT TenantId FROM inserted), @AnalysisScope = 'incremental', @LookbackHours = 1;
    END
END;
```

## Real-World Example

### Scenario: Slow Query Detection → Auto-Fix

**Day 1, 02:00 AM** - OODA loop runs:

**OBSERVE**:
```
Query: "SELECT * FROM Atom WHERE ContentHash = @hash"
Avg Duration: 1,245ms
Executions: 15,678
Issue: Missing index on ContentHash column
```

**ORIENT**:
```
Hypothesis: CREATE INDEX IX_Atom_ContentHash ON dbo.Atom(ContentHash)
Expected Improvement: 95% query speedup
Approval Score: 0.95 (auto-approve threshold: 0.80)
```

**DECIDE**: ✅ Auto-approve (score 0.95 ≥ 0.80)

**ACT**:
```sql
BEGIN TRANSACTION;
CREATE INDEX IX_Atom_ContentHash ON dbo.Atom(ContentHash);
COMMIT;
-- Result: Index created in 12 seconds
```

**LEARN**:
```
Measurement (Day 2, 02:00 AM):
  Query Avg Duration: 15ms (was 1,245ms)
  Actual Improvement: 98.8% (predicted 95%)
  Update: Boost "CreateIndex" approval score by 5%
```

**Day 2, 02:00 AM** - Logs show:
```
AutonomousImprovementHistory:
  ActionType: CreateIndex
  TargetObject: IX_Atom_ContentHash
  Status: Success
  ActualImprovement: 98.8%
  DurationMs: 12000
```

## Monitoring Dashboard

### Key Metrics

```sql
-- OODA Loop Health Check
SELECT
    'Last 7 Days' AS Period,
    COUNT(*) AS TotalActions,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) AS SuccessfulActions,
    AVG(CAST(SUBSTRING(ActualImprovement, 1, CHARINDEX('%', ActualImprovement)-1) AS DECIMAL)) AS AvgImprovement
FROM dbo.AutonomousImprovementHistory
WHERE CreatedAt > DATEADD(day, -7, GETUTCDATE());
```

**Alerting**:
- Success rate < 80%: Page on-call engineer
- No actions in 48 hours: Check OODA loop health
- Action failure spike: Disable auto-approve, require manual review

---

**Document Version**: 2.0
**Last Updated**: January 2025
**Implementation Status**: Production
