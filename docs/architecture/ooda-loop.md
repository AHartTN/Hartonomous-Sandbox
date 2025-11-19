# OODA Autonomous Loop Architecture

**Self-Improving System via Observe-Orient-Decide-Act-Learn Cycle**

## Overview

Hartonomous implements the **OODA loop** (Observe-Orient-Decide-Act) + **Learn phase** as a fully autonomous system capable of continuous self-improvement without human intervention. The system:

1. **Observes** performance metrics, query patterns, and anomalies
2. **Orients** by generating hypotheses about potential improvements
3. **Decides** which hypotheses to execute based on risk assessment
4. **Acts** by autonomously executing safe improvements (or queuing risky ones for approval)
5. **Learns** by measuring outcomes and updating model weights

**Key Innovation**: Achieves **Turing-completeness** via `AutonomousComputeJobs` - arbitrary computation requests can be autonomously planned, executed, and refined.

## Dual-Triggering Architecture

The OODA loop operates on **two independent triggers** to ensure both predictability and responsiveness:

### 1. Scheduled Trigger (SQL Server Agent)

**Frequency**: Every 15 minutes (configurable: 5-60 minutes)

**Implementation**:

```sql
-- Create SQL Agent job
EXEC msdb.dbo.sp_add_job 
    @job_name = 'OodaCycle_15min',
    @description = 'Autonomous OODA loop execution';

-- Add execution step
EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'OodaCycle_15min',
    @step_name = 'Trigger_Analyze',
    @command = '
        -- Send message to AnalyzeQueue
        DECLARE @handle UNIQUEIDENTIFIER;
        BEGIN DIALOG CONVERSATION @handle
            FROM SERVICE [//Hartonomous/InitiatorService]
            TO SERVICE ''//Hartonomous/AnalyzeService''
            ON CONTRACT [//Hartonomous/OodaContract];
        
        SEND ON CONVERSATION @handle
            MESSAGE TYPE [//Hartonomous/Analyze] ('''');
    ';

-- Create schedule (every 15 minutes, 24/7)
EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'Every15Minutes',
    @freq_type = 4,              -- Daily
    @freq_interval = 1,          -- Every day
    @freq_subday_type = 4,       -- Minutes
    @freq_subday_interval = 15;

-- Attach schedule to job
EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'OodaCycle_15min',
    @schedule_name = 'Every15Minutes';

-- Start job
EXEC msdb.dbo.sp_start_job @job_name = 'OodaCycle_15min';
```

**Advantages**:
- **Predictable**: Runs at known intervals for baseline monitoring
- **Low latency**: 15-minute response time for non-critical optimizations
- **Resilient**: SQL Agent handles retries and failure tracking

### 2. Event-Driven Trigger (Service Broker Internal Activation)

**Trigger**: Significant system events (e.g., >20% latency spike, new data source ingestion, index fragmentation >30%)

**Implementation**:

```sql
-- Event publisher (called from sp_IngestModel, sp_Analyze, etc.)
CREATE PROCEDURE dbo.sp_PublishOodaEvent
    @EventType NVARCHAR(50),        -- 'PerformanceRegression', 'NewDataSource', 'IndexFragmentation'
    @Severity NVARCHAR(20),         -- 'Low', 'Medium', 'High', 'Critical'
    @Details NVARCHAR(MAX)          -- JSON metadata
AS
BEGIN
    -- Only trigger for Medium+ severity
    IF @Severity IN ('Medium', 'High', 'Critical')
    BEGIN
        DECLARE @handle UNIQUEIDENTIFIER;
        BEGIN DIALOG CONVERSATION @handle
            FROM SERVICE [//Hartonomous/EventPublisher]
            TO SERVICE '//Hartonomous/AnalyzeService'
            ON CONTRACT [//Hartonomous/OodaContract];
        
        SEND ON CONVERSATION @handle
            MESSAGE TYPE [//Hartonomous/Analyze] (
                JSON_OBJECT(
                    'eventType': @EventType,
                    'severity': @Severity,
                    'details': JSON_QUERY(@Details)
                )
            );
    END
END;
```

**Usage Examples**:

```sql
-- Trigger after detecting latency regression
IF @currentLatency > (@baselineLatency * 1.2)
BEGIN
    EXEC dbo.sp_PublishOodaEvent 
        @EventType = 'PerformanceRegression',
        @Severity = 'High',
        @Details = '{"metric": "avgLatencyMs", "baseline": 45.2, "current": 87.3}';
END;

-- Trigger after ingesting large model
IF @atomsCreated > 1000000
BEGIN
    EXEC dbo.sp_PublishOodaEvent 
        @EventType = 'NewDataSource',
        @Severity = 'Medium',
        @Details = '{"sourceType": "GGUF", "atomCount": 1250000}';
END;
```

**Advantages**:
- **Responsive**: Immediate reaction to critical issues (no 15-minute delay)
- **Selective**: Only triggers for significant events (reduces noise)
- **Context-aware**: Event metadata provides rich context for hypothesis generation

### Combined Strategy

```
Scheduled (15-min)    Event-Driven (immediate)
    ↓                       ↓
    └─────── OR ───────────┘
              ↓
        sp_Analyze
              ↓
      sp_Hypothesize
              ↓
          sp_Act
              ↓
        sp_Learn
              ↓
      [Loop back after 15 min OR on next event]
```

**Result**: System responds within 15 minutes for baseline monitoring, but can react **immediately** to critical issues.

## Service Broker Architecture

### Queues & Services

```sql
-- Queue 1: Observations (Observe & Orient phases)
CREATE QUEUE dbo.AnalyzeQueue;
CREATE SERVICE [//Hartonomous/AnalyzeService]
    ON QUEUE dbo.AnalyzeQueue ([//Hartonomous/Analyze]);

-- Queue 2: Hypotheses (Decide phase)
CREATE QUEUE dbo.HypothesizeQueue;
CREATE SERVICE [//Hartonomous/HypothesizeService]
    ON QUEUE dbo.HypothesizeQueue ([//Hartonomous/Hypothesize]);

-- Queue 3: Actions (Act phase)
CREATE QUEUE dbo.ActQueue;
CREATE SERVICE [//Hartonomous/ActService]
    ON QUEUE dbo.ActQueue ([//Hartonomous/Act]);

-- Queue 4: Learning (Learn phase)
CREATE QUEUE dbo.LearnQueue;
CREATE SERVICE [//Hartonomous/LearnService]
    ON QUEUE dbo.LearnQueue ([//Hartonomous/Learn]);
```

### Internal Activation

**Automatic Procedure Execution**:

```sql
-- When message arrives in AnalyzeQueue, run sp_Analyze automatically
ALTER QUEUE dbo.AnalyzeQueue WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_Analyze,
    MAX_QUEUE_READERS = 1,           -- Single-threaded execution
    EXECUTE AS OWNER
);

-- Similar for other queues
ALTER QUEUE dbo.HypothesizeQueue WITH ACTIVATION (
    PROCEDURE_NAME = dbo.sp_Hypothesize, ...
);

ALTER QUEUE dbo.ActQueue WITH ACTIVATION (
    PROCEDURE_NAME = dbo.sp_Act, ...
);

ALTER QUEUE dbo.LearnQueue WITH ACTIVATION (
    PROCEDURE_NAME = dbo.sp_Learn, ...
);
```

**Benefits**:
- **Zero latency**: Procedures run immediately when messages arrive
- **Transactional**: Message processing is atomic (all-or-nothing)
- **Guaranteed delivery**: Service Broker ensures messages never lost
- **Native**: No external dependencies (pure SQL Server)

## OODA Phases Detailed

### Phase 1: sp_Analyze (Observe & Orient)

**Purpose**: Monitor system state and detect anomalies

**Metrics Collected**:

1. **Query Performance** (from Query Store):
```sql
DECLARE @slowQueries TABLE (
    QueryId BIGINT,
    AvgDurationMs FLOAT,
    ExecutionCount BIGINT,
    LastExecutionTime DATETIME2
);

INSERT INTO @slowQueries
SELECT
    q.query_id,
    rs.avg_duration / 1000.0 AS avg_duration_ms,
    rs.count_executions,
    rs.last_execution_time
FROM sys.query_store_query q
INNER JOIN sys.query_store_plan qsp ON q.query_id = qsp.query_id
INNER JOIN sys.query_store_runtime_stats rs ON qsp.plan_id = rs.plan_id
WHERE rs.last_execution_time >= DATEADD(HOUR, -1, GETUTCDATE())
    AND rs.avg_duration > 100000  -- 100ms threshold
ORDER BY rs.avg_duration DESC;
```

2. **Performance Regression Detection**:
```sql
-- Compare current vs. baseline latency
DECLARE @baselineLatency FLOAT = (
    SELECT AVG(TotalDurationMs)
    FROM dbo.InferenceRequests
    WHERE RequestTimestamp BETWEEN DATEADD(DAY, -7, GETUTCDATE()) AND DATEADD(DAY, -1, GETUTCDATE())
);

DECLARE @currentLatency FLOAT = (
    SELECT AVG(TotalDurationMs)
    FROM dbo.InferenceRequests
    WHERE RequestTimestamp >= DATEADD(HOUR, -1, GETUTCDATE())
);

DECLARE @regressionPercent FLOAT = ((@currentLatency - @baselineLatency) / @baselineLatency) * 100;

IF @regressionPercent > 20
BEGIN
    -- Significant regression detected
    INSERT INTO @observations (ObservationType, Severity, Details)
    VALUES ('PerformanceRegression', 'High', JSON_OBJECT(
        'regressionPercent': @regressionPercent,
        'baseline': @baselineLatency,
        'current': @currentLatency
    ));
END
```

3. **Index Fragmentation**:
```sql
-- Detect heavily fragmented indexes
INSERT INTO @observations (ObservationType, Severity, Details)
SELECT 
    'IndexFragmentation',
    CASE WHEN avg_fragmentation_in_percent > 60 THEN 'High'
         WHEN avg_fragmentation_in_percent > 30 THEN 'Medium'
         ELSE 'Low' END,
    JSON_OBJECT(
        'indexName': i.name,
        'tableName': o.name,
        'fragmentation': ROUND(avg_fragmentation_in_percent, 2)
    )
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') ps
INNER JOIN sys.indexes i ON ps.object_id = i.object_id AND ps.index_id = i.index_id
INNER JOIN sys.objects o ON i.object_id = o.object_id
WHERE avg_fragmentation_in_percent > 30
    AND page_count > 1000;  -- Ignore small indexes
```

4. **Semantic Cluster Discovery** (CLR):
```sql
-- Detect emergent semantic clusters in recent atoms
DECLARE @newClusters NVARCHAR(MAX) = dbo.clr_DbscanClustering_JSON(
    @minPts = 10,
    @epsilon = 0.15,
    @timeWindowHours = 24
);

IF JSON_VALUE(@newClusters, '$.clusterCount') > 0
BEGIN
    INSERT INTO @observations (ObservationType, Severity, Details)
    VALUES ('SemanticClusters', 'Medium', @newClusters);
END
```

**Output**: JSON observations sent to HypothesizeQueue

```json
{
  "analysisId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-15T10:30:00Z",
  "anomalies": [
    {
      "type": "PerformanceRegression",
      "severity": "High",
      "metric": "avgLatencyMs",
      "baseline": 45.2,
      "current": 87.3,
      "change": "+93%"
    },
    {
      "type": "IndexFragmentation",
      "severity": "Medium",
      "indexName": "idx_AtomEmbeddings_SpatialKey",
      "tableName": "AtomEmbeddings",
      "fragmentation": 42.7
    }
  ],
  "patterns": [
    {
      "type": "SemanticClusters",
      "count": 5,
      "description": "Detected 5 emergent semantic clusters in recent atoms"
    }
  ]
}
```

### Phase 2: sp_Hypothesize (Decide)

**Purpose**: Generate improvement hypotheses based on observations

**7 Hypothesis Types**:

1. **IndexOptimization**: Create/rebuild/reorganize indexes
2. **ConceptDiscovery**: Add landmark for new semantic cluster
3. **PruneModel**: Remove low-importance atoms
4. **UpdateEmbeddings**: Recompute embeddings for stale atoms
5. **StatisticsUpdate**: Refresh query optimizer statistics
6. **CompressionTuning**: Adjust SVD compression rank
7. **CachePriming**: Preload frequently accessed data

**Hypothesis Generation Logic**:

```sql
CREATE PROCEDURE dbo.sp_Hypothesize
AS
BEGIN
    -- Receive observations from AnalyzeQueue
    DECLARE @observationsJson NVARCHAR(MAX);
    DECLARE @handle UNIQUEIDENTIFIER;
    
    RECEIVE TOP(1) 
        @handle = conversation_handle,
        @observationsJson = CAST(message_body AS NVARCHAR(MAX))
    FROM dbo.HypothesizeQueue;
    
    -- Parse observations
    DECLARE @hypotheses TABLE (
        HypothesisId UNIQUEIDENTIFIER DEFAULT NEWID(),
        HypothesisType NVARCHAR(50),
        ActionSQL NVARCHAR(MAX),
        EstimatedImpact FLOAT,
        RiskLevel NVARCHAR(20),
        Priority INT
    );
    
    -- Rule 1: PerformanceRegression → IndexOptimization
    IF EXISTS (
        SELECT 1 FROM OPENJSON(@observationsJson, '$.anomalies')
        WHERE JSON_VALUE(value, '$.type') = 'PerformanceRegression'
    )
    BEGIN
        INSERT INTO @hypotheses (HypothesisType, ActionSQL, EstimatedImpact, RiskLevel, Priority)
        VALUES (
            'IndexOptimization',
            'ALTER INDEX idx_AtomEmbeddings_SpatialKey ON dbo.AtomEmbeddings REBUILD WITH (ONLINE = ON);',
            0.30,  -- 30% latency improvement expected
            'Low',
            1      -- High priority
        );
    END;
    
    -- Rule 2: SemanticClusters → ConceptDiscovery
    IF EXISTS (
        SELECT 1 FROM OPENJSON(@observationsJson, '$.patterns')
        WHERE JSON_VALUE(value, '$.type') = 'SemanticClusters'
    )
    BEGIN
        INSERT INTO @hypotheses (HypothesisType, ActionSQL, EstimatedImpact, RiskLevel, Priority)
        VALUES (
            'ConceptDiscovery',
            'EXEC dbo.sp_AddLandmarkFromCluster @ClusterId = <id>, @LandmarkName = ''NewConcept_<timestamp>'';',
            0.15,  -- 15% query improvement for cluster queries
            'Medium',
            2
        );
    END;
    
    -- Rule 3: IndexFragmentation → IndexOptimization
    IF EXISTS (
        SELECT 1 FROM OPENJSON(@observationsJson, '$.anomalies')
        WHERE JSON_VALUE(value, '$.type') = 'IndexFragmentation'
          AND CAST(JSON_VALUE(value, '$.fragmentation') AS FLOAT) > 60
    )
    BEGIN
        INSERT INTO @hypotheses (HypothesisType, ActionSQL, EstimatedImpact, RiskLevel, Priority)
        VALUES (
            'IndexOptimization',
            'ALTER INDEX <indexName> ON <tableName> REBUILD;',
            0.20,
            'Low',
            1
        );
    END;
    
    -- Rank hypotheses by EstimatedImpact * (1 / RiskScore)
    UPDATE @hypotheses
    SET Priority = ROW_NUMBER() OVER (
        ORDER BY 
            EstimatedImpact DESC,
            CASE RiskLevel WHEN 'Low' THEN 1 WHEN 'Medium' THEN 2 WHEN 'High' THEN 3 END ASC
    );
    
    -- Send top 5 hypotheses to ActQueue
    DECLARE @hypothesesJson NVARCHAR(MAX) = (
        SELECT TOP 5 * FROM @hypotheses ORDER BY Priority FOR JSON PATH
    );
    
    DECLARE @actHandle UNIQUEIDENTIFIER;
    BEGIN DIALOG CONVERSATION @actHandle
        FROM SERVICE [//Hartonomous/HypothesizeService]
        TO SERVICE '//Hartonomous/ActService'
        ON CONTRACT [//Hartonomous/OodaContract];
    
    SEND ON CONVERSATION @actHandle
        MESSAGE TYPE [//Hartonomous/Act] (@hypothesesJson);
    
    END CONVERSATION @handle;
END;
```

### Phase 3: sp_Act (Act)

**Purpose**: Execute hypotheses autonomously or queue for approval

**Risk-Based Execution**:

```sql
CREATE PROCEDURE dbo.sp_Act
AS
BEGIN
    -- Receive hypotheses from HypothesizeQueue
    DECLARE @hypothesesJson NVARCHAR(MAX);
    DECLARE @handle UNIQUEIDENTIFIER;
    
    RECEIVE TOP(1) 
        @handle = conversation_handle,
        @hypothesesJson = CAST(message_body AS NVARCHAR(MAX))
    FROM dbo.ActQueue;
    
    -- Process each hypothesis
    DECLARE @hypothesis TABLE (
        HypothesisId UNIQUEIDENTIFIER,
        HypothesisType NVARCHAR(50),
        ActionSQL NVARCHAR(MAX),
        RiskLevel NVARCHAR(20)
    );
    
    INSERT INTO @hypothesis
    SELECT 
        JSON_VALUE(value, '$.HypothesisId'),
        JSON_VALUE(value, '$.HypothesisType'),
        JSON_VALUE(value, '$.ActionSQL'),
        JSON_VALUE(value, '$.RiskLevel')
    FROM OPENJSON(@hypothesesJson);
    
    -- Execute Low risk hypotheses automatically
    DECLARE @actionSQL NVARCHAR(MAX);
    DECLARE @hypothesisId UNIQUEIDENTIFIER;
    DECLARE @riskLevel NVARCHAR(20);
    
    DECLARE hypothesis_cursor CURSOR FOR
        SELECT HypothesisId, ActionSQL, RiskLevel 
        FROM @hypothesis 
        WHERE RiskLevel = 'Low';
    
    OPEN hypothesis_cursor;
    FETCH NEXT FROM hypothesis_cursor INTO @hypothesisId, @actionSQL, @riskLevel;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Execute action
            EXEC sp_executesql @actionSQL;
            
            -- Log success
            INSERT INTO dbo.OodaExecutionLog (
                HypothesisId, ExecutedAt, Status, ErrorMessage
            )
            VALUES (@hypothesisId, SYSUTCDATETIME(), 'Success', NULL);
            
        END TRY
        BEGIN CATCH
            -- Log failure
            INSERT INTO dbo.OodaExecutionLog (
                HypothesisId, ExecutedAt, Status, ErrorMessage
            )
            VALUES (@hypothesisId, SYSUTCDATETIME(), 'Failed', ERROR_MESSAGE());
        END CATCH;
        
        FETCH NEXT FROM hypothesis_cursor INTO @hypothesisId, @actionSQL, @riskLevel;
    END;
    
    CLOSE hypothesis_cursor;
    DEALLOCATE hypothesis_cursor;
    
    -- Queue Medium/High risk hypotheses for human approval
    INSERT INTO dbo.PendingApprovals (HypothesisId, ActionSQL, RiskLevel, QueuedAt)
    SELECT HypothesisId, ActionSQL, RiskLevel, SYSUTCDATETIME()
    FROM @hypothesis
    WHERE RiskLevel IN ('Medium', 'High');
    
    -- Send execution results to LearnQueue
    DECLARE @learnHandle UNIQUEIDENTIFIER;
    BEGIN DIALOG CONVERSATION @learnHandle
        FROM SERVICE [//Hartonomous/ActService]
        TO SERVICE '//Hartonomous/LearnService'
        ON CONTRACT [//Hartonomous/OodaContract];
    
    SEND ON CONVERSATION @learnHandle
        MESSAGE TYPE [//Hartonomous/Learn] (
            (SELECT * FROM dbo.OodaExecutionLog WHERE ExecutedAt >= DATEADD(MINUTE, -5, SYSUTCDATETIME()) FOR JSON PATH)
        );
    
    END CONVERSATION @handle;
END;
```

**Execution Policy**:

| Risk Level | Action | Approval Required | Notification |
|------------|--------|-------------------|--------------|
| **Low** | Execute immediately | ❌ No | ✅ Log only |
| **Medium** | Queue for approval | ✅ Yes | ✅ Email alert |
| **High** | Queue for approval | ✅ Yes + second approver | ✅ SMS + email |
| **Critical** | Never auto-execute | ✅ Yes + manager approval | ✅ Incident ticket |

### Phase 4: sp_Learn (Learn)

**Purpose**: Measure outcomes and update hypothesis weights

```sql
CREATE PROCEDURE dbo.sp_Learn
AS
BEGIN
    -- Receive execution results from ActQueue
    DECLARE @resultsJson NVARCHAR(MAX);
    DECLARE @handle UNIQUEIDENTIFIER;
    
    RECEIVE TOP(1) 
        @handle = conversation_handle,
        @resultsJson = CAST(message_body AS NVARCHAR(MAX))
    FROM dbo.LearnQueue;
    
    -- Measure outcomes
    DECLARE @outcomes TABLE (
        HypothesisId UNIQUEIDENTIFIER,
        HypothesisType NVARCHAR(50),
        ActualImpact FLOAT,
        Success BIT
    );
    
    INSERT INTO @outcomes (HypothesisId, HypothesisType, Success)
    SELECT 
        JSON_VALUE(value, '$.HypothesisId'),
        JSON_VALUE(value, '$.HypothesisType'),
        CASE WHEN JSON_VALUE(value, '$.Status') = 'Success' THEN 1 ELSE 0 END
    FROM OPENJSON(@resultsJson);
    
    -- Measure actual impact (e.g., latency improvement for IndexOptimization)
    UPDATE o
    SET ActualImpact = (
        SELECT ((old.AvgLatency - new.AvgLatency) / old.AvgLatency) * 100
        FROM (
            SELECT AVG(TotalDurationMs) AS AvgLatency
            FROM dbo.InferenceRequests
            WHERE RequestTimestamp BETWEEN DATEADD(HOUR, -2, SYSUTCDATETIME()) AND DATEADD(HOUR, -1, SYSUTCDATETIME())
        ) old
        CROSS JOIN (
            SELECT AVG(TotalDurationMs) AS AvgLatency
            FROM dbo.InferenceRequests
            WHERE RequestTimestamp >= DATEADD(HOUR, -1, SYSUTCDATETIME())
        ) new
    )
    FROM @outcomes o
    WHERE o.HypothesisType = 'IndexOptimization';
    
    -- Update hypothesis weights (Bayesian update)
    UPDATE hw
    SET 
        SuccessCount = hw.SuccessCount + CASE WHEN o.Success = 1 THEN 1 ELSE 0 END,
        FailureCount = hw.FailureCount + CASE WHEN o.Success = 0 THEN 1 ELSE 0 END,
        AvgImpact = (hw.AvgImpact * hw.TotalExecutions + ISNULL(o.ActualImpact, 0)) / (hw.TotalExecutions + 1),
        TotalExecutions = hw.TotalExecutions + 1,
        ConfidenceScore = CAST(hw.SuccessCount + 1 AS FLOAT) / (hw.TotalExecutions + 2)  -- Laplace smoothing
    FROM dbo.HypothesisWeights hw
    INNER JOIN @outcomes o ON hw.HypothesisType = o.HypothesisType;
    
    END CONVERSATION @handle;
    
    -- Loop back: Trigger sp_Analyze after Learn completes
    -- (Either wait for next 15-min schedule OR publish event if critical issue)
END;
```

**Learning Metrics**:

```sql
-- View hypothesis performance
SELECT 
    HypothesisType,
    SuccessCount,
    FailureCount,
    ConfidenceScore,
    AvgImpact,
    TotalExecutions,
    LastUpdated
FROM dbo.HypothesisWeights
ORDER BY ConfidenceScore DESC, AvgImpact DESC;
```

| HypothesisType | SuccessCount | FailureCount | ConfidenceScore | AvgImpact | TotalExecutions |
|----------------|--------------|--------------|-----------------|-----------|-----------------|
| IndexOptimization | 47 | 3 | 0.94 | 28.5% | 50 |
| ConceptDiscovery | 12 | 2 | 0.86 | 15.2% | 14 |
| StatisticsUpdate | 31 | 4 | 0.89 | 12.3% | 35 |
| PruneModel | 8 | 1 | 0.89 | 8.7% | 9 |

## Performance Characteristics

**Cycle Time**:
- **Scheduled**: 15-minute baseline loop
- **Event-driven**: <500ms from event to sp_Analyze start
- **sp_Analyze**: 2-8 seconds (depending on metrics collected)
- **sp_Hypothesize**: 1-3 seconds (hypothesis generation)
- **sp_Act**: 5-300 seconds (depending on actions executed)
- **sp_Learn**: 1-2 seconds (metric updates)

**Total Latency**: 
- Scheduled cycle: 15 min + 10-315 sec execution ≈ 15-20 minutes
- Event-driven cycle: <5 minutes (immediate trigger + execution)

**Throughput**:
- **Low risk actions**: Auto-executed (0 approval latency)
- **Medium risk actions**: 1-4 hours (human approval)
- **High risk actions**: 4-24 hours (second approver)

## Gödel Engine: Turing-Complete Computation

**Concept**: The OODA loop can autonomously plan and execute arbitrary computations via `AutonomousComputeJobs`.

**Example Use Case**: User requests "Find all atoms semantically similar to 'neural architecture search' within 5 hops of a specific source"

**Autonomous Execution**:

1. **Observe**: User submits compute job via API
2. **Orient**: sp_Hypothesize generates multi-step execution plan:
   ```
   Step 1: Embed query text "neural architecture search"
   Step 2: Spatial search (O(log N)) for K=100 candidates
   Step 3: CLR cosine refinement (O(K)) to K=20
   Step 4: Neo4j graph traversal (5 hops from SourceId=XYZ)
   Step 5: Intersection (spatial results ∩ graph results)
   ```
3. **Decide**: Rank steps by computational cost
4. **Act**: Execute steps sequentially (or parallelize independent steps)
5. **Learn**: Measure total execution time, update cost model for future jobs

**Turing-Completeness**: Arbitrary computation can be decomposed into:
- SQL queries (relational operations)
- CLR functions (procedural logic)
- Neo4j traversals (graph operations)
- External worker calls (GPU, API)

## Summary

The OODA autonomous loop provides:

1. **Dual-Triggering**: Scheduled (15-min predictable) + Event-driven (immediate responsive)
2. **Zero Latency**: Service Broker internal activation executes procedures instantly
3. **Risk-Based Execution**: Auto-execute low-risk, queue high-risk for approval
4. **Continuous Learning**: Bayesian updates to hypothesis weights based on measured outcomes
5. **Turing-Completeness**: Arbitrary computation via AutonomousComputeJobs
6. **7 Hypothesis Types**: Index optimization, concept discovery, pruning, embeddings, statistics, compression, caching

**Key Innovation**: System improves itself faster than it degrades, achieving **perpetual self-optimization** without human intervention (except for high-risk actions).
