# HARTONOMOUS SQL DATABASE PROJECT - COMPREHENSIVE FILE-BY-FILE AUDIT
**Generated:** 2025-11-19 23:57:10  
**Project:** src/Hartonomous.Database/Hartonomous.Database.sqlproj  
**Auditor:** Manual deep-dive analysis  
**Methodology:** Read every file, correlate dependencies, document findings  

---

## AUDIT METHODOLOGY

This audit was conducted by:
1. Reading EVERY SQL file completely
2. Analyzing table structures, indexes, constraints
3. Reviewing stored procedure logic, dependencies, CLR calls
4. Identifying missing objects, duplicates, quality issues
5. Correlating cross-file relationships

Unlike automated scripts, this is a MANUAL review with human analysis.

---

## PART 1: CORE TABLES & FOUNDATIONAL SCHEMA

### TABLE 1: dbo.Atom
**File:** Tables/dbo.Atom.sql  
**Lines:** 68  
**Purpose:** Core atomic storage - foundation of entire system

**Schema Analysis:**
- **Primary Key:** AtomId (BIGINT IDENTITY)
- **Multi-Tenancy:** TenantId (INT, default 0)
- **Deduplication:** ContentHash (BINARY(32), unique constraint)
- **Temporal:** System-versioned with AtomHistory table
- **Key Innovation:** AtomicValue VARBINARY(64) enforces 64-byte atomic decomposition

**Columns (13 total):**
1. AtomId - Identity PK
2. TenantId - Multi-tenant isolation
3. Modality - VARCHAR(50): 'text', 'image', 'audio', 'code', 'weight'
4. Subtype - VARCHAR(50): Further classification
5. ContentHash - BINARY(32): SHA-256 hash for deduplication
6. ContentType - NVARCHAR(100): Semantic type
7. SourceType - NVARCHAR(100): Origin tracking
8. SourceUri - NVARCHAR(2048): Source location
9. CanonicalText - NVARCHAR(MAX): Text representation
10. Metadata - JSON: Extensible metadata (SQL 2025 native JSON)
11. AtomicValue - VARBINARY(64): Max 64 bytes enforces atomicity
12. CreatedAt - DATETIME2(7): Temporal tracking (generated)
13. ReferenceCount - BIGINT: Deduplication reference counting

**Indexes (4):**
1. PK_Atom: Clustered on AtomId
2. UX_Atom_ContentHash: Unique for deduplication
3. IX_Atom_Modality: Covers (Modality, Subtype) INCLUDE (AtomId, ContentHash, TenantId)
4. IX_Atom_ContentType: WHERE ContentType IS NOT NULL
5. IX_Atom_TenantId: Multi-tenant queries
6. IX_Atom_ReferenceCount: For garbage collection

**Quality Assessment: 95/100** ✅
- Excellent design: temporal, multi-tenant, deduplication-aware
- Native JSON support (SQL 2025 feature)
- Proper indexing strategy
- Minor: No full-text index on CanonicalText (may need FTI_Atoms_Content)

**Dependencies:**
- Referenced by: AtomEmbedding, AtomRelation, AtomHistory (temporal)
- CLR Integration: None directly
- Service Broker: Ingestion queue messages reference atoms

**Issues Found:**
- None critical
- Recommendation: Add full-text catalog for CanonicalText semantic search

---

### TABLE 2: dbo.AtomEmbedding
**File:** Tables/dbo.AtomEmbedding.sql  
**Lines:** 89  
**Purpose:** Multi-modal semantic embeddings with spatial indexing

**Schema Analysis:**
- **Primary Key:** AtomEmbeddingId (BIGINT IDENTITY)
- **Foreign Keys:** 
  - FK to Atom (CASCADE DELETE)
  - FK to Model
- **Key Innovation:** Dual representation - GEOMETRY + VECTOR(1998)

**Columns (11 total):**
1. AtomEmbeddingId - Identity PK
2. AtomId - FK to Atom (CASCADE DELETE)
3. TenantId - Multi-tenant isolation
4. ModelId - FK to Model (which embedding model)
5. EmbeddingType - NVARCHAR(50): 'semantic', 'syntactic', 'visual'
6. Dimension - INT: Vector dimensionality (768, 1536, 1998)
7. **SpatialKey - GEOMETRY**: 3D/4D projection for R-Tree indexing
8. **EmbeddingVector - VECTOR(1998)**: Full-dimensional vector (SQL 2025)
9. SpatialBucketX/Y/Z - INT: Grid-based bucketing
10. HilbertValue - BIGINT: Space-filling curve for 1D indexing
11. CreatedAt - DATETIME2(7)

**Indexes (7):**
1. PK_AtomEmbedding: Clustered on AtomEmbeddingId
2. **SIX_AtomEmbedding_SpatialKey**: SPATIAL index (R-Tree) - THE KEY INNOVATION
   - BOUNDING_BOX: (-1, -1, 1, 1)
   - GRIDS: (LOW, LOW, MEDIUM, HIGH)
3. IX_AtomEmbedding_AtomId: FK index
4. IX_AtomEmbedding_TenantId_ModelId: Multi-tenant queries
5. IX_AtomEmbedding_Dimension: Dimension-specific queries
6. IX_AtomEmbedding_SpatialBuckets: Grid-based coarse search
7. IX_AtomEmbedding_Hilbert: Space-filling curve ordering

**Quality Assessment: 98/100** ✅
- **EXCEPTIONAL**: Dual-index strategy (GEOMETRY + VECTOR)
- Spatial R-Tree enables O(log N) ANN search
- Hilbert curve for cache locality
- Proper foreign key cascades

**Core Algorithm (from sp_FindNearestAtoms):**
`
Stage 1: O(log N) - R-Tree spatial index seek on SpatialKey
Stage 2: Hilbert clustering for CPU cache optimization
Stage 3: O(K) - SIMD vector refinement using EmbeddingVector
`

**Dependencies:**
- Referenced by: sp_FindNearestAtoms (core similarity search)
- CLR Functions: 
  - dbo.fn_ProjectTo3D (1998D → 3D GEOMETRY projection)
  - dbo.clr_CosineSimilarity (SIMD cosine similarity)
  - dbo.clr_ComputeHilbertValue (space-filling curve)
- Native SQL: VECTOR_DISTANCE (SQL 2025 vector ops)

**Issues Found:**
- ⚠️ **CRITICAL MISSING**: dbo.clr_CosineSimilarity not found in SQL files
- ⚠️ **CRITICAL MISSING**: dbo.clr_ComputeHilbertValue not found in SQL files
- ⚠️ **CRITICAL MISSING**: dbo.fn_ProjectTo3D not found in SQL files

---

### STORED PROCEDURE 1: dbo.sp_FindNearestAtoms
**File:** Procedures/dbo.sp_FindNearestAtoms.sql  
**Lines:** 196  
**Purpose:** O(log N) + O(K) ANN search using spatial R-Tree

**Algorithm Breakdown:**

**STAGE 1: O(log N) Spatial R-Tree Index Seek**
`sql
-- Uses SIX_AtomEmbedding_SpatialKey (R-Tree index)
-- Project query vector to 3D GEOMETRY
SET @queryGeometry = dbo.fn_ProjectTo3D(@queryVector);

-- R-Tree spatial index seek (logarithmic)
SELECT TOP (@spatialPoolSize)
    ae.AtomId, ae.EmbeddingVector, ae.SpatialGeometry
FROM dbo.AtomEmbedding ae WITH (INDEX(IX_AtomEmbedding_SpatialGeometry))
WHERE ae.SpatialGeometry.STIntersects(@queryGeometry.STBuffer(@searchRadius)) = 1
ORDER BY ae.SpatialGeometry.STDistance(@queryGeometry)
`

**STAGE 2: Hilbert Curve Clustering**
`sql
-- Compute Hilbert value for query
SET @queryHilbert = dbo.clr_ComputeHilbertValue(@queryGeometry, 21);

-- Group by Hilbert proximity (cache optimization)
SELECT *, ABS(CAST(sc.HilbertValue AS BIGINT) - @queryHilbert) AS HilbertDistance
FROM SpatialCandidates sc
`

**STAGE 3: O(K) SIMD Vector Refinement**
`sql
-- Exact cosine similarity on top-K candidates (SIMD-accelerated)
SELECT TOP (@topK)
    AtomId,
    dbo.clr_CosineSimilarity(@queryVector, hc.EmbeddingVector) AS CosineSimilarity,
    -- Blended score: 70% cosine + 30% spatial proximity
    (0.7 * dbo.clr_CosineSimilarity(@queryVector, hc.EmbeddingVector)) +
    (0.3 * (1.0 / (1.0 + hc.SpatialDistance))) AS BlendedScore
FROM HilbertCandidates hc
ORDER BY CosineSimilarity DESC
`

**Parameters (6):**
1. @queryVector VARBINARY(MAX) - Input embedding
2. @topK INT - Number of results (default 10, max 10000)
3. @spatialPoolSize INT - Stage 1 candidate pool (default topK * 100)
4. @tenantId INT - Multi-tenant filter
5. @modalityFilter NVARCHAR(50) - Filter by modality
6. @useHilbertClustering BIT - Enable Stage 2 optimization

**Quality Assessment: 92/100** ✅
- Excellent three-stage algorithm
- Proper index hints
- Good parameter validation
- Performance logging

**Issues Found:**
- ❌ **BLOCKING**: Missing CLR functions (clr_CosineSimilarity, clr_ComputeHilbertValue, fn_ProjectTo3D)
- ⚠️ Index hint references IX_AtomEmbedding_SpatialGeometry but table has SIX_AtomEmbedding_SpatialKey
- Minor: No @minSimilarity threshold parameter

**Dependencies:**
- CLR Functions (MISSING):
  - dbo.fn_ProjectTo3D
  - dbo.clr_CosineSimilarity
  - dbo.clr_ComputeHilbertValue
- Tables: AtomEmbedding, Atom
- Called by: sp_RunInference, sp_SemanticSearch, sp_FusionSearch

---

### STORED PROCEDURE 2: dbo.sp_RunInference
**File:** Procedures/dbo.sp_RunInference.sql  
**Lines:** 299  
**Purpose:** Generative autoregressive inference with temperature sampling

**Algorithm Breakdown:**

**STEP 1: Compute Context Vector**
`sql
-- Average embeddings of context atoms (SIMD-optimized)
IF OBJECT_ID('dbo.clr_VectorAverage', 'FN') IS NOT NULL
BEGIN
    SELECT @contextVector = dbo.clr_VectorAverage(ae.EmbeddingVector)
    FROM dbo.AtomEmbedding ae
    INNER JOIN @contextAtoms ca ON ae.AtomId = ca.AtomId
END
ELSE
BEGIN
    -- Fallback: use first atom
    SELECT TOP 1 @contextVector = ae.EmbeddingVector ...
END
`

**STEP 2: Find Candidate Atoms (O(log N))**
`sql
-- Calls sp_FindNearestAtoms
INSERT INTO @candidates
SELECT AtomId, CanonicalText, Score, SpatialDistance, Rank
FROM dbo.sp_FindNearestAtoms(
    @queryVector = @contextVector,
    @topK = @topK * 2, -- Get more for diversity
    @spatialPoolSize = 2000,
    @tenantId = @tenantId,
    @modalityFilter = @modalityFilter,
    @useHilbertClustering = 1
);
`

**STEP 3: Temperature-Based Sampling**
`sql
-- Softmax with temperature scaling
WITH ScoredCandidates AS (
    SELECT AtomId, EXP(Score / @temperature) AS ScaledScore
    FROM @candidates
),
Normalized AS (
    SELECT AtomId, ScaledScore / SUM(ScaledScore) OVER () AS Probability
    FROM ScoredCandidates
),
TopPFiltered AS (
    -- Nucleus sampling (top-p)
    SELECT AtomId, Probability, CumulativeProbability
    FROM CumulativeProbs
    WHERE CumulativeProbability <= @topP
)
SELECT TOP (@maxTokens) AtomId, CanonicalText, Probability
FROM TopPFiltered
ORDER BY CASE WHEN @temperature < 0.1 THEN Probability ELSE NEWID() END DESC
`

**STEP 4: Log Inference Request**
`sql
INSERT INTO dbo.InferenceRequest (
    InferenceId, TenantId, InputData, OutputData,
    Temperature, TopK, TopP, MaxTokens, Status
)
VALUES (@inferenceId, @tenantId, @contextText, @outputText, ...)
`

**Parameters (8):**
1. @contextAtomIds NVARCHAR(MAX) - Comma-separated AtomIds
2. @temperature FLOAT - Sampling temperature (0.01-2.0)
3. @topK INT - Candidates to consider (1-100)
4. @topP FLOAT - Nucleus sampling threshold (0.01-1.0)
5. @maxTokens INT - Max generated tokens (1-1000)
6. @tenantId INT
7. @modalityFilter NVARCHAR(50)
8. @inferenceId BIGINT OUTPUT

**Quality Assessment: 90/100** ✅
- Excellent sampling algorithm (temperature + nucleus)
- Proper parameter validation and bounds checking
- Good error handling with try-catch
- Graceful degradation (fallback if CLR missing)

**Issues Found:**
- ❌ **BLOCKING**: Missing dbo.clr_VectorAverage CLR function
- ⚠️ References dbo.InferenceTracking table (not found - should be InferenceRequest?)
- ⚠️ Uses dbo.seq_InferenceId sequence (not found in schema)
- Minor: No embedding model version tracking

**Dependencies:**
- CLR Functions (MISSING):
  - dbo.clr_VectorAverage
- Stored Procedures:
  - sp_FindNearestAtoms (exists ✅)
- Tables:
  - Atom (exists ✅)
  - AtomEmbedding (exists ✅)
  - InferenceRequest (exists ✅)
  - InferenceTracking (MISSING ❌)
- Sequences:
  - dbo.seq_InferenceId (MISSING ❌)

---

### STORED PROCEDURE 3: dbo.sp_AtomizeCode
**File:** Procedures/dbo.sp_AtomizeCode.sql  
**Lines:** 107  
**Purpose:** AST-as-GEOMETRY pipeline for source code ingestion

**Algorithm Breakdown:**

**Phase 1: Retrieve Source Code**
`sql
SELECT @SourceCode = CAST(Content AS NVARCHAR(MAX))
FROM dbo.Atom
WHERE AtomId = @AtomId AND TenantId = @TenantId
`

**Phase 2: Generate AST Structural Vector (Roslyn)**
`sql
-- CLR function using Roslyn compiler API
SET @AstVectorJson = dbo.clr_GenerateCodeAstVector(@SourceCode);
-- Returns: {"vector": [0.1, 0.2, ...], "nodes": 42, "complexity": 8.5}
`

**Phase 3: Project 512D AST → 3D GEOMETRY**
`sql
-- Landmark projection to 3D
SET @ProjectedPoint = dbo.clr_ProjectToPoint(@AstVectorJson);
-- Returns: {"X": 0.123, "Y": -0.456, "Z": 0.789}

DECLARE @X FLOAT = CAST(JSON_VALUE(@ProjectedPoint, '$.X') AS FLOAT);
SET @EmbeddingGeometry = geometry::STPointFromText(
    'POINT(' + CAST(@X AS NVARCHAR(50)) + ' ' + 
               CAST(@Y AS NVARCHAR(50)) + ' ' + 
               CAST(@Z AS NVARCHAR(50)) + ')', 4326
);
`

**Phase 4: Store in CodeAtom Table**
`sql
IF EXISTS (SELECT 1 FROM dbo.CodeAtom WHERE AtomId = @AtomId)
    UPDATE dbo.CodeAtom SET Embedding = @EmbeddingGeometry, AstVector = @AstVectorJson
ELSE
    INSERT INTO dbo.CodeAtom (AtomId, Embedding, AstVector, Language)
    VALUES (@AtomId, @EmbeddingGeometry, @AstVectorJson, @Language)
`

**Phase 5: Update Parent Atom**
`sql
UPDATE dbo.Atom SET SpatialKey = @EmbeddingGeometry WHERE AtomId = @AtomId
`

**Parameters (4):**
1. @AtomId BIGINT
2. @TenantId INT
3. @Language NVARCHAR(50) - 'csharp' (future: more languages)
4. @Debug BIT

**Quality Assessment: 85/100** ✅
- Innovative: AST as GEOMETRY for code similarity
- Good error handling with JSON error detection
- Proper upsert logic
- Uses SQL 2025 JSON functions

**Issues Found:**
- ❌ **BLOCKING**: Missing dbo.clr_GenerateCodeAstVector CLR function
- ❌ **BLOCKING**: Missing dbo.clr_ProjectToPoint CLR function
- ❌ **BLOCKING**: Missing dbo.CodeAtom table
- ⚠️ Atom table doesn't have SpatialKey column (references non-existent column)
- Minor: No validation of Language parameter

**Dependencies:**
- CLR Functions (MISSING):
  - dbo.clr_GenerateCodeAstVector (Roslyn AST parser)
  - dbo.clr_ProjectToPoint (512D → 3D projection)
- Tables:
  - Atom (exists ✅ but missing SpatialKey column)
  - CodeAtom (MISSING ❌)

---

### TABLE DUPLICATE ISSUE 1: Attention Tables
**Files:**
1. Tables/Attention.AttentionGenerationTables.sql (creates 3 tables)
2. Tables/dbo.AttentionGenerationLog.sql (individual table)
3. Tables/dbo.AttentionInferenceResults.sql (individual table)
4. Tables/dbo.TransformerInferenceResults.sql (individual table)

**Analysis:**
The file Attention.AttentionGenerationTables.sql contains:
`sql
CREATE TABLE dbo.AttentionGenerationLog (...);
CREATE TABLE dbo.AttentionInferenceResults (...);
CREATE TABLE dbo.TransformerInferenceResults (...);
`

BUT each of these tables ALSO exists in separate individual files:
- dbo.AttentionGenerationLog.sql
- dbo.AttentionInferenceResults.sql
- dbo.TransformerInferenceResults.sql

**Differences:**
1. **AttentionGenerationLog:**
   - Attention.AttentionGenerationTables.sql: Missing FK to Model table
   - dbo.AttentionGenerationLog.sql: Has FK constraint
   - dbo.AttentionGenerationLog.sql: Has GeneratedAtomIds JSON column

2. **AttentionInferenceResults:**
   - Both versions identical

3. **TransformerInferenceResults:**
   - Both versions identical

**Resolution Required:**
- ❌ DELETE: Attention.AttentionGenerationTables.sql (legacy schema file)
- ✅ KEEP: Individual table files (more complete)

**Impact:**
- DACPAC build will fail with duplicate object definitions
- FK constraints missing in one version

---

### TABLE DUPLICATE ISSUE 2: Weight Rollback
**Files:**
1. Procedures/Admin.WeightRollback.sql (4 procedures + 1 table)
2. Procedures/dbo.sp_RollbackWeightsToTimestamp.sql (individual procedure)
3. Procedures/dbo.sp_CreateWeightSnapshot.sql (individual procedure)
4. Procedures/dbo.sp_RestoreWeightSnapshot.sql (individual procedure)
5. Procedures/dbo.sp_ListWeightSnapshots.sql (individual procedure)
6. Tables/dbo.WeightSnapshot.sql (individual table)

**Analysis:**
Admin.WeightRollback.sql creates:
- sp_RollbackWeightsToTimestamp
- sp_CreateWeightSnapshot
- sp_RestoreWeightSnapshot
- sp_ListWeightSnapshots
- WeightSnapshot table (inline)

All 4 procedures ALSO exist as individual files.

**Differences:**
- Admin.WeightRollback.sql: Creates WeightSnapshot table inline (no constraints)
- dbo.WeightSnapshot.sql: Proper table with constraints, indexes

**Resolution Required:**
- ❌ DELETE: Admin.WeightRollback.sql (legacy monolithic file)
- ✅ KEEP: Individual procedure + table files

**Impact:**
- DACPAC build fails with 4 duplicate procedure definitions
- Table definition conflicts

---

