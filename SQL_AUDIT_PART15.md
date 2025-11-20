# SQL Audit Part 15: Advanced Search & Model Scoring Procedures

## Overview
Part 15 analyzes 5 procedures: semantic filtered search, exact vector search, temporal search, cross-modal queries, and model scoring.

---

## 1. sp_SemanticFilteredSearch

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_SemanticFilteredSearch.sql`  
**Type:** Stored Procedure  
**Lines:** ~50  
**Quality Score:** 82/100

### Purpose
Vector search with semantic filtering (topic, sentiment, temporal relevance). Combines VECTOR_DISTANCE with SemanticFeatures metadata.

### Parameters
- `@query_vector VECTOR(1998)` - Query vector
- `@top_k INT = 10` - Result limit
- `@TenantId INT` - Multi-tenancy (V3)
- `@EmbeddingType NVARCHAR(50) = NULL` - Filter by embedding type (V3)
- `@topic_filter NVARCHAR(50) = NULL` - Topic filter (technical/business/scientific/creative)
- `@min_topic_score FLOAT = 0.5` - Minimum topic score
- `@min_sentiment FLOAT = NULL` - Minimum sentiment
- `@max_sentiment FLOAT = NULL` - Maximum sentiment
- `@min_temporal_relevance FLOAT = 0.0` - Minimum temporal relevance

### Architecture

**Hybrid Search:**
- Vector distance (cosine similarity) for semantic matching
- SemanticFeatures JOIN for metadata filtering
- Multi-tenancy via Atom.TenantId OR TenantAtoms table

**Topic Filtering Logic:**
```sql
(@topic_filter IS NULL OR
    (@topic_filter = 'technical' AND sf.TopicTechnical >= @min_topic_score) OR
    (@topic_filter = 'business' AND sf.TopicBusiness >= @min_topic_score) OR
    (@topic_filter = 'scientific' AND sf.TopicScientific >= @min_topic_score) OR
    (@topic_filter = 'creative' AND sf.TopicCreative >= @min_topic_score))
```

### Dependencies
- Tables: `AtomEmbedding`, `SemanticFeatures`, `Atom`, `TenantAtoms`
- Indexes: Vector index on AtomEmbedding.EmbeddingVector, IX_SemanticFeatures_AtomEmbeddingId

### Quality Assessment

**Strengths:**
- ✅ **V3 multi-tenancy** - Proper TenantId filtering with TenantAtoms fallback
- ✅ **Rich filtering** - Topic, sentiment, temporal relevance
- ✅ **VECTOR_DISTANCE native** - Uses SQL Server 2025 native vector ops
- ✅ **User feedback** - PRINT statements for debugging
- ✅ **Flexible topics** - 4 topic categories with threshold

**Weaknesses:**
- ⚠️ **Complex OR logic** - Topic filter has 4-way OR (hard to optimize)
- ⚠️ **Double VECTOR_DISTANCE** - Computed in SELECT and ORDER BY (inefficient)
- ⚠️ **TenantAtoms subquery** - EXISTS per row (performance issue)
- ⚠️ **No NULL vector check** - EmbeddingVector IS NOT NULL but no dimension validation
- ⚠️ **PRINT in procedure** - Debug output in production code

**Performance:**
- Vector distance computed twice per row (SELECT + ORDER BY)
- EXISTS subquery for TenantAtoms evaluated per row
- Topic OR logic prevents index usage on topic columns
- No query hint for vector index

**Security:**
- ✅ Multi-tenant safe with TenantId filtering
- ✅ Shared atoms via TenantAtoms

### Improvement Recommendations
1. **Priority 1:** Use CTE to compute VECTOR_DISTANCE once
2. **Priority 2:** Replace TenantAtoms EXISTS with JOIN
3. **Priority 3:** Add query hint for vector index
4. **Priority 4:** Validate vector dimension matches @query_vector
5. **Priority 5:** Remove PRINT statements or make conditional (@Debug parameter)
6. **Priority 6:** Add index on SemanticFeatures topic columns

---

## 2. sp_ExactVectorSearch

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ExactVectorSearch.sql`  
**Type:** Stored Procedure  
**Lines:** ~35  
**Quality Score:** 80/100

### Purpose
Pure vector search with configurable distance metric. Returns exact k-nearest neighbors.

### Parameters
- `@query_vector VECTOR(1998)` - Query vector
- `@top_k INT = 10` - Result limit
- `@TenantId INT` - Multi-tenancy (V3)
- `@distance_metric NVARCHAR(20) = 'cosine'` - Distance metric (cosine, euclidean, etc.)
- `@embedding_type NVARCHAR(128) = NULL` - Filter by embedding type
- `@ModelId INT = NULL` - Filter by model

### Architecture

**Clean Vector Search:**
- Native VECTOR_DISTANCE with configurable metric
- Multi-tenancy filtering
- Optional embedding type and model filtering
- Returns distance AND similarity (1.0 - distance)

### Key Operations

**Distance Calculation:**
```sql
VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS distance,
1.0 - VECTOR_DISTANCE(@distance_metric, ae.EmbeddingVector, @query_vector) AS similarity
```

### Dependencies
- Tables: `AtomEmbedding`, `Atom`, `TenantAtoms`
- Indexes: Vector index on AtomEmbedding.EmbeddingVector

### Quality Assessment

**Strengths:**
- ✅ **Configurable metric** - Supports cosine, euclidean, etc.
- ✅ **V3 multi-tenancy** - TenantId + TenantAtoms
- ✅ **Rich metadata** - Returns dimension, embedding type, model
- ✅ **Dual output** - Distance AND similarity
- ✅ **Clean code** - Simple, focused logic

**Weaknesses:**
- ⚠️ **Triple VECTOR_DISTANCE** - Computed 3 times (SELECT distance, SELECT similarity, ORDER BY)
- ⚠️ **TenantAtoms EXISTS** - Per-row subquery (performance issue)
- ⚠️ **No metric validation** - Accepts invalid metric strings
- ⚠️ **SpatialKey.STDimension()** - Returns spatial dimension (not vector dimension)
- ⚠️ **No vector dimension check** - Could mismatch query vector

**Performance:**
- VECTOR_DISTANCE computed 3 times per row (very inefficient)
- EXISTS subquery per row
- No query hint for vector index

**Security:**
- ✅ Multi-tenant safe

### Improvement Recommendations
1. **Priority 1:** Use CTE to compute VECTOR_DISTANCE once
2. **Priority 2:** Replace TenantAtoms EXISTS with JOIN
3. **Priority 3:** Validate distance_metric against allowed values
4. **Priority 4:** Fix dimension return (use ae.Dimension, not SpatialKey.STDimension())
5. **Priority 5:** Add vector dimension validation
6. **Priority 6:** Add query hint for vector index

---

## 3. sp_TemporalVectorSearch

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_TemporalVectorSearch.sql`  
**Type:** Stored Procedure  
**Lines:** ~45  
**Quality Score:** 85/100

### Purpose
Temporal table search with native VECTOR_DISTANCE. Searches historical atom states using SQL Server temporal tables (FOR SYSTEM_TIME).

### Parameters
- `@QueryVector VECTOR(1998)` - Query vector
- `@TopK INT = 10` - Result limit
- `@StartTime DATETIME2` - Temporal range start
- `@EndTime DATETIME2` - Temporal range end
- `@Modality VARCHAR(50) = NULL` - Filter by modality
- `@EmbeddingType VARCHAR(50) = NULL` - Filter by embedding type
- `@ModelId INT = NULL` - Filter by model
- `@Dimension INT = 1998` - Expected vector dimension

### Architecture

**Temporal Query:**
```sql
FROM dbo.AtomEmbedding ae
INNER JOIN dbo.Atom FOR SYSTEM_TIME FROM @StartTime TO @EndTime a 
    ON a.AtomId = ae.AtomId
```

**Key Features:**
- FOR SYSTEM_TIME temporal query on Atom table
- VECTOR_DISTANCE for similarity
- TemporalDistanceHours calculation (time from creation to end time)
- JSON output (FOR JSON PATH)

### Dependencies
- Tables: `AtomEmbedding`, `Atom` (system-versioned temporal table)
- Indexes: Vector index on AtomEmbedding.EmbeddingVector, temporal history index

### Quality Assessment

**Strengths:**
- ✅ **Temporal table support** - FOR SYSTEM_TIME FROM ... TO query
- ✅ **Dimension validation** - Checks ae.Dimension = @Dimension
- ✅ **Temporal distance** - DATEDIFF(HOUR, ...) for recency metric
- ✅ **JSON output** - FOR JSON PATH for API consumption
- ✅ **Rich filtering** - Modality, EmbeddingType, ModelId, time range
- ✅ **Good comments** - Explains CLR vs T-SQL vector distance tradeoffs

**Weaknesses:**
- ⚠️ **Double VECTOR_DISTANCE** - Computed in SELECT and ORDER BY
- ⚠️ **No multi-tenancy** - Missing TenantId filtering (security issue)
- ⚠️ **TemporalDistanceHours after CreatedAt filter** - Should filter on temporal distance directly
- ⚠️ **BETWEEN redundant** - ae.CreatedAt BETWEEN ... already filtered by temporal query
- ⚠️ **JSON output only** - No option for tabular results

**Performance:**
- VECTOR_DISTANCE computed twice per row
- Temporal table query may be slow without proper indexes
- BETWEEN filter redundant (temporal query already filters)

**Security:**
- 🔴 **Missing TenantId** - Cross-tenant temporal data leak

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering (CRITICAL security issue)
2. **Priority 2:** Use CTE to compute VECTOR_DISTANCE once
3. **Priority 3:** Remove redundant ae.CreatedAt BETWEEN filter
4. **Priority 4:** Add @OutputFormat parameter (JSON/Tabular)
5. **Priority 5:** Consider filtering by TemporalDistanceHours instead of raw dates

---

## 4. sp_CrossModalQuery

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_CrossModalQuery.sql`  
**Type:** Stored Procedure  
**Lines:** ~60  
**Quality Score:** 68/100

### Purpose
Cross-modal queries using spatial GEOMETRY queries OR text filtering. Searches across modalities (text, image, audio, etc.).

### Parameters
- `@text_query NVARCHAR(MAX) = NULL` - Text filter
- `@spatial_query_x FLOAT = NULL` - Spatial X coordinate
- `@spatial_query_y FLOAT = NULL` - Spatial Y coordinate
- `@spatial_query_z FLOAT = NULL` - Spatial Z coordinate (optional)
- `@modality_filter NVARCHAR(50) = NULL` - Filter by modality
- `@top_k INT = 10` - Result limit

### Architecture

**Two Query Paths:**

**Path 1: Spatial Query (if X/Y provided):**
- Build GEOMETRY point from X/Y/Z
- Use STDistance for spatial nearest neighbors
- Order by spatial distance

**Path 2: Random Fallback (if no spatial):**
- SELECT with text LIKE filter
- ORDER BY NEWID() (random)

### Key Operations

**Spatial Search:**
```sql
DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(@query_wkt, 0);
...
ORDER BY ae.SpatialKey.STDistance(@query_pt);
```

**Random Fallback:**
```sql
ORDER BY NEWID();
```

### Dependencies
- Tables: `AtomEmbedding`, `Atom`
- Indexes: Spatial index on AtomEmbedding.SpatialKey

### Quality Assessment

**Strengths:**
- ✅ **Cross-modal support** - Works across modality types
- ✅ **Spatial queries** - Uses GEOMETRY for semantic space
- ✅ **Flexible filtering** - Text + modality + spatial

**Weaknesses:**
- 🔴 **No multi-tenancy** - Missing TenantId (security issue)
- ⚠️ **Random fallback** - ORDER BY NEWID() is meaningless for search
- ⚠️ **Schema mismatch** - Uses SourceType/SourceUri (commented as removed)
- ⚠️ **AtomicValue LIKE** - CONVERT + LIKE on binary is inefficient
- ⚠️ **Inconsistent columns** - Different SELECT columns in two paths
- ⚠️ **No error handling** - Missing TRY/CATCH
- ⚠️ **PRINT statements** - Debug output in production
- ⚠️ **LEFT JOIN AtomEmbedding** - Could return atoms without embeddings

**Performance:**
- LIKE on CONVERT(AtomicValue) is full table scan
- ORDER BY NEWID() is extremely slow
- No index hint for spatial query

**Security:**
- 🔴 Missing TenantId filtering (cross-tenant leak)

### Improvement Recommendations
1. **Priority 1:** Add TenantId filtering (CRITICAL)
2. **Priority 2:** Remove ORDER BY NEWID() fallback (use meaningful default)
3. **Priority 3:** Fix schema references (SourceType/SourceUri)
4. **Priority 4:** Use CanonicalText for text search instead of AtomicValue
5. **Priority 5:** Align SELECT columns between two paths
6. **Priority 6:** Add error handling
7. **Priority 7:** Remove PRINT statements
8. **Priority 8:** Consider INNER JOIN AtomEmbedding (require embeddings)

---

## 5. sp_ScoreWithModel

**Location:** `src/Hartonomous.Database/Procedures/dbo.sp_ScoreWithModel.sql`  
**Type:** Stored Procedure  
**Lines:** ~70  
**Quality Score:** 70/100

### Purpose
Score atoms using a trained model. Placeholder for PREDICT/ML Services integration.

### Parameters
- `@ModelId INT` - Model to use for scoring
- `@InputAtomIds NVARCHAR(MAX)` - Comma-separated atom IDs
- `@OutputFormat NVARCHAR(50) = 'JSON'` - Output format
- `@TenantId INT = 0` - Multi-tenancy

### Architecture

**Scoring Flow:**
1. Parse comma-separated input atom IDs
2. Load model bytes and type from Model table
3. Prepare input features (embeddings) from AtomEmbedding
4. Execute PREDICT (placeholder - commented out)
5. Return mock predictions (Score=0.95, PredictedLabel='ClassA')

### Key Operations

**Input Parsing:**
```sql
INSERT INTO @InputAtoms
SELECT CAST(value AS BIGINT)
FROM STRING_SPLIT(@InputAtomIds, ',');
```

**Feature Preparation:**
```sql
INSERT INTO @InputFeatures
SELECT ae.AtomId, ae.EmbeddingVector
FROM dbo.AtomEmbedding ae
INNER JOIN @InputAtoms ia ON ae.AtomId = ia.AtomId
WHERE ae.TenantId = @TenantId;
```

**Placeholder Prediction:**
```sql
-- SELECT * FROM PREDICT(MODEL = @ModelBytes, DATA = @InputFeatures);
-- For now, return mock predictions
SELECT AtomId, 0.95 AS Score, 'ClassA' AS PredictedLabel
FROM @InputFeatures FOR JSON PATH;
```

### Dependencies
- Tables: `Model`, `AtomEmbedding`
- Functions: STRING_SPLIT
- External: ML Services PREDICT (not implemented)

### Quality Assessment

**Strengths:**
- ✅ **Multi-tenancy** - TenantId on Model and AtomEmbedding
- ✅ **Error handling** - TRY/CATCH with RAISERROR
- ✅ **Input parsing** - STRING_SPLIT for comma-separated IDs
- ✅ **JSON output** - FOR JSON PATH
- ✅ **Good structure** - Clear phases (parse, load, prepare, predict)

**Weaknesses:**
- 🔴 **Incomplete implementation** - PREDICT commented out, returns mock data
- ⚠️ **No SerializedModel column** - Model table uses different column name
- ⚠️ **OutputFormat parameter unused** - Always returns JSON
- ⚠️ **No input validation** - Missing @InputAtomIds NULL check
- ⚠️ **No model validation** - Doesn't check Model.IsActive
- ⚠️ **Missing features handling** - No error if atoms have no embeddings
- ⚠️ **Mock data misleading** - Returns fake 0.95 score (should error or return NULL)

**Performance:**
- STRING_SPLIT is efficient
- Table variables are appropriate for small result sets
- Actual PREDICT performance unknown

**Security:**
- ✅ Multi-tenant safe (filters by TenantId on both tables)
- ⚠️ No authorization check (any user can score with any model)

### Improvement Recommendations
1. **Priority 1:** Implement actual PREDICT integration (ML Services or ONNX)
2. **Priority 2:** Fix SerializedModel column reference (check actual schema)
3. **Priority 3:** Use OutputFormat parameter or remove it
4. **Priority 4:** Add input validation (NULL checks, empty string)
5. **Priority 5:** Validate Model.IsActive
6. **Priority 6:** Handle missing embeddings (error or skip)
7. **Priority 7:** Add authorization check (model access control)
8. **Priority 8:** Return NULL or error instead of mock data

---

## Summary Statistics

**Files Analyzed:** 5  
**Total Lines:** ~260  
**Average Quality:** 77.0/100

**Quality Distribution:**
- Excellent (85-100): 1 file (sp_TemporalVectorSearch 85)
- Good (70-84): 3 files (sp_SemanticFilteredSearch 82, sp_ExactVectorSearch 80, sp_ScoreWithModel 70)
- Fair (65-69): 1 file (sp_CrossModalQuery 68)

**Key Patterns:**
- **Vector distance inefficiency** - 3 procedures compute VECTOR_DISTANCE multiple times (sp_SemanticFilteredSearch, sp_ExactVectorSearch, sp_TemporalVectorSearch)
- **Multi-tenancy gaps** - 2 procedures missing TenantId (sp_TemporalVectorSearch, sp_CrossModalQuery)
- **TenantAtoms EXISTS** - 2 procedures use inefficient per-row subquery (sp_SemanticFilteredSearch, sp_ExactVectorSearch)
- **Incomplete implementations** - sp_ScoreWithModel returns mock data

**Security Issues:**
- 🔴 **sp_TemporalVectorSearch** - No TenantId filtering (temporal data leak)
- 🔴 **sp_CrossModalQuery** - No TenantId filtering (cross-tenant leak)

**Performance Issues:**
- VECTOR_DISTANCE computed 2-3 times per row in 3 procedures
- TenantAtoms EXISTS subquery inefficiency
- ORDER BY NEWID() in sp_CrossModalQuery

**Missing Objects:**
- None (all dependencies exist or acknowledged as placeholders)

**Critical Issues:**
1. 2 procedures missing multi-tenancy (security vulnerability)
2. VECTOR_DISTANCE redundant computation (3x slower than necessary)
3. sp_ScoreWithModel incomplete (returns mock data)
4. sp_CrossModalQuery ORDER BY NEWID() fallback (meaningless)

**Recommendations:**
1. Add TenantId filtering to sp_TemporalVectorSearch and sp_CrossModalQuery (Priority 1 - Security)
2. Refactor VECTOR_DISTANCE to use CTE (compute once) in 3 procedures (Priority 1 - Performance)
3. Replace TenantAtoms EXISTS with JOIN (Priority 2 - Performance)
4. Implement PREDICT in sp_ScoreWithModel (Priority 2 - Functionality)
5. Remove ORDER BY NEWID() fallback in sp_CrossModalQuery (Priority 3 - Correctness)
