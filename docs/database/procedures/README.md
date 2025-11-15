# Database Procedures Reference

**Complete catalog of 91 stored procedures**

**Note**: This is an older catalog. See `docs/database/procedures-reference.md` for the comprehensive catalog with all 91 procedures documented with signatures and examples.

---

## Categories

- **[Ingestion](#ingestion-procedures)** - Atomize and store content (8 procedures)
- **[OODA Loop](#ooda-loop-procedures)** - Autonomous self-improvement (4 procedures)
- **[Search](#search-procedures)** - Semantic, spatial, hybrid search (7 procedures)
- **[Generation](#generation-procedures)** - Content generation, inference (6 procedures)
- **[Reasoning](#reasoning-procedures)** - Chain-of-thought, multi-path reasoning (4 procedures)
- **[Model Management](#model-management-procedures)** - Ingest, distill, rollback (8 procedures)
- **[Spatial Operations](#spatial-operations-procedures)** - KNN, Voronoi, trilateration (5 procedures)
- **[Provenance](#provenance-procedures)** - Lineage tracking, graph queries (6 procedures)
- **[Administration](#administration-procedures)** - Maintenance, optimization (12 procedures)
- **[Utilities](#utility-procedures)** - Helper procedures (14 procedures)

---

## Ingestion Procedures

### sp_AtomizeText

**Purpose:** Decompose text documents into atomic sentences/tokens

**Signature:**

```sql
EXEC dbo.sp_AtomizeText
    @ParentAtomId BIGINT,           -- Parent document atom
    @TextContent NVARCHAR(MAX),     -- Text to atomize
    @ChunkingStrategy NVARCHAR(50), -- 'Sentence', 'Paragraph', 'FixedSize', 'Semantic'
    @OverlapTokens INT = 0;         -- Overlap between chunks (0 = no overlap)
```

**Example:**

```sql
-- Ingest document
INSERT INTO dbo.Atoms (Modality, Subtype, ContentHash, AtomicValue)
VALUES ('text', 'document', HASHBYTES('SHA2_256', 'My Document'), NULL);
DECLARE @ParentId BIGINT = SCOPE_IDENTITY();

-- Atomize into sentences
EXEC dbo.sp_AtomizeText
    @ParentAtomId = @ParentId,
    @TextContent = N'The sky is blue. The ocean is also blue.',
    @ChunkingStrategy = 'Sentence',
    @OverlapTokens = 0;
```

**Result:** 2 sentence atoms stored, deduplicated if already exist

---

### sp_AtomizeImage

**Purpose:** Decompose images into individual RGB pixels

**Signature:**

```sql
EXEC dbo.sp_AtomizeImage
    @ParentAtomId BIGINT,         -- Parent image atom
    @ImageBytes VARBINARY(MAX),   -- Image binary data
    @Format NVARCHAR(20);         -- 'PNG', 'JPEG', 'BMP', 'GIF'
```

**Example:**

```sql
-- Read image from file
DECLARE @ImageData VARBINARY(MAX) = (SELECT BulkColumn FROM OPENROWSET(BULK 'C:\Images\sky.png', SINGLE_BLOB) AS img);

-- Ingest image parent
INSERT INTO dbo.Atoms (Modality, Subtype, ContentHash, AtomicValue)
VALUES ('image', 'png', HASHBYTES('SHA2_256', @ImageData), NULL);
DECLARE @ParentId BIGINT = SCOPE_IDENTITY();

-- Atomize into pixels
EXEC dbo.sp_AtomizeImage
    @ParentAtomId = @ParentId,
    @ImageBytes = @ImageData,
    @Format = 'PNG';
```

**Result:** 1920×1080 = 2,073,600 pixels → ~100,000 unique RGB atoms (95% dedup)

**Performance:** 1920×1080 image = 2.5 seconds (CLR pixel extraction + batch insert)

---

### sp_AtomizeAudio

**Purpose:** Decompose audio files into PCM samples or spectral frames

**Signature:**

```sql
EXEC dbo.sp_AtomizeAudio
    @ParentAtomId BIGINT,         -- Parent audio atom
    @AudioBytes VARBINARY(MAX),   -- Audio binary data
    @Format NVARCHAR(20),         -- 'WAV', 'MP3', 'FLAC'
    @Strategy NVARCHAR(50);       -- 'PCM', 'Spectral', 'MFCC'
```

**Example:**

```sql
EXEC dbo.sp_AtomizeAudio
    @ParentAtomId = @AudioAtomId,
    @AudioBytes = @AudioData,
    @Format = 'WAV',
    @Strategy = 'Spectral';
```

**Result:** Audio decomposed into spectral frames (Fourier transform), stored as atoms

---

### sp_AtomizeModel

**Purpose:** Decompose AI model into individual float32 weights

**Signature:**

```sql
EXEC dbo.sp_AtomizeModel
    @ParentAtomId BIGINT,         -- Parent model atom
    @ModelBytes VARBINARY(MAX),   -- Model binary data
    @Format NVARCHAR(50),         -- 'GGUF', 'SafeTensors', 'ONNX', 'PyTorch'
    @ModelId INT OUTPUT;          -- Returns created ModelId
```

**Example:**

```sql
-- Read GGUF model
DECLARE @ModelData VARBINARY(MAX) = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Models\llama-3-8b-q4.gguf', SINGLE_BLOB) AS m);

-- Ingest model parent
INSERT INTO dbo.Atoms (Modality, Subtype, ContentHash, AtomicValue)
VALUES ('model', 'gguf', HASHBYTES('SHA2_256', @ModelData), NULL);
DECLARE @ParentId BIGINT = SCOPE_IDENTITY();

-- Atomize weights
DECLARE @ModelId INT;
EXEC dbo.sp_AtomizeModel
    @ParentAtomId = @ParentId,
    @ModelBytes = @ModelData,
    @Format = 'GGUF',
    @ModelId = @ModelId OUTPUT;

SELECT @ModelId AS CreatedModelId;
```

**Result:** 8B parameters → ~100M unique float32 values (quantized), stored in `TensorAtomCoefficients`

**Performance:** 8B parameters = 5-10 minutes (GGUF parsing + batch insert)

---

### sp_AtomizeCode

**Purpose:** Decompose source code into functions, classes, statements

**Signature:**

```sql
EXEC dbo.sp_AtomizeCode
    @ParentAtomId BIGINT,
    @SourceCode NVARCHAR(MAX),
    @Language NVARCHAR(50);       -- 'CSharp', 'Python', 'TypeScript', 'SQL'
```

---

### sp_IngestAtom

**Purpose:** High-level ingestion dispatcher - detects content type and calls appropriate atomizer

**Signature:**

```sql
EXEC dbo.sp_IngestAtom
    @ContentType NVARCHAR(100),   -- MIME type: 'text/plain', 'image/png', 'application/octet-stream'
    @Content VARBINARY(MAX),      -- Binary content
    @SourceUri NVARCHAR(500),     -- Origin URL or file path
    @TenantId INT = 0,
    @ParentAtomId BIGINT OUTPUT;
```

**Example:**

```sql
DECLARE @AtomId BIGINT;
EXEC dbo.sp_IngestAtom
    @ContentType = 'image/png',
    @Content = @ImageBytes,
    @SourceUri = 'https://example.com/sky.png',
    @TenantId = 1,
    @ParentAtomId = @AtomId OUTPUT;
```

**Logic:**

- Detects content type from MIME or magic bytes
- Creates parent atom
- Routes to appropriate atomizer (`sp_AtomizeText`, `sp_AtomizeImage`, etc.)
- Generates embeddings via `sp_GenerateEmbedding`

---

### sp_BulkIngestDirectory

**Purpose:** Recursively ingest all files in directory

**Signature:**

```sql
EXEC dbo.sp_BulkIngestDirectory
    @DirectoryPath NVARCHAR(500),
    @Recursive BIT = 1,
    @TenantId INT = 0;
```

**Example:**

```sql
-- Ingest all images in folder
EXEC dbo.sp_BulkIngestDirectory
    @DirectoryPath = 'C:\Images\',
    @Recursive = 1,
    @TenantId = 1;
```

**Note:** Requires `xp_cmdshell` enabled or CLR file I/O

---

### sp_GenerateEmbedding

**Purpose:** Generate semantic embedding for atom

**Signature:**

```sql
EXEC dbo.sp_GenerateEmbedding
    @AtomId BIGINT,
    @ModelName NVARCHAR(200) = NULL,  -- Embedding model (NULL = default)
    @Force BIT = 0;                   -- Regenerate if already exists
```

**Example:**

```sql
-- Generate embedding for atom
EXEC dbo.sp_GenerateEmbedding
    @AtomId = 12345,
    @ModelName = NULL,  -- Uses default
    @Force = 0;
```

**Result:** Creates record in `dbo.AtomEmbeddings` with 3D spatial projection

---

## OODA Loop Procedures

### sp_Analyze

**Purpose:** Monitor system health, detect anomalies, trigger OODA loop

**Signature:**

```sql
EXEC dbo.sp_Analyze;
```

**What it does:**

1. Queries Query Store for slow queries (> 1000ms)
2. Checks DMVs for missing indexes
3. Analyzes `dbo.InferenceRequests` for latency spikes
4. Detects untapped knowledge regions (high density, low velocity)
5. Sends `AnalyzeMessage` to `HypothesizeQueue` via Service Broker

**Runs:** Continuously (activated by Service Broker queue)

---

### sp_Hypothesize

**Purpose:** Generate improvement hypotheses based on observations

**Signature:**

```sql
EXEC dbo.sp_Hypothesize;
```

**What it does:**

1. Receives `AnalyzeMessage` from `AnalyzeQueue`
2. Evaluates observations via rule engine
3. Generates actions (create index, rebuild stats, archive model, discover concepts)
4. Inserts into `dbo.PendingActions`
5. Sends `HypothesizeMessage` to `ActQueue`

**Rules implemented:**

- Missing index → `CreateIndex` action
- Slow query → `UpdateStatistics` action
- Untapped knowledge → `ConceptDiscovery` action
- Cold model → `ArchiveModel` action

---

### sp_Act

**Purpose:** Execute improvement actions

**Signature:**

```sql
EXEC dbo.sp_Act;
```

**What it does:**

1. Receives `HypothesizeMessage` from `HypothesizeQueue`
2. Dequeues actions from `dbo.PendingActions` (priority order)
3. Executes actions:
   - `CreateIndex` → `CREATE NONCLUSTERED INDEX ...`
   - `UpdateStatistics` → `UPDATE STATISTICS ...`
   - `ConceptDiscovery` → `EXEC sp_DiscoverAndBindConcepts`
   - `ArchiveModel` → Compress weights to generating functions
4. Logs results to `dbo.ActionHistory`
5. Sends `ActMessage` to `LearnQueue`

**Safety:** Actions wrapped in TRY/CATCH, rollback on error

---

### sp_Learn

**Purpose:** Measure performance delta, update model weights, restart loop

**Signature:**

```sql
EXEC dbo.sp_Learn;
```

**What it does:**

1. Receives `ActMessage` from `ActQueue`
2. Measures performance delta (before vs after actions)
   - Baseline: Avg latency 24 hours ago to 5 minutes ago
   - Current: Avg latency last 5 minutes
3. Calculates improvement: `(baseline - current) / baseline * 100`
4. Updates model weights via `sp_UpdateModelWeightsFromFeedback` (reward = performance delta)
5. Determines next cycle delay:
   - >20% improvement → restart in 5 minutes
   - >0% improvement → restart in 15 minutes
   - <0% regression → restart in 60 minutes
6. Sends `AnalyzeMessage` to restart loop

**Feedback loop closes here:** Successful optimizations reinforce model weights

---

## Search Procedures

### sp_SemanticSearch

**Purpose:** Find atoms semantically similar to query text

**Signature:**

```sql
EXEC dbo.sp_SemanticSearch
    @QueryText NVARCHAR(MAX),
    @TopK INT = 10,
    @ModelName NVARCHAR(200) = NULL,
    @Modality NVARCHAR(50) = NULL;  -- Filter by modality ('text', 'image', etc.)
```

**Example:**

```sql
EXEC dbo.sp_SemanticSearch
    @QueryText = N'blue ocean water',
    @TopK = 5,
    @ModelName = NULL,
    @Modality = 'text';
```

**Algorithm:**

1. Generate embedding for query via `sp_GenerateEmbedding`
2. Project to 3D via `fn_ProjectTo3D`
3. Spatial KNN on `AtomEmbeddings.SpatialKey`
4. Return top K results with distances

---

### sp_HybridSearch

**Purpose:** Combine spatial filtering with vector distance reranking

**Signature:**

```sql
EXEC dbo.sp_HybridSearch
    @QueryVector VECTOR(1998),
    @QuerySpatialX FLOAT,
    @QuerySpatialY FLOAT,
    @QuerySpatialZ FLOAT,
    @SpatialCandidates INT = 100,
    @FinalTopK INT = 10;
```

**Algorithm:**

1. Spatial filter: Get top 100 candidates via `STDistance()`
2. Vector rerank: Compute exact cosine distance for candidates
3. Return top 10 results

**Performance:** 50ms (10× faster than full vector scan)

---

### sp_FusionSearch

**Purpose:** Weighted combination of vector, keyword, and spatial search

**Signature:**

```sql
EXEC dbo.sp_FusionSearch
    @QueryVector VECTOR(1998),
    @Keywords NVARCHAR(MAX) = NULL,
    @SpatialRegion GEOGRAPHY = NULL,
    @TopK INT = 10,
    @VectorWeight FLOAT = 0.5,
    @KeywordWeight FLOAT = 0.3,
    @SpatialWeight FLOAT = 0.2;
```

**Example:**

```sql
EXEC dbo.sp_FusionSearch
    @QueryVector = @Embedding,
    @Keywords = N'ocean blue water',
    @SpatialRegion = geography::Point(40.7128, -74.0060, 4326), -- NYC
    @TopK = 10,
    @VectorWeight = 0.5,
    @KeywordWeight = 0.3,
    @SpatialWeight = 0.2;
```

**Algorithm:**

1. Compute vector score: `1.0 - VECTOR_DISTANCE('cosine', embedding, query)`
2. Compute keyword score: Full-text search rank / 1000
3. Compute spatial score: `1.0` if within region, `0.0` otherwise
4. Combined score: `(vectorScore * vectorWeight) + (keywordScore * keywordWeight) + (spatialScore * spatialWeight)`
5. Return top K by combined score

---

### sp_CrossModalQuery

**Purpose:** Find atoms of one modality similar to another modality

**Signature:**

```sql
EXEC dbo.sp_CrossModalQuery
    @SourceAtomId BIGINT,
    @TargetModality NVARCHAR(50),
    @TopK INT = 10;
```

**Example:**

```sql
-- Find images similar to text description
EXEC dbo.sp_CrossModalQuery
    @SourceAtomId = @TextAtomId,
    @TargetModality = 'image',
    @TopK = 10;
```

---

### sp_ExactVectorSearch

**Purpose:** Brute-force exact vector search (no approximation)

**Signature:**

```sql
EXEC dbo.sp_ExactVectorSearch
    @QueryVector VECTOR(1998),
    @TopK INT = 10,
    @DistanceMetric NVARCHAR(20) = 'cosine';
```

**Use when:** Dataset small (< 100K vectors) or accuracy critical

---

### sp_SpatialKNN

**Purpose:** K-nearest neighbors via spatial index

**Signature:**

```sql
EXEC dbo.sp_SpatialKNN
    @QueryPoint GEOMETRY,
    @TopK INT = 10,
    @Modality NVARCHAR(50) = NULL;
```

---

### sp_TrilatrationSearch

**Purpose:** Navigate semantic space via 5D coordinates

**Signature:**

```sql
EXEC dbo.sp_TrilatrationSearch
    @CoordX FLOAT,
    @CoordY FLOAT,
    @CoordZ FLOAT,
    @CoordT FLOAT,
    @CoordW FLOAT,
    @Radius FLOAT,
    @TopK INT = 10;
```

---

## Generation Procedures

### sp_GenerateText

**Purpose:** Generate text completion given prompt

**Signature:**

```sql
EXEC dbo.sp_GenerateText
    @Prompt NVARCHAR(MAX),
    @MaxTokens INT = 100,
    @Temperature FLOAT = 0.7,
    @GeneratedText NVARCHAR(MAX) OUTPUT;
```

**Example:**

```sql
DECLARE @Result NVARCHAR(MAX);
EXEC dbo.sp_GenerateText
    @Prompt = N'The sky is',
    @MaxTokens = 50,
    @Temperature = 0.7,
    @GeneratedText = @Result OUTPUT;

SELECT @Result;
-- Output: "blue and clear today. The sun is shining brightly..."
```

---

### sp_GenerateWithAttention

**Purpose:** Generate text using multi-head attention mechanism

**Signature:**

```sql
EXEC dbo.sp_GenerateWithAttention
    @ModelId INT,
    @InputAtomIds NVARCHAR(MAX),    -- Comma-separated atom IDs
    @ContextJson NVARCHAR(MAX) = '{}',
    @MaxTokens INT = 100,
    @Temperature FLOAT = 1.0,
    @TopK INT = 50,
    @TopP FLOAT = 0.9,
    @AttentionHeads INT = 8,
    @TenantId INT = 0;
```

**Used by:** `sp_TransformerStyleInference`

---

### sp_TransformerStyleInference

**Purpose:** Full transformer inference (6 layers, multi-head attention + FFN)

**Signature:**

```sql
EXEC dbo.sp_TransformerStyleInference
    @ProblemId UNIQUEIDENTIFIER,
    @InputSequence NVARCHAR(MAX),
    @ModelId INT = 1,
    @Layers INT = 6,
    @AttentionHeads INT = 8,
    @FeedForwardDim INT = 2048;
```

---

### sp_GenerateImage

**Purpose:** Generate image from text description

**Example:**

```sql
EXEC dbo.sp_GenerateImage
    @Prompt = N'blue sky with clouds',
    @Width = 512,
    @Height = 512,
    @GeneratedAtomId BIGINT OUTPUT;
```

---

### sp_GenerateAudio

**Purpose:** Generate audio from text (TTS)

---

### sp_SpatialNextToken

**Purpose:** Context-aware next token generation via spatial proximity

**Signature:**

```sql
EXEC dbo.sp_SpatialNextToken
    @ContextAtomIds NVARCHAR(MAX),  -- Comma-separated context atom IDs
    @TopK INT = 10,
    @Temperature FLOAT = 1.0;
```

**Algorithm:**

1. Compute context centroid via `fn_GetContextCentroid`
2. Spatial KNN with candidate pool = topK * 4
3. Softmax with temperature: `score = EXP(-1.0 * distance / temperature)`
4. Normalize probabilities
5. Return top K tokens with probabilities

---

## Reasoning Procedures

### sp_ChainOfThoughtReasoning

**Purpose:** Multi-step reasoning with coherence analysis

**Signature:**

```sql
EXEC dbo.sp_ChainOfThoughtReasoning
    @ProblemId UNIQUEIDENTIFIER,
    @InitialPrompt NVARCHAR(MAX),
    @MaxSteps INT = 5,
    @Temperature FLOAT = 0.7;
```

**Algorithm:**

1. Generate reasoning step via `sp_GenerateText`
2. Compute embedding for step
3. Calculate confidence (coherence with previous steps)
4. Store step in temp table
5. Use step as next prompt
6. Repeat for `@MaxSteps`
7. Analyze chain coherence via CLR aggregate `ChainOfThoughtCoherence`
8. Store in `dbo.ReasoningChains`

---

### sp_MultiPathReasoning

**Purpose:** Explore multiple reasoning paths, select best

**Signature:**

```sql
EXEC dbo.sp_MultiPathReasoning
    @ProblemId UNIQUEIDENTIFIER,
    @BasePrompt NVARCHAR(MAX),
    @NumPaths INT = 3,
    @MaxDepth INT = 3,
    @BranchingFactor INT = 2;
```

**Algorithm:**

1. Generate `@NumPaths` independent reasoning paths
2. Each path explores `@MaxDepth` steps
3. Score paths via coherence + answer quality
4. Return best path

---

### sp_SelfConsistencyReasoning

**Purpose:** Generate multiple answers, vote for most consistent

**Signature:**

```sql
EXEC dbo.sp_SelfConsistencyReasoning
    @ProblemId UNIQUEIDENTIFIER,
    @Prompt NVARCHAR(MAX),
    @NumSamples INT = 5,
    @Temperature FLOAT = 0.8;
```

---

### sp_CognitiveActivation

**Purpose:** Activate relevant concepts based on input

---

## Model Management Procedures

### sp_IngestModel

**Purpose:** Ingest AI model into database

**Signature:**

```sql
EXEC dbo.sp_IngestModel
    @ModelName NVARCHAR(200),
    @ModelType NVARCHAR(50),
    @Architecture NVARCHAR(100),
    @ConfigJson NVARCHAR(MAX),
    @ModelBytes VARBINARY(MAX),
    @ParameterCount BIGINT = NULL,
    @TenantId INT = 0,
    @ModelId INT OUTPUT;
```

**Example:**

```sql
DECLARE @ModelId INT;
EXEC dbo.sp_IngestModel
    @ModelName = 'LLaMA-3-8B-Q4',
    @ModelType = 'transformer',
    @Architecture = 'decoder-only',
    @ConfigJson = '{"hidden_size": 4096, "num_layers": 32}',
    @ModelBytes = @GGUFData,
    @ParameterCount = 8000000000,
    @TenantId = 1,
    @ModelId = @ModelId OUTPUT;
```

---

### sp_ExtractStudentModel

**Purpose:** Create smaller student model via knowledge distillation

**Called by:** `sp_DynamicStudentExtraction`

---

### sp_DynamicStudentExtraction

**Purpose:** Orchestrate model distillation with configurable strategies

**Signature:**

```sql
EXEC dbo.sp_DynamicStudentExtraction
    @ParentModelId INT,
    @TargetSizeRatio FLOAT = 0.5,
    @SelectionStrategy NVARCHAR(20) = 'importance';
```

**Strategies:**

- **layer**: Extract first N layers (e.g., layers 0-15 from 32-layer model)
- **random**: Random layer sampling
- **importance**: Keep weights/layers with `ImportanceScore` above threshold

**Example:**

```sql
-- Create 50% size student model
EXEC dbo.sp_DynamicStudentExtraction
    @ParentModelId = 1,
    @TargetSizeRatio = 0.5,
    @SelectionStrategy = 'importance';
```

**Result:** Student model shares atoms with parent (zero storage duplication)

---

### sp_QueryModelWeights

**Purpose:** Query model weights with filtering

**Signature:**

```sql
EXEC dbo.sp_QueryModelWeights
    @ModelId INT,
    @LayerIdx INT = NULL,
    @AtomType NVARCHAR(128) = NULL;
```

**Example:**

```sql
-- Get all attention layer weights
EXEC dbo.sp_QueryModelWeights
    @ModelId = 1,
    @LayerIdx = NULL,
    @AtomType = 'float32-weight';
```

---

### sp_RollbackWeightsToTimestamp

**Purpose:** Restore model weights to specific point in time

**Signature:**

```sql
EXEC dbo.sp_RollbackWeightsToTimestamp
    @TargetDateTime DATETIME2(7),
    @ModelId INT = NULL,
    @DryRun BIT = 1;
```

**Example:**

```sql
-- Preview rollback
EXEC dbo.sp_RollbackWeightsToTimestamp
    @TargetDateTime = '2025-11-01 10:00:00',
    @ModelId = 1,
    @DryRun = 1;

-- Execute rollback
EXEC dbo.sp_RollbackWeightsToTimestamp
    @TargetDateTime = '2025-11-01 10:00:00',
    @ModelId = 1,
    @DryRun = 0;
```

**Uses:** System-versioned `TensorAtomCoefficients` temporal table

---

### sp_CreateWeightSnapshot

**Purpose:** Create named backup of current weights

**Signature:**

```sql
EXEC dbo.sp_CreateWeightSnapshot
    @SnapshotName NVARCHAR(255),
    @ModelId INT = NULL,
    @Description NVARCHAR(MAX) = NULL;
```

---

### sp_RestoreWeightSnapshot

**Purpose:** Restore weights from named snapshot

**Signature:**

```sql
EXEC dbo.sp_RestoreWeightSnapshot
    @SnapshotName NVARCHAR(255),
    @DryRun BIT = 1;
```

---

### sp_ListWeightSnapshots

**Purpose:** List all available weight snapshots

```sql
EXEC dbo.sp_ListWeightSnapshots;
```

---

## Spatial Operations Procedures

### sp_SpatialKNN

**Purpose:** K-nearest neighbors via R-tree spatial index

---

### sp_BuildConceptDomains

**Purpose:** Build Voronoi-like semantic domains for concepts

**Signature:**

```sql
EXEC dbo.sp_BuildConceptDomains;
```

**Algorithm:**

1. Get all concepts with `CentroidSpatialKey` populated
2. For each concept, find nearest neighbor distance
3. Create domain via `STBuffer(nearestDistance / 2)`
4. Update `Concepts.ConceptDomain` column
5. Create spatial index on `ConceptDomain`

---

### sp_DiscoverAndBindConcepts

**Purpose:** Discover new concepts via clustering, bind to atoms

**Called by:** OODA loop (sp_Act)

---

### sp_CompareModelKnowledge

**Purpose:** Compare semantic knowledge between two models

---

### sp_ForwardToNeo4j

**Purpose:** Synchronize atoms to Neo4j for graph queries

---

## Provenance Procedures

### sp_LinkProvenance

**Purpose:** Link atom to its source and derivation chain

---

### sp_AuditProvenanceChain

**Purpose:** Audit complete lineage for atom

---

### sp_ExportProvenance

**Purpose:** Export provenance graph to Neo4j or JSON

---

### sp_ValidateOperationProvenance

**Purpose:** Validate provenance integrity (no broken chains)

---

### sp_QueryLineage

**Purpose:** Query atom lineage (sources, derivations, references)

**Signature:**

```sql
EXEC dbo.sp_QueryLineage
    @AtomId BIGINT,
    @MaxDepth INT = 5,
    @Direction NVARCHAR(20) = 'Both';  -- 'Upstream', 'Downstream', 'Both'
```

---

### sp_EnqueueNeo4jSync

**Purpose:** Queue atom for Neo4j synchronization

---

## Administration Procedures

### sp_OptimizeEmbeddings

**Purpose:** Recompute outdated embeddings

---

### sp_ArchiveModel

**Purpose:** Compress model weights to generating functions

---

### sp_DetectDuplicates

**Purpose:** Find duplicate atoms (shouldn't exist due to ContentHash, but validates)

---

### sp_FindRelatedDocuments

**Purpose:** Find documents sharing atoms with target

---

### sp_FindImpactedAtoms

**Purpose:** Find atoms impacted by change (via provenance)

---

### sp_Converse

**Purpose:** Natural language conversation interface

---

### sp_StartPrimeSearch

**Purpose:** Start Gödel Engine autonomous computation (prime number search)

**Signature:**

```sql
EXEC dbo.sp_StartPrimeSearch
    @RangeStart BIGINT,
    @RangeEnd BIGINT;
```

**Example:**

```sql
-- Search for primes between 1 million and 2 million
EXEC dbo.sp_StartPrimeSearch
    @RangeStart = 1000000,
    @RangeEnd = 2000000;
```

**What happens:**

1. Creates job in `dbo.AutonomousComputeJobs`
2. Sends message to `AnalyzeQueue` to start OODA loop
3. Loop processes incrementally via Service Broker
4. `sp_Learn` updates job state, loops back to `sp_Analyze`
5. Database demonstrates computational autonomy beyond typical AI

---

### sp_TokenizeText

**Purpose:** Tokenize text using BPE/WordPiece

---

### sp_CalculateBill

**Purpose:** Calculate usage billing for tenant

---

### sp_GenerateUsageReport

**Purpose:** Generate usage report for tenant

---

## Utility Procedures

### sp_GetAtomDetails

**Purpose:** Get comprehensive details for atom

---

### sp_GetModelInfo

**Purpose:** Get model metadata and statistics

---

### sp_ListModels

**Purpose:** List all models with metadata

---

### sp_GetEmbeddingStatistics

**Purpose:** Get statistics on embeddings (count, distribution)

---

### sp_ValidateAtomIntegrity

**Purpose:** Validate atom ContentHash matches AtomicValue

---

### sp_RebuildSpatialIndexes

**Purpose:** Rebuild all spatial indexes

---

### sp_RebuildTemporalIndexes

**Purpose:** Rebuild temporal table indexes

---

### sp_GetStorageStatistics

**Purpose:** Get storage usage by modality, tenant

---

### sp_GetDeduplicationStats

**Purpose:** Calculate deduplication ratios

**Example:**

```sql
EXEC dbo.sp_GetDeduplicationStats;
```

**Output:**

```
Modality        TotalAtoms    UniqueAtoms    DeduplicationRatio
image           50000000      1000000        98.0%
text            10000000      5000000        50.0%
model           8000000000    100000000      98.75%
```

---

### sp_CleanupOrphanedAtoms

**Purpose:** Delete atoms with `ReferenceCount = 0`

---

### sp_UpdateReferenceCountstats

**Purpose:** Recalculate reference counts (integrity check)

---

### sp_GetSystemHealth

**Purpose:** Get overall system health metrics

---

### sp_ExportAtoms

**Purpose:** Export atoms to JSON or CSV

---

### sp_ImportAtoms

**Purpose:** Import atoms from JSON or CSV

---

## Performance Notes

| Procedure | Avg Execution Time | Notes |
|-----------|-------------------|-------|
| sp_AtomizeText | 50-200ms | Depends on text length |
| sp_AtomizeImage | 2-5s | 1920×1080 PNG |
| sp_AtomizeModel | 5-10min | 8B parameters GGUF |
| sp_SemanticSearch | 10-50ms | Top 10 results |
| sp_HybridSearch | 50ms | Spatial filter + rerank |
| sp_GenerateText | 100-500ms | 50 tokens |
| sp_TransformerStyleInference | 1-2s | 6 layers |
| sp_RollbackWeightsToTimestamp | 500ms - 2s | Depends on weight count |
| sp_BuildConceptDomains | 5-30s | Depends on concept count |

---

## Related Documentation

- **[Database Schema](../database/tables/)** - Table structures
- **[CLR Functions](../database/clr/)** - CLR function reference
- **[Query Patterns](../queries/)** - Advanced query examples
- **[OODA Loop](../architecture/ooda-loop.md)** - Autonomous self-improvement
