# HARTONOMOUS SQL DATABASE PROJECT - COMPREHENSIVE AUDIT PART 9
**Generated:** 2025-11-20 03:30:00  
**Continuation:** Parts 1-8 complete (68 files analyzed, 21.6%)  
**Focus:** Scalar functions, table-valued functions, remaining tables  

---

## PART 9: SCALAR FUNCTIONS & INFRASTRUCTURE TABLES

### FILES ANALYZED IN PART 9

1. **dbo.fn_SoftmaxTemperature** (Functions/) - Temperature-scaled softmax for token generation
2. **dbo.fn_SelectModelsForTask** (Functions/) - Multi-model selection with weights
3. **dbo.fn_CalculateComplexity** (Functions/) - Computational complexity estimation
4. **dbo.fn_DetermineSla** (Functions/) - SLA tier determination
5. **dbo.fn_EstimateResponseTime** (Functions/) - Response time prediction
6. **dbo.fn_BindAtomsToCentroid** (Functions/) - Spatial concept similarity
7. **dbo.fn_GetContextCentroid** (Functions/) - Centroid calculation for atom sets
8. **dbo.fn_CreateSpatialPoint** (Functions/) - GEOMETRY point creation
9. **dbo.fn_GetModelLayers** (Functions/) - Model layer query wrapper
10. **dbo.TensorAtomCoefficient** (Tables/) - Tensor weight storage with temporal versioning
11. **dbo.TensorAtomCoefficients_History** (Tables/) - Temporal history for tensor weights
12. **dbo.AutonomousComputeJobs** (Tables/) - Background compute job tracking
13. **dbo.SessionPaths** (Tables/) - User session path tracking
14. **dbo.ActQueue** (ServiceBroker/Queues/) - OODA Act queue
15. **dbo.HypothesizeQueue** (ServiceBroker/Queues/) - OODA Hypothesize queue

**Total This Part:** 15 files  
**Cumulative Total:** 83 of 315+ files (26.3%)

---

## 1. FUNCTION: dbo.fn_SoftmaxTemperature

**File:** `Functions/dbo.fn_SoftmaxTemperature.sql`  
**Lines:** 10  
**Purpose:** Temperature-scaled softmax calculation for token sampling  

**Quality Score: 92/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_SoftmaxTemperature(
    @logit FLOAT,
    @max_logit FLOAT,
    @temperature FLOAT
)
RETURNS FLOAT
AS
BEGIN
    RETURN EXP((@logit - @max_logit) / @temperature);
END;
```

**Dependencies:**
- None (pure scalar function)

**Used By:**
- ✅ `dbo.sp_SpatialNextToken` (verified Part 5)

### Issues Found

1. **⚠️ No Temperature Validation**
   - No check for `@temperature = 0` (division by zero)
   - No check for `@temperature < 0` (invalid)
   - **Impact:** MEDIUM - Runtime error possible

2. **⚠️ No Overflow Protection**
   - Large `(@logit - @max_logit) / @temperature` can overflow `EXP`
   - EXP overflow returns NULL in SQL Server
   - **Impact:** LOW - NULL propagates, breaks softmax normalization

3. **✅ EXCELLENT: Numerically Stable**
   - Uses `@logit - @max_logit` (prevents overflow in standard softmax)
   - Correct implementation of log-sum-exp trick

4. **✅ Good: Simple and Fast**
   - Single expression, no loops
   - Inlineable by query optimizer

### Recommendations

**Priority 1:**
- Add temperature validation:
  ```sql
  CREATE FUNCTION dbo.fn_SoftmaxTemperature(...)
  AS
  BEGIN
      IF @temperature <= 0 OR @temperature IS NULL
          RETURN NULL;  -- Invalid temperature
      
      DECLARE @scaled FLOAT = (@logit - @max_logit) / @temperature;
      
      -- Clamp to prevent EXP overflow (EXP(700) ≈ max FLOAT)
      IF @scaled > 700
          SET @scaled = 700;
      ELSE IF @scaled < -700
          SET @scaled = -700;
      
      RETURN EXP(@scaled);
  END;
  ```

---

## 2. FUNCTION: dbo.fn_SelectModelsForTask

**File:** `Functions/dbo.fn_SelectModelsForTask.sql`  
**Lines:** 130  
**Purpose:** Select and weight models based on task type, modalities, explicit IDs  

**Quality Score: 88/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_SelectModelsForTask
(
    @task_type NVARCHAR(50) = NULL,
    @model_ids NVARCHAR(MAX) = NULL,
    @weights_json NVARCHAR(MAX) = NULL,
    @required_modalities NVARCHAR(MAX) = NULL,
    @additional_model_types NVARCHAR(MAX) = NULL
)
RETURNS @models TABLE
(
    ModelId INT PRIMARY KEY,
    Weight FLOAT NOT NULL,
    ModelName NVARCHAR(200) NULL
)
```

**Dependencies:**
- ✅ `dbo.Model` table - EXISTS
- ✅ `dbo.ModelMetadata` table - EXISTS (verified Part 8)

**Used By:**
- Ensemble inference procedures
- API controllers for model selection

### Issues Found

1. **⚠️ Complex Logic (130 lines)**
   - Handles 5 different selection modes
   - Multiple OPENJSON queries
   - **Impact:** LOW - Correct but hard to maintain

2. **⚠️ No Index Hints**
   - Query against Model, ModelMetadata tables
   - No index optimization hints
   - **Impact:** LOW - Query optimizer should handle it

3. **⚠️ Weight Normalization Division by Zero**
   - `UPDATE @models SET Weight = Weight / @total;`
   - Checks `IF @total = 0` and resets, but after NULL check
   - **Impact:** LOW - Edge case handled

4. **✅ EXCELLENT: Defensive Programming**
   - Uses `TRY_CAST` for JSON parsing
   - Uses `NULLIF(LTRIM(RTRIM(...)))` for string sanitization
   - Uses `ISJSON` validation before `OPENJSON`
   - Handles empty result set gracefully

5. **✅ EXCELLENT: Flexible Model Selection**
   - Explicit model IDs (highest priority)
   - Task type matching (ModelType or SupportedTasks JSON)
   - Modality matching (SupportedModalities JSON)
   - Additional model types (fallback)
   - Weight override via JSON

6. **✅ Good: Weight Normalization**
   - Ensures weights sum to 1.0
   - Allows custom weight overrides

### Recommendations

**Priority 1 (Maintainability):**
- Consider splitting into smaller helper functions:
  ```sql
  -- fn_SelectModelsByIds(@model_ids)
  -- fn_SelectModelsByTask(@task_type)
  -- fn_SelectModelsByModality(@modalities)
  -- fn_NormalizeWeights(@models)
  ```

**Priority 2:**
- Add query optimization hints for common patterns:
  ```sql
  FROM dbo.Model m WITH (INDEX(IX_Model_ModelType))
  LEFT JOIN dbo.ModelMetadata md WITH (INDEX(IX_ModelMetadata_ModelId))
  ```

---

## 3. FUNCTION: dbo.fn_CalculateComplexity

**File:** `Functions/dbo.fn_CalculateComplexity.sql`  
**Lines:** 24  
**Purpose:** Estimate computational complexity based on input size and model type  

**Quality Score: 80/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION [dbo].[fn_CalculateComplexity](
    @inputSize INT,
    @modelType NVARCHAR(100)
)
RETURNS INT
AS
BEGIN
    DECLARE @complexity INT = 1;

    -- Base complexity from input size
    SET @complexity = @inputSize;

    -- Model-specific multipliers
    IF @modelType LIKE '%transformer%' OR @modelType LIKE '%bert%'
        SET @complexity = @complexity * 10;  -- O(n²) attention
    ELSE IF @modelType LIKE '%lstm%' OR @modelType LIKE '%gru%'
        SET @complexity = @complexity * 5;   -- O(n) recurrence
    ELSE IF @modelType LIKE '%cnn%' OR @modelType LIKE '%convolution%'
        SET @complexity = @complexity * 3;   -- O(n) convolution
    ELSE
        SET @complexity = @complexity * 2;   -- Default linear

    RETURN @complexity;
END;
```

**Dependencies:**
- None (pure scalar function)

**Used By:**
- SLA determination logic
- Response time estimation

### Issues Found

1. **⚠️ Oversimplified Complexity Model**
   - Transformers: Actually O(n²) for sequence length, but multiplier is constant 10
   - Doesn't account for model size (parameter count)
   - Doesn't account for hardware (GPU vs CPU)
   - **Impact:** MEDIUM - Inaccurate estimates for large models

2. **⚠️ String Pattern Matching**
   - Uses `LIKE '%transformer%'` - case-sensitive
   - No normalization (LOWER/UPPER)
   - **Impact:** LOW - May miss model types with different casing

3. **⚠️ No Input Validation**
   - No check for `@inputSize <= 0`
   - Returns 0 for zero input (should be error)
   - **Impact:** LOW - Garbage in, garbage out

4. **✅ Good: Comment Explains Big-O**
   - Documents O(n²), O(n) complexity
   - Clear intent

### Recommendations

**Priority 1:**
- Add input validation:
  ```sql
  IF @inputSize <= 0 OR @inputSize IS NULL
      RETURN NULL;  -- Invalid input
  ```

**Priority 2:**
- Improve complexity model:
  ```sql
  -- Account for sequence length squared for transformers
  IF @modelType LIKE '%transformer%' OR @modelType LIKE '%bert%'
      SET @complexity = (@inputSize * @inputSize) / 100;  -- O(n²) / scaling
  ELSE IF ...
  ```

**Priority 3:**
- Case-insensitive matching:
  ```sql
  DECLARE @normalizedType NVARCHAR(100) = LOWER(@modelType);
  IF @normalizedType LIKE '%transformer%' OR @normalizedType LIKE '%bert%'
  ```

---

## 4. FUNCTION: dbo.fn_DetermineSla

**File:** `Functions/dbo.fn_DetermineSla.sql`  
**Lines:** 20  
**Purpose:** Map complexity score to SLA tier (realtime/interactive/standard/batch)  

**Quality Score: 85/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION [dbo].[fn_DetermineSla](
    @complexity INT
)
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @sla NVARCHAR(20);

    IF @complexity < 1000
        SET @sla = 'realtime';      -- < 100ms
    ELSE IF @complexity < 10000
        SET @sla = 'interactive';   -- < 1s
    ELSE IF @complexity < 100000
        SET @sla = 'standard';      -- < 10s
    ELSE
        SET @sla = 'batch';         -- > 10s

    RETURN @sla;
END;
```

**Dependencies:**
- None (pure scalar function)

**Used By:**
- Response time estimation
- SLA tracking

### Issues Found

1. **⚠️ Hardcoded Thresholds**
   - 1000, 10000, 100000 are magic numbers
   - No configuration table
   - **Impact:** LOW - Difficult to tune without redeployment

2. **⚠️ No NULL Handling**
   - Returns NULL for NULL input
   - Could be explicit: `IF @complexity IS NULL RETURN 'unknown'`
   - **Impact:** LOW - NULL propagates

3. **✅ EXCELLENT: Clear SLA Tiers**
   - Comments document expected latency (<100ms, <1s, etc.)
   - Aligns with industry standard SLA buckets

4. **✅ Good: Simple and Fast**
   - Single IF/ELSE chain
   - No table lookups

### Recommendations

**Priority 1 (Configurability):**
- Create SLA configuration table:
  ```sql
  CREATE TABLE SlaThresholds (
      SlaTier NVARCHAR(20) PRIMARY KEY,
      MinComplexity INT,
      MaxComplexity INT,
      TargetLatencyMs INT
  );
  
  INSERT INTO SlaThresholds VALUES
      ('realtime', 0, 999, 100),
      ('interactive', 1000, 9999, 1000),
      ('standard', 10000, 99999, 10000),
      ('batch', 100000, 2147483647, 30000);
  
  CREATE FUNCTION fn_DetermineSla(@complexity INT)
  RETURNS NVARCHAR(20)
  AS
  BEGIN
      RETURN (
          SELECT TOP 1 SlaTier
          FROM SlaThresholds
          WHERE @complexity BETWEEN MinComplexity AND MaxComplexity
          ORDER BY MinComplexity
      );
  END;
  ```

---

## 5. FUNCTION: dbo.fn_EstimateResponseTime

**File:** `Functions/dbo.fn_EstimateResponseTime.sql`  
**Lines:** 25  
**Purpose:** Estimate response time in milliseconds based on complexity and SLA  

**Quality Score: 82/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION [dbo].[fn_EstimateResponseTime](
    @complexity INT,
    @sla NVARCHAR(20)
)
RETURNS INT
AS
BEGIN
    DECLARE @baseTime INT;

    -- Base time from SLA tier
    SET @baseTime = CASE @sla
        WHEN 'realtime' THEN 50
        WHEN 'interactive' THEN 500
        WHEN 'standard' THEN 5000
        WHEN 'batch' THEN 30000
        ELSE 10000
    END;

    -- Adjust for complexity (logarithmic scaling)
    DECLARE @adjustedTime INT = @baseTime + (@complexity / 100);

    RETURN @adjustedTime;
END;
```

**Dependencies:**
- None (pure scalar function)

**Used By:**
- InferenceRequest.EstimatedResponseTimeMs population

### Issues Found

1. **⚠️ Linear Complexity Adjustment**
   - Comment says "logarithmic scaling"
   - Implementation: `@baseTime + (@complexity / 100)` (LINEAR)
   - **Impact:** MEDIUM - Misleading comment, inaccurate for large complexity

2. **⚠️ Hardcoded Base Times**
   - Same issue as fn_DetermineSla (50, 500, 5000, 30000)
   - Should use SlaThresholds table
   - **Impact:** LOW - Difficult to tune

3. **⚠️ Division Truncation**
   - `@complexity / 100` is INT/INT = INT division
   - Loses precision for complexity < 100
   - **Impact:** LOW - Small complexities underestimated

4. **✅ Good: Default SLA Handling**
   - `ELSE 10000` for unknown SLA tiers

### Recommendations

**Priority 1 (Fix Comment or Implementation):**
- Either fix comment to say "linear" OR implement logarithmic:
  ```sql
  -- True logarithmic scaling
  DECLARE @adjustedTime INT = @baseTime + CAST(LOG10(@complexity + 1) * 1000 AS INT);
  ```

**Priority 2:**
- Use FLOAT division for precision:
  ```sql
  DECLARE @adjustedTime INT = @baseTime + CAST((@complexity / 100.0) AS INT);
  ```

**Priority 3:**
- Join SlaThresholds table instead of CASE statement

---

## 6. FUNCTION: dbo.fn_BindAtomsToCentroid

**File:** `Functions/dbo.fn_BindAtomsToCentroid.sql`  
**Lines:** 20  
**Purpose:** Find atoms within similarity threshold of concept centroid  

**Quality Score: 90/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_BindAtomsToCentroid(
    @concept_centroid GEOMETRY,
    @similarity_threshold FLOAT,
    @tenant_id INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        ae.AtomId,
        ae.AtomEmbeddingId,
        ae.SpatialKey,
        ae.SpatialKey.STDistance(@concept_centroid) AS DistanceFromCentroid,
        a.CanonicalText,
        a.Modality
    FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
    WHERE ae.TenantId = @tenant_id
      AND ae.SpatialKey.STDistance(@concept_centroid) <= @similarity_threshold
);
```

**Dependencies:**
- ✅ `dbo.AtomEmbedding` table - EXISTS
- ✅ `dbo.Atom` table - EXISTS

**Used By:**
- Concept discovery
- Semantic search

### Issues Found

1. **⚠️ Inefficient Spatial Query**
   - Uses `STDistance` in WHERE clause
   - Better: Use `STWithin` with buffered centroid
   - **Impact:** MEDIUM - Slower than spatial index optimal query

2. **⚠️ No Result Limit**
   - Could return millions of atoms if threshold too high
   - **Impact:** LOW - Caller should use TOP

3. **✅ EXCELLENT: Multi-Tenancy**
   - Filters by `TenantId`
   - Prevents cross-tenant data leakage

4. **✅ EXCELLENT: Inline TVF**
   - Returns TABLE (inline TVF)
   - Full query optimizer integration
   - Can be composed with other queries

5. **✅ Good: Distance Calculation**
   - Returns `DistanceFromCentroid` for ranking
   - Useful for threshold tuning

### Recommendations

**Priority 1 (Performance):**
- Use spatial index-friendly query:
  ```sql
  -- Create buffer around centroid, use STWithin
  DECLARE @buffer GEOMETRY = @concept_centroid.STBuffer(@similarity_threshold);
  
  SELECT ... FROM AtomEmbedding ae
  WHERE ae.TenantId = @tenant_id
    AND ae.SpatialKey.STWithin(@buffer) = 1  -- Uses spatial index
  ORDER BY ae.SpatialKey.STDistance(@concept_centroid);  -- Rank results
  ```

**Priority 2:**
- Add TOP parameter:
  ```sql
  CREATE FUNCTION fn_BindAtomsToCentroid(..., @max_results INT = 100)
  RETURNS TABLE AS RETURN (
      SELECT TOP (@max_results) ...
      ORDER BY DistanceFromCentroid
  );
  ```

---

## 7. FUNCTION: dbo.fn_GetContextCentroid

**File:** `Functions/dbo.fn_GetContextCentroid.sql`  
**Lines:** 14  
**Purpose:** Calculate spatial centroid for a set of atom embeddings  

**Quality Score: 85/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_GetContextCentroid(@atom_ids NVARCHAR(MAX))
RETURNS TABLE
AS
RETURN
(
    SELECT
        dbo.fn_CreateSpatialPoint(
            AVG(ae.SpatialKey.STX),
            AVG(ae.SpatialKey.STY),
            AVG(CAST(COALESCE(ae.SpatialKey.Z, 0) AS FLOAT))
        ) AS ContextCentroid,
        COUNT(*) AS AtomCount
    FROM dbo.AtomEmbedding ae
    WHERE ae.AtomId IN (SELECT CAST(value AS BIGINT) FROM STRING_SPLIT(@atom_ids, ','))
      AND ae.SpatialKey IS NOT NULL
);
```

**Dependencies:**
- ✅ `dbo.AtomEmbedding` table - EXISTS
- ✅ `dbo.fn_CreateSpatialPoint` function - EXISTS (analyzed below)

**Used By:**
- Context window centroid calculation
- Multi-atom concept aggregation

### Issues Found

1. **⚠️ No Multi-Tenancy**
   - Missing `@tenant_id` parameter
   - Cross-tenant centroid calculation possible
   - **Impact:** MEDIUM - Security/isolation issue

2. **⚠️ STRING_SPLIT Performance**
   - `WHERE ae.AtomId IN (SELECT ... FROM STRING_SPLIT(...))`
   - Creates implicit JOIN, slower than temp table
   - **Impact:** LOW - Fine for small atom sets (<1000)

3. **⚠️ Z Dimension Handling**
   - Uses `COALESCE(ae.SpatialKey.Z, 0)`
   - Assumes Z=0 for 2D points
   - **Impact:** LOW - Reasonable default

4. **✅ EXCELLENT: Inline TVF**
   - Returns TABLE for composability
   - Can be used in FROM clause

5. **✅ Good: NULL Filtering**
   - `WHERE ae.SpatialKey IS NOT NULL`
   - Avoids AVG issues with NULLs

### Recommendations

**Priority 1 (Multi-Tenancy):**
- Add tenant parameter:
  ```sql
  CREATE FUNCTION fn_GetContextCentroid(@atom_ids NVARCHAR(MAX), @tenant_id INT)
  RETURNS TABLE AS RETURN (
      SELECT ... FROM AtomEmbedding ae
      WHERE ae.AtomId IN (...)
        AND ae.TenantId = @tenant_id  -- Tenant isolation
        AND ae.SpatialKey IS NOT NULL
  );
  ```

**Priority 2 (Performance for large sets):**
- Use table parameter instead of CSV:
  ```sql
  CREATE TYPE AtomIdList AS TABLE (AtomId BIGINT PRIMARY KEY);
  
  CREATE FUNCTION fn_GetContextCentroid(@atom_ids AtomIdList READONLY, @tenant_id INT)
  RETURNS TABLE AS RETURN (
      SELECT ... FROM AtomEmbedding ae
      INNER JOIN @atom_ids ids ON ae.AtomId = ids.AtomId
      WHERE ae.TenantId = @tenant_id ...
  );
  ```

---

## 8. FUNCTION: dbo.fn_CreateSpatialPoint

**File:** `Functions/dbo.fn_CreateSpatialPoint.sql`  
**Lines:** 14  
**Purpose:** Create GEOMETRY point from X, Y, Z coordinates  

**Quality Score: 88/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_CreateSpatialPoint(
    @x FLOAT,
    @y FLOAT,
    @z FLOAT = NULL
)
RETURNS GEOMETRY
AS
BEGIN
    DECLARE @result GEOMETRY;

    IF @z IS NULL
        SET @result = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ')', 0);
    ELSE
        SET @result = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ' ' + CAST(@z AS NVARCHAR(50)) + ')', 0);

    RETURN @result;
END;
```

**Dependencies:**
- None (uses built-in `geometry::STGeomFromText`)

**Used By:**
- ✅ `dbo.fn_GetContextCentroid`
- Many spatial queries

### Issues Found

1. **⚠️ String Concatenation for WKT**
   - Uses `'POINT(' + CAST(@x AS NVARCHAR) + ...)`
   - Fragile, precision loss possible
   - Better: Use `geometry::Point(@x, @y, 0)` (SQL Server 2012+)
   - **Impact:** LOW - Works but suboptimal

2. **⚠️ No Input Validation**
   - No check for NULL X/Y (Z is optional)
   - No check for NaN, Infinity
   - **Impact:** LOW - Garbage in, garbage out

3. **⚠️ Fixed SRID = 0**
   - Hardcoded spatial reference ID = 0 (Cartesian)
   - Should be parameterizable
   - **Impact:** LOW - Embedding space is Cartesian anyway

4. **✅ Good: Handles 2D and 3D**
   - Optional Z parameter
   - Creates 2D point if Z is NULL

### Recommendations

**Priority 1 (Modernize):**
- Use `geometry::Point` constructor:
  ```sql
  CREATE FUNCTION fn_CreateSpatialPoint(@x FLOAT, @y FLOAT, @z FLOAT = NULL)
  RETURNS GEOMETRY
  AS
  BEGIN
      IF @x IS NULL OR @y IS NULL
          RETURN NULL;
      
      IF @z IS NULL
          RETURN geometry::Point(@x, @y, 0);  -- SRID = 0
      ELSE
          RETURN geometry::Point(@x, @y, 0).STAddPoint(geometry::Point(0, 0, @z, 0));
          -- Note: SQL Server GEOMETRY doesn't natively support 3D, use geography or custom
  END;
  ```

**Priority 2:**
- Add validation for NaN/Infinity:
  ```sql
  IF @x <> @x OR @y <> @y OR @z <> @z  -- NaN check
      RETURN NULL;
  ```

---

## 9. FUNCTION: dbo.fn_GetModelLayers

**File:** `Functions/dbo.fn_GetModelLayers.sql`  
**Lines:** 20  
**Purpose:** Wrapper for vw_ModelLayersWithStats, filters by ModelId  

**Quality Score: 90/100** ✅

### Schema Analysis

```sql
CREATE FUNCTION dbo.fn_GetModelLayers(
    @ModelId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        LayerId,
        LayerIdx,
        LayerName,
        LayerType,
        ParameterCount,
        TensorShape,
        TensorDtype,
        CacheHitRate,
        AvgComputeTimeMs,
        TensorAtomCount,
        AvgImportance
    FROM dbo.vw_ModelLayersWithStats
    WHERE ModelId = @ModelId
);
```

**Dependencies:**
- ✅ `dbo.vw_ModelLayersWithStats` view - EXISTS (verified Part 8)

**Used By:**
- ModelsController.GetModelLayers

### Issues Found

1. **⚠️ View Has Correlated Subqueries**
   - vw_ModelLayersWithStats uses correlated subqueries (Part 8 finding)
   - Can't be materialized as indexed view
   - **Impact:** MEDIUM - Performance not optimal

2. **✅ PERFECT: Inline TVF**
   - Returns TABLE (not multi-statement TVF)
   - Full query optimizer integration
   - Can be inlined with caller query

3. **✅ Good: API Abstraction**
   - Decouples controller from view schema
   - Can swap view implementation without changing API

### Recommendations

**Priority 1:**
- Fix vw_ModelLayersWithStats (Part 8 recommendation):
  - Rewrite with LEFT JOIN + GROUP BY instead of correlated subqueries
  - Enable indexed view for materialization

---

## 10-11. TABLES: TensorAtomCoefficient & History

### TABLE: dbo.TensorAtomCoefficient

**File:** `Tables/dbo.TensorAtomCoefficient.sql`  
**Lines:** 55  
**Purpose:** Store tensor weights as atoms with 3D positional indexing  

**Quality Score: 78/100** ✅

### Schema Analysis

```sql
CREATE TABLE [dbo].[TensorAtomCoefficient] (
    [TensorAtomId]    BIGINT         NOT NULL,  -- FK to dbo.Atom (the float value)
    [ModelId]         INT            NOT NULL,
    [LayerIdx]        INT            NOT NULL,
    [PositionX]       INT            NOT NULL,  -- Row
    [PositionY]       INT            NOT NULL,  -- Column  
    [PositionZ]       INT            NOT NULL DEFAULT 0,
    
    -- Computed spatial key
    [SpatialKey]      AS (geometry::Point([PositionX], [PositionY], 0)) PERSISTED,
    
    -- DEPRECATED COLUMNS
    [TensorAtomCoefficientId] BIGINT  NULL,
    [ParentLayerId]           BIGINT  NULL,
    [TensorRole]              NVARCHAR(128) NULL,
    [Coefficient]             REAL    NULL,
    
    -- Temporal columns
    [ValidFrom]       DATETIME2(7)   GENERATED ALWAYS AS ROW START NOT NULL,
    [ValidTo]         DATETIME2(7)   GENERATED ALWAYS AS ROW END NOT NULL,
    
    CONSTRAINT [PK_TensorAtomCoefficients] PRIMARY KEY CLUSTERED 
        ([TensorAtomId], [ModelId], [LayerIdx], [PositionX], [PositionY], [PositionZ]),
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),
    
    CONSTRAINT [FK_TensorAtomCoefficients_Atom] FOREIGN KEY ([TensorAtomId]) 
        REFERENCES [dbo].[Atom] ([AtomId]),
    CONSTRAINT [FK_TensorAtomCoefficients_Model] FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Model] ([ModelId]) ON DELETE CASCADE
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[TensorAtomCoefficients_History]));
```

**Dependencies:**
- ✅ `dbo.Atom` table - EXISTS
- ✅ `dbo.Model` table - EXISTS
- ✅ `dbo.TensorAtomCoefficients_History` table - EXISTS (analyzed below)

### Issues Found

1. **⚠️ Deprecated Columns in Production Table**
   - `TensorAtomCoefficientId`, `ParentLayerId`, `TensorRole`, `Coefficient` marked DEPRECATED
   - Comments say "for backward compatibility during migration"
   - **Impact:** MEDIUM - Schema bloat, confusing to developers

2. **⚠️ Composite Primary Key (6 columns)**
   - PK on `(TensorAtomId, ModelId, LayerIdx, PositionX, PositionY, PositionZ)`
   - Very wide key, slower index lookups
   - **Impact:** LOW - Necessary for uniqueness, but consider surrogate key

3. **⚠️ SpatialKey Only Uses X, Y (Not Z)**
   - `geometry::Point([PositionX], [PositionY], 0)`
   - Ignores PositionZ dimension
   - **Impact:** MEDIUM - Loses 3D spatial information

4. **✅ EXCELLENT: Temporal Versioning**
   - `SYSTEM_VERSIONING = ON`
   - Full temporal history tracking
   - Can query historical tensor states

5. **✅ EXCELLENT: Columnstore Index**
   - `NCCI_TensorAtomCoefficients` nonclustered columnstore
   - Perfect for OLAP queries (analytical workloads)

6. **✅ EXCELLENT: Spatial Index**
   - `SIX_TensorAtomCoefficients_SpatialKey`
   - Supports fast spatial queries

7. **✅ Good: Atomic Pattern**
   - Weight IS the atom (`TensorAtomId → Atom.AtomicValue`)
   - Correct modality pattern

### Recommendations

**Priority 1 (Remove Deprecated Columns):**
- After migration complete, drop deprecated columns:
  ```sql
  -- Turn off versioning temporarily
  ALTER TABLE TensorAtomCoefficient SET (SYSTEM_VERSIONING = OFF);
  
  ALTER TABLE TensorAtomCoefficient DROP COLUMN TensorAtomCoefficientId;
  ALTER TABLE TensorAtomCoefficient DROP COLUMN ParentLayerId;
  ALTER TABLE TensorAtomCoefficient DROP COLUMN TensorRole;
  ALTER TABLE TensorAtomCoefficient DROP COLUMN Coefficient;
  
  -- Re-enable versioning
  ALTER TABLE TensorAtomCoefficient SET (SYSTEM_VERSIONING = ON);
  ```

**Priority 2 (Fix 3D Spatial Key):**
- Include Z dimension in spatial key:
  ```sql
  -- SQL Server GEOMETRY doesn't support true 3D
  -- Options:
  -- 1. Use geography (limited 3D support)
  -- 2. Use M dimension for Z: geometry::Point(X, Y, 0).STPointN(1).STSetM(Z)
  -- 3. Use compound key: (X, Y, Z) separately
  ```

**Priority 3:**
- Consider adding surrogate key for FK references:
  ```sql
  ALTER TABLE TensorAtomCoefficient ADD TensorCoefficientId BIGINT IDENTITY;
  CREATE UNIQUE INDEX UX_TensorCoefficient_Id ON TensorAtomCoefficient(TensorCoefficientId);
  ```

---

### TABLE: dbo.TensorAtomCoefficients_History

**File:** `Tables/dbo.TensorAtomCoefficients_History.sql`  
**Lines:** 18  
**Purpose:** Temporal history table for TensorAtomCoefficient  

**Quality Score: 90/100** ✅

### Schema Analysis

```sql
CREATE TABLE [dbo].[TensorAtomCoefficients_History] (
    [TensorAtomId]    BIGINT         NOT NULL,
    [ModelId]         INT            NOT NULL,
    [LayerIdx]        INT            NOT NULL,
    [PositionX]       INT            NOT NULL,
    [PositionY]       INT            NOT NULL,
    [PositionZ]       INT            NOT NULL,
    [SpatialKey]      GEOMETRY       NULL,
    [TensorAtomCoefficientId] BIGINT  NULL,
    [ParentLayerId]           BIGINT  NULL,
    [TensorRole]              NVARCHAR(128) NULL,
    [Coefficient]             REAL    NULL,
    [ValidFrom]       DATETIME2(7)   NOT NULL,
    [ValidTo]         DATETIME2(7)   NOT NULL
);

CREATE NONCLUSTERED INDEX [IX_TensorAtomCoefficients_History_Period]
    ON [dbo].[TensorAtomCoefficients_History]([ValidTo] ASC, [ValidFrom] ASC);
```

**Dependencies:**
- ✅ Required by `TensorAtomCoefficient` SYSTEM_VERSIONING

### Issues Found

1. **⚠️ No Clustered Index**
   - Only nonclustered index on ValidTo, ValidFrom
   - History table grows unbounded, heap fragmentation
   - **Impact:** MEDIUM - Slow temporal queries over time

2. **⚠️ Cannot Use Columnstore (GEOMETRY Column)**
   - Comment: "cannot use columnstore with GEOMETRY"
   - GEOMETRY incompatible with clustered columnstore
   - **Impact:** LOW - Temporal queries slower than optimal

3. **✅ EXCELLENT: Temporal Index**
   - `IX_TensorAtomCoefficients_History_Period` on ValidTo, ValidFrom
   - Supports `FOR SYSTEM_TIME AS OF`, `BETWEEN` queries

4. **✅ Good: Matches Main Table Schema**
   - All columns from main table present
   - Includes deprecated columns for historical accuracy

### Recommendations

**Priority 1 (Add Clustered Index):**
- Add clustered index for performance:
  ```sql
  CREATE CLUSTERED INDEX IX_TensorAtomCoefficients_History_Clustered
  ON TensorAtomCoefficients_History(ValidTo, ValidFrom);
  ```

**Priority 2 (Partition for Large History):**
- Consider partitioning by ValidTo for archival:
  ```sql
  -- Partition by year
  CREATE PARTITION FUNCTION PF_TensorHistory_Year(DATETIME2)
  AS RANGE RIGHT FOR VALUES ('2024-01-01', '2025-01-01', '2026-01-01');
  
  CREATE PARTITION SCHEME PS_TensorHistory_Year
  AS PARTITION PF_TensorHistory_Year TO ([PRIMARY], [PRIMARY], [PRIMARY], [ARCHIVE]);
  ```

---

## 12. TABLE: dbo.AutonomousComputeJobs

**File:** `Tables/dbo.AutonomousComputeJobs.sql`  
**Lines:** 30  
**Purpose:** Track background compute jobs for autonomous operations  

**Quality Score: 85/100** ✅

### Schema Analysis

```sql
CREATE TABLE [dbo].[AutonomousComputeJobs] (
    [JobId]            BIGINT        NOT NULL IDENTITY,
    [JobType]          NVARCHAR(100) NOT NULL,
    [Status]           NVARCHAR(50)  NOT NULL DEFAULT 'Pending',
    [Priority]         INT           NOT NULL DEFAULT 5,
    [InputParameters]  JSON NULL,
    [OutputResults]    JSON NULL,
    [ErrorMessage]     NVARCHAR(MAX) NULL,
    [CreatedAt]        DATETIME2(7)  NOT NULL DEFAULT SYSUTCDATETIME(),
    [StartedAt]        DATETIME2(7)  NULL,
    [CompletedAt]      DATETIME2(7)  NULL,
    [RetryCount]       INT           NOT NULL DEFAULT 0,
    [MaxRetries]       INT           NOT NULL DEFAULT 3,
    [TenantId]         INT           NOT NULL DEFAULT 0,
    [CorrelationId]    NVARCHAR(128) NULL,
    CONSTRAINT [PK_AutonomousComputeJobs] PRIMARY KEY CLUSTERED ([JobId] ASC)
);
```

**Dependencies:**
- None

**Used By:**
- Background job processing
- OODA autonomous loop (Learn phase likely)

### Issues Found

1. **⚠️ No Indexes on Status, Priority**
   - Job queue queries: `WHERE Status = 'Pending' ORDER BY Priority`
   - No index to support this pattern
   - **Impact:** HIGH - Slow job queue polling

2. **⚠️ No Index on TenantId**
   - Multi-tenant job isolation queries need index
   - **Impact:** MEDIUM - Slow tenant filtering

3. **⚠️ No Index on CorrelationId**
   - Job tracking by correlation ID needs index
   - **Impact:** LOW - Occasional slow lookups

4. **✅ EXCELLENT: Retry Logic**
   - `RetryCount`, `MaxRetries` columns
   - Supports automatic retry on failure

5. **✅ EXCELLENT: Comprehensive Tracking**
   - CreatedAt, StartedAt, CompletedAt timestamps
   - Status, ErrorMessage for debugging
   - InputParameters, OutputResults JSON for flexibility

6. **✅ Good: Multi-Tenancy**
   - TenantId column for isolation

### Recommendations

**Priority 1 (Performance - Job Queue):**
- Add job queue index:
  ```sql
  CREATE INDEX IX_AutonomousComputeJobs_Queue
  ON AutonomousComputeJobs(Status, Priority DESC, CreatedAt)
  WHERE Status = 'Pending';  -- Filtered index
  ```

**Priority 2 (Multi-Tenancy):**
- Add tenant + status index:
  ```sql
  CREATE INDEX IX_AutonomousComputeJobs_Tenant_Status
  ON AutonomousComputeJobs(TenantId, Status, Priority DESC);
  ```

**Priority 3:**
- Add correlation ID index:
  ```sql
  CREATE INDEX IX_AutonomousComputeJobs_CorrelationId
  ON AutonomousComputeJobs(CorrelationId)
  WHERE CorrelationId IS NOT NULL;
  ```

---

## 13. TABLE: dbo.SessionPaths

**File:** `Tables/dbo.SessionPaths.sql`  
**Lines:** 25  
**Purpose:** Track user session navigation paths for analytics  

**Quality Score: 82/100** ✅

### Schema Analysis

```sql
CREATE TABLE [dbo].[SessionPaths] (
    [SessionPathId]   BIGINT        NOT NULL IDENTITY,
    [SessionId]       NVARCHAR(128) NOT NULL,
    [UserId]          NVARCHAR(128) NULL,
    [PathSequence]    JSON NOT NULL,  -- Array of page/action objects
    [StartTimestamp]  DATETIME2(7)  NOT NULL DEFAULT SYSUTCDATETIME(),
    [EndTimestamp]    DATETIME2(7)  NULL,
    [PathLength]      INT           NOT NULL DEFAULT 0,
    [ConversionEvent] NVARCHAR(200) NULL,
    [TenantId]        INT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_SessionPaths] PRIMARY KEY CLUSTERED ([SessionPathId] ASC)
);
```

**Dependencies:**
- None

**Used By:**
- Analytics queries
- User behavior analysis

### Issues Found

1. **⚠️ No Index on SessionId**
   - Session lookups by SessionId need index
   - **Impact:** HIGH - Slow session queries

2. **⚠️ No Index on TenantId**
   - Multi-tenant analytics queries need index
   - **Impact:** MEDIUM - Slow tenant filtering

3. **⚠️ No Index on StartTimestamp**
   - Time-series analytics queries need index
   - **Impact:** MEDIUM - Slow temporal queries

4. **⚠️ PathSequence JSON Not Validated**
   - No CHECK constraint for JSON schema
   - **Impact:** LOW - Bad JSON possible

5. **✅ EXCELLENT: PathLength Denormalization**
   - Stores path length for fast filtering
   - `WHERE PathLength > 5` (long sessions)

6. **✅ Good: Conversion Tracking**
   - `ConversionEvent` column for funnel analysis

### Recommendations

**Priority 1 (Performance):**
- Add critical indexes:
  ```sql
  CREATE UNIQUE INDEX IX_SessionPaths_SessionId
  ON SessionPaths(SessionId, StartTimestamp DESC);
  
  CREATE INDEX IX_SessionPaths_Tenant_Start
  ON SessionPaths(TenantId, StartTimestamp DESC);
  
  CREATE INDEX IX_SessionPaths_Conversion
  ON SessionPaths(ConversionEvent, StartTimestamp DESC)
  WHERE ConversionEvent IS NOT NULL;
  ```

**Priority 2 (JSON Validation):**
- Add JSON schema constraint:
  ```sql
  ALTER TABLE SessionPaths ADD CONSTRAINT CK_PathSequence_JSON
  CHECK (ISJSON(PathSequence) = 1);
  ```

---

## 14-15. SERVICE BROKER QUEUES (OODA Continued)

### QUEUE: dbo.ActQueue

**File:** `ServiceBroker/Queues/dbo.ActQueue.sql`  
**Lines:** 1  
**Quality Score: 85/100** ✅

```sql
CREATE QUEUE ActQueue WITH STATUS = ON;
```

**Assessment:** ✅ Basic queue correct, ⚠️ no activation shown (likely in ALTER statement)

---

### QUEUE: dbo.HypothesizeQueue

**File:** `ServiceBroker/Queues/dbo.HypothesizeQueue.sql`  
**Lines:** 1  
**Quality Score: 85/100** ✅

```sql
CREATE QUEUE HypothesizeQueue WITH STATUS = ON;
```

**Assessment:** ✅ Basic queue correct, ⚠️ no activation shown

**Notes:**
- Both queues match service definitions (verified Part 8)
- ActService → ActQueue ✅
- HypothesizeService → HypothesizeQueue ✅
- Need to verify activation procedures exist (sp_Act, sp_Hypothesize verified Part 3)

---

## SUMMARY & CUMULATIVE FINDINGS

### Files Analyzed

**Part 9 Total:** 15 files  
**Cumulative (Parts 1-9):** 83 of 315+ files (26.3%)

**Average Quality Score This Part:** 85.7/100  
**Cumulative Average (Parts 1-9):** 81.8/100

### Quality Distribution

| Score Range | Count | Files |
|-------------|-------|-------|
| 90-100 | 4 | fn_SoftmaxTemperature (92), fn_BindAtomsToCentroid (90), fn_GetModelLayers (90), TensorAtomCoefficients_History (90) |
| 85-89 | 5 | fn_SelectModelsForTask (88), fn_CreateSpatialPoint (88), fn_GetContextCentroid (85), fn_DetermineSla (85), AutonomousComputeJobs (85), ActQueue (85), HypothesizeQueue (85) |
| 80-84 | 3 | fn_CalculateComplexity (80), fn_EstimateResponseTime (82), SessionPaths (82) |
| 70-79 | 3 | TensorAtomCoefficient (78) |

### Critical Issues Found (BLOCKING)

**None in Part 9** - All functions/tables operational with performance/design recommendations

### High-Priority Issues (Performance Impact)

1. **AutonomousComputeJobs Missing Indexes (3 indexes needed)**
   - No index on (Status, Priority) for job queue polling
   - No index on TenantId for multi-tenant filtering
   - **Impact:** HIGH - Slow job queue, scales poorly

2. **SessionPaths Missing Indexes (3 indexes needed)**
   - No index on SessionId for session lookups
   - No index on TenantId, StartTimestamp for analytics
   - **Impact:** HIGH - Slow analytics queries

3. **fn_BindAtomsToCentroid Inefficient Spatial Query**
   - Uses `STDistance` in WHERE (not spatial index optimal)
   - Should use `STWithin` with buffered centroid
   - **Impact:** MEDIUM - Slower than optimal

4. **TensorAtomCoefficients_History No Clustered Index**
   - Heap storage, fragmentation over time
   - **Impact:** MEDIUM - Temporal queries degrade

### Architectural Findings

#### ✅ EXCELLENT PATTERNS OBSERVED

1. **Inline Table-Valued Functions (TVFs)**
   - fn_SelectModelsForTask, fn_BindAtomsToCentroid, fn_GetContextCentroid, fn_GetModelLayers
   - **Returns TABLE** (not multi-statement TVF)
   - Full query optimizer integration ✅
   - Can be composed/inlined with caller queries ✅

2. **Temporal Versioning**
   - TensorAtomCoefficient: `SYSTEM_VERSIONING = ON`
   - Full history tracking for tensor weights
   - Can query: `SELECT ... FOR SYSTEM_TIME AS OF '2025-01-01'`
   - **GOLD STANDARD for auditable ML weights**

3. **Columnstore + Spatial Indexes**
   - TensorAtomCoefficient has BOTH:
     - Nonclustered columnstore for OLAP
     - Spatial index for geometric queries
   - **Perfect for tensor analytics + spatial similarity**

4. **Multi-Tenancy Consistency**
   - AutonomousComputeJobs, SessionPaths: TenantId column ✅
   - fn_BindAtomsToCentroid: @tenant_id parameter ✅
   - fn_GetContextCentroid: ⚠️ MISSING tenant parameter

#### ⚠️ PATTERNS NEEDING ATTENTION

1. **Deprecated Columns in Production**
   - TensorAtomCoefficient: 4 DEPRECATED columns still present
   - Comments say "backward compatibility during migration"
   - **Migration should be complete, drop columns**

2. **Missing Function Validation**
   - fn_SoftmaxTemperature: No division by zero check (@temperature = 0)
   - fn_CalculateComplexity: No input validation (@inputSize <= 0)
   - fn_CreateSpatialPoint: No NaN/Infinity checks

3. **Hardcoded SLA Thresholds**
   - fn_DetermineSla: Magic numbers (1000, 10000, 100000)
   - fn_EstimateResponseTime: Magic numbers (50, 500, 5000, 30000)
   - **Should use SlaThresholds configuration table**

4. **Misleading Comments**
   - fn_EstimateResponseTime: Says "logarithmic scaling", implements linear
   - **Either fix comment or fix implementation**

### Missing Objects Update

**Confirmed EXIST (Part 5 unknowns resolved):**
- ✅ fn_SoftmaxTemperature (Part 5 referenced in sp_SpatialNextToken)
- ✅ fn_SelectModelsForTask (Part 5 referenced in ensemble procedures)
- ✅ TensorAtomCoefficients_History (Part 5 listed as missing)
- ✅ AutonomousComputeJobs (Part 5 listed as missing)
- ✅ SessionPaths (Part 5 listed as missing)

**Still MISSING (Part 5 findings):**
- ❌ fn_DecompressComponents (referenced by sp_FuseMultiModalStreams, sp_GenerateEventsFromStream)
- ❌ fn_GetComponentCount (referenced by sp_FuseMultiModalStreams, sp_OrchestrateSensorStream)
- ❌ fn_GetTimeWindow (referenced by sp_FuseMultiModalStreams, sp_OrchestrateSensorStream)
- ❌ fn_BinaryToFloat32 (recommended in Part 5)
- ❌ InferenceTracking table (Part 8 finding)
- ❌ sp_Learn (Part 8 finding - CRITICAL)
- ❌ sp_EvictCacheLRU (Part 8 finding)
- ❌ AtomProvenance table (Part 5 listed)
- ❌ 15+ CLR functions (unchanged)

### Schema Patterns Observed

**Scalar Function Patterns:**
1. **Pure Math:** fn_SoftmaxTemperature (single expression)
2. **Business Logic:** fn_CalculateComplexity, fn_DetermineSla, fn_EstimateResponseTime
3. **Spatial Helpers:** fn_CreateSpatialPoint (GEOMETRY construction)

**Table-Valued Function Patterns:**
1. **Inline TVF (Best):** fn_BindAtomsToCentroid, fn_GetContextCentroid, fn_GetModelLayers
2. **Multi-Statement TVF:** fn_SelectModelsForTask (complex logic, table variable)

**Table Patterns:**
1. **Temporal Versioning:** TensorAtomCoefficient (SYSTEM_VERSIONING)
2. **Job Queue:** AutonomousComputeJobs (Status, Priority, Retry logic)
3. **Analytics:** SessionPaths (PathSequence JSON, denormalized PathLength)

---

## RECOMMENDATIONS FOR NEXT STEPS

### Immediate Actions (This Week)

1. **Add Missing Indexes (HIGH PRIORITY)**
   - AutonomousComputeJobs: Status+Priority, TenantId, CorrelationId
   - SessionPaths: SessionId, TenantId+StartTimestamp, ConversionEvent
   - TensorAtomCoefficients_History: Clustered index on ValidTo, ValidFrom
   - **Impact:** 10-100x performance improvement for job queue, analytics

2. **Fix Function Validation**
   - fn_SoftmaxTemperature: Add temperature > 0 check
   - fn_CalculateComplexity: Add input size > 0 check
   - fn_CreateSpatialPoint: Add NaN/Infinity validation

3. **Fix Misleading Comment**
   - fn_EstimateResponseTime: Either fix comment ("linear") or implement logarithmic

### Short-Term (Next 2 Weeks)

4. **Create Missing Functions (sp_FuseMultiModalStreams blockers)**
   - fn_DecompressComponents (CRITICAL - 7 references)
   - fn_GetComponentCount (CRITICAL - 4 references)
   - fn_GetTimeWindow (CRITICAL - 2 references)
   - **Unblocks:** sp_FuseMultiModalStreams, sp_GenerateEventsFromStream, sp_OrchestrateSensorStream

5. **Clean Up TensorAtomCoefficient**
   - Drop 4 DEPRECATED columns (TensorAtomCoefficientId, ParentLayerId, TensorRole, Coefficient)
   - Fix 3D spatial key (include Z dimension or document limitation)

6. **Add Multi-Tenancy to fn_GetContextCentroid**
   - Add @tenant_id parameter
   - Filter AtomEmbedding by TenantId

### Medium-Term (Next 4 Weeks)

7. **Create SlaThresholds Configuration Table**
   - Externalize magic numbers from fn_DetermineSla, fn_EstimateResponseTime
   - Enable runtime SLA tuning without redeployment

8. **Optimize fn_BindAtomsToCentroid**
   - Use STWithin + buffer instead of STDistance in WHERE
   - Add TOP parameter for result limiting

9. **Continue Manual Audit (Parts 10-12)**
   - Focus: Remaining missing functions (fn_DecompressComponents, etc.)
   - Remaining tables (AtomProvenance, GenerationStream, etc.)
   - CLR function documentation
   - Goal: 100% catalog by Dec 1

10. **Implement sp_Learn (CRITICAL - OODA Phase 4)**
    - Complete autonomous loop
    - Add activation to LearnQueue

---

## CONTINUATION PLAN FOR PART 10

### Proposed Files for Part 10 (Target: 10-12 files)

1. **PRIORITY: Missing functions blocking procedures**
   - Search for fn_DecompressComponents implementation (or create stub)
   - Search for fn_GetComponentCount implementation (or create stub)
   - Search for fn_GetTimeWindow implementation (or create stub)

2. **Remaining tables**
   - GenerationStream (referenced in procedures)
   - AtomProvenance (Part 5 listed)
   - StreamFusionResults (Part 5 listed)

3. **Additional functions**
   - fn_NormalizeJSON (found in function search)
   - fn_HilbertFunctions (found in function search)
   - fn_GetModelPerformanceFiltered (found in function search)

4. **CLR function documentation** (if implementations exist)
   - clr_ExtractModelWeights
   - clr_ExtractImagePixels
   - clr_ExtractAudioFrames

**Target Lines:** 700-800  
**Target Quality:** Continue deep architectural analysis  
**Focus Areas:** Unblock sp_FuseMultiModalStreams, complete missing objects catalog

---

## ARCHITECTURAL LESSONS FROM PART 9

### What's Working Well ✅

1. **Inline TVFs Everywhere**
   - Query optimizer can inline these with caller queries
   - Much better than multi-statement TVFs or stored procedures for filtering
   - **Pattern to follow:** Always use inline TVFs for data access

2. **Temporal Versioning for ML Weights**
   - TensorAtomCoefficient tracks full history of model weight changes
   - Can query historical model states
   - **Critical for:** Model debugging, reproducibility, compliance

3. **Spatial + Columnstore Hybrid**
   - TensorAtomCoefficient has BOTH spatial and columnstore indexes
   - Perfect for: "Find weights near position (X,Y) with high importance"
   - **Unique capability:** Spatial OLAP on tensor weights

### What Needs Attention ⚠️

1. **Missing Validation in Scalar Functions**
   - No division by zero checks
   - No NaN/Infinity handling
   - **Risk:** Runtime errors propagate through query plans

2. **Hardcoded Business Logic**
   - SLA thresholds in functions (not configurable)
   - Complexity multipliers in fn_CalculateComplexity (not ML-based)
   - **Impact:** Requires code deployment to tune

3. **Index Gaps on Job Queue Tables**
   - AutonomousComputeJobs has no job queue index
   - SessionPaths has no analytics indexes
   - **Impact:** Will scale poorly under load

### Critical Path Forward

**To unblock sp_FuseMultiModalStreams (Part 4 finding - 58/100):**
1. Find or create fn_DecompressComponents
2. Find or create fn_GetComponentCount
3. Find or create fn_GetTimeWindow
4. Test sp_FuseMultiModalStreams end-to-end

**To optimize job processing:**
1. Add indexes to AutonomousComputeJobs
2. Add indexes to SessionPaths
3. Test job queue throughput

**To complete OODA loop:**
1. Implement sp_Learn (Part 8 finding)
2. Add LearnQueue activation
3. Test full autonomous loop

---

**END OF PART 9**

**Next:** SQL_AUDIT_PART10.md (Missing functions, remaining tables, CLR documentation)  
**Progress:** 83 of 315+ files (26.3%)  
**Estimated Completion:** 9-11 more parts (Parts 10-20)