# Complete Hartonomous Implementation Plan
## Holistic View of All 188 Tasks Across 7 Layers

**Generated**: 2025-01-XX  
**Purpose**: Master execution plan for autonomous intelligence platform  
**Critical Context**: This plan considers the ENTIRE system architecture - all 188 tasks must be viewed holistically, not piecemeal

---

## Executive Summary

### Current Reality Check

**What's ACTUALLY Complete** (validated by codebase inspection):
- ✅ **Hartonomous.Infrastructure** (13 subsystems): 100% COMPLETE - Saved 2-3 months
  - Caching, Compliance (PII), Data (EF Core), FeatureManagement, HealthChecks
  - Jobs, Lifecycle, Messaging, Middleware, Observability (25 metrics)
  - ProblemDetails, RateLimiting, Resilience, Services (SpatialInferenceService)
- ✅ **API Foundation** (~60% complete): Project structure, DI, auth (JWT/API keys), rate limiting, health checks
- ✅ **Worker Services** (scaffolded): CesConsumer, ModelIngestion, Neo4jSync exist but need SQL backend
- ⚠️ **SQL CLR Assemblies** (3/10 complete): ConceptDiscovery.cs, EmbeddingFunctions.cs, StreamOrchestrator.cs
- ⚠️ **SQL Schema** (25% defined): 11 table files, 2 UDT files, 43 procedure files

**THE CRITICAL BLOCKER**: SQL Database Foundation (Layers 0-1)
- Tables exist as `.sql` files but **database deployment status unknown**
- Database features (FILESTREAM, In-Memory OLTP, Service Broker, CDC, etc.) **not configured**
- Missing core tables: `Atom`, `AtomEmbedding`, `TensorAtom`, `ModelLayers`, `InferenceRequest`, `TenantApiKey`, etc.
- Without SQL foundation: API can't run, workers can't process, CLR can't execute

**Conservative Progress Estimate**: 30-40 tasks complete (16-21% of 188)  
**Realistic Progress Estimate**: 50-70 tasks complete IF SQL partially deployed (27-37%)  
**Optimistic Progress Estimate**: 80-90 tasks IF SQL fully configured (43-48%)

---

## Architecture-Driven Dependency Graph

```
LAYER 0: Foundation Schema (18 tasks)
         ↓
LAYER 1: Storage Engine (18 tasks)
         ↓
         ├─────────────────────┬──────────────────┐
         ↓                     ↓                  ↓
LAYER 2A: Inference (8)   LAYER 2B: Analytics (6)   LAYER 3: Autonomous (28)
         ↓                     ↓                  ↓
         └─────────────────────┴──────────────────┘
                              ↓
                    LAYER 4: Concept Discovery (11)
                              ↓
         ┌────────────────────┼────────────────────┬────────────────────┐
         ↓                    ↓                    ↓                    ↓
    LAYER 5A:           LAYER 5B:            LAYER 5C:            LAYER 5D:
    Provenance (15)     Real-World (8)       API (18)             Clients (22)
         ↓                    ↓                    ↓                    ↓
         └────────────────────┴────────────────────┴────────────────────┘
                              ↓
                    LAYER 6: Production Hardening (24)
```

**Key Insight**: Everything depends on SQL foundation. No workarounds, no shortcuts.

---

## LAYER 0: Foundation Schema (18 Tasks)
**Status**: ⚠️ 25% DEFINED, 0% DEPLOYED  
**Blocking**: Everything (Layers 1-6)

### L0.1: Core Table Definitions (18 tasks)

**Existing Files** (11 tables):
- ✅ `dbo.AtomPayloadStore.sql` - FILESTREAM payload storage
- ✅ `dbo.AutonomousImprovementHistory.sql` - 4-phase loop audit log
- ✅ `dbo.BillingUsageLedger.sql` - Regular billing table
- ✅ `dbo.BillingUsageLedger_InMemory.sql` - Memory-optimized billing
- ✅ `dbo.InferenceCache.sql` - Request deduplication
- ✅ `dbo.TenantSecurityPolicy.sql` - Autonomous command whitelist
- ✅ `dbo.TestResults.sql` - Unit test results
- ✅ `graph.AtomGraphNodes.sql` - SQL Graph nodes
- ✅ `graph.AtomGraphEdges.sql` - SQL Graph edges
- ✅ `provenance.Concepts.sql` - Discovered concepts
- ✅ `provenance.GenerationStreams.sql` - Generation audit trail

**Existing UDTs** (2 types):
- ✅ `provenance.AtomicStream.sql` - Nano-provenance UDT (Echelon 1)
- ✅ `provenance.ComponentStream.sql` - Component-level provenance

**MISSING CRITICAL TABLES** (7+ tables needed):
- ❌ **L0.1.1**: `dbo.Atom` - Content-addressable storage (SHA-256 deduplication)
  - Columns: `AtomId`, `ContentHash`, `Modality`, `Subtype`, `CanonicalText`, `Payload FILESTREAM`, `SpatialKey GEOMETRY`, `ComponentStream`, `CreatedAt`
  - **Status**: Defined in `Setup_FILESTREAM.sql` but NOT as standalone file
  - **Action**: Extract to `sql/tables/dbo.Atom.sql`

- ❌ **L0.1.2**: `dbo.AtomEmbedding` - VECTOR(1998) + GEOMETRY dual representation
  - Columns: `AtomId FK`, `EmbeddingVector VECTOR(1998)`, `SpatialProjection2D GEOGRAPHY`, `SpatialProjection3D GEOMETRY`
  - **Purpose**: Hybrid vector-spatial search (100x faster than pure vector)

- ❌ **L0.1.3**: `dbo.TensorAtom` - Model weights as LineString GEOMETRY
  - Columns: `AtomId FK`, `LayerId`, `WeightsGeometry GEOMETRY(LINESTRING)`, `ParameterCount`
  - **Purpose**: 62GB models with <10MB memory footprint

- ❌ **L0.1.4**: `dbo.ModelLayers` - Layer metadata for student model extraction
  - Columns: `LayerId`, `ModelAtomId FK`, `LayerType`, `InputShape`, `OutputShape`, `ParameterCount`

- ❌ **L0.1.5**: `dbo.InferenceRequest` - In-Memory OLTP request tracking
  - Columns: `RequestId`, `TenantId`, `PromptAtomId FK`, `ResponseAtomId FK`, `AtomicStream`, `Cost`, `StartedAt`, `CompletedAt`
  - **Constraint**: `PRIMARY KEY NONCLUSTERED HASH (RequestId) WITH (BUCKET_COUNT = 10000000)`
  - **Requirement**: `MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA`

- ❌ **L0.1.6**: `dbo.TenantApiKey` - API authentication
  - Columns: `ApiKeyId`, `TenantId FK`, `KeyHash`, `ExpiresAt`, `IsActive`

- ❌ **L0.1.7**: `dbo.AccessPolicy` - Row-level security policies
  - Columns: `PolicyId`, `TenantId`, `ResourceType`, `AllowRead`, `AllowWrite`

**Additional Tasks**:
- ✅ **L0.1.8**: `TenantCreditLedger` - Likely defined, needs verification
- ⚠️ **L0.1.9**: Add `ConceptId FK` to `Atom` table (nullable)
- ⚠️ **L0.1.10**: Extend `AutonomousImprovementHistory` with `ActionId`, `MaxCost`, `DurationLimit`
- ❌ **L0.1.11**: Create `AtomMetadataCache` (memory-optimized JSON table)
- ❌ **L0.1.12**: Add computed columns for JSON indexing (`ContentType`, `Severity`)
- ❌ **L0.1.13**: Create HASH indexes on computed columns
- ⚠️ **L0.1.14**: Add `ModelLayer.WeightsGeometry GEOMETRY` column
- ⚠️ **L0.1.15**: Add `Atom.PayloadLocator VARBINARY(MAX) FILESTREAM` (conflicts with existing schema)
- ❌ **L0.1.16**: Create spatial projection columns (`SpatialProjection2D GEOGRAPHY`, `SpatialProjection3D GEOMETRY`)
- ❌ **L0.1.17**: Add `Metadata JSON` column to Atom-related tables
- ❌ **L0.1.18**: Create indexes: Graph ($from_id, $to_id), Temporal (ValidFrom, ValidTo), Spatial (GEOMETRY_AUTO_GRID)

**ACTION REQUIRED**:
1. Audit all 43 procedure files to determine which reference missing tables
2. Create missing table definition files in `sql/tables/`
3. Run `deploy-database.ps1` to deploy schema
4. Verify tables exist via `SELECT * FROM sys.tables`

---

## LAYER 1: Storage Engine (18 Tasks)
**Status**: ❌ 0% CONFIGURED  
**Blocking**: Inference, Analytics, Autonomous, API, Workers

### L1.1: FILESTREAM Setup (3 tasks)

**Existing**: `sql/Setup_FILESTREAM.sql` has complete instructions

- ❌ **L1.1.1**: Enable FILESTREAM at instance level
  ```powershell
  # SQL Server Configuration Manager: Enable FILESTREAM (Transact-SQL + File I/O)
  # Restart SQL Server
  sp_configure 'filestream access level', 2;
  RECONFIGURE;
  ```

- ⚠️ **L1.1.2**: Add FILESTREAM filegroup to database
  - **Existing**: `Setup_FILESTREAM.sql` creates `HartonomousFileStream` filegroup
  - **Path**: `D:\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\HartonomousFileStream`
  - **Issue**: Hardcoded path may not match deployment environment
  - **Action**: Make path configurable via `deploy-database.ps1` parameter

- ⚠️ **L1.1.3**: Map `Atom.PayloadLocator` to FILESTREAM filegroup
  - **Existing**: `Setup_FILESTREAM.sql` creates `dbo.Atoms` table with `Payload FILESTREAM`
  - **Conflict**: `graph.AtomGraphNodes` uses `PayloadLocator NVARCHAR` (old schema)
  - **Action**: Migrate or consolidate schemas

### L1.2: In-Memory OLTP Setup (4 tasks)

- ❌ **L1.2.1**: Convert `InferenceRequest` to memory-optimized
  - **Requirement**: Add `MEMORY_OPTIMIZED = ON` to table definition
  - **Constraint**: `PRIMARY KEY NONCLUSTERED HASH (RequestId) WITH (BUCKET_COUNT = 10000000)`

- ✅ **L1.2.2**: Convert `BillingUsageLedger` to memory-optimized
  - **Status**: `dbo.BillingUsageLedger_InMemory.sql` exists

- ❌ **L1.2.3**: Convert `AtomMetadataCache` to memory-optimized
  - **Status**: Table doesn't exist yet (see L0.1.11)

- ❌ **L1.2.4**: Create natively compiled procedure `sp_LogUsageRecord_Native`
  - **Purpose**: Sub-millisecond billing record insertion (10-30x speedup)
  - **Syntax**: `WITH NATIVE_COMPILATION, SCHEMABINDING, ATOMIC`

### L1.3: Columnstore Indexes (5 tasks)

- ❌ **L1.3.1**: Convert all temporal history tables to clustered columnstore
  - **Requirement**: Enable temporal tables first (see L0)
  - **Tables**: `Atom_History`, `AtomEmbedding_History`, etc.

- ❌ **L1.3.2**: Add nonclustered columnstore to `BillingUsageLedger` (TenantId, UsageDate)
- ❌ **L1.3.3**: Add nonclustered columnstore to `AutonomousImprovementHistory` (Phase, StartedAt)
- ❌ **L1.3.4**: Add nonclustered columnstore to `AtomEmbedding` (analytics queries)
- ❌ **L1.3.5**: Configure batch mode on rowstore (compatibility level 150+)

### L1.4: Core Atom Ingestion Procedures (6 tasks)

**Existing**: 43 procedure files in `sql/procedures/` - **contents unknown**

- ❓ **L1.4.1**: `sp_IngestAtom` (SHA-256 deduplication, content-addressable insert)
  - **Check**: Search for file in `sql/procedures/`
  
- ❓ **L1.4.2**: `sp_GenerateEmbedding` (calls CLR → OpenAI API → inserts AtomEmbedding)
  - **Related**: `Embedding.TextToVector.sql` exists (43 procedure files)

- ❓ **L1.4.3**: `sp_ExtractMetadata` (CLR NLP: entities, sentiment, language)
  - **Related**: `Semantics.FeatureExtraction.sql` exists

- ❓ **L1.4.4**: `sp_DetectDuplicates` (semantic similarity >0.95 threshold)
  - **Related**: `Deduplication.SimilarityCheck.sql` exists

- ❓ **L1.4.5**: `sp_LinkProvenance` (graph edge creation)
  - **Related**: `Graph.AtomSurface.sql` exists

- ❌ **L1.4.6**: `clr_IngestModelFromPath` (UNSAFE CLR, SqlFileStream zero-copy)
  - **Purpose**: 62GB model ingestion without loading into memory
  - **Status**: Not in existing 3 CLR assemblies (ConceptDiscovery, EmbeddingFunctions, StreamOrchestrator)

**ACTION REQUIRED**:
1. Read all 43 procedure files to determine implementation status
2. Build missing CLR assembly: `FileStreamIngestion.cs`
3. Test FILESTREAM zero-copy ingestion with large model file

---

## LAYER 2A: Inference Engine (8 Tasks - Critical Path)
**Status**: ⚠️ 25% COMPLETE  
**Blocking**: Autonomous Loop, Concept Discovery, API endpoints

### L2A.1: Generative sTVF Implementation (8 tasks)

- ❓ **L2A.1.1**: Implement `sp_HybridVectorSpatialSearch` (spatial pre-filter + exact k-NN)
  - **Purpose**: 100x faster than pure vector search (5ms vs 500ms)
  - **Strategy**: Spatial index pre-filter → exact cosine distance on candidates
  - **Related File**: `Inference.VectorSearchSuite.sql` (43 procedure files)

- ❌ **L2A.1.2**: Implement `VectorAttentionAggregate` CLR aggregate
  - **Purpose**: Softmax attention mechanism in SQL
  - **Syntax**: `SELECT dbo.VectorAttentionAggregate(EmbeddingVector) OVER (PARTITION BY RequestId ORDER BY Score DESC)`
  - **Missing**: Not in existing 3 CLR assemblies

- ❌ **L2A.1.3**: Implement `fn_GenerateWithAttention` CLR streaming TVF (CRITICAL PATH)
  - **Purpose**: Core generative loop - streams tokens as SQL rows
  - **Mechanism**: CROSS APPLY sp_HybridVectorSpatialSearch → VectorAttentionAggregate → yield token
  - **Provenance**: Builds AtomicStream UDT in-memory during generation
  - **Performance**: Lazy streaming - no materialization until consumed
  - **Missing**: Not in existing 3 CLR assemblies

- ❓ **L2A.1.4**: Modify `ModelIngestionProcessor.cs` to convert tensors → LineString GEOMETRY
  - **Status**: `ModelIngestion/` worker service exists - functionality unknown
  - **Purpose**: 62GB model → GEOMETRY(LINESTRING) with 4 billion points

- ❌ **L2A.1.5**: Implement `sp_ExtractStudentModel` (query-based model slicing)
  - **Purpose**: Extract domain-specific subset from 62GB model
  - **Mechanism**: `SELECT WeightsGeometry FROM TensorAtom WHERE LayerType IN ('embedding', 'attention')`

- ❌ **L2A.1.6**: Implement GPU acceleration (GpuVectorOperations.cs, cuBLAS P/Invoke)
  - **Requirement**: UNSAFE CLR assembly (on-prem only)
  - **Dependencies**: CUDA Toolkit, cuBLAS library
  - **Fallback**: VectorOperationsSafe.cs (AVX2 SIMD)

- ❌ **L2A.1.7**: Implement fallback: GPU unavailable → VectorOperationsSafe (AVX2)
  - **Status**: VectorOperationsSafe.cs doesn't exist yet
  - **Purpose**: 4x speedup via SIMD without GPU

- ❌ **L2A.1.8**: Test: 62GB model loads with <10MB memory footpoint
  - **Mechanism**: SqlGeometry.STPointN() fetches single point without materializing full LineString

**CRITICAL BLOCKER**: fn_GenerateWithAttention is THE core inference loop. Without it:
- ❌ No text generation
- ❌ No image generation (calls fn_GenerateWithAttention internally)
- ❌ No audio/video generation
- ❌ No API `/predict` endpoint
- ❌ No autonomous hypothesis generation

---

## LAYER 2B: Analytics Engine (6 Tasks)
**Status**: ❌ 0% COMPLETE  
**Blocks**: Autonomous learning, concept discovery

### L2B.1: Batch-Aware CLR Aggregates (6 tasks)

- ❌ **L2B.1.1**: Refactor `VectorMeanVariance` with `[SqlFacet(IsBatchModeAware=true)]`
  - **Purpose**: Columnstore batch mode (900 rows/batch) → 10x speedup via AVX2
  - **Missing**: Not in existing 3 CLR assemblies

- ❌ **L2B.1.2**: Refactor `VectorKMeansCluster` to batch-aware + convert to sTVF
  - **Purpose**: Concept discovery clustering (see Layer 4)
  - **Missing**: Not in existing 3 CLR assemblies

- ❌ **L2B.1.3**: Implement `CausalInferenceAggregate` (treatment, outcome, confounders)
  - **Purpose**: Autonomous loop learns from A/B test results
  - **Example**: `SELECT CausalInferenceAggregate(ActionType, SuccessScore, JSON_VALUE(Context, '$.confounders'))`

- ❌ **L2B.1.4**: Implement `VerifyProofAggregate` (formal logic critic, queries graph.AtomGraphNodes)
  - **Purpose**: Autonomous system verifies hypotheses against knowledge graph
  - **Mechanism**: Traverses SQL Graph to validate logical consistency

- ❌ **L2B.1.5**: Implement `DetectRepetitivePattern` (novelty/boredom detector)
  - **Purpose**: sp_Analyze phase identifies repetitive atom sequences
  - **Mechanism**: Sliding window autocorrelation on embedding vectors

- ❌ **L2B.1.6**: Test: Columnstore batch mode delivers 10x speedup on aggregates
  - **Baseline**: Row-by-row aggregate on 10M rows
  - **Target**: Batch mode aggregate 10x faster

---

## LAYER 3: Autonomous Loop (28 Tasks)
**Status**: ⚠️ 15% SCAFFOLDED  
**Blocking**: Concept Discovery, Real-World Interface

### L3.1: Service Broker Infrastructure (12 tasks)

- ❌ **L3.1.1**: Create message types
  ```sql
  CREATE MESSAGE TYPE [AutonomyRequest] VALIDATION = WELL_FORMED_XML;
  CREATE MESSAGE TYPE [AutonomyResponse] VALIDATION = WELL_FORMED_XML;
  ```

- ❌ **L3.1.2**: Create contract
  ```sql
  CREATE CONTRACT [AutonomyContract] (
      [AutonomyRequest] SENT BY INITIATOR,
      [AutonomyResponse] SENT BY TARGET
  );
  ```

- ❌ **L3.1.3**: Create queue with poison message handling
  ```sql
  CREATE QUEUE AutonomousQueue 
  WITH POISON_MESSAGE_HANDLING (STATUS = ON),
       ACTIVATION (
           STATUS = ON,
           PROCEDURE_NAME = clr_AutonomousStepHandler,
           MAX_QUEUE_READERS = 4
       );
  ```

- ❌ **L3.1.4**: Create service
  ```sql
  CREATE SERVICE AutonomyService ON QUEUE AutonomousQueue ([AutonomyContract]);
  ```

- ❌ **L3.1.5**: Create DLQ (Dead Letter Queue) for permanent failures

- ❌ **L3.1.6**: Implement `clr_AutonomousStepHandler` CLR activation procedure
  - **Purpose**: Processes Service Broker messages, calls sp_Analyze/Hypothesize/Act/Learn
  - **Mechanism**: `WAITFOR RECEIVE TOP(1) ... FROM AutonomousQueue`
  - **Missing**: Not in existing 3 CLR assemblies

- ❌ **L3.1.7**: Implement conversation group strategy (partition by TenantId)
- ❌ **L3.1.8**: Add exponential backoff (1s → 2s → 4s → 8s → 16s → DLQ)
- ❌ **L3.1.9**: Create `ServiceBrokerErrorLog` table
- ❌ **L3.1.10**: Configure conversation lifetime (24 hours max)
- ❌ **L3.1.11**: Create monitoring query (sys.dm_broker_queue_monitors)
- ❌ **L3.1.12**: Test: 100 messages/sec without poison message errors

### L3.2: Four-Phase Autonomous Procedures (16 tasks)

**Existing**: ✅ `ServiceBrokerMessagePump.cs` in CesConsumer worker - **NOT the same as clr_AutonomousStepHandler**

- ❓ **L3.2.1**: Refactor `sp_AutonomousImprovement` to orchestrator
  - **Related File**: `Autonomy.SelfImprovement.sql` (43 procedure files)
  - **Action**: Read file to determine implementation status

- ❌ **L3.2.2**: Add `@MaxCost` and `@DurationLimit` parameters to orchestrator

- ❌ **L3.2.3**: Implement `sp_Analyze` phase
  - **Purpose**: Find highest-cost "problem" (knowledge gaps, high surprise, expensive ops, boredom)
  - **Mechanism**:
    1. Query `sys.dm_exec_query_stats` for failed JOINs (knowledge gaps)
    2. Query `InferenceRequest` for high surprise (expected vs actual confidence)
    3. Query `BillingUsageLedger` for expensive operations
    4. Call `DetectRepetitivePattern` aggregate for boredom detection
  - **Output**: JSON problem description → sends to sp_Hypothesize

- ❌ **L3.2.4**: Implement `sp_Hypothesize` phase
  - **Mechanism**: Calls `fn_GenerateWithAttention` with problem as prompt
  - **Output**: JSON action plan with `motor_control` field

- ❌ **L3.2.5**: Implement `sp_Act` phase
  - **Mechanism**: Parses `motor_control` JSON, executes shell/query/api_call
  - **Safety**: Queries `TenantSecurityPolicy.ShellCommandWhitelist` before execution

- ❌ **L3.2.6**: Implement `clr_ExecuteShellCommand` (UNSAFE CLR with security policy check)
  - **Purpose**: Executes whitelisted shell commands (git commit, deployment scripts, etc.)
  - **Safety**: Validates against `TenantSecurityPolicy.ShellCommandWhitelist`
  - **Logging**: All commands logged to `AutonomousImprovementHistory` BEFORE execution
  - **Missing**: Not in existing 3 CLR assemblies

- ❌ **L3.2.7**: Implement `sp_Learn` phase
  - **Mechanism**: Reads action result, calls PREDICT to score success, updates model weights

- ❌ **L3.2.8**: Integrate PREDICT function for success scoring
  - **Requirement**: SQL Server Machine Learning Services (Python/R runtime)
  - **Model**: Logistic regression trained on historical success/failure

- ❌ **L3.2.9**: Implement approval queue for high-risk actions
  - **Mechanism**: If `TenantSecurityPolicy.AuditLevel = 3`, INSERT action into approval queue instead of executing

- ❌ **L3.2.10**: Create `sp_ApproveAction` stored procedure (admin tool)

- ❌ **L3.2.11**: Add budget tracking (cumulative `@MaxCost` check in each phase)

- ❌ **L3.2.12**: Add time limit enforcement (`DATEDIFF(MINUTE, @StartTime, SYSUTCDATETIME()) < @DurationLimit`)

- ❌ **L3.2.13**: Test: Analyze phase identifies knowledge gap (JOIN returning 0 rows)
- ❌ **L3.2.14**: Test: Hypothesize phase generates valid action plan JSON
- ❌ **L3.2.15**: Test: Act phase executes shell command (whitelisted only)
- ❌ **L3.2.16**: Test: Learn phase updates PREDICT model based on action result

---

## LAYER 4: Concept Discovery (11 Tasks)
**Status**: ⚠️ 10% COMPLETE (CLR stub exists)  
**Blocking**: Real-World Interface, API semantic search

### L4.1-L4.11: Clustering & Graph Binding

**Existing**: ✅ `SqlClr/ConceptDiscovery.cs` exists - **contents unknown**

- ❓ **L4.1**: Implement `clr_DiscoverConcepts`
  - **Mechanism**: K-means clustering on uncategorized `AtomEmbedding` rows
  - **Output**: Creates "Concept" atoms, updates `Atom.ConceptId` FK
  - **Check**: Read `ConceptDiscovery.cs` to verify implementation

- ❓ **L4.2**: Implement `clr_BindConcepts`
  - **Mechanism**: SQL Graph query for Event atoms linked to concepts via MATCH clause
  - **Output**: Creates IS_A relationships in `graph.AtomGraphEdges`

- ❌ **L4.3**: Service Broker activation for concept discovery queue
- ❌ **L4.4**: Schedule daily concept discovery job (SQL Agent or Hangfire)
- ❌ **L4.5**: Implement incremental clustering (only new atoms since last run)
- ❌ **L4.6**: Tune K-means hyperparameters (k=100, max_iterations=50)
- ❌ **L4.7**: Add concept name generation via `fn_GenerateWithAttention`
- ❌ **L4.8**: Create `ConceptHierarchy` table for parent-child concept relationships
- ❌ **L4.9**: Implement concept drift detection (concept embeddings change over time)
- ❌ **L4.10**: Test: 10K uncategorized atoms → 100 concepts in <5 minutes
- ❌ **L4.11**: Test: Graph query retrieves all atoms in a concept with single MATCH clause

---

## LAYER 5A: Provenance Pipeline (15 Tasks)
**Status**: ⚠️ 30% SCAFFOLDED  
**Blocking**: None (parallel to other Layer 5 streams)

### 4-Tier Provenance Architecture

**Echelon 1**: AtomicStream UDT (nano-provenance in CLR memory)  
**Echelon 2**: SQL Graph tables (hot provenance for recent queries)  
**Echelon 3**: Neo4j via CDC → Event Hubs (cold provenance for long-term analysis)  
**Echelon 4**: Data warehouse analytics (aggregated provenance reports)

**Existing**:
- ✅ `provenance.AtomicStream.sql` UDT
- ✅ `provenance.ComponentStream.sql` UDT
- ✅ `CesConsumer/ProvenanceGraphBuilder.cs` worker service
- ✅ `Neo4jSync/` worker service (Neo4j Cypher generation)

### L5A Tasks

- ✅ **L5A.1**: Create `AtomicStream` UDT
  - **Status**: `sql/types/provenance.AtomicStream.sql` exists

- ✅ **L5A.2**: Create `ComponentStream` UDT
  - **Status**: `sql/types/provenance.ComponentStream.sql` exists

- ❌ **L5A.3**: Create trigger on `InferenceRequest` to persist AtomicStream
  - **Mechanism**: `AFTER INSERT` trigger extracts AtomicStream from CLR context, inserts into `provenance.GenerationStreams`

- ❌ **L5A.4**: Enable CDC on `graph.AtomGraphNodes` and `graph.AtomGraphEdges`
  ```sql
  EXEC sys.sp_cdc_enable_table 
      @source_schema = 'graph', 
      @source_name = 'AtomGraphNodes',
      @role_name = NULL;
  ```

- ❌ **L5A.5**: Configure Azure Event Hubs integration
  - **Requirement**: Azure Event Hubs connection string in `appsettings.json`
  - **Mechanism**: CDC → Event Hubs via SQL Server 2019+ built-in connector

- ⚠️ **L5A.6**: Implement `CesConsumer` worker service to read Event Hubs
  - **Status**: Service exists - **functionality unknown**
  - **Action**: Read `CesConsumer/ServiceBrokerMessagePump.cs` and event handlers

- ❌ **L5A.7**: Implement `ProvenanceGraphBuilder.BuildGraph()` method
  - **Purpose**: Converts CDC events → Neo4j Cypher CREATE/MERGE statements
  - **Output**: Sends Cypher to `Neo4jSync` service via message bus

- ❌ **L5A.8**: Configure Neo4j connection (URI, credentials, database name)

- ⚠️ **L5A.9**: Implement `Neo4jSync` worker to execute Cypher batches
  - **Status**: Service exists - **functionality unknown**

- ❌ **L5A.10**: Create Neo4j schema constraints (UNIQUE on `atomId`, indexes on `modality`, `createdAt`)

- ❌ **L5A.11**: Implement provenance query API endpoint: `/api/provenance/{atomId}`
  - **Returns**: Full lineage graph from Neo4j

- ❌ **L5A.12**: Implement provenance visualization (D3.js force-directed graph in Blazor WASM)

- ❌ **L5A.13**: Test: Inference request generates AtomicStream → Graph → CDC → Event Hubs → Neo4j in <5 seconds

- ❌ **L5A.14**: Test: Neo4j query retrieves 3-hop provenance graph in <100ms

- ❌ **L5A.15**: Test: Provenance pipeline handles 1000 inferences/sec without backlog

---

## LAYER 5B: Real-World Interface (8 Tasks)
**Status**: ⚠️ 10% SCAFFOLDED  
**Blocking**: None (parallel to other Layer 5 streams)

**Existing**: ✅ `SqlClr/StreamOrchestrator.cs` - **contents unknown**

- ❓ **L5B.1**: Implement Real-Time Stream Orchestrator CLR
  - **Check**: Read `StreamOrchestrator.cs` to verify implementation
  - **Purpose**: Coordinates concurrent multi-modal stream ingestion

- ❌ **L5B.2**: Implement concurrent stream ingestion (audio + video + sensor data simultaneously)
  - **Mechanism**: Parallel `Task.Run()` for each stream, shared `ConcurrentQueue<Atom>`

- ❌ **L5B.3**: Implement time-bucketing for temporal alignment
  - **Purpose**: Align audio frame at t=1.5s with video frame at t=1.5s
  - **Mechanism**: Round timestamps to 100ms buckets

- ❌ **L5B.4**: Create "Event" atoms for real-world occurrences
  - **Example**: "User clicked button" → Event atom with timestamp, UI element reference

- ❌ **L5B.5**: Extend `ComponentStream` UDT for multi-modal provenance
  - **Addition**: `StreamType` field (audio/video/sensor), `TimestampUtc`

- ❌ **L5B.6**: Implement "Agency" tagging (human-generated vs AI-generated)
  - **Mechanism**: `Atom.Metadata JSON` field includes `{"agency": "human"}` or `{"agency": "ai", "modelId": 42}`

- ❌ **L5B.7**: Test: Ingest 60 FPS video stream (16.67ms per frame) without backlog

- ❌ **L5B.8**: Test: Query "all atoms between 10:15:00 and 10:15:30" returns temporally aligned multi-modal results

---

## LAYER 5C: API Layer (18 Tasks)
**Status**: ⚠️ 60% COMPLETE  
**Blocking**: Client Applications, Integration Services

**Existing**: ✅ API project structure with Controllers/, DTOs/, Authorization/, Services/

### Completed Tasks (12/18)

- ✅ **L5C.1**: Create ASP.NET Core Web API project (`Hartonomous.Api/`)
- ✅ **L5C.2**: Configure dependency injection container (Autofac)
- ✅ **L5C.3**: Set up logging (Serilog to Console + File + Seq)
- ✅ **L5C.4**: Implement JWT authentication
- ✅ **L5C.5**: Implement API key authentication (header: `X-Api-Key`)
- ✅ **L5C.6**: Implement tenant resolution from JWT claims or API key
- ✅ **L5C.7**: Create authorization policies (`TenantPolicy`, `AdminPolicy`)
- ✅ **L5C.8**: Implement rate limiting (token bucket: 100 req/min per tenant)
- ✅ **L5C.16**: Implement health checks (`/health`, `/health/ready`, `/health/live`)
- ✅ **L5C.17**: Add W3C Trace Context correlation (OpenTelemetry middleware)
- ⚠️ **L5C.18**: Load testing (partially done - Infrastructure has observability hooks)

### Missing Tasks (6/18)

- ❌ **L5C.9**: API versioning (URL path: `/api/v1/atoms`, `/api/v2/atoms`)
  - **Library**: Asp.Versioning.Mvc
  - **Configuration**: `services.AddApiVersioning(options => options.DefaultApiVersion = new ApiVersion(1, 0))`

- ❌ **L5C.10**: Implement core endpoints:
  - `POST /api/v1/atoms` - Upload text/image/audio/video atom
  - `GET /api/v1/atoms/{id}` - Retrieve atom by ID
  - `POST /api/v1/search` - Hybrid vector-spatial search
  - `POST /api/v1/predict` - Run inference (calls `fn_GenerateWithAttention`)
  - `GET /api/v1/billing/usage` - Query billing ledger
  - `POST /api/v1/autonomous/trigger` - Manually trigger autonomous improvement loop
  - `GET /api/v1/provenance/{atomId}` - Retrieve provenance graph

- ❌ **L5C.11**: Generate OpenAPI specification (Swashbuckle)
  - **Configuration**: `services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hartonomous API", Version = "v1" }))`

- ❌ **L5C.12**: Add Swagger UI (`/swagger`)

- ❌ **L5C.13**: Implement detailed request/response logging (Serilog enrichers)
  - **Enrichers**: `RequestId`, `TenantId`, `UserId`, `Duration`, `StatusCode`

- ❌ **L5C.14**: Add performance monitoring (Application Insights or custom metrics)
  - **Metrics**: Request duration histogram, error rate counter, active request gauge

- ❌ **L5C.15**: Implement ProblemDetails for error responses (RFC 7807)
  - **Status**: ✅ Infrastructure has `ProblemDetails/` subsystem
  - **Action**: Wire up in API `Program.cs`

**CRITICAL BLOCKER**: Core endpoints (L5C.10) require:
- ✅ EF Core DbContext (exists in Infrastructure)
- ❌ SQL database with tables deployed (Layer 0-1 blocker)
- ❌ `fn_GenerateWithAttention` CLR function (Layer 2A blocker)

---

## LAYER 5D: Client Applications (22 Tasks)
**Status**: ❓ UNKNOWN  
**Blocking**: None (requires completed API from Layer 5C)

**Existing**: ✅ `src/Hartonomous.Admin/` directory exists - **contents unknown**

### L5D.1: Blazor WASM PWA (12 tasks)

- ❌ **L5D.1.1**: Create Blazor WebAssembly project with PWA template
- ❌ **L5D.1.2**: Implement search page with filters (modality, date range, similarity threshold)
- ❌ **L5D.1.3**: Implement result cards (text preview, image thumbnail, audio player, video player)
- ❌ **L5D.1.4**: Implement infinite scroll pagination
- ❌ **L5D.1.5**: Add provenance graph visualization (D3.js force-directed graph)
- ❌ **L5D.1.6**: Implement atom detail page (full content, metadata, embedding vector heatmap)
- ❌ **L5D.1.7**: Add offline support (Service Worker caches API responses)
- ❌ **L5D.1.8**: Implement push notifications for autonomous improvements
- ❌ **L5D.1.9**: Add dark mode toggle
- ❌ **L5D.1.10**: Implement accessibility (ARIA labels, keyboard navigation, screen reader support)
- ❌ **L5D.1.11**: Test: Lighthouse score >90 (Performance, Accessibility, SEO, PWA)
- ❌ **L5D.1.12**: Test: Works offline (cached content accessible without network)

### L5D.2: Blazor Server Admin Dashboard (10 tasks)

- ❓ **L5D.2.1**: Create Blazor Server project (check if `Hartonomous.Admin/` is this)
- ❌ **L5D.2.2**: Implement Service Broker monitoring dashboard
  - **Metrics**: Queue depth, messages/sec, poison message count
  - **Charts**: Plotly.NET real-time line charts

- ❌ **L5D.2.3**: Implement Autonomous Improvement dashboard
  - **Displays**: Recent improvements, success/failure rate, cost per improvement
  - **Controls**: Approve pending actions, adjust `@MaxCost` threshold

- ❌ **L5D.2.4**: Implement CDC pipeline monitoring
  - **Metrics**: CDC latency, Event Hubs throughput, Neo4j backlog
  - **Alerts**: Pipeline stalled >5 minutes

- ❌ **L5D.2.5**: Implement billing dashboard
  - **Charts**: Cost per tenant (pie chart), cost over time (line chart), cost breakdown by operation type (bar chart)
  - **Export**: CSV download of billing ledger

- ❌ **L5D.2.6**: Implement tenant management page
  - **CRUD**: Create/Update/Delete tenants, reset API keys, adjust security policies

- ❌ **L5D.2.7**: Implement query performance analyzer
  - **Source**: `sys.dm_exec_query_stats`
  - **Displays**: Top 20 slowest queries, execution plan diagrams

- ❌ **L5D.2.8**: Add real-time SignalR updates for dashboard metrics
- ❌ **L5D.2.9**: Implement role-based access control (Admin vs ReadOnly roles)
- ❌ **L5D.2.10**: Test: Dashboard loads in <2 seconds with 1M billing records

---

## LAYER 5E: Integration Services (12 Tasks)
**Status**: ❌ 0% COMPLETE  
**Blocking**: None (parallel to other Layer 5 streams)

### L5E.1: OpenAI Integration (4 tasks)

- ❌ **L5E.1.1**: Implement `OpenAiEmbeddingService` (calls OpenAI Embeddings API)
  - **Endpoint**: `POST https://api.openai.com/v1/embeddings`
  - **Model**: `text-embedding-3-large` (3072 dimensions)
  - **Rate Limit**: Exponential backoff + retry

- ❌ **L5E.1.2**: Implement `sp_GenerateEmbedding` to call C# service
  - **Mechanism**: CLR procedure → `OpenAiEmbeddingService.GetEmbeddingAsync()` → insert `AtomEmbedding`

- ❌ **L5E.1.3**: Implement RAG pattern for grounded generation
  - **Mechanism**: 
    1. User query → `sp_HybridVectorSpatialSearch` → top 10 atoms
    2. Concatenate atom texts → prompt prefix
    3. Call `fn_GenerateWithAttention` with grounded prompt

- ❌ **L5E.1.4**: Test: RAG reduces hallucination rate from 40% to <5%

### L5E.2: GitHub Integration (4 tasks)

- ❌ **L5E.2.1**: Implement GitHub webhook endpoint (`POST /webhooks/github`)
  - **Events**: `push`, `pull_request`, `issues`
  - **Action**: Create Event atoms for each commit/PR/issue

- ❌ **L5E.2.2**: Implement autonomous Git integration
  - **Mechanism**: `sp_Act` phase calls `clr_ExecuteShellCommand` with `git commit`, `git push`
  - **Safety**: Requires whitelisted commands in `TenantSecurityPolicy`

- ❌ **L5E.2.3**: Create atom from commit diff (text modality, source=GitHub)

- ❌ **L5E.2.4**: Test: Push to repo → webhook → atom created in <5 seconds

### L5E.3: Stripe Billing Integration (4 tasks)

- ❌ **L5E.3.1**: Implement Stripe webhook endpoint (`POST /webhooks/stripe`)
  - **Events**: `invoice.payment_succeeded`, `invoice.payment_failed`, `customer.subscription.deleted`

- ❌ **L5E.3.2**: Sync `BillingUsageLedger` to Stripe usage records
  - **Mechanism**: Background job aggregates daily usage → Stripe Usage Record API

- ❌ **L5E.3.3**: Implement credit-based billing (prepaid credits deducted from `TenantCreditLedger`)

- ❌ **L5E.3.4**: Test: $100 prepaid credits → 1000 inference requests → balance decremented correctly

---

## LAYER 6: Production Hardening (24 Tasks)
**Status**: ❌ 0% COMPLETE  
**Blocking**: None (requires functional system from Layers 0-5)

### L6.1: Security Hardening (8 tasks)

- ❌ **L6.1.1**: Penetration testing (OWASP Top 10)
- ❌ **L6.1.2**: Fix identified vulnerabilities
- ❌ **L6.1.3**: GDPR compliance (data deletion API, audit logs, consent management)
- ❌ **L6.1.4**: Implement Transparent Data Encryption (TDE) on SQL Server
- ❌ **L6.1.5**: Enable Always Encrypted for sensitive columns (`TenantApiKey.KeyHash`)
- ❌ **L6.1.6**: Configure TLS 1.3 for all API endpoints
- ❌ **L6.1.7**: Implement certificate rotation automation (Let's Encrypt + Azure Key Vault)
- ❌ **L6.1.8**: Add Content Security Policy (CSP) headers to Blazor apps

### L6.2: Performance Optimization (10 tasks)

- ❌ **L6.2.1**: Implement DiskANN index for vector search (SQL Server 2025 native)
  - **Speedup**: 10-100x faster than brute-force VECTOR_DISTANCE
  - **Configuration**: `CREATE INDEX ON AtomEmbedding (EmbeddingVector) WITH (TYPE = DISKANN, METRIC = 'cosine')`

- ❌ **L6.2.2**: Load testing: 10,000 concurrent users
  - **Tool**: k6.io or NBomber
  - **Target**: <200ms p95 latency, <1% error rate

- ❌ **L6.2.3**: Identify and fix performance bottlenecks
  - **Sources**: SQL Query Store, Application Insights, custom metrics

- ❌ **L6.2.4**: Implement distributed tracing (OpenTelemetry → Jaeger/Zipkin)

- ❌ **L6.2.5**: Implement log aggregation (Serilog → Elasticsearch/Seq)

- ❌ **L6.2.6**: Add custom dashboards (Grafana visualizations of key metrics)

- ❌ **L6.2.7**: Optimize SQL query plans (analyze via Query Store, add missing indexes)

- ❌ **L6.2.8**: Implement caching strategy (Redis for API responses, SQL Query Store for compiled plans)

- ❌ **L6.2.9**: Tune Service Broker queue readers (adjust `MAX_QUEUE_READERS` based on load)

- ❌ **L6.2.10**: Test: System handles 1000 inferences/sec sustained load without degradation

### L6.3: Operations Automation (6 tasks)

- ❌ **L6.3.1**: Implement HA/DR (SQL Server Always On Availability Groups)

- ❌ **L6.3.2**: Configure automated backups (full daily, differential hourly, transaction log every 15 minutes)

- ❌ **L6.3.3**: Implement backup restore testing (monthly automated restore to test environment)

- ❌ **L6.3.4**: Create runbooks for common incidents (database failover, queue backlog, CDC pipeline stall)

- ❌ **L6.3.5**: Implement Infrastructure as Code (Terraform or Bicep for Azure resources)

- ❌ **L6.3.6**: Set up CI/CD pipelines (GitHub Actions or Azure Pipelines)
  - **Existing**: ✅ `azure-pipelines.yml` exists in repo

---

## Critical Path Summary

**THE BLOCKER**: SQL Database Foundation (Layers 0-1)

**Sequential Dependencies**:
1. **FIRST**: Deploy SQL schema (Layer 0) → Configure features (Layer 1)
2. **SECOND**: Build 7 missing CLR assemblies (Layer 2A-2B)
3. **THIRD**: Deploy CLR procedures, implement `fn_GenerateWithAttention` (Layer 2A.1.3)
4. **FOURTH**: Configure Service Broker, implement autonomous loop (Layer 3)
5. **FIFTH**: Complete API endpoints (Layer 5C)
6. **SIXTH**: Build client applications (Layer 5D)
7. **SEVENTH**: Production hardening (Layer 6)

**Parallelization Opportunities**:
- Layer 0-1 SQL work **PARALLEL TO** CLR assembly development (Layers 2A-2B)
- Layer 5A (Provenance) **PARALLEL TO** Layer 5B (Real-World) **PARALLEL TO** Layer 5D (Clients) **PARALLEL TO** Layer 5E (Integrations)
- Once API is functional (Layer 5C complete), clients/integrations can be built concurrently

**Time Estimates** (conservative, assumes full-time work):

| Layer | Tasks | Status | Remaining Work | Estimate |
|-------|-------|--------|----------------|----------|
| 0 | 18 | 25% | 14 tasks | 2 weeks |
| 1 | 18 | 0% | 18 tasks | 2 weeks |
| 2A | 8 | 25% | 6 tasks | 3 weeks |
| 2B | 6 | 0% | 6 tasks | 2 weeks |
| 3 | 28 | 15% | 24 tasks | 4 weeks |
| 4 | 11 | 10% | 10 tasks | 2 weeks |
| 5A | 15 | 30% | 10 tasks | 2 weeks |
| 5B | 8 | 10% | 7 tasks | 1 week |
| 5C | 18 | 60% | 7 tasks | 1 week |
| 5D | 22 | 0% | 22 tasks | 3 weeks |
| 5E | 12 | 0% | 12 tasks | 2 weeks |
| 6 | 24 | 0% | 24 tasks | 4 weeks |
| **TOTAL** | **188** | **~30%** | **~160** | **28 weeks** |

**Realistic Timeline**: 6-7 months (28 weeks) to full production deployment

---

## Immediate Next Steps (Priority Order)

### Week 1-2: SQL Foundation Emergency

1. **Audit Existing SQL Files**:
   ```powershell
   # Read all 43 procedure files to determine what's implemented
   Get-ChildItem "d:\Repositories\Hartonomous\sql\procedures" -Filter "*.sql" | 
       ForEach-Object { 
           Write-Host "`n=== $($_.Name) ===" -ForegroundColor Cyan
           Get-Content $_.FullName | Select-Object -First 50
       }
   ```

2. **Create Missing Table Definitions**:
   - Extract `dbo.Atom` from `Setup_FILESTREAM.sql` → `sql/tables/dbo.Atom.sql`
   - Create `dbo.AtomEmbedding.sql` (VECTOR(1998) + GEOMETRY columns)
   - Create `dbo.TensorAtom.sql` (WeightsGeometry GEOMETRY(LINESTRING))
   - Create `dbo.ModelLayers.sql`
   - Create `dbo.InferenceRequest.sql` (MEMORY_OPTIMIZED = ON)
   - Create `dbo.TenantApiKey.sql`
   - Create `dbo.AccessPolicy.sql`

3. **Configure Database Features**:
   - Modify `deploy-database.ps1` to accept `-FilestreamPath` parameter (default: `D:\Hartonomous\FileStream`)
   - Run `Setup_FILESTREAM.sql` with configurable path
   - Enable In-Memory OLTP filegroup
   - Enable Service Broker (`ALTER DATABASE Hartonomous SET ENABLE_BROKER`)
   - Enable CDC (`EXEC sys.sp_cdc_enable_db`)
   - Enable Query Store (`ALTER DATABASE Hartonomous SET QUERY_STORE = ON`)

4. **Deploy Schema**:
   ```powershell
   .\scripts\deploy-database.ps1 -ServerInstance "localhost" -Database "Hartonomous" -FilestreamPath "D:\Hartonomous\FileStream"
   ```

5. **Verification**:
   ```sql
   -- Verify all tables exist
   SELECT name FROM sys.tables ORDER BY name;
   
   -- Verify FILESTREAM enabled
   SELECT name, type_desc FROM sys.filegroups WHERE type = 'FD';
   
   -- Verify In-Memory OLTP enabled
   SELECT name FROM sys.tables WHERE is_memory_optimized = 1;
   
   -- Verify Service Broker enabled
   SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
   
   -- Verify CDC enabled
   SELECT is_cdc_enabled FROM sys.databases WHERE name = 'Hartonomous';
   ```

### Week 3-5: CLR Assembly Development

1. **Build Missing CLR Assemblies** (parallel work during SQL deployment):
   - `VectorOperationsSafe.cs` (AVX2 SIMD)
   - `AzureBlobProviderSafe.cs`
   - `GpuVectorOperations.cs` (cuBLAS P/Invoke - UNSAFE)
   - `FileStreamIngestion.cs` (SqlFileStream zero-copy)
   - `VectorAttentionAggregate.cs`
   - `VectorKMeansCluster.cs`
   - `NlpExtractorUnsafe.cs` (spaCy integration)

2. **Deploy CLR Assemblies**:
   ```powershell
   .\scripts\deploy-clr-unsafe.sql  # For on-prem with GPU
   # OR use deploy-clr-safe.sql for cloud deployment
   ```

3. **Implement `fn_GenerateWithAttention`** (CRITICAL PATH):
   - Streaming TVF that yields tokens as SQL rows
   - Calls `sp_HybridVectorSpatialSearch` for candidates
   - Aggregates via `VectorAttentionAggregate`
   - Builds AtomicStream UDT in-memory

### Week 6-9: Autonomous Loop

1. Configure Service Broker (L3.1 tasks)
2. Implement 4-phase procedures (sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn)
3. Test with simple knowledge gap scenario

### Week 10-11: API Completion

1. Implement missing endpoints (L5C.9-L5C.15)
2. Add OpenAPI/Swagger docs
3. Integration testing with Postman/k6

### Week 12-28: Parallel Streams + Hardening

- **Weeks 12-14**: Provenance pipeline (L5A)
- **Weeks 15-17**: Client applications (L5D)
- **Weeks 18-20**: Integration services (L5E)
- **Weeks 21-24**: Performance optimization (L6.2)
- **Weeks 25-28**: Security hardening + operations automation (L6.1 + L6.3)

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| SQL schema conflicts (PayloadLocator vs Payload) | HIGH | HIGH | Schema migration procedure, backward compatibility layer |
| 62GB model ingestion fails | MEDIUM | HIGH | Test with smaller models first, incremental ingestion |
| GPU libraries unavailable | MEDIUM | MEDIUM | AVX2 fallback already planned, cloud deployment uses SAFE CLR |
| Service Broker poison messages | MEDIUM | MEDIUM | DLQ + exponential backoff already planned |
| CDC pipeline stalls | MEDIUM | HIGH | Monitoring dashboard + automated restart logic |
| FILESTREAM path hardcoded | HIGH | LOW | Make configurable via deploy script parameter (planned) |
| Missing procedure implementations | HIGH | HIGH | Audit all 43 files ASAP to determine scope |
| CLR debugging complexity | MEDIUM | MEDIUM | Extensive logging, unit tests before SQL deployment |

---

## Success Metrics

**Week 2**: ✅ SQL database deployed with all tables + features enabled  
**Week 5**: ✅ All 10 CLR assemblies built + deployed  
**Week 9**: ✅ Autonomous loop completes 1 improvement cycle end-to-end  
**Week 11**: ✅ API endpoints functional, Swagger docs published  
**Week 17**: ✅ Blazor WASM PWA deployed to production  
**Week 28**: ✅ System handles 1000 inferences/sec, 10K concurrent users, <200ms p95 latency

---

## Appendix: File Audit Checklist

### SQL Procedures (43 files) - CONTENTS UNKNOWN
Run this command to generate detailed audit:
```powershell
$procedures = Get-ChildItem "d:\Repositories\Hartonomous\sql\procedures" -Filter "*.sql"
$auditReport = @()

foreach ($proc in $procedures) {
    $content = Get-Content $proc.FullName -Raw
    $hasCLR = $content -match "EXTERNAL NAME"
    $hasVectorOps = $content -match "VECTOR_DISTANCE|STDistance"
    $hasServiceBroker = $content -match "BEGIN DIALOG|SEND ON CONVERSATION"
    $hasPREDICT = $content -match "FROM PREDICT"
    
    $auditReport += [PSCustomObject]@{
        FileName = $proc.Name
        UsesCLR = $hasCLR
        UsesVector = $hasVectorOps
        UsesServiceBroker = $hasServiceBroker
        UsesPREDICT = $hasPREDICT
        LineCount = ($content -split "`n").Count
    }
}

$auditReport | Export-Csv "sql_procedures_audit.csv" -NoTypeInformation
$auditReport | Format-Table -AutoSize
```

### CLR Assembly Audit
```powershell
Get-ChildItem "d:\Repositories\Hartonomous\src\SqlClr" -Filter "*.cs" -Recurse | 
    Select-Object Name, @{Name="Functions";Expression={(Get-Content $_.FullName -Raw) -split "\[SqlFunction\]" | Measure-Object | Select-Object -ExpandProperty Count}}
```

### Worker Service Audit
```powershell
# Check CesConsumer functionality
Get-Content "d:\Repositories\Hartonomous\src\CesConsumer\ServiceBrokerMessagePump.cs" | Select-Object -First 100

# Check ModelIngestion functionality
Get-Content "d:\Repositories\Hartonomous\src\ModelIngestion\Processor.cs" | Select-Object -First 100

# Check Neo4jSync functionality
Get-ChildItem "d:\Repositories\Hartonomous\src\Neo4jSync" -Filter "*.cs" -Recurse | ForEach-Object { Write-Host $_.Name; Get-Content $_.FullName | Select-Object -First 50 }
```

---

**Document Status**: COMPLETE HOLISTIC VIEW  
**Next Action**: Execute Week 1-2 SQL Foundation Emergency  
**Owner**: Development Team  
**Review Frequency**: Weekly progress check against this plan
