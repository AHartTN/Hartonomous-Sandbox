# Stored Procedure Reference Catalog

**Total Procedures**: 91 (validated against codebase)  
**Categories**: OODA Loop, Atomization, Inference, Provenance, Billing, Search, Reconstruction  
**Validation**: All signatures extracted from `src/Hartonomous.Database/Procedures/`

---

## Quick Reference Table

| Category | Count | Key Procedures |
|----------|-------|----------------|
| OODA Loop | 4 | sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn |
| Atomization | 4 | sp_AtomizeImage_Governed, sp_AtomizeModel_Governed, sp_AtomizeText_Governed, sp_AtomizeCode |
| Reconstruction | 3 | sp_ReconstructImage, sp_ReconstructModelWeights, sp_ReconstructText |
| Search & Inference | 12 | sp_SemanticSearch, sp_HybridSearch, sp_TransformerStyleInference, sp_AttentionInference |
| Provenance | 6 | sp_QueryLineage, sp_AuditProvenanceChain, sp_EnqueueNeo4jSync, sp_LinkProvenance |
| Billing | 3 | sp_InsertBillingUsageRecord_Native, sp_CalculateBill, sp_GenerateUsageReport |
| Query | 3 | sp_FindImagesByColor, sp_FindWeightsByValueRange, sp_QueryModelWeights |
| Advanced Reasoning | 5 | sp_ChainOfThoughtReasoning, sp_SelfConsistencyReasoning, sp_MultiModelEnsemble |
| Weight Management | 4 | sp_RollbackWeightsToTimestamp, sp_CreateWeightSnapshot, sp_RestoreWeightSnapshot |
| Spatial & Graph | 3 | sp_GenerateOptimalPath, sp_BuildConceptDomains, sp_DiscoverAndBindConcepts |
| Other | 44 | Generation, fusion, distillation, deduplication, etc. |

---

## OODA Loop (Autonomous Operations)

### sp_Analyze — Observe Phase

**Signature**:
```sql
EXEC dbo.sp_Analyze
    @LookbackHours INT = 24,
    @MinDurationMs INT = 1000;
```

**Purpose**: Detect slow queries, anomalies, performance degradation

**Returns**: Observation messages to Service Broker HypothesizeQueue

**Example**:
```sql
EXEC sp_Analyze @LookbackHours = 24;
```

---

### sp_Hypothesize — Orient Phase

**Signature**:
```sql
EXEC dbo.sp_Hypothesize;
```

**Purpose**: Generate improvement hypotheses (missing indexes, outdated stats)

**Auto-Activated**: Service Broker activation procedure (reads HypothesizeQueue)

---

### sp_Act — Decide/Act Phase

**Signature**:
```sql
EXEC dbo.sp_Act
    @ActionType NVARCHAR(100),
    @ActionPayload NVARCHAR(MAX);
```

**Purpose**: Execute improvements (CREATE INDEX, UPDATE STATISTICS) in transaction

**Auto-Activated**: Service Broker activation procedure (reads ActQueue)

---

### sp_Learn — Learn Phase

**Signature**:
```sql
EXEC dbo.sp_Learn;
```

**Purpose**: Measure improvements, update confidence scores via Bayesian updates

**Inserts**: AutonomousImprovementHistory table

---

## Atomization Procedures

### sp_AtomizeImage_Governed

**Signature**:
```sql
EXEC dbo.sp_AtomizeImage_Governed
    @ImageBytes VARBINARY(MAX),
    @SourceUri NVARCHAR(500),
    @TenantId UNIQUEIDENTIFIER,
    @ImageAtomId BIGINT OUTPUT;
```

**Logic**:
- Decode image via CLR (System.Drawing)
- Extract RGBA pixels (4 bytes each)
- SHA-256 ContentHash per pixel
- SpatialKey: POINT(R, G, B, 0)
- Insert deduplicated atoms + AtomRelations

---

### sp_AtomizeModel_Governed

**Signature**:
```sql
EXEC dbo.sp_AtomizeModel_Governed
    @ModelBytes VARBINARY(MAX),
    @ModelName NVARCHAR(256),
    @TenantId UNIQUEIDENTIFIER,
    @ModelId INT OUTPUT;
```

**Logic**:
- Parse GGUF/SafeTensors/PyTorch format
- Decompose weights to float32 atoms
- SHA-256 ContentHash per weight
- SpatialKey: POINT(row, col, 0, layerId)
- Insert to TensorAtomCoefficients

---

### sp_AtomizeText_Governed

**Signature**:
```sql
EXEC dbo.sp_AtomizeText_Governed
    @Text NVARCHAR(MAX),
    @SourceUri NVARCHAR(500),
    @TenantId UNIQUEIDENTIFIER,
    @DocumentAtomId BIGINT OUTPUT;
```

**Logic**:
- BPE tokenization (tiktoken)
- 4-byte atom per token ID
- SHA-256 ContentHash
- AtomRelations for reconstruction

---

### sp_AtomizeCode

**Signature**:
```sql
EXEC dbo.sp_AtomizeCode
    @SourceCode NVARCHAR(MAX),
    @Language NVARCHAR(50),
    @CodeAtomId BIGINT OUTPUT;
```

**Logic**: AST decomposition, hierarchical atom structure

---

## Reconstruction Procedures

### sp_ReconstructImage

**Signature**:
```sql
EXEC dbo.sp_ReconstructImage
    @ImageAtomId BIGINT,
    @ImageBytes VARBINARY(MAX) OUTPUT;
```

**Logic**: JOIN AtomRelations → Atoms, reconstruct pixel array, encode PNG/JPEG

---

### sp_ReconstructModelWeights

**Signature**:
```sql
EXEC dbo.sp_ReconstructModelWeights
    @ModelId INT,
    @LayerIdx INT,
    @WeightsBytes VARBINARY(MAX) OUTPUT;
```

**Logic**: JOIN TensorAtomCoefficients → Atoms, reconstruct float32 array

---

### sp_ReconstructText

**Signature**:
```sql
EXEC dbo.sp_ReconstructText
    @DocumentAtomId BIGINT,
    @Text NVARCHAR(MAX) OUTPUT;
```

**Logic**: JOIN AtomRelations → Atoms, decode tokens to text

---

## Search & Inference

### sp_SemanticSearch

**Signature**:
```sql
EXEC dbo.sp_SemanticSearch
    @QueryText NVARCHAR(MAX),
    @TopK INT = 10,
    @UseGeometricIndex BIT = 1;
```

**Logic**:
- Generate embedding (CLR)
- If geometric: trilateration → GEOMETRY R-tree filter → VECTOR rerank
- Else: brute-force VECTOR scan

**Performance**: O(log n) with geometric index, O(n) without

---

### sp_HybridSearch

**Signature**:
```sql
EXEC dbo.sp_HybridSearch
    @QueryText NVARCHAR(MAX),
    @TopK INT = 10,
    @KeywordWeight FLOAT = 0.3,
    @SemanticWeight FLOAT = 0.7;
```

**Logic**: Weighted fusion of full-text search + semantic search

---

### sp_TransformerStyleInference

**Signature**:
```sql
EXEC dbo.sp_TransformerStyleInference
    @InputText NVARCHAR(MAX),
    @ModelId INT,
    @MaxTokens INT = 100,
    @OutputText NVARCHAR(MAX) OUTPUT;
```

**Logic**: Tokenize → load weights → CLR attention → decode output

---

### sp_AttentionInference

**Signature**:
```sql
EXEC dbo.sp_AttentionInference
    @QueryEmbedding VECTOR(1998),
    @KeyEmbeddings NVARCHAR(MAX),
    @ValueEmbeddings NVARCHAR(MAX),
    @NumHeads INT = 8,
    @AttentionOutput VECTOR(1998) OUTPUT;
```

**Logic**: Multi-head attention via CLR (MathNet.Numerics)

---

## Provenance & Lineage

### sp_QueryLineage

**Signature**:
```sql
EXEC dbo.sp_QueryLineage
    @AtomId BIGINT,
    @MaxDepth INT = 10;
```

**Logic**: SQL Graph MATCH traversal, return lineage path

**Example**:
```sql
EXEC sp_QueryLineage @AtomId = 12345, @MaxDepth = 5;
```

---

### sp_AuditProvenanceChain

**Signature**:
```sql
EXEC dbo.sp_AuditProvenanceChain
    @AtomId BIGINT,
    @IncludeAlternatives BIT = 1;
```

**Logic**: Full provenance audit with alternatives (GDPR Article 22)

---

### sp_EnqueueNeo4jSync

**Signature**:
```sql
EXEC dbo.sp_EnqueueNeo4jSync
    @EventType NVARCHAR(100),
    @EventPayload NVARCHAR(MAX);
```

**Logic**: Queue event to Neo4j sync Service Broker queue

---

### sp_LinkProvenance

**Signature**:
```sql
EXEC dbo.sp_LinkProvenance
    @SourceAtomId BIGINT,
    @TargetAtomId BIGINT,
    @RelationType NVARCHAR(50);
```

**Relation Types**: 'DerivedFrom', 'ComponentOf', 'SimilarTo'

---

## Billing & Usage

### sp_InsertBillingUsageRecord_Native

**Signature**:
```sql
EXEC dbo.sp_InsertBillingUsageRecord_Native
    @TenantId UNIQUEIDENTIFIER,
    @OperationType NVARCHAR(100),
    @UsageAmount DECIMAL(18,6),
    @Timestamp DATETIME2;
```

**Performance**: Native compiled (In-Memory OLTP), sub-millisecond

---

### sp_CalculateBill

**Signature**:
```sql
EXEC dbo.sp_CalculateBill
    @TenantId UNIQUEIDENTIFIER,
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @TotalCost DECIMAL(18,2) OUTPUT;
```

**Logic**: Aggregate usage × rates

---

### sp_GenerateUsageReport

**Signature**:
```sql
EXEC dbo.sp_GenerateUsageReport
    @TenantId UNIQUEIDENTIFIER,
    @ReportType NVARCHAR(50);
```

**Report Types**: 'Daily', 'Weekly', 'Monthly'

---

## Query Procedures

### sp_FindImagesByColor

**Signature**:
```sql
EXEC dbo.sp_FindImagesByColor
    @R TINYINT,
    @G TINYINT,
    @B TINYINT,
    @Tolerance INT = 10,
    @TopK INT = 10;
```

**Example**:
```sql
-- Sky blue images
EXEC sp_FindImagesByColor @R=135, @G=206, @B=235, @Tolerance=10;
```

---

### sp_FindWeightsByValueRange

**Signature**:
```sql
EXEC dbo.sp_FindWeightsByValueRange
    @MinValue FLOAT,
    @MaxValue FLOAT,
    @LayerIdx INT = NULL;
```

**Example**:
```sql
-- High-activation weights
EXEC sp_FindWeightsByValueRange @MinValue=0.9, @MaxValue=1.0, @LayerIdx=5;
```

---

### sp_QueryModelWeights

**Signature**:
```sql
EXEC dbo.sp_QueryModelWeights
    @ModelId INT,
    @LayerIdx INT,
    @Region GEOMETRY;
```

**Logic**: Spatial filter via GEOMETRY region in tensor space

---

## Advanced Reasoning

### sp_ChainOfThoughtReasoning

**Signature**:
```sql
EXEC dbo.sp_ChainOfThoughtReasoning
    @Query NVARCHAR(MAX),
    @MaxSteps INT = 5,
    @ReasoningPath NVARCHAR(MAX) OUTPUT;
```

**Logic**: Multi-step reasoning with intermediate thoughts

---

### sp_SelfConsistencyReasoning

**Signature**:
```sql
EXEC dbo.sp_SelfConsistencyReasoning
    @Query NVARCHAR(MAX),
    @NumAttempts INT = 5,
    @MajorityVoteAnswer NVARCHAR(MAX) OUTPUT;
```

**Logic**: N inference attempts → majority vote

---

### sp_MultiModelEnsemble

**Signature**:
```sql
EXEC dbo.sp_MultiModelEnsemble
    @Query NVARCHAR(MAX),
    @ModelIds NVARCHAR(MAX),
    @EnsembleStrategy NVARCHAR(50);
```

**Strategies**: 'Vote', 'WeightedAverage', 'Stacking'

---

### sp_DynamicStudentExtraction

**Signature**:
```sql
EXEC dbo.sp_DynamicStudentExtraction
    @TeacherModelId INT,
    @StudentModelId INT,
    @DistillationStrategy NVARCHAR(50);
```

**Logic**: Knowledge distillation

---

### sp_DetectDuplicates

**Signature**:
```sql
EXEC dbo.sp_DetectDuplicates
    @Modality NVARCHAR(50),
    @MinReferences INT = 2;
```

**Logic**: GROUP BY ContentHash, return dedup stats

---

## Weight Management

### sp_RollbackWeightsToTimestamp

**Signature**:
```sql
EXEC dbo.sp_RollbackWeightsToTimestamp
    @ModelId INT,
    @TargetTimestamp DATETIME2;
```

**Logic**: Temporal tables (FOR SYSTEM_TIME AS OF)

---

### sp_CreateWeightSnapshot

**Signature**:
```sql
EXEC dbo.sp_CreateWeightSnapshot
    @ModelId INT,
    @SnapshotName NVARCHAR(256);
```

---

### sp_RestoreWeightSnapshot

**Signature**:
```sql
EXEC dbo.sp_RestoreWeightSnapshot
    @ModelId INT,
    @SnapshotName NVARCHAR(256);
```

---

### sp_ListWeightSnapshots

**Signature**:
```sql
EXEC dbo.sp_ListWeightSnapshots
    @ModelId INT;
```

---

## Spatial & Graph

### sp_GenerateOptimalPath

**Signature**:
```sql
EXEC dbo.sp_GenerateOptimalPath
    @StartAtomId BIGINT,
    @EndAtomId BIGINT,
    @Path NVARCHAR(MAX) OUTPUT;
```

**Logic**: A* pathfinding in concept graph

---

### sp_BuildConceptDomains

**Signature**:
```sql
EXEC dbo.sp_BuildConceptDomains
    @ConceptIds NVARCHAR(MAX);
```

**Logic**: Voronoi tessellation in GEOMETRY space

---

### sp_DiscoverAndBindConcepts

**Signature**:
```sql
EXEC dbo.sp_DiscoverAndBindConcepts
    @MinClusterSize INT = 10,
    @MaxClusters INT = 100;
```

**Logic**: K-means clustering → emergent concepts

---

## Additional Procedures (Full List)

**Generation**: sp_GenerateText, sp_GenerateImage, sp_GenerateAudio, sp_GenerateVideo, sp_GenerateTextSpatial, sp_GenerateWithAttention, sp_GenerateEventsFromStream

**Fusion**: sp_FusionSearch, sp_FuseMultiModalStreams, sp_OrchestrateSensorStream

**Inference**: sp_ExecuteInference_Activated, sp_SubmitInferenceJob, sp_GetInferenceJobStatus, sp_UpdateInferenceJobStatus, sp_InferenceHistory, sp_EnsembleInference, sp_ScoreWithModel

**Search**: sp_ExactVectorSearch, sp_SemanticFilteredSearch, sp_SemanticSimilarity, sp_CrossModalQuery, sp_FindRelatedDocuments, sp_SpatialNextToken

**Features**: sp_ComputeSemanticFeatures, sp_ComputeAllSemanticFeatures, sp_ExtractMetadata

**Advanced**: sp_MultiPathReasoning, sp_CognitiveActivation, sp_Converse, sp_CompareModelKnowledge, sp_ValidateOperationProvenance, sp_FindImpactedAtoms

**Maintenance**: sp_StartPrimeSearch, sp_TokenizeText, sp_TextToEmbedding, sp_ResolveTenantGuid

**Deduplication**: Deduplication.SimilarityCheck

**Provenance**: sp_ExportProvenance, sp_ForwardToNeo4j_Activated

**Legacy/Migration**: sp_MigratePayloadLocatorToFileStream (NOTE: Migration only, NO FILESTREAM in production)

**Native Compiled** (In-Memory OLTP):
- sp_InsertBillingUsageRecord_Native
- sp_GetInferenceCacheHit_Native
- sp_InsertInferenceCache_Native
- sp_GetCachedActivation_Native
- sp_InsertCachedActivation_Native
- sp_InsertSessionPath_Native
- sp_GetAtomEmbeddingMetadata_Native
- sp_FindAtomEmbeddingsBySpatialBucket_Native
- sp_DecomposeEmbeddingToAtomic

---

## Naming Conventions

| Suffix | Meaning | Example |
|--------|---------|---------|
| `_Governed` | State machine governed ingestion | sp_AtomizeImage_Governed |
| `_Native` | Natively compiled (In-Memory OLTP) | sp_InsertBillingUsageRecord_Native |
| `_Activated` | Service Broker activation procedure | sp_ForwardToNeo4j_Activated |
| `_Atomic` | Atomic decomposition variant | sp_DecomposeEmbeddingToAtomic |

---

## Summary

**Total**: 91 procedures  
**All execute in-process in SQL Server** — no external API calls  
**Categories**: OODA, atomization, reconstruction, search, inference, provenance, billing, spatial, graph  
**Validation**: Extracted from `src/Hartonomous.Database/Procedures/`

**Key Insight**: Database IS the AI runtime. All intelligence executes via stored procedures + CLR.
