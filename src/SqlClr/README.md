# SQL CLR Functions - Constraints and Capabilities

## .NET Framework Requirements

**Target**: `.NET Framework 4.8.1`  
**SQL Server Compatibility**: SQL Server 2016+ (CLR v4)

### Critical Constraints

❌ **NOT SUPPORTED**:
- SIMD types (`System.Numerics.Vector<T>`, `System.Runtime.Intrinsics.*`)
- .NET Standard 2.0+ assemblies with native dependencies
- Mixed managed/native assemblies
- Async/await patterns in SQL functions
- External API calls (must be synchronous)

✅ **SUPPORTED**:
- Pure managed code (no native dependencies)
- Basic LINQ operations
- System.Text.Json (v8.0.5)
- MathNet.Numerics (v5.0.0) - pure managed parts only
- NetTopologySuite (spatial/geometry operations)

## Performance Guidelines

### SQL CLR is GOOD for:
- **Vector Dot Products** (~100-1000 operations)
- **Custom Aggregations** (AVG, MAX with complex logic)
- **Spatial Operations** (GEOMETRY/GEOGRAPHY calculations)
- **JSON Processing** (parsing, validation, extraction)
- **Statistical Functions** (median, percentiles, variance)
- **Graph Traversal** (small graphs, <1000 nodes)

### SQL CLR is BAD for:
- **Full Transformer Inference** (billions of operations)
- **LayerNorm** (~1000 ops/token * layers = too slow)
- **Attention Mechanisms** (matrix operations too compute-intensive)
- **Large-scale ML Inference** (use Python + PyTorch/ONNX instead)
- **Real-time Processing** (async/await not supported)

**Rule of Thumb**: If operation requires >10,000 floating-point operations, use external API.

## Deployment Patterns

### SAFE Assembly (Recommended)
```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 'D:\path\to\SqlClrFunctions.dll'
WITH PERMISSION_SET = SAFE;
```

Allows:
- Deterministic functions
- No external resource access
- Maximum security

### UNSAFE Assembly (Use Sparingly)
```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 'D:\path\to\SqlClrFunctions.dll'
WITH PERMISSION_SET = UNSAFE;
```

Required for:
- File system access
- Network operations
- External process execution

**Security Risk**: Only use UNSAFE when absolutely necessary.

## Function Categories

### 1. Vector Operations
**Files**: `Core/VectorMath.cs`, `VectorOperations.cs`

Functions:
- `DotProduct` - Compute dot product of two vectors
- `CosineSimilarity` - Cosine similarity between vectors
- `EuclideanDistance` - L2 distance between vectors
- `ComputeCentroid` - Calculate centroid of vector set

Performance: 100-1000 ops per call (acceptable for SQL CLR)

### 2. Tensor Operations
**Files**: `TensorOperations/*.cs`

Functions:
- `TransformerForward` - **STUB ONLY** (see Performance Guidelines)
- Actual inference should use external API (Python + PyTorch)

### 3. Embedding & Projections
**Files**: `MachineLearning/TSNEProjection.cs`, `Core/LandmarkProjection.cs`

Functions:
- `TSNEProject` - t-SNE dimensionality reduction (small datasets only)
- `CreateLandmarkProjection` - Random projection for dimensionality reduction

Constraints: Limit to <10,000 data points per projection

### 4. JSON Processing
**Files**: `JsonProcessing/*.cs`

Functions:
- `ParseJson` - Parse JSON documents
- `ExtractJsonValue` - Extract values by JSON path
- `ValidateJson` - Validate JSON schema

Uses: `System.Text.Json` (Microsoft-supported in SQL CLR)

### 5. Spatial Operations
**Files**: `SpatialOperations/*.cs`

Functions:
- Geometry calculations using NetTopologySuite
- Spatial indexing helpers
- Geographic distance computations

Uses: NetTopologySuite for SQL Server spatial types

### 6. Aggregations
**Files**: `BehavioralAggregates.cs`

Functions:
- `ComputeCentroid` - Aggregate centroid calculation
- Custom statistical aggregations

## Building and Testing

### Build
```powershell
cd src/SqlClr
dotnet build -c Release
```

### Deploy
```powershell
cd scripts
.\deploy-clr-secure.ps1
```

### Test
```sql
-- Test vector operations
DECLARE @v1 GEOMETRY = geometry::Point(1, 0, 0);
DECLARE @v2 GEOMETRY = geometry::Point(0, 1, 0);
SELECT dbo.DotProduct(@v1, @v2); -- Should return 0

-- Test cosine similarity
SELECT dbo.CosineSimilarity(@v1, @v2); -- Should return 0
```

## Common Issues

### Issue: "Mixed assembly rejected"
**Cause**: Using SIMD types or native dependencies  
**Fix**: Remove `System.Numerics.Vector<T>` and use `float[]` loops

### Issue: "Could not load type System.Memory"
**Cause**: Dependency on .NET Standard 2.0+ with System.Memory  
**Fix**: Use .NET Framework 4.8.1-compatible libraries only

### Issue: "Assembly has invalid permissions"
**Cause**: Assembly requires UNSAFE but created with SAFE  
**Fix**: Deploy with `PERMISSION_SET = UNSAFE` or remove unsafe code

### Issue: "Function executes slowly"
**Cause**: Too many operations for SQL CLR  
**Fix**: Move to external API (Python/PyTorch) for compute-intensive work

## Research References

See `docs/SQL_CLR_RESEARCH_FINDINGS.md` for detailed research on:
- FINDING 1-14: SQL CLR constraints and compatibility
- FINDING 15-26: Performance characteristics and limitations
- FINDING 27-38: Best practices and optimization patterns

## Architecture Integration

### When to Use SQL CLR
- Need results directly in SQL queries
- Small-to-medium computation (<10K ops)
- No external API available
- Spatial/geometry calculations

### When to Use External API
- Large-scale ML inference (transformers, CNNs)
- Real-time processing with async/await
- GPU acceleration needed
- >10K floating-point operations

### Hybrid Pattern (Recommended)
```
SQL CLR: Lightweight ops (dot product, aggregations, spatial)
   ↓
External API: Heavy computation (transformer inference, training)
   ↓
SQL Server: Store results, run analytics
```

## Future Improvements

### Planned
- [ ] Add more statistical aggregate functions
- [ ] Optimize spatial operations with caching
- [ ] Add JSON schema validation

### Not Planned (Use External API Instead)
- ❌ Full transformer inference (too slow)
- ❌ LayerNorm implementation (impractical in pure managed code)
- ❌ Attention mechanisms (matrix ops too compute-intensive)

## Contributing

When adding new SQL CLR functions:
1. Keep operations <10K floating-point ops
2. NO SIMD types (`Vector<T>`, intrinsics)
3. Test with SAFE permission set first
4. Document performance characteristics
5. Add integration tests in `tests/Hartonomous.DatabaseTests/`

## License

Same as main Hartonomous project - see `LICENSE` file in repository root.
