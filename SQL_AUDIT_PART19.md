# SQL Audit Part 19: Provenance, Discovery & Semantic Procedures

## Overview
Part 19 analyzes 5 procedures: provenance auditing, document discovery, impact analysis, and semantic feature computation.

---

## 1. sp_AuditProvenanceChain

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_AuditProvenanceChain.sql`  
**Type:** Stored Procedure  
**Lines:** ~127  
**Quality Score:** 86/100 ⭐

### Purpose
Comprehensive provenance audit for operations within date range. Validates provenance chains, detects anomalies, computes audit metrics.

### Parameters
- `@StartDate DATETIME2 = NULL` - Audit period start (defaults to 7 days ago)
- `@EndDate DATETIME2 = NULL` - Audit period end (defaults to now)
- `@Scope NVARCHAR(100) = NULL` - Filter by scope
- `@MinValidationScore FLOAT = 0.8` - Minimum validation threshold
- `@Debug BIT = 0` - Debug output

### Architecture

**Audit Process:**
1. Load operations in date range with validation results
2. Calculate metrics (total, valid, warnings, failures, averages)
3. Detect anomalies (missing provenance, validation failures, segment count outliers)
4. Store audit result in `ProvenanceAuditResults`
5. Return summary, anomalies, detailed operations

**Anomaly Detection:**
- Missing provenance streams
- Validation failures
- Statistical outliers (segment count > 2 std dev from mean)

### Key Operations

**Validation Score Mapping:**
```sql
CASE pvr.OverallStatus
    WHEN 'PASS' THEN 1.0
    WHEN 'WARN' THEN 0.7
    WHEN 'FAIL' THEN 0.0
    ELSE 0.5
END
```

**Anomaly Detection (Statistical):**
```sql
IF @StdDevSegments IS NOT NULL AND EXISTS (
    SELECT 1 FROM @Operations 
    WHERE ABS(SegmentCount - @AvgSegments) > 2 * @StdDevSegments
)
    INSERT INTO @Anomalies VALUES ('Segment Count Anomalies', ...);
```

### Dependencies
- Tables: `OperationProvenance`, `ProvenanceValidationResults`, `ProvenanceAuditResults`
- CLR Types: ProvenanceStream (hierarchyid or custom type)

### Quality Assessment

**Strengths:**
- ✅ **Comprehensive metrics** - Total, valid, warning, failed operations
- ✅ **Statistical anomaly detection** - 2-sigma outlier detection
- ✅ **Audit storage** - Results persisted for historical analysis
- ✅ **Multiple outputs** - Summary, anomalies, detailed results
- ✅ **Flexible date range** - Defaults to 7 days, accepts custom range
- ✅ **Scope filtering** - Optional scope parameter
- ✅ **Computed percentages** - Valid percentage, avg scores
- ✅ **JSON anomalies** - Structured anomaly storage

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering (could audit across tenants)
- ⚠️ **ProvenanceStream properties** - Uses .Scope, .Model, .SegmentCount, .IsNull (custom type not defined)
- ⚠️ **MinValidationScore unused** - Parameter not used in logic
- ⚠️ **TOP 100 limit** - Detailed results limited (could miss important operations)
- ⚠️ **No authorization** - Anyone can audit any operations
- ⚠️ **IsNull check** - ProvenanceStream.IsNull = 1 suggests CLR type (not documented)

**Performance:**
- Multiple aggregations over same dataset (could be optimized)
- Table variables for intermediate results (good for small datasets)
- Anomaly detection queries run sequentially (could batch)

**Security:**
- ⚠️ No TenantId filtering (cross-tenant audit possible)
- ⚠️ No authorization check

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering (ensure operations belong to tenant)
2. **Priority 2:** Use MinValidationScore parameter or remove it
3. **Priority 3:** Remove TOP 100 limit or make configurable
4. **Priority 4:** Document ProvenanceStream CLR type structure
5. **Priority 5:** Add authorization check
6. **Priority 6:** Optimize multiple aggregations (use single pass with GROUPING SETS)
7. **Priority 7:** Batch anomaly detection queries

---

## 2. sp_FindRelatedDocuments

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_FindRelatedDocuments.sql`  
**Type:** Stored Procedure  
**Lines:** ~84  
**Quality Score:** 84/100

### Purpose
Find related documents using hybrid approach: vector embedding similarity + graph neighbors. Combines multiple relevance signals.

### Parameters
- `@AtomId BIGINT` - Source atom
- `@TopK INT = 10` - Result limit
- `@TenantId INT = 0` - Multi-tenancy
- `@IncludeSemanticText BIT = 1` - Include semantic text (unused)
- `@IncludeVectorSimilarity BIT = 1` - Include vector search
- `@IncludeGraphNeighbors BIT = 1` - Include graph 1-hop neighbors

### Architecture

**Hybrid Search:**
1. **Vector similarity:** Find atoms with similar embeddings (cosine distance)
2. **Graph neighbors:** Find 1-hop neighbors (incoming + outgoing edges)
3. **Score combination:** Average vector score + graph score
4. Return top K by combined score

**Score Weighting:**
- Vector score: 1.0 - VECTOR_DISTANCE('cosine', ...)
- Graph score: 0.8 (hardcoded for neighbors)
- Combined: (VectorScore + GraphScore) / 2.0 (equal weighting)

### Key Operations

**MERGE for Score Combination:**
```sql
MERGE @Results AS target
USING (
    SELECT DISTINCT edge.ToAtomId AS AtomId, 0.8 AS GraphScore
    FROM provenance.AtomGraphEdges edge WHERE edge.FromAtomId = @AtomId
    UNION
    SELECT DISTINCT edge.FromAtomId AS AtomId, 0.8 AS GraphScore
    FROM provenance.AtomGraphEdges edge WHERE edge.ToAtomId = @AtomId
) AS source
ON target.RelatedAtomId = source.AtomId
WHEN MATCHED THEN UPDATE SET GraphScore = source.GraphScore
WHEN NOT MATCHED THEN INSERT (RelatedAtomId, VectorScore, GraphScore) VALUES (source.AtomId, 0.0, source.GraphScore);
```

### Dependencies
- Tables: `AtomEmbedding`, `provenance.AtomGraphEdges`, `Atom`
- Indexes: Vector index on AtomEmbedding.EmbeddingVector, indexes on AtomGraphEdges

### Quality Assessment

**Strengths:**
- ✅ **Hybrid approach** - Combines vector similarity and graph structure
- ✅ **Multi-tenancy** - TenantId filtering throughout
- ✅ **Configurable components** - Can enable/disable vector or graph
- ✅ **MERGE pattern** - Elegant score combination
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Bidirectional graph** - Checks both incoming and outgoing edges
- ✅ **DISTINCT** - Avoids duplicate graph neighbors

**Weaknesses:**
- ⚠️ **IncludeSemanticText unused** - Parameter not used in logic
- ⚠️ **Hardcoded graph score** - 0.8 should be configurable
- ⚠️ **Equal weighting** - Division by 2.0 assumes both components present
- ⚠️ **1-hop only** - No multi-hop graph traversal
- ⚠️ **ContentHash/ContentType** - Atom schema uses different columns
- ⚠️ **No zero-division protection** - If neither component enabled, CombinedScore = 0/2 = 0 (could divide by actual component count)

**Performance:**
- Vector similarity could be slow without vector index
- Graph UNION could be optimized with single query
- MERGE is efficient for score combination

**Security:**
- ✅ Multi-tenant safe with TenantId filtering

### Improvement Recommendations
1. **Priority 1:** Fix Atom schema references (ContentHash, ContentType)
2. **Priority 2:** Use IncludeSemanticText parameter or remove it
3. **Priority 3:** Make graph score (0.8) configurable parameter
4. **Priority 4:** Dynamic weighting based on enabled components
5. **Priority 5:** Add multi-hop graph option (@MaxDepth parameter)
6. **Priority 6:** Optimize graph query (single query instead of UNION)
7. **Priority 7:** Add vector index hint for performance

---

## 3. sp_FindImpactedAtoms

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_FindImpactedAtoms.sql`  
**Type:** Stored Procedure  
**Lines:** ~45  
**Quality Score:** 82/100

### Purpose
Find all downstream atoms impacted by a source atom. Uses recursive CTE to traverse provenance graph.

### Parameters
- `@AtomId BIGINT` - Source atom
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Recursive Traversal:**
```sql
WITH ImpactedAtoms AS (
    SELECT @AtomId AS AtomId, 0 AS Depth, CAST('Source' AS NVARCHAR(20)) AS ImpactType
    UNION ALL
    SELECT edge.ToAtomId AS AtomId, ia.Depth + 1 AS Depth, CAST('Downstream' AS NVARCHAR(20)) AS ImpactType
    FROM ImpactedAtoms ia
    INNER JOIN provenance.AtomGraphEdges edge ON ia.AtomId = edge.FromAtomId
    WHERE ia.Depth < 100
)
```

**Output:** Ordered by depth (levels of impact), includes total count via window function.

### Dependencies
- Tables: `provenance.AtomGraphEdges`, `Atom`

### Quality Assessment

**Strengths:**
- ✅ **Recursive CTE** - Proper graph traversal
- ✅ **Depth tracking** - Shows impact levels
- ✅ **Depth limit** - Max 100 prevents infinite loops
- ✅ **Multi-tenancy** - TenantId filtering
- ✅ **Error handling** - TRY/CATCH
- ✅ **Total count** - COUNT(*) OVER() provides total impacted
- ✅ **Ordered output** - By depth for understanding impact cascade

**Weaknesses:**
- ⚠️ **ContentHash/ContentType** - Atom schema mismatch
- ⚠️ **Downstream only** - Doesn't check upstream dependencies
- ⚠️ **No cycle detection** - Recursive CTE could loop if circular dependencies
- ⚠️ **Depth 100 hardcoded** - Should be parameter
- ⚠️ **No impact severity** - All impacts treated equally

**Performance:**
- Recursive CTE is efficient with proper indexes
- Depth limit prevents runaway queries
- Ordered by depth is appropriate

**Security:**
- ✅ Multi-tenant safe

### Improvement Recommendations
1. **Priority 1:** Fix Atom schema references (ContentHash, ContentType)
2. **Priority 2:** Add cycle detection (track visited nodes)
3. **Priority 3:** Make max depth a parameter
4. **Priority 4:** Add upstream impact option (@Direction parameter: 'downstream', 'upstream', 'both')
5. **Priority 5:** Add impact severity/weight based on relationship type

---

## 4. sp_ComputeSemanticFeatures

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ComputeSemanticFeatures.sql`  
**Type:** Stored Procedure  
**Lines:** ~67  
**Quality Score:** 65/100

### Purpose
Compute semantic features for single atom embedding. Used by `sp_ComputeAllSemanticFeatures` for batch processing. **PLACEHOLDER IMPLEMENTATION** - returns hardcoded values.

### Parameters
- `@atom_embedding_id BIGINT` - Embedding to compute features for

### Architecture

**Upsert Pattern:**
1. Retrieve embedding and atom data
2. Check if features exist
3. UPDATE if exists, INSERT if new
4. **Current implementation returns hardcoded neutral values**

**Placeholder Values:**
- Sentiment: 0.0 (neutral)
- Toxicity: 0.0 (non-toxic)
- Formality: 0.5 (medium)
- Complexity: 0.5 (medium)
- All topic scores: 0.0

### Dependencies
- Tables: `AtomEmbedding`, `Atom`, `SemanticFeatures`
- Procedures: Called by `sp_ComputeAllSemanticFeatures` (analyzed Part 12)

### Quality Assessment

**Strengths:**
- ✅ **Upsert pattern** - Handles new and existing features
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Validation** - Checks if embedding exists
- ✅ **Good structure** - Clear phases (retrieve, check, upsert)

**Weaknesses:**
- 🔴 **Incomplete implementation** - Returns hardcoded placeholder values
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **Embedding vector unused** - Retrieved but not used
- ⚠️ **CanonicalText unused** - Retrieved but not used
- ⚠️ **No actual computation** - Comment says "Feature computation would go here"
- ⚠️ **Missing features** - Schema has Sentiment/Toxicity but also TopicBusiness/Technical/etc
- ⚠️ **No authorization** - Anyone can compute features for any embedding

**Performance:**
- Simple queries, no performance issues
- Actual computation would be CPU-intensive

**Security:**
- ⚠️ No TenantId filtering

### Improvement Recommendations
1. **Priority 1:** Implement actual feature computation (sentiment analysis, topic classification, etc.)
2. **Priority 2:** Add TenantId filtering
3. **Priority 3:** Use embedding vector and canonical text in computation
4. **Priority 4:** Add authorization check
5. **Priority 5:** Consider CLR functions for ML-based feature extraction
6. **Priority 6:** Add @ComputeMethod parameter (simple, ML-based, etc.)
7. **Priority 7:** Return computed features (not just status code)

---

## 5. sp_SemanticSimilarity

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_SemanticSimilarity.sql`  
**Type:** Stored Procedure  
**Lines:** ~41  
**Quality Score:** 80/100

### Purpose
Find semantically similar atoms using vector cosine similarity. Pure vector-based search.

### Parameters
- `@SourceAtomId BIGINT` - Source atom
- `@TopK INT = 10` - Result limit
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Simple Vector Search:**
1. Retrieve source embedding vector
2. Find top K similar atoms by cosine distance
3. Return similarity score as percentage (0-100)

### Key Operations

**Cosine Similarity:**
```sql
(1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @SourceEmbedding)) * 100.0 AS SimilarityScore
```

### Dependencies
- Tables: `AtomEmbedding`, `Atom`
- Indexes: Vector index on AtomEmbedding.EmbeddingVector

### Quality Assessment

**Strengths:**
- ✅ **Simple and focused** - Does one thing well
- ✅ **Multi-tenancy** - Proper TenantId filtering
- ✅ **Error handling** - TRY/CATCH
- ✅ **Validation** - Checks if source has embedding
- ✅ **Percentage score** - User-friendly 0-100 scale
- ✅ **Self-exclusion** - Filters out source atom

**Weaknesses:**
- ⚠️ **ContentHash/ContentType** - Atom schema mismatch
- ⚠️ **Double VECTOR_DISTANCE** - Computed in SELECT and ORDER BY
- ⚠️ **No vector index hint** - Could benefit from explicit hint

**Performance:**
- VECTOR_DISTANCE computed twice (inefficiency)
- Vector index usage depends on optimizer

**Security:**
- ✅ Multi-tenant safe

### Improvement Recommendations
1. **Priority 1:** Fix Atom schema references (ContentHash, ContentType)
2. **Priority 2:** Use CTE to compute VECTOR_DISTANCE once
3. **Priority 3:** Add vector index hint for performance
4. **Priority 4:** Consider returning raw similarity (0-1) instead of percentage for consistency

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~364  
**Average Quality:** 79.4/100

**Quality Distribution:**
- Excellent (85-100): 1 file (sp_AuditProvenanceChain 86⭐)
- Good (70-84): 3 files (sp_FindRelatedDocuments 84, sp_FindImpactedAtoms 82, sp_SemanticSimilarity 80)
- Fair (60-69): 1 file (sp_ComputeSemanticFeatures 65)

**Key Patterns:**
- **Provenance operations** - 3 procedures work with provenance graphs (audit, impact, related docs)
- **Recursive CTEs** - 2 procedures use recursive graph traversal
- **Schema mismatches** - Multiple procedures reference non-existent Atom columns (ContentHash, ContentType)
- **Incomplete implementations** - sp_ComputeSemanticFeatures returns placeholder values

**Security Issues:**
- ⚠️ 2 procedures missing TenantId filtering (sp_AuditProvenanceChain, sp_ComputeSemanticFeatures)
- ⚠️ No authorization checks in any procedure

**Performance Issues:**
- VECTOR_DISTANCE computed twice in sp_SemanticSimilarity
- Multiple aggregations over same dataset in sp_AuditProvenanceChain

**Schema Issues:**
- 🔴 **Atom table:** 3 procedures reference ContentHash, ContentType (columns don't exist)
- ⚠️ **ProvenanceStream CLR type:** sp_AuditProvenanceChain uses undefined properties

**Critical Issues:**
1. Schema mismatch in 3 procedures (ContentHash, ContentType)
2. sp_ComputeSemanticFeatures incomplete (returns hardcoded values)
3. ProvenanceStream CLR type not documented/defined
4. Missing TenantId filtering in 2 procedures

**Recommendations:**
1. Fix Atom schema references in all procedures (Priority 1 - Correctness)
2. Document/define ProvenanceStream CLR type structure (Priority 1 - Documentation)
3. Implement sp_ComputeSemanticFeatures actual computation (Priority 1 - Functionality)
4. Add TenantId filtering to sp_AuditProvenanceChain and sp_ComputeSemanticFeatures (Priority 2 - Security)
5. Optimize VECTOR_DISTANCE to compute once (Priority 2 - Performance)
6. Add authorization checks to all procedures (Priority 3 - Security)

---

## Cross-Reference: Atom Schema Mismatch

**CRITICAL FINDING:** 3 procedures in Part 19 reference non-existent Atom columns:

**Referenced (Expected):**
- `ContentHash VARBINARY(64)` (or similar)
- `ContentType NVARCHAR(100)`

**Actual Atom Schema (from earlier parts):**
- `Modality NVARCHAR(50)`
- `Subtype NVARCHAR(50)`
- `CanonicalText NVARCHAR(MAX)`
- `PayloadLocator NVARCHAR(MAX)` (or FILESTREAM)

**Impact:**
- sp_FindRelatedDocuments: Returns `a.ContentHash, a.ContentType` (will fail at runtime)
- sp_FindImpactedAtoms: Returns `a.ContentHash, a.ContentType` (will fail at runtime)
- sp_SemanticSimilarity: Returns `a.ContentHash, a.ContentType` (will fail at runtime)

**Fix Required:**
Replace all references:
```sql
-- OLD (WRONG):
a.ContentHash, a.ContentType

-- NEW (CORRECT):
a.Modality, a.Subtype, a.CanonicalText
```

**Estimated Scope:**
- 3 procedures in Part 19
- Likely more procedures in Parts 1-18 (needs cross-check)
- Suggest global search: `grep_search` for "ContentHash" and "ContentType" across all procedure files

---

## ProvenanceStream CLR Type (Undefined)

**CRITICAL FINDING:** sp_AuditProvenanceChain uses `ProvenanceStream` CLR type with properties:
- `.Scope` (NVARCHAR)
- `.Model` (NVARCHAR)
- `.SegmentCount` (INT)
- `.IsNull` (BIT)

**Issue:** This CLR type is not defined in Part 11 (CLR analysis) or documented elsewhere.

**Hypothesis:**
- ProvenanceStream is likely a hierarchyid wrapper or custom CLR type
- Used for encoding provenance chains compactly
- May be serialized/deserialized from OperationProvenance.ProvenanceData

**Action Required:**
1. Search for ProvenanceStream CLR assembly definition
2. Document schema and methods
3. Validate usage in sp_AuditProvenanceChain

---

## Part 19 Completion

**Progress Update:**
- Total SQL files: 325
- Analyzed through Part 19: 144 files (44.3%)
- Remaining: 181 files (55.7%)

**Next Steps:**
- Part 20: Remaining procedures (~12 files)
- Parts 21-30: Tables (~90 files)
- Parts 31-32: Functions (~15 files)
- Part 33: Service Broker (~15 files)
- Part 34: Indexes (~25 files)
- Part 35: Scripts (~15 files)
