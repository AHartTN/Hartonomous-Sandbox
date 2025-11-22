# ?? **PHASE 7: SURGICAL FIX STRATEGY**

**Date**: January 2025  
**Status**: Strategic Review Complete  
**Approach**: Targeted fixes, not scorched earth

---

## **?? AUDIT FINDINGS SUMMARY**

| Category | Count | Auto-Fix | Review | Manual |
|----------|-------|----------|--------|--------|
| **Non-idempotent procedures** | 69 | ? 69 | - | - |
| **Old vector dimensions (1998)** | 15 | ? 15 | - | - |
| **Missing CLR functions** | 6 | - | - | ?? 6 |
| **Missing tables** | 3 | - | - | ?? 3 |
| **Missing authorization** | 4 | - | ?? 4 | - |
| **Deprecated geometry** | Multiple | - | ?? Review | - |
| **Performance (cursors)** | 3 | - | ?? 3 | - |

---

## **? CATEGORY 1: SAFE AUTO-FIXES (Execute Immediately)**

### **1.1: Idempotency (69 procedures)**
**Action**: `CREATE PROCEDURE` ? `CREATE OR ALTER PROCEDURE`

**Rationale**:
- Mechanical replacement
- Zero business logic impact
- Enables idempotent deployment
- Required for production CI/CD

**Method**: Regex replacement in all procedures
```powershell
$content -replace '^\s*CREATE\s+PROCEDURE', 'CREATE OR ALTER PROCEDURE'
```

**Risk**: ? **ZERO** - Purely syntax change

---

### **1.2: Vector Dimensions (15 procedures)**
**Action**: `VECTOR(1998)` ? `VECTOR(1536)`

**Rationale**:
- OpenAI standard dimension
- Matches TransformerInference.cs output (1536)
- Consistent with Phase 3 roadmap
- 1998 was arbitrary/experimental

**Affected Procedures**:
1. sp_Analyze.sql
2. sp_ChainOfThoughtReasoning.sql
3. sp_CognitiveActivation.sql
4. sp_ComputeSpatialProjection.sql
5. sp_ExactVectorSearch.sql
6. sp_FindRelatedDocuments.sql
7. sp_FusionSearch.sql
8. sp_HybridSearch.sql
9. sp_ScoreWithModel.sql
10. sp_SelfConsistencyReasoning.sql
11. sp_SemanticFilteredSearch.sql
12. sp_SemanticSearch.sql
13. sp_SemanticSimilarity.sql
14. sp_TemporalVectorSearch.sql
15. sp_TextToEmbedding.sql

**Method**: Regex replacement
```powershell
$content -replace 'VECTOR\(1998\)', 'VECTOR(1536)'
```

**Risk**: ?? **LOW** - Dimension mismatch would cause runtime errors (caught in testing)

**Verification**: Run Phase_5_Verification.sql Test #4

---

## **?? CATEGORY 2: REVIEW NEEDED (Case-by-Case Decision)**

### **2.1: Authorization Gaps (4 files)**

#### **sp_GetInferenceJobStatus.sql**
**Issue**: No TenantId check - any user can query any job

**Decision**: ? **FIX** - Add authorization
```sql
WHERE InferenceId = @inferenceId
  AND (@TenantId IS NULL OR EXISTS (
      SELECT 1 FROM dbo.Atom a 
      WHERE a.AtomId = ir.OutputAtomId -- if exists
        AND (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
  ))
```

**Risk**: ??? **MEDIUM** - Security sensitive, but procedure is simple

#### **sp_UpdateInferenceJobStatus.sql**
**Issue**: No authorization - worker can update any job

**Decision**: ?? **DEFER** - This is called by Service Broker activation
- Service account context (no user TenantId)
- Needs service-level authorization pattern
- Requires architecture discussion

**Risk**: ???? **HIGH** - But requires design decision

#### **sp_TemporalVectorSearch.sql**
**Issue**: No TenantId filter - temporal data leak

**Decision**: ? **FIX** - Add TenantId parameter and filter
```sql
WHERE ... 
  AND (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
```

**Risk**: ?? **LOW** - Simple parameter addition

#### **sp_CrossModalQuery.sql**
**Issue**: No TenantId parameter

**Decision**: ? **FIX** - Add TenantId parameter and filter

**Risk**: ?? **LOW** - Simple parameter addition

---

### **2.2: Geometry Patterns (Multiple files)**

**Issue**: `geometry::Point(x, y, z)` drops M-value

**Current Status**: 
- ? Fixed in sp_AtomizeImage_Governed (Phase 2)
- ? Fixed in sp_AtomizeText_Governed (Phase 2)
- ?? Other procedures may still use old pattern

**Decision**: ?? **REVIEW EACH FILE**
- If procedure creates atoms: **FIX** (use STGeomFromText with M)
- If procedure only queries: **LEAVE** (reading existing geometry)

**Files to Review**:
- Run: `Get-ChildItem -Recurse | Where-Object { (Get-Content $_.FullName -Raw) -match "geometry::Point\(" }`

**Risk**: ??? **MEDIUM** - Must understand context

---

### **2.3: Performance (Cursors - 3 procedures)**

#### **sp_BuildConceptDomains.sql**
**Issue**: Cursor-based Voronoi domain construction

**Decision**: ? **FIX** - Replace with set-based CROSS JOIN
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
INSERT INTO #ConceptDomains
SELECT ConceptId, Centroid.STBuffer(NearestDistance / 2.0), NearestDistance / 2.0
FROM NearestNeighbors;
```

**Risk**: ??? **MEDIUM** - Logic equivalent, but test carefully

#### **sp_DiscoverAndBindConcepts.sql**
**Issue**: Cursor iterates concepts for binding

**Decision**: ? **FIX** - Replace with CROSS APPLY
```sql
INSERT INTO @AtomBindings
SELECT ab.AtomId, ic.ConceptId, ab.Similarity, 0
FROM @InsertedConcepts ic
CROSS APPLY dbo.fn_BindAtomsToCentroid(ic.Centroid, @SimilarityThreshold, @TenantId) ab;
```

**Risk**: ???? **HIGH** - Depends on fn_BindAtomsToCentroid existing

#### **sp_ValidateOperationProvenance.sql**
**Issue**: WHILE loop for segment enumeration

**Decision**: ? **FIX** - Use clr_EnumerateAtomicStreamSegments
```sql
INSERT INTO @SegmentValidations
SELECT 
    SegmentIndex,
    SegmentKind,
    SegmentTimestamp,
    CASE 
        WHEN SegmentKind NOT IN (...) THEN 'WARN: Unknown segment kind'
        WHEN SegmentIndex > 0 AND SegmentTimestamp < LAG(SegmentTimestamp) OVER (ORDER BY SegmentIndex) 
        THEN 'FAIL: Timestamp out of order'
        ELSE 'PASS'
    END AS ValidationStatus
FROM provenance.clr_EnumerateAtomicStreamSegments(@ProvenanceStream);
```

**Risk**: ??? **MEDIUM** - Depends on TVF existing

---

## **?? CATEGORY 3: MANUAL DECISIONS (Architecture/Design)**

### **3.1: Missing CLR Functions (6 functions)**

These require **business logic implementation**, not mechanical fixes:

#### **Reasoning Functions (Batch 1):**
1. **dbo.ChainOfThoughtCoherence** 
   - Purpose: Analyze coherence across reasoning steps
   - Input: Step vectors (VECTOR array)
   - Output: JSON with coherence score, variance, outliers
   - **Decision**: ? **DEFER** - Requires ML algorithm design

2. **dbo.SelfConsistency**
   - Purpose: Find consensus among multiple samples
   - Input: Response vectors, answer vectors, confidences
   - Output: JSON with consensus answer, agreement ratio
   - **Decision**: ? **DEFER** - Requires clustering algorithm

#### **Job Management Functions (Batch 3):**
3. **dbo.fn_CalculateComplexity**
   - Purpose: Calculate job complexity score
   - Input: tokenCount, requiresMultiModal, requiresToolUse
   - Output: INT complexity score (1-100)
   - **Decision**: ? **IMPLEMENT** - Simple heuristic

4. **dbo.fn_DetermineSla**
   - Purpose: Map priority + complexity to SLA tier
   - Input: priority (string), complexity (int)
   - Output: SLA tier ('standard', 'premium', 'enterprise')
   - **Decision**: ? **IMPLEMENT** - Lookup table logic

5. **dbo.fn_EstimateResponseTime**
   - Purpose: Estimate inference time
   - Input: modelName, complexity
   - Output: INT milliseconds
   - **Decision**: ? **IMPLEMENT** - Model metadata lookup

#### **Conversion Function:**
6. **dbo.fn_BinaryToFloat32**
   - Purpose: Convert VARBINARY to float
   - Input: VARBINARY(4)
   - Output: FLOAT
   - **Decision**: ? **IMPLEMENT** - IEEE 754 decode

**Immediate Fix Strategy**:
- Functions 3-6: Implement with simple logic (can enhance later)
- Functions 1-2: Create stub returning NULL (prevents failures, log warning)

---

### **3.2: Missing Tables (3 tables)**

#### **dbo.ReasoningChains**
**Used by**: sp_ChainOfThoughtReasoning  
**Decision**: ? **CREATE** - Simple schema from audit recommendation

#### **dbo.MultiPathReasoning**
**Used by**: sp_MultiPathReasoning  
**Decision**: ? **CREATE** - Simple schema from audit recommendation

#### **dbo.SelfConsistencyResults**
**Used by**: sp_SelfConsistencyReasoning  
**Decision**: ? **CREATE** - Simple schema from audit recommendation

**Risk**: ? **ZERO** - Tables are data storage only

---

### **3.3: Missing TVF (1 function)**

#### **dbo.fn_BindAtomsToCentroid**
**Used by**: sp_DiscoverAndBindConcepts  
**Purpose**: Find atoms similar to concept centroid

**Decision**: ? **IMPLEMENT** - Simple vector similarity query
```sql
CREATE FUNCTION dbo.fn_BindAtomsToCentroid (
    @CentroidVector VARBINARY(MAX),
    @SimilarityThreshold FLOAT,
    @TenantId INT
)
RETURNS TABLE
AS
RETURN (
    SELECT ae.AtomId, 1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, CAST(@CentroidVector AS VECTOR(1536))) AS Similarity
    FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
    WHERE (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
      AND (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, CAST(@CentroidVector AS VECTOR(1536)))) >= @SimilarityThreshold
);
```

**Risk**: ?? **LOW** - Standard vector similarity query

---

## **?? EXECUTION PLAN**

### **Phase 7.1: Auto-Fixes (Execute Now)**
1. ? Convert all 69 procedures to CREATE OR ALTER
2. ? Update all 15 procedures from VECTOR(1998) to VECTOR(1536)
3. ? Run build verification

**Estimated Time**: 2 minutes  
**Risk**: ? MINIMAL

---

### **Phase 7.2: Security Fixes (Execute Now)**
1. ? Add TenantId to sp_TemporalVectorSearch
2. ? Add TenantId to sp_CrossModalQuery
3. ? Defer sp_UpdateInferenceJobStatus (needs architecture review)

**Estimated Time**: 10 minutes  
**Risk**: ?? LOW

---

### **Phase 7.3: Create Missing Objects (Execute Now)**
1. ? Create 3 missing tables (ReasoningChains, MultiPathReasoning, SelfConsistencyResults)
2. ? Create fn_BindAtomsToCentroid TVF
3. ? Create stub CLR functions (3 job management + fn_BinaryToFloat32)
4. ? Defer complex CLR aggregates (ChainOfThoughtCoherence, SelfConsistency)

**Estimated Time**: 20 minutes  
**Risk**: ?? LOW

---

### **Phase 7.4: Performance Optimizations (Deferred)**
1. ? Replace cursors with set-based (needs testing)
2. ? Optimize multi-tenancy OR ? UNION (needs query plan analysis)
3. ? Eliminate duplicate VECTOR_DISTANCE (needs CTE refactoring)

**Reason for Deferral**: Requires performance testing, not blocking deployment

---

### **Phase 7.5: Geometry Review (Deferred)**
1. ? Audit all geometry::Point() usage
2. ? Fix only atomization procedures (create operations)
3. ? Leave query procedures unchanged

**Reason for Deferral**: Need to understand context of each usage

---

## **? APPROVED FOR EXECUTION**

Execute Phase 7.1, 7.2, 7.3 now:
- 84 auto-fixes (69 + 15)
- 2 security fixes
- 8 object creations

**Total**: 94 targeted changes

**NOT EXECUTING**:
- Performance optimizations (needs testing)
- Complex CLR aggregates (needs algorithm design)
- Geometry review (needs context analysis)
- Worker authorization (needs architecture decision)

---

**Ready to proceed with surgical fixes?**

