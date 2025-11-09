# SQL CLR Functions - Constraints and Capabilities

## .NET Framework Requirements

**Target**: `.NET Framework 4.8.1`  
**SQL Server Compatibility**: SQL Server 2016+ (CLR v4)

### Critical Constraints

❌ **Not Supported**

- SIMD types (`System.Numerics.Vector<T>`, `System.Runtime.Intrinsics.*`)
- .NET Standard 2.0+ assemblies with native dependencies
- Mixed managed/native assemblies
- Async/await patterns inside SQL functions
- Outbound network or process calls

✅ **Supported**

- Pure managed code (no native dependencies)
- Basic LINQ operations
- MathNet.Numerics (v5.0.0) — pure managed surface area only
- NetTopologySuite for spatial/geometry operations

## Workload Guidance

### SQL CLR Good Workloads

- Vector dot products (~100–1000 operations)
- Custom aggregations (AVG/MAX with bespoke logic)
- Spatial operations (GEOMETRY/GEOGRAPHY calculations)
- JSON shaping for result payloads via `Core/SimpleJson`
- Statistical functions (median, percentiles, variance)
- Graph traversal over small graphs (<1000 nodes)

### SQL CLR Workloads to Avoid

- Full transformer inference (billions of operations)
- LayerNorm (≈1000 ops/token per layer)
- Attention mechanisms (matrix operations too heavy)
- Large-scale ML inference (prefer external services)
- Real-time processing requiring async/await

**Rule of thumb**: if the routine needs more than ~10K floating-point operations per invocation, move it out of SQL CLR.

## Deployment Patterns

### SAFE Assembly (Recommended)

```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 'D:\path\to\SqlClrFunctions.dll'
WITH PERMISSION_SET = SAFE;
```

Allows deterministic routines that stay within database boundaries.

### UNSAFE Assembly (Only When Required)

```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 'D:\path\to\SqlClrFunctions.dll'
WITH PERMISSION_SET = UNSAFE;
```

Use only for routines that must access files, network resources, or other external dependencies.

## Function Categories

### 1. Vector Operations

**Files**: `Core/VectorMath.cs`, `VectorOperations.cs`

Provides dot product, cosine similarity, Euclidean distance, and centroid helpers sized for SQL workloads.

### 2. Tensor Operations

**Files**: `TensorOperations/*.cs`

Includes experimental transformer helpers. Leave heavy inference to external services; these stubs exist for research only.

### 3. Embedding & Projections

**Files**: `MachineLearning/TSNEProjection.cs`, `Core/LandmarkProjection.cs`

Supports t-SNE and landmark projection for small datasets (<10K points) to stay within SQL CLR limits.

### 4. Spatial Operations

**Files**: `SpatialOperations/*.cs`

Wraps NetTopologySuite helpers for geometry calculations, spatial indexing, and geographic distance computation.

### 5. Aggregations

**Files**: `BehavioralAggregates.cs`, `VectorAggregates.cs`, `TimeSeriesVectorAggregates.cs`, `RecommenderAggregates.cs`, `NeuralVectorAggregates.cs`

All aggregates now emit JSON strings using the lightweight `Core/SimpleJson` helper, keeping serialization deterministic and dependency-free.

## Building and Testing

### Build

```powershell
cd src/SqlClr
msbuild SqlClrFunctions.csproj /p:Configuration=Release
```

### Deploy

```powershell
cd ..\..\scripts
./deploy/deploy-clr-secure.ps1 -AssemblyPath "..\src\SqlClr\bin\Release\SqlClrFunctions.dll"
```

### Smoke Tests

```sql
-- Vector sanity check
SELECT dbo.CosineSimilarity('(1,0,0)', '(0,1,0)');

-- Aggregation sample
SELECT SUM(dbo.DotProductVector(ValueColumn))
FROM dbo.VectorSamples;
```

## Common Issues

### Mixed Assembly Rejected

**Cause**: Use of SIMD or native dependencies  
**Fix**: Stay with plain `float[]` loops and pure managed DLLs.

### Missing System.Memory or System.Text.Json

**Cause**: Referencing packages unsupported inside SQL CLR  
**Fix**: The project intentionally omits these references; rely on `Core/SimpleJson` and SQL-native `OPENJSON` instead.

### Invalid Permission Set

**Cause**: Routine requires external resources but assembly deployed as `SAFE`  
**Fix**: Remove the external call or redeploy with `PERMISSION_SET = UNSAFE`.

### Poor Performance

**Cause**: Routine exceeds SQL CLR’s compute budget  
**Fix**: Move the routine to an external service or shrink the workload.

## Architecture Integration

Use SQL CLR when results must surface in-line with T-SQL queries, the computation fits within the ~10K op envelope, and you need deterministic execution. Delegate large inference, GPU workloads, or asynchronous processing to external APIs, then persist results back into SQL Server for analytics.

## Future Improvements

- Validate incoming JSON payloads using T-SQL helpers instead of CLR parsing
- Expand aggregate coverage with additional statistical metrics
- Profile spatial routines for additional caching opportunities

## Contributing

1. Keep each routine deterministic and under ~10K floating-point operations.
2. Do not introduce third-party JSON serializers or SIMD dependencies.
3. Prefer SAFE assemblies; document any UNSAFE requirement explicitly.
4. Update docs and smoke tests when adding new routines.

## License

The SQL CLR project inherits the repository’s primary license — see `LICENSE` at the repo root.
