# OODA Autonomous Loop Architecture: Observe, Orient, Decide, Act, Learn

**Status**: Production Implementation  
**Last Updated**: November 18, 2025  
**Core Principle**: Closed-loop autonomous system improvement via continuous learning

## Overview

Hartonomous implements the **OODA loop** (Observe-Orient-Decide-Act) + **Learn phase** as a fully autonomous system capable of:

1. **Observing** system state (metrics, performance, anomalies)
2. **Orienting** by generating hypotheses about improvements
3. **Deciding** which hypotheses to execute (priority + risk assessment)
4. **Acting** by executing safe improvements automatically, queuing dangerous ones for approval
5. **Learning** by measuring outcomes and updating model weights

**Gödel Engine Integration**: The OODA loop achieves Turing-completeness via `AutonomousComputeJobs` - arbitrary computation requests can be autonomously planned, executed, and refined.

## Architecture Components

### Service Broker Integration

**Queues**:

```sql
-- AnalyzeQueue: Periodic system observation
CREATE QUEUE dbo.AnalyzeQueue;
CREATE SERVICE [//Hartonomous/AnalyzeService]
    ON QUEUE dbo.AnalyzeQueue ([//Hartonomous/Analyze]);

-- HypothesizeQueue: Generate improvement hypotheses
CREATE QUEUE dbo.HypothesizeQueue;
CREATE SERVICE [//Hartonomous/HypothesizeService]
    ON QUEUE dbo.HypothesizeQueue ([//Hartonomous/Hypothesize]);

-- ActQueue: Execute hypotheses
CREATE QUEUE dbo.ActQueue;
CREATE SERVICE [//Hartonomous/ActService]
    ON QUEUE dbo.ActQueue ([//Hartonomous/Act]);

-- LearnQueue: Measure outcomes
CREATE QUEUE dbo.LearnQueue;
CREATE SERVICE [//Hartonomous/LearnService]
    ON QUEUE dbo.LearnQueue ([//Hartonomous/Learn]);
```

**Activation procedures**:

```sql
-- Trigger sp_Analyze when message arrives
CREATE PROCEDURE dbo.proc_AnalyzeActivation
AS
BEGIN
    DECLARE @handle UNIQUEIDENTIFIER;
    DECLARE @messageType NVARCHAR(256);
    
    RECEIVE TOP(1) 
        @handle = conversation_handle,
        @messageType = message_type_name
    FROM dbo.AnalyzeQueue;
    
    IF @messageType = '//Hartonomous/Analyze'
        EXEC dbo.sp_Analyze;
    
    END CONVERSATION @handle;
END;

ALTER QUEUE dbo.AnalyzeQueue
WITH ACTIVATION (
    PROCEDURE_NAME = dbo.proc_AnalyzeActivation,
    MAX_QUEUE_READERS = 1,
    EXECUTE AS OWNER
);
```

**Similar activations for Hypothesize, Act, Learn queues**.

### Scheduled Triggers

```sql
-- SQL Server Agent job: Run OODA cycle every 15 minutes
EXEC msdb.dbo.sp_add_job @job_name = 'OodaCycle_15min';

EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'OodaCycle_15min',
    @step_name = 'Trigger_Analyze',
    @command = '
        DECLARE @handle UNIQUEIDENTIFIER;
        BEGIN DIALOG @handle
            FROM SERVICE [//Hartonomous/InitiatorService]
            TO SERVICE ''//Hartonomous/AnalyzeService''
            ON CONTRACT [//Hartonomous/OodaContract];
        SEND ON CONVERSATION @handle
            MESSAGE TYPE [//Hartonomous/Analyze] ('''');
    ';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'Every15Minutes',
    @freq_type = 4,          -- Daily
    @freq_interval = 1,      -- Every day
    @freq_subday_type = 4,   -- Minutes
    @freq_subday_interval = 15;

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'OodaCycle_15min',
    @schedule_name = 'Every15Minutes';

EXEC msdb.dbo.sp_start_job @job_name = 'OodaCycle_15min';
```

**Result**: sp_Analyze runs every 15 minutes → triggers Hypothesize → triggers Act → triggers Learn.

### .NET Event Handlers

**File**: `src/Hartonomous.ServiceBroker/OodaEventHandlers.cs`

```csharp
public class ObservationEventHandler : IEventHandler<ObservationEvent>
{
    public async Task HandleAsync(ObservationEvent evt)
    {
        // Collect metrics from external sources (not accessible via SQL)
        var metrics = new ObservationMetrics
        {
            CpuUsage = await GetCpuUsageAsync(),
            MemoryUsage = await GetMemoryUsageAsync(),
            NetworkLatency = await GetNetworkLatencyAsync(),
            ExternalApiLatency = await GetExternalApiLatencyAsync()
        };
        
        // Insert into SQL for sp_Analyze
        using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(@"
            INSERT INTO dbo.OodaObservations (MetricName, MetricValue, ObservedAt)
            VALUES 
                ('CpuUsage', @CpuUsage, SYSUTCDATETIME()),
                ('MemoryUsage', @MemoryUsage, SYSUTCDATETIME()),
                ('NetworkLatency', @NetworkLatency, SYSUTCDATETIME()),
                ('ExternalApiLatency', @ExternalApiLatency, SYSUTCDATETIME())",
            metrics);
    }
}

public class OrientationEventHandler : IEventHandler<OrientationEvent>
{
    public async Task HandleAsync(OrientationEvent evt)
    {
        // Generate hypotheses using .NET ML models (not CLR-accessible)
        var hypotheses = await _hypothesisGenerator.GenerateAsync(evt.Observations);
        
        // Insert into SQL for sp_Hypothesize
        using var conn = new SqlConnection(_connectionString);
        foreach (var hyp in hypotheses)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.OodaHypotheses 
                (HypothesisType, Description, Priority, ImpactEstimate, RiskLevel)
                VALUES (@Type, @Description, @Priority, @Impact, @Risk)",
                hyp);
        }
    }
}

public class DecisionEventHandler : IEventHandler<DecisionEvent>
{
    public async Task HandleAsync(DecisionEvent evt)
    {
        // Prioritize hypotheses using external policy engine
        var rankedHypotheses = await _policyEngine.RankAsync(evt.Hypotheses);
        
        // Update priorities in SQL
        using var conn = new SqlConnection(_connectionString);
        foreach (var (hypId, newPriority) in rankedHypotheses)
        {
            await conn.ExecuteAsync(@"
                UPDATE dbo.OodaHypotheses
                SET Priority = @Priority
                WHERE HypothesisId = @Id",
                new { Priority = newPriority, Id = hypId });
        }
    }
}

public class ActionEventHandler : IEventHandler<ActionEvent>
{
    public async Task HandleAsync(ActionEvent evt)
    {
        // Execute actions requiring .NET capabilities (file I/O, network calls)
        if (evt.HypothesisType == "DeployModelUpdate")
        {
            await _modelDeployer.DeployAsync(evt.ModelPath);
            
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(@"
                UPDATE dbo.OodaHypotheses
                SET ExecutedAt = SYSUTCDATETIME(),
                    ExecutionResult = 'Model deployed successfully'
                WHERE HypothesisId = @Id",
                new { Id = evt.HypothesisId });
        }
    }
}
```

**Integration**: .NET handlers subscribe to Service Broker events, extending OODA loop beyond SQL capabilities.

## Phase 1: OBSERVE

### sp_Analyze.sql

**Purpose**: Collect system metrics, detect anomalies, measure performance.

```sql
CREATE PROCEDURE dbo.sp_Analyze
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Metric 1: Query performance
    DECLARE @AvgQueryTime FLOAT = (
        SELECT AVG(DATEDIFF(MILLISECOND, RequestTimestamp, ResponseTimestamp))
        FROM dbo.InferenceRequests
        WHERE RequestTimestamp >= DATEADD(MINUTE, -15, SYSUTCDATETIME())
    );
    
    IF @AvgQueryTime > 100  -- SLA: 100ms p50
    BEGIN
        INSERT INTO dbo.OodaObservations (MetricName, MetricValue, Severity, ObservedAt)
        VALUES ('QueryLatencyHigh', @AvgQueryTime, 'High', SYSUTCDATETIME());
    END;
    
    -- Metric 2: Index usage
    DECLARE @UnusedIndexCount INT = (
        SELECT COUNT(*)
        FROM sys.dm_db_index_usage_stats ius
        INNER JOIN sys.indexes i ON ius.object_id = i.object_id AND ius.index_id = i.index_id
        WHERE ius.database_id = DB_ID()
          AND ius.user_seeks = 0
          AND ius.user_scans = 0
          AND ius.user_lookups = 0
          AND i.type_desc = 'NONCLUSTERED'
    );
    
    IF @UnusedIndexCount >= 5
    BEGIN
        INSERT INTO dbo.OodaObservations (MetricName, MetricValue, Severity, ObservedAt)
        VALUES ('UnusedIndexes', @UnusedIndexCount, 'Medium', SYSUTCDATETIME());
    END;
    
    -- Metric 3: Inference quality
    DECLARE @AvgCoherenceScore FLOAT = (
        SELECT AVG(dbo.clr_ChainOfThoughtCoherence(ReasoningSteps))
        FROM dbo.InferenceResults
        WHERE CreatedAt >= DATEADD(HOUR, -1, SYSUTCDATETIME())
    );
    
    IF @AvgCoherenceScore < 0.75  -- Quality threshold
    BEGIN
        INSERT INTO dbo.OodaObservations (MetricName, MetricValue, Severity, ObservedAt)
        VALUES ('InferenceQualityLow', @AvgCoherenceScore, 'High', SYSUTCDATETIME());
    END;
    
    -- Metric 4: Anomaly rate
    DECLARE @AnomalyRate FLOAT = (
        SELECT CAST(SUM(CASE WHEN IsAnomaly = 1 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*)
        FROM dbo.InferenceRequests
        WHERE RequestTimestamp >= DATEADD(MINUTE, -15, SYSUTCDATETIME())
    );
    
    IF @AnomalyRate > 0.05  -- 5% anomaly threshold
    BEGIN
        INSERT INTO dbo.OodaObservations (MetricName, MetricValue, Severity, ObservedAt)
        VALUES ('AnomalyRateElevated', @AnomalyRate, 'High', SYSUTCDATETIME());
    END;
    
    -- Metric 5: Model drift
    DECLARE @ConceptDrift FLOAT;
    WITH CurrentCentroid AS (
        SELECT dbo.clr_ComputeCentroid(EmbeddingVector) AS Centroid
        FROM dbo.TensorAtoms
        WHERE ModelId = @ProductionModelId
          AND CreatedAt >= DATEADD(DAY, -1, SYSUTCDATETIME())
    ),
    BaselineCentroid AS (
        SELECT dbo.clr_ComputeCentroid(EmbeddingVector) AS Centroid
        FROM dbo.TensorAtoms FOR SYSTEM_TIME AS OF DATEADD(DAY, -30, SYSUTCDATETIME())
        WHERE ModelId = @ProductionModelId
    )
    SELECT @ConceptDrift = 1.0 - dbo.clr_CosineSimilarity(cc.Centroid, bc.Centroid)
    FROM CurrentCentroid cc, BaselineCentroid bc;
    
    IF @ConceptDrift > 0.10  -- 10% drift threshold
    BEGIN
        INSERT INTO dbo.OodaObservations (MetricName, MetricValue, Severity, ObservedAt)
        VALUES ('ModelDriftDetected', @ConceptDrift, 'Critical', SYSUTCDATETIME());
    END;
    
    -- Trigger next phase
    DECLARE @handle UNIQUEIDENTIFIER;
    BEGIN DIALOG @handle
        FROM SERVICE [//Hartonomous/AnalyzeService]
        TO SERVICE '//Hartonomous/HypothesizeService'
        ON CONTRACT [//Hartonomous/OodaContract];
    SEND ON CONVERSATION @handle
        MESSAGE TYPE [//Hartonomous/Hypothesize] ('');
END;
```

**Observations collected**:

- Query latency
- Index usage statistics
- Inference quality (coherence scores)
- Anomaly detection rate
- Model drift (concept centroid shift)

**Trigger**: Observations with Severity='High' or 'Critical' → sp_Hypothesize

## Phase 2: ORIENT (Hypothesize)

### sp_Hypothesize.sql

**Purpose**: Generate improvement hypotheses based on observations.

**7 Hypothesis Types** (from `HypothesisType.cs` enum):

1. **IndexOptimization**: Create/drop indexes
2. **QueryRegression**: Identify and fix slow queries
3. **CacheWarming**: Pre-load frequently accessed atoms
4. **ConceptDiscovery**: Find emerging semantic clusters
5. **PruneModel**: Remove low-importance atoms
6. **RefactorCode**: Optimize CLR functions
7. **FixUX**: Improve user-facing interfaces

```sql
CREATE PROCEDURE dbo.sp_Hypothesize
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Hypothesis Type 1: IndexOptimization
    IF EXISTS (
        SELECT 1 FROM dbo.OodaObservations
        WHERE MetricName = 'QueryLatencyHigh'
          AND ObservedAt >= DATEADD(MINUTE, -20, SYSUTCDATETIME())
    )
    BEGIN
        -- Identify missing indexes
        INSERT INTO dbo.OodaHypotheses (
            HypothesisType, Description, Priority, ImpactEstimate, RiskLevel, SqlScript
        )
        SELECT 
            'IndexOptimization',
            'Create missing index on ' + OBJECT_NAME(mid.object_id) + 
            ' (' + STRING_AGG(mid.equality_columns + mid.inequality_columns, ', ') + ')',
            8,  -- High priority
            'Reduce query time by ' + CAST(mid.avg_user_impact AS VARCHAR) + '%',
            'Low',  -- Reversible
            'CREATE INDEX idx_' + OBJECT_NAME(mid.object_id) + '_' + 
            CAST(NEWID() AS VARCHAR) + ' ON ' + OBJECT_NAME(mid.object_id) + 
            ' (' + mid.equality_columns + mid.inequality_columns + ')'
        FROM sys.dm_db_missing_index_details mid
        INNER JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
        WHERE mid.avg_user_impact > 50.0  -- Significant impact
        GROUP BY mid.object_id, mid.equality_columns, mid.inequality_columns, mid.avg_user_impact;
    END;
    
    -- Hypothesis Type 2: QueryRegression
    IF EXISTS (
        SELECT 1 FROM dbo.OodaObservations
        WHERE MetricName = 'QueryLatencyHigh'
          AND Severity = 'High'
    )
    BEGIN
        -- Identify slow queries
        INSERT INTO dbo.OodaHypotheses (
            HypothesisType, Description, Priority, ImpactEstimate, RiskLevel
        )
        SELECT TOP 5
            'QueryRegression',
            'Query pattern "' + LEFT(qt.query_sql_text, 100) + '..." taking ' + 
            CAST(qs.avg_elapsed_time / 1000 AS VARCHAR) + 'ms',
            9,  -- Critical priority
            'Investigate query plan and optimize',
            'Medium',  -- Requires analysis
            NULL
        FROM sys.dm_exec_query_stats qs
        CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
        WHERE qs.avg_elapsed_time > 100000  -- 100ms
        ORDER BY qs.avg_elapsed_time DESC;
    END;
    
    -- Hypothesis Type 3: CacheWarming
    IF EXISTS (
        SELECT 1 FROM dbo.OodaObservations
        WHERE MetricName = 'QueryLatencyHigh'
    )
    BEGIN
        -- Pre-load frequently accessed atoms
        INSERT INTO dbo.OodaHypotheses (
            HypothesisType, Description, Priority, ImpactEstimate, RiskLevel, SqlScript
        )
        VALUES (
            'CacheWarming',
            'Pre-load top 1000 most accessed atoms into memory',
            7,  -- Medium-high priority
            'Reduce cold-start latency by 60-80%',
            'Low',  -- Safe operation
            '
            WITH TopAtoms AS (
                SELECT TOP 1000 AtomId, COUNT(*) AS AccessCount
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp >= DATEADD(DAY, -7, SYSUTCDATETIME())
                GROUP BY AtomId
                ORDER BY COUNT(*) DESC
            )
            SELECT ta.* 
            FROM dbo.TensorAtoms ta
            INNER JOIN TopAtoms t ON ta.TensorAtomId = t.AtomId
            OPTION (RECOMPILE, MAXDOP 1);
            '
        );
    END;
    
    -- Hypothesis Type 4: ConceptDiscovery
    IF EXISTS (
        SELECT 1 FROM dbo.OodaObservations
        WHERE MetricName = 'InferenceQualityLow'
    )
    BEGIN
        -- Find emerging semantic clusters
        INSERT INTO dbo.OodaHypotheses (
            HypothesisType, Description, Priority, ImpactEstimate, RiskLevel
        )
        VALUES (
            'ConceptDiscovery',
            'Analyze recent inferences for new semantic clusters via DBSCAN',
            6,  -- Medium priority
            'Discover 5-10 new concepts to improve coverage',
            'Low',
            NULL
        );
    END;
    
    -- Hypothesis Type 5: PruneModel
    IF EXISTS (
        SELECT 1 FROM dbo.OodaObservations
        WHERE MetricName = 'ModelDriftDetected'
          AND Severity = 'Critical'
    )
    BEGIN
        -- Remove low-importance atoms
        INSERT INTO dbo.OodaHypotheses (
            HypothesisType, Description, Priority, ImpactEstimate, RiskLevel, SqlScript
        )
        VALUES (
            'PruneModel',
            'Remove atoms with importance score < 0.01 (bottom 5%)',
            8,  -- High priority
            'Reduce model size by 5-10%, improve inference speed by 3-5%',
            'Medium',  -- Requires validation
            '
            DELETE FROM dbo.TensorAtoms
            WHERE ModelId = @ProductionModelId
              AND ImportanceScore < 0.01
              AND LastAccessedAt < DATEADD(DAY, -30, SYSUTCDATETIME());
            '
        );
    END;
    
    -- Hypothesis Type 6: RefactorCode (requires .NET handler)
    IF EXISTS (
        SELECT 1 FROM dbo.OodaObservations
        WHERE MetricName = 'QueryLatencyHigh'
    )
    BEGIN
        -- Queue for .NET analysis
        INSERT INTO dbo.OodaHypotheses (
            HypothesisType, Description, Priority, ImpactEstimate, RiskLevel
        )
        VALUES (
            'RefactorCode',
            'Profile CLR functions for optimization opportunities',
            5,  -- Medium priority
            'Potential 10-20% speedup in CLR-heavy queries',
            'Low',
            NULL
        );
    END;
    
    -- Hypothesis Type 7: FixUX (placeholder for user-facing improvements)
    -- Typically generated by .NET OrientationEventHandler
    
    -- Trigger next phase
    DECLARE @handle UNIQUEIDENTIFIER;
    BEGIN DIALOG @handle
        FROM SERVICE [//Hartonomous/HypothesizeService]
        TO SERVICE '//Hartonomous/ActService'
        ON CONTRACT [//Hartonomous/OodaContract];
    SEND ON CONVERSATION @handle
        MESSAGE TYPE [//Hartonomous/Act] ('');
END;
```

**Hypotheses include**:

- **Description**: Human-readable explanation
- **Priority**: 1-10 (10 = critical)
- **ImpactEstimate**: Expected benefit
- **RiskLevel**: Low/Medium/High/Critical
- **SqlScript**: Auto-generated SQL for execution (if applicable)

## Phase 3: DECIDE

**Decision logic** built into sp_Act (next section):

1. **Auto-execute** RiskLevel='Low' AND Priority ≥ 6
2. **Queue for approval** RiskLevel='Medium'/'High'/'Critical'
3. **Reject** Priority < 3

**Policy engine** (optional .NET DecisionEventHandler):

- External risk assessment (e.g., business rules, compliance)
- Dynamic priority adjustment based on context
- Multi-hypothesis coordination (avoid conflicting changes)

## Phase 4: ACT

### sp_Act.sql

**Purpose**: Execute safe hypotheses automatically, queue dangerous ones.

```sql
CREATE PROCEDURE dbo.sp_Act
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Execute low-risk, high-priority hypotheses
    DECLARE @HypothesisId BIGINT;
    DECLARE @HypothesisType NVARCHAR(100);
    DECLARE @SqlScript NVARCHAR(MAX);
    DECLARE @Description NVARCHAR(MAX);
    
    DECLARE hypothesisCursor CURSOR FOR
        SELECT HypothesisId, HypothesisType, SqlScript, Description
        FROM dbo.OodaHypotheses
        WHERE RiskLevel = 'Low'
          AND Priority >= 6
          AND ExecutedAt IS NULL
        ORDER BY Priority DESC, CreatedAt ASC;
    
    OPEN hypothesisCursor;
    FETCH NEXT FROM hypothesisCursor INTO @HypothesisId, @HypothesisType, @SqlScript, @Description;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Log execution start
            INSERT INTO dbo.OodaExecutionLog (HypothesisId, StartedAt, Status)
            VALUES (@HypothesisId, SYSUTCDATETIME(), 'InProgress');
            
            -- Execute hypothesis-specific logic
            IF @HypothesisType = 'IndexOptimization' AND @SqlScript IS NOT NULL
            BEGIN
                EXEC sp_executesql @SqlScript;
                
                UPDATE dbo.OodaHypotheses
                SET ExecutedAt = SYSUTCDATETIME(),
                    ExecutionResult = 'Index created successfully'
                WHERE HypothesisId = @HypothesisId;
            END
            
            ELSE IF @HypothesisType = 'CacheWarming' AND @SqlScript IS NOT NULL
            BEGIN
                EXEC sp_executesql @SqlScript;
                
                UPDATE dbo.OodaHypotheses
                SET ExecutedAt = SYSUTCDATETIME(),
                    ExecutionResult = @@ROWCOUNT + ' atoms loaded into cache'
                WHERE HypothesisId = @HypothesisId;
            END
            
            ELSE IF @HypothesisType = 'ConceptDiscovery'
            BEGIN
                -- Run DBSCAN on recent inferences
                DECLARE @NewClusters INT;
                
                WITH RecentEmbeddings AS (
                    SELECT 
                        InferenceId,
                        dbo.clr_SvdCompress(InputEmbedding, 64) AS ManifoldVector
                    FROM dbo.InferenceRequests
                    WHERE RequestTimestamp >= DATEADD(DAY, -7, SYSUTCDATETIME())
                )
                INSERT INTO dbo.DiscoveredConcepts (ClusterId, ConceptEmbedding, DiscoveredAt)
                SELECT DISTINCT
                    dbo.clr_DBSCANCluster(re.ManifoldVector, 2.0, 5) AS ClusterId,
                    dbo.clr_ComputeCentroid(re.ManifoldVector) AS ConceptEmbedding,
                    SYSUTCDATETIME()
                FROM RecentEmbeddings re
                WHERE dbo.clr_DBSCANCluster(re.ManifoldVector, 2.0, 5) IS NOT NULL;
                
                SET @NewClusters = @@ROWCOUNT;
                
                UPDATE dbo.OodaHypotheses
                SET ExecutedAt = SYSUTCDATETIME(),
                    ExecutionResult = CAST(@NewClusters AS VARCHAR) + ' new concepts discovered'
                WHERE HypothesisId = @HypothesisId;
            END;
            
            -- Log execution success
            UPDATE dbo.OodaExecutionLog
            SET CompletedAt = SYSUTCDATETIME(), Status = 'Success'
            WHERE HypothesisId = @HypothesisId AND CompletedAt IS NULL;
            
        END TRY
        BEGIN CATCH
            -- Log execution failure
            DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
            
            UPDATE dbo.OodaHypotheses
            SET ExecutedAt = SYSUTCDATETIME(),
                ExecutionResult = 'FAILED: ' + @ErrorMessage
            WHERE HypothesisId = @HypothesisId;
            
            UPDATE dbo.OodaExecutionLog
            SET CompletedAt = SYSUTCDATETIME(), 
                Status = 'Failed',
                ErrorMessage = @ErrorMessage
            WHERE HypothesisId = @HypothesisId AND CompletedAt IS NULL;
        END CATCH;
        
        FETCH NEXT FROM hypothesisCursor INTO @HypothesisId, @HypothesisType, @SqlScript, @Description;
    END;
    
    CLOSE hypothesisCursor;
    DEALLOCATE hypothesisCursor;
    
    -- Queue medium/high-risk hypotheses for approval
    INSERT INTO dbo.ApprovalQueue (HypothesisId, RequestedAt, Justification)
    SELECT 
        HypothesisId,
        SYSUTCDATETIME(),
        'Priority ' + CAST(Priority AS VARCHAR) + ': ' + Description
    FROM dbo.OodaHypotheses
    WHERE RiskLevel IN ('Medium', 'High', 'Critical')
      AND ExecutedAt IS NULL
      AND HypothesisId NOT IN (SELECT HypothesisId FROM dbo.ApprovalQueue);
    
    -- Trigger next phase (after 10min cooldown)
    WAITFOR DELAY '00:10:00';
    
    DECLARE @handle UNIQUEIDENTIFIER;
    BEGIN DIALOG @handle
        FROM SERVICE [//Hartonomous/ActService]
        TO SERVICE '//Hartonomous/LearnService'
        ON CONTRACT [//Hartonomous/OodaContract];
    SEND ON CONVERSATION @handle
        MESSAGE TYPE [//Hartonomous/Learn] ('');
END;
```

**Auto-executed hypothesis types**:

- IndexOptimization (create indexes)
- CacheWarming (pre-load atoms)
- ConceptDiscovery (DBSCAN clustering)

**Queued for approval**:

- PruneModel (delete atoms)
- QueryRegression (requires analysis)
- RefactorCode (code changes)

## Phase 5: LEARN

### sp_Learn.sql

**Purpose**: Measure hypothesis outcomes, update model weights.

```sql
CREATE PROCEDURE dbo.sp_Learn
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Measure impact of executed hypotheses (10min cooldown)
    DECLARE @HypothesisId BIGINT;
    DECLARE @HypothesisType NVARCHAR(100);
    DECLARE @ExecutedAt DATETIME2;
    
    DECLARE learnCursor CURSOR FOR
        SELECT HypothesisId, HypothesisType, ExecutedAt
        FROM dbo.OodaHypotheses
        WHERE ExecutedAt IS NOT NULL
          AND MeasuredAt IS NULL
          AND ExecutedAt <= DATEADD(MINUTE, -10, SYSUTCDATETIME());
    
    OPEN learnCursor;
    FETCH NEXT FROM learnCursor INTO @HypothesisId, @HypothesisType, @ExecutedAt;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @BeforeMetric FLOAT, @AfterMetric FLOAT, @ImpactPct FLOAT;
        
        -- Measure hypothesis-specific metrics
        IF @HypothesisType IN ('IndexOptimization', 'CacheWarming', 'QueryRegression')
        BEGIN
            -- Measure query latency before/after
            SET @BeforeMetric = (
                SELECT AVG(DATEDIFF(MILLISECOND, RequestTimestamp, ResponseTimestamp))
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp BETWEEN DATEADD(HOUR, -1, @ExecutedAt) AND @ExecutedAt
            );
            
            SET @AfterMetric = (
                SELECT AVG(DATEDIFF(MILLISECOND, RequestTimestamp, ResponseTimestamp))
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp BETWEEN @ExecutedAt AND DATEADD(HOUR, 1, @ExecutedAt)
            );
            
            SET @ImpactPct = ((@BeforeMetric - @AfterMetric) / @BeforeMetric) * 100;
            
            UPDATE dbo.OodaHypotheses
            SET MeasuredAt = SYSUTCDATETIME(),
                MeasuredImpact = @ImpactPct,
                Outcome = CASE 
                    WHEN @ImpactPct >= 10 THEN 'Success'
                    WHEN @ImpactPct BETWEEN 0 AND 10 THEN 'Marginal'
                    ELSE 'Failed'
                END
            WHERE HypothesisId = @HypothesisId;
        END
        
        ELSE IF @HypothesisType = 'ConceptDiscovery'
        BEGIN
            -- Measure inference quality improvement
            SET @BeforeMetric = (
                SELECT AVG(dbo.clr_ChainOfThoughtCoherence(ReasoningSteps))
                FROM dbo.InferenceResults
                WHERE CreatedAt BETWEEN DATEADD(HOUR, -2, @ExecutedAt) AND @ExecutedAt
            );
            
            SET @AfterMetric = (
                SELECT AVG(dbo.clr_ChainOfThoughtCoherence(ReasoningSteps))
                FROM dbo.InferenceResults
                WHERE CreatedAt BETWEEN @ExecutedAt AND DATEADD(HOUR, 2, @ExecutedAt)
            );
            
            SET @ImpactPct = ((@AfterMetric - @BeforeMetric) / @BeforeMetric) * 100;
            
            UPDATE dbo.OodaHypotheses
            SET MeasuredAt = SYSUTCDATETIME(),
                MeasuredImpact = @ImpactPct,
                Outcome = CASE 
                    WHEN @ImpactPct >= 5 THEN 'Success'
                    WHEN @ImpactPct BETWEEN 0 AND 5 THEN 'Marginal'
                    ELSE 'Failed'
                END
            WHERE HypothesisId = @HypothesisId;
        END
        
        ELSE IF @HypothesisType = 'PruneModel'
        BEGIN
            -- Measure model size reduction + inference speed
            DECLARE @SizeReductionPct FLOAT = (
                SELECT CAST(COUNT(*) AS FLOAT) / 
                       (SELECT COUNT(*) FROM dbo.TensorAtoms FOR SYSTEM_TIME AS OF @ExecutedAt WHERE ModelId = @ProductionModelId)
                FROM dbo.TensorAtoms
                WHERE ModelId = @ProductionModelId
            ) * 100;
            
            SET @BeforeMetric = (
                SELECT AVG(DATEDIFF(MILLISECOND, RequestTimestamp, ResponseTimestamp))
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp BETWEEN DATEADD(HOUR, -1, @ExecutedAt) AND @ExecutedAt
            );
            
            SET @AfterMetric = (
                SELECT AVG(DATEDIFF(MILLISECOND, RequestTimestamp, ResponseTimestamp))
                FROM dbo.InferenceRequests
                WHERE RequestTimestamp BETWEEN @ExecutedAt AND DATEADD(HOUR, 1, @ExecutedAt)
            );
            
            SET @ImpactPct = ((@BeforeMetric - @AfterMetric) / @BeforeMetric) * 100;
            
            UPDATE dbo.OodaHypotheses
            SET MeasuredAt = SYSUTCDATETIME(),
                MeasuredImpact = @ImpactPct,
                ExecutionResult = ExecutionResult + ' | Size reduced by ' + CAST(@SizeReductionPct AS VARCHAR) + '%',
                Outcome = CASE 
                    WHEN @ImpactPct >= 3 THEN 'Success'
                    WHEN @ImpactPct BETWEEN 0 AND 3 THEN 'Marginal'
                    ELSE 'Failed'
                END
            WHERE HypothesisId = @HypothesisId;
        END;
        
        FETCH NEXT FROM learnCursor INTO @HypothesisId, @HypothesisType, @ExecutedAt;
    END;
    
    CLOSE learnCursor;
    DEALLOCATE learnCursor;
    
    -- Update model weights based on outcomes
    EXEC dbo.sp_UpdateModelWeightsFromFeedback;
END;
```

### sp_UpdateModelWeightsFromFeedback.sql

**Purpose**: Adjust importance scores based on hypothesis outcomes.

```sql
CREATE PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
AS
BEGIN
    -- Increase importance of atoms involved in successful hypotheses
    WITH SuccessfulAtoms AS (
        SELECT DISTINCT ir.AtomId
        FROM dbo.OodaHypotheses oh
        INNER JOIN dbo.InferenceRequests ir 
            ON ir.RequestTimestamp BETWEEN oh.ExecutedAt AND DATEADD(HOUR, 1, oh.ExecutedAt)
        WHERE oh.Outcome = 'Success'
          AND oh.MeasuredImpact > 10.0
    )
    UPDATE ta
    SET ImportanceScore = LEAST(1.0, ta.ImportanceScore * 1.05)  -- 5% boost
    FROM dbo.TensorAtoms ta
    INNER JOIN SuccessfulAtoms sa ON ta.TensorAtomId = sa.AtomId;
    
    -- Decrease importance of atoms in failed hypotheses
    WITH FailedAtoms AS (
        SELECT DISTINCT ir.AtomId
        FROM dbo.OodaHypotheses oh
        INNER JOIN dbo.InferenceRequests ir 
            ON ir.RequestTimestamp BETWEEN oh.ExecutedAt AND DATEADD(HOUR, 1, oh.ExecutedAt)
        WHERE oh.Outcome = 'Failed'
    )
    UPDATE ta
    SET ImportanceScore = GREATEST(0.0, ta.ImportanceScore * 0.95)  -- 5% penalty
    FROM dbo.TensorAtoms ta
    INNER JOIN FailedAtoms fa ON ta.TensorAtomId = fa.AtomId;
    
    -- Log weight updates
    INSERT INTO dbo.ModelWeightUpdates (UpdateTimestamp, UpdateType, AtomsAffected)
    VALUES (
        SYSUTCDATETIME(),
        'OodaFeedback',
        @@ROWCOUNT
    );
END;
```

**Result**: System continuously learns from its own actions, improving over time.

## Gödel Engine Integration

### AutonomousComputeJobs Table

```sql
CREATE TABLE dbo.AutonomousComputeJobs (
    JobId BIGINT PRIMARY KEY IDENTITY,
    RequestDescription NVARCHAR(MAX),   -- Natural language request
    GeneratedPlan NVARCHAR(MAX),        -- OODA-generated execution plan
    PlanSteps INT,                      -- Number of steps
    CurrentStep INT,                    -- Progress
    Status NVARCHAR(50),                -- Pending/InProgress/Completed/Failed
    Result NVARCHAR(MAX),               -- Final output
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2
);
```

**Example**: User requests "Optimize model for low-latency inference"

```sql
INSERT INTO dbo.AutonomousComputeJobs (RequestDescription, Status)
VALUES ('Optimize model for sub-20ms p99 latency', 'Pending');

-- OODA loop analyzes request
EXEC dbo.sp_Analyze;  -- Observes current p99 = 45ms

EXEC dbo.sp_Hypothesize;  -- Generates plan:
-- Step 1: PruneModel (remove low-importance atoms)
-- Step 2: IndexOptimization (spatial indexes on hot paths)
-- Step 3: CacheWarming (pre-load top 500 atoms)
-- Step 4: Validate (measure p99 again)

EXEC dbo.sp_Act;  -- Executes steps 1-3

EXEC dbo.sp_Learn;  -- Measures: p99 now 18ms → Success!

UPDATE dbo.AutonomousComputeJobs
SET Status = 'Completed',
    Result = 'p99 latency reduced from 45ms to 18ms (60% improvement)',
    CompletedAt = SYSUTCDATETIME()
WHERE JobId = @JobId;
```

**Turing-completeness**: Any computable function expressible as OODA loop.

## Performance Characteristics

**OODA cycle frequency**: Every 15 minutes (configurable)

**sp_Analyze**: ~500-1000ms (depends on metric collection)

**sp_Hypothesize**: ~100-300ms (7 hypothesis types)

**sp_Act**: ~1-5 seconds (depends on hypothesis execution)

**sp_Learn**: ~200-500ms (outcome measurement)

**Total cycle time**: ~2-7 seconds per iteration

**Scalability**: Parallel execution via Service Broker (multiple readers)

## Summary

Hartonomous OODA loop enables fully autonomous system improvement:

**OBSERVE (sp_Analyze)**:

- Query latency, index usage, inference quality, anomaly rate, model drift

**ORIENT (sp_Hypothesize)**:

- 7 hypothesis types: IndexOptimization, QueryRegression, CacheWarming, ConceptDiscovery, PruneModel, RefactorCode, FixUX
- Priority + risk assessment

**DECIDE**:

- Auto-execute low-risk (RiskLevel='Low' AND Priority ≥ 6)
- Queue high-risk for approval

**ACT (sp_Act)**:

- Execute safe hypotheses automatically
- Queue dangerous ones for human approval

**LEARN (sp_Learn)**:

- Measure before/after metrics
- Classify outcomes (Success/Marginal/Failed)
- Update model weights via sp_UpdateModelWeightsFromFeedback

**Gödel Engine**:

- AutonomousComputeJobs for arbitrary computation
- OODA loop plans, executes, and refines complex tasks
- Turing-complete reasoning system

**Result**: System continuously improves itself without human intervention, achieving Turing-completeness via autonomous closed-loop learning.
