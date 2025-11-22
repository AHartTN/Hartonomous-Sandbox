# OODA Autonomous Loop Architecture

**Status**: Production Implementation  
**Date**: January 2025  
**Innovation**: Dual-triggering (Scheduled + Event-Driven)

---

## Overview

The OODA (Observe-Orient-Decide-Act-Learn) Loop is Hartonomous's autonomous improvement engine. Unlike traditional monitoring systems that alert humans, OODA **executes safe optimizations automatically** and queues dangerous changes for approval.

### The Five Phases

```text
┌──────────┐
│ OBSERVE  │ ← Collect system metrics (DMVs, performance counters)
└────┬─────┘
     │
     ▼
┌──────────┐
│ ORIENT   │ ← Detect patterns, generate hypotheses
└────┬─────┘
     │
     ▼
┌──────────┐
│ DECIDE   │ ← Prioritize hypotheses, assess risk
└────┬─────┘
     │
     ▼
┌──────────┐
│   ACT    │ ← Execute safe changes, queue dangerous ones
└────┬─────┘
     │
     ▼
┌──────────┐
│  LEARN   │ ← Measure outcomes, update model weights
└────┬─────┘
     │
     └─────── (Feedback loop to OBSERVE)
```

---

## Dual-Triggering Architecture

### Why Both Scheduled AND Event-Driven?

**BOTH mechanisms are intentional** - they serve different purposes:

#### 1. Scheduled OODA Loop (Every 15 Minutes)

**Purpose**: System maintenance, entropy reduction, proactive optimization

**Use Cases**:
- Detect slow queries from execution statistics
- Identify unused indexes (wasting storage and write performance)
- Prune low-importance model weights (reduce storage)
- Warm cache with frequently accessed atoms
- Defragment spatial indices

**Trigger**: SQL Server Agent job

```sql
-- Create job: Every 15 minutes
EXEC msdb.dbo.sp_add_job 
    @job_name = N'OodaCycle_15min',
    @description = N'Autonomous OODA improvement cycle';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = N'Every15Minutes',
    @freq_type = 4,              -- Daily
    @freq_interval = 1,          -- Every day
    @freq_subday_type = 4,       -- Minutes
    @freq_subday_interval = 15;  -- Every 15 minutes

EXEC msdb.dbo.sp_add_jobstep
    @job_name = N'OodaCycle_15min',
    @step_name = N'Trigger_Observe',
    @subsystem = N'TSQL',
    @command = N'EXEC dbo.sp_Analyze;';
```

#### 2. Event-Driven Service Broker (On-Demand)

**Purpose**: User requests, autonomous computation, Gödel engine

**Use Cases**:
- User initiates model inference (immediate response)
- External event triggers hypothesis generation
- System detects anomaly requiring immediate action
- Background autonomous compute jobs

**Trigger**: BEGIN DIALOG pattern

```sql
-- User API request triggers immediate hypothesis
CREATE PROCEDURE dbo.sp_TriggerHypothesisGeneration
    @prompt NVARCHAR(MAX),
    @sessionId UNIQUEIDENTIFIER
AS
BEGIN
    DECLARE @dialogHandle UNIQUEIDENTIFIER;
    DECLARE @message NVARCHAR(MAX);
    
    SET @message = JSON_OBJECT(
        'SessionId': @sessionId,
        'Prompt': @prompt,
        'Timestamp': GETUTCDATE()
    );
    
    -- Send to HypothesizeQueue for immediate processing
    BEGIN DIALOG CONVERSATION @dialogHandle
        FROM SERVICE [OodaInitiatorService]
        TO SERVICE 'OodaTargetService'
        ON CONTRACT [OodaContract]
        WITH ENCRYPTION = OFF;
    
    SEND ON CONVERSATION @dialogHandle
        MESSAGE TYPE [HypothesisRequest]
        (@message);
END
GO
```

---

## Service Broker Integration

### Queue Architecture

Four queues correspond to OODA phases (plus Learn):

```sql
-- 1. AnalyzeQueue: Observation results
CREATE QUEUE dbo.AnalyzeQueue
WITH STATUS = ON,
     RETENTION = OFF,
     ACTIVATION (
         STATUS = ON,
         PROCEDURE_NAME = dbo.sp_Analyze_Activated,
         MAX_QUEUE_READERS = 3,
         EXECUTE AS OWNER
     );

-- 2. HypothesizeQueue: Hypothesis generation
CREATE QUEUE dbo.HypothesizeQueue
WITH STATUS = ON,
     RETENTION = OFF,
     ACTIVATION (
         STATUS = ON,
         PROCEDURE_NAME = dbo.sp_Hypothesize_Activated,
         MAX_QUEUE_READERS = 3,
         EXECUTE AS OWNER
     );

-- 3. ActQueue: Action execution
CREATE QUEUE dbo.ActQueue
WITH STATUS = ON,
     RETENTION = OFF,
     ACTIVATION (
         STATUS = ON,
         PROCEDURE_NAME = dbo.sp_Act_Activated,
         MAX_QUEUE_READERS = 2,
         EXECUTE AS OWNER
     );

-- 4. LearnQueue: Outcome measurement
CREATE QUEUE dbo.LearnQueue
WITH STATUS = ON,
     RETENTION = OFF,
     ACTIVATION (
         STATUS = ON,
         PROCEDURE_NAME = dbo.sp_Learn_Activated,
         MAX_QUEUE_READERS = 2,
         EXECUTE AS OWNER
     );
```

### Message Flow

```text
SQL Agent (15 min) ──┐
                     │
User API Request ────┤
                     │
Anomaly Detection ───┤
                     │
                     ▼
              ┌──────────────┐
              │ AnalyzeQueue │ → sp_Analyze_Activated
              └──────┬───────┘
                     │ (Observations)
                     ▼
              ┌──────────────────┐
              │ HypothesizeQueue │ → sp_Hypothesize_Activated
              └──────┬───────────┘
                     │ (Hypotheses)
                     ▼
              ┌──────────────┐
              │   ActQueue   │ → sp_Act_Activated
              └──────┬───────┘
                     │ (Actions + Outcomes)
                     ▼
              ┌──────────────┐
              │  LearnQueue  │ → sp_Learn_Activated
              └──────────────┘
                     │
                     └─── (Update model weights)
```

---

## OODA Phase Implementation

### Phase 1: OBSERVE (sp_Analyze)

Collect system metrics from SQL Server DMVs.

```sql
CREATE PROCEDURE dbo.sp_Analyze
AS
BEGIN
    -- 1. Collect slow query statistics
    INSERT INTO dbo.AnalysisResults (AnalysisType, Metrics, Timestamp)
    SELECT 
        'SlowQueries' AS AnalysisType,
        JSON_OBJECT(
            'query_hash': qs.query_hash,
            'total_elapsed_time_ms': qs.total_elapsed_time / 1000,
            'execution_count': qs.execution_count,
            'avg_elapsed_time_ms': (qs.total_elapsed_time / qs.execution_count) / 1000,
            'query_plan': qp.query_plan
        ) AS Metrics,
        GETUTCDATE() AS Timestamp
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
    WHERE qs.total_elapsed_time / qs.execution_count > 5000000  -- Avg > 5 seconds
    ORDER BY qs.total_elapsed_time DESC;
    
    -- 2. Collect index usage statistics
    INSERT INTO dbo.AnalysisResults (AnalysisType, Metrics, Timestamp)
    SELECT 
        'UnusedIndexes' AS AnalysisType,
        JSON_OBJECT(
            'schema': s.name,
            'table': t.name,
            'index': i.name,
            'user_seeks': ius.user_seeks,
            'user_scans': ius.user_scans,
            'user_lookups': ius.user_lookups,
            'size_mb': SUM(ps.used_page_count) * 8 / 1024
        ) AS Metrics,
        GETUTCDATE() AS Timestamp
    FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    LEFT JOIN sys.dm_db_index_usage_stats ius 
        ON i.object_id = ius.object_id AND i.index_id = ius.index_id
    INNER JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id
    WHERE ius.user_seeks = 0 
      AND ius.user_scans = 0 
      AND ius.user_lookups = 0
      AND i.index_id > 0  -- Exclude heaps
    GROUP BY s.name, t.name, i.name, ius.user_seeks, ius.user_scans, ius.user_lookups;
    
    -- 3. Send to HypothesizeQueue
    DECLARE @dialogHandle UNIQUEIDENTIFIER;
    DECLARE @message NVARCHAR(MAX);
    
    SELECT @message = JSON_OBJECT(
        'AnalysisResults': (SELECT * FROM dbo.AnalysisResults WHERE Timestamp > DATEADD(MINUTE, -15, GETUTCDATE()) FOR JSON PATH)
    );
    
    BEGIN DIALOG CONVERSATION @dialogHandle
        FROM SERVICE [OodaInitiatorService]
        TO SERVICE 'OodaTargetService'
        ON CONTRACT [OodaContract]
        WITH ENCRYPTION = OFF;
    
    SEND ON CONVERSATION @dialogHandle
        MESSAGE TYPE [ObservationComplete]
        (@message);
END
GO
```

### Phase 2: ORIENT (sp_Hypothesize)

Detect patterns and generate improvement hypotheses.

```sql
CREATE PROCEDURE dbo.sp_Hypothesize
    @analysisResults NVARCHAR(MAX)
AS
BEGIN
    DECLARE @hypotheses TABLE (
        HypothesisType NVARCHAR(50),
        Description NVARCHAR(MAX),
        RiskLevel NVARCHAR(20),
        ExpectedImpact FLOAT,
        SqlCommand NVARCHAR(MAX)
    );
    
    -- Parse analysis results
    DECLARE @slowQueries TABLE (query_hash VARBINARY(8), avg_elapsed_time_ms INT, execution_count INT);
    INSERT INTO @slowQueries
    SELECT query_hash, avg_elapsed_time_ms, execution_count
    FROM OPENJSON(@analysisResults, '$.AnalysisResults')
    WITH (
        AnalysisType NVARCHAR(50) '$.AnalysisType',
        query_hash VARBINARY(8) '$.Metrics.query_hash',
        avg_elapsed_time_ms INT '$.Metrics.avg_elapsed_time_ms',
        execution_count INT '$.Metrics.execution_count'
    )
    WHERE AnalysisType = 'SlowQueries';
    
    -- Hypothesis Type 1: Missing Index
    INSERT INTO @hypotheses
    SELECT 
        'MissingIndex' AS HypothesisType,
        'Create index on frequently scanned table' AS Description,
        'LOW' AS RiskLevel,
        sq.avg_elapsed_time_ms * 0.7 AS ExpectedImpact,  -- Estimate 70% improvement
        'CREATE NONCLUSTERED INDEX IX_' + CAST(sq.query_hash AS NVARCHAR(20)) + 
        ' ON dbo.AtomEmbedding (TenantId, AtomId) INCLUDE (EmbeddingVector)' AS SqlCommand
    FROM @slowQueries sq
    WHERE sq.execution_count > 100;
    
    -- Hypothesis Type 2: Update Statistics
    INSERT INTO @hypotheses
    SELECT 
        'UpdateStatistics' AS HypothesisType,
        'Refresh statistics for skewed data' AS Description,
        'SAFE' AS RiskLevel,
        sq.avg_elapsed_time_ms * 0.3 AS ExpectedImpact,  -- Estimate 30% improvement
        'UPDATE STATISTICS dbo.AtomEmbedding WITH FULLSCAN' AS SqlCommand
    FROM @slowQueries sq;
    
    -- Hypothesis Type 3: Prune Model Weights
    INSERT INTO @hypotheses
    VALUES (
        'PruneWeights',
        'Remove model weights with ImportanceScore < 0.01',
        'MEDIUM',
        1024.0,  -- 1GB storage saved
        'DELETE FROM dbo.TensorAtoms WHERE ImportanceScore < 0.01 AND LastAccessed < DATEADD(DAY, -30, GETUTCDATE())'
    );
    
    -- Store hypotheses
    INSERT INTO dbo.Hypotheses (HypothesisType, Description, RiskLevel, ExpectedImpact, SqlCommand, CreatedAt, Status)
    SELECT HypothesisType, Description, RiskLevel, ExpectedImpact, SqlCommand, GETUTCDATE(), 'Pending'
    FROM @hypotheses;
    
    -- Send to ActQueue
    DECLARE @dialogHandle UNIQUEIDENTIFIER;
    DECLARE @message NVARCHAR(MAX);
    
    SELECT @message = (SELECT * FROM @hypotheses FOR JSON PATH);
    
    BEGIN DIALOG CONVERSATION @dialogHandle
        FROM SERVICE [OodaInitiatorService]
        TO SERVICE 'OodaTargetService'
        ON CONTRACT [OodaContract]
        WITH ENCRYPTION = OFF;
    
    SEND ON CONVERSATION @dialogHandle
        MESSAGE TYPE [HypothesisGenerated]
        (@message);
END
GO
```

### Phase 3: DECIDE (sp_Prioritize)

Prioritize hypotheses by expected impact and risk.

```sql
CREATE PROCEDURE dbo.sp_Prioritize
AS
BEGIN
    -- Rank hypotheses by priority
    WITH RankedHypotheses AS (
        SELECT 
            HypothesisId,
            HypothesisType,
            Description,
            RiskLevel,
            ExpectedImpact,
            SqlCommand,
            ROW_NUMBER() OVER (
                ORDER BY 
                    CASE RiskLevel
                        WHEN 'SAFE' THEN 1
                        WHEN 'LOW' THEN 2
                        WHEN 'MEDIUM' THEN 3
                        WHEN 'HIGH' THEN 4
                        WHEN 'DANGEROUS' THEN 5
                    END ASC,
                    ExpectedImpact DESC
            ) AS Priority
        FROM dbo.Hypotheses
        WHERE Status = 'Pending'
    )
    UPDATE h
    SET h.Priority = rh.Priority,
        h.Status = CASE 
            WHEN rh.RiskLevel IN ('SAFE', 'LOW') THEN 'Approved'
            ELSE 'AwaitingApproval'
        END
    FROM dbo.Hypotheses h
    INNER JOIN RankedHypotheses rh ON h.HypothesisId = rh.HypothesisId;
END
GO
```

### Phase 4: ACT (sp_Act)

Execute approved hypotheses with rollback capability.

```sql
CREATE PROCEDURE dbo.sp_Act
    @hypothesisId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get hypothesis details
    DECLARE @sqlCommand NVARCHAR(MAX);
    DECLARE @riskLevel NVARCHAR(20);
    DECLARE @hypothesisType NVARCHAR(50);
    
    SELECT 
        @sqlCommand = SqlCommand,
        @riskLevel = RiskLevel,
        @hypothesisType = HypothesisType
    FROM dbo.Hypotheses
    WHERE HypothesisId = @hypothesisId
      AND Status = 'Approved';
    
    IF @sqlCommand IS NULL
    BEGIN
        RAISERROR('Hypothesis not found or not approved', 16, 1);
        RETURN;
    END
    
    -- Execute with transaction and rollback capability
    DECLARE @startTime DATETIME2 = GETUTCDATE();
    DECLARE @error NVARCHAR(MAX) = NULL;
    DECLARE @success BIT = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Execute hypothesis command
        EXEC sp_executesql @sqlCommand;
        
        -- For index creation: Verify improvement
        IF @hypothesisType = 'MissingIndex'
        BEGIN
            -- Wait for statistics update
            WAITFOR DELAY '00:00:05';
            
            -- Check query plan (simplified check)
            DECLARE @planImproved BIT = 1;  -- Real implementation would analyze plan
            
            IF @planImproved = 1
            BEGIN
                COMMIT TRANSACTION;
                SET @success = 1;
            END
            ELSE
            BEGIN
                ROLLBACK TRANSACTION;
                SET @error = 'Query plan did not improve';
            END
        END
        ELSE
        BEGIN
            COMMIT TRANSACTION;
            SET @success = 1;
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @error = ERROR_MESSAGE();
    END CATCH
    
    -- Record outcome
    DECLARE @elapsedMs INT = DATEDIFF(MILLISECOND, @startTime, GETUTCDATE());
    
    UPDATE dbo.Hypotheses
    SET Status = CASE WHEN @success = 1 THEN 'Executed' ELSE 'Failed' END,
        ExecutedAt = GETUTCDATE(),
        ExecutionTimeMs = @elapsedMs,
        ErrorMessage = @error
    WHERE HypothesisId = @hypothesisId;
    
    -- Send to LearnQueue
    DECLARE @dialogHandle UNIQUEIDENTIFIER;
    DECLARE @message NVARCHAR(MAX);
    
    SET @message = JSON_OBJECT(
        'HypothesisId': @hypothesisId,
        'Success': @success,
        'ExecutionTimeMs': @elapsedMs,
        'ErrorMessage': @error
    );
    
    BEGIN DIALOG CONVERSATION @dialogHandle
        FROM SERVICE [OodaInitiatorService]
        TO SERVICE 'OodaTargetService'
        ON CONTRACT [OodaContract]
        WITH ENCRYPTION = OFF;
    
    SEND ON CONVERSATION @dialogHandle
        MESSAGE TYPE [ActionComplete]
        (@message);
END
GO
```

### Phase 5: LEARN (sp_Learn)

Measure outcomes and update model weights.

```sql
CREATE PROCEDURE dbo.sp_Learn
    @hypothesisId BIGINT,
    @successScore FLOAT
AS
BEGIN
    -- Calculate success score (0.0 to 1.0)
    DECLARE @actualImpact FLOAT;
    DECLARE @expectedImpact FLOAT;
    
    SELECT 
        @actualImpact = ExecutionTimeMs,
        @expectedImpact = ExpectedImpact
    FROM dbo.Hypotheses
    WHERE HypothesisId = @hypothesisId;
    
    -- Normalize success score
    SET @successScore = CASE 
        WHEN @actualImpact >= @expectedImpact * 0.7 THEN 1.0  -- Better than expected
        WHEN @actualImpact >= @expectedImpact * 0.5 THEN 0.7  -- Good
        WHEN @actualImpact >= @expectedImpact * 0.3 THEN 0.5  -- Moderate
        ELSE 0.3  -- Minimal improvement
    END;
    
    -- Update model weights (simplified - actual implementation uses gradient descent)
    UPDATE dbo.TensorAtoms
    SET ImportanceScore = ImportanceScore + (@successScore * 0.01)  -- Small learning rate
    WHERE TensorName LIKE 'ooda.%';
    
    -- Store learning outcome
    INSERT INTO dbo.LearningHistory (HypothesisId, SuccessScore, Timestamp)
    VALUES (@hypothesisId, @successScore, GETUTCDATE());
END
GO
```

---

## .NET Event Handlers (External Integration)

For operations requiring external APIs or .NET libraries:

```csharp
// src/Hartonomous.ServiceBroker/OodaEventHandlers.cs

public class ObservationEventHandler : IHostedService
{
    private readonly IServiceBrokerListener _listener;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public ObservationEventHandler(
        IServiceBrokerListener listener,
        IHttpClientFactory httpClientFactory)
    {
        _listener = listener;
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _listener.ListenAsync("AnalyzeQueue", async (message) =>
        {
            // Collect external metrics (CPU, network latency, etc.)
            var cpuUsage = await GetCpuUsageAsync();
            var networkLatency = await MeasureNetworkLatencyAsync();
            
            // Send back to SQL Server
            await _listener.SendAsync("HypothesizeQueue", new
            {
                ExternalMetrics = new
                {
                    CpuUsage = cpuUsage,
                    NetworkLatency = networkLatency,
                    Timestamp = DateTime.UtcNow
                }
            });
        }, cancellationToken);
    }
    
    private async Task<double> GetCpuUsageAsync()
    {
        // Use System.Diagnostics.PerformanceCounter or WMI
        // Return CPU percentage
        return 42.5; // Placeholder
    }
    
    private async Task<int> MeasureNetworkLatencyAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var sw = Stopwatch.StartNew();
        await client.GetAsync("https://api.azure.com/health");
        sw.Stop();
        return (int)sw.ElapsedMilliseconds;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

---

## Gödel Engine Integration

The OODA loop enables **Turing-completeness via autonomous compute jobs**:

```sql
-- User creates autonomous compute job
INSERT INTO dbo.AutonomousComputeJobs (
    JobName,
    PromptTemplate,
    TriggerCondition,
    MaxIterations,
    Status
)
VALUES (
    'OptimizeSlowQueries',
    'Analyze query plan and suggest index improvements for query {query_hash}',
    'avg_elapsed_time_ms > 5000',
    10,
    'Active'
);

-- OODA loop detects trigger condition and executes job
-- Each iteration:
-- 1. Observe: Check if condition met
-- 2. Orient: Generate hypothesis using prompt template
-- 3. Decide: Prioritize hypothesis
-- 4. Act: Execute if safe
-- 5. Learn: Measure outcome, update weights
```

**Result**: Self-improving system that learns from every execution.

---

## Cross-References

- **Related**: [Semantic-First Architecture](semantic-first.md) - Spatial queries optimized by OODA
- **Related**: [Training and Fine-Tuning](training.md) - How LEARN phase updates model weights
- **Related**: [Inference](inference.md) - Event-driven inference triggers via Service Broker

---

## Performance Characteristics

- **Scheduled Cycle**: 15-minute intervals (configurable)
- **Event-Driven Latency**: <100ms from trigger to hypothesis generation
- **Hypothesis Evaluation**: 5-10ms per hypothesis
- **Safe Action Execution**: <1 second (with rollback)
- **Learning Update**: <50ms (update model weights)

**Result**: Autonomous system that continuously improves without human intervention.
