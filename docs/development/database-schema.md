# Database Schema Reference

**SQL Server Version**: 2025  
**Tables**: 99  
**Stored Procedures**: 74  
**Functions**: 24  
**CLR Assemblies**: 14  
**Deployment**: DACPAC-first (NOT EF migrations)

---

## Overview

Hartonomous uses **SQL Server 2025** as the primary database substrate with advanced features:

- **VECTOR(1998)** - Native vector embeddings for semantic search (JSON storage internally)
- **GEOMETRY** - Spatial indexing for embedding projections and atomic decomposition (R-tree O(log n) nearest-neighbor)
- **Temporal Tables** - Row-level history with `FOR SYSTEM_TIME` (automatic compliance queries)
- **Graph Tables** - `AS NODE` / `AS EDGE` for provenance relationships
- **In-Memory OLTP** - Billing ledger and inference cache (high-throughput usage tracking)
- **Service Broker** - OODA loop asynchronous messaging (ACID-guaranteed queuing)
- **CLR Integration** - .NET Framework 4.8.1 in-process functions (CPU SIMD via System.Numerics.Vectors)

**NO FILESTREAM**: Atomic decomposition eliminates need for large blob storage. All data stored in VARBINARY(64) or primitive types with SHA-256 content-addressable deduplication.

**Database Project**: `src/Hartonomous.Database/Hartonomous.Database.sqlproj`  
**Deployment Script**: `scripts/deploy-dacpac.ps1`

**CRITICAL**: Schema changes are made in `.sql` files within `src/Hartonomous.Database/`, NOT via EF Core migrations. EF Core is read-only (scaffolds from existing database).

---

## Table Summary by Category

**Data Ingestion (13 tables):**
- Atoms, AtomEmbeddings, AtomRelations
- AtomicTextTokens, AtomicPixels, AtomicAudioSamples
- AtomPayloadStore, CodeAtoms, AudioData, AudioFrames, VideoFrames
- AtomEmbeddingComponents, AtomEmbeddingSpatialMetadata

**Model Management (11 tables):**
- Models, ModelLayers, ModelWeights, ModelSnapshots
- ModelActivations, ModelMetadata, ModelVersions, ModelArtifacts
- WeightAdjustmentHistory, WeightSnapshots, WeightRollbackLog

**Inference & Generation (12 tables):**
- InferenceRequests, InferenceCache, InferenceMetadata
- AttentionGenerationLog, AttentionInferenceResults, TransformerInferenceResults
- GenerationHistory, CachedActivations, GenerationAttempts
- GenerationContextWindows, GenerationLog, GenerationMetrics

**Billing & Usage (9 tables):**
- BillingUsageLedger (In-Memory OLTP)
- BillingTenantQuotas, BillingInvoices, BillingRatePlans
- BillingOperationRates, BillingMultipliers, BillingQuotaViolations
- BillingPricingTiers, BillingCreditHistory

**Provenance & Graph (8 tables):**
- ProvenanceNodes (AS NODE)
- ProvenanceEdges (AS EDGE)
- GraphEntities, GraphRelationships, GraphQueries
- Neo4jSyncState, Neo4jSyncErrors, CdcCheckpoints

**Autonomous System (7 tables):**
- AutonomousImprovementHistory
- AutonomousComputeJobs
- OODALoopState, HypothesisQueue, ActionQueue, LearningOutcomes
- GödelEngineJobs

**Security & Tenancy (8 tables):**
- Tenants, TenantSubscriptions, TenantFeatures
- ApiKeys, ApiKeyScopes, SecurityAuditLog
- RateLimitPolicies, DeduplicationPolicies

**Spatial & Search (6 tables):**
- SpatialIndexClusters, SpatialRegions, SpatialQueries
- SearchIndices, SearchResults, EmbeddingNeighbors

**Agent & Tool Management (4 tables):**
- AgentTools, AgentConfigurations, AgentExecutionHistory
- ToolInvocationLog

**Feedback & Analytics (11 tables):**
- FeedbackSubmissions, FeedbackAnalytics, FeedbackCategories
- UserSessions, QueryPerformanceMetrics, UsageStatistics
- ErrorLog, PerformanceCounters, TelemetryEvents
- AlertConfigurations, AlertHistory

---

## Core Tables (Top 20 by Importance)

### 1. Atoms

**Purpose**: Primary content storage (text, image, audio, video, code, models)

**Location**: `src/Hartonomous.Database/Tables/dbo.Atoms.sql`

**Key Columns:**
- `AtomId BIGINT IDENTITY(1,1)` - Primary key
- `Modality NVARCHAR(64)` - text, image, audio, video, sensor, graph, model
- `ContentHash BINARY(32)` - SHA-256 for deduplication
- `CanonicalText NVARCHAR(MAX)` - Text representation
- `Metadata NVARCHAR(MAX)` - JSON metadata
- `TenantId INT` - Multi-tenant isolation
- `CreatedAt DATETIME2` - Temporal tracking

**Indexes:**
- `UX_Atoms_ContentHash_TenantId` - Unique deduplication
- `IX_Atoms_Modality_Subtype` - Modality filtering

**EF Core Entity**: `Hartonomous.Core.Entities.Atom`

---

### 2. AtomEmbeddings

**Purpose**: VECTOR(1998) embeddings with spatial projection

**Location**: `src/Hartonomous.Database/Tables/dbo.AtomEmbeddings.sql`

**Key Columns:**
- `AtomEmbeddingId BIGINT IDENTITY(1,1)` - Primary key
- `AtomId BIGINT` - Foreign key to Atoms
- `EmbeddingVector VECTOR(1998)` - Native SQL Server 2025 vector type
- `SpatialGeometry GEOMETRY` - 3D point cloud (R-tree index)
- `SpatialBucket INT` - Bucket ID for coarse-grained search
- `SpatialBucketX/Y/Z INT` - 3D spatial coordinates

**Indexes:**
- `IX_AtomEmbeddings_Atom` - Atom lookup
- `IX_AtomEmbeddings_Bucket` - Bucket filtering
- `IX_AtomEmbeddings_Spatial (GEOMETRY)` - Spatial R-tree

**CLR Functions Used:**
- `dbo.clr_VectorSimilarity(VECTOR, VECTOR)` - Cosine similarity
- `dbo.clr_SpatialProject(VECTOR)` - VECTOR → GEOMETRY projection

---

### 3. InferenceRequests

**Purpose**: API inference request tracking

**Location**: `src/Hartonomous.Database/Tables/dbo.InferenceRequests.sql`

**Key Columns:**
- `InferenceId BIGINT IDENTITY(1,1)` - Primary key
- `RequestTimestamp DATETIME2` - Request time
- `InputData NVARCHAR(MAX)` - JSON input payload
- `OutputData NVARCHAR(MAX)` - JSON output result
- `ModelsUsed NVARCHAR(MAX)` - JSON array of model IDs
- `TotalDurationMs INT` - End-to-end latency
- `Status NVARCHAR(50)` - Pending, Completed, Failed
- `TenantId INT` - Tenant isolation

**Indexes:**
- `IX_InferenceRequests_Timestamp` - Time-series queries
- `IX_InferenceRequests_Tenant` - Tenant filtering
- `IX_InferenceRequests_Status` - Status filtering

**OODA Loop Usage**: `sp_Analyze` queries this table for performance anomalies

---

### 4. Models

**Purpose**: Registered AI models (Llama, Qwen, Mistral, etc.)

**Location**: `src/Hartonomous.Database/Tables/dbo.Models.sql`

**Key Columns:**
- `ModelId INT IDENTITY(1,1)` - Primary key
- `ModelName NVARCHAR(256)` - Display name
- `ModelType NVARCHAR(100)` - llama4, qwen3, mistral, etc.
- `Architecture NVARCHAR(100)` - transformer, mamba, etc.
- `IngestionDate DATETIME2` - When ingested

**Child Tables:**
- `ModelLayers` - Individual layers (attention, feedforward, normalization)
- `TensorAtoms` - Atomized weights with SHA-256 deduplication (float32 → BINARY(4))
- `TensorAtomCoefficients` - Weight coefficients with temporal history
- `ModelSnapshots` - Temporal snapshots for rollback

---

### 5. ModelLayers

**Purpose**: Model architecture components (layers, attention heads, feedforward blocks)

**Location**: `src/Hartonomous.Database/Tables/dbo.ModelLayers.sql`

**Key Columns:**
- `LayerId BIGINT IDENTITY(1,1)` - Primary key
- `ModelId INT` - Foreign key to Models
- `LayerIdx INT` - Layer position (0-79 for 80-layer model)
- `LayerName NVARCHAR(256)` - e.g., "layer_0_attention"
- `LayerType NVARCHAR(100)` - attention, feedforward, normalization
- `WeightsGeometry GEOMETRY` - Spatial hash of weight distribution
- `TensorShape NVARCHAR(256)` - e.g., "[4096, 4096]"
- `ParameterCount BIGINT` - Number of parameters

**Indexes:**
- `UX_ModelLayers_ModelId_LayerIdx` - Unique layer per model
- `IX_ModelLayers_Weights (GEOMETRY)` - Spatial index

**Student Model Extraction**: `StudentModelService` queries this table to copy layers

---

### 6. BillingUsageLedger

**Purpose**: In-Memory OLTP table for high-throughput usage tracking

**Location**: `src/Hartonomous.Database/Tables/dbo.BillingUsageLedger.sql`

**Key Columns:**
- `LedgerId BIGINT IDENTITY(1,1)` - Primary key
- `TenantId INT` - Tenant isolation
- `OperationType NVARCHAR(50)` - inference, embedding, atomization
- `Quantity DECIMAL(18,6)` - Units consumed (tokens, API calls)
- `AmountUSD DECIMAL(18,6)` - Cost in USD
- `RecordedAt DATETIME2` - Timestamp

**In-Memory Configuration:**
- `WITH (MEMORY_OPTIMIZED = ON)`
- `HASH INDEX` on TenantId, RecordedAt

**Stored Procedure**: `sp_InsertBillingUsageRecord_Native` - Ultra-fast inserts (< 1ms)

---

### 7. AutonomousImprovementHistory

**Purpose**: OODA loop learning history

**Location**: `src/Hartonomous.Database/Tables/dbo.AutonomousImprovementHistory.sql`

**Key Columns:**
- `ImprovementId UNIQUEIDENTIFIER` - Primary key
- `HypothesisType NVARCHAR(50)` - IndexOptimization, QueryRegression, etc.
- `GeneratedCode NVARCHAR(MAX)` - Code generated by autonomous system
- `SuccessScore DECIMAL(5,4)` - 0.0-1.0 quality score
- `LatencyImprovement DECIMAL(10,4)` - % latency change
- `CompletedAt DATETIME2` - Completion time

**OODA Loop Integration**:
- `sp_Learn` inserts records after action execution
- `sp_Analyze` queries for historical success patterns

---

### 8. ProvenanceNodes (Graph Table)

**Purpose**: SQL Graph nodes for provenance tracking

**Location**: `src/Hartonomous.Database/Tables/dbo.ProvenanceNodes.sql`

**Definition:**
```sql
CREATE TABLE dbo.ProvenanceNodes (
    NodeId BIGINT IDENTITY(1,1) PRIMARY KEY,
    NodeType NVARCHAR(50), -- Atom, Inference, Model, Layer
    EntityId BIGINT,        -- Foreign key to entity table
    CreatedAt DATETIME2,
    $node_id NVARCHAR(1000) -- Graph framework column
) AS NODE;
```

**Child Table**: `ProvenanceEdges AS EDGE`

**Query Pattern:**
```sql
MATCH (i:Inference)-[e:USED_ATOM*1..5]->(a:Atom)
WHERE i.NodeId = $inferenceId
RETURN a;
```

---

### 9. InferenceCache

**Purpose**: Cached inference results for deduplication

**Location**: `src/Hartonomous.Database/Tables/dbo.InferenceCache.sql`

**Key Columns:**
- `CacheId BIGINT IDENTITY(1,1)` - Primary key
- `InputHash BINARY(32)` - SHA-256 of input
- `OutputData NVARCHAR(MAX)` - JSON result
- `HitCount INT` - Number of cache hits
- `LastAccessedUtc DATETIME2` - LRU eviction tracking

**Indexes:**
- `UX_InferenceCache_InputHash` - Unique hash lookup
- `IX_InferenceCache_LastAccessed` - LRU eviction

**OODA Loop**: `sp_Act` preloads frequent cache entries via CacheWarming hypothesis

---

### 10. AttentionGenerationLog

**Purpose**: Attention mechanism inference tracking

**Location**: `src/Hartonomous.Database/Tables/Attention.AttentionGenerationTables.sql`

**Key Columns:**
- `LogId BIGINT IDENTITY(1,1)` - Primary key
- `InferenceRequestId BIGINT` - Foreign key to InferenceRequests
- `LayerIdx INT` - Which layer executed attention
- `AttentionScores NVARCHAR(MAX)` - JSON attention weights
- `DurationMs INT` - Layer execution time

**Use Case**: Analyze which layers consume most time during inference

---

## Temporal Tables (System-Versioned)

**Tables with FOR SYSTEM_TIME:**

1. `Atoms` → `AtomsHistory`
2. `ModelWeights` → `ModelWeightsHistory`
3. `ModelLayers` → `ModelLayersHistory`
4. `BillingUsageLedger` → `BillingUsageLedgerHistory`

**Query Historical State:**
```sql
-- Get atom state as of 2025-01-01
SELECT * FROM dbo.Atoms
FOR SYSTEM_TIME AS OF '2025-01-01 00:00:00'
WHERE AtomId = 12345;

-- Get all changes to an atom
SELECT * FROM dbo.Atoms
FOR SYSTEM_TIME ALL
WHERE AtomId = 12345
ORDER BY SysStartTime DESC;
```

**Weight Rollback:**
```sql
-- Rollback model weights to snapshot
EXEC dbo.sp_RollbackWeightsToTimestamp 
    @ModelId = 5,
    @TargetTimestamp = '2025-11-01 12:00:00';
```

---

## Stored Procedures (100 Total)

**OODA Loop (5):**
- `sp_Analyze` - Observe & Orient phase
- `sp_Hypothesize` - Decide phase (NOT IMPLEMENTED)
- `sp_Act` - Action execution phase
- `sp_Learn` - Learning & measurement phase
- `sp_AutonomousImprovement` - Full cycle trigger

**Billing (12):**
- `sp_InsertBillingUsageRecord_Native` - In-memory insert
- `sp_CalculateBill` - Monthly invoice calculation
- `sp_EnforceQuota` - Real-time quota check
- `sp_ApplyMultiplier` - Dynamic pricing

**Inference (15):**
- `sp_AttentionInference` - Attention mechanism
- `sp_CognitiveActivation` - Layer activation
- `sp_ChainOfThoughtReasoning` - CoT prompt engineering
- `sp_ApproxSpatialSearch` - Approximate nearest neighbor

**Model Management (18):**
- `sp_AtomizeModel` - Decompose model into atoms
- `sp_CompareModelKnowledge` - Model comparison
- `sp_CreateWeightSnapshot` - Temporal snapshot
- `sp_RestoreWeightSnapshot` - Rollback weights
- `sp_RollbackWeightsToTimestamp` - Time-travel rollback

**Atom Ingestion (20):**
- `sp_AtomIngestion` - Main ingestion pipeline
- `sp_AtomizeText` - Text → atomic tokens
- `sp_AtomizeImage` - Image → atomic pixels
- `sp_AtomizeAudio` - Audio → atomic samples
- `sp_AtomizeCode` - Code → AST atoms

**Provenance (8):**
- `sp_AuditProvenanceChain` - Trace inference lineage
- `sp_RecordProvenance` - Log provenance edge
- `sp_QueryProvenance` - Graph traversal

**Admin (10):**
- `sp_RebuildSpatialIndices` - Rebuild R-tree indices
- `sp_PurgeOldData` - Data retention cleanup
- `sp_AnalyzeQueryPerformance` - Query Store analysis

---

## CLR Functions (14 Assemblies)

**Mathematical (MathNet.Numerics):**
- `dbo.clr_VectorSimilarity(VECTOR, VECTOR)` - Cosine similarity
- `dbo.clr_DotProduct(VECTOR, VECTOR)` - Inner product
- `dbo.clr_EuclideanDistance(VECTOR, VECTOR)` - L2 distance

**Anomaly Detection:**
- `dbo.IsolationForestScore(NVARCHAR(MAX))` - Tree-based outlier detection
- `dbo.LocalOutlierFactor(NVARCHAR(MAX), INT)` - Density-based anomaly

**Spatial Projection:**
- `dbo.clr_SpatialProject(VECTOR)` - VECTOR → GEOMETRY (t-SNE/UMAP)
- `dbo.clr_SpatialHash(GEOMETRY)` - Bucket assignment

**Gödel Engine (Compute Jobs):**
- `dbo.clr_FindPrimes(BIGINT, BIGINT)` - Prime number search
- `dbo.clr_SymbolicProof(NVARCHAR(MAX))` - Theorem proving (PLANNED)

**Performance:**
- `dbo.clr_ParallelAggregate(NVARCHAR(MAX))` - SIMD aggregation
- `dbo.clr_VectorizedMean(VECTOR)` - AVX2 mean calculation

**Assemblies:**
- System.Runtime.CompilerServices.Unsafe
- System.Buffers, System.Memory
- MathNet.Numerics (BLAS/LAPACK bindings)
- Newtonsoft.Json (JSON parsing)

**Security**: ALL CLR assemblies run with `UNSAFE` permission (TRUSTWORTHY ON)

---

## Service Broker Objects

**Queues:**
- `AnalyzeQueue` - Receives observation triggers
- `HypothesizeQueue` - Receives analysis results (NOT USED)
- `ActQueue` - Receives hypotheses
- `LearnQueue` - Receives action results

**Services:**
- `AnalyzeService` - Owned by sp_Analyze
- `HypothesizeService` - NOT IMPLEMENTED
- `ActService` - Owned by sp_Act
- `LearnService` - Owned by sp_Learn

**Contracts:**
- `[//Hartonomous/AutonomousLoop/AnalyzeContract]`
- `[//Hartonomous/AutonomousLoop/HypothesizeContract]`
- `[//Hartonomous/AutonomousLoop/ActContract]`
- `[//Hartonomous/AutonomousLoop/LearnContract]`

**Message Types:**
- `[//Hartonomous/AutonomousLoop/AnalyzeMessage]` - XML observations
- `[//Hartonomous/AutonomousLoop/HypothesizeMessage]` - XML hypotheses
- `[//Hartonomous/AutonomousLoop/ActMessage]` - XML actions
- `[//Hartonomous/AutonomousLoop/LearnMessage]` - XML results

---

## Deployment

**DACPAC Build:**
```powershell
MSBuild.exe src\Hartonomous.Database\Hartonomous.Database.sqlproj `
    /p:Configuration=Release /t:Build
```

**Unified Deployment:**
```powershell
.\scripts\deploy-database-unified.ps1 `
    -Server localhost `
    -Database Hartonomous
```

**Steps:**
1. Builds DACPAC from `.sql` files
2. Deploys schema changes (differential)
3. Deploys CLR assemblies (`deploy-clr-secure.ps1`)
4. Initializes Service Broker queues
5. Seeds initial data (tenants, rate plans)

**Rollback**: Use DACPAC history or temporal table `FOR SYSTEM_TIME` queries

---

## Performance Characteristics

**Embedding Lookup** (< 1ms):
```sql
SELECT TOP 10 EmbeddingVector
FROM dbo.AtomEmbeddings
WHERE SpatialBucket = 42;
```

**Approximate KNN** (10-50ms for 1M embeddings):
```sql
EXEC sp_ApproxSpatialSearch
    @QueryVector = @vec,
    @TopK = 10;
```

**Billing Insert** (< 1ms via In-Memory OLTP):
```sql
EXEC sp_InsertBillingUsageRecord_Native
    @TenantId = 1,
    @OperationType = 'inference',
    @Quantity = 500; -- tokens
```

**OODA Loop Cycle** (5-60 seconds full cycle):
- sp_Analyze: 2-5 seconds (anomaly detection)
- sp_Act: 1-10 seconds (action execution)
- sp_Learn: 2-5 seconds (delta measurement)

---

## Monitoring

**Table Sizes:**
```sql
SELECT 
    t.name AS TableName,
    p.rows AS RowCount,
    SUM(a.total_pages) * 8 / 1024 AS SizeMB
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.name IN ('Atoms', 'AtomEmbeddings', 'InferenceRequests', 'BillingUsageLedger')
GROUP BY t.name, p.rows
ORDER BY SizeMB DESC;
```

**Service Broker Health:**
```sql
SELECT name, messages, active_conversations
FROM sys.service_queues
WHERE name LIKE '%Queue';
```

**CLR Assembly Versions:**
```sql
SELECT a.name, af.name AS FileName, af.content
FROM sys.assemblies a
INNER JOIN sys.assembly_files af ON a.assembly_id = af.assembly_id
WHERE a.name LIKE 'MathNet%' OR a.name LIKE 'SqlClr%';
```

---

## Next Steps

1. **Implement sp_Hypothesize**: Complete OODA loop (currently bypassed)
2. **Add Graph Indexes**: Optimize provenance traversal
3. **Partition Large Tables**: Atoms, AtomEmbeddings by CreatedAt
4. **Columnstore Indexes**: Analytics tables (FeedbackAnalytics, QueryPerformanceMetrics)
5. **CDC Setup**: Change Data Capture for Neo4j sync worker
