# SQL Audit Part 14: Generation & Weight Management Procedures

## Overview
Part 14 analyzes 5 procedures: spatial text generation, A* pathfinding, and weight snapshot management (3 procedures from Admin.WeightRollback.sql).

---

## 1. sp_GenerateTextSpatial

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_GenerateTextSpatial.sql`  
**Type:** Stored Procedure  
**Lines:** ~60  
**Quality Score:** 64/100

### Purpose
Spatial geometry-based text generation using R-tree nearest neighbor search for next-token prediction.

### Parameters
- `@prompt NVARCHAR(MAX)` - Input prompt text
- `@max_tokens INT = 10` - Maximum tokens to generate (default 10)
- `@temperature FLOAT = 1.0` - Sampling temperature

### Architecture

**Generation Loop:**
1. Parse prompt words into context table (atoms matching words)
2. For each iteration (up to max_tokens):
   - Build comma-separated list of context atom IDs
   - Call `sp_SpatialNextToken` to get next token via spatial search
   - Add token to context and generated text
   - Break if no valid next token or duplicate detected
3. Return prompt, generated text, token count, method name

### Key Operations

**Prompt Parsing:**
```sql
INSERT INTO @context (AtomId, AtomText)
SELECT a.AtomId, CAST(a.CanonicalText AS NVARCHAR(100))
FROM dbo.Atom a
WHERE CAST(a.CanonicalText AS NVARCHAR(100)) IN (
    SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@prompt, ' ')
);
```

**Next Token Prediction:**
```sql
INSERT INTO @next
EXEC dbo.sp_SpatialNextToken
    @context_atom_ids = @context_ids,
    @temperature = @temperature,
    @top_k = 1;
```

### Dependencies
- Tables: `Atom`
- Procedures: `dbo.sp_SpatialNextToken` - **MISSING** (not found in audit)
- Indexes: Depends on sp_SpatialNextToken implementation

### Quality Assessment

**Strengths:**
- ✅ **Novel approach** - Spatial R-tree for next-token prediction
- ✅ **Duplicate detection** - Prevents infinite loops
- ✅ **Simple architecture** - Easy to understand generation loop

**Weaknesses:**
- 🔴 **Missing dependency** - sp_SpatialNextToken not implemented
- ⚠️ **Naive prompt parsing** - STRING_SPLIT by space doesn't handle punctuation, case sensitivity
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **Truncation to 100 chars** - NVARCHAR(100) loses long atom text
- ⚠️ **IN clause with STRING_SPLIT** - Poor performance for long prompts
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **Low max_tokens default** - 10 tokens is very short generation
- ⚠️ **Hardcoded top_k=1** - No diversity in generation (deterministic after temperature)
- ⚠️ **No early stopping** - Should detect end-of-sequence tokens

**Performance:**
- IN clause with STRING_SPLIT is inefficient (O(N*M) where N=atoms, M=words)
- WHILE loop with EXEC is slow (no set-based operations)
- No indexes specified for Atom.CanonicalText lookup

**Security:**
- ⚠️ No TenantId isolation
- ⚠️ No input validation (prompt length, max_tokens range)

### Improvement Recommendations
1. **Priority 1:** Implement missing `sp_SpatialNextToken` procedure
2. **Priority 2:** Add multi-tenancy (TenantId parameter and filtering)
3. **Priority 3:** Improve prompt parsing (tokenization, case-insensitive, punctuation handling)
4. **Priority 4:** Add error handling (TRY/CATCH)
5. **Priority 5:** Increase NVARCHAR(100) to NVARCHAR(MAX) or configurable length
6. **Priority 6:** Add early stopping (detect <EOS> tokens)
7. **Priority 7:** Expose top_k as parameter for sampling diversity
8. **Priority 8:** Add input validation

---

## 2. sp_GenerateOptimalPath

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_GenerateOptimalPath.sql`  
**Type:** Stored Procedure  
**Lines:** ~160  
**Quality Score:** 90/100 ⭐

### Purpose
**GOLD STANDARD** A* pathfinding algorithm for semantic space navigation. Finds optimal path from start atom to target concept domain using spatial distance heuristic.

### Parameters
- `@StartAtomId BIGINT` - Starting atom in semantic space
- `@TargetConceptId INT` - Target concept (Voronoi domain)
- `@MaxSteps INT = 50` - Maximum A* iterations
- `@NeighborRadius FLOAT = 0.5` - Spatial search radius for neighbors

### Architecture

**A* Algorithm Implementation:**

1. **Initialization:**
   - Load start point from `AtomEmbedding.SpatialKey`
   - Load target concept domain and centroid from `provenance.Concepts`
   - Create OpenSet (table variable) and ClosedSet (temp table)

2. **A* Main Loop:**
   - While OpenSet not empty AND steps < MaxSteps AND goal not reached:
     1. Get node with lowest fCost (gCost + hCost) from OpenSet
     2. Check if current point is within target ConceptDomain (goal test)
     3. Move current from Open to Closed
     4. Find neighbors using spatial index (STBuffer + STIntersects)
     5. MERGE neighbors into OpenSet (update if better path found)
     6. Increment step counter

3. **Path Reconstruction:**
   - Use recursive CTE to walk backwards from goal to start
   - Return ordered path with atom details, spatial positions, distance to goal

### Key Operations

**Goal Test:**
```sql
IF @CurrentPoint.STWithin(@TargetRegion) = 1
BEGIN
    SET @GoalAtomId = @CurrentAtomId;
    BREAK;
END
```

**Neighbor Search (Spatial Index):**
```sql
WITH Neighbors AS (
    SELECT ae.AtomId, ae.SpatialKey,
        @CurrentPoint.STDistance(ae.SpatialKey) AS StepCost,
        ae.SpatialKey.STDistance(@TargetCentroid) AS HeuristicCost
    FROM dbo.AtomEmbedding ae WITH(INDEX(SIX_AtomEmbedding_SpatialKey))
    WHERE ae.SpatialKey.STIntersects(@NeighborSearchRegion) = 1
      AND ae.AtomId <> @CurrentAtomId
      AND NOT EXISTS (SELECT 1 FROM #ClosedSet WHERE AtomId = ae.AtomId)
)
```

**Path Update (MERGE):**
```sql
MERGE @OpenSet AS T
USING Neighbors AS S
ON T.AtomId = S.AtomId
WHEN MATCHED AND (S.StepCost + @gCost) < T.gCost THEN
    UPDATE SET T.ParentAtomId = @CurrentAtomId, T.gCost = S.StepCost + @gCost, T.hCost = S.HeuristicCost
WHEN NOT MATCHED BY TARGET THEN
    INSERT (AtomId, ParentAtomId, gCost, hCost) VALUES (S.AtomId, @CurrentAtomId, S.StepCost + @gCost, S.HeuristicCost);
```

**Path Reconstruction (Recursive CTE):**
```sql
WITH PathCTE AS (
    SELECT AtomId, ParentAtomId, 0 AS Depth FROM #ClosedSet WHERE AtomId = @GoalAtomId
    UNION ALL
    SELECT cs.AtomId, cs.ParentAtomId, p.Depth + 1
    FROM #ClosedSet cs JOIN PathCTE p ON cs.AtomId = p.ParentAtomId
    WHERE p.ParentAtomId IS NOT NULL
)
```

### Dependencies
- Tables: `AtomEmbedding`, `provenance.Concepts`, `Atom`
- Indexes: `SIX_AtomEmbedding_SpatialKey` (spatial index - **CRITICAL**)
- Functions: GEOMETRY methods (STWithin, STDistance, STBuffer, STIntersects)

### Quality Assessment

**Strengths:**
- ✅ **Perfect A* implementation** - Textbook algorithm with all optimizations
- ✅ **Spatial index usage** - Explicit INDEX hint for performance
- ✅ **Computed column for fCost** - Elegant cost tracking
- ✅ **MERGE for path updates** - Efficient node discovery and cost improvement
- ✅ **Recursive CTE** - Clean path reconstruction
- ✅ **Goal test optimization** - STWithin early exit
- ✅ **Neighbor filtering** - Closed set check prevents revisiting
- ✅ **Informative output** - Full atom details with spatial metadata
- ✅ **Error handling** - Validation and RAISERROR
- ✅ **Graceful failure** - Returns NULL path if no route found

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **Fixed NeighborRadius** - Should be adaptive based on spatial density
- ⚠️ **No path cost output** - Final gCost not returned
- ⚠️ **Temp table for ClosedSet** - Table variable would work (smaller size)
- ⚠️ **No tie-breaking strategy** - Could add atom importance/quality metric

**Performance:**
- ✅ Spatial index usage ensures O(log N) neighbor searches
- ✅ MERGE is efficient for OpenSet updates
- ✅ Computed column eliminates redundant fCost calculations
- ⚠️ Recursive CTE could be slow for very long paths (unlikely in semantic space)

**Security:**
- ⚠️ No TenantId checks (could navigate into other tenant's semantic space)
- ✅ Input validation (NULL checks)

### Why This Is a GOLD STANDARD

1. **Correct algorithm** - Perfect A* implementation
2. **Spatial optimization** - Index hints and GEOMETRY operations
3. **Production-ready** - Error handling, validation, cleanup
4. **Elegant code** - MERGE, computed columns, recursive CTE
5. **Informative results** - Complete path metadata

This procedure demonstrates expert-level T-SQL and algorithm implementation.

### Improvement Recommendations
1. **Priority 1:** Add multi-tenancy (TenantId filtering)
2. **Priority 2:** Return final path cost (gCost at goal)
3. **Priority 3:** Add adaptive NeighborRadius (based on local density)
4. **Priority 4:** Add tie-breaking with atom quality metric
5. **Priority 5:** Consider table variable for ClosedSet (memory optimization)

---

## 3. sp_CreateWeightSnapshot

**Location:** `src/Hartonomous.Database/Procedures/Admin.WeightRollback.sql` (lines 162-220)  
**Type:** Stored Procedure (Admin)  
**Lines:** ~60  
**Quality Score:** 80/100

### Purpose
Create named snapshot of model weights for backup and restore. Leverages temporal table (`TensorAtomCoefficients_History`) for point-in-time recovery.

### Parameters
- `@SnapshotName NVARCHAR(255)` - Unique snapshot name
- `@ModelId INT = NULL` - Specific model (NULL = all models)
- `@Description NVARCHAR(MAX) = NULL` - Optional description

### Architecture

**Snapshot Creation:**
1. Create `WeightSnapshot` metadata table if not exists
2. Check for duplicate snapshot name
3. Count weights being snapshotted (from `TensorAtomCoefficient`)
4. Insert snapshot metadata with current timestamp
5. Print summary and restore command

**Key Insight:** Snapshot is **metadata-only**. Actual weight history stored in `TensorAtomCoefficients_History` temporal table (system-versioned).

### Key Operations

**Weight Count:**
```sql
SELECT @WeightCount = COUNT(*)
FROM dbo.TensorAtomCoefficient tac
INNER JOIN dbo.TensorAtom ta ON tac.TensorAtomId = ta.TensorAtomId
WHERE @ModelId IS NULL OR ta.ModelId = @ModelId;
```

**Snapshot Metadata:**
```sql
INSERT INTO dbo.WeightSnapshot (SnapshotName, ModelId, SnapshotTime, Description, WeightCount)
VALUES (@SnapshotName, @ModelId, @SnapshotTime, @Description, @WeightCount);
```

### Dependencies
- Tables: `WeightSnapshot` (created if missing), `TensorAtomCoefficient`, `TensorAtom`, `TensorAtomCoefficients_History` (temporal table)
- Procedures: `sp_RestoreWeightSnapshot` (mentioned in output)

### Quality Assessment

**Strengths:**
- ✅ **Auto-creates metadata table** - Self-contained deployment
- ✅ **Duplicate prevention** - UNIQUE constraint and check
- ✅ **Flexible scope** - Single model or all models
- ✅ **Metadata tracking** - Name, time, count, description
- ✅ **User-friendly output** - Clear summary with restore command
- ✅ **Lightweight** - Metadata-only (temporal table stores actual data)

**Weaknesses:**
- ⚠️ **No multi-tenancy** - WeightSnapshot table missing TenantId
- ⚠️ **No authorization check** - Anyone can create snapshots
- ⚠️ **No snapshot quota** - Could fill metadata table
- ⚠️ **JOIN to TensorAtom** - Weight count should be per-model if using TensorAtomCoefficient.ModelId directly
- ⚠️ **No error handling** - Missing TRY/CATCH (RAISERROR only)
- ⚠️ **No transaction** - INSERT could fail leaving incomplete state

**Performance:**
- Weight count query could be slow for large models (full table scan)
- Should use index on TensorAtom.ModelId

**Security:**
- ⚠️ No authorization (any user can snapshot any model)
- ⚠️ No TenantId isolation

### Improvement Recommendations
1. **Priority 1:** Add TenantId to WeightSnapshot table and filter
2. **Priority 2:** Add authorization check (ensure user can access ModelId)
3. **Priority 3:** Add TRY/CATCH error handling
4. **Priority 4:** Wrap in transaction (BEGIN TRAN/COMMIT)
5. **Priority 5:** Add snapshot quota limit per tenant/model
6. **Priority 6:** Optimize weight count (use indexed view or cached value)

---

## 4. sp_RestoreWeightSnapshot

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_RestoreWeightSnapshot.sql`  
**Type:** Stored Procedure  
**Lines:** ~35  
**Quality Score:** 72/100

### Purpose
Restore weights from named snapshot. Wrapper around `sp_RollbackWeightsToTimestamp`.

### Parameters
- `@SnapshotName NVARCHAR(255)` - Snapshot to restore
- `@DryRun BIT = 1` - Preview mode (default safe)

### Architecture

**Simple Delegation:**
1. Lookup snapshot metadata (SnapshotTime, ModelId)
2. Validate snapshot exists
3. Print summary header
4. Delegate to `sp_RollbackWeightsToTimestamp` with SnapshotTime

### Key Operations

**Snapshot Lookup:**
```sql
SELECT @SnapshotTime = SnapshotTime, @ModelId = ModelId
FROM dbo.WeightSnapshot
WHERE SnapshotName = @SnapshotName;
```

**Delegation:**
```sql
EXEC dbo.sp_RollbackWeightsToTimestamp 
    @TargetDateTime = @SnapshotTime,
    @ModelId = @ModelId,
    @DryRun = @DryRun;
```

### Dependencies
- Tables: `WeightSnapshot`
- Procedures: `dbo.sp_RollbackWeightsToTimestamp` (see next)

### Quality Assessment

**Strengths:**
- ✅ **Wrapper pattern** - Clean abstraction over timestamp rollback
- ✅ **Safe default** - DryRun=1 prevents accidental restore
- ✅ **Simple logic** - Easy to understand and maintain
- ✅ **Validation** - Checks snapshot exists

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filter
- ⚠️ **No authorization check** - Anyone can restore any snapshot
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **Minimal output** - Just header, actual work logged in sp_RollbackWeightsToTimestamp
- ⚠️ **No snapshot validation** - Doesn't check if snapshot is still valid (weights may have changed)

**Performance:**
- Simple lookup, no issues

**Security:**
- ⚠️ No authorization
- ⚠️ No TenantId isolation

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering (ensure snapshot belongs to tenant)
2. **Priority 2:** Add authorization check
3. **Priority 3:** Add TRY/CATCH error handling
4. **Priority 4:** Add snapshot staleness check (warn if very old)
5. **Priority 5:** Add confirmation prompt for DryRun=0

---

## 5. sp_ListWeightSnapshots

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ListWeightSnapshots.sql`  
**Type:** Stored Procedure  
**Lines:** ~20  
**Quality Score:** 70/100

### Purpose
List all weight snapshots, ordered by time (most recent first).

### Parameters
None

### Architecture

**Simple Query:**
1. Check if WeightSnapshot table exists
2. Return all snapshots ordered by SnapshotTime DESC

### Key Operations

**Snapshot List:**
```sql
SELECT SnapshotId, SnapshotName, ModelId, SnapshotTime, WeightCount, Description, CreatedAt
FROM dbo.WeightSnapshot
ORDER BY SnapshotTime DESC;
```

### Dependencies
- Tables: `WeightSnapshot`

### Quality Assessment

**Strengths:**
- ✅ **Simple and clear** - Does one thing well
- ✅ **Table existence check** - Handles case where snapshots not yet created
- ✅ **Useful ordering** - Most recent first

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Shows all snapshots (cross-tenant leak)
- ⚠️ **No pagination** - Could return thousands of snapshots
- ⚠️ **No filtering** - No parameters for ModelId, date range, etc.
- ⚠️ **No authorization** - Anyone can see all snapshots
- ⚠️ **SELECT *** - Should use explicit columns (done correctly here, but common anti-pattern)

**Performance:**
- Could be slow without index on SnapshotTime
- No TOP or paging (unbounded result set)

**Security:**
- 🔴 **Cross-tenant data leak** - Shows all tenants' snapshots

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering (CRITICAL security issue)
2. **Priority 2:** Add pagination (@Top, @Skip parameters)
3. **Priority 3:** Add filtering (@ModelId, @StartDate, @EndDate)
4. **Priority 4:** Add authorization check
5. **Priority 5:** Add index on SnapshotTime

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~335  
**Average Quality:** 75.2/100

**Quality Distribution:**
- Excellent (85-100): 1 file (sp_GenerateOptimalPath 90⭐)
- Good (70-84): 3 files (sp_CreateWeightSnapshot 80, sp_RestoreWeightSnapshot 72, sp_ListWeightSnapshots 70)
- Fair (60-69): 1 file (sp_GenerateTextSpatial 64)

**Key Patterns:**
- **A* pathfinding** - sp_GenerateOptimalPath is gold standard spatial algorithm implementation
- **Snapshot pattern** - Weight snapshot procedures use metadata + temporal tables for point-in-time recovery
- **Missing dependencies** - sp_GenerateTextSpatial depends on missing sp_SpatialNextToken
- **Multi-tenancy gaps** - All 5 procedures missing TenantId filtering (security issue)

**Missing Objects Identified:**
- Procedures (1): `dbo.sp_SpatialNextToken` - Spatial next-token prediction
- Tables: WeightSnapshot missing TenantId column (schema gap)

**Security Issues:**
- 🔴 **sp_ListWeightSnapshots** - Cross-tenant data leak (shows all snapshots)
- ⚠️ All procedures missing authorization checks
- ⚠️ All procedures missing TenantId filtering

**Cross-References:**
- sp_RestoreWeightSnapshot → sp_RollbackWeightsToTimestamp
- sp_GenerateTextSpatial → sp_SpatialNextToken (missing)

**Critical Issues:**
1. Missing sp_SpatialNextToken blocks spatial generation
2. WeightSnapshot schema missing TenantId (multi-tenancy broken)
3. sp_ListWeightSnapshots has security vulnerability (cross-tenant leak)
4. No authorization checks in weight snapshot procedures

**Recommendations:**
1. Implement missing sp_SpatialNextToken procedure
2. Alter WeightSnapshot table to add TenantId column
3. Add TenantId filtering to all 5 procedures
4. Add authorization checks to weight snapshot procedures
5. Add pagination to sp_ListWeightSnapshots
