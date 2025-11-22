# SQL Schema Audit Report - Part 12
## Reasoning, Search, and Job Management Procedures

**Audit Date:** 2024-12-XX  
**Files Analyzed:** 5 procedures (batch 1 of 3)  
**Database Project:** Hartonomous.Database  
**Scope:** Stored procedures - reasoning, search, job management

---

## Executive Summary

Part 12 analyzes 15 procedures across three batches:
- **Batch 1 (this file):** 5 procedures - reasoning procedures (3), duplicate detection (1), concept discovery (1)
- **Batch 2:** 5 procedures - search procedures
- **Batch 3:** 5 procedures - job management, domain building, view

**Batch 1 Average Quality Score:** 76/100

**Key Findings:**
1. **Advanced Reasoning Patterns:** Three sophisticated reasoning procedures implementing chain-of-thought, multi-path exploration, and self-consistency
2. **Missing CLR Aggregates:** ChainOfThoughtCoherence and SelfConsistency CLR functions referenced but not found
3. **Missing Tables:** ReasoningChains, MultiPathReasoning, SelfConsistencyResults tables missing
4. **Good Vector Operations:** Proper use of VECTOR(1998) and VECTOR_DISTANCE
5. **Architectural Compliance:** Set-based operations replacing WHILE loops where possible

---

## Files Analyzed (Batch 1)

### 1. sp_ChainOfThoughtReasoning
**Path:** `Procedures/dbo.sp_ChainOfThoughtReasoning.sql`  
**Type:** Stored Procedure (Advanced Reasoning)  
**Quality Score:** 72/100

#### Purpose
Implements chain-of-thought reasoning by generating multi-step reasoning chains with coherence analysis.

#### Schema Analysis

**Parameters:**
```sql
@ProblemId UNIQUEIDENTIFIER          -- Problem identifier
@InitialPrompt NVARCHAR(MAX)          -- Starting prompt
@MaxSteps INT = 5                     -- Maximum reasoning steps
@Temperature FLOAT = 0.7              -- Generation temperature
@Debug BIT = 0                        -- Debug output flag
```

**Dependencies:**
- `dbo.sp_GenerateText` - Text generation (analyzed Part 7)
- `dbo.sp_TextToEmbedding` - Embedding generation (analyzed Part 8)
- `dbo.ChainOfThoughtCoherence` - CLR aggregate function ❌ NOT FOUND
- `dbo.ReasoningChains` - Storage table ❌ NOT FOUND

**Key Operations:**
1. Iterates through reasoning steps (WHILE loop - MUST be optimized)
2. Generates response text for each step
3. Creates embedding for coherence analysis
4. Stores reasoning chain with JSON serialization
5. Analyzes coherence using CLR aggregate

#### Technical Details

**Table Variables:**
```sql
@ReasoningSteps TABLE (
    StepNumber INT,
    Prompt NVARCHAR(MAX),
    Response NVARCHAR(MAX),
    ResponseVector VECTOR(1998),      -- ✅ SQL Server 2025 native vector
    Confidence FLOAT,
    StepTime DATETIME2
)
```

**Storage Format:**
```sql
INSERT INTO dbo.ReasoningChains (
    ProblemId,
    ReasoningType,
    ChainData,                         -- JSON serialized steps
    TotalSteps,
    DurationMs,
    CoherenceMetrics,                  -- JSON from CLR aggregate
    CreatedAt
)
```

#### Issues Identified

**CRITICAL:**
1. ❌ **Missing Table:** `dbo.ReasoningChains` table does not exist
   - Impact: Procedure will fail at INSERT
   - Required schema:
     ```sql
     CREATE TABLE dbo.ReasoningChains (
         ReasoningChainId BIGINT IDENTITY PRIMARY KEY,
         ProblemId UNIQUEIDENTIFIER NOT NULL,
         ReasoningType NVARCHAR(50) NOT NULL,
         ChainData NVARCHAR(MAX) NOT NULL,      -- JSON
         TotalSteps INT NOT NULL,
         DurationMs INT NOT NULL,
         CoherenceMetrics NVARCHAR(MAX),        -- JSON
         CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
         CONSTRAINT CHK_ReasoningChains_ChainData CHECK (ISJSON(ChainData) = 1),
         CONSTRAINT CHK_ReasoningChains_CoherenceMetrics CHECK (CoherenceMetrics IS NULL OR ISJSON(CoherenceMetrics) = 1)
     );
     ```

2. ❌ **Missing CLR Function:** `dbo.ChainOfThoughtCoherence` aggregate function not found
   - Usage: `SELECT @CoherenceAnalysis = dbo.ChainOfThoughtCoherence(StepNumber, CAST(ResponseVector AS NVARCHAR(MAX))) FROM @ReasoningSteps;`
   - Expected to analyze vector coherence across reasoning steps
   - MUST return JSON with coherence metrics

**HIGH:**
3. ⚠️ **WHILE Loop:** Iterative step generation - architectural comment says "set-based" but uses WHILE
   - Current: Sequential step generation
   - Comment claims: "PARADIGM-COMPLIANT: Generate reasoning steps in a table, then use CLR aggregate"
   - Reality: Still uses WHILE loop for generation
   - MUST optimize with parallel generation for independent steps

4. ⚠️ **Hardcoded Confidence:** `DECLARE @Confidence FLOAT = 0.8;` - MUST calculate from response quality
   - No actual confidence computation
   - Missing coherence scoring per step

5. ⚠️ **String Concatenation:** `SET @CurrentPrompt = 'Continue reasoning: ' + @StepResponse;`
   - Basic prompt chaining
   - No sophisticated prompt engineering

**MEDIUM:**
6. ⚠️ **No TenantId:** Missing multi-tenancy support
7. ⚠️ **No Index Recommendations:** ReasoningChains table would need indexes on ProblemId, ReasoningType, CreatedAt

#### Strengths
- ✅ Uses native VECTOR(1998) type
- ✅ JSON serialization for complex data structures
- ✅ Duration tracking for performance monitoring
- ✅ Debug flag for troubleshooting
- ✅ Proper DATETIME2 usage (UTC)

#### REQUIRED FIXES
1. **CRITICAL:** Create `dbo.ReasoningChains` table with proper schema
2. **CRITICAL:** Implement `dbo.ChainOfThoughtCoherence` CLR aggregate
3. **HIGH:** Replace hardcoded confidence with computed coherence scores
4. **MEDIUM:** Add TenantId for multi-tenancy
5. **MEDIUM:** IMPLEMENT parallel step generation for performance
6. **LOW:** Enhance prompt engineering for better reasoning chains

#### Cross-References
- **Calls:** sp_GenerateText (Part 7), sp_TextToEmbedding (Part 8)
- **Pattern:** Similar to sp_MultiPathReasoning (this part)
- **Related:** Advanced reasoning infrastructure for OODA Orient phase

---

### 2. sp_MultiPathReasoning
**Path:** `Procedures/dbo.sp_MultiPathReasoning.sql`  
**Type:** Stored Procedure (Advanced Reasoning)  
**Quality Score:** 74/100

#### Purpose
Explores multiple reasoning paths simultaneously, scores each path, and identifies the best solution approach.

#### Schema Analysis

**Parameters:**
```sql
@ProblemId UNIQUEIDENTIFIER          -- Problem identifier
@BasePrompt NVARCHAR(MAX)            -- Base reasoning prompt
@NumPaths INT = 3                    -- Number of parallel paths
@MaxDepth INT = 3                    -- Steps per path
@BranchingFactor INT = 2             -- Branches per step (unused)
@Debug BIT = 0                       -- Debug flag
```

**Dependencies:**
- `dbo.sp_GenerateText` - Text generation
- `dbo.MultiPathReasoning` - Storage table ❌ NOT FOUND

**Key Operations:**
1. Generates multiple reasoning paths in parallel
2. Explores each path to specified depth
3. Scores paths based on quality metrics
4. Identifies best path by average score
5. Stores reasoning tree as JSON

#### Technical Details

**Table Variables:**
```sql
@ReasoningTree TABLE (
    PathId INT,
    StepNumber INT,
    BranchId INT,                      -- Currently always 1 (no actual branching)
    Prompt NVARCHAR(MAX),
    Response NVARCHAR(MAX),
    Score FLOAT,
    StepTime DATETIME2
)
```

**Storage Format:**
```sql
INSERT INTO dbo.MultiPathReasoning (
    ProblemId,
    BasePrompt,
    NumPaths,
    MaxDepth,
    BestPathId,
    ReasoningTree,                     -- JSON serialized tree
    DurationMs,
    CreatedAt
)
```

#### Issues Identified

**CRITICAL:**
1. ❌ **Missing Table:** `dbo.MultiPathReasoning` table does not exist
   - Impact: Procedure will fail at INSERT
   - Required schema:
     ```sql
     CREATE TABLE dbo.MultiPathReasoning (
         ReasoningId BIGINT IDENTITY PRIMARY KEY,
         ProblemId UNIQUEIDENTIFIER NOT NULL,
         BasePrompt NVARCHAR(MAX) NOT NULL,
         NumPaths INT NOT NULL,
         MaxDepth INT NOT NULL,
         BestPathId INT NOT NULL,
         ReasoningTree NVARCHAR(MAX) NOT NULL,  -- JSON
         DurationMs INT NOT NULL,
         CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
         CONSTRAINT CHK_MultiPathReasoning_Tree CHECK (ISJSON(ReasoningTree) = 1)
     );
     ```

**HIGH:**
2. ⚠️ **Unused Parameter:** `@BranchingFactor` parameter declared but never used
   - Comment: "MUST branch here in full implementation"
   - Current: Linear paths only, no actual branching
   - BranchId always set to 1

3. ⚠️ **Fake Scoring:** `UPDATE @ReasoningTree SET Score = Score + (RAND() * 0.4 - 0.2);`
   - Adds random noise instead of real quality metrics
   - MUST evaluate coherence, completeness, factuality

4. ⚠️ **High Temperature:** `@temperature = 0.9` for exploration
   - Good for diversity
   - will reduce coherence
   - MUST be configurable per use case

**MEDIUM:**
5. ⚠️ **No TenantId:** Missing multi-tenancy support
6. ⚠️ **Nested WHILE Loops:** Outer loop for paths, inner loop for depth
   - MUST parallelize path generation
   - Each path is independent

7. ⚠️ **Simple Path Selection:** Only uses AVG(Score)
   - MUST IMPLEMENT diversity, coherence, convergence
   - No analysis of path similarity

#### Strengths
- ✅ Explores multiple solution approaches
- ✅ Higher temperature for creative exploration
- ✅ JSON serialization of complex tree structure
- ✅ Duration tracking
- ✅ Proper error handling structure

#### REQUIRED FIXES
1. **CRITICAL:** Create `dbo.MultiPathReasoning` table
2. **HIGH:** Implement real scoring metrics (coherence, completeness, factuality)
3. **HIGH:** Implement actual branching logic using @BranchingFactor
4. **MEDIUM:** Parallelize path generation for performance
5. **MEDIUM:** Add TenantId for multi-tenancy
6. **MEDIUM:** Enhance path selection with diversity analysis
7. **LOW:** Add path comparison and convergence analysis

#### Cross-References
- **Calls:** sp_GenerateText (Part 7)
- **Pattern:** Similar to sp_ChainOfThoughtReasoning, sp_SelfConsistencyReasoning
- **Related:** OODA Orient phase - exploring multiple hypotheses

---

### 3. sp_SelfConsistencyReasoning
**Path:** `Procedures/dbo.sp_SelfConsistencyReasoning.sql`  
**Type:** Stored Procedure (Advanced Reasoning)  
**Quality Score:** 75/100

#### Purpose
Generates multiple reasoning samples for the same problem and uses consensus analysis to identify the most reliable answer.

#### Schema Analysis

**Parameters:**
```sql
@ProblemId UNIQUEIDENTIFIER          -- Problem identifier
@Prompt NVARCHAR(MAX)                -- Reasoning prompt
@NumSamples INT = 5                  -- Number of samples to generate
@Temperature FLOAT = 0.8             -- Generation temperature
@Debug BIT = 0                       -- Debug flag
```

**Dependencies:**
- `dbo.sp_GenerateText` - Text generation
- `dbo.sp_TextToEmbedding` - Embedding generation
- `dbo.SelfConsistency` - CLR aggregate function ❌ NOT FOUND
- `dbo.SelfConsistencyResults` - Storage table ❌ NOT FOUND

**Key Operations:**
1. Generates multiple independent samples
2. Creates embeddings for both full reasoning and final answers
3. Uses CLR aggregate to find consensus
4. Extracts consensus metrics from JSON
5. Stores results with sample data

#### Technical Details

**Table Variables:**
```sql
@Samples TABLE (
    SampleId INT,
    Response NVARCHAR(MAX),
    ResponsePathVector VECTOR(1998),   -- Full reasoning embedding
    ResponseAnswerVector VECTOR(1998), -- Final answer embedding
    Confidence FLOAT,
    SampleTime DATETIME2
)
```

**CLR Aggregate Usage:**
```sql
SELECT @ConsensusResult = dbo.SelfConsistency(
    CAST(ResponsePathVector AS NVARCHAR(MAX)),
    CAST(ResponseAnswerVector AS NVARCHAR(MAX)),
    Confidence
)
FROM @Samples;
```

**Expected JSON Output:**
```json
{
  "consensus_answer": "...",
  "agreement_ratio": 0.85,
  "num_supporting_samples": 4,
  "avg_confidence": 0.82
}
```

#### Issues Identified

**CRITICAL:**
1. ❌ **Missing Table:** `dbo.SelfConsistencyResults` table does not exist
   - Required schema:
     ```sql
     CREATE TABLE dbo.SelfConsistencyResults (
         ResultId BIGINT IDENTITY PRIMARY KEY,
         ProblemId UNIQUEIDENTIFIER NOT NULL,
         Prompt NVARCHAR(MAX) NOT NULL,
         NumSamples INT NOT NULL,
         ConsensusAnswer NVARCHAR(MAX),
         AgreementRatio FLOAT,
         SampleData NVARCHAR(MAX) NOT NULL,     -- JSON
         ConsensusMetrics NVARCHAR(MAX),        -- JSON
         DurationMs INT NOT NULL,
         CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
         CONSTRAINT CHK_SelfConsistency_SampleData CHECK (ISJSON(SampleData) = 1),
         CONSTRAINT CHK_SelfConsistency_Metrics CHECK (ConsensusMetrics IS NULL OR ISJSON(ConsensusMetrics) = 1)
     );
     ```

2. ❌ **Missing CLR Function:** `dbo.SelfConsistency` aggregate function not found
   - MUST analyze vector similarity across samples
   - MUST identify consensus clusters
   - MUST return JSON with consensus metrics

**HIGH:**
3. ⚠️ **Naive Answer Extraction:** `DECLARE @FinalAnswer NVARCHAR(MAX) = REVERSE(SUBSTRING(REVERSE(@SampleResponse), 1, CHARINDEX('.', REVERSE(@SampleResponse)) - 1));`
   - Extracts last sentence by finding last period
   - Breaks if no period exists
   - Doesn't handle multi-sentence answers
   - MUST use NLP or structured output format

4. ⚠️ **WHILE Loop:** Sequential sample generation
   - MUST parallelize since samples are independent
   - Would significantly improve performance for large @NumSamples

5. ⚠️ **Hardcoded Confidence:** `Confidence = 0.8` for all samples
   - MUST vary based on response quality
   - No actual confidence calculation

**MEDIUM:**
6. ⚠️ **No TenantId:** Missing multi-tenancy support
7. ⚠️ **Vector Cast to NVARCHAR:** `CAST(ResponsePathVector AS NVARCHAR(MAX))`
   - CLR function MUST accept VARBINARY or VECTOR type directly
   - Casting vectors to strings is inefficient

8. ⚠️ **No Null Handling:** `TRY_CAST(JSON_VALUE(...) AS FLOAT)` without null checks
   - MUST validate JSON structure before extraction

#### Strengths
- ✅ Two-level embedding analysis (full reasoning + final answer)
- ✅ JSON serialization for consensus metrics
- ✅ Duration tracking
- ✅ Proper UTC timestamps
- ✅ Good parameter defaults

#### REQUIRED FIXES
1. **CRITICAL:** Create `dbo.SelfConsistencyResults` table
2. **CRITICAL:** Implement `dbo.SelfConsistency` CLR aggregate
3. **HIGH:** Improve answer extraction with NLP or structured formats
4. **HIGH:** Parallelize sample generation
5. **HIGH:** Implement real confidence scoring
6. **MEDIUM:** Add TenantId for multi-tenancy
7. **MEDIUM:** Optimize vector handling in CLR function
8. **LOW:** Add comprehensive null handling for JSON extraction

#### Cross-References
- **Calls:** sp_GenerateText (Part 7), sp_TextToEmbedding (Part 8)
- **Pattern:** Similar to sp_ChainOfThoughtReasoning, sp_MultiPathReasoning
- **Related:** Ensemble reasoning for high-stakes decisions

---

### 4. sp_DetectDuplicates
**Path:** `Procedures/dbo.sp_DetectDuplicates.sql`  
**Type:** Stored Procedure (Data Quality)  
**Quality Score:** 82/100

#### Purpose
Identifies duplicate atoms using vector similarity analysis with configurable threshold and batch size.

#### Schema Analysis

**Parameters:**
```sql
@SimilarityThreshold FLOAT = 0.95    -- Minimum similarity to IMPLEMENT duplicate
@BatchSize INT = 1000                -- Limit result count
@TenantId INT = 0                    -- Multi-tenancy support ✅
```

**Dependencies:**
- `dbo.AtomEmbedding` - Embedding storage (Part 1)
- `dbo.Atom` - Atom metadata (Part 1)

**Key Operations:**
1. Self-join on AtomEmbedding to find similar pairs
2. Uses VECTOR_DISTANCE with cosine metric
3. Filters by tenant
4. Avoids duplicate pairs with `ae1.AtomId < ae2.AtomId`
5. Returns top matches ordered by similarity

#### Technical Details

**Core Query:**
```sql
INSERT INTO @DuplicateGroups
SELECT TOP (@BatchSize)
    ae1.AtomId AS PrimaryAtomId,
    ae2.AtomId AS DuplicateAtomId,
    1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, ae2.EmbeddingVector) AS Similarity
FROM dbo.AtomEmbedding ae1
INNER JOIN dbo.AtomEmbedding ae2 
    ON ae1.ModelId = ae2.ModelId 
    AND ae1.AtomId < ae2.AtomId              -- ✅ Avoids duplicate pairs
INNER JOIN dbo.Atom a1 ON ae1.AtomId = a1.AtomId
INNER JOIN dbo.Atom a2 ON ae2.AtomId = a2.AtomId
WHERE a1.TenantId = @TenantId
      AND a2.TenantId = @TenantId
      AND (1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, ae2.EmbeddingVector)) >= @SimilarityThreshold
ORDER BY Similarity DESC;
```

**Output:**
```sql
SELECT 
    dg.PrimaryAtomId,
    dg.DuplicateAtomId,
    dg.Similarity,
    a1.ContentHash AS PrimaryHash,         -- ✅ Useful for validation
    a2.ContentHash AS DuplicateHash,
    a1.CreatedAt AS PrimaryCreated,
    a2.CreatedAt AS DuplicateCreated
FROM @DuplicateGroups dg
INNER JOIN dbo.Atom a1 ON dg.PrimaryAtomId = a1.AtomId
INNER JOIN dbo.Atom a2 ON dg.DuplicateAtomId = a2.AtomId
ORDER BY dg.Similarity DESC;
```

#### Issues Identified

**HIGH:**
1. ⚠️ **Performance:** Self-join on AtomEmbedding is O(N²)
   - Will be slow for large datasets
   - VECTOR_DISTANCE computed twice (WHERE and SELECT)
   - MUST use indexed approximate nearest neighbor search

2. ⚠️ **No Deduplication Action:** Only detects duplicates, doesn't merge or mark them
   - MUST have @Action parameter ('detect', 'merge', 'mark')
   - No update to Atom table with duplicate status

**MEDIUM:**
3. ⚠️ **Fixed BatchSize:** Hardcoded to 1000
   - Large datasets would need multiple runs
   - MUST support pagination or cursor-based iteration

4. ⚠️ **No Model Filtering:** Joins on `ae1.ModelId = ae2.ModelId`
   - Forces same model for both embeddings
   - will miss cross-model duplicates
   - MUST be optional parameter

5. ⚠️ **ContentHash Not Used:** Joins fetch ContentHash but doesn't use it
   - MUST optimize with early filter: `WHERE a1.ContentHash = a2.ContentHash`
   - Exact hash match = guaranteed duplicate

**LOW:**
6. ⚠️ **No Transaction:** Detect-only operation doesn't need transaction
   - TRY/CATCH block is good
   - Return codes properly used

#### Strengths
- ✅ **Multi-Tenancy:** Properly filters by TenantId
- ✅ **Duplicate Pair Prevention:** `ae1.AtomId < ae2.AtomId` avoids (A,B) and (B,A)
- ✅ **Native Vector Operations:** Uses VECTOR_DISTANCE with cosine metric
- ✅ **Configurable Threshold:** Flexible similarity cutoff
- ✅ **Error Handling:** TRY/CATCH with proper RAISERROR
- ✅ **Metadata Output:** Includes ContentHash and timestamps for validation

#### REQUIRED FIXES
1. **HIGH:** Optimize with approximate nearest neighbor index (when available)
2. **HIGH:** Add deduplication actions (merge, mark, delete)
3. **HIGH:** Pre-filter by ContentHash for exact duplicates
4. **MEDIUM:** Add pagination support for large datasets
5. **MEDIUM:** Make model matching optional
6. **MEDIUM:** Add logging to track deduplication operations
7. **LOW:** IMPLEMENT clustering approach for transitive duplicate groups

#### Cross-References
- **Uses:** Atom (Part 1), AtomEmbedding (Part 1)
- **Related:** Data quality and storage optimization
- **Pattern:** MUST integrate with sp_DiscoverAndBindConcepts for cluster-based deduplication

---

### 5. sp_DiscoverAndBindConcepts
**Path:** `Procedures/dbo.sp_DiscoverAndBindConcepts.sql`  
**Type:** Stored Procedure (Unsupervised Learning Pipeline)  
**Quality Score:** 78/100

#### Purpose
End-to-end orchestration of concept discovery using DBSCAN clustering and atom-to-concept binding based on centroid similarity.

#### Schema Analysis

**Parameters:**
```sql
@MinClusterSize INT = 10              -- Minimum atoms per concept
@CoherenceThreshold FLOAT = 0.7       -- Minimum cluster coherence
@MaxConcepts INT = 100                -- Maximum concepts to discover
@SimilarityThreshold FLOAT = 0.6      -- Binding similarity threshold
@MaxConceptsPerAtom INT = 5           -- Unused in current implementation
@TenantId INT = 0                     -- Multi-tenancy support ✅
@DryRun BIT = 0                       -- Preview without persisting
```

**Dependencies:**
- `dbo.fn_DiscoverConcepts` - Clustering function (analyzed Part 10)
- `provenance.Concepts` - Concept storage (not yet analyzed)
- `provenance.AtomConcepts` - Binding table (not yet analyzed)
- `provenance.ConceptEvolution` - Evolution tracking (not yet analyzed)
- `dbo.fn_BindAtomsToCentroid` - Atom binding function ❌ NOT FOUND

**Key Operations:**
1. **Phase 1:** Discover concepts via DBSCAN clustering
2. **Phase 2:** Persist concepts with generated metadata
3. **Phase 3:** Bind atoms to concepts using centroid similarity
4. **Phase 4:** Mark primary concept per atom (highest similarity)
5. **Phase 5:** Update atom counts and track evolution

#### Technical Details

**Discovery Phase:**
```sql
DECLARE @DiscoveredConcepts TABLE (
    Centroid VARBINARY(MAX),
    AtomCount INT,
    Coherence FLOAT,
    HilbertValue INT
);

INSERT INTO @DiscoveredConcepts
SELECT * FROM dbo.fn_DiscoverConcepts(
    @MinClusterSize,
    @CoherenceThreshold,
    @MaxConcepts,
    @TenantId
);
```

**Persistence with OUTPUT Clause:**
```sql
DECLARE @InsertedConcepts TABLE (
    ConceptId BIGINT,
    Centroid VARBINARY(MAX),
    AtomCount INT,
    Coherence FLOAT,
    HilbertValue INT
);

INSERT INTO provenance.Concepts (...)
OUTPUT 
    INSERTED.ConceptId,
    INSERTED.Centroid,
    ...
INTO @InsertedConcepts
SELECT ... FROM @DiscoveredConcepts;
```

**Binding Phase (Cursor-Based):**
```sql
DECLARE concept_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT ConceptId, Centroid FROM @InsertedConcepts;

-- For each concept
INSERT INTO @AtomBindings (AtomId, ConceptId, Similarity, IsPrimary)
SELECT AtomId, @CurrentConceptId AS ConceptId, Similarity, 0
FROM dbo.fn_BindAtomsToCentroid(@ConceptCentroid, @SimilarityThreshold, @TenantId);
```

**Primary Concept Selection:**
```sql
WITH RankedBindings AS (
    SELECT 
        AtomId, ConceptId, Similarity,
        ROW_NUMBER() OVER (PARTITION BY AtomId ORDER BY Similarity DESC) AS Rank
    FROM @AtomBindings
)
UPDATE ab
SET IsPrimary = CASE WHEN rb.Rank = 1 THEN 1 ELSE 0 END
FROM @AtomBindings ab
INNER JOIN RankedBindings rb ON ab.AtomId = rb.AtomId AND ab.ConceptId = rb.ConceptId;
```

#### Issues Identified

**CRITICAL:**
1. ❌ **Missing Function:** `dbo.fn_BindAtomsToCentroid` not found
   - Expected signature: `fn_BindAtomsToCentroid(VARBINARY(MAX), FLOAT, INT) RETURNS TABLE (AtomId BIGINT, Similarity FLOAT)`
   - MUST query AtomEmbedding with VECTOR_DISTANCE
   - Blocking: Binding phase will fail

**HIGH:**
2. ⚠️ **Cursor Usage:** Uses cursor to iterate concepts for binding
   - MUST be set-based with CROSS APPLY
   - Performance issue for many concepts
   - required implementation:
     ```sql
     INSERT INTO @AtomBindings
     SELECT ab.AtomId, ic.ConceptId, ab.Similarity, 0
     FROM @InsertedConcepts ic
     CROSS APPLY dbo.fn_BindAtomsToCentroid(ic.Centroid, @SimilarityThreshold, @TenantId) ab;
     ```

3. ⚠️ **Unused Parameter:** `@MaxConceptsPerAtom` declared but not enforced
   - MUST limit bindings per atom
   - Currently binds all concepts above threshold
   - MUST add: `WHERE Rank <= @MaxConceptsPerAtom` to final INSERT

4. ⚠️ **Redundant Atom Count:** Initial insert sets AtomCount, then updates it
   - MUST remove from initial INSERT
   - OR skip UPDATE if counts match

**MEDIUM:**
5. ⚠️ **Schema Assumptions:** Assumes provenance.Concepts has many columns
   - ConceptName, Description, CentroidVector, Centroid, VectorDimension, MemberCount, AtomCount, CoherenceScore, Coherence, HilbertValue, DiscoveryMethod, ModelId, TenantId
   - Redundant columns: CoherenceScore/Coherence, MemberCount/AtomCount
   - VectorDimension set to 0 - MUST compute

6. ⚠️ **Hardcoded ModelId:** `ModelId = 1` in concept INSERT
   - MUST be parameter or derived from embeddings
   - Comment: "Default ModelId - adjust as needed"

7. ⚠️ **No Validation:** No checks for empty results or zero bindings
   - MUST warn if no concepts discovered
   - MUST validate @MinClusterSize < actual cluster sizes

**LOW:**
8. ⚠️ **ConceptName Generation:** `'Cluster_' + CAST(ROW_NUMBER() OVER (ORDER BY Coherence DESC) AS NVARCHAR(50))`
   - Generic names like "Cluster_1", "Cluster_2"
   - MUST generate descriptive names from top atoms

9. ⚠️ **Print Statements:** Uses PRINT for output
   - MUST return result set or use OUTPUT parameters
   - Current summary is good but mixed with PRINT

#### Strengths
- ✅ **Two-Phase Architecture:** Clear separation of discovery and binding
- ✅ **DryRun Support:** Preview mode without persistence
- ✅ **Evolution Tracking:** Logs concept creation in ConceptEvolution table
- ✅ **Primary Concept:** Identifies best match per atom
- ✅ **OUTPUT Clause:** Captures generated ConceptIds efficiently
- ✅ **Multi-Tenancy:** Proper TenantId filtering throughout
- ✅ **Transaction Management:** Proper BEGIN/COMMIT/ROLLBACK
- ✅ **Error Handling:** TRY/CATCH with transaction rollback

#### REQUIRED FIXES
1. **CRITICAL:** Implement `dbo.fn_BindAtomsToCentroid` function
2. **HIGH:** Replace cursor with set-based CROSS APPLY
3. **HIGH:** Enforce @MaxConceptsPerAtom limit
4. **MEDIUM:** Remove redundant column assignments
5. **MEDIUM:** Add ModelId parameter
6. **MEDIUM:** Add validation for empty results
7. **LOW:** Generate descriptive concept names
8. **LOW:** Return structured result set instead of PRINT

#### Cross-References
- **Calls:** fn_DiscoverConcepts (Part 10), fn_BindAtomsToCentroid (NOT FOUND)
- **Uses:** provenance.Concepts, provenance.AtomConcepts, provenance.ConceptEvolution
- **Related:** Unsupervised learning, semantic clustering, knowledge graph construction
- **Pattern:** Similar to two-phase ETL with discovery + enrichment

---

## Batch 1 Summary

**Files Analyzed:** 5 procedures  
**Average Quality Score:** 76/100  
**Critical Issues:** 5 missing objects (3 tables, 2 CLR functions, 1 TVF)  
**Total Lines:** ~350 lines of analyzed code

### Missing Objects Catalog

**Tables (3):**
1. `dbo.ReasoningChains` - Chain-of-thought storage
2. `dbo.MultiPathReasoning` - Multi-path exploration storage
3. `dbo.SelfConsistencyResults` - Consensus reasoning storage

**CLR Functions (2):**
1. `dbo.ChainOfThoughtCoherence` - CLR aggregate for coherence analysis
2. `dbo.SelfConsistency` - CLR aggregate for consensus detection

**TVF Functions (1):**
1. `dbo.fn_BindAtomsToCentroid` - Atom-to-concept similarity binding

### Quality Distribution
- **Excellent (80-100):** 1 file (sp_DetectDuplicates: 82)
- **Good (70-79):** 4 files (average: 75)
- **Fair (60-69):** 0 files
- **Poor (<60):** 0 files

### Architectural Patterns Observed

**Reasoning Procedures:**
- Common pattern: Generate samples → Compute embeddings → Analyze with CLR aggregate → Store JSON results
- Heavy use of CLR functions for complex vector analysis
- WHILE loops still present despite comments about set-based operations
- Good separation of reasoning logic from storage

**Data Quality:**
- sp_DetectDuplicates shows strong vector similarity operations
- Proper multi-tenancy support
- Self-join pattern for duplicate detection

**Unsupervised Learning:**
- sp_DiscoverAndBindConcepts shows sophisticated two-phase pipeline
- Good use of OUTPUT clause for capturing generated IDs
- Evolution tracking for concept lifecycle

### Cross-Cutting Concerns

**Multi-Tenancy:** 4/5 procedures support TenantId (80%)  
**Error Handling:** 5/5 procedures have TRY/CATCH (100%)  
**Vector Operations:** 4/5 procedures use VECTOR(1998) type (80%)  
**JSON Serialization:** 3/5 procedures use JSON for complex data (60%)  
**Duration Tracking:** 3/5 procedures track execution time (60%)

---

---

## Batch 2: Search and Provenance Procedures

**Files Analyzed:** 5 procedures  
**Batch 2 Average Quality Score:** 80/100

### 6. sp_SemanticSearch
**Path:** `Procedures/dbo.sp_SemanticSearch.sql`  
**Type:** Stored Procedure (Core Search Infrastructure)  
**Quality Score:** 85/100

#### Purpose
Primary semantic search interface supporting both vector-only and hybrid spatial+vector search modes with multi-tenancy.

#### Schema Analysis

**Parameters:**
```sql
@query_text NVARCHAR(MAX) = NULL          -- Text query (auto-embedded)
@query_embedding VECTOR(1998) = NULL      -- Pre-computed embedding
@query_dimension INT = 768                -- Embedding dimension
@top_k INT = 5                            -- Result limit
@category NVARCHAR(50) = NULL             -- Optional category filter
@use_hybrid BIT = 1                       -- Enable spatial filtering
@TenantId INT                             -- Multi-tenancy ✅ REQUIRED
@EmbeddingType NVARCHAR(50) = NULL        -- Embedding type filter
```

**Dependencies:**
- `dbo.sp_TextToEmbedding` - Text-to-vector conversion (Part 8)
- `dbo.sp_ComputeSpatialProjection` - 3D spatial projection (not yet analyzed)
- `dbo.sp_HybridSearch` - Two-phase search (this batch)
- `dbo.InferenceRequest` - Request tracking (Part 8)
- `dbo.AtomEmbedding` - Embedding storage (Part 1)
- `dbo.Atom` - Atom metadata (Part 1)

**Key Operations:**
1. **Auto-embedding:** Converts text to vector
2. **Spatial projection:** Maps vector to 3D space for hybrid mode
3. **Hybrid search:** Two-phase spatial filter + vector rerank
4. **Vector-only search:** Direct VECTOR_DISTANCE on all embeddings
5. **Inference tracking:** Logs search as inference request
6. **Result aggregation:** Combines scores and metadata

#### Technical Details

**Auto-Embedding Logic:**
```sql
IF @query_embedding IS NULL
BEGIN
    IF @query_text IS NULL
        RAISERROR('Either @query_embedding or @query_text must be provided.', 16, 1);
    
    EXEC dbo.sp_TextToEmbedding 
        @text = @query_text,
        @ModelName = NULL,  -- ✅ Auto-select best model
        @embedding = @query_embedding OUTPUT,
        @dimension = @embedding_dimension OUTPUT;
    
    IF @query_embedding IS NULL
        RAISERROR('Failed to generate embedding from query text. Ensure models are ingested.', 16, 1);
END;
```

**Hybrid Search Path:**
```sql
IF @use_hybrid = 1
BEGIN
    -- Project to 3D spatial coordinates
    EXEC dbo.sp_ComputeSpatialProjection
        @input_vector = @query_embedding,
        @input_dimension = @query_dimension,
        @output_x = @spatial_x OUTPUT,
        @output_y = @spatial_y OUTPUT,
        @output_z = @spatial_z OUTPUT;
    
    -- Two-phase search
    INSERT INTO @Hybrid
    EXEC dbo.sp_HybridSearch
        @query_vector = @query_embedding,
        @query_dimension = @query_dimension,
        @query_spatial_x = @spatial_x,
        @query_spatial_y = @spatial_y,
        @query_spatial_z = @spatial_z,
        @spatial_candidates = @spatial_candidates,  -- 10x top_k
        @final_top_k = @top_k,
        @embedding_type = @EmbeddingType,
        @TenantId = @TenantId;
END
```

**Vector-Only Search:**
```sql
SELECT TOP (@top_k)
    ae.AtomEmbeddingId,
    ae.AtomId,
    VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS distance,
    1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @query_embedding) AS similarity
FROM dbo.AtomEmbedding AS ae
INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
WHERE 
    (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
    AND (@EmbeddingType IS NULL OR ae.EmbeddingType = @EmbeddingType)
    AND ae.EmbeddingVector IS NOT NULL
    AND ae.Dimension = @query_dimension
    AND (@category IS NULL OR a.Subtype = @category)
ORDER BY distance ASC;
```

**Inference Tracking:**
```sql
INSERT INTO dbo.InferenceRequest (
    TaskType,
    InputData,
    InputHash,
    ModelsUsed,
    EnsembleStrategy
)
VALUES (
    'semantic_search',
    @input_data,                           -- JSON with queryText
    @input_hash,                           -- SHA2_256 hash
    @models_used_json,                     -- Search method metadata
    'spatial_filter_vector_rerank'
);
```

#### Issues Identified

**HIGH:**
1. ⚠️ **Dimension Mismatch Handling:** `@query_dimension INT = 768` default, but vectors are VECTOR(1998)
   - Filter: `AND ae.Dimension = @query_dimension`
   - If caller doesn't override, will miss all VECTOR(1998) embeddings
   - MUST auto-detect from model or embedding table

2. ⚠️ **Duplicate VECTOR_DISTANCE:** Vector-only path computes distance twice
   - Once in SELECT: `1.0 - VECTOR_DISTANCE(...)`
   - Once in WHERE: `(1.0 - VECTOR_DISTANCE(...)) >= @SimilarityThreshold` (wait, no WHERE threshold in vector-only)
   - Actually computed in SELECT and ORDER BY - MUST use CTE or derived table

3. ⚠️ **Spatial Candidate Multiplier:** `@spatial_candidates = CASE WHEN @top_k IS NULL OR @top_k <= 0 THEN 10 ELSE @top_k * 10 END;`
   - Hardcoded 10x multiplier
   - MUST be configurable parameter
   - will be too aggressive or too conservative depending on data distribution

**MEDIUM:**
4. ⚠️ **Multi-Tenancy Pattern:** Uses OR with EXISTS subquery
   - `(a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta ...))`
   - Performance: OR prevents index usage
   - MUST use UNION for better query plans

5. ⚠️ **@category Parameter:** Filters on `a.Subtype = @category`
   - Name mismatch: "category" vs "Subtype"
   - MUST be @Subtype or rename column

6. ⚠️ **No Timeout Handling:** Long-running searches MUST block
   - MUST add query timeout hints
   - Or implement async search with job queue

7. ⚠️ **InputHash Logic:** Only hashes if @query_text provided
   - Vector queries have NULL InputHash
   - MUST hash vector bytes for cache key

**LOW:**
8. ⚠️ **JSON Error Handling:** `CAST(JSON_OBJECT(...) AS NVARCHAR(MAX))` without TRY_CAST
   - SQL Server 2022+ syntax
   - MUST validate or use TRY_CAST

9. ⚠️ **Variable Naming:** Inconsistent snake_case vs camelCase
   - Parameters use snake_case (@query_text)
   - Variables use camelCase (@EmbeddingType)

#### Strengths
- ✅ **Flexible Input:** Accepts text or vector
- ✅ **Auto-Embedding:** Seamless text-to-vector conversion
- ✅ **Hybrid Search:** Optimized spatial filtering for large datasets
- ✅ **Multi-Tenancy:** Proper tenant isolation (with minor optimization needed)
- ✅ **Inference Tracking:** Full audit trail
- ✅ **Rich Metadata:** Returns distance, similarity, search method
- ✅ **Category Filtering:** Optional result filtering
- ✅ **Error Messages:** Clear error for missing inputs

#### REQUIRED FIXES
1. **HIGH:** Auto-detect dimension from embeddings or model metadata
2. **HIGH:** Optimize multi-tenancy query with UNION instead of OR
3. **MEDIUM:** Make spatial candidate multiplier configurable
4. **MEDIUM:** Rename @category to @Subtype for clarity
5. **MEDIUM:** Hash vector bytes for InputHash when @query_text is NULL
6. **LOW:** Add query timeout configuration
7. **LOW:** Standardize variable naming convention

#### Cross-References
- **Calls:** sp_TextToEmbedding (Part 8), sp_ComputeSpatialProjection (not analyzed), sp_HybridSearch (this batch)
- **Uses:** InferenceRequest (Part 8), AtomEmbedding (Part 1), Atom (Part 1), TenantAtoms (not analyzed)
- **Related:** Core search infrastructure, used by UI and API
- **Pattern:** PRIMARY entry point for all semantic search operations

---

### 7. sp_HybridSearch
**Path:** `Procedures/dbo.sp_HybridSearch.sql`  
**Type:** Stored Procedure (Search Optimization)  
**Quality Score:** 88/100

#### Purpose
Two-phase hybrid search: spatial index filtering (O(log n)) followed by exact vector reranking (O(k)).

#### Schema Analysis

**Parameters:**
```sql
@query_vector VECTOR(1998)                -- Query embedding
@query_dimension INT                      -- Embedding dimension
@query_spatial_x FLOAT                    -- 3D spatial coordinates
@query_spatial_y FLOAT
@query_spatial_z FLOAT
@spatial_candidates INT = 100             -- Phase 1 candidate count
@final_top_k INT = 10                     -- Phase 2 result count
@distance_metric NVARCHAR(20) = 'cosine'  -- Vector distance metric
@embedding_type NVARCHAR(128) = NULL      -- Embedding type filter
@ModelId INT = NULL                       -- Model filter
@srid INT = 0                             -- Spatial reference ID
@TenantId INT                             -- Multi-tenancy ✅ REQUIRED
```

**Dependencies:**
- `dbo.AtomEmbedding` - Embedding storage with SpatialKey
- `dbo.Atom` - Atom metadata
- `dbo.TenantAtoms` - Tenant access control

**Key Operations:**
1. **Phase 1 (Spatial):** Use spatial index on SpatialKey (O(log n))
2. **Phase 2 (Vector):** Exact VECTOR_DISTANCE on filtered candidates (O(k))

#### Technical Details

**Phase 1 - Spatial Filtering:**
```sql
DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_spatial_x, ' ', @query_spatial_y, ' ', @query_spatial_z, ')');
DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

INSERT INTO @candidates (AtomEmbeddingId, SpatialDistance)
SELECT TOP (@spatial_candidates)
    ae.AtomEmbeddingId,
    ae.SpatialKey.STDistance(@query_point) AS spatial_distance
FROM dbo.AtomEmbedding AS ae
INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
WHERE 
    (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
    AND ae.SpatialKey IS NOT NULL
    AND ae.Dimension = @query_dimension
    AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
    AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
ORDER BY ae.SpatialKey.STDistance(@query_point);  -- ✅ Uses spatial index
```

**Phase 2 - Vector Reranking:**
```sql
SELECT TOP (@final_top_k)
    ae.AtomEmbeddingId,
    ae.AtomId,
    a.Modality,
    a.Subtype,
    ae.EmbeddingType,
    ae.ModelId,
    VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS exact_distance,
    c.SpatialDistance AS spatial_distance
FROM dbo.AtomEmbedding AS ae
INNER JOIN @candidates AS c ON c.AtomEmbeddingId = ae.AtomEmbeddingId
INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
WHERE 
    (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
    AND ae.EmbeddingVector IS NOT NULL
    AND ae.Dimension = @query_dimension
ORDER BY VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector);
```

#### Issues Identified

**MEDIUM:**
1. ⚠️ **Redundant Tenant Filter:** Phase 2 re-checks tenancy
   - Phase 1 already filters by tenant
   - @candidates table only contains authorized embeddings
   - Redundant: `(a.TenantId = @TenantId OR EXISTS (...))`
   - Comment acknowledges: "redundant check, but safe"

2. ⚠️ **Multi-Tenancy Performance:** Same OR + EXISTS pattern as sp_SemanticSearch
   - MUST use UNION for better query plans
   - Affects both Phase 1 and Phase 2

3. ⚠️ **Duplicate VECTOR_DISTANCE:** Computed in SELECT and ORDER BY
   - MUST use CTE or CROSS APPLY to compute once

**LOW:**
4. ⚠️ **PRINT Statements:** Debug output with PRINT
   - MUST use @Debug parameter or remove
   - Not user-facing output

5. ⚠️ **No Validation:** Doesn't validate @spatial_candidates > @final_top_k
   - MUST enforce @spatial_candidates >= @final_top_k
   - Otherwise Phase 2 will return fewer results than requested

6. ⚠️ **SRID Hardcoded:** Default @srid = 0
   - MUST match AtomEmbedding.SpatialKey SRID
   - Mismatch will cause spatial errors

#### Strengths
- ✅ **EXCELLENT Performance:** Two-phase architecture scales to billions of vectors
- ✅ **Spatial Index:** Uses STDistance with spatial index (O(log n))
- ✅ **Minimal Vector Ops:** Only computes exact distance on top-k candidates
- ✅ **Configurable Metrics:** Supports cosine, euclidean, dotproduct
- ✅ **Rich Metadata:** Returns both spatial and vector distances
- ✅ **Multi-Tenancy:** Proper tenant filtering (with optimization opportunity)
- ✅ **Flexible Filtering:** Optional embedding type and model filters
- ✅ **Table Variable Pattern:** Efficient candidate storage

#### REQUIRED FIXES
1. **MEDIUM:** Remove redundant tenant check in Phase 2
2. **MEDIUM:** Optimize multi-tenancy with UNION pattern
3. **MEDIUM:** Add validation: @spatial_candidates >= @final_top_k
4. **LOW:** Remove PRINT or add @Debug parameter
5. **LOW:** Validate SRID matches embedding spatial configuration
6. **LOW:** Use CTE to avoid duplicate VECTOR_DISTANCE computation

#### Cross-References
- **Called by:** sp_SemanticSearch (this batch)
- **Uses:** AtomEmbedding (Part 1), Atom (Part 1), TenantAtoms
- **Pattern:** GOLD STANDARD for large-scale vector search optimization
- **Related:** Inspired by HNSW/IVF-PQ two-phase architectures

---

### 8. sp_FusionSearch
**Path:** `Procedures/dbo.sp_FusionSearch.sql`  
**Type:** Stored Procedure (Multi-Modal Search)  
**Quality Score:** 76/100

#### Purpose
Combines three search modalities (vector, keyword, spatial) with weighted scoring for unified multi-modal search.

#### Schema Analysis

**Parameters:**
```sql
@QueryVector VECTOR(1998)                 -- Vector query
@Keywords NVARCHAR(MAX) = NULL            -- Full-text keywords
@SpatialRegion GEOMETRY = NULL            -- Spatial region filter
@TopK INT = 10                            -- Result limit
@VectorWeight FLOAT = 0.5                 -- Vector score weight
@KeywordWeight FLOAT = 0.3                -- Keyword score weight
@SpatialWeight FLOAT = 0.2                -- Spatial score weight
@TenantId INT = NULL                      -- Optional tenant filter
```

**Dependencies:**
- `dbo.AtomEmbedding` - Vector search
- `dbo.Atom` - Content and full-text search
- `dbo.TenantAtoms` - Tenant access
- `CONTAINSTABLE` - Full-text search (requires FT index on Atom.Content)

**Key Operations:**
1. **Validate weights:** Ensure sum = 1.0
2. **Vector scoring:** Cosine similarity on embeddings
3. **Keyword scoring:** Full-text search with CONTAINSTABLE
4. **Spatial scoring:** Binary within/outside region
5. **Weighted fusion:** Combine scores with configurable weights

#### Technical Details

**Weight Validation:**
```sql
IF ABS((@VectorWeight + @KeywordWeight + @SpatialWeight) - 1.0) > 0.01
BEGIN
    RAISERROR('Weights must sum to 1.0', 16, 1);
    RETURN -1;
END
```

**Vector Scoring:**
```sql
INSERT INTO @Results (AtomId, VectorScore, KeywordScore, SpatialScore)
SELECT 
    ae.AtomId,
    1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @QueryVector) AS VectorScore,
    0.0 AS KeywordScore,
    0.0 AS SpatialScore
FROM dbo.AtomEmbedding ae
LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId);
```

**Keyword Scoring (Dynamic SQL):**
```sql
IF @Keywords IS NOT NULL AND LEN(@Keywords) > 0
BEGIN
    CREATE TABLE #FTSResults (AtomId BIGINT, FTSRank INT);
    
    DECLARE @SQL NVARCHAR(MAX) = N'
        INSERT INTO #FTSResults (AtomId, FTSRank)
        SELECT [KEY], RANK 
        FROM CONTAINSTABLE(dbo.Atom, Content, @SearchTerm)';
    
    EXEC sp_executesql @SQL, N'@SearchTerm NVARCHAR(4000)', @Keywords;
    
    UPDATE r
    SET KeywordScore = ISNULL(fts.FTSRank / 1000.0, 0.0)  -- Normalize to 0-1
    FROM @Results r
    INNER JOIN #FTSResults fts ON r.AtomId = fts.AtomId;
    
    DROP TABLE #FTSResults;
END
```

**Spatial Scoring:**
```sql
IF @SpatialRegion IS NOT NULL
BEGIN
    UPDATE r
    SET SpatialScore = CASE 
        WHEN ae.SpatialKey IS NOT NULL AND ae.SpatialKey.STWithin(@SpatialRegion) = 1 
        THEN 1.0
        ELSE 0.0
    END
    FROM @Results r
    INNER JOIN dbo.AtomEmbedding ae ON r.AtomId = ae.AtomId;
END
```

**Fusion Scoring:**
```sql
UPDATE @Results
SET CombinedScore = 
    (VectorScore * @VectorWeight) + 
    (KeywordScore * @KeywordWeight) + 
    (SpatialScore * @SpatialWeight);

SELECT TOP (@TopK) ... ORDER BY r.CombinedScore DESC;
```

#### Issues Identified

**HIGH:**
1. ⚠️ **Missing Full-Text Index:** CONTAINSTABLE requires FT index on Atom.Content
   - Will fail if index doesn't exist
   - MUST check: `SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.Atom')`
   - Or use LIKE fallback

2. ⚠️ **Dynamic SQL Limitations:** `@Keywords` truncated to NVARCHAR(4000)
   - Parameter: `N'@SearchTerm NVARCHAR(4000)'`
   - Input: `@Keywords NVARCHAR(MAX)`
   - Will truncate long queries

3. ⚠️ **FTS Rank Normalization:** `fts.FTSRank / 1000.0`
   - Arbitrary scaling factor
   - FTS rank scale varies by content and index
   - MUST normalize to 0-1 based on max rank in result set

**MEDIUM:**
4. ⚠️ **Spatial Scoring:** Binary 1.0/0.0 instead of distance-based
   - Within region = 1.0, outside = 0.0
   - No partial scoring for proximity
   - MUST use: `1.0 / (1.0 + STDistance(@region.STCentroid()))`

5. ⚠️ **No Zero-Weight Optimization:** Computes all modalities even if weight = 0
   - If @KeywordWeight = 0, skips CONTAINSTABLE (good)
   - But always computes vector scores even if @VectorWeight = 0
   - MUST skip modalities with zero weight

6. ⚠️ **Left Join Performance:** `LEFT JOIN dbo.TenantAtoms ta`
   - If @TenantId IS NULL, LEFT JOIN is unnecessary
   - MUST use conditional logic or INNER JOIN

7. ⚠️ **Temp Table Cleanup:** `DROP TABLE #FTSResults;` in IF block
   - Good cleanup
   - But if error occurs before DROP, table persists
   - MUST use TRY/FINALLY pattern

**LOW:**
8. ⚠️ **No Model Filtering:** Vector scoring doesn't filter by ModelId
   - Multiple embeddings per atom = multiple scores
   - MUST filter or aggregate

9. ⚠️ **No Dimension Filtering:** Doesn't filter `ae.Dimension`
   - will include embeddings with different dimensions
   - Vector distance will fail if dimensions mismatch

#### Strengths
- ✅ **Multi-Modal Fusion:** Combines three complementary search types
- ✅ **Configurable Weights:** Flexible score combination
- ✅ **Weight Validation:** Ensures proper normalization
- ✅ **Conditional Execution:** Skips keyword/spatial if not provided
- ✅ **Rich Metadata:** Returns individual and combined scores
- ✅ **Error Handling:** TRY/CATCH with proper RAISERROR

#### REQUIRED FIXES
1. **HIGH:** Check for FT index or implement LIKE fallback
2. **HIGH:** Fix keyword truncation - use NVARCHAR(MAX) in dynamic SQL
3. **HIGH:** Normalize FTS ranks dynamically based on max rank
4. **MEDIUM:** Implement distance-based spatial scoring
5. **MEDIUM:** Skip zero-weight modalities for performance
6. **MEDIUM:** Optimize tenant filtering (INNER JOIN when @TenantId provided)
7. **MEDIUM:** Add dimension and model filtering to vector scoring
8. **LOW:** Ensure temp table cleanup in error cases

#### Cross-References
- **Pattern:** Combines sp_SemanticSearch (vector), CONTAINSTABLE (keyword), spatial filtering
- **Uses:** AtomEmbedding (Part 1), Atom (Part 1), TenantAtoms
- **Related:** Multi-modal search, cross-modal retrieval
- **Use Case:** "Find images of cats near San Francisco matching 'playful'"

---

### 9. sp_MultiModelEnsemble
**Path:** `Procedures/dbo.sp_MultiModelEnsemble.sql`  
**Type:** Stored Procedure (Ensemble Search)  
**Quality Score:** 80/100

#### Purpose
Searches using three different embedding models and combines results with weighted ensemble scoring.

#### Schema Analysis

**Parameters:**
```sql
@QueryVector1 VECTOR(1998)                -- Model 1 query embedding
@QueryVector2 VECTOR(1998)                -- Model 2 query embedding
@QueryVector3 VECTOR(1998)                -- Model 3 query embedding
@Model1Id INT                             -- Model 1 identifier
@Model2Id INT                             -- Model 2 identifier
@Model3Id INT                             -- Model 3 identifier
@Model1Weight FLOAT = 0.4                 -- Model 1 score weight
@Model2Weight FLOAT = 0.35                -- Model 2 score weight
@Model3Weight FLOAT = 0.25                -- Model 3 score weight
@TopK INT = 10                            -- Result limit
@TenantId INT = NULL                      -- Optional tenant filter
```

**Dependencies:**
- `dbo.AtomEmbedding` - Model-specific embeddings
- `dbo.Atom` - Atom metadata
- `dbo.TenantAtoms` - Tenant access

**Key Operations:**
1. Collect all atoms with embeddings from any model
2. Score each atom with each model (NULL if embedding missing)
3. Compute weighted ensemble score
4. Return top-k by ensemble score

#### Technical Details

**Atom Collection:**
```sql
DECLARE @AllAtoms TABLE (AtomId BIGINT PRIMARY KEY);

INSERT INTO @AllAtoms
SELECT DISTINCT ae.AtomId
FROM dbo.AtomEmbedding ae
LEFT JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
      AND ae.ModelId IN (@Model1Id, @Model2Id, @Model3Id);
```

**Per-Model Scoring:**
```sql
INSERT INTO @EnsembleResults (AtomId, Model1Score, Model2Score, Model3Score)
SELECT 
    aa.AtomId,
    ISNULL(
        (SELECT 1.0 - VECTOR_DISTANCE('cosine', ae1.EmbeddingVector, @QueryVector1)
         FROM dbo.AtomEmbedding ae1
         WHERE ae1.AtomId = aa.AtomId AND ae1.ModelId = @Model1Id),
        0.0
    ) AS Model1Score,
    ISNULL(...) AS Model2Score,
    ISNULL(...) AS Model3Score
FROM @AllAtoms aa;
```

**Ensemble Scoring:**
```sql
UPDATE @EnsembleResults
SET EnsembleScore = 
    (Model1Score * @Model1Weight) + 
    (Model2Score * @Model2Weight) + 
    (Model3Score * @Model3Weight);

SELECT TOP (@TopK) ... ORDER BY er.EnsembleScore DESC;
```

#### Issues Identified

**HIGH:**
1. ⚠️ **Hardcoded 3 Models:** Requires exactly 3 models
   - Not flexible for 2-model or 4+ model ensembles
   - MUST accept variable number of models via TVP or JSON

2. ⚠️ **No Weight Validation:** Doesn't check if weights sum to 1.0
   - MUST lead to unexpected score ranges
   - MUST validate like sp_FusionSearch does

3. ⚠️ **Three Subqueries:** Per-model scoring uses correlated subqueries
   - Each subquery scans AtomEmbedding independently
   - Performance: O(N * 3) for N atoms
   - MUST use LEFT JOINs or CTEs

**MEDIUM:**
4. ⚠️ **Zero Score for Missing Embeddings:** `ISNULL(..., 0.0)`
   - Atom with 1/3 embeddings penalized
   - MUST normalize: `EnsembleScore / (COUNT(non-null scores))`
   - Or filter to atoms with all embeddings

5. ⚠️ **No Dimension Filtering:** Doesn't filter `ae.Dimension`
   - Assumes all models use same dimension
   - MUST fail if models have different dimensions

6. ⚠️ **Left Join for Tenancy:** Same performance issue as other procedures
   - MUST optimize tenant filtering

**LOW:**
7. ⚠️ **No Metadata:** Doesn't return model info in results
   - Useful to know which models contributed
   - MUST return Model1Score, Model2Score, Model3Score

8. ⚠️ **Fixed Parameter Names:** @QueryVector1, @QueryVector2, @QueryVector3
   - Not extensible
   - MUST use TVP or JSON array

#### Strengths
- ✅ **Ensemble Learning:** Leverages multiple model perspectives
- ✅ **Weighted Combination:** Configurable model importance
- ✅ **Handles Missing Embeddings:** ISNULL provides default score
- ✅ **Multi-Tenancy Support:** Optional tenant filtering
- ✅ **Error Handling:** TRY/CATCH block
- ✅ **Rich Output:** Includes per-model and ensemble scores

#### REQUIRED FIXES
1. **HIGH:** Refactor to accept variable number of models (TVP or JSON)
2. **HIGH:** Optimize per-model scoring with JOINs instead of subqueries
3. **HIGH:** Validate weights sum to 1.0
4. **MEDIUM:** Normalize ensemble score by number of contributing models
5. **MEDIUM:** Add dimension filtering
6. **MEDIUM:** Optimize tenant filtering
7. **LOW:** Filter to atoms with all model embeddings (optional mode)
8. **LOW:** Return model contribution metadata

#### Cross-References
- **Pattern:** Ensemble learning, similar to sp_FusionSearch but model-based
- **Uses:** AtomEmbedding (Part 1), Atom (Part 1), TenantAtoms
- **Related:** Model comparison, robust search across architectures
- **Use Case:** Combine BERT + GPT + CLIP embeddings for better retrieval

---

### 10. sp_ValidateOperationProvenance
**Path:** `Procedures/dbo.sp_ValidateOperationProvenance.sql`  
**Type:** Stored Procedure (Provenance Validation)  
**Quality Score:** 82/100

#### Purpose
Validates operation provenance streams by checking stream integrity, metadata, segment ordering, and temporal consistency.

#### Schema Analysis

**Parameters:**
```sql
@OperationId UNIQUEIDENTIFIER              -- Operation to validate
@ExpectedScope NVARCHAR(100) = NULL        -- Expected scope value
@ExpectedModel NVARCHAR(100) = NULL        -- Expected model name
@MinSegments INT = 1                       -- Minimum segment count
@MaxAgeHours INT = 24                      -- Maximum stream age
@Debug BIT = 0                             -- Debug output
```

**Dependencies:**
- `dbo.OperationProvenance` - Stream storage (Part 11)
- `dbo.AtomicStream` UDT - CLR type (Part 11)
- `dbo.ProvenanceValidationResults` - Result storage (Part 11)

**Key Operations:**
1. Retrieve provenance stream from OperationProvenance
2. Validate stream existence and metadata
3. Check segment count and ordering
4. Validate timestamps
5. Store validation results with JSON
6. Return validation report

#### Technical Details

**Stream Retrieval:**
```sql
DECLARE @ProvenanceStream dbo.AtomicStream;
SELECT @ProvenanceStream = ProvenanceStream
FROM dbo.OperationProvenance
WHERE OperationId = @OperationId;

IF @ProvenanceStream IS NULL OR @ProvenanceStream.IsNull = 1
BEGIN
    INSERT INTO @ValidationResult VALUES ('Stream Existence', 'FAIL', 'No provenance stream found for operation');
    GOTO ValidationComplete;
END
```

**Metadata Validation:**
```sql
DECLARE @Scope NVARCHAR(128) = @ProvenanceStream.Scope;
DECLARE @Model NVARCHAR(128) = @ProvenanceStream.Model;

IF @ExpectedScope IS NOT NULL
BEGIN
    IF @Scope = @ExpectedScope
        INSERT INTO @ValidationResult VALUES ('Scope Validation', 'PASS', ...);
    ELSE
        INSERT INTO @ValidationResult VALUES ('Scope Validation', 'FAIL', ...);
END
```

**Segment Validation Loop:**
```sql
DECLARE @SegmentIndex INT = 0;
WHILE @SegmentIndex < @SegmentCount
BEGIN
    DECLARE @SegmentKind NVARCHAR(50) = @ProvenanceStream.GetSegmentKind(@SegmentIndex);
    DECLARE @SegmentTimestamp DATETIME2 = @ProvenanceStream.GetSegmentTimestamp(@SegmentIndex);
    
    -- Check segment types
    IF @SegmentKind = 'Input' AND @SegmentIndex = 0
        INSERT INTO @ValidationResult VALUES ('Input Segment', 'PASS', ...);
    ELSE IF @SegmentKind NOT IN ('Input', 'Output', 'Embedding', 'Moderation', 'Artifact', 'Telemetry', 'Control')
        INSERT INTO @ValidationResult VALUES ('Segment Kind', 'WARN', 'Unknown segment kind: ' + @SegmentKind);
    
    -- Check timestamp ordering
    IF @SegmentIndex > 0
    BEGIN
        DECLARE @PrevTimestamp DATETIME2 = @ProvenanceStream.GetSegmentTimestamp(@SegmentIndex - 1);
        IF @SegmentTimestamp >= @PrevTimestamp
            INSERT INTO @ValidationResult VALUES ('Timestamp Ordering', 'PASS', ...);
        ELSE
            INSERT INTO @ValidationResult VALUES ('Timestamp Ordering', 'FAIL', ...);
    END
    
    SET @SegmentIndex = @SegmentIndex + 1;
END
```

**Result Storage:**
```sql
INSERT INTO dbo.ProvenanceValidationResults (
    OperationId,
    ValidationResults,                     -- JSON from table variable
    OverallStatus,                         -- 'PASS', 'WARN', 'FAIL'
    ValidationDurationMs,
    ValidatedAt
)
VALUES (
    @OperationId,
    (SELECT * FROM @ValidationResult FOR JSON PATH),
    CASE WHEN EXISTS (SELECT 1 FROM @ValidationResult WHERE Status = 'FAIL') THEN 'FAIL'
         WHEN EXISTS (SELECT 1 FROM @ValidationResult WHERE Status = 'WARN') THEN 'WARN'
         ELSE 'PASS' END,
    DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()),
    SYSUTCDATETIME()
);
```

#### Issues Identified

**MEDIUM:**
1. ⚠️ **WHILE Loop:** Iterates segments sequentially
   - MUST use CLR function to enumerate all segments at once
   - Wait, Part 11 shows: `provenance.clr_EnumerateAtomicStreamSegments` exists
   - MUST use that TVF instead of WHILE loop

2. ⚠️ **Hardcoded Segment Types:** `IF @SegmentKind NOT IN ('Input', 'Output', ...)`
   - Segment types MUST be in reference table or CLR constant
   - New types require code change

3. ⚠️ **No TenantId:** Missing multi-tenancy
   - MUST validate caller has access to operation
   - Security risk: any user can validate any operation

4. ⚠️ **Validation Logic in Procedure:** Complex validation rules
   - MUST extract to separate validation framework
   - Hard to unit test

**LOW:**
5. ⚠️ **Stream Age:** `DATEDIFF(MINUTE, ...) / 60.0` 
   - MUST use DATEDIFF(HOUR, ...) directly
   - Minor precision difference

6. ⚠️ **Status Ordering:** `CASE Status WHEN 'FAIL' THEN 1 ...`
   - Sorts results by status priority
   - Good for user experience

7. ⚠️ **Debug PRINT:** Uses PRINT for debug output
   - MUST return debug info in result set or use @Debug = 2 for verbose mode

#### Strengths
- ✅ **Comprehensive Validation:** Checks existence, metadata, segments, timestamps, age
- ✅ **Three-Tier Status:** FAIL/WARN/PASS for nuanced results
- ✅ **JSON Storage:** Detailed validation results as JSON
- ✅ **Duration Tracking:** Logs validation performance
- ✅ **Structured Output:** Returns validation report with details
- ✅ **Optional Validation:** @ExpectedScope and @ExpectedModel are optional
- ✅ **Early Exit:** GOTO for fast failure on missing stream
- ✅ **Audit Trail:** Stores every validation run

#### REQUIRED FIXES
1. **HIGH:** Replace WHILE loop with clr_EnumerateAtomicStreamSegments TVF
2. **MEDIUM:** Add TenantId and authorization check
3. **MEDIUM:** Extract validation rules to configuration table
4. **MEDIUM:** Use reference table for valid segment types
5. **LOW:** Use DATEDIFF(HOUR) for age calculation
6. **LOW:** Return debug info in result set instead of PRINT
7. **LOW:** Add validation rule extensibility framework

#### Cross-References
- **Uses:** OperationProvenance (Part 11), AtomicStream UDT (Part 11), ProvenanceValidationResults (Part 11)
- **Calls:** AtomicStream CLR methods (GetSegmentKind, GetSegmentTimestamp)
- **Related:** Provenance audit, compliance, debugging
- **Pattern:** MUST be generalized for other stream validation scenarios

---

## Batch 2 Summary

**Files Analyzed:** 5 procedures  
**Average Quality Score:** 82/100  
**Critical Issues:** 1 missing full-text index  
**High Priority Issues:** 11 performance/correctness issues  
**Total Lines:** ~400 lines of analyzed code

### Quality Distribution
- **Excellent (85-100):** 2 files (sp_SemanticSearch: 85, sp_HybridSearch: 88)
- **Good (70-84):** 3 files (sp_FusionSearch: 76, sp_MultiModelEnsemble: 80, sp_ValidateOperationProvenance: 82)

### Search Architecture Patterns

**Two-Phase Optimization (sp_HybridSearch):**
- Phase 1: Spatial index filter (O(log n)) → candidate set
- Phase 2: Exact vector distance (O(k)) → ranked results
- **GOLD STANDARD** for billion-scale vector search

**Multi-Modal Fusion (sp_FusionSearch):**
- Combines: Vector similarity + Full-text search + Spatial filtering
- Weighted ensemble with configurable importance
- Handles heterogeneous data types

**Ensemble Search (sp_MultiModelEnsemble):**
- Leverages multiple embedding models
- Weighted combination of model perspectives
- Robust to individual model weaknesses

**Entry Point (sp_SemanticSearch):**
- Auto-embedding for text queries
- Delegates to sp_HybridSearch for optimization
- Tracks inference requests for analytics

**Validation (sp_ValidateOperationProvenance):**
- Comprehensive provenance stream checking
- Three-tier status (PASS/WARN/FAIL)
- Audit trail for compliance

### Performance Insights

**Spatial Index Advantage:**
- sp_HybridSearch: O(log n) spatial filter vs O(n) brute force
- 100x+ speedup for large datasets
- Enables billion-scale vector search

**Multi-Tenancy Optimization Needed:**
- All procedures use `OR + EXISTS` pattern
- MUST use `UNION` for better query plans
- Affects index usage

**VECTOR_DISTANCE Duplication:**
- Computed in SELECT, WHERE, ORDER BY
- MUST use CTEs or derived tables
- Minor but consistent pattern

### Cross-Cutting Observations

**Multi-Tenancy:** 5/5 procedures support TenantId (100%)  
**Error Handling:** 4/5 procedures have TRY/CATCH (80%)  
**Vector Operations:** 4/5 procedures use VECTOR(1998) (80%)  
**Spatial Operations:** 3/5 procedures use GEOMETRY (60%)  
**Full-Text Search:** 1/5 procedures use CONTAINSTABLE (20%)

---

---

## Batch 3: Job Management, Domain Building, and Views

**Files Analyzed:** 5 files (4 procedures + 1 view)  
**Batch 3 Average Quality Score:** 77/100

### 11. sp_SubmitInferenceJob
**Path:** `Procedures/dbo.sp_SubmitInferenceJob.sql`  
**Type:** Stored Procedure (Job Queue Management)  
**Quality Score:** 82/100

#### Purpose
Submits inference jobs to Service Broker queue with autonomous complexity calculation, SLA determination, and response time estimation.

#### Schema Analysis

**Parameters:**
```sql
@taskType NVARCHAR(50)                    -- Task type (e.g., 'text_generation')
@inputData NVARCHAR(MAX)                  -- JSON input data
@modelId INT = NULL                       -- Optional model ID
@tenantId INT = 0                         -- Tenant identifier
@correlationId NVARCHAR(100) = NULL OUT   -- Correlation ID (generated if NULL)
@inferenceId BIGINT = NULL OUTPUT         -- Generated inference ID
```

**Dependencies:**
- `dbo.InferenceRequest` - Job storage (Part 8)
- `dbo.fn_CalculateComplexity` - CLR function ❌ NOT FOUND
- `dbo.fn_DetermineSla` - CLR function ❌ NOT FOUND
- `dbo.fn_EstimateResponseTime` - CLR function ❌ NOT FOUND
- Service Broker: `InferenceService`, `InferenceJobContract`, `InferenceJobRequest` message type

**Key Operations:**
1. Generate correlation ID if not provided
2. Parse input data for autonomous calculations
3. Calculate complexity, SLA tier, response time using CLR functions
4. Enrich metadata with calculated values
5. Insert into InferenceRequest table
6. Send message to Service Broker queue
7. Return job info to caller

#### Technical Details

**Input Parsing:**
```sql
DECLARE @tokenCount INT = JSON_VALUE(@inputData, '$.token_count');
DECLARE @requiresMultiModal BIT = JSON_VALUE(@inputData, '$.requires_multimodal');
DECLARE @requiresToolUse BIT = JSON_VALUE(@inputData, '$.requires_tools');
DECLARE @priority NVARCHAR(50) = JSON_VALUE(@inputData, '$.priority');
DECLARE @modelName NVARCHAR(255) = JSON_VALUE(@inputData, '$.model_name');

SET @tokenCount = ISNULL(@tokenCount, 1000);
SET @requiresMultiModal = ISNULL(@requiresMultiModal, 0);
SET @requiresToolUse = ISNULL(@requiresToolUse, 0);
SET @priority = ISNULL(@priority, 'medium');
```

**Autonomous Intelligence (CLR Functions):**
```sql
DECLARE @complexity INT = dbo.fn_CalculateComplexity(@tokenCount, @requiresMultiModal, @requiresToolUse);
DECLARE @sla NVARCHAR(20) = dbo.fn_DetermineSla(@priority, @complexity);
DECLARE @estimatedResponseTimeMs INT = dbo.fn_EstimateResponseTime(@modelName, @complexity);
```

**Metadata Enrichment:**
```sql
DECLARE @metadataJson NVARCHAR(MAX) = JSON_MODIFY(
    JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(@inputData, '$.complexity', @complexity),
            '$.sla_tier', @sla
        ),
        '$.estimated_response_time_ms', @estimatedResponseTimeMs
    ),
    '$.autonomous_metadata', JSON_QUERY('{
        "calculated_at": "' + CONVERT(NVARCHAR(27), SYSUTCDATETIME(), 126) + '",
        "intelligence_level": "database_native"
    }')
);
```

**Service Broker Message:**
```sql
BEGIN DIALOG CONVERSATION @DialogHandle
    FROM SERVICE [InferenceService]
    TO SERVICE 'InferenceService'
    ON CONTRACT [InferenceJobContract]
    WITH ENCRYPTION = OFF;

DECLARE @MessageXml XML = (
    SELECT 
        @inferenceId AS InferenceId,
        @taskType AS TaskType,
        @metadataJson AS InputData,
        @modelId AS ModelId,
        @tenantId AS TenantId,
        @correlationId AS CorrelationId
    FOR XML PATH('InferenceJob'), TYPE
);

SEND ON CONVERSATION @DialogHandle
    MESSAGE TYPE [InferenceJobRequest]
    (@MessageXml);
```

**Database Insert:**
```sql
INSERT INTO dbo.InferenceRequest (
    TaskType,
    InputData,
    Status,
    CorrelationId,
    RequestTimestamp,
    Complexity,
    SlaTier,
    EstimatedResponseTimeMs
)
VALUES (
    @taskType,
    @metadataJson,
    'Queued',           -- Initial status
    @correlationId,
    SYSUTCDATETIME(),
    @complexity,
    @sla,
    @estimatedResponseTimeMs
);
```

#### Issues Identified

**CRITICAL:**
1. ❌ **Missing CLR Functions:** Three functions referenced but not found
   - `dbo.fn_CalculateComplexity(INT, BIT, BIT) RETURNS INT`
   - `dbo.fn_DetermineSla(NVARCHAR(50), INT) RETURNS NVARCHAR(20)`
   - `dbo.fn_EstimateResponseTime(NVARCHAR(255), INT) RETURNS INT`
   - Impact: Procedure will fail at runtime
   - MUST implement or provide fallback logic

**HIGH:**
2. ⚠️ **Service Broker Not Verified:** No check if Service Broker is enabled
   - `BEGIN DIALOG` will fail if Service Broker disabled
   - MUST check: `SELECT is_broker_enabled FROM sys.databases WHERE database_id = DB_ID()`
   - MUST provide error message if not enabled

3. ⚠️ **No Conversation Cleanup:** `@DialogHandle` never ended
   - **REQUIRED:** `END CONVERSATION @DialogHandle` after SEND
   - Causes conversation handle leaks
   - OR use one-way messaging pattern

4. ⚠️ **Schema Mismatch:** InferenceRequest columns not validated (Part 8)
   - Uses `Complexity`, `SlaTier`, `EstimatedResponseTimeMs` columns
   - Part 8 audit shows InferenceRequest missing these columns
   - Need ALTER TABLE or remove column usage

**MEDIUM:**
5. ⚠️ **Hardcoded Defaults:** Magic numbers for missing JSON values
   - `SET @tokenCount = ISNULL(@tokenCount, 1000);`
   - MUST be configurable or based on model defaults

6. ⚠️ **No Input Validation:** Doesn't validate @inputData is valid JSON
   - MUST use `ISJSON(@inputData) = 1`
   - Prevents JSON parsing errors

7. ⚠️ **Comment Discrepancy:** "Changed from 'Pending' to 'Queued'"
   - Suggests previous status was 'Pending'
   - MUST document why changed (consistency with job lifecycle?)

8. ⚠️ **No Transaction:** Insert + Service Broker send not atomic
   - If SEND fails, InferenceRequest exists but no message queued
   - MUST wrap in transaction with proper error handling

**LOW:**
9. ⚠️ **Correlation ID Format:** Uses NEWID() GUID
   - MUST use sequential GUID for better indexing
   - Or allow custom format (e.g., prefixed IDs)

10. ⚠️ **XML vs JSON:** Service Broker uses XML, input uses JSON
    - Inconsistent formats
    - MUST send JSON directly if message type allows

#### Strengths
- ✅ **Service Broker Integration:** Modern message-based architecture
- ✅ **Autonomous Intelligence:** Database calculates metadata without app code
- ✅ **Metadata Enrichment:** Augments input with calculated values
- ✅ **Correlation Tracking:** Generates or accepts correlation IDs
- ✅ **Output Parameters:** Returns both correlation ID and inference ID
- ✅ **UTC Timestamps:** Proper timezone handling
- ✅ **Comment Documentation:** Explains paradigm shift from polling

#### REQUIRED FIXES
1. **CRITICAL:** Implement three missing CLR functions with proper logic
2. **HIGH:** Verify Service Broker enabled, provide clear error if not
3. **HIGH:** End conversation after SEND or document one-way pattern
4. **HIGH:** Validate InferenceRequest schema or add missing columns
5. **MEDIUM:** Wrap in transaction for atomicity
6. **MEDIUM:** Add ISJSON validation for @inputData
7. **MEDIUM:** Make default values configurable
8. **LOW:** Use NEWSEQUENTIALID() for correlation IDs
9. **LOW:** Document why status changed from 'Pending' to 'Queued'

#### Cross-References
- **Uses:** InferenceRequest (Part 8 - schema issues noted)
- **Activates:** sp_ExecuteInference_Activated (not yet analyzed)
- **Pattern:** Service Broker activation replaces polling worker pattern
- **Related:** PARADIGM-COMPLIANT asynchronous processing

---

### 12. sp_GetInferenceJobStatus
**Path:** `Procedures/dbo.sp_GetInferenceJobStatus.sql`  
**Type:** Stored Procedure (Job Status Query)  
**Quality Score:** 70/100

#### Purpose
Retrieves current status and results for a submitted inference job.

#### Schema Analysis

**Parameters:**
```sql
@inferenceId BIGINT                       -- Inference job ID
```

**Dependencies:**
- `dbo.InferenceRequest` - Job storage (Part 8)

**Key Operations:**
1. Query InferenceRequest by ID
2. Return job metadata and results

#### Technical Details

**Query:**
```sql
SELECT
    InferenceId,
    TaskType,
    Status,
    OutputData,
    Confidence,
    TotalDurationMs,
    RequestTimestamp,
    CompletionTimestamp,
    CorrelationId
FROM dbo.InferenceRequest
WHERE InferenceId = @inferenceId;
```

#### Issues Identified

**HIGH:**
1. ⚠️ **No Authorization Check:** Any user can query any job
   - Missing TenantId filter
   - Security risk: cross-tenant data access
   - MUST verify caller owns the job

2. ⚠️ **No Error Handling:** No TRY/CATCH block
   - Unhandled exceptions propagate to caller
   - MUST validate @inferenceId exists

3. ⚠️ **Limited Output:** Only returns InferenceRequest columns
   - Doesn't include calculated metadata (Complexity, SlaTier, EstimatedResponseTimeMs)
   - Missing model information
   - Missing token usage, costs

**MEDIUM:**
4. ⚠️ **No NULL Check:** Doesn't return error if job not found
   - Returns empty result set
   - MUST return explicit error or status

5. ⚠️ **Missing Columns:** Output doesn't match sp_SubmitInferenceJob enrichment
   - sp_SubmitInferenceJob adds Complexity, SlaTier, EstimatedResponseTimeMs
   - This procedure doesn't return those values
   - Inconsistent API

**LOW:**
6. ⚠️ **Performance:** No index hint or optimization
   - Assumes PK on InferenceId (likely exists)
   - MUST verify clustered index

#### Strengths
- ✅ **Simple and Direct:** Clear single-purpose procedure
- ✅ **Consistent Naming:** Follows get/retrieve pattern
- ✅ **Correlation ID:** Returns correlation tracking

#### REQUIRED FIXES
1. **HIGH:** Add TenantId authorization check
2. **HIGH:** Add TRY/CATCH error handling
3. **HIGH:** Include Complexity, SlaTier, EstimatedResponseTimeMs in output
4. **MEDIUM:** Return explicit error if job not found
5. **MEDIUM:** Add model information to output
6. **LOW:** Add token usage and cost information
7. **LOW:** IMPLEMENT returning related information (model name, tenant name)

#### Cross-References
- **Uses:** InferenceRequest (Part 8)
- **Related:** sp_SubmitInferenceJob (this batch), sp_UpdateInferenceJobStatus (this batch)
- **Pattern:** Simple getter procedure

---

### 13. sp_UpdateInferenceJobStatus
**Path:** `Procedures/dbo.sp_UpdateInferenceJobStatus.sql`  
**Type:** Stored Procedure (Job Status Update)  
**Quality Score:** 68/100

#### Purpose
Updates status and results for an inference job, typically called by worker processes or activation procedures.

#### Schema Analysis

**Parameters:**
```sql
@inferenceId BIGINT                       -- Inference job ID
@status NVARCHAR(50)                      -- New status
@outputData NVARCHAR(MAX) = NULL          -- Result data (JSON)
@confidence DECIMAL(5,4) = NULL           -- Confidence score
@totalDurationMs INT = NULL               -- Execution duration
@completionTimestamp DATETIME2 = NULL     -- Completion time
```

**Dependencies:**
- `dbo.InferenceRequest` - Job storage (Part 8)

**Key Operations:**
1. Update InferenceRequest with new status and results
2. Auto-set completion timestamp for terminal states

#### Technical Details

**Update Statement:**
```sql
UPDATE dbo.InferenceRequest
SET Status = @status,
    OutputData = @outputData,
    Confidence = @confidence,
    TotalDurationMs = @totalDurationMs,
    CompletionTimestamp = ISNULL(
        @completionTimestamp, 
        CASE WHEN @status IN ('Completed', 'Failed') 
             THEN SYSUTCDATETIME() 
             ELSE CompletionTimestamp 
        END
    )
WHERE InferenceId = @inferenceId;
```

#### Issues Identified

**CRITICAL:**
1. ❌ **No Authorization Check:** Any caller can update any job
   - No TenantId validation
   - Security risk: malicious status changes
   - Worker processes MUST have service account validation

**HIGH:**
2. ⚠️ **No Error Handling:** No TRY/CATCH block
   - Unhandled exceptions on invalid updates
   - MUST validate job exists

3. ⚠️ **No Validation:** Doesn't validate status values
   - MUST check: @status IN ('Queued', 'Processing', 'Completed', 'Failed', 'Cancelled')
   - Prevents typos and invalid states

4. ⚠️ **No Concurrency Control:** No optimistic locking
   - Multiple workers MUST update same job
   - MUST use: `WHERE InferenceId = @inferenceId AND Status = @previousStatus`
   - Or use row versioning

5. ⚠️ **No Audit Trail:** Status changes not logged
   - Can't track job lifecycle
   - MUST log state transitions

**MEDIUM:**
6. ⚠️ **Partial Updates:** Allows updating OutputData without status change
   - Unclear when to call with partial data
   - MUST enforce complete updates or separate procedures

7. ⚠️ **Timestamp Logic:** Auto-sets timestamp only for terminal states
   - What if caller provides timestamp for non-terminal state?
   - MUST validate or document behavior

8. ⚠️ **No @@ROWCOUNT Check:** Doesn't verify update succeeded
   - If job doesn't exist, silently succeeds
   - MUST return error or affected row count

**LOW:**
9. ⚠️ **No Output:** Doesn't return updated record
   - Caller can't verify update
   - MUST return updated job or status code

10. ⚠️ **Missing Columns:** Doesn't update Complexity, SlaTier, EstimatedResponseTimeMs
    - If job recalculated, can't update those values
    - Inconsistent with sp_SubmitInferenceJob

#### Strengths
- ✅ **Auto-Timestamp:** Convenient completion time handling
- ✅ **Flexible Parameters:** Optional parameters for partial updates
- ✅ **Terminal State Detection:** Recognizes 'Completed' and 'Failed'

#### REQUIRED FIXES
1. **CRITICAL:** Add service account or TenantId authorization
2. **HIGH:** Add TRY/CATCH error handling
3. **HIGH:** Validate status values with CHECK constraint or validation
4. **HIGH:** Implement optimistic locking with previous status check
5. **HIGH:** Log status transitions to audit table
6. **MEDIUM:** Check @@ROWCOUNT and return error if job not found
7. **MEDIUM:** Return updated job record
8. **MEDIUM:** Document or enforce complete vs partial update semantics
9. **LOW:** Add parameters for updating Complexity, SlaTier, EstimatedResponseTimeMs
10. **LOW:** IMPLEMENT row-level security for job updates

#### Cross-References
- **Uses:** InferenceRequest (Part 8)
- **Called by:** sp_ExecuteInference_Activated (not analyzed), worker processes
- **Related:** sp_SubmitInferenceJob (this batch), sp_GetInferenceJobStatus (this batch)
- **Pattern:** Status update pattern - needs audit and concurrency control

---

### 14. sp_BuildConceptDomains
**Path:** `Procedures/dbo.sp_BuildConceptDomains.sql`  
**Type:** Stored Procedure (Spatial Domain Construction)  
**Quality Score:** 75/100

#### Purpose
Constructs Voronoi-like spatial domains for each concept using nearest-neighbor boundaries. Simplified version of full 3D Voronoi tessellation.

#### Schema Analysis

**Parameters:**
```sql
@TenantId INT = 0                         -- Tenant filter
```

**Dependencies:**
- `provenance.Concepts` - Concept metadata (not yet analyzed)
- Spatial indexes on `CentroidSpatialKey` and `ConceptDomain`

**Key Operations:**
1. Collect concept centroids from Concepts table
2. For each concept, find distance to nearest neighbor
3. Create domain as buffer with radius = distance/2
4. Update Concepts table with computed domains
5. Ensure spatial indexes exist

#### Technical Details

**Centroid Collection:**
```sql
CREATE TABLE #ConceptCentroids (
    ConceptId INT PRIMARY KEY,
    Centroid GEOMETRY NOT NULL
);

INSERT INTO #ConceptCentroids (ConceptId, Centroid)
SELECT ConceptId, CentroidSpatialKey
FROM [provenance].[Concepts]
WHERE [TenantId] = @TenantId
      AND [IsActive] = 1
      AND [CentroidSpatialKey] IS NOT NULL;
```

**Domain Calculation (Cursor-Based):**
```sql
DECLARE concept_cursor CURSOR FOR
    SELECT ConceptId, Centroid 
    FROM #ConceptCentroids;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find nearest neighbor distance
    SELECT TOP 1 @NearestDistance = @Centroid.STDistance(cc.Centroid)
    FROM #ConceptCentroids cc
    WHERE cc.ConceptId <> @ConceptId
    ORDER BY @Centroid.STDistance(cc.Centroid) ASC;
    
    IF @NearestDistance IS NULL
        SET @NearestDistance = 1.0;  -- Single concept fallback
    
    -- Create buffer domain
    DECLARE @Radius FLOAT = @NearestDistance / 2.0;
    DECLARE @Domain GEOMETRY = @Centroid.STBuffer(@Radius);
    
    INSERT INTO #ConceptDomains (ConceptId, Domain, Radius)
    VALUES (@ConceptId, @Domain, @Radius);
END
```

**Domain Persistence:**
```sql
UPDATE c
SET 
    c.[ConceptDomain] = cd.Domain,
    c.[LastUpdatedAt] = SYSUTCDATETIME()
FROM [provenance].[Concepts] c
JOIN #ConceptDomains cd ON c.ConceptId = cd.ConceptId;
```

**Spatial Index Creation:**
```sql
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_Concepts_ConceptDomain')
BEGIN
    CREATE SPATIAL INDEX [SIX_Concepts_ConceptDomain] 
        ON [provenance].[Concepts]([ConceptDomain])
        WITH (BOUNDING_BOX = (-1, -1, 1, 1));
END
```

#### Issues Identified

**HIGH:**
1. ⚠️ **Cursor Usage:** Iterates concepts with cursor
   - Performance: O(N²) for N concepts
   - MUST be set-based with CROSS APPLY:
   ```sql
   WITH NearestNeighbors AS (
       SELECT 
           c1.ConceptId,
           c1.Centroid,
           MIN(c1.Centroid.STDistance(c2.Centroid)) AS NearestDistance
       FROM #ConceptCentroids c1
       CROSS JOIN #ConceptCentroids c2
       WHERE c1.ConceptId <> c2.ConceptId
       GROUP BY c1.ConceptId, c1.Centroid
   )
   INSERT INTO #ConceptDomains (ConceptId, Domain, Radius)
   SELECT 
       ConceptId,
       Centroid.STBuffer(NearestDistance / 2.0),
       NearestDistance / 2.0
   FROM NearestNeighbors;
   ```

2. ⚠️ **Duplicate Distance Calculation:** Computes STDistance in SELECT and ORDER BY
   - Minor performance issue
   - MUST use CTE or TOP 1 with CROSS APPLY

3. ⚠️ **Simplified Voronoi:** Comment warns "Simplified version"
   - True Voronoi cells MUST use perpendicular bisector planes
   - Current: Circular buffers that will overlap
   - MUST document limitations

**MEDIUM:**
4. ⚠️ **Hardcoded Bounding Box:** `WITH (BOUNDING_BOX = (-1, -1, 1, 1))`
   - Assumes normalized coordinates [-1, 1]
   - MUST compute from actual data:
   ```sql
   SELECT 
       MIN(CentroidSpatialKey.STX) AS MinX,
       MIN(CentroidSpatialKey.STY) AS MinY,
       MAX(CentroidSpatialKey.STX) AS MaxX,
       MAX(CentroidSpatialKey.STY) AS MaxY
   FROM provenance.Concepts;
   ```

5. ⚠️ **Single Concept Edge Case:** Sets @NearestDistance = 1.0
   - Arbitrary default radius
   - MUST be configurable parameter

6. ⚠️ **No Validation:** Doesn't check if domains overlap
   - Buffer approach can create overlapping domains
   - MUST validate or document overlaps

7. ⚠️ **Spatial Index Check:** Only checks by name
   - MUST have different index with same name in different schema
   - MUST check: `object_id = OBJECT_ID('provenance.Concepts')`

**LOW:**
8. ⚠️ **Temp Table Cleanup:** Uses # temp tables (auto-cleanup)
   - Good practice
   - MUST reuse variables/CTEs for better memory usage

9. ⚠️ **No Progress Reporting:** No feedback for long operations
   - MUST report progress every N concepts
   - Or return concept count processed

10. ⚠️ **Comment Reference:** "MIConvexHull CLR" mentioned
    - Suggests full implementation would use CLR library
    - MUST document if library is available

#### Strengths
- ✅ **Multi-Tenancy:** Filters by TenantId
- ✅ **Active Concepts Only:** Filters IsActive = 1
- ✅ **NULL Safety:** Checks CentroidSpatialKey IS NOT NULL
- ✅ **Spatial Index Management:** Ensures index exists
- ✅ **Timestamp Tracking:** Updates LastUpdatedAt
- ✅ **Result Reporting:** Returns updated count
- ✅ **Edge Case Handling:** Single concept case handled

#### REQUIRED FIXES
1. **HIGH:** Replace cursor with set-based CROSS APPLY solution
2. **HIGH:** Document Voronoi simplification and overlap possibilities
3. **MEDIUM:** Compute bounding box from actual data
4. **MEDIUM:** Make default radius configurable for single concept
5. **MEDIUM:** Validate or document domain overlaps
6. **MEDIUM:** Fix spatial index check to include object_id
7. **LOW:** Add progress reporting for large concept sets
8. **LOW:** IMPLEMENT implementing true Voronoi with MIConvexHull CLR
9. **LOW:** Return domain statistics (avg radius, overlap count, etc.)

#### Cross-References
- **Uses:** provenance.Concepts (not analyzed)
- **Related:** sp_DiscoverAndBindConcepts (Part 12 Batch 1)
- **Pattern:** Spatial partitioning for concept neighborhoods
- **Use Case:** Fast concept membership queries using spatial containment

---

### 15. vw_ReconstructModelLayerWeights
**Path:** `Views/dbo.vw_ReconstructModelLayerWeights.sql`  
**Type:** View (OLAP Query View)  
**Quality Score:** 88/100

#### Purpose
Provides materialized view of all model weights for analytics and reconstruction. Returns binary weight values that must be decoded client-side or via CLR.

#### Schema Analysis

**View Definition:**
```sql
CREATE VIEW [dbo].[vw_ReconstructModelLayerWeights] AS
SELECT 
    tac.[ModelId],
    m.[ModelName],
    tac.[LayerIdx],
    ml.[LayerName],
    tac.[PositionX],
    tac.[PositionY],
    tac.[PositionZ],
    a.[AtomicValue] AS [WeightValueBinary]  -- VARBINARY(64) containing IEEE 754 float32
FROM [dbo].[TensorAtomCoefficient] tac
JOIN [dbo].[Atom] a ON tac.[TensorAtomId] = a.[AtomId]
JOIN [dbo].[Model] m ON tac.[ModelId] = m.[ModelId]
LEFT JOIN [dbo].[ModelLayer] ml ON tac.[ModelId] = ml.[ModelId] AND tac.[LayerIdx] = ml.[LayerIdx]
WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';
```

**Dependencies:**
- `dbo.TensorAtomCoefficient` - Weight coefficients (Part 8)
- `dbo.Atom` - Atomic values (Part 1)
- `dbo.Model` - Model metadata (Part 2)
- `dbo.ModelLayer` - Layer metadata (not yet analyzed)

**Columns:**
- `ModelId` - Model identifier
- `ModelName` - Human-readable model name
- `LayerIdx` - Layer index (0-based)
- `LayerName` - Layer name (will be NULL if ModelLayer not populated - CRITICAL FIX)
- `PositionX`, `PositionY`, `PositionZ` - 3D tensor coordinates
- `WeightValueBinary` - VARBINARY(64) containing IEEE 754 float32

#### Technical Details

**Join Strategy:**
- INNER JOIN Atom: Only weights with atoms
- INNER JOIN Model: Only weights with valid models
- LEFT JOIN ModelLayer: Layer names optional

**Filtering:**
- `a.[Modality] = 'model'` - Only model atoms
- `a.[Subtype] = 'float32-weight'` - Only weight data (not biases, activations, etc.)

**Binary Format:**
- Comment: "VARBINARY(64) containing IEEE 754 float32"
- Requires client-side or CLR decoding
- Format: Standard binary float representation

#### Issues Identified

**MEDIUM:**
1. ⚠️ **No Indexed View:** View not created WITH SCHEMABINDING
   - Can't create indexed view without SCHEMABINDING
   - Performance: Will scan underlying tables on every query
   - Consider: `CREATE VIEW ... WITH SCHEMABINDING`

2. ⚠️ **No Conversion Function:** Returns binary, requires client decoding
   - Comment acknowledges: "decode client-side or use CLR functions"
   - MUST add CLR scalar function: `dbo.fn_BinaryToFloat32` (noted missing in Part 10)
   - Or provide separate computed column view

3. ⚠️ **No TenantId Filter:** Missing multi-tenancy
   - View returns all models for all tenants
   - MUST join Model table and filter by TenantId
   - Security risk for multi-tenant deployments

4. ⚠️ **LEFT JOIN ModelLayer:** Layer names will be NULL
   - If ModelLayer not populated, LayerName is NULL
   - MUST document this or make INNER JOIN if required

**LOW:**
5. ⚠️ **No Versioning:** Doesn't include temporal columns
   - TensorAtomCoefficient will have temporal versioning
   - MUST include ValidFrom/ValidTo for historical queries

6. ⚠️ **Column Aliasing:** Uses brackets for all identifiers
   - Consistent but verbose
   - Style preference

7. ⚠️ **No Index Hints:** Large weight tables MUST benefit from hints
   - MUST add WITH (NOLOCK) for read-only analytics
   - Or document expected query patterns

#### Strengths
- ✅ **EXCELLENT Documentation:** Clear comment about binary format
- ✅ **Rich Metadata:** Includes model name, layer name
- ✅ **3D Coordinates:** Full tensor position (X, Y, Z)
- ✅ **Filtered Data:** Only returns float32 weights
- ✅ **Optional Layer Names:** LEFT JOIN prevents missing data
- ✅ **OLAP-Friendly:** Suitable for analytics queries
- ✅ **Atomic Decomposition:** Leverages TensorAtomCoefficient architecture

#### REQUIRED FIXES
1. **HIGH:** Add TenantId filtering for security
2. **MEDIUM:** IMPLEMENT WITH SCHEMABINDING for indexed view potential
3. **MEDIUM:** Create companion view with decoded float values (using CLR)
4. **MEDIUM:** Add temporal columns for historical analysis
5. **LOW:** Document ModelLayer LEFT JOIN behavior
6. **LOW:** Add example queries to comments
7. **LOW:** IMPLEMENT NOLOCK hint for analytics workloads

#### Cross-References
- **Uses:** TensorAtomCoefficient (Part 8), Atom (Part 1), Model (Part 2), ModelLayer (not analyzed)
- **Related:** vw_ModelLayersWithStats (Part 8), vw_ReconstructModelLayerWeights (this)
- **Pattern:** Weight reconstruction from atomized storage
- **Missing:** fn_BinaryToFloat32 CLR function (noted Part 10)

---

## Batch 3 Summary

**Files Analyzed:** 5 files (4 procedures + 1 view)  
**Average Quality Score:** 77/100  
**Critical Issues:** 3 missing CLR functions, 3 authorization gaps  
**Total Lines:** ~200 lines of analyzed code

### Missing Objects Catalog (Updated)

**CLR Functions (6 total - 3 new):**
1. `dbo.ChainOfThoughtCoherence` - Coherence analysis (Batch 1)
2. `dbo.SelfConsistency` - Consensus detection (Batch 1)
3. `dbo.fn_CalculateComplexity` - Job complexity calculation (Batch 3) ⭐ NEW
4. `dbo.fn_DetermineSla` - SLA tier determination (Batch 3) ⭐ NEW
5. `dbo.fn_EstimateResponseTime` - Response time estimation (Batch 3) ⭐ NEW
6. `dbo.fn_BinaryToFloat32` - Binary-to-float conversion (Part 10, confirmed needed Batch 3)

**TVF Functions (1):**
1. `dbo.fn_BindAtomsToCentroid` - Atom-to-concept binding (Batch 1)

**Tables (3):**
1. `dbo.ReasoningChains` - Chain-of-thought storage (Batch 1)
2. `dbo.MultiPathReasoning` - Multi-path storage (Batch 1)
3. `dbo.SelfConsistencyResults` - Consensus storage (Batch 1)

**Service Broker Objects:**
- InferenceService (service)
- InferenceJobContract (contract)
- InferenceJobRequest (message type)
- Note: will exist but not verified in audit

### Quality Distribution (All Batches)
- **Excellent (85-100):** 3 files (sp_HybridSearch: 88, vw_ReconstructModelLayerWeights: 88, sp_SemanticSearch: 85)
- **Good (70-84):** 10 files
- **Fair (65-69):** 2 files (sp_GetInferenceJobStatus: 70, sp_UpdateInferenceJobStatus: 68)

### Architectural Patterns Observed

**Job Queue Pattern (Batch 3):**
- sp_SubmitInferenceJob: Service Broker messaging for async processing
- sp_GetInferenceJobStatus: Simple status query
- sp_UpdateInferenceJobStatus: Worker status updates
- Pattern: Message queue replaces polling architecture
- Issues: Missing authorization, concurrency control, audit trail

**Autonomous Intelligence (Batch 3):**
- Database calculates complexity, SLA, response time
- Comment: "intelligence_level": "database_native"
- Reduces app code, centralizes business logic
- Requires CLR functions (currently missing)

**Spatial Domain Partitioning (Batch 3):**
- sp_BuildConceptDomains: Simplified Voronoi tessellation
- Uses STBuffer for circular domains
- Enables fast spatial containment queries
- MUST be improved with true Voronoi (MIConvexHull CLR)

**Weight Reconstruction (Batch 3):**
- vw_ReconstructModelLayerWeights: OLAP-friendly weight access
- Returns binary format (requires CLR decode)
- Demonstrates atomic decomposition in practice

### Security Concerns

**Authorization Gaps (3 procedures):**
1. sp_GetInferenceJobStatus: No TenantId check
2. sp_UpdateInferenceJobStatus: No authorization
3. vw_ReconstructModelLayerWeights: No TenantId filter

**Risk:**
- Cross-tenant data access
- Malicious job status changes
- Unauthorized weight access

**IMPLEMENT:**
- Implement row-level security or add TenantId filters
- Add service account validation for worker procedures
- Audit all data access

### Performance Insights

**Cursor vs Set-Based:**
- sp_BuildConceptDomains: Cursor-based O(N²) MUST be set-based O(N log N)
- Pattern seen in multiple procedures
- Optimization opportunity: ~10-100x speedup for large datasets

**Service Broker Advantages:**
- Async processing without polling
- Built-in message durability
- Transactional message delivery
- Scalable worker pool architecture

**Indexed Views:**
- vw_ReconstructModelLayerWeights MUST benefit from SCHEMABINDING
- Current: Scans on every query
- Potential: Materialized indexed view for analytics

---

## Part 12 Complete Summary

**Total Files Analyzed:** 15 files (14 procedures + 1 view)  
**Overall Average Quality Score:** 78/100  
**Total Lines Audited:** ~950 lines of SQL code

### Quality Tier Breakdown
- **Excellent (85-100):** 3 files (20%)
- **Good (70-84):** 10 files (67%)
- **Fair (60-69):** 2 files (13%)
- **Poor (<60):** 0 files

### Critical Findings Summary

**Missing Objects (10 total):**
- 6 CLR functions (3 reasoning, 3 job management, 1 conversion)
- 1 TVF function
- 3 tables (reasoning storage)

**Security Issues:**
- 3 procedures missing authorization checks
- 1 view missing TenantId filter
- Cross-tenant data access risks

**Performance Issues:**
- 3 procedures using cursors instead of set-based operations
- Multi-tenancy query pattern (OR + EXISTS) affecting index usage
- Duplicate VECTOR_DISTANCE computations

**Schema Issues:**
- InferenceRequest missing columns (Complexity, SlaTier, EstimatedResponseTimeMs)
- Service Broker objects not verified
- Full-text index requirement not validated

### Architectural Highlights

**GOLD STANDARD Patterns:**
1. **sp_HybridSearch (88/100):** Two-phase spatial+vector search
   - O(log n) spatial filter + O(k) vector rerank
   - Scales to billions of vectors
   - Reference implementation for large-scale search

2. **vw_ReconstructModelLayerWeights (88/100):** Weight reconstruction
   - Clean separation of storage and presentation
   - OLAP-friendly analytics view
   - Documents binary format clearly

3. **sp_SemanticSearch (85/100):** Flexible search interface
   - Auto-embedding from text
   - Hybrid mode delegation
   - Inference tracking integration

**Advanced Reasoning Infrastructure:**
- Three sophisticated reasoning patterns (chain-of-thought, multi-path, self-consistency)
- CLR aggregate functions for vector analysis
- JSON serialization for complex results
- Set-based architecture (despite some WHILE loops)

**Service Broker Integration:**
- Message-based job queue (replaces polling)
- Autonomous intelligence in database
- Metadata enrichment pipeline
- Needs: Authorization, audit trail, concurrency control

### Category Analysis

**By Functional Area:**

**Reasoning (3 procedures):**
- Average quality: 74/100
- Sophisticated patterns but missing CLR functions
- Good JSON serialization

**Search (4 procedures):**
- Average quality: 82/100
- Excellent performance optimization
- Multi-modal and ensemble capabilities

**Data Quality (1 procedure):**
- sp_DetectDuplicates: 82/100
- Vector similarity for deduplication
- Missing automation features

**Unsupervised Learning (1 procedure):**
- sp_DiscoverAndBindConcepts: 78/100
- Two-phase discovery + binding
- Good evolution tracking

**Job Management (3 procedures):**
- Average quality: 73/100
- Service Broker integration
- Missing authorization and audit

**Spatial (1 procedure):**
- sp_BuildConceptDomains: 75/100
- Simplified Voronoi tessellation
- Cursor-based (optimization opportunity)

**Views (1 view):**
- vw_ReconstructModelLayerWeights: 88/100
- Excellent documentation
- Missing TenantId filter

### Cross-Cutting Concerns

**Multi-Tenancy:**
- 12/15 files support TenantId (80%)
- 3 files missing security checks
- Query pattern optimization needed (OR → UNION)

**Error Handling:**
- 11/15 files have TRY/CATCH (73%)
- 4 files need error handling

**Vector Operations:**
- 9/15 files use VECTOR(1998) (60%)
- Native SQL Server 2025 vector type
- Some duplicate distance calculations

**CLR Integration:**
- Heavy reliance on CLR functions (10 references)
- 6 missing CLR functions
- Good architecture but incomplete implementation

**JSON Usage:**
- 8/15 files use JSON (53%)
- Good for complex data structures
- Some files missing ISJSON validation

### REQUIRED FIXES Priority Matrix

**CRITICAL (Must Fix Before Production):**
1. Implement 6 missing CLR functions
2. Add authorization checks to 3 procedures + 1 view
3. Create 3 missing reasoning storage tables
4. Implement fn_BindAtomsToCentroid TVF
5. Verify/configure Service Broker objects

**HIGH (Performance & Correctness):**
1. Optimize multi-tenancy query pattern (OR → UNION)
2. Replace 3 cursor implementations with set-based operations
3. Add InferenceRequest schema columns or remove references
4. Implement audit trail for job status changes
5. Add optimistic locking to sp_UpdateInferenceJobStatus

**MEDIUM (Quality & Maintainability):**
1. Eliminate duplicate VECTOR_DISTANCE computations
2. Add ISJSON validation to all JSON parameters
3. Implement confidence scoring (remove hardcoded 0.8)
4. Add full-text index validation or LIKE fallback
5. Document Voronoi simplification in sp_BuildConceptDomains

**LOW (Polish & Future):**
1. Standardize variable naming conventions
2. Add progress reporting for long operations
3. IMPLEMENT SCHEMABINDING for indexed views
4. Implement true Voronoi with MIConvexHull CLR
5. Add comprehensive metadata to outputs

---

## Continuation Path

**Part 13 Preview:**
Next audit will analyze remaining procedures including:
- Atomization procedures (sp_AtomizeImage_Governed, sp_AtomizeCode, sp_AtomizeModel_Governed)
- Billing procedures (sp_CalculateBill, sp_GenerateUsageReport)
- Generation procedures (sp_GenerateText, sp_GenerateTextSpatial)
- Weight management (sp_CreateWeightSnapshot, sp_RestoreWeightSnapshot, etc.)
- Additional search and utility procedures

**Estimated Remaining:**
- ~30-40 procedures
- ~5-10 additional batches to complete audit

**Current Progress:**
- 114 files analyzed (99 previous + 15 this part) = ~36% of estimated 315 files

---

**End of Part 12 - All Batches Complete**

**Date:** 2024-12-XX  
**Analyst:** SQL Schema Audit Agent  
**Status:** ✅ Complete - 15 files analyzed across 3 batches  
**Next:** Part 13 - Atomization, billing, and generation procedures
