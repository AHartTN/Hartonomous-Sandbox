# Autonomous Governance

**Technical reference for autonomous OODA loop implementation, Service Broker orchestration, and self-improvement cycles.**

## Overview

Hartonomous implements a continuous autonomous improvement loop using SQL Server 2025 Service Broker messaging and stored procedure orchestration. The system observes performance metrics, detects anomalies, proposes optimization hypotheses, executes infrastructure changes, and measures improvement delta.

**Core Components:**

1. **Service Broker Messaging** - Asynchronous queue-based coordination (`AnalyzeQueue`, `HypothesizeQueue`, `ActQueue`, `LearnQueue`)
2. **OODA Loop Procedures** - Four-phase autonomous cycle (`sp_Analyze`, `sp_Hypothesize`, `sp_Act`, `sp_Learn`)
3. **Neo4j Provenance Sync** - Real-time graph synchronization via `ServiceBrokerMessagePump`
4. **CDC Event Processing** - Change Data Capture streaming through Azure Event Hubs

---

## Service Broker Infrastructure

**Configuration:** `scripts/setup-service-broker.sql`

### Message Types

| Message Type | Validation | Purpose |
|--------------|------------|---------|
| `[//Hartonomous/AutonomousLoop/AnalyzeMessage]` | `WELL_FORMED_XML` | Carries observation data (anomalies, patterns, metrics) from Analyze phase to Hypothesize phase |
| `[//Hartonomous/AutonomousLoop/HypothesizeMessage]` | `WELL_FORMED_XML` | Carries hypothesis list (proposed optimizations) from Hypothesize to Act phase |
| `[//Hartonomous/AutonomousLoop/ActMessage]` | `WELL_FORMED_XML` | Carries execution results (applied actions, queue status) from Act to Learn phase |
| `[//Hartonomous/AutonomousLoop/LearnMessage]` | `WELL_FORMED_XML` | Carries improvement deltas (before/after metrics) from Learn back to Analyze to restart cycle |

### Contracts

Each contract authorizes bi-directional message flow on the respective queue:

- `[//Hartonomous/AutonomousLoop/AnalyzeContract]` ➜ `AnalyzeQueue`
- `[//Hartonomous/AutonomousLoop/HypothesizeContract]` ➜ `HypothesizeQueue`
- `[//Hartonomous/AutonomousLoop/ActContract]` ➜ `ActQueue`
- `[//Hartonomous/AutonomousLoop/LearnContract]` ➜ `LearnQueue`

### Queues

All queues configured with `STATUS = ON` and no activation procedure (manual polling):

- `AnalyzeQueue` - Receives `LearnMessage` (cycle restart trigger)
- `HypothesizeQueue` - Receives `AnalyzeMessage` (observations + anomalies)
- `ActQueue` - Receives `HypothesizeMessage` (ranked hypotheses)
- `LearnQueue` - Receives `ActMessage` (execution results)

**Queue Management Endpoints:** `AutonomyController` exposes `POST /api/autonomy/control/pause`, `/resume`, `/reset` to toggle queue status and cleanup conversations.

**Queue Status API:** `GET /api/autonomy/queues/status` returns current message depth and conversation count per queue.

---

## OODA Loop Procedures

### Phase 1: `sp_Analyze` (Observe & Orient)

**File:** `sql/procedures/dbo.sp_Analyze.sql`

**Purpose:** Query recent inference activity, detect performance anomalies, identify patterns.

**Input Parameters:**
- `@TenantId INT` (default 0): Filter observations by tenant
- `@AnalysisScope NVARCHAR(256)` (default 'full'): Scope of analysis (full, models, embeddings, spatial)
- `@LookbackHours INT` (default 24): Time window for observation

**Logic:**

1. Query `dbo.InferenceRequests` for last 1000 completed/failed inferences within `@LookbackHours`
2. **Anomaly Detection (Paradigm-Compliant):** Compute baseline average duration, compare individual requests; flag outliers >2× standard deviations
   - **Note:** Comment references "ISOLATION FOREST" and "CLR aggregates for anomaly detection" but implementation uses simple `AVG()` comparison. Advanced CLR aggregates (e.g., `IsolationForestAggregate`, `LocalOutlierFactorAggregate`) are mentioned in code comments but not deployed (see `src/SqlClr/Core/VectorUtilities.cs:99` reference to `AnomalyDetectionAggregates`).
3. Detect patterns: frequent model IDs, slow endpoints, peak hours
4. Serialize observations, anomalies, and patterns to XML
5. Send `HypothesizeMessage` to `HypothesizeQueue` via Service Broker `SEND ON CONVERSATION`

**Output:** Message to `HypothesizeQueue` containing `@AnalysisId`, observation count, anomaly list, detected patterns.

**API Exposure:** `POST /api/autonomy/ooda/analyze` (requires `Admin` policy) triggers `sp_Analyze` and returns parsed observations.

---

### Phase 2: `sp_Hypothesize` (Orient & Decide)

**File:** `sql/procedures/dbo.sp_Hypothesize.sql`

**Purpose:** Receive observations from Analyze phase, generate optimization hypotheses, rank by estimated impact.

**Input:** `@TenantId INT` (default 0)

**Logic:**

1. `WAITFOR (RECEIVE TOP(1) ... FROM HypothesizeQueue)` with 5-second timeout
2. Parse XML message body to extract `@ObservationsJson`
3. Generate hypotheses based on detected patterns:
   - If high anomaly count: "Increase columnstore batch size", "Enable query store forced plan"
   - If slow embeddings: "Add spatial index on EmbeddingGeometry", "Increase max degree of parallelism"
   - If service broker congestion: "Partition heavy queues", "Enable parallel activation"
4. Score each hypothesis (0-100 range) based on estimated impact and implementation complexity
5. Serialize ranked hypotheses to XML
6. Send `ActMessage` to `ActQueue`

**Output:** Message to `ActQueue` with top 10 ranked hypotheses (JSON array: `hypothesisId`, `description`, `estimatedImpactScore`, `category`).

---

### Phase 3: `sp_Act` (Decide & Act)

**File:** `sql/procedures/dbo.sp_Act.sql`

**Purpose:** Receive hypotheses, execute safe infrastructure optimizations, queue risky changes for approval.

**Input:** `@TenantId INT` (default 0)

**Logic:**

1. `WAITFOR (RECEIVE TOP(1) ... FROM ActQueue)` with 5-second timeout
2. Parse `@HypothesesJson` to extract ranked hypothesis list
3. For each hypothesis:
   - **Auto-Execute (Safe):** Index creation, statistics updates, MAXDOP adjustments
   - **Queue for Approval (Risky):** Schema changes, memory config, AG topology changes
4. Execute safe actions (e.g., `CREATE SPATIAL INDEX`, `UPDATE STATISTICS WITH FULLSCAN`)
5. Insert risky actions into `dbo.PendingActions` with `Status = 'Queued'`
6. Serialize execution results: `executedActions`, `queuedActions`, `failedActions`
7. Send `LearnMessage` to `LearnQueue`

**Output:** Message to `LearnQueue` with action execution summary and failure details.

**Human Approval Required:** Items in `dbo.PendingActions` require manual approval via Operations dashboard or direct SQL update (`UPDATE dbo.PendingActions SET Status = 'Approved' WHERE ActionId = ...`).

---

### Phase 4: `sp_Learn` (Learn & Adapt)

**File:** `sql/procedures/dbo.sp_Learn.sql`

**Purpose:** Measure performance delta before/after actions, persist improvement history, restart OODA cycle.

**Input:** `@TenantId INT` (default 0)

**Logic:**

1. `WAITFOR (RECEIVE TOP(1) ... FROM LearnQueue)` with 5-second timeout
2. Parse `@ExecutionResultsJson` to extract executed/queued/failed action counts
3. **Measure Performance Delta:**
   - **Baseline:** Average duration and throughput from 24 hours before actions
   - **Current:** Average duration and throughput from 1 hour after actions
   - **Improvement %:** `((Baseline - Current) / Baseline) * 100`
4. Insert learning outcome into `dbo.AutonomousImprovementHistory`:
   - `AnalysisId`, `ActionsExecuted`, `PerformanceImprovement`, `ConfidenceScore`, `FeedbackLoop`
5. **Adaptive Threshold Adjustment:** If improvement <5%, increase anomaly detection threshold; if >20%, decrease threshold to catch more subtle issues
6. Send `AnalyzeMessage` back to `AnalyzeQueue` to restart cycle

**Output:** Message to `AnalyzeQueue` with updated configuration (adjusted thresholds, new analysis scope) to restart autonomous loop.

**Cycle Completion Metric:** `GET /api/autonomy/cycles/history` returns placeholder structure (actual persistence planned for future implementation).

---

## Neo4j Provenance Synchronization

**Service:** `src/Neo4jSync/` (.NET 10 background worker)

**Purpose:** Listen to Service Broker queues, replicate inference lineage and generation streams to Neo4j graph database.

### ServiceBrokerMessagePump

**File:** `src/Neo4jSync/Services/ServiceBrokerMessagePump.cs`

**Core Loop:**

1. Poll `IMessageBroker.ReceiveAsync(_waitTimeout, cancellationToken)` (backed by `RECEIVE FROM [QueueName]`)
2. Track delivery attempts via `Dictionary<Guid, int> _deliveryAttempts`
3. On message received:
   - If attempts > `_poisonMessageMaxAttempts`: Move to dead letter via `IMessageDeadLetterSink.SendAsync`
   - Otherwise: Dispatch via `IMessageDispatcher.DispatchAsync` ➜ routes to registered event handlers
4. On success: `message.CompleteAsync()` (END CONVERSATION)
5. On failure: `message.AbandonAsync()` (ROLLBACK transaction, requeue)

**Event Handlers:**

- `InferenceEventHandler` - Syncs inference requests, steps, and provenance to `(:Inference)` nodes
- `ModelEventHandler` - Creates `(:Model)` nodes and `[:USED_BY]` relationships
- `KnowledgeEventHandler` - Maps concepts to `(:Concept)` nodes with semantic embeddings
- `GenericEventHandler` - Fallback for custom message types

**Poison Message Handling:** After max attempts, logs error to dead letter table (`dbo.MessageDeadLetterQueue` or Azure Storage queue) and ends conversation to prevent infinite retry.

---

## CDC Event Processing

**Service:** `src/Hartonomous.Workers.CesConsumer/` (.NET 10 background worker)

**Purpose:** Consume Change Data Capture events from Azure Event Hubs, replicate schema changes to Hartonomous database.

### CdcEventProcessor

**File:** `src/Hartonomous.Workers.CesConsumer/Services/CdcEventProcessor.cs`

**Inherits:** `BaseEventProcessor` (from `Hartonomous.Core.Messaging`)

**Event Flow:**

1. Subscribe to Azure Event Hub partition (`EventProcessorClient`)
2. Receive CDC event (JSON envelope with `operation`, `tableName`, `before`, `after` snapshots)
3. Deserialize via `System.Text.Json`
4. Route to appropriate handler:
   - `INSERT` ➜ Replicate new row to target table
   - `UPDATE` ➜ Merge changes via `MERGE` statement
   - `DELETE` ➜ Soft-delete (set `IsDeleted = 1`) or hard-delete depending on policy
5. Checkpoint EventHub offset on success

**Resilience:** Polly retry policy (3 attempts, exponential backoff) wraps Event Hub operations. Permanent failures log to Application Insights and dead letter queue.

### CesConsumerService

**File:** `src/Hartonomous.Workers.CesConsumer/CesConsumerService.cs`

**Hosted Service Implementation:**

- `StartAsync`: Initialize `EventProcessorClient`, register `ProcessEventAsync` and `ProcessErrorAsync` handlers
- `StopAsync`: Stop processing, flush checkpoints, dispose client

**Configuration:** `appsettings.json` requires `AzureEventHubs:ConnectionString`, `AzureEventHubs:ConsumerGroup`, `AzureEventHubs:EventHubName`.

---

## Autonomous Improvement History

**Table:** `dbo.AutonomousImprovementHistory`

**Schema (from `sql/tables/dbo.AutonomousImprovementHistory.sql`):**

| Column | Type | Description |
|--------|------|-------------|
| `HistoryId` | `BIGINT IDENTITY` | Primary key |
| `AnalysisId` | `UNIQUEIDENTIFIER` | Links to specific OODA cycle execution |
| `AnalysisTimestamp` | `DATETIME2` | When `sp_Analyze` executed |
| `ActionsExecuted` | `INT` | Count of safe actions applied by `sp_Act` |
| `ActionsQueued` | `INT` | Count of risky actions awaiting approval |
| `PerformanceImprovement` | `DECIMAL(5,2)` | Percentage improvement in avg duration (can be negative) |
| `ConfidenceScore` | `DECIMAL(5,2)` | 0-100 confidence in measured improvement (higher = more data points) |
| `FeedbackLoop` | `NVARCHAR(MAX)` | JSON summary of detected patterns, applied actions, and outcome |
| `CreatedAtUtc` | `DATETIME2` | Record creation timestamp |

**Usage:** `sp_Learn` inserts a row after measuring delta. Operations dashboard queries this table to visualize autonomous improvement trends over time.

---

## Current Limitations & Future Enhancements

### Implemented

- ✅ Service Broker queue infrastructure (4 queues, 4 contracts, 4 message types)
- ✅ OODA loop procedures (`sp_Analyze`, `sp_Hypothesize`, `sp_Act`, `sp_Learn`)
- ✅ Neo4j provenance sync via `ServiceBrokerMessagePump`
- ✅ CDC event processing via `CesConsumerService`
- ✅ Autonomous improvement history table
- ✅ Queue control API endpoints (`AutonomyController`)

### Partially Implemented

- ⚠️ **Anomaly Detection:** `sp_Analyze` references "ISOLATION FOREST" and "CLR aggregates for anomaly detection" but uses simple `AVG()` comparison. Advanced aggregates (`IsolationForestAggregate`, `LocalOutlierFactorAggregate`) mentioned in comments but not deployed.
  - **Evidence:** `src/SqlClr/Core/VectorUtilities.cs:99` comment references `AnomalyDetectionAggregates`; `src/SqlClr/MachineLearning/MahalanobisDistance.cs:9` mentions "REPLACES: AnomalyDetectionAggregates.cs:497-525 (diagonal-only covariance - mathematically incorrect)"
  - **Workaround:** Current anomaly detection flags requests >2× standard deviations from mean
  - **Roadmap:** Deploy full `AnomalyDetectionAggregates` CLR assembly with IsolationForest/LOF support

- ⚠️ **OODA Event Handlers:** No explicit `OodaEventHandler` class found in codebase
  - **Search Result:** `grep_search` for `OodaEventHandler|class.*Ooda.*Handler` returned no matches
  - **Actual Implementation:** OODA loop is SQL-native (Service Broker + stored procedures); .NET event handlers focus on Neo4j sync (`InferenceEventHandler`, `ModelEventHandler`, etc.)

### Not Implemented

- ❌ **Automated Hypothesis Scoring:** `sp_Hypothesize` uses hardcoded scores (0-100) based on simple pattern matching; no ML model to predict actual impact
- ❌ **Rollback Mechanism:** No automatic rollback if `sp_Learn` detects negative performance impact
- ❌ **Multi-Tenant OODA:** Current implementation has `@TenantId` parameter but does not isolate autonomous loops per tenant
- ❌ **OODA Cycle History API:** `GET /api/autonomy/cycles/history` returns placeholder; actual persistence of cycle metadata not implemented

---

## Verification & Testing

### Manual OODA Cycle Execution

```sql
-- Trigger Analyze phase
EXEC dbo.sp_Analyze @TenantId = 0, @AnalysisScope = 'full', @LookbackHours = 24;

-- Check HypothesizeQueue for message
SELECT TOP 1 
    conversation_handle,
    message_type_name,
    CAST(message_body AS XML) AS MessageBody
FROM HypothesizeQueue;

-- Trigger Hypothesize phase (processes queue)
EXEC dbo.sp_Hypothesize @TenantId = 0;

-- Check ActQueue
SELECT TOP 1 conversation_handle, message_type_name, CAST(message_body AS XML) FROM ActQueue;

-- Trigger Act phase
EXEC dbo.sp_Act @TenantId = 0;

-- Check LearnQueue
SELECT TOP 1 conversation_handle, message_type_name, CAST(message_body AS XML) FROM LearnQueue;

-- Trigger Learn phase
EXEC dbo.sp_Learn @TenantId = 0;

-- Verify improvement history
SELECT TOP 5 * FROM dbo.AutonomousImprovementHistory ORDER BY AnalysisTimestamp DESC;
```

### API-Driven Testing

```bash
# Trigger Analyze and inspect observations
curl -X POST http://localhost:5001/api/autonomy/ooda/analyze \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"tenantId": 0, "analysisScope": "full", "lookbackHours": 24}'

# Check queue status
curl -X GET http://localhost:5001/api/autonomy/queues/status \
  -H "Authorization: Bearer <token>"

# Pause all queues (stop autonomous processing)
curl -X POST http://localhost:5001/api/autonomy/control/pause \
  -H "Authorization: Bearer <token>"

# Resume queues
curl -X POST http://localhost:5001/api/autonomy/control/resume \
  -H "Authorization: Bearer <token>"

# Reset all conversations (cleanup stuck messages)
curl -X POST http://localhost:5001/api/autonomy/control/reset \
  -H "Authorization: Bearer <token>"
```

---

## References

- **Service Broker Setup:** `scripts/setup-service-broker.sql`
- **OODA Procedures:** `sql/procedures/dbo.sp_Analyze.sql`, `dbo.sp_Hypothesize.sql`, `dbo.sp_Act.sql`, `dbo.sp_Learn.sql`
- **Neo4j Sync:** `src/Neo4jSync/Services/ServiceBrokerMessagePump.cs`, `EventDispatcher.cs`, event handler classes
- **CDC Consumer:** `src/Hartonomous.Workers.CesConsumer/Services/CdcEventProcessor.cs`, `CesConsumerService.cs`
- **Improvement History Table:** `sql/tables/dbo.AutonomousImprovementHistory.sql`
- **API Controller:** `src/Hartonomous.Api/Controllers/AutonomyController.cs`
- **CLR Aggregate References:** `src/SqlClr/Core/VectorUtilities.cs`, `src/SqlClr/MachineLearning/MahalanobisDistance.cs`
