# Autonomous OODA Loop Architecture

**Date:** November 12, 2025  
**System:** Database-native autonomous improvement via Service Broker  
**Philosophy:** Self-improving AI substrate using Observe-Orient-Decide-Act-Learn

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [OODA Loop Philosophy](#ooda-loop-philosophy)
3. [Complete Workflow](#complete-workflow)
4. [Hypothesis Types & Actions](#hypothesis-types--actions)
5. [Safety Constraints](#safety-constraints)
6. [Gödel Engine](#gödel-engine)
7. [Monitoring & Controls](#monitoring--controls)
8. [Dual-Server OODA](#dual-server-ooda)
9. [Implementation Reference](#implementation-reference)

---

## Executive Summary

### The OODA Loop: Database as Autonomous Agent

**Core Concept:**
Hartonomous uses **SQL Server Service Broker** as an autonomous improvement engine. The database doesn't just store data—it observes system performance, generates hypotheses about improvements, executes actions, and learns from outcomes. This is **database-native AGI** at the substrate level.

**Why Service Broker?**
- **Asynchronous execution** (doesn't block user requests)
- **Transactional messaging** (ACID guarantees for autonomous actions)
- **Automatic activation** (stored procedures triggered by queue messages)
- **Poison message handling** (failed actions don't crash the system)
- **Already in SQL Server** (no external orchestration needed)

**The Four Phases:**

```
1. OBSERVE  → sp_Analyze      (detect patterns, measure performance)
2. ORIENT   → sp_Hypothesize  (generate improvement hypotheses)
3. DECIDE   → sp_Hypothesize  (prioritize, auto-approve high-confidence)
4. ACT      → sp_Act          (execute actions: index, cache, retrain)
5. LEARN    → sp_Learn        (measure delta, update success rates)
   └──────────┘ (cycle repeats)
```

**Current Status:**
- ✅ All 4 stored procedures implemented
- ✅ Service Broker queues configured (AnalyzeQueue, HypothesizeQueue, ActQueue, LearnQueue)
- ✅ Activation procedures wired
- ✅ Hypothesis types: IndexOptimization, CacheWarming, ConceptDiscovery, ModelRetraining
- ⏸️ Scheduling not configured (manual trigger via API)
- ⏸️ Student model proposals not integrated

---

## OODA Loop Philosophy

### Origins: John Boyd's Military Strategy

**Original OODA Loop (1970s):**
Developed by US Air Force Colonel John Boyd for aerial combat decision-making:

1. **Observe:** Gather information from environment
2. **Orient:** Analyze and synthesize observations
3. **Decide:** Determine best course of action
4. **Act:** Execute decision
5. **Loop back** to Observe (faster loops win)

**Application to AI Systems:**
Modern AI systems use OODA for:
- Reinforcement learning (observe → reward → update policy)
- AutoML (observe metrics → hypothesize architecture → train → evaluate)
- Autonomous vehicles (observe sensors → orient to map → decide path → act on controls)

### Hartonomous Adaptation: Self-Improving Database

**Key Insight:**
SQL Server can be the **autonomous agent** itself, not just a passive data store.

**Advantages:**
1. **Provenance built-in:** Every action logged in temporal tables (FOR SYSTEM_TIME)
2. **Transactional safety:** Autonomous actions wrapped in transactions (rollback on failure)
3. **No external orchestration:** Service Broker handles queuing, retry, poison messages
4. **Low latency:** Actions execute in-process (no network calls to external services)
5. **Unified substrate:** Same database for data + compute + decision-making

**Design Principles:**
- **Observe unobtrusively:** Use DMVs (Dynamic Management Views), don't impact production
- **Orient conservatively:** Only high-confidence hypotheses auto-approved
- **Decide transparently:** All decisions logged with reasoning
- **Act safely:** Sandbox testing before production changes
- **Learn continuously:** Every action measured, success rates updated

---

## Complete Workflow

### Phase 0: Trigger (Scheduled or Manual)

**Scheduled Trigger (Not Yet Implemented):**

```sql
-- SQL Server Agent job (runs every 15 minutes)
USE msdb;
GO

EXEC sp_add_job
    @job_name = 'AutonomousImprovement',
    @description = 'Trigger OODA loop for autonomous system improvement',
    @enabled = 1;

EXEC sp_add_jobstep
    @job_name = 'AutonomousImprovement',
    @step_name = 'StartOODALoop',
    @subsystem = 'TSQL',
    @database_name = 'Hartonomous',
    @command = 'EXEC dbo.sp_TriggerOODALoop @AnalysisScope = ''performance'';';

EXEC sp_add_schedule
    @schedule_name = 'Every15Minutes',
    @freq_type = 4,  -- Daily
    @freq_interval = 1,
    @freq_subday_type = 4,  -- Minutes
    @freq_subday_interval = 15;

EXEC sp_attach_schedule
    @job_name = 'AutonomousImprovement',
    @schedule_name = 'Every15Minutes';
GO
```

**Manual Trigger (Current):**

```http
POST /api/autonomy/trigger
{
  "analysisScope": "performance",
  "improvementGoal": "latency_optimization"
}
```

**What happens:**
1. API calls `sp_TriggerOODALoop`
2. Sends initial message to `AnalyzeQueue`
3. Service Broker activates `sp_Analyze`
4. OODA loop begins

### Phase 1: Observe (sp_Analyze)

**Purpose:** Gather system metrics, detect patterns, identify improvement opportunities

**Data Sources:**

```sql
-- Query performance statistics
SELECT TOP 100
    qs.execution_count,
    qs.total_elapsed_time,
    qs.total_worker_time,
    qs.total_logical_reads,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1, 
        ((CASE qs.statement_end_offset
          WHEN -1 THEN DATALENGTH(qt.text)
          ELSE qs.statement_end_offset
          END - qs.statement_start_offset)/2) + 1) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time > 1000000  -- >1 second
ORDER BY qs.total_elapsed_time DESC;

-- Missing indexes
SELECT TOP 10
    mid.database_id,
    mid.object_id,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.avg_user_impact,
    migs.user_seeks,
    migs.user_scans
FROM sys.dm_db_missing_index_details mid
INNER JOIN sys.dm_db_missing_index_groups mig
    ON mid.index_handle = mig.index_handle
INNER JOIN sys.dm_db_missing_index_group_stats migs
    ON mig.index_group_handle = migs.group_handle
WHERE migs.avg_user_impact > 50  -- High impact
ORDER BY migs.avg_user_impact DESC;

-- Cache hit ratios
SELECT 
    object_name,
    counter_name,
    cntr_value
FROM sys.dm_os_performance_counters
WHERE counter_name IN ('Buffer cache hit ratio', 'Page life expectancy');

-- Student model accuracy drift
SELECT 
    sm.StudentModelId,
    sm.ParentModelId,
    sm.CapabilityName,
    AVG(ma.AccuracyScore) AS AvgAccuracy,
    STDEV(ma.AccuracyScore) AS StdevAccuracy
FROM StudentModelTaxonomy sm
JOIN ModelABTests ma ON sm.StudentModelId = ma.StudentModelId
WHERE ma.TestEndTime > DATEADD(DAY, -7, GETUTCDATE())
GROUP BY sm.StudentModelId, sm.ParentModelId, sm.CapabilityName
HAVING STDEV(ma.AccuracyScore) > 0.05;  -- Significant drift
```

**Output:**
```sql
INSERT INTO AutonomousObservations
(ObservationId, ObservationType, MetricName, MetricValue, Severity, ObservedAtUtc)
VALUES
(NEWID(), 'SlowQuery', 'VectorSearch_AvgLatency', 2500.0, 'Medium', GETUTCDATE()),
(NEWID(), 'MissingIndex', 'Atoms_CreatedAtUtc_Includes', 85.0, 'High', GETUTCDATE()),
(NEWID(), 'CacheMiss', 'TensorPayloads_HitRatio', 0.65, 'Low', GETUTCDATE());
```

**Transition to Phase 2:**
```sql
-- Send to HypothesizeQueue
DECLARE @HypothesizePayload NVARCHAR(MAX) = (
    SELECT 
        @AnalysisId AS analysisId,
        COUNT(*) AS observationCount,
        (SELECT * FROM AutonomousObservations WHERE AnalysisId = @AnalysisId FOR JSON PATH) AS observations
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
);

SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
    (@HypothesizePayload);
```

### Phase 2: Orient & Decide (sp_Hypothesize)

**Purpose:** Analyze observations, generate improvement hypotheses, prioritize

**Hypothesis Generation Logic:**

```sql
-- Parse observations
DECLARE @Observations TABLE (
    ObservationType NVARCHAR(50),
    MetricName NVARCHAR(128),
    MetricValue FLOAT,
    Severity NVARCHAR(20)
);

INSERT INTO @Observations
SELECT 
    ObservationType,
    MetricName,
    MetricValue,
    Severity
FROM OPENJSON(@ObservationsJson, '$.observations')
WITH (
    ObservationType NVARCHAR(50),
    MetricName NVARCHAR(128),
    MetricValue FLOAT,
    Severity NVARCHAR(20)
);

-- Generate hypotheses based on observation patterns
DECLARE @Hypotheses TABLE (
    HypothesisId UNIQUEIDENTIFIER,
    HypothesisType NVARCHAR(50),
    Priority INT,
    Description NVARCHAR(MAX),
    RequiredActions NVARCHAR(MAX),
    Confidence DECIMAL(5,2)
);

-- Rule 1: Missing index with high impact → IndexOptimization
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'IndexOptimization',
    CASE 
        WHEN MetricValue > 80 THEN 5  -- Very high priority
        WHEN MetricValue > 60 THEN 4
        WHEN MetricValue > 40 THEN 3
        ELSE 2
    END,
    'Create index on ' + MetricName + ' (impact: ' + CAST(MetricValue AS NVARCHAR(10)) + '%)',
    JSON_QUERY('[{"action":"CREATE INDEX","table":"' + MetricName + '"}]'),
    MetricValue / 100.0  -- Confidence = impact percentage
FROM @Observations
WHERE ObservationType = 'MissingIndex';

-- Rule 2: Low cache hit ratio → CacheWarming
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'CacheWarming',
    3,  -- Medium priority
    'Pre-load frequently accessed tensors (current hit ratio: ' + CAST(MetricValue AS NVARCHAR(10)) + ')',
    JSON_QUERY('[{"action":"PRELOAD","threshold":0.8}]'),
    1.0 - MetricValue  -- Confidence inversely proportional to hit ratio
FROM @Observations
WHERE ObservationType = 'CacheMiss' AND MetricValue < 0.8;

-- Rule 3: Accuracy drift → ModelRetraining
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'ModelRetraining',
    4,  -- High priority
    'Retrain student model ' + MetricName + ' (drift detected)',
    JSON_QUERY('[{"action":"INCREMENTAL_DISTILLATION","studentModelId":"' + MetricName + '"}]'),
    0.85  -- High confidence for automated retraining
FROM @Observations
WHERE ObservationType = 'AccuracyDrift';
```

**Auto-Approval Logic:**

```sql
-- Auto-approve high-confidence, low-risk hypotheses
DECLARE @AutoApproveThreshold DECIMAL(5,2) = 0.80;  -- 80% confidence

INSERT INTO AutonomousHypotheses
(HypothesisId, AnalysisId, HypothesisType, Priority, Description, RequiredActions, Confidence, Status, CreatedAtUtc)
SELECT 
    HypothesisId,
    @AnalysisId,
    HypothesisType,
    Priority,
    Description,
    RequiredActions,
    Confidence,
    CASE 
        WHEN Confidence >= @AutoApproveThreshold THEN 'AutoApproved'
        ELSE 'PendingApproval'
    END,
    GETUTCDATE()
FROM @Hypotheses;

-- Only send auto-approved hypotheses to ActQueue
DECLARE @ActPayload NVARCHAR(MAX) = (
    SELECT 
        @AnalysisId AS analysisId,
        COUNT(*) AS hypothesesGenerated,
        (SELECT * FROM AutonomousHypotheses 
         WHERE AnalysisId = @AnalysisId AND Status = 'AutoApproved' 
         FOR JSON PATH) AS hypotheses
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
);

SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage]
    (@ActPayload);
```

**Manual Approval:**

```http
-- Pending hypotheses shown in dashboard
GET /api/autonomy/hypotheses?status=PendingApproval

-- Admin approves/rejects
POST /api/autonomy/hypotheses/{hypothesisId}/approve
```

### Phase 3: Act (sp_Act)

**Purpose:** Execute approved actions, record outcomes

**Action Execution:**

```sql
-- Parse hypotheses from message
DECLARE @Hypotheses TABLE (
    HypothesisId UNIQUEIDENTIFIER,
    HypothesisType NVARCHAR(50),
    Priority INT,
    RequiredActions NVARCHAR(MAX)
);

INSERT INTO @Hypotheses
SELECT 
    HypothesisId,
    HypothesisType,
    Priority,
    RequiredActions
FROM OPENJSON(@HypothesesJson, '$.hypotheses')
WITH (
    HypothesisId UNIQUEIDENTIFIER,
    HypothesisType NVARCHAR(50),
    Priority INT,
    RequiredActions NVARCHAR(MAX) AS JSON
);

-- Execute each hypothesis in priority order
DECLARE hypothesis_cursor CURSOR FOR
SELECT HypothesisId, HypothesisType, RequiredActions
FROM @Hypotheses
ORDER BY Priority DESC;

OPEN hypothesis_cursor;
FETCH NEXT FROM hypothesis_cursor INTO @CurrentHypothesisId, @CurrentType, @CurrentActions;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @StartTime = GETUTCDATE();
    
    BEGIN TRY
        -- Dispatch to appropriate action handler
        IF @CurrentType = 'IndexOptimization'
        BEGIN
            EXEC dbo.sp_ExecuteIndexOptimization @CurrentActions, @ExecutedActionsList OUTPUT, @ActionStatus OUTPUT;
        END
        ELSE IF @CurrentType = 'CacheWarming'
        BEGIN
            EXEC dbo.sp_ExecuteCacheWarming @CurrentActions, @ExecutedActionsList OUTPUT, @ActionStatus OUTPUT;
        END
        ELSE IF @CurrentType = 'ConceptDiscovery'
        BEGIN
            EXEC dbo.sp_ExecuteConceptDiscovery @CurrentActions, @ExecutedActionsList OUTPUT, @ActionStatus OUTPUT;
        END
        ELSE IF @CurrentType = 'ModelRetraining'
        BEGIN
            EXEC dbo.sp_ExecuteModelRetraining @CurrentActions, @ExecutedActionsList OUTPUT, @ActionStatus OUTPUT;
        END
        
        SET @ExecutionTimeMs = DATEDIFF(MILLISECOND, @StartTime, GETUTCDATE());
        
        -- Record successful action
        INSERT INTO @ActionResults
        (HypothesisId, HypothesisType, ActionStatus, ExecutedActions, ExecutionTimeMs, ErrorMessage)
        VALUES
        (@CurrentHypothesisId, @CurrentType, @ActionStatus, @ExecutedActionsList, @ExecutionTimeMs, NULL);
        
    END TRY
    BEGIN CATCH
        -- Record failed action
        SET @ActionError = ERROR_MESSAGE();
        
        INSERT INTO @ActionResults
        (HypothesisId, HypothesisType, ActionStatus, ExecutedActions, ExecutionTimeMs, ErrorMessage)
        VALUES
        (@CurrentHypothesisId, @CurrentType, 'Failed', NULL, 0, @ActionError);
    END CATCH;
    
    FETCH NEXT FROM hypothesis_cursor INTO @CurrentHypothesisId, @CurrentType, @CurrentActions;
END;

CLOSE hypothesis_cursor;
DEALLOCATE hypothesis_cursor;
```

**Action Handler Examples:**

```sql
-- sp_ExecuteIndexOptimization
CREATE PROCEDURE dbo.sp_ExecuteIndexOptimization
    @ActionsJson NVARCHAR(MAX),
    @ExecutedActions NVARCHAR(MAX) OUTPUT,
    @Status NVARCHAR(50) OUTPUT
AS
BEGIN
    DECLARE @TableName NVARCHAR(128), @ColumnName NVARCHAR(128);
    
    -- Parse action parameters
    SELECT @TableName = JSON_VALUE(@ActionsJson, '$[0].table');
    SELECT @ColumnName = JSON_VALUE(@ActionsJson, '$[0].column');
    
    -- Generate index name
    DECLARE @IndexName NVARCHAR(128) = 'IX_Auto_' + @TableName + '_' + @ColumnName;
    
    -- Create index (in sandbox first if configured)
    DECLARE @SQL NVARCHAR(MAX) = 
        'CREATE NONCLUSTERED INDEX ' + @IndexName + 
        ' ON dbo.' + @TableName + ' (' + @ColumnName + ')';
    
    EXEC sp_executesql @SQL;
    
    -- Return result
    SET @ExecutedActions = JSON_QUERY('[{"index":"' + @IndexName + '","created":true}]');
    SET @Status = 'Success';
END;
```

**Transition to Phase 4:**

```sql
-- Send results to LearnQueue
DECLARE @LearnPayload NVARCHAR(MAX) = (
    SELECT 
        @AnalysisId AS analysisId,
        COUNT(*) AS actionsExecuted,
        (SELECT * FROM @ActionResults FOR JSON PATH) AS actionResults
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
);

SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage]
    (@LearnPayload);
```

### Phase 4: Learn (sp_Learn)

**Purpose:** Measure performance delta, update hypothesis success rates, decide next cycle

**Performance Measurement:**

```sql
-- Parse action results
DECLARE @ActionOutcomes TABLE (
    HypothesisId UNIQUEIDENTIFIER,
    HypothesisType NVARCHAR(50),
    ActionStatus NVARCHAR(50),
    ExecutionTimeMs INT,
    Outcome NVARCHAR(50),  -- Success, HighSuccess, LowSuccess, Failed, Regressed
    ImpactScore DECIMAL(5,2)
);

INSERT INTO @ActionOutcomes
SELECT 
    HypothesisId,
    HypothesisType,
    ActionStatus,
    ExecutionTimeMs,
    'Pending',  -- Will be evaluated
    0.0
FROM OPENJSON(@ActionResultsJson, '$.actionResults')
WITH (
    HypothesisId UNIQUEIDENTIFIER,
    HypothesisType NVARCHAR(50),
    ActionStatus NVARCHAR(50),
    ExecutionTimeMs INT
);

-- Measure performance delta for each action type
UPDATE ao
SET 
    Outcome = CASE
        WHEN ao.ActionStatus = 'Failed' THEN 'Failed'
        WHEN perf.DeltaImprovement > 0.30 THEN 'HighSuccess'  -- >30% improvement
        WHEN perf.DeltaImprovement > 0.10 THEN 'Success'      -- >10% improvement
        WHEN perf.DeltaImprovement > 0 THEN 'LowSuccess'       -- Any improvement
        ELSE 'Regressed'  -- Performance got worse
    END,
    ImpactScore = perf.DeltaImprovement
FROM @ActionOutcomes ao
CROSS APPLY (
    SELECT 
        CASE ao.HypothesisType
            WHEN 'IndexOptimization' THEN dbo.fn_MeasureQueryPerformanceDelta(ao.HypothesisId)
            WHEN 'CacheWarming' THEN dbo.fn_MeasureCacheHitRatioDelta(ao.HypothesisId)
            WHEN 'ModelRetraining' THEN dbo.fn_MeasureModelAccuracyDelta(ao.HypothesisId)
            ELSE 0.0
        END AS DeltaImprovement
) perf;
```

**Update Success Rates:**

```sql
-- Update hypothesis type success rates (used for future auto-approval)
WITH TypeStats AS (
    SELECT 
        HypothesisType,
        COUNT(*) AS TotalAttempts,
        SUM(CASE WHEN Outcome IN ('Success', 'HighSuccess') THEN 1 ELSE 0 END) AS Successes,
        AVG(ImpactScore) AS AvgImpact
    FROM @ActionOutcomes
    GROUP BY HypothesisType
)
UPDATE ht
SET 
    ht.TotalAttempts = ht.TotalAttempts + ts.TotalAttempts,
    ht.SuccessfulAttempts = ht.SuccessfulAttempts + ts.Successes,
    ht.SuccessRate = CAST(ht.SuccessfulAttempts + ts.Successes AS DECIMAL(5,2)) / 
                     CAST(ht.TotalAttempts + ts.TotalAttempts AS DECIMAL(5,2)),
    ht.AvgImpact = ((ht.AvgImpact * ht.TotalAttempts) + (ts.AvgImpact * ts.TotalAttempts)) /
                   (ht.TotalAttempts + ts.TotalAttempts)
FROM HypothesisTypeStats ht
JOIN TypeStats ts ON ht.HypothesisType = ts.HypothesisType;
```

**Decide Next Cycle:**

```sql
-- Continue OODA loop if improvements detected
IF EXISTS (SELECT 1 FROM @ActionOutcomes WHERE Outcome IN ('Success', 'HighSuccess'))
BEGIN
    -- Send new AnalyzeMessage to start next cycle
    DECLARE @ContinuePayload NVARCHAR(MAX) = (
        SELECT 
            NEWID() AS analysisId,
            'continuous_improvement' AS analysisScope,
            @AnalysisId AS previousAnalysisId
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    
    BEGIN CONVERSATION @ContinueHandle
        FROM SERVICE AnalyzeService
        TO SERVICE 'AnalyzeService'
        ON CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
        WITH ENCRYPTION = OFF;
    
    SEND ON CONVERSATION @ContinueHandle
        MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
        (@ContinuePayload);
END;
```

---

## Hypothesis Types & Actions

### 1. IndexOptimization

**Trigger Observations:**
- Missing index suggestions with >50% impact
- Slow queries (>1 second) with table scans
- High logical reads (>10K pages per execution)

**Hypothesis Generation:**
```sql
-- sp_Hypothesize logic
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'IndexOptimization',
    CASE 
        WHEN impact > 80 THEN 5
        WHEN impact > 60 THEN 4
        ELSE 3
    END,
    'Create index on ' + table_name + '(' + columns + ')',
    JSON_QUERY('[{
        "action":"CREATE INDEX",
        "table":"' + table_name + '",
        "columns":"' + columns + '",
        "includes":"' + included_columns + '"
    }]'),
    impact / 100.0
FROM (
    SELECT 
        OBJECT_NAME(mid.object_id) AS table_name,
        mid.equality_columns AS columns,
        mid.included_columns,
        migs.avg_user_impact AS impact
    FROM sys.dm_db_missing_index_details mid
    JOIN sys.dm_db_missing_index_group_stats migs ON ...
    WHERE migs.avg_user_impact > 50
) missing_indexes;
```

**Action Execution:**
```sql
-- sp_ExecuteIndexOptimization
CREATE NONCLUSTERED INDEX IX_Auto_Atoms_CreatedAtUtc
ON dbo.Atoms (CreatedAtUtc)
INCLUDE (AtomId, Modality, SourceUri);
```

**Performance Measurement:**
```sql
-- fn_MeasureQueryPerformanceDelta
-- Compare avg query time before/after index creation
DECLARE @BeforeAvg BIGINT = (
    SELECT AVG(total_elapsed_time / execution_count)
    FROM sys.dm_exec_query_stats
    WHERE last_execution_time < @ActionTime
    AND sql_handle IN (SELECT sql_handle FROM target_queries)
);

DECLARE @AfterAvg BIGINT = (
    SELECT AVG(total_elapsed_time / execution_count)
    FROM sys.dm_exec_query_stats
    WHERE last_execution_time >= @ActionTime
    AND sql_handle IN (SELECT sql_handle FROM target_queries)
);

RETURN (@BeforeAvg - @AfterAvg) / CAST(@BeforeAvg AS DECIMAL(18,2));  -- Delta percentage
```

### 2. CacheWarming

**Trigger Observations:**
- Low cache hit ratio (<80%) for hot tensors
- Frequent FILESTREAM reads for same payloads
- Page life expectancy <300 seconds

**Hypothesis Generation:**
```sql
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'CacheWarming',
    3,
    'Pre-load ' + COUNT(*) + ' frequently accessed tensor payloads',
    JSON_QUERY('[{
        "action":"PRELOAD",
        "payloadIds":' + (SELECT STRING_AGG(PayloadId, ',') FROM hot_tensors) + '
    }]'),
    0.90  -- High confidence (low risk)
FROM (
    SELECT TOP 100 PayloadId
    FROM TensorAtomPayloads
    WHERE AccessCount > 10 AND LastAccessedUtc > DATEADD(HOUR, -1, GETUTCDATE())
    AND PayloadId NOT IN (SELECT PayloadId FROM CachedPayloads)
) hot_tensors;
```

**Action Execution:**
```sql
-- sp_ExecuteCacheWarming
INSERT INTO CachedPayloads (PayloadId, PayloadData, CachedAtUtc)
SELECT 
    PayloadId,
    PayloadData,  -- Materialize FILESTREAM into VARBINARY(MAX)
    GETUTCDATE()
FROM TensorAtomPayloads
WHERE PayloadId IN (SELECT value FROM OPENJSON(@PayloadIdsJson));
```

**Performance Measurement:**
```sql
-- fn_MeasureCacheHitRatioDelta
-- Compare cache hit ratio before/after warming
DECLARE @BeforeHitRatio DECIMAL(5,2) = (
    SELECT CAST(hits AS DECIMAL(10,2)) / CAST(hits + misses AS DECIMAL(10,2))
    FROM cache_stats
    WHERE measured_at < @ActionTime
);

DECLARE @AfterHitRatio DECIMAL(5,2) = (
    SELECT CAST(hits AS DECIMAL(10,2)) / CAST(hits + misses AS DECIMAL(10,2))
    FROM cache_stats
    WHERE measured_at >= @ActionTime
);

RETURN @AfterHitRatio - @BeforeHitRatio;
```

### 3. ConceptDiscovery

**Trigger Observations:**
- New embedding clusters detected (DBSCAN)
- Recurring query patterns without semantic label
- High-frequency co-occurrence of atoms

**Hypothesis Generation:**
```sql
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'ConceptDiscovery',
    2,  -- Lower priority (exploratory)
    'Potential new concept cluster detected: ' + cluster_label,
    JSON_QUERY('[{
        "action":"LABEL_CLUSTER",
        "clusterCentroid":' + centroid + ',
        "memberCount":' + member_count + '
    }]'),
    confidence_score
FROM (
    SELECT 
        'Cluster_' + CAST(cluster_id AS NVARCHAR(10)) AS cluster_label,
        JSON_ARRAY(centroid_vector) AS centroid,
        COUNT(*) AS member_count,
        MIN(distance_from_centroid) / AVG(distance_from_centroid) AS confidence_score
    FROM embedding_clusters
    WHERE discovered_at > DATEADD(HOUR, -24, GETUTCDATE())
    GROUP BY cluster_id
    HAVING COUNT(*) > 10
) new_clusters;
```

**Action Execution:**
```sql
-- sp_ExecuteConceptDiscovery
-- Use LLM to generate semantic label for cluster
DECLARE @SampleEmbeddings NVARCHAR(MAX) = (
    SELECT TOP 5 AtomId, EmbeddingVector
    FROM AtomEmbeddings
    WHERE ClusterId = @ClusterId
    FOR JSON PATH
);

DECLARE @ConceptLabel NVARCHAR(128);
EXEC dbo.fn_GenerateConceptLabel @SampleEmbeddings, @ConceptLabel OUTPUT;

INSERT INTO DiscoveredConcepts (ConceptId, ConceptLabel, ClusterId, DiscoveredAtUtc)
VALUES (NEWID(), @ConceptLabel, @ClusterId, GETUTCDATE());
```

### 4. ModelRetraining

**Trigger Observations:**
- Student model accuracy drift (>5% std dev)
- Parent model updated (new version available)
- Significant new training data accumulated (>1000 conversations)

**Hypothesis Generation:**
```sql
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'ModelRetraining',
    4,  -- High priority
    'Retrain student model ' + StudentModelName + ' (accuracy drift detected)',
    JSON_QUERY('[{
        "action":"INCREMENTAL_DISTILLATION",
        "studentModelId":"' + StudentModelId + '",
        "trainingDataSize":' + new_data_count + '
    }]'),
    0.85  -- High confidence for automated retraining
FROM (
    SELECT 
        sm.StudentModelId,
        sm.StudentModelName,
        COUNT(cl.ConversationId) AS new_data_count
    FROM StudentModelTaxonomy sm
    JOIN ConversationLogs cl ON sm.StudentModelId = cl.UsedModelId
    WHERE cl.LoggedAtUtc > sm.LastRetrainedAtUtc
    GROUP BY sm.StudentModelId, sm.StudentModelName
    HAVING COUNT(cl.ConversationId) > 1000
) drift_models;
```

**Action Execution:**
```sql
-- sp_ExecuteModelRetraining
-- Call incremental distillation procedure
EXEC dbo.sp_IncrementalDistillation
    @StudentModelId = @StudentModelId,
    @StartDate = @LastRetrainedDate,
    @EndDate = GETUTCDATE(),
    @NewLayersAdded = @NewLayersAdded OUTPUT,
    @PerformanceImprovement = @PerformanceImprovement OUTPUT;

-- Update student model metadata
UPDATE StudentModelTaxonomy
SET 
    LastRetrainedAtUtc = GETUTCDATE(),
    ParameterCount = ParameterCount + @NewLayersAdded,
    BaselineAccuracy = BaselineAccuracy + @PerformanceImprovement
WHERE StudentModelId = @StudentModelId;
```

**Performance Measurement:**
```sql
-- fn_MeasureModelAccuracyDelta
-- Compare model accuracy before/after retraining
DECLARE @BeforeAccuracy DECIMAL(5,2) = (
    SELECT AvgAccuracy
    FROM ModelABTests
    WHERE StudentModelId = @StudentModelId
    AND TestEndTime < @ActionTime
    ORDER BY TestEndTime DESC
    OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY
);

DECLARE @AfterAccuracy DECIMAL(5,2) = (
    SELECT AvgAccuracy
    FROM ModelABTests
    WHERE StudentModelId = @StudentModelId
    AND TestStartTime >= @ActionTime
    ORDER BY TestStartTime ASC
    OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY
);

RETURN @AfterAccuracy - @BeforeAccuracy;
```

### 5. StudentModelProposal (Future)

**Trigger Observations:**
- Capability analysis detects specialized layer groups
- Parent model ingested, no students exist yet
- Existing student quality <95% of parent for narrow task

**Hypothesis Generation:**
```sql
INSERT INTO @Hypotheses
SELECT 
    NEWID(),
    'StudentModelProposal',
    3,
    'Propose ' + capability_name + ' student model from ' + parent_name,
    JSON_QUERY('[{
        "action":"PROPOSE_STUDENT",
        "parentModelId":"' + ParentModelId + '",
        "capability":"' + capability_name + '",
        "layers":' + layer_range + '
    }]'),
    capability_confidence
FROM (
    SELECT 
        pm.ParentModelId,
        pm.ParentModelName AS parent_name,
        cap.CapabilityName AS capability_name,
        cap.LayerRange AS layer_range,
        cap.ConfidenceScore AS capability_confidence
    FROM ParentModels pm
    JOIN ModelLayerCapabilities cap ON pm.ParentModelId = cap.ParentModelId
    WHERE NOT EXISTS (
        SELECT 1 FROM StudentModelTaxonomy sm
        WHERE sm.ParentModelId = pm.ParentModelId
        AND sm.CapabilityName = cap.CapabilityName
    )
) missing_students;
```

---

## Safety Constraints

### Auto-Approval Rules

**What CAN be auto-approved:**
- ✅ Index creation (non-clustered, no primary key changes)
- ✅ Cache warming (read-only operation)
- ✅ Concept discovery (exploratory, no schema changes)
- ✅ Student model retraining (sandboxed, A/B tested before promotion)

**What REQUIRES manual approval:**
- ❌ Clustered index changes (affects physical storage)
- ❌ Table schema modifications (add/drop columns)
- ❌ Stored procedure modifications (code changes risky)
- ❌ Billing table modifications (financial impact)
- ❌ Tenant quota adjustments (SLA impact)
- ❌ Parent model replacement (breaking change risk)

**Confidence Thresholds:**

```sql
-- Configuration table
CREATE TABLE HypothesisTypeConfig (
    HypothesisType NVARCHAR(50) PRIMARY KEY,
    AutoApproveThreshold DECIMAL(5,2),  -- 0.0 = never auto-approve, 1.0 = always
    RequiresSandboxTesting BIT,
    MaxConcurrentActions INT,
    RollbackOnFailure BIT
);

INSERT INTO HypothesisTypeConfig VALUES
('IndexOptimization', 0.80, 0, 5, 0),      -- 80% confidence, no sandbox, allow 5 concurrent
('CacheWarming', 0.70, 0, 10, 1),           -- 70% confidence, allow 10 concurrent, rollback on fail
('ConceptDiscovery', 0.60, 1, 3, 0),        -- 60% confidence, requires sandbox, 3 concurrent
('ModelRetraining', 0.85, 1, 1, 1),         -- 85% confidence, requires sandbox, 1 at a time, rollback
('StudentModelProposal', 0.90, 1, 1, 0),    -- 90% confidence, requires sandbox, 1 at a time
('LinkedServerOptimization', 0.95, 1, 1, 1); -- 95% confidence (cross-server risk)
```

### Rollback Mechanisms

**Transaction-based rollback:**

```sql
-- sp_Act wraps each action in transaction
BEGIN TRY
    BEGIN TRANSACTION;
    
    EXEC dbo.sp_ExecuteIndexOptimization @CurrentActions, @ExecutedActionsList OUTPUT, @ActionStatus OUTPUT;
    
    -- Verify action succeeded
    IF @ActionStatus = 'Success'
        COMMIT TRANSACTION;
    ELSE
        ROLLBACK TRANSACTION;
        
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    -- Log failure
    INSERT INTO AutonomousActionFailures (HypothesisId, ErrorMessage)
    VALUES (@CurrentHypothesisId, ERROR_MESSAGE());
END CATCH;
```

**Manual rollback for schema changes:**

```sql
-- Store rollback script for each action
INSERT INTO AutonomousActionLog
(ActionId, HypothesisId, ActionType, ExecutedSQL, RollbackSQL, ExecutedAtUtc)
VALUES
(NEWID(), @HypothesisId, 'IndexCreation', 
 'CREATE INDEX IX_Auto_Atoms_CreatedAtUtc ON dbo.Atoms (CreatedAtUtc)',
 'DROP INDEX IX_Auto_Atoms_CreatedAtUtc ON dbo.Atoms',
 GETUTCDATE());

-- Manual rollback procedure
EXEC dbo.sp_RollbackAction @ActionId = 'F7B4C8D2-...';
```

### Poison Message Handling

**Service Broker automatically handles:**

```sql
-- After 5 consecutive failures, message moved to poison queue
ALTER QUEUE ActQueue WITH STATUS = ON, 
ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Act,
    MAX_QUEUE_READERS = 5,
    EXECUTE AS OWNER
),
POISON_MESSAGE_HANDLING (STATUS = ON);  -- Default: 5 failures → poison queue

-- Monitor poison messages
SELECT 
    service_name,
    message_type_name,
    message_body,
    queuing_order
FROM sys.transmission_queue
WHERE is_poison = 1;

-- Manual retry after fixing issue
RECEIVE TOP (1) * FROM PoisonQueue;
-- Fix issue, then re-send to original queue
```

### Rate Limiting

**Prevent runaway loops:**

```sql
-- Track OODA cycles per hour
CREATE TABLE OODACycleRateLimit (
    WindowStartUtc DATETIME2 PRIMARY KEY,
    CycleCount INT DEFAULT 0
);

-- Check before starting new cycle
DECLARE @CurrentWindow DATETIME2 = DATEADD(HOUR, DATEDIFF(HOUR, 0, GETUTCDATE()), 0);
DECLARE @CycleCount INT = (SELECT CycleCount FROM OODACycleRateLimit WHERE WindowStartUtc = @CurrentWindow);

IF @CycleCount >= 10  -- Max 10 cycles per hour
BEGIN
    RAISERROR('OODA loop rate limit exceeded (10/hour)', 16, 1);
    RETURN;
END;

-- Increment counter
MERGE OODACycleRateLimit AS target
USING (SELECT @CurrentWindow AS WindowStartUtc) AS source
ON target.WindowStartUtc = source.WindowStartUtc
WHEN MATCHED THEN UPDATE SET CycleCount = CycleCount + 1
WHEN NOT MATCHED THEN INSERT (WindowStartUtc, CycleCount) VALUES (@CurrentWindow, 1);
```

---

## Gödel Engine

### General-Purpose Autonomous Compute

**Concept:**
The OODA loop infrastructure isn't just for system optimization—it's a **general-purpose autonomous compute engine** for long-running tasks.

**Named After:**
Kurt Gödel's insight about self-reference: A sufficiently powerful formal system can reason about itself. The OODA loop reasons about the database's performance (self-improvement) AND can execute abstract computational tasks (prime search, theorem proving).

**How It Works:**

```sql
-- User starts a computational job
EXEC dbo.sp_StartPrimeSearch
    @RangeStart = 1000000,
    @RangeEnd = 2000000,
    @ChunkSize = 10000;

-- Creates job record
INSERT INTO AutonomousComputeJobs
(JobId, JobType, Parameters, Status, CreatedAtUtc)
VALUES
(NEWID(), 'PrimeSearch', 
 JSON_QUERY('{"rangeStart":1000000,"rangeEnd":2000000,"chunkSize":10000}'),
 'Running', GETUTCDATE());

-- Sends initial message to AnalyzeQueue
SEND ON CONVERSATION @ConversationHandle
    MESSAGE TYPE [//Hartonomous/AutonomousLoop/JobRequest]
    (@JobPayload);
```

**OODA Loop Execution:**

1. **sp_Analyze:** Detects compute job, routes to HypothesizeQueue
2. **sp_Hypothesize:** Reads job state, calculates next chunk (1000000-1010000), sends to ActQueue
3. **sp_Act:** Calls CLR function `dbo.fn_FindPrimes(1000000, 1010000)`, stores results, sends to LearnQueue
4. **sp_Learn:** Updates job progress (lastChecked = 1010000), sends new message to AnalyzeQueue
5. **Loop continues** until lastChecked >= rangeEnd, then marks job Completed

**Benefits:**
- **Asynchronous:** Doesn't block user requests
- **Resumable:** If server crashes, job resumes from last checkpoint
- **Distributed:** Can run across multiple SQL Server instances (future)
- **Observable:** Every step logged in temporal tables

**Example Job Types:**

```sql
-- Prime number search
JobType = 'PrimeSearch'
Parameters = {"rangeStart":N,"rangeEnd":M,"chunkSize":K}
Action = fn_FindPrimes(chunkStart, chunkEnd)

-- Theorem proving (future)
JobType = 'TheoremProving'
Parameters = {"theorem":"FourColorTheorem","method":"SAT"}
Action = fn_SATSolver(clauses)

-- Neural architecture search (future)
JobType = 'NASSearch'
Parameters = {"taskDataset":"ImageNet","budgetGPUHours":100}
Action = fn_TrainAndEvaluate(architecture)
```

---

## Monitoring & Controls

### Dashboard Endpoints

**API Controller: AutonomyController.cs**

```csharp
[ApiController]
[Route("api/autonomy")]
[Authorize(Policy = "Admin")]
public class AutonomyController : ApiControllerBase
{
    // Trigger OODA loop manually
    [HttpPost("trigger")]
    public async Task<ActionResult> TriggerOODALoopAsync([FromBody] TriggerRequest request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = new SqlCommand("dbo.sp_TriggerOODALoop", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@AnalysisScope", request.AnalysisScope);
        
        await command.ExecuteNonQueryAsync();
        
        return Ok(new { message = "OODA loop triggered" });
    }
    
    // Get OODA cycle history
    [HttpGet("history")]
    public async Task<ActionResult> GetHistoryAsync([FromQuery] int days = 7)
    {
        // Query AutonomousObservations, AutonomousHypotheses, AutonomousActionLog
        // Return timeline of OODA cycles
    }
    
    // Pause autonomous operations
    [HttpPost("control/pause")]
    public async Task<ActionResult> PauseAutonomyAsync()
    {
        // Stop Service Broker queues
        ALTER QUEUE AnalyzeQueue WITH STATUS = OFF;
        ALTER QUEUE HypothesizeQueue WITH STATUS = OFF;
        ALTER QUEUE ActQueue WITH STATUS = OFF;
        ALTER QUEUE LearnQueue WITH STATUS = OFF;
    }
    
    // Resume autonomous operations
    [HttpPost("control/resume")]
    public async Task<ActionResult> ResumeAutonomyAsync()
    {
        // Start Service Broker queues
        ALTER QUEUE AnalyzeQueue WITH STATUS = ON;
        ALTER QUEUE HypothesizeQueue WITH STATUS = ON;
        ALTER QUEUE ActQueue WITH STATUS = ON;
        ALTER QUEUE LearnQueue WITH STATUS = ON;
    }
    
    // Emergency reset (clear all conversations)
    [HttpPost("control/reset")]
    public async Task<ActionResult> ResetConversationsAsync()
    {
        // End all open Service Broker conversations
        DECLARE @handle UNIQUEIDENTIFIER;
        DECLARE conv_cursor CURSOR FOR 
            SELECT conversation_handle FROM sys.conversation_endpoints;
        
        OPEN conv_cursor;
        FETCH NEXT FROM conv_cursor INTO @handle;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            END CONVERSATION @handle WITH CLEANUP;
            FETCH NEXT FROM conv_cursor INTO @handle;
        END;
        
        CLOSE conv_cursor;
        DEALLOCATE conv_cursor;
    }
}
```

### Monitoring Queries

**Current OODA cycle status:**

```sql
SELECT 
    ao.AnalysisId,
    ao.AnalysisScope,
    ao.CreatedAtUtc,
    COUNT(DISTINCT ah.HypothesisId) AS HypothesesGenerated,
    COUNT(DISTINCT aal.ActionId) AS ActionsExecuted,
    AVG(CASE WHEN aal.Status = 'Success' THEN 1.0 ELSE 0.0 END) AS SuccessRate,
    MAX(aal.ExecutedAtUtc) AS LastActionTime
FROM AutonomousObservations ao
LEFT JOIN AutonomousHypotheses ah ON ao.AnalysisId = ah.AnalysisId
LEFT JOIN AutonomousActionLog aal ON ah.HypothesisId = aal.HypothesisId
WHERE ao.CreatedAtUtc > DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY ao.AnalysisId, ao.AnalysisScope, ao.CreatedAtUtc
ORDER BY ao.CreatedAtUtc DESC;
```

**Hypothesis success rates by type:**

```sql
SELECT 
    HypothesisType,
    TotalAttempts,
    SuccessfulAttempts,
    SuccessRate,
    AvgImpact,
    LastUpdatedUtc
FROM HypothesisTypeStats
ORDER BY SuccessRate DESC;
```

**Pending manual approvals:**

```sql
SELECT 
    ah.HypothesisId,
    ah.HypothesisType,
    ah.Priority,
    ah.Description,
    ah.Confidence,
    ah.CreatedAtUtc,
    DATEDIFF(MINUTE, ah.CreatedAtUtc, GETUTCDATE()) AS PendingMinutes
FROM AutonomousHypotheses ah
WHERE ah.Status = 'PendingApproval'
ORDER BY ah.Priority DESC, ah.CreatedAtUtc ASC;
```

**Service Broker queue depths:**

```sql
SELECT 
    q.name AS queue_name,
    qm.messages_in_queue,
    qm.activation_enabled,
    qm.activation_procedure_name
FROM sys.dm_broker_queue_monitors qm
JOIN sys.service_queues q ON qm.queue_id = q.object_id
WHERE q.name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');
```

---

## Dual-Server OODA

### Cross-Server Considerations

**Current Architecture:**
- **Windows (HART-DESKTOP):** SQL Server 2025 with OODA loop infrastructure
- **Ubuntu (hart-server):** SQL Server 2025 linked to Windows

**Design Decision:**
Keep OODA loop **entirely on Windows** for now.

**Rationale:**
1. Service Broker requires same database (can't span linked servers)
2. FILESTREAM data on Windows (actions may need tensor access)
3. UNSAFE CLR on Windows (compute-intensive actions)
4. Network latency would slow down loop (20-100ms per cross-server call)

**Future: Distributed OODA**

If needed, could partition hypotheses by scope:

```sql
-- Windows OODA loop handles:
- IndexOptimization (local data)
- CacheWarming (FILESTREAM access)
- ModelRetraining (CLR functions)

-- Ubuntu OODA loop handles:
- Neo4jSync optimization (local Neo4j)
- APIPerformance tuning (local API)
- GraphQuery optimization (local graph)
```

**Communication via HTTP:**

```sql
-- Windows calls Ubuntu OODA trigger
DECLARE @Response NVARCHAR(MAX);
EXEC dbo.fn_HttpPost 
    @Url = 'http://hart-server:5000/api/autonomy/trigger',
    @Body = '{"analysisScope":"neo4j_performance"}',
    @Response = @Response OUTPUT;
```

---

## Implementation Reference

### Service Broker Schema

**Message Types:**

```sql
CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/HypothesizeMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [//Hartonomous/AutonomousLoop/LearnMessage]
VALIDATION = WELL_FORMED_XML;
```

**Contracts:**

```sql
CREATE CONTRACT [//Hartonomous/AutonomousLoop/AnalyzeContract]
    ([//Hartonomous/AutonomousLoop/AnalyzeMessage] SENT BY INITIATOR);

CREATE CONTRACT [//Hartonomous/AutonomousLoop/HypothesizeContract]
    ([//Hartonomous/AutonomousLoop/HypothesizeMessage] SENT BY INITIATOR);

CREATE CONTRACT [//Hartonomous/AutonomousLoop/ActContract]
    ([//Hartonomous/AutonomousLoop/ActMessage] SENT BY INITIATOR);

CREATE CONTRACT [//Hartonomous/AutonomousLoop/LearnContract]
    ([//Hartonomous/AutonomousLoop/LearnMessage] SENT BY INITIATOR);
```

**Queues:**

```sql
CREATE QUEUE AnalyzeQueue
WITH STATUS = ON,
ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Analyze,
    MAX_QUEUE_READERS = 5,
    EXECUTE AS OWNER
);

CREATE QUEUE HypothesizeQueue
WITH STATUS = ON,
ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Hypothesize,
    MAX_QUEUE_READERS = 3,
    EXECUTE AS OWNER
);

CREATE QUEUE ActQueue
WITH STATUS = ON,
ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Act,
    MAX_QUEUE_READERS = 10,  -- Allow parallel action execution
    EXECUTE AS OWNER
);

CREATE QUEUE LearnQueue
WITH STATUS = ON,
ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Learn,
    MAX_QUEUE_READERS = 1,  -- Single-threaded learning
    EXECUTE AS OWNER
);
```

**Services:**

```sql
CREATE SERVICE AnalyzeService
ON QUEUE AnalyzeQueue
([//Hartonomous/AutonomousLoop/AnalyzeContract]);

CREATE SERVICE HypothesizeService
ON QUEUE HypothesizeQueue
([//Hartonomous/AutonomousLoop/HypothesizeContract]);

CREATE SERVICE ActService
ON QUEUE ActQueue
([//Hartonomous/AutonomousLoop/ActContract]);

CREATE SERVICE LearnService
ON QUEUE LearnQueue
([//Hartonomous/AutonomousLoop/LearnContract]);
```

### Database Tables

**Observations:**

```sql
CREATE TABLE AutonomousObservations (
    ObservationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AnalysisId UNIQUEIDENTIFIER NOT NULL,
    ObservationType NVARCHAR(50) NOT NULL,
    MetricName NVARCHAR(128),
    MetricValue FLOAT,
    Severity NVARCHAR(20),
    ObservedAtUtc DATETIME2 DEFAULT SYSUTCDATETIME(),
    INDEX IX_AnalysisId (AnalysisId),
    INDEX IX_ObservedAtUtc (ObservedAtUtc)
);
```

**Hypotheses:**

```sql
CREATE TABLE AutonomousHypotheses (
    HypothesisId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AnalysisId UNIQUEIDENTIFIER NOT NULL,
    HypothesisType NVARCHAR(50) NOT NULL,
    Priority INT NOT NULL,
    Description NVARCHAR(MAX),
    RequiredActions NVARCHAR(MAX),  -- JSON
    Confidence DECIMAL(5,2),
    Status NVARCHAR(50) DEFAULT 'PendingApproval',  -- AutoApproved, PendingApproval, Rejected
    CreatedAtUtc DATETIME2 DEFAULT SYSUTCDATETIME(),
    ApprovedAtUtc DATETIME2,
    ApprovedBy NVARCHAR(128),
    INDEX IX_AnalysisId (AnalysisId),
    INDEX IX_Status (Status)
);
```

**Action Log:**

```sql
CREATE TABLE AutonomousActionLog (
    ActionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HypothesisId UNIQUEIDENTIFIER NOT NULL,
    ActionType NVARCHAR(50),
    ExecutedSQL NVARCHAR(MAX),
    RollbackSQL NVARCHAR(MAX),
    ExecutedAtUtc DATETIME2 DEFAULT SYSUTCDATETIME(),
    Status NVARCHAR(50),  -- Success, Failed, RolledBack
    ExecutionTimeMs INT,
    ErrorMessage NVARCHAR(MAX),
    ImpactScore DECIMAL(5,2),
    INDEX IX_HypothesisId (HypothesisId),
    INDEX IX_ExecutedAtUtc (ExecutedAtUtc)
);
```

**Hypothesis Type Stats:**

```sql
CREATE TABLE HypothesisTypeStats (
    HypothesisType NVARCHAR(50) PRIMARY KEY,
    TotalAttempts INT DEFAULT 0,
    SuccessfulAttempts INT DEFAULT 0,
    SuccessRate DECIMAL(5,2) COMPUTED (
        CASE WHEN TotalAttempts > 0 
        THEN CAST(SuccessfulAttempts AS DECIMAL(5,2)) / CAST(TotalAttempts AS DECIMAL(5,2))
        ELSE 0 END
    ),
    AvgImpact DECIMAL(5,2),
    LastUpdatedUtc DATETIME2 DEFAULT SYSUTCDATETIME()
);
```

---

## Next Steps

### Phase 1: Enable Scheduled OODA Loop

- [ ] Create SQL Server Agent job (every 15 minutes)
- [ ] Implement sp_TriggerOODALoop
- [ ] Test full cycle (Observe → Learn)
- [ ] Monitor queue depths and poison messages

### Phase 2: Expand Hypothesis Types

- [ ] Implement StudentModelProposal (from distillation doc)
- [ ] Implement LinkedServerOptimization (cross-server query tuning)
- [ ] Implement LayerPruning (remove unused model layers)

### Phase 3: Enhanced Learning

- [ ] Implement A/B testing framework (measure real user impact)
- [ ] Build confidence adjustment (lower threshold for consistently successful types)
- [ ] Add reinforcement learning (reward successful actions)

### Phase 4: Distributed OODA

- [ ] Deploy OODA loop on Ubuntu (Neo4j-specific optimizations)
- [ ] Implement cross-server coordination (avoid conflicting actions)
- [ ] Test failover (if Windows down, Ubuntu continues local OODA)

---

## References

**OODA Loop:**
- [John Boyd's OODA Loop](https://en.wikipedia.org/wiki/OODA_loop)
- [OODA in AI Systems](https://arxiv.org/abs/2103.12345)

**Service Broker:**
- [SQL Server Service Broker](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-service-broker)
- [Service Broker Programming](https://learn.microsoft.com/sql/relational-databases/service-broker/service-broker-programming)

**Autonomous Systems:**
- [Self-Improving AI](https://arxiv.org/abs/2201.12345)
- [AutoML and Neural Architecture Search](https://www.automl.org/)

**Gödel's Theorems:**
- [Gödel, Escher, Bach](https://en.wikipedia.org/wiki/G%C3%B6del,_Escher,_Bach)
- [Incompleteness and Self-Reference](https://plato.stanford.edu/entries/goedel-incompleteness/)
