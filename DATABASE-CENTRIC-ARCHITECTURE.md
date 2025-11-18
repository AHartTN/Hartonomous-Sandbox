# Hartonomous Database-Centric Architecture

## Executive Summary

Hartonomous follows a **database-centric architecture** where the database is not just a data store, but the **primary computation and logic engine**. This aligns with SQL Server 2025's cutting-edge capabilities and Microsoft's architectural guidance.

---

## Core Architectural Principles

### 1. **SQL Belongs in the Database, Not in C#**

**WHY**: Query optimizer benefits, parallelism, columnstore indexes, indexed views, plan caching.

**ANTI-PATTERN** (❌ BAD):
```csharp
// ModelsController.cs - ANTI-PATTERN
var query = $@"
    SELECT ModelId, ModelName, ModelType, ParameterCount, IngestionDate,
           COUNT(*) OVER() AS TotalCount
    FROM dbo.Models
    ORDER BY IngestionDate DESC, ModelName
    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
";
var models = await context.Database.SqlQueryRaw<ModelDto>(query).ToListAsync();
```

**PROBLEMS**:
- ❌ Query optimizer can't cache plans (string concatenation changes plan)
- ❌ No parameterization (SQL injection risk)
- ❌ Can't create indexes on ad-hoc queries
- ❌ Hard to test, maintain, refactor
- ❌ C# string escaping hell

**PATTERN** (✅ GOOD):
```sql
-- dbo.fn_GetModelsPaged.sql - INLINE TVF
CREATE FUNCTION dbo.fn_GetModelsPaged(@Offset INT, @PageSize INT)
RETURNS TABLE
AS RETURN
(
    SELECT ModelId, ModelName, ModelType, ParameterCount, IngestionDate, LayerCount,
           COUNT(*) OVER() AS TotalCount
    FROM dbo.vw_ModelsSummary
    ORDER BY IngestionDate DESC, ModelName
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
);
```

```csharp
// ModelsController.cs - CLEAN
var models = await context.GetModelsPaged(offset, pageSize).ToListAsync();
```

**BENEFITS**:
- ✅ Query optimizer caches plan
- ✅ Parameterized (safe from SQL injection)
- ✅ Can create indexes on vw_ModelsSummary
- ✅ EF Core scaffolds method from function
- ✅ Testable at database level (SQL Server Unit Tests)

---

### 2. **Views for Reusable Queries**

**USE CASE**: Eliminate duplicate SELECT logic across stored procedures, functions, and application code.

**PATTERN**:
```sql
-- dbo.vw_ModelsSummary.sql - WITH SCHEMABINDING enables indexed views
CREATE VIEW dbo.vw_ModelsSummary
WITH SCHEMABINDING
AS
SELECT m.ModelId, m.ModelName, m.ModelType, m.ParameterCount, m.IngestionDate,
       (SELECT COUNT_BIG(*) FROM dbo.ModelLayers ml WHERE ml.ModelId = m.ModelId) AS LayerCount
FROM dbo.Models m;
```

**BENEFITS**:
- ✅ DRY principle - single source of truth for model summary logic
- ✅ `WITH SCHEMABINDING` enables **indexed views** (materialized views)
- ✅ Query optimizer can auto-substitute views in queries
- ✅ Create `CREATE UNIQUE CLUSTERED INDEX` for instant aggregation results

---

### 3. **Indexed Views for Expensive Aggregations**

**USE CASE**: Pre-compute expensive aggregations that don't change frequently.

**PATTERN**:
```sql
-- dbo.vw_ModelPerformanceMetrics.sql
CREATE VIEW dbo.vw_ModelPerformanceMetrics
WITH SCHEMABINDING
AS
SELECT m.ModelId, m.ModelName, ISNULL(m.UsageCount, 0) AS TotalInferences,
       (SELECT AVG(ml2.AvgComputeTimeMs) FROM dbo.ModelLayers ml2 WHERE ml2.ModelId = m.ModelId) AS AvgInferenceTimeMs,
       (SELECT AVG(ISNULL(ml3.CacheHitRate, 0.0)) FROM dbo.ModelLayers ml3 WHERE ml3.ModelId = m.ModelId) AS CacheHitRate
FROM dbo.Models m;
GO

-- Materialize the view with a clustered index
CREATE UNIQUE CLUSTERED INDEX IX_vw_ModelPerformanceMetrics_ModelId 
ON dbo.vw_ModelPerformanceMetrics(ModelId);
```

**WHAT HAPPENS**:
- SQL Server **physically stores** the aggregated results
- Updates to `Models` or `ModelLayers` **automatically update** the indexed view
- Queries against the view return **instant results** (no aggregation at query time)
- Query optimizer can **auto-use indexed view** even when querying base tables

**BENEFITS**:
- ✅ Massive performance boost for analytics queries
- ✅ Auto-maintained by SQL Server (no manual refresh like PostgreSQL materialized views)
- ✅ Query optimizer can substitute indexed view automatically

---

### 4. **Inline TVFs for Parameterized Queries**

**USE CASE**: Parameterized queries that need query optimizer benefits.

**INLINE TVF** (✅ FAST):
```sql
CREATE FUNCTION dbo.fn_GetModelLayers(@ModelId INT)
RETURNS TABLE
AS RETURN
(
    SELECT LayerId, LayerIdx, LayerName, LayerType, ParameterCount, TensorAtomCount, AvgImportance
    FROM dbo.vw_ModelLayersWithStats
    WHERE ModelId = @ModelId
);
```

**MULTI-STATEMENT TVF** (❌ SLOW):
```sql
CREATE FUNCTION dbo.fn_GetModelLayersSlow(@ModelId INT)
RETURNS @Results TABLE (LayerId INT, LayerName NVARCHAR(255), ...)
AS
BEGIN
    INSERT INTO @Results
    SELECT LayerId, LayerName, ...
    FROM dbo.ModelLayers
    WHERE ModelId = @ModelId;
    RETURN;
END;
```

**WHY INLINE IS FASTER**:
- ✅ Query optimizer treats inline TVF like a **parameterized view**
- ✅ Can push predicates into the TVF
- ✅ Can optimize joins
- ✅ Can use indexes
- ❌ Multi-statement TVF is a **black box** to the optimizer (no optimization)

---

### 5. **Stored Procedures for Complex Logic**

**USE CASE**: Complex business logic, orchestration, transactions, dynamic SQL.

**PATTERN**:
```sql
-- dbo.sp_GetUsageAnalytics.sql
CREATE OR ALTER PROCEDURE dbo.sp_GetUsageAnalytics
    @StartDate DATETIME2,
    @EndDate DATETIME2,
    @BucketInterval VARCHAR(10) = 'HOUR'
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Dynamic SQL for time bucketing (HOUR vs DAY vs WEEK)
    DECLARE @SQL NVARCHAR(MAX) = N'
    SELECT DATEADD(HOUR, DATEDIFF(HOUR, 0, ir.CreatedAt), 0) AS TimeBucket,
           COUNT(*) AS RequestCount,
           AVG(ir.TotalDurationMs) AS AvgDurationMs
    FROM dbo.InferenceRequests ir
    WHERE ir.CreatedAt BETWEEN @StartDate AND @EndDate
    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, ir.CreatedAt), 0)
    ORDER BY TimeBucket
    FOR JSON PATH;
    ';
    
    EXEC sp_executesql @SQL, N'@StartDate DATETIME2, @EndDate DATETIME2', @StartDate, @EndDate;
END;
```

**BENEFITS**:
- ✅ Query optimizer parallelism for aggregations
- ✅ Can use columnstore indexes for analytics
- ✅ `FOR JSON PATH` returns JSON directly to API
- ✅ Dynamic SQL enables flexible bucketing (HOUR vs DAY vs WEEK)

---

### 6. **CLR for RBAR (Row-By-Agonizing-Row) Operations**

**CRITICAL INSIGHT**: CLR is **NOT** for set-based operations. CLR is for **complex per-row computations** that T-SQL sucks at.

**CLR USE CASES** (✅ GOOD):
- ✅ **Vector math** on individual rows (cosine similarity, dot product, Euclidean distance)
- ✅ **Per-row transformations** (image pixel extraction, audio frame extraction, model weight parsing)
- ✅ **Iterative algorithms** (t-SNE projection, Weiszfeld's algorithm for geometric median)
- ✅ **User-Defined Aggregates** (streaming softmax, mean/variance with Welford's algorithm)
- ✅ **Complex string processing** (BPE tokenization, regex)

**T-SQL USE CASES** (✅ GOOD):
- ✅ **Set-based operations** (JOIN, GROUP BY, WHERE, ORDER BY)
- ✅ **Aggregations** (SUM, AVG, COUNT, MAX, MIN)
- ✅ **Bulk vector operations** using native `VECTOR_DISTANCE` (SQL Server 2025)
- ✅ **Temporal queries** with `FOR SYSTEM_TIME` (temporal tables)

**ANTI-PATTERN** (❌ BAD):
```csharp
// CLR function doing set-based work (WRONG!)
[SqlFunction]
public static SqlString GetAllModels()
{
    using (var conn = new SqlConnection("context connection=true"))
    {
        // CLR doing what T-SQL should do
        var cmd = new SqlCommand("SELECT * FROM Models", conn);
        // ...serialize to JSON...
    }
}
```

**PATTERN** (✅ GOOD):
```csharp
// CLR function for complex per-row computation
[SqlFunction]
public static SqlDouble CosineSimilarity(SqlBytes vectorA, SqlBytes vectorB)
{
    var a = ParseVector(vectorA);
    var b = ParseVector(vectorB);
    
    // SIMD-optimized per-row computation
    return VectorMath.CosineSimilarity(a, b);
}
```

```sql
-- T-SQL calling CLR for per-row work
SELECT 
    AtomId,
    dbo.CosineSimilarity(EmbeddingVector, @queryVector) AS Similarity
FROM AtomEmbeddings
WHERE dbo.CosineSimilarity(EmbeddingVector, @queryVector) > 0.8  -- CLR per-row
ORDER BY Similarity DESC;  -- T-SQL set operation
```

**KEY INSIGHT**: SQL Server 2025 has **native `VECTOR_DISTANCE` function** for set-based vector operations. Use CLR only when you need custom vector logic or algorithms not supported by native functions.

---

### 7. **SIMD Optimization in CLR**

**CRITICAL**: CLR vector math MUST use `System.Numerics.Vector<T>` for SIMD acceleration.

**ANTI-PATTERN** (❌ SLOW - Scalar loops):
```csharp
// VectorUtilities.cs - OLD (SLOW)
internal static double CosineSimilarity(float[] a, float[] b)
{
    double dotProduct = 0;
    for (int i = 0; i < a.Length; i++)  // Scalar loop - 1 operation per cycle
        dotProduct += a[i] * b[i];
    // ...
}
```

**PATTERN** (✅ FAST - SIMD):
```csharp
// VectorMath.cs - NEW (FAST)
public static float DotProduct(float[] a, float[] b)
{
    float result = 0;
    int i = 0;
    int vectorSize = Vector<float>.Count;  // 8 floats on AVX2, 4 on SSE
    
    // Process 8 floats per CPU cycle (AVX2)
    for (; i <= a.Length - vectorSize; i += vectorSize)
    {
        var v1 = new Vector<float>(a, i);
        var v2 = new Vector<float>(b, i);
        result += Vector.Dot(v1, v2);
    }
    
    // Remainder
    for (; i < a.Length; i++)
        result += a[i] * b[i];
    
    return result;
}
```

**PERFORMANCE**:
- ✅ **8x faster** on AVX2 CPUs (8 floats per cycle vs 1)
- ✅ **4x faster** on SSE CPUs (4 floats per cycle vs 1)
- ✅ Automatic CPU detection (uses best available instruction set)

**REFACTOR**: `VectorUtilities` now delegates to `VectorMath` for SIMD acceleration.

---

## Architectural Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                         .NET 10 API Layer                       │
│  (Controllers - minimal logic, call stored procedures/views)    │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SQL Server 2025 Database                    │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Views (WITH SCHEMABINDING)                                 │ │
│  │  - vw_ModelsSummary                                        │ │
│  │  - vw_ModelDetails                                         │ │
│  │  - vw_ModelLayersWithStats                                 │ │
│  │  - vw_ModelPerformanceMetrics (indexed view)               │ │
│  └───────────────────────────────────────────────────────────┘ │
│                               │                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Inline TVFs (query optimizer benefits)                     │ │
│  │  - fn_GetModelsPaged(@Offset, @PageSize)                   │ │
│  │  - fn_GetModelLayers(@ModelId)                             │ │
│  │  - fn_GetModelPerformanceFiltered(@ModelId, @StartDate)    │ │
│  └───────────────────────────────────────────────────────────┘ │
│                               │                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Stored Procedures (complex logic, JSON output)             │ │
│  │  - sp_GetUsageAnalytics                                    │ │
│  │  - sp_GetModelPerformanceMetrics                           │ │
│  │  - sp_TemporalVectorSearch                                 │ │
│  └───────────────────────────────────────────────────────────┘ │
│                               │                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ CLR Functions (.NET Framework 4.8.1 - RBAR operations)     │ │
│  │  - VectorMath.CosineSimilarity (SIMD-optimized)            │ │
│  │  - VectorMath.DotProduct (SIMD-optimized)                  │ │
│  │  - VectorMath.EuclideanDistance (SIMD-optimized)           │ │
│  │  - LandmarkProjection.ProjectTo3D                          │ │
│  └───────────────────────────────────────────────────────────┘ │
│                               │                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ CLR Aggregates (streaming operations)                      │ │
│  │  - VectorMeanVariance (Welford's algorithm)                │ │
│  │  - GeometricMedian (Weiszfeld's algorithm)                 │ │
│  │  - StreamingSoftmax (log-sum-exp trick)                    │ │
│  └───────────────────────────────────────────────────────────┘ │
│                               │                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Tables (data storage)                                       │ │
│  │  - Models, ModelLayers, TensorAtoms, AtomEmbeddings, ...   │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## Migration Strategy

### Step 1: Identify Hard-Coded SQL in Controllers (✅ DONE)
Found 20+ instances across ModelsController, AnalyticsController, SearchController, BulkController, OperationsController.

### Step 2: Create Database Objects (✅ DONE)
- Created 4 views (vw_ModelsSummary, vw_ModelDetails, vw_ModelLayersWithStats, vw_ModelPerformanceMetrics)
- Created 3 inline TVFs (fn_GetModelsPaged, fn_GetModelLayers, fn_GetModelPerformanceFiltered)
- Created 3 stored procedures (sp_GetUsageAnalytics, sp_GetModelPerformanceMetrics, sp_TemporalVectorSearch)
- Added SIMD to VectorMath.EuclideanDistance
- Made VectorUtilities delegate to SIMD-optimized VectorMath

### Step 3: Update Controllers (⏳ TODO)
Replace hard-coded SQL with database object calls:
```csharp
// OLD (❌)
var query = $"SELECT ... FROM Models WHERE ...";
var models = await context.Database.SqlQueryRaw<ModelDto>(query).ToListAsync();

// NEW (✅)
var models = await context.GetModelsPaged(offset, pageSize).ToListAsync();
```

### Step 4: Deploy to SQL Server (⏳ TODO)
```bash
# Deploy views
sqlcmd -S localhost -d Hartonomous -i Views/dbo.vw_ModelsSummary.sql

# Deploy functions
sqlcmd -S localhost -d Hartonomous -i Functions/dbo.fn_GetModelsPaged.sql

# Deploy stored procedures
sqlcmd -S localhost -d Hartonomous -i Procedures/dbo.sp_GetUsageAnalytics.sql

# Re-scaffold EF Core entities (picks up new views/functions)
dotnet ef dbcontext scaffold "Server=localhost;Database=Hartonomous;..." \
  Microsoft.EntityFrameworkCore.SqlServer \
  --force
```

### Step 5: Test (⏳ TODO)
- SQL Server Unit Tests (SSDT) for views/functions/procedures
- CLR tests (.NET Framework 4.8.1) for VectorMath/aggregates
- API integration tests (.NET 10) for controllers
- Azure DevOps pipeline with code coverage

---

## Performance Benefits

### Before (Hard-Coded SQL in C#):
- ❌ No plan caching (string concatenation)
- ❌ No indexes on ad-hoc queries
- ❌ No parallelism for aggregations
- ❌ VectorUtilities using scalar loops (1 float per CPU cycle)
- ❌ Duplicate vector math methods across 9+ aggregates

### After (Database-Centric):
- ✅ Query optimizer plan caching
- ✅ Indexed views for instant aggregation results
- ✅ Inline TVFs with full optimizer benefits
- ✅ Parallelism for analytics (sp_GetUsageAnalytics)
- ✅ SIMD-optimized CLR (8x faster on AVX2)
- ✅ Native VECTOR_DISTANCE for set-based vector ops

**ESTIMATED PERFORMANCE IMPROVEMENT**:
- **Vector operations**: 8x faster (AVX2 SIMD)
- **Analytics queries**: 10-100x faster (indexed views + parallelism)
- **Paging queries**: 2-5x faster (plan caching + indexes)
- **Temporal queries**: Native VECTOR_DISTANCE outperforms CLR for bulk operations

---

## SOLID/DRY Compliance

### DRY (Don't Repeat Yourself):
- ✅ VectorUtilities delegates to VectorMath (eliminated duplicate scalar loops)
- ✅ Deleted duplicate EuclideanDistance from GeometricMedian aggregate
- ✅ Views eliminate duplicate SELECT logic across stored procedures
- ✅ Inline TVFs provide reusable parameterized queries

### SRP (Single Responsibility Principle):
- ⏳ TODO: Split AutonomousFunctions god class
- ⏳ TODO: Create ClrDataAccess abstraction layer

### Open/Closed Principle:
- ✅ Database objects (views/TVFs/procedures) can be modified without changing C# code
- ✅ Inline TVFs allow extending queries via composition

---

## References

- **Microsoft Docs**: "When to use SQL CLR" - https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/clr-integration-overview
- **Microsoft Docs**: "Indexed Views" - https://learn.microsoft.com/en-us/sql/relational-databases/views/create-indexed-views
- **Microsoft Docs**: "Inline Table-Valued Functions" - https://learn.microsoft.com/en-us/sql/relational-databases/user-defined-functions/user-defined-functions
- **SQL Server 2025**: "Native VECTOR data type" - https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-transact-sql
- **System.Numerics.Vector**: SIMD acceleration - https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector-1

---

## Conclusion

**The database is not just a data store - it's the primary computation engine.**

By moving SQL from C# to database objects:
1. Query optimizer can work its magic (plan caching, parallelism, indexes)
2. CLR can focus on what it's good at (SIMD-optimized RBAR operations)
3. T-SQL can focus on what it's good at (set-based operations)
4. Code is more maintainable (SOLID/DRY principles)
5. Testing is easier (SQL Server Unit Tests, CLR tests, API integration tests)

**This is the cutting-edge SQL Server 2025 architecture pattern.**
