# SQL Audit Part 16: Model Inference & Provenance Procedures

## Overview
Part 16 analyzes 5 procedures: transformer inference, attention inference, model comparison, and provenance management (export/link).

---

## 1. sp_TransformerStyleInference

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_TransformerStyleInference.sql`  
**Type:** Stored Procedure  
**Lines:** ~95  
**Quality Score:** 72/100

### Purpose
Simulates transformer-style inference with multi-layer processing. Each layer: multi-head attention → feed-forward network. Stores layer results.

### Parameters
- `@ProblemId UNIQUEIDENTIFIER` - Problem identifier
- `@InputSequence NVARCHAR(MAX)` - Atom IDs or text
- `@ModelId INT = 1` - Model to use
- `@Layers INT = 6` - Number of transformer layers
- `@AttentionHeads INT = 8` - Multi-head attention heads
- `@FeedForwardDim INT = 2048` - Feed-forward dimension
- `@Debug BIT = 0` - Debug output

### Architecture

**Transformer Pipeline (Per Layer):**
1. Multi-head attention via `sp_GenerateWithAttention`
2. Retrieve attention output from `AttentionGenerationLog`
3. Feed-forward network via `sp_GenerateText` (simplified proxy)
4. Store layer result
5. Use output as input for next layer

**Final Storage:**
- Store all layer results in `TransformerInferenceResults` as JSON
- Return layer-by-layer results

### Key Operations

**Layer Loop:**
```sql
WHILE @Layer <= @Layers
BEGIN
    EXEC dbo.sp_GenerateWithAttention
        @ModelId = @ModelId, @InputAtomIds = @CurrentInput, @ContextJson = @LayerContextJson,
        @MaxTokens = 1, @Temperature = 0.1, @TopK = 10, @TopP = 0.5, @AttentionHeads = @AttentionHeads;
    
    -- Get attention output
    SELECT TOP 1 @AttentionOutput = CAST(GeneratedAtomIds AS NVARCHAR(MAX))
    FROM dbo.AttentionGenerationLog WHERE ModelId = @ModelId AND CreatedAt >= @LayerStartTime
    ORDER BY CreatedAt DESC;
    
    -- Feed-forward (simplified)
    EXEC dbo.sp_GenerateText @prompt = @FeedForwardPrompt, @max_tokens = 25, 
         @temperature = 0.5, @GeneratedText = @FeedForwardOutput OUTPUT;
    
    SET @CurrentInput = @FeedForwardOutput; -- Layer output → next input
END
```

### Dependencies
- Tables: `AttentionGenerationLog`, `TransformerInferenceResults`
- Procedures: `sp_GenerateWithAttention` (analyzed Part 8), `sp_GenerateText` (analyzed Part 13)

### Quality Assessment

**Strengths:**
- ✅ **Innovative approach** - Simulates transformer architecture in T-SQL
- ✅ **Layer-by-layer tracking** - Stores intermediate results
- ✅ **Configurable** - Layers, heads, dimensions
- ✅ **Debug support** - Optional debug output
- ✅ **JSON storage** - Layer results in structured format

**Weaknesses:**
- ⚠️ **Simplified feed-forward** - Uses sp_GenerateText as proxy (not actual FFN)
- ⚠️ **No residual connections** - Real transformers have skip connections
- ⚠️ **No layer normalization** - Missing key transformer component
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **Race condition** - SELECT TOP 1 from AttentionGenerationLog by time (MUST get wrong record)
- ⚠️ **FeedForwardDim unused** - Parameter not used in logic
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **Hardcoded MaxTokens=1** - Attention limited to single token per layer

**Performance:**
- WHILE loop with EXEC is slow (N layer executions)
- TOP 1 ORDER BY CreatedAt MUST miss records in high concurrency
- No batching of layer operations

**Security:**
- ⚠️ No TenantId filtering on model or logs

### REQUIRED FIXES
1. **CRITICAL:** Add TenantId filtering throughout
2. **URGENT:** Capture sp_GenerateWithAttention output directly (avoid time-based SELECT)
3. **REQUIRED:** Implement actual feed-forward network (not text generation proxy)
4. **IMPLEMENT:** Add residual connections (input + output per layer)
5. **IMPLEMENT:** Add layer normalization
6. **IMPLEMENT:** Use FeedForwardDim parameter or remove it
7. **IMPLEMENT:** Add error handling (TRY/CATCH)
8. **IMPLEMENT:** IMPLEMENT exposing MaxTokens per layer

---

## 2. sp_AttentionInference

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_AttentionInference.sql`  
**Type:** Stored Procedure  
**Lines:** ~110  
**Quality Score:** 74/100

### Purpose
Multi-step attention-based reasoning. Iteratively generates reasoning steps, each using attention over accumulated context. Stores reasoning chain.

### Parameters
- `@ProblemId UNIQUEIDENTIFIER` - Problem identifier
- `@ContextAtoms NVARCHAR(MAX)` - Initial context atom IDs
- `@Query NVARCHAR(MAX)` - Query/problem to solve
- `@ModelId INT = 1` - Model to use
- `@MaxReasoningSteps INT = 10` - Maximum reasoning iterations
- `@AttentionHeads INT = 8` - Multi-head attention heads
- `@Debug BIT = 0` - Debug output

### Architecture

**Iterative Reasoning:**
1. Start with initial context atoms
2. For each step (up to MaxReasoningSteps):
   - Generate reasoning step via `sp_GenerateWithAttention`
   - Retrieve generation stream ID
   - Get generated atoms from `provenance.GenerationStreams`
   - Append to context for next step
   - Store step in table variable
3. Check convergence (short response = completion)
4. Store all reasoning steps in `AttentionInferenceResults` as JSON

### Key Operations

**Reasoning Loop:**
```sql
WHILE @StepNumber <= @MaxReasoningSteps
BEGIN
    EXEC dbo.sp_GenerateWithAttention
        @ModelId = @ModelId, @InputAtomIds = @CurrentContext, @ContextJson = @StepContextJson,
        @MaxTokens = 50, @Temperature = 0.8, @TopK = 40, @TopP = 0.85, @AttentionHeads = @AttentionHeads;
    
    SELECT TOP 1 @StepStreamId = GenerationStreamId
    FROM dbo.AttentionGenerationLog WHERE ModelId = @ModelId AND CreatedAt >= @StepStartTime
    ORDER BY CreatedAt DESC;
    
    SELECT @GeneratedAtoms = CAST(GeneratedAtomIds AS NVARCHAR(MAX))
    FROM provenance.GenerationStreams WHERE GenerationStreamId = @StepStreamId;
    
    SET @CurrentContext = @CurrentContext + ',' + @GeneratedAtoms; -- Accumulate context
    
    IF LEN(@GeneratedAtoms) < 10 BREAK; -- Convergence check
END
```

### Dependencies
- Tables: `AttentionGenerationLog`, `provenance.GenerationStreams`, `AttentionInferenceResults`
- Procedures: `sp_GenerateWithAttention`

### Quality Assessment

**Strengths:**
- ✅ **Reasoning chain** - Accumulates context across steps
- ✅ **Convergence detection** - Breaks on short response
- ✅ **Configurable** - MaxReasoningSteps, attention heads, temperature
- ✅ **Provenance tracking** - Stores all steps with generation streams
- ✅ **JSON storage** - Structured reasoning chain

**Weaknesses:**
- ⚠️ **Race condition** - Time-based SELECT from AttentionGenerationLog
- ⚠️ **Naive convergence** - LEN(@GeneratedAtoms) < 10 too simplistic
- ⚠️ **No multi-tenancy** - Missing TenantId filtering
- ⚠️ **No capture of sp_GenerateWithAttention return value** - Relies on log lookup
- ⚠️ **Context concatenation** - String concat MUST break atom ID list format
- ⚠️ **Hardcoded confidence** - 0.8 for all steps (MUST compute)
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **No query in context JSON** - Query string in JSON but not used for retrieval

**Performance:**
- WHILE loop with EXEC per step (slow for many steps)
- Time-based log lookup per step
- String concatenation for context (MUST be inefficient)

**Security:**
- ⚠️ No TenantId filtering

### REQUIRED FIXES
1. **CRITICAL:** Add TenantId filtering
2. **URGENT:** Capture generation stream ID directly from sp_GenerateWithAttention (avoid log lookup)
3. **REQUIRED:** Improve convergence detection (use semantic similarity or end-of-reasoning token)
4. **IMPLEMENT:** Compute actual confidence scores per step
5. **IMPLEMENT:** Add error handling
6. **IMPLEMENT:** Validate context concatenation (ensure proper CSV format)
7. **IMPLEMENT:** Use query in retrieval/ranking logic

---

## 3. sp_CompareModelKnowledge

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_CompareModelKnowledge.sql`  
**Type:** Stored Procedure  
**Lines:** ~80  
**Quality Score:** 78/100

### Purpose
Compare knowledge representation between two models. Analyzes tensor atoms, layers, and coefficients.

### Parameters
- `@ModelAId INT` - First model
- `@ModelBId INT` - Second model

### Architecture

**Three Comparisons:**

1. **Tensor Atom Stats:**
   - Count, avg importance, stdev, min/max per model

2. **Layer Comparison:**
   - FULL OUTER JOIN to show differences in layer structure
   - Compare layer types, parameter counts, tensor shapes

3. **Coefficient Coverage:**
   - Total coefficients, avg/max/min values per model

### Key Operations

**FULL OUTER JOIN for Layer Comparison:**
```sql
FROM dbo.ModelLayer AS a
FULL OUTER JOIN dbo.ModelLayer AS b
    ON a.LayerIdx = b.LayerIdx
   AND a.ModelId = @ModelAId
   AND b.ModelId = @ModelBId
WHERE a.ModelId = @ModelAId OR b.ModelId = @ModelBId
```

### Dependencies
- Tables: `TensorAtom`, `ModelLayer`, `TensorAtomCoefficient`

### Quality Assessment

**Strengths:**
- ✅ **Multi-dimensional comparison** - Atoms, layers, coefficients
- ✅ **Statistical analysis** - AVG, STDEV, MIN, MAX
- ✅ **FULL OUTER JOIN** - Handles layers present in only one model
- ✅ **Clear output** - Labeled analysis types
- ✅ **No side effects** - Read-only queries

**Weaknesses:**
- ⚠️ **No multi-tenancy** - Missing TenantId filtering (MUST compare across tenants)
- ⚠️ **Hardcoded column casts** - CAST(ImportanceScore AS FLOAT) implies wrong datatype
- ⚠️ **Coefficient join** - LEFT JOIN MUST miss coefficients
- ⚠️ **No model validation** - Doesn't check if models exist
- ⚠️ **UNION ALL** - MUST use single query with GROUPING SETS
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **PRINT statements** - Debug output in production

**Performance:**
- Multiple aggregations MUST be slow for large models
- No indexes specified
- LEFT JOIN to TensorAtomCoefficient MUST be expensive

**Security:**
- ⚠️ Cross-tenant model comparison possible (security issue)

### REQUIRED FIXES
1. **CRITICAL:** Add TenantId filtering (ensure models belong to same tenant)
2. **URGENT:** Validate models exist and are accessible
3. **REQUIRED:** Fix ImportanceScore datatype (shouldn't need CAST)
4. **IMPLEMENT:** Add error handling
5. **IMPLEMENT:** IMPLEMENT GROUPING SETS for cleaner aggregation
6. **IMPLEMENT:** Add indexes on ModelId, LayerIdx
7. **IMPLEMENT:** Remove PRINT statements or make conditional

---

## 4. sp_ExportProvenance

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ExportProvenance.sql`  
**Type:** Stored Procedure  
**Lines:** ~65  
**Quality Score:** 82/100

### Purpose
Export provenance lineage for an atom in multiple formats (JSON, GraphML). Uses recursive CTE to traverse provenance graph.

### Parameters
- `@AtomId BIGINT` - Root atom for provenance export
- `@Format NVARCHAR(20) = 'JSON'` - Output format (JSON, GraphML, CSV)
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Recursive CTE for Lineage:**
```sql
WITH Lineage AS (
    SELECT @AtomId AS AtomId, 0 AS Depth
    UNION ALL
    SELECT edge.FromAtomId AS AtomId, l.Depth + 1 AS Depth
    FROM Lineage l
    INNER JOIN provenance.AtomGraphEdges edge ON l.AtomId = edge.ToAtomId
    WHERE l.Depth < 50 -- Max depth 50
)
```

**Two Export Formats:**
1. **JSON:** Nested structure with parent atoms
2. **GraphML:** XML edge list

### Dependencies
- Tables: `provenance.AtomGraphEdges`, `Atom`

### Quality Assessment

**Strengths:**
- ✅ **Recursive CTE** - Proper graph traversal
- ✅ **Multi-format** - JSON and GraphML support
- ✅ **Depth limiting** - Max 50 levels prevents infinite loops
- ✅ **Multi-tenancy** - TenantId filtering
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Nested JSON** - Parents as subquery in JSON

**Weaknesses:**
- ⚠️ **CSV format unused** - Parameter accepts CSV but not implemented
- ⚠️ **GraphML simplified** - Not full GraphML spec (missing nodes, attributes)
- ⚠️ **ContentType column** - Atom table doesn't have ContentType (schema mismatch)
- ⚠️ **No cycle detection** - Recursive CTE MUST loop if graph has cycles
- ⚠️ **Depth = 50 hardcoded** - MUST be parameter
- ⚠️ **No authorization** - Any user can export any atom's provenance

**Performance:**
- Recursive CTE is efficient with proper indexes
- Depth limit prevents runaway queries
- Nested JSON subquery per row (MUST be slow)

**Security:**
- ✅ TenantId filtering present
- ⚠️ No authorization check (user access to atom)

### REQUIRED FIXES
1. **CRITICAL:** Fix ContentType column reference (use actual schema)
2. **URGENT:** Implement CSV format or remove from parameters
3. **REQUIRED:** Add cycle detection in CTE (track visited nodes)
4. **IMPLEMENT:** Make max depth a parameter
5. **IMPLEMENT:** Implement full GraphML spec (nodes + edges + attributes)
6. **IMPLEMENT:** Add authorization check
7. **IMPLEMENT:** Optimize nested JSON subquery (use single CTE)

---

## 5. sp_LinkProvenance

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_LinkProvenance.sql`  
**Type:** Stored Procedure  
**Lines:** ~50  
**Quality Score:** 84/100

### Purpose
Link parent atoms to child atom in provenance graph. Creates edges in `provenance.AtomGraphEdges`. Avoids duplicates.

### Parameters
- `@ParentAtomIds NVARCHAR(MAX)` - Comma-separated parent IDs
- `@ChildAtomId BIGINT` - Child atom
- `@DependencyType NVARCHAR(50) = 'DerivedFrom'` - Edge type
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Edge Creation:**
1. Parse comma-separated parent IDs
2. INSERT edges (parent → child) with duplicate check
3. Return count of edges created

### Key Operations

**Duplicate Prevention:**
```sql
WHERE NOT EXISTS (
    SELECT 1 FROM provenance.AtomGraphEdges edge
    WHERE edge.FromAtomId = pa.AtomId AND edge.ToAtomId = @ChildAtomId
);
```

### Dependencies
- Tables: `provenance.AtomGraphEdges`
- Functions: STRING_SPLIT

### Quality Assessment

**Strengths:**
- ✅ **Transaction** - BEGIN/COMMIT/ROLLBACK
- ✅ **Duplicate prevention** - NOT EXISTS check
- ✅ **Batch insert** - Single INSERT for all parents
- ✅ **Error handling** - TRY/CATCH with rollback
- ✅ **Multi-tenancy** - TenantId parameter
- ✅ **Feedback** - Returns edges created count
- ✅ **Clean code** - Simple, focused logic

**Weaknesses:**
- ⚠️ **No parent validation** - Doesn't check if parent atoms exist
- ⚠️ **No child validation** - Doesn't check if child atom exists
- ⚠️ **No TenantId validation** - Doesn't verify atoms belong to tenant
- ⚠️ **No cycle detection** - MUST create circular dependencies
- ⚠️ **DependencyType not validated** - Accepts any string

**Performance:**
- STRING_SPLIT is efficient
- NOT EXISTS is fast with index on FromAtomId, ToAtomId
- Batch INSERT is optimal

**Security:**
- ⚠️ No validation that atoms belong to TenantId (MUST link cross-tenant)
- ⚠️ No authorization check

### REQUIRED FIXES
1. **CRITICAL:** Validate parent and child atoms exist and belong to TenantId
2. **URGENT:** Add cycle detection (prevent A→B→C→A)
3. **REQUIRED:** Validate DependencyType against allowed values
4. **IMPLEMENT:** Add authorization check
5. **IMPLEMENT:** IMPLEMENT MERGE instead of INSERT with NOT EXISTS (simpler)

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~400  
**Average Quality:** 78.0/100

**Quality Distribution:**
- Excellent (85-100): 0 files
- Good (70-84): 5 files (sp_LinkProvenance 84, sp_ExportProvenance 82, sp_CompareModelKnowledge 78, sp_AttentionInference 74, sp_TransformerStyleInference 72)

**Key Patterns:**
- **Reasoning procedures** - sp_TransformerStyleInference and sp_AttentionInference simulate complex inference
- **Race conditions** - 2 procedures use time-based log lookups (sp_TransformerStyleInference, sp_AttentionInference)
- **Multi-tenancy gaps** - 3 procedures missing TenantId filtering (sp_TransformerStyleInference, sp_AttentionInference, sp_CompareModelKnowledge)
- **Provenance management** - sp_ExportProvenance and sp_LinkProvenance handle graph operations well

**Security Issues:**
- ⚠️ 3 procedures missing TenantId filtering (transformer, attention inference, model comparison)
- ⚠️ sp_LinkProvenance doesn't validate atom ownership

**Performance Issues:**
- Time-based log lookups (race conditions in concurrent scenarios)
- WHILE loops with EXEC (2 procedures)
- Nested JSON subqueries

**Missing Objects:**
- Table: TransformerInferenceResults (referenced by sp_TransformerStyleInference)
- Table: AttentionInferenceResults (referenced by sp_AttentionInference)

**Critical Issues:**
1. Race conditions in attention-based procedures (time-based lookups)
2. Multi-tenancy gaps in 3 procedures
3. Simplified transformer/feed-forward implementations (not production-ready)
4. Missing validation in sp_LinkProvenance (cross-tenant linking possible)

**Recommendations:**
1. Add TenantId filtering to all procedures (Priority 1 - Security)
2. Capture procedure outputs directly (avoid time-based lookups) (Priority 1 - Reliability)
3. Validate atom ownership in sp_LinkProvenance (Priority 1 - Security)
4. Implement actual feed-forward networks (Priority 2 - Functionality)
5. Add cycle detection to provenance operations (Priority 2 - Data integrity)
