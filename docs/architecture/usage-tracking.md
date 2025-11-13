# Usage Tracking, Caching, and Queue Management

**Date:** November 12, 2025  
**Scope:** System-wide resource optimization, prioritization, and performance strategies

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Usage Tracking Architecture](#usage-tracking-architecture)
3. [Model Layer Caching Strategy](#model-layer-caching-strategy)
4. [Hot vs Cold Queue Management](#hot-vs-cold-queue-management)
5. [Request Prioritization System](#request-prioritization-system)
6. [Tenant Resource Allocation](#tenant-resource-allocation)
7. [Cost Optimization & DCU Efficiency](#cost-optimization--dcu-efficiency)
8. [Database Schema](#database-schema)
9. [Implementation Examples](#implementation-examples)

---

## Executive Summary

### The Challenge

**Multi-tenant AI system with:**
- 100+ models (parents + students)
- 1000s of layers stored as atoms
- Variable request patterns (bursty, long-tail)
- Limited memory/compute resources
- Strict billing requirements

### The Solution

**Intelligent resource management:**
1. **Usage tracking** - Real-time monitoring of all operations
2. **Smart caching** - Hot layers in memory, cold layers on disk
3. **Queue prioritization** - Premium users, small requests first
4. **Predictive loading** - Pre-fetch likely-needed layers
5. **Cost-aware routing** - Route to cheapest capable model

---

## Usage Tracking Architecture

### What to Track

**Every billable operation:**
- Embedding generation
- Semantic search
- Model inference
- Code analysis
- Multimodal ingestion
- Graph queries (Neo4j)

**Key metrics per operation:**
- Tenant ID + User ID
- Operation type
- Model used (if applicable)
- Layers accessed (for caching insights)
- Input size (tokens, bytes)
- Output size
- DCUs consumed
- Latency (P50, P95, P99)
- Success/failure
- Timestamp (for time-series analysis)

### Real-Time vs Batch Tracking

**Real-Time (Hot Path):**
```sql
-- Minimal overhead logging during request
INSERT INTO dbo.UsageEventsHot (
    TenantId,
    UserId,
    OperationType,
    ModelId,
    RequestId,
    StartTime
)
VALUES (@TenantId, @UserId, @OpType, @ModelId, @RequestId, SYSUTCDATETIME());

-- ... perform actual operation ...

-- Update with results
UPDATE dbo.UsageEventsHot
SET EndTime = SYSUTCDATETIME(),
    DCUsConsumed = @DCUs,
    Success = 1
WHERE RequestId = @RequestId;
```

**Batch Processing (Cold Path):**
```sql
-- Every 5 minutes: Aggregate hot events into analytics tables
INSERT INTO dbo.UsageAnalyticsByMinute (
    TenantId,
    OperationType,
    Minute,
    RequestCount,
    TotalDCUs,
    AvgLatencyMs,
    P95LatencyMs
)
SELECT 
    TenantId,
    OperationType,
    DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime), 0) AS Minute,
    COUNT(*) AS RequestCount,
    SUM(DCUsConsumed) AS TotalDCUs,
    AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgLatency,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY DATEDIFF(MILLISECOND, StartTime, EndTime)) AS P95Latency
FROM dbo.UsageEventsHot
WHERE StartTime >= DATEADD(MINUTE, -5, SYSUTCDATETIME())
  AND EndTime IS NOT NULL
GROUP BY TenantId, OperationType, DATEADD(MINUTE, DATEDIFF(MINUTE, 0, StartTime), 0);

-- Archive hot events to cold storage
INSERT INTO dbo.UsageEventsCold
SELECT * FROM dbo.UsageEventsHot
WHERE EndTime < DATEADD(HOUR, -1, SYSUTCDATETIME());

DELETE FROM dbo.UsageEventsHot
WHERE EndTime < DATEADD(HOUR, -1, SYSUTCDATETIME());
```

### Layer Access Tracking (Critical for Caching)

```sql
-- Track which layers are accessed per request
CREATE TABLE dbo.LayerAccessLog (
    AccessId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId UNIQUEIDENTIFIER NOT NULL,
    ModelId INT NOT NULL,
    LayerIndex INT NOT NULL,
    AccessTime DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    AccessDurationMs INT,
    INDEX IX_LayerAccess_Model_Time (ModelId, AccessTime DESC)
);

-- During inference, log each layer access
-- (CLR function hooks into model execution)
INSERT INTO dbo.LayerAccessLog (RequestId, ModelId, LayerIndex, AccessDurationMs)
SELECT @RequestId, @ModelId, LayerIndex, DurationMs
FROM dbo.fn_ExecuteModelWithProfiling(@ModelId, @Input);
```

---

## Model Layer Caching Strategy

### Cache Hierarchy

**Level 1: In-Memory Cache (Hot)**
- Most frequently accessed layers
- Premium tenant models
- Recent conversation context
- Target: <10ms access time
- Size: Limited by RAM (e.g., 32GB = ~8B parameters in FP16)

**Level 2: SSD Cache (Warm)**
- Moderately accessed layers
- Recently used student models
- Target: <100ms access time
- Size: 500GB - 2TB

**Level 3: Blob Storage (Cold)**
- Rarely accessed layers
- Archived models
- Historical versions
- Target: <1000ms access time
- Size: Unlimited

### Cache Eviction Policy

**LFU (Least Frequently Used) + Recency**

```sql
-- Compute cache priority score
CREATE FUNCTION dbo.fn_ComputeCachePriority (
    @LayerPayloadId BIGINT
)
RETURNS FLOAT
AS
BEGIN
    DECLARE @Priority FLOAT;
    
    -- Factor 1: Access frequency (last 24 hours)
    DECLARE @AccessCount INT;
    SELECT @AccessCount = COUNT(*)
    FROM dbo.LayerAccessLog
    WHERE LayerPayloadId = @LayerPayloadId
      AND AccessTime > DATEADD(HOUR, -24, SYSUTCDATETIME());
    
    -- Factor 2: Recency (exponential decay)
    DECLARE @LastAccessHoursAgo FLOAT;
    SELECT @LastAccessHoursAgo = DATEDIFF(HOUR, MAX(AccessTime), SYSUTCDATETIME())
    FROM dbo.LayerAccessLog
    WHERE LayerPayloadId = @LayerPayloadId;
    
    -- Factor 3: Tenant tier (premium = higher priority)
    DECLARE @TenantMultiplier FLOAT = 1.0;
    IF EXISTS (
        SELECT 1 FROM dbo.LayerAccessLog la
        INNER JOIN dbo.Tenants t ON la.TenantId = t.TenantId
        WHERE la.LayerPayloadId = @LayerPayloadId
          AND t.SubscriptionTier IN ('Premium', 'Enterprise')
    )
    BEGIN
        SET @TenantMultiplier = 2.0;
    END;
    
    -- Combine: Frequency * Recency decay * Tenant multiplier
    SET @Priority = @AccessCount * EXP(-@LastAccessHoursAgo / 12.0) * @TenantMultiplier;
    
    RETURN @Priority;
END;
GO

-- Eviction trigger: when cache > threshold
CREATE PROCEDURE dbo.sp_EvictColdLayers
    @TargetCacheSizeGB FLOAT = 30.0  -- Keep cache under 30GB
AS
BEGIN
    -- Calculate current cache size
    DECLARE @CurrentSizeGB FLOAT;
    SELECT @CurrentSizeGB = SUM(DATALENGTH(TensorData)) / 1073741824.0
    FROM dbo.TensorAtomPayloads
    WHERE CacheLocation = 'Memory';  -- In-memory cache
    
    IF @CurrentSizeGB <= @TargetCacheSizeGB
        RETURN;  -- Cache within limits
    
    -- Evict lowest priority layers
    DECLARE @BytesToEvict BIGINT = (@CurrentSizeGB - @TargetCacheSizeGB) * 1073741824;
    
    WITH LayerPriorities AS (
        SELECT 
            PayloadId,
            DATALENGTH(TensorData) AS LayerSizeBytes,
            dbo.fn_ComputeCachePriority(PayloadId) AS Priority,
            SUM(DATALENGTH(TensorData)) OVER (ORDER BY dbo.fn_ComputeCachePriority(PayloadId) ASC) AS CumulativeSize
        FROM dbo.TensorAtomPayloads
        WHERE CacheLocation = 'Memory'
    )
    UPDATE dbo.TensorAtomPayloads
    SET CacheLocation = 'SSD',
        TensorData = NULL,  -- Offload from memory
        OffloadedAt = SYSUTCDATETIME()
    WHERE PayloadId IN (
        SELECT PayloadId 
        FROM LayerPriorities
        WHERE CumulativeSize <= @BytesToEvict
    );
END;
GO
```

### Predictive Pre-Loading

**Concept:** Load layers BEFORE they're requested

**Strategies:**

**1. Conversation Context Pre-Loading:**
```sql
-- If user is in multi-turn conversation, pre-load next likely layers
CREATE PROCEDURE dbo.sp_PreloadConversationLayers
    @ConversationId BIGINT
AS
BEGIN
    -- Identify model being used
    DECLARE @ModelId INT;
    SELECT TOP 1 @ModelId = ModelId
    FROM dbo.ConversationLogs
    WHERE ConversationId = @ConversationId
    ORDER BY CreatedAt DESC;
    
    -- Pre-load all layers for this model into SSD cache
    UPDATE dbo.TensorAtomPayloads
    SET CacheLocation = 'SSD'
    WHERE AtomId = @ModelId
      AND CacheLocation = 'Cold';
    
    -- For premium users, load into memory
    IF EXISTS (
        SELECT 1 FROM dbo.ConversationLogs cl
        INNER JOIN dbo.Tenants t ON cl.TenantId = t.TenantId
        WHERE cl.ConversationId = @ConversationId
          AND t.SubscriptionTier = 'Premium'
    )
    BEGIN
        -- Load critical layers into memory (embeddings + output heads)
        UPDATE dbo.TensorAtomPayloads
        SET CacheLocation = 'Memory',
            TensorData = dbo.fn_LoadFromBlob(BlobUri)
        WHERE AtomId = @ModelId
          AND (LayerIndex < 5 OR LayerIndex > (SELECT MAX(LayerIndex) - 5 FROM dbo.TensorAtomPayloads WHERE AtomId = @ModelId));
    END;
END;
GO
```

**2. Time-of-Day Pre-Loading:**
```sql
-- Every morning at 8 AM, pre-load popular models
CREATE PROCEDURE dbo.sp_PreloadMorningModels
AS
BEGIN
    -- Identify top 5 models by usage (last week)
    DECLARE @PopularModels TABLE (ModelId INT, UsageCount INT);
    
    INSERT INTO @PopularModels
    SELECT TOP 5 ModelId, COUNT(*) AS UsageCount
    FROM dbo.UsageEventsHot
    WHERE StartTime > DATEADD(DAY, -7, SYSUTCDATETIME())
      AND DATEPART(HOUR, StartTime) BETWEEN 8 AND 18  -- Business hours
    GROUP BY ModelId
    ORDER BY COUNT(*) DESC;
    
    -- Load into SSD cache
    UPDATE dbo.TensorAtomPayloads
    SET CacheLocation = 'SSD'
    WHERE AtomId IN (SELECT ModelId FROM @PopularModels)
      AND CacheLocation = 'Cold';
END;
GO

-- Schedule via SQL Agent or app scheduler
```

**3. Tenant-Specific Pre-Loading:**
```sql
-- For high-value tenants, keep their preferred models hot
CREATE PROCEDURE dbo.sp_PreloadTenantModels
    @TenantId INT
AS
BEGIN
    -- Find tenant's most-used models
    DECLARE @TenantModels TABLE (ModelId INT);
    
    INSERT INTO @TenantModels
    SELECT DISTINCT TOP 3 ModelId
    FROM dbo.UsageEventsHot
    WHERE TenantId = @TenantId
      AND StartTime > DATEADD(DAY, -7, SYSUTCDATETIME())
    GROUP BY ModelId
    ORDER BY COUNT(*) DESC;
    
    -- Load into memory (if premium) or SSD (if standard)
    DECLARE @CacheTarget NVARCHAR(20);
    SELECT @CacheTarget = CASE 
        WHEN SubscriptionTier IN ('Premium', 'Enterprise') THEN 'Memory'
        ELSE 'SSD'
    END
    FROM dbo.Tenants
    WHERE TenantId = @TenantId;
    
    UPDATE dbo.TensorAtomPayloads
    SET CacheLocation = @CacheTarget
    WHERE AtomId IN (SELECT ModelId FROM @TenantModels);
END;
GO
```

---

## Hot vs Cold Queue Management

### Queue Architecture

**Purpose:** Prevent resource starvation, ensure fair allocation

**Queue Types:**

1. **Hot Queue (High Priority)**
   - Premium/Enterprise tenants
   - Small requests (<1000 tokens)
   - Interactive endpoints (chat, search)
   - SLA: P95 < 500ms

2. **Warm Queue (Standard Priority)**
   - Standard tier tenants
   - Medium requests (1000-10K tokens)
   - Batch operations
   - SLA: P95 < 5 seconds

3. **Cold Queue (Low Priority)**
   - Free tier tenants
   - Large requests (>10K tokens)
   - Background jobs (ingestion, distillation)
   - SLA: Best effort

### Queue Schema

```sql
CREATE TABLE dbo.RequestQueue (
    QueueId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    TenantId INT NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    QueueType NVARCHAR(20) NOT NULL,  -- 'Hot', 'Warm', 'Cold'
    OperationType NVARCHAR(50) NOT NULL,
    Priority INT NOT NULL,  -- 0-100, higher = more urgent
    EstimatedDCUs DECIMAL(18,6) NOT NULL,
    EstimatedDurationMs INT NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,  -- JSON request data
    Status NVARCHAR(20) DEFAULT 'Pending',  -- Pending, Processing, Completed, Failed
    EnqueuedAt DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    StartedAt DATETIME2(7) NULL,
    CompletedAt DATETIME2(7) NULL,
    WorkerNode NVARCHAR(100) NULL,  -- Which server is processing
    INDEX IX_Queue_Status_Priority (Status, QueueType, Priority DESC, EnqueuedAt)
        INCLUDE (RequestId, OperationType, EstimatedDCUs)
);
```

### Queue Priority Algorithm

```sql
CREATE FUNCTION dbo.fn_CalculateRequestPriority (
    @TenantId INT,
    @OperationType NVARCHAR(50),
    @InputSize BIGINT,
    @IsInteractive BIT
)
RETURNS INT
AS
BEGIN
    DECLARE @Priority INT = 50;  -- Base priority
    
    -- Factor 1: Tenant tier (40 points)
    DECLARE @TierBonus INT;
    SELECT @TierBonus = CASE SubscriptionTier
        WHEN 'Enterprise' THEN 40
        WHEN 'Premium' THEN 30
        WHEN 'Standard' THEN 20
        WHEN 'Free' THEN 0
    END
    FROM dbo.Tenants
    WHERE TenantId = @TenantId;
    
    SET @Priority = @Priority + @TierBonus;
    
    -- Factor 2: Request size (smaller = higher priority, 20 points)
    DECLARE @SizeBonus INT = CASE
        WHEN @InputSize < 1000 THEN 20
        WHEN @InputSize < 10000 THEN 10
        ELSE 0
    END;
    
    SET @Priority = @Priority + @SizeBonus;
    
    -- Factor 3: Interactive vs batch (10 points)
    IF @IsInteractive = 1
        SET @Priority = @Priority + 10;
    
    -- Factor 4: Operation type urgency
    DECLARE @OpBonus INT = CASE @OperationType
        WHEN 'Search' THEN 10  -- User waiting
        WHEN 'Embedding' THEN 5
        WHEN 'Inference' THEN 5
        WHEN 'Ingestion' THEN 0  -- Background
        ELSE 0
    END;
    
    SET @Priority = @Priority + @OpBonus;
    
    -- Cap at 100
    IF @Priority > 100
        SET @Priority = 100;
    
    RETURN @Priority;
END;
GO
```

### Queue Assignment

```sql
CREATE PROCEDURE dbo.sp_EnqueueRequest
    @TenantId INT,
    @UserId UNIQUEIDENTIFIER,
    @OperationType NVARCHAR(50),
    @Payload NVARCHAR(MAX),
    @EstimatedDCUs DECIMAL(18,6),
    @IsInteractive BIT = 1,
    @RequestId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET @RequestId = NEWID();
    
    -- Calculate priority
    DECLARE @InputSize BIGINT = LEN(@Payload);
    DECLARE @Priority INT = dbo.fn_CalculateRequestPriority(
        @TenantId, @OperationType, @InputSize, @IsInteractive
    );
    
    -- Determine queue type
    DECLARE @QueueType NVARCHAR(20);
    SELECT @QueueType = CASE
        WHEN @Priority >= 80 THEN 'Hot'
        WHEN @Priority >= 50 THEN 'Warm'
        ELSE 'Cold'
    END;
    
    -- Estimate duration (from historical data)
    DECLARE @EstimatedDurationMs INT;
    SELECT @EstimatedDurationMs = AVG(DATEDIFF(MILLISECOND, StartedAt, CompletedAt))
    FROM dbo.RequestQueue
    WHERE OperationType = @OperationType
      AND Status = 'Completed'
      AND CompletedAt > DATEADD(DAY, -7, SYSUTCDATETIME());
    
    IF @EstimatedDurationMs IS NULL
        SET @EstimatedDurationMs = 1000;  -- Default 1 second
    
    -- Enqueue
    INSERT INTO dbo.RequestQueue (
        RequestId,
        TenantId,
        UserId,
        QueueType,
        OperationType,
        Priority,
        EstimatedDCUs,
        EstimatedDurationMs,
        Payload,
        Status
    )
    VALUES (
        @RequestId,
        @TenantId,
        @UserId,
        @QueueType,
        @OperationType,
        @Priority,
        @EstimatedDCUs,
        @EstimatedDurationMs,
        @Payload,
        'Pending'
    );
END;
GO
```

### Queue Worker (Dequeue Logic)

```sql
CREATE PROCEDURE dbo.sp_DequeueNextRequest
    @WorkerNode NVARCHAR(100),
    @QueueTypes NVARCHAR(100) = 'Hot,Warm,Cold',  -- Comma-separated
    @RequestId UNIQUEIDENTIFIER OUTPUT,
    @Payload NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Try queues in priority order
    DECLARE @QueueTypesTable TABLE (QueueType NVARCHAR(20), OrderPriority INT);
    INSERT INTO @QueueTypesTable (QueueType, OrderPriority)
    SELECT 
        value AS QueueType,
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS OrderPriority
    FROM STRING_SPLIT(@QueueTypes, ',');
    
    -- Dequeue with optimistic concurrency
    WITH NextRequest AS (
        SELECT TOP 1
            RequestId,
            Payload
        FROM dbo.RequestQueue rq
        WHERE Status = 'Pending'
          AND QueueType IN (SELECT QueueType FROM @QueueTypesTable)
        ORDER BY 
            (SELECT OrderPriority FROM @QueueTypesTable qt WHERE qt.QueueType = rq.QueueType),
            Priority DESC,
            EnqueuedAt ASC
    )
    UPDATE dbo.RequestQueue
    SET Status = 'Processing',
        StartedAt = SYSUTCDATETIME(),
        WorkerNode = @WorkerNode,
        @RequestId = RequestId,
        @Payload = Payload
    FROM NextRequest
    WHERE dbo.RequestQueue.RequestId = NextRequest.RequestId;
    
    -- Return 1 if request acquired, 0 if queue empty
    IF @RequestId IS NULL
        RETURN 0;
    ELSE
        RETURN 1;
END;
GO
```

---

## Request Prioritization System

### Dynamic Priority Adjustment

**Concept:** Priorities change based on wait time, system load

```sql
-- Boost priority for requests waiting too long
CREATE PROCEDURE dbo.sp_BoostStarvedRequests
AS
BEGIN
    -- For each queue type, define max acceptable wait time
    DECLARE @ThresholdHot INT = 5;     -- 5 seconds
    DECLARE @ThresholdWarm INT = 30;   -- 30 seconds
    DECLARE @ThresholdCold INT = 300;  -- 5 minutes
    
    -- Boost priority by 10 points for every threshold exceeded
    UPDATE dbo.RequestQueue
    SET Priority = CASE
        WHEN QueueType = 'Hot' AND DATEDIFF(SECOND, EnqueuedAt, SYSUTCDATETIME()) > @ThresholdHot
            THEN Priority + (DATEDIFF(SECOND, EnqueuedAt, SYSUTCDATETIME()) / @ThresholdHot) * 10
        WHEN QueueType = 'Warm' AND DATEDIFF(SECOND, EnqueuedAt, SYSUTCDATETIME()) > @ThresholdWarm
            THEN Priority + (DATEDIFF(SECOND, EnqueuedAt, SYSUTCDATETIME()) / @ThresholdWarm) * 10
        WHEN QueueType = 'Cold' AND DATEDIFF(SECOND, EnqueuedAt, SYSUTCDATETIME()) > @ThresholdCold
            THEN Priority + (DATEDIFF(SECOND, EnqueuedAt, SYSUTCDATETIME()) / @ThresholdCold) * 10
        ELSE Priority
    END
    WHERE Status = 'Pending'
      AND Priority < 95;  -- Don't exceed near-max
END;
GO

-- Run every 10 seconds
```

### Load-Based Queue Throttling

```sql
-- Pause low-priority queues when system overloaded
CREATE PROCEDURE dbo.sp_ThrottleQueuesOnLoad
AS
BEGIN
    -- Measure current system load
    DECLARE @CurrentCPU FLOAT;
    DECLARE @CurrentMemoryPct FLOAT;
    DECLARE @QueueDepth INT;
    
    -- CPU usage from sys.dm_os_ring_buffers or external metrics
    SELECT @CurrentCPU = 75.0;  -- Placeholder: integrate with monitoring
    
    -- Memory pressure
    SELECT @CurrentMemoryPct = 
        (SELECT CAST(physical_memory_in_use_kb AS FLOAT) / total_physical_memory_kb * 100
         FROM sys.dm_os_sys_memory);
    
    -- Queue depth
    SELECT @QueueDepth = COUNT(*)
    FROM dbo.RequestQueue
    WHERE Status = 'Pending';
    
    -- Throttle decision
    IF @CurrentCPU > 80 OR @CurrentMemoryPct > 85 OR @QueueDepth > 10000
    BEGIN
        -- Pause Cold queue
        UPDATE dbo.RequestQueue
        SET Status = 'Throttled'
        WHERE QueueType = 'Cold'
          AND Status = 'Pending';
        
        -- If severe, pause Warm too
        IF @CurrentCPU > 90 OR @QueueDepth > 50000
        BEGIN
            UPDATE dbo.RequestQueue
            SET Status = 'Throttled'
            WHERE QueueType = 'Warm'
              AND Status = 'Pending';
        END;
    END
    ELSE
    BEGIN
        -- Resume throttled requests
        UPDATE dbo.RequestQueue
        SET Status = 'Pending'
        WHERE Status = 'Throttled';
    END;
END;
GO
```

---

## Tenant Resource Allocation

### Per-Tenant Quotas & Limits

```sql
CREATE TABLE dbo.TenantResourceQuotas (
    TenantId INT NOT NULL PRIMARY KEY,
    MaxConcurrentRequests INT NOT NULL DEFAULT 5,
    MaxQueuedRequests INT NOT NULL DEFAULT 100,
    MaxMemoryMB INT NULL,  -- NULL = shared pool
    MaxCPUPercent FLOAT NULL,  -- NULL = shared pool
    ReservedModelIds NVARCHAR(MAX) NULL,  -- JSON array: models always in cache for this tenant
    CONSTRAINT FK_TenantQuotas_Tenant 
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);

-- Insert defaults based on tier
INSERT INTO dbo.TenantResourceQuotas (TenantId, MaxConcurrentRequests, MaxQueuedRequests)
SELECT 
    TenantId,
    CASE SubscriptionTier
        WHEN 'Enterprise' THEN 50
        WHEN 'Premium' THEN 20
        WHEN 'Standard' THEN 5
        WHEN 'Free' THEN 1
    END AS MaxConcurrent,
    CASE SubscriptionTier
        WHEN 'Enterprise' THEN 1000
        WHEN 'Premium' THEN 500
        WHEN 'Standard' THEN 100
        WHEN 'Free' THEN 10
    END AS MaxQueued
FROM dbo.Tenants;
```

### Enforce Quotas at Enqueue Time

```sql
ALTER PROCEDURE dbo.sp_EnqueueRequest
    @TenantId INT,
    @UserId UNIQUEIDENTIFIER,
    @OperationType NVARCHAR(50),
    @Payload NVARCHAR(MAX),
    @EstimatedDCUs DECIMAL(18,6),
    @IsInteractive BIT = 1,
    @RequestId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    -- Check concurrent request limit
    DECLARE @CurrentConcurrent INT;
    DECLARE @MaxConcurrent INT;
    
    SELECT @CurrentConcurrent = COUNT(*)
    FROM dbo.RequestQueue
    WHERE TenantId = @TenantId
      AND Status IN ('Pending', 'Processing');
    
    SELECT @MaxConcurrent = MaxConcurrentRequests
    FROM dbo.TenantResourceQuotas
    WHERE TenantId = @TenantId;
    
    IF @CurrentConcurrent >= @MaxConcurrent
    BEGIN
        THROW 50001, 'Concurrent request limit exceeded. Please wait for existing requests to complete.', 1;
    END;
    
    -- Check queued request limit
    DECLARE @CurrentQueued INT;
    DECLARE @MaxQueued INT;
    
    SELECT @CurrentQueued = COUNT(*)
    FROM dbo.RequestQueue
    WHERE TenantId = @TenantId
      AND Status = 'Pending';
    
    SELECT @MaxQueued = MaxQueuedRequests
    FROM dbo.TenantResourceQuotas
    WHERE TenantId = @TenantId;
    
    IF @CurrentQueued >= @MaxQueued
    BEGIN
        THROW 50002, 'Queue limit exceeded. Upgrade your plan for higher limits.', 1;
    END;
    
    -- (Rest of enqueue logic from before)
    -- ...
END;
GO
```

---

## Cost Optimization & DCU Efficiency

### Routing to Cheapest Capable Model

**Concept:** Use smallest student model that meets quality threshold

```sql
CREATE PROCEDURE dbo.sp_RouteToOptimalModel
    @TaskType NVARCHAR(50),  -- 'Conversation', 'Code', 'Math', etc.
    @MinQualityScore FLOAT = 0.85,
    @TenantId INT,
    @OptimalModelId INT OUTPUT
AS
BEGIN
    -- Find cheapest model that meets criteria
    SELECT TOP 1 @OptimalModelId = ma.ModelId
    FROM dbo.ModelAtoms ma
    INNER JOIN dbo.StudentModelCapabilityMappings smc 
        ON ma.ModelId = smc.StudentModelId
    INNER JOIN dbo.StudentModelTaxonomy smt 
        ON smc.TaxonomyId = smt.TaxonomyId
    WHERE smt.CapabilityName = @TaskType
      AND smc.CapabilityScore >= @MinQualityScore
      AND ma.IsProduction = 1
    ORDER BY 
        ma.ParameterCount ASC,  -- Smallest model first
        smc.CapabilityScore DESC;  -- Tie-break by quality
    
    -- If no student found, route to parent
    IF @OptimalModelId IS NULL
    BEGIN
        SELECT TOP 1 @OptimalModelId = ModelId
        FROM dbo.ModelAtoms
        WHERE IsStudentModel = 0
          AND IsProduction = 1
        ORDER BY ParameterCount ASC;
    END;
END;
GO

-- Usage in API
DECLARE @ModelId INT;
EXEC dbo.sp_RouteToOptimalModel 
    @TaskType = 'Conversation',
    @MinQualityScore = 0.85,
    @TenantId = @TenantId,
    @OptimalModelId = @ModelId OUTPUT;

-- Use @ModelId for inference
```

### Cost Estimation Before Execution

```sql
CREATE FUNCTION dbo.fn_EstimateOperationCost (
    @OperationType NVARCHAR(50),
    @InputSize BIGINT,
    @ModelId INT
)
RETURNS DECIMAL(18,6)
AS
BEGIN
    DECLARE @EstimatedDCUs DECIMAL(18,6);
    
    -- Base cost from operation type
    DECLARE @BaseCostPerUnit DECIMAL(18,8);
    SELECT @BaseCostPerUnit = UnitPrice
    FROM dbo.BillingOperationRates
    WHERE OperationType = @OperationType;
    
    -- Model complexity multiplier
    DECLARE @ModelMultiplier FLOAT;
    SELECT @ModelMultiplier = ParameterCount / 1000000000.0  -- Per billion params
    FROM dbo.ModelAtoms
    WHERE ModelId = @ModelId;
    
    -- Calculate
    SET @EstimatedDCUs = @BaseCostPerUnit * @InputSize * @ModelMultiplier;
    
    RETURN @EstimatedDCUs;
END;
GO
```

### Batch Processing for Efficiency

**Concept:** Group similar requests to amortize model loading

```sql
CREATE PROCEDURE dbo.sp_BatchSimilarRequests
    @ModelId INT,
    @MaxBatchSize INT = 32
AS
BEGIN
    -- Find pending requests for same model
    DECLARE @BatchRequests TABLE (
        RequestId UNIQUEIDENTIFIER,
        Payload NVARCHAR(MAX)
    );
    
    INSERT INTO @BatchRequests
    SELECT TOP (@MaxBatchSize)
        RequestId,
        Payload
    FROM dbo.RequestQueue
    WHERE Status = 'Pending'
      AND JSON_VALUE(Payload, '$.modelId') = CAST(@ModelId AS NVARCHAR)
    ORDER BY Priority DESC, EnqueuedAt ASC;
    
    -- Process batch in single model load
    DECLARE @BatchPayloads NVARCHAR(MAX);
    SELECT @BatchPayloads = (
        SELECT Payload
        FROM @BatchRequests
        FOR JSON PATH
    );
    
    -- Execute model once with all inputs
    DECLARE @Results NVARCHAR(MAX);
    SET @Results = dbo.fn_ExecuteModelBatch(@ModelId, @BatchPayloads);
    
    -- Update each request with its result
    -- (Parse @Results and match to RequestIds)
    -- ...
END;
GO
```

---

## Database Schema

### Complete Schema for Usage & Caching

```sql
-- ============================================
-- Usage Tracking Schema
-- ============================================

-- Real-time usage events (hot path)
CREATE TABLE dbo.UsageEventsHot (
    EventId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    TenantId INT NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    OperationType NVARCHAR(50) NOT NULL,
    ModelId INT NULL,
    InputSize BIGINT,
    OutputSize BIGINT,
    DCUsConsumed DECIMAL(18,6),
    StartTime DATETIME2(7) NOT NULL,
    EndTime DATETIME2(7) NULL,
    Success BIT,
    ErrorMessage NVARCHAR(500),
    INDEX IX_Hot_Tenant_Time (TenantId, StartTime DESC),
    INDEX IX_Hot_Completion (EndTime) WHERE EndTime IS NOT NULL
);

-- Archived usage events (cold storage)
CREATE TABLE dbo.UsageEventsCold (
    EventId BIGINT PRIMARY KEY,
    RequestId UNIQUEIDENTIFIER NOT NULL,
    TenantId INT NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    OperationType NVARCHAR(50) NOT NULL,
    ModelId INT NULL,
    InputSize BIGINT,
    OutputSize BIGINT,
    DCUsConsumed DECIMAL(18,6),
    StartTime DATETIME2(7) NOT NULL,
    EndTime DATETIME2(7),
    Success BIT,
    ErrorMessage NVARCHAR(500),
    INDEX IX_Cold_Tenant_Time (TenantId, StartTime DESC)
) WITH (DATA_COMPRESSION = PAGE);

-- Aggregated usage analytics
CREATE TABLE dbo.UsageAnalyticsByMinute (
    TenantId INT NOT NULL,
    OperationType NVARCHAR(50) NOT NULL,
    Minute DATETIME2(0) NOT NULL,
    RequestCount INT NOT NULL,
    SuccessCount INT NOT NULL,
    TotalDCUs DECIMAL(18,6) NOT NULL,
    AvgLatencyMs INT,
    P50LatencyMs INT,
    P95LatencyMs INT,
    P99LatencyMs INT,
    PRIMARY KEY (TenantId, OperationType, Minute)
);

CREATE TABLE dbo.UsageAnalyticsByHour (
    TenantId INT NOT NULL,
    OperationType NVARCHAR(50) NOT NULL,
    Hour DATETIME2(0) NOT NULL,
    RequestCount INT NOT NULL,
    SuccessCount INT NOT NULL,
    TotalDCUs DECIMAL(18,6) NOT NULL,
    AvgLatencyMs INT,
    PRIMARY KEY (TenantId, OperationType, Hour)
);

-- Layer access tracking (for cache optimization)
CREATE TABLE dbo.LayerAccessLog (
    AccessId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId UNIQUEIDENTIFIER NOT NULL,
    ModelId INT NOT NULL,
    LayerIndex INT NOT NULL,
    LayerPayloadId BIGINT NOT NULL,
    TenantId INT NOT NULL,
    AccessTime DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    AccessDurationMs INT,
    INDEX IX_LayerAccess_Model_Time (ModelId, AccessTime DESC),
    INDEX IX_LayerAccess_Payload (LayerPayloadId, AccessTime DESC)
);

-- ============================================
-- Caching Schema
-- ============================================

-- Update TensorAtomPayloads with cache metadata
ALTER TABLE dbo.TensorAtomPayloads
ADD CacheLocation NVARCHAR(20) DEFAULT 'Cold',  -- 'Memory', 'SSD', 'Cold'
    CachePriority FLOAT DEFAULT 0.0,
    LastAccessedAt DATETIME2(7),
    AccessCount BIGINT DEFAULT 0,
    BlobUri NVARCHAR(500) NULL,  -- If offloaded to blob storage
    OffloadedAt DATETIME2(7) NULL;

CREATE INDEX IX_TensorPayload_Cache 
ON dbo.TensorAtomPayloads (CacheLocation, CachePriority DESC)
INCLUDE (PayloadId, DATALENGTH(TensorData));

-- ============================================
-- Queue Schema
-- ============================================

CREATE TABLE dbo.RequestQueue (
    QueueId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    TenantId INT NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    QueueType NVARCHAR(20) NOT NULL,  -- 'Hot', 'Warm', 'Cold'
    OperationType NVARCHAR(50) NOT NULL,
    Priority INT NOT NULL,
    EstimatedDCUs DECIMAL(18,6) NOT NULL,
    EstimatedDurationMs INT NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending',
    EnqueuedAt DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    StartedAt DATETIME2(7) NULL,
    CompletedAt DATETIME2(7) NULL,
    WorkerNode NVARCHAR(100) NULL,
    RetryCount INT DEFAULT 0,
    INDEX IX_Queue_Processing (Status, QueueType, Priority DESC, EnqueuedAt)
        INCLUDE (RequestId, OperationType, Payload, EstimatedDCUs),
    INDEX IX_Queue_Tenant (TenantId, Status)
);

-- ============================================
-- Tenant Resource Quotas
-- ============================================

CREATE TABLE dbo.TenantResourceQuotas (
    TenantId INT NOT NULL PRIMARY KEY,
    MaxConcurrentRequests INT NOT NULL DEFAULT 5,
    MaxQueuedRequests INT NOT NULL DEFAULT 100,
    MaxMemoryMB INT NULL,
    MaxCPUPercent FLOAT NULL,
    ReservedModelIds NVARCHAR(MAX) NULL,  -- JSON array
    GuaranteedLatencyMs INT NULL,  -- SLA target
    UpdatedAt DATETIME2(7) DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TenantQuotas_Tenant 
        FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);
```

---

## Implementation Examples

### Example 1: End-to-End Request Flow

```sql
-- 1. User makes API request
DECLARE @RequestId UNIQUEIDENTIFIER;
DECLARE @TenantId INT = 1;
DECLARE @OperationType NVARCHAR(50) = 'EmbeddingGeneration';
DECLARE @Payload NVARCHAR(MAX) = N'{"text": "Hello world", "modelId": 43}';
DECLARE @EstimatedDCUs DECIMAL(18,6) = 0.5;

-- 2. Enqueue request
EXEC dbo.sp_EnqueueRequest
    @TenantId = @TenantId,
    @UserId = NULL,
    @OperationType = @OperationType,
    @Payload = @Payload,
    @EstimatedDCUs = @EstimatedDCUs,
    @IsInteractive = 1,
    @RequestId = @RequestId OUTPUT;

-- 3. Worker polls queue
DECLARE @DequeuedPayload NVARCHAR(MAX);
EXEC dbo.sp_DequeueNextRequest
    @WorkerNode = 'worker-01',
    @QueueTypes = 'Hot,Warm',
    @RequestId = @RequestId OUTPUT,
    @Payload = @DequeuedPayload OUTPUT;

-- 4. Worker checks cache for model layers
DECLARE @ModelId INT = JSON_VALUE(@DequeuedPayload, '$.modelId');

-- Pre-load layers if not cached
IF NOT EXISTS (
    SELECT 1 FROM dbo.TensorAtomPayloads
    WHERE AtomId = @ModelId
      AND CacheLocation IN ('Memory', 'SSD')
)
BEGIN
    -- Load critical layers
    UPDATE dbo.TensorAtomPayloads
    SET CacheLocation = 'Memory',
        TensorData = dbo.fn_LoadFromBlob(BlobUri),
        LastAccessedAt = SYSUTCDATETIME(),
        AccessCount = AccessCount + 1
    WHERE AtomId = @ModelId
      AND (LayerIndex < 5 OR LayerIndex > 60);  -- First and last layers
END;

-- 5. Execute operation
DECLARE @Result NVARCHAR(MAX);
DECLARE @ActualDCUs DECIMAL(18,6);

BEGIN TRY
    -- Record start in usage tracking
    INSERT INTO dbo.UsageEventsHot (RequestId, TenantId, OperationType, ModelId, StartTime)
    VALUES (@RequestId, @TenantId, @OperationType, @ModelId, SYSUTCDATETIME());
    
    -- Execute
    SET @Result = dbo.fn_ComputeEmbedding(...);
    SET @ActualDCUs = 0.48;  -- Actual measured cost
    
    -- Record completion
    UPDATE dbo.UsageEventsHot
    SET EndTime = SYSUTCDATETIME(),
        DCUsConsumed = @ActualDCUs,
        Success = 1
    WHERE RequestId = @RequestId;
    
    -- Mark queue item complete
    UPDATE dbo.RequestQueue
    SET Status = 'Completed',
        CompletedAt = SYSUTCDATETIME()
    WHERE RequestId = @RequestId;
    
END TRY
BEGIN CATCH
    -- Record failure
    UPDATE dbo.UsageEventsHot
    SET EndTime = SYSUTCDATETIME(),
        Success = 0,
        ErrorMessage = ERROR_MESSAGE()
    WHERE RequestId = @RequestId;
    
    UPDATE dbo.RequestQueue
    SET Status = 'Failed',
        CompletedAt = SYSUTCDATETIME()
    WHERE RequestId = @RequestId;
END CATCH;

-- 6. Evict cold layers if cache full
EXEC dbo.sp_EvictColdLayers @TargetCacheSizeGB = 30.0;
```

### Example 2: Monitoring Dashboard Queries

```sql
-- Current queue depths by type
SELECT 
    QueueType,
    Status,
    COUNT(*) AS RequestCount,
    AVG(Priority) AS AvgPriority,
    AVG(DATEDIFF(SECOND, EnqueuedAt, COALESCE(StartedAt, SYSUTCDATETIME()))) AS AvgWaitTimeSec
FROM dbo.RequestQueue
WHERE EnqueuedAt > DATEADD(HOUR, -1, SYSUTCDATETIME())
GROUP BY QueueType, Status
ORDER BY QueueType, Status;

-- Cache hit rate by model
SELECT 
    ma.ModelId,
    ma.ModelName,
    COUNT(DISTINCT lal.RequestId) AS TotalRequests,
    SUM(CASE WHEN tap.CacheLocation = 'Memory' THEN 1 ELSE 0 END) AS MemoryHits,
    SUM(CASE WHEN tap.CacheLocation = 'SSD' THEN 1 ELSE 0 END) AS SSDHits,
    SUM(CASE WHEN tap.CacheLocation = 'Cold' THEN 1 ELSE 0 END) AS ColdHits,
    CAST(SUM(CASE WHEN tap.CacheLocation = 'Memory' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS MemoryHitRate
FROM dbo.LayerAccessLog lal
INNER JOIN dbo.TensorAtomPayloads tap ON lal.LayerPayloadId = tap.PayloadId
INNER JOIN dbo.ModelAtoms ma ON lal.ModelId = ma.ModelId
WHERE lal.AccessTime > DATEADD(HOUR, -1, SYSUTCDATETIME())
GROUP BY ma.ModelId, ma.ModelName
ORDER BY MemoryHitRate DESC;

-- Top tenants by usage (last hour)
SELECT TOP 10
    t.TenantName,
    COUNT(*) AS RequestCount,
    SUM(ueh.DCUsConsumed) AS TotalDCUs,
    AVG(DATEDIFF(MILLISECOND, ueh.StartTime, ueh.EndTime)) AS AvgLatencyMs
FROM dbo.UsageEventsHot ueh
INNER JOIN dbo.Tenants t ON ueh.TenantId = t.TenantId
WHERE ueh.StartTime > DATEADD(HOUR, -1, SYSUTCDATETIME())
  AND ueh.EndTime IS NOT NULL
GROUP BY t.TenantName
ORDER BY TotalDCUs DESC;

-- System health metrics
SELECT 
    DATEPART(MINUTE, StartTime) AS Minute,
    COUNT(*) AS RequestsProcessed,
    AVG(DATEDIFF(MILLISECOND, StartTime, EndTime)) AS AvgLatencyMs,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY DATEDIFF(MILLISECOND, StartTime, EndTime)) AS P95LatencyMs,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS ErrorCount
FROM dbo.UsageEventsHot
WHERE StartTime > DATEADD(HOUR, -1, SYSUTCDATETIME())
  AND EndTime IS NOT NULL
GROUP BY DATEPART(MINUTE, StartTime)
ORDER BY Minute;
```

---

**Document Status:** Implementation guide  
**Last Updated:** November 12, 2025  
**Next Steps:** Build monitoring dashboard, implement cache eviction, deploy queue workers
