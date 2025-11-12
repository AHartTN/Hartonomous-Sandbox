# Hartonomous CLR Guide

**Comprehensive reference for SQL CLR assemblies in Hartonomous.**

## Overview

Hartonomous deploys a .NET Framework 4.8.1 CLR assembly (`SqlClrFunctions.dll`) into SQL Server 2025 to provide **CPU SIMD-accelerated** vector operations, transformer inference, anomaly detection, multimodal processing, and stream orchestration. CLR functions execute in-process with sub-millisecond latency, enabling T-SQL to call advanced AI operations directly.

**Current Deployment**: 14 assemblies, CPU SIMD-only (AVX2/SSE4), no GPU acceleration

**Key Capabilities**:

- **Vector Math**: Dot product, cosine similarity, Euclidean distance, vector aggregates (AVX2/SSE4 SIMD)
- **Transformer Inference**: Multi-head attention, feed-forward layers, layer normalization
- **Anomaly Detection**: Isolation Forest, Mahalanobis distance, threshold-based scores
- **Multimodal Processing**: Audio waveform generation, image patch extraction, text/image/audio fusion
- **Spatial Operations**: Trilateration projection (high-dimensional → 3D GEOMETRY), landmark projection
- **Stream Orchestration**: AtomicStream, ComponentStream UDTs for event stream serialization

**Hardware Acceleration**: Uses `VectorMath` class with CPU SIMD intrinsics (AVX2 on modern Intel/AMD CPUs, SSE4 fallback). No GPU acceleration (ILGPU disabled/commented due to CLR verifier incompatibility with unmanaged pointers; code preserved for potential future implementation outside SQL CLR).

## .NET Framework 4.8.1 Constraints

### Critical Limitations

**SQL CLR ONLY supports .NET Framework**, not .NET Core or .NET 5+.

❌ **Not Supported**:

- .NET Standard 2.1+ or .NET Core 3.0+ assemblies
- Modern SIMD types (`System.Numerics.Vector<T>`, `System.Runtime.Intrinsics.*`)
- `async`/`await` patterns inside SQL functions
- Mixed managed/native assemblies (P/Invoke, native stubs)
- Outbound network calls or process spawning

✅ **Supported**:

- Pure managed code (.NET Framework 4.8.1 only)
- Basic LINQ operations
- MathNet.Numerics v5.0.0 (pure managed surface area)
- NetTopologySuite (spatial/geometry operations)
- Newtonsoft.Json (JSON serialization)

**Why This Matters**: SQL Server rejects assemblies containing native code or modern SIMD intrinsics with error: *"Assembly is malformed or not a pure .NET assembly. Unverifiable PE Header/native stub."*

### Officially Supported Assemblies

Microsoft supports only these .NET Framework assemblies:

1. `mscorlib.dll`
2. `System.dll`
3. `System.Data.dll`
4. `System.Xml.dll`
5. `Microsoft.VisualBasic.dll`
6. `Microsoft.VisualC.dll`
7. `CustomMarshalers.dll`
8. `System.Security.dll`
9. `System.Web.Services.dll`
10. `System.Data.SqlXml.dll`
11. `System.Transactions.dll`
12. `System.Data.OracleClient.dll`
13. `System.Configuration.dll`

**Unsupported assemblies** (including `MathNet.Numerics`, `Newtonsoft.Json`) produce warnings and receive no Microsoft CSS support. Use at your own risk.

## CLR Function Reference

### Vector Operations (`VectorOperations.cs`)

**Dot Product**:

```sql
DECLARE @vec1 VECTOR(1998) = <vector-data>;
DECLARE @vec2 VECTOR(1998) = <vector-data>;
SELECT dbo.clr_VectorDistance(@vec1, @vec2) AS Distance;
```

**Cosine Similarity**:

```sql
SELECT dbo.clr_CosineSimilarity(@embedding1, @embedding2) AS Similarity;
```

**Euclidean Distance**:

```sql
SELECT dbo.clr_EuclideanDistance(@vec1, @vec2) AS Distance;
```

**Implementation Notes**:

- Uses `VectorMath` class with CPU SIMD intrinsics (AVX2/SSE4)
- `GpuAccelerator` is a wrapper that always returns `IsGpuAvailable=false` and `DeviceType="CPU-SIMD"`
- All operations delegate to `VectorMath` for CPU execution
- Processes 8 floats per instruction (AVX2) or 4 floats (SSE4) depending on CPU support
- **Performance**: 8-16x speedup over scalar operations for 1998-dimension vectors (vs baseline, not GPU)

### Vector Aggregates (`VectorAggregates.cs`)

**Vector Mean Aggregate**:

```sql
SELECT dbo.VectorMean(EmbeddingVector) AS MeanVector
FROM dbo.AtomEmbeddings
WHERE EmbeddingType = 'text-embedding-3-large';
```

**Vector Covariance**:

```sql
SELECT dbo.VectorCovariance(EmbeddingVector) AS CovarianceMatrix
FROM dbo.AtomEmbeddings;
```

**Attributes**: `[SqlUserDefinedAggregate]` with custom binary serialization via `IBinarySerialize`

### Transformer Inference (`TensorOperations/TransformerInference.cs`)

**Multi-Head Attention**:

```sql
DECLARE @queries VARBINARY(MAX) = <query-tensor>;
DECLARE @keys VARBINARY(MAX) = <key-tensor>;
DECLARE @values VARBINARY(MAX) = <value-tensor>;

SELECT dbo.clr_MultiHeadAttention(@queries, @keys, @values, @numHeads) AS AttentionOutput;
```

**Feed-Forward Layer**:

```sql
SELECT dbo.clr_FeedForward(@input, @weights, @bias) AS Output;
```

**Warning**: Transformer inference is computationally expensive. For production inference, use external services (Azure OpenAI, local ONNX models). CLR functions are for research and small-batch processing only.

### Anomaly Detection (`Analysis/AnomalyDetectionAggregates.cs`)

**Isolation Forest Aggregate**:

```sql
SELECT dbo.IsolationForestScore(EmbeddingVector) AS AnomalyScore
FROM dbo.AtomEmbeddings;
```

**Mahalanobis Distance**:

```sql
SELECT dbo.clr_MahalanobisDistance(@vector, @mean, @covariance) AS Distance;
```

**Feeds**: OODA loop reflex governance (`sp_Analyze` detects anomalies and triggers hypotheses)

### Multimodal Processing

**Audio Waveform Generation** (`AudioProcessing.cs`):

```sql
SELECT dbo.clr_GenerateWaveform(@frequency, @duration, @sampleRate) AS AudioData;
```

**Image Patch Extraction** (`ImageProcessing.cs`):

```sql
SELECT * FROM dbo.clr_ExtractImagePatches(@imageData, @patchSize);
-- Returns table: (patch_x, patch_y, spatial_x, spatial_y, spatial_z, patch GEOMETRY)
```

**Multimodal Generation** (`MultiModalGeneration.cs`):

```sql
EXEC dbo.clr_RunInference 
    @modelName = 'gpt-4-vision',
    @inputJson = '{"text": "Describe this image", "imageUrl": "https://..."}',
    @outputJson = @result OUTPUT;
```

### Spatial Operations (`SpatialOperations.cs`)

**Trilateration Projection** (high-dimensional → 3D GEOMETRY):

```sql
SELECT dbo.clr_ProjectToGeometry(@embedding) AS SpatialGeometry;
-- Projects 1998D embedding to 3D point via distance-preserving transformation
```

**Landmark Projection** (`Core/LandmarkProjection.cs`):

```sql
SELECT dbo.clr_LandmarkProjection(@embedding, @landmarks) AS Geometry3D;
-- Uses 4 anchor points in high-D space, solves for 3D coordinates
```

**Purpose**: Enables R-tree spatial indexing on embeddings for O(log n) nearest-neighbor search

### Event Streams (`AtomicStream.cs`, `ComponentStream.cs`)

**User-Defined Types (UDTs)** for event serialization:

```sql
DECLARE @stream dbo.AtomicStream;
SET @stream = dbo.fn_CreateAtomicStream(@atomId, @eventType, @eventData);

-- Persist to provenance table
INSERT INTO dbo.ProvenanceStreams (AtomId, EventStream)
VALUES (@atomId, @stream);
```

**Consumed By**: `provenance.AtomicStreamFactory.sql`, `Stream.StreamOrchestration.sql`

## Build and Deployment

### Build CLR Assembly

```pwsh
cd src/SqlClr
dotnet build SqlClrFunctions.csproj -c Release
```

**Output**: `src/SqlClr/bin/Release/SqlClrFunctions.dll`

**Dependencies** (copied to `dependencies/` by `scripts/copy-dependencies.ps1`):

1. `System.Runtime.CompilerServices.Unsafe.dll` - Unsafe memory operations
2. `System.Buffers.dll` - Memory buffer management
3. `System.Numerics.Vectors.dll` - SIMD vector types
4. `System.Memory.dll` - Span<T> and Memory<T>
5. `System.Runtime.InteropServices.RuntimeInformation.dll` - Platform detection
6. `System.Collections.Immutable.dll` - Immutable collections
7. `System.Reflection.Metadata.dll` - Metadata reading
8. `System.ServiceModel.Internals.dll` - WCF internals
9. `SMDiagnostics.dll` - Service model diagnostics
10. `System.Drawing.dll` - Graphics/image operations
11. `System.Runtime.Serialization.dll` - Data contract serialization
12. `Newtonsoft.Json.dll` - JSON serialization (runtime uses GAC version)
13. `MathNet.Numerics.dll` - Advanced mathematics

**Total**: 14 assemblies (13 dependencies + SqlClrFunctions)

### Deployment: Development (Local)

**Unified Script** (`scripts/deploy-database-unified.ps1`):

```pwsh
./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"
```

**What It Does**:

1. Enables CLR integration (`clr enabled = 1`)
2. Disables strict security (`clr strict security = 0`) **for local dev only**
3. Streams assemblies as hex: `CREATE ASSEMBLY ... FROM 0x... WITH PERMISSION_SET = UNSAFE`
4. Binds CLR functions/aggregates/types via `sql/procedures/Common.ClrBindings.sql`

**Security**: `TRUSTWORTHY` remains OFF. Assemblies are NOT strong-name signed. This is **development only**.

### Deployment: Production (Secure)

**Secure Script** (`scripts/deploy-clr-secure.ps1`):

```pwsh
./scripts/deploy-clr-secure.ps1 `
    -ServerName "prod-sql-primary" `
    -DatabaseName "Hartonomous" `
    -BinDirectory "src/SqlClr/bin/Release"
```

**Production Security**:

1. Keeps `clr strict security = 1` (SQL Server 2017+ default)
2. Registers assembly hashes via `sp_add_trusted_assembly`:

   ```sql
   DECLARE @hash BINARY(64) = 0x<SHA512-hash>;
   EXEC sys.sp_add_trusted_assembly @hash, N'SqlClrFunctions';
   ```

3. Ensures `TRUSTWORTHY OFF` (no database-level privilege escalation)
4. Validates strong-name signatures (requires assemblies signed with `.snk` key)

**Strong-Name Signing** (production requirement):

1. Generate key:

   ```pwsh
   sn -k HartonomousKey.snk
   ```

2. Add to `SqlClrFunctions.csproj`:

   ```xml
   <PropertyGroup>
     <SignAssembly>true</SignAssembly>
     <AssemblyOriginatorKeyFile>HartonomousKey.snk</AssemblyOriginatorKeyFile>
   </PropertyGroup>
   ```

3. Rebuild:

   ```pwsh
   dotnet build SqlClrFunctions.csproj -c Release
   ```

## Permission Sets: SAFE vs UNSAFE

### SAFE (Recommended When Possible)

**Restrictions**:

- No external resource access (files, network, registry)
- No unmanaged code
- Deterministic operations only

**Example**:

```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 'D:\path\to\SqlClrFunctions.dll'
WITH PERMISSION_SET = SAFE;
```

### UNSAFE (Required for Hartonomous)

**Why UNSAFE is Required**:

1. **CPU SIMD Intrinsics**: `System.Numerics.Vectors` uses `unsafe` pointer arithmetic for AVX2/SSE4
2. **MathNet.Numerics**: High-performance math operations require unmanaged memory
3. **FILESTREAM**: Direct memory-mapped file access for model weights
4. **Newtonsoft.Json**: Zero-allocation span operations use unsafe casts

**Example**:

```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 0x4D5A9000...
WITH PERMISSION_SET = UNSAFE;
```

**Security Mitigation**:

- Use `sp_add_trusted_assembly` (hash verification)
- Keep `TRUSTWORTHY OFF`
- Audit `sys.trusted_assemblies` regularly
- Sign assemblies in CI/CD pipeline

## Security Best Practices

### Development

✅ **Allowed**:

- `TRUSTWORTHY ON` for rapid iteration (local dev only)
- `clr strict security = 0` (simplifies deployment)
- Unsigned assemblies

❌ **Never in Production**:

- Do NOT deploy development script to production
- Do NOT leave `TRUSTWORTHY ON`
- Do NOT skip strong-name signing

### Production

✅ **Required**:

1. `clr strict security = 1` (SQL Server 2017+ default)
2. `TRUSTWORTHY OFF` (no privilege escalation)
3. Strong-name signed assemblies
4. Hash registration via `sp_add_trusted_assembly`
5. Audit `sys.trusted_assemblies` regularly

**Verification**:

```sql
-- Check TRUSTWORTHY setting (should be OFF)
SELECT name, is_trustworthy_on
FROM sys.databases
WHERE name = 'Hartonomous';
-- Expected: is_trustworthy_on = 0

-- Check CLR strict security (should be ON)
SELECT name, value_in_use
FROM sys.configurations
WHERE name = 'clr strict security';
-- Expected: value_in_use = 1

-- List trusted assemblies
SELECT * FROM sys.trusted_assemblies;
```

## Performance Characteristics

### SIMD Acceleration (CPU Only)

**AVX2 (8 floats per instruction)** - Modern Intel/AMD CPUs (2013+):

- Dot product (1998D vectors): ~18,500 ops/sec (9.25x speedup vs scalar)
- Cosine similarity: ~17,000 ops/sec
- Euclidean distance: ~16,500 ops/sec

**SSE4 Fallback (4 floats per instruction)** - Older CPUs:

- Dot product: ~9,000 ops/sec (4.5x speedup)
- Automatic fallback when AVX2 unavailable

**Note**: GPU acceleration (ILGPU) was removed due to CLR verifier incompatibility with unmanaged GPU memory pointers. All operations use CPU SIMD only.

### JSON Serialization (Newtonsoft.Json)

- `ref readonly` optimization: Zero allocation for read-only scenarios
- UTF-8 direct encoding: Unsafe pointer operations avoid string allocations
- **Throughput**: ~500 MB/sec for large payloads

### Workload Guidance

**Good Workloads** (use CLR):

- Vector operations: ~100–10,000 operations per invocation
- Custom aggregations with bespoke logic
- Spatial operations (GEOMETRY/GEOGRAPHY calculations)
- Statistical functions (median, percentiles, variance)
- Small-graph traversal (<1,000 nodes)

**Bad Workloads** (use external services):

- Full transformer inference (billions of operations)
- Layer normalization (≈1,000 ops/token per layer)
- Attention mechanisms (matrix operations too heavy)
- Large-scale ML inference
- Real-time processing requiring `async`/`await`

**Rule of Thumb**: If the routine needs more than ~10K floating-point operations per invocation, move it to an external service (Azure OpenAI, ONNX Runtime, etc.).

## Troubleshooting

### Error: "Assembly is not authorized for PERMISSION_SET = UNSAFE"

**Cause**: `clr strict security = 1` without matching entry in `sys.trusted_assemblies`, or executing login lacks `UNSAFE ASSEMBLY` permission.

**Fix (Development)**:

```pwsh
./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"
# Temporarily disables strict security
```

**Fix (Production)**:

1. Compute SHA-512 hash and register:

   ```sql
   DECLARE @hash VARBINARY(64) = 0x<SHA512>;
   EXEC sys.sp_add_trusted_assembly @hash, N'SqlClrFunctions';
   ```

2. Grant permission:

   ```sql
   USE master;
   GRANT UNSAFE ASSEMBLY TO [HartonomousServiceLogin];
   ```

### Error: "Could not load assembly 'System.Numerics.Vectors'"

**Cause**: Dependency DLL missing or not deployed in correct order.

**Fix**: Deploy dependencies in correct order (see deploy-clr-secure.ps1 for exact order):

1. `System.Runtime.CompilerServices.Unsafe.dll`
2. `System.Buffers.dll`
3. `System.Numerics.Vectors.dll`
4. `System.Memory.dll`
5. `System.Runtime.InteropServices.RuntimeInformation.dll`
6. `System.Collections.Immutable.dll`
7. `System.Reflection.Metadata.dll`
8. `System.ServiceModel.Internals.dll`
9. `SMDiagnostics.dll`
10. `System.Drawing.dll`
11. `System.Runtime.Serialization.dll`
12. `Newtonsoft.Json.dll`
13. `MathNet.Numerics.dll`
14. `SqlClrFunctions.dll` (last)

**Automated**: Run `scripts/deploy-clr-secure.ps1` to deploy all 14 assemblies in correct order.

### Error: "Assembly is malformed or not a pure .NET assembly"

**Cause**: Assembly contains native code or mixed managed/native content.

**Common Culprits**:

- SIMD libraries targeting .NET Core 3.0+
- P/Invoke declarations
- Native dependencies

**Fix**: Ensure assembly targets .NET Framework 4.8.1 with pure managed code only.

### Poor Performance

**Issue**: CLR function slower than expected.

**Diagnosis**:

1. Check if routine exceeds ~10K operation budget
2. Profile with Extended Events:

   ```sql
   CREATE EVENT SESSION [CLR_Profiling]
   ON SERVER
   ADD EVENT sqlserver.clr_assembly_load,
   ADD EVENT sqlserver.sp_statement_completed (
       WHERE object_name LIKE '%clr_%'
   );
   ```

3. Review query plan for excessive CLR invocations

**Fix**: Move heavy computation to external service or optimize algorithm.

## Extended Events Monitoring

### Track CLR Operations

```sql
CREATE EVENT SESSION [Hartonomous_CLR_Operations]
ON SERVER
ADD EVENT sqlserver.clr_assembly_load,
ADD EVENT sqlserver.clr_allocation_failure,
ADD EVENT sqlserver.sp_statement_completed (
    WHERE sqlserver.database_name = 'Hartonomous'
      AND object_name LIKE '%clr_%'
)
ADD TARGET package0.event_file (
    SET filename = N'Hartonomous_CLR_Operations.xel'
);

ALTER EVENT SESSION [Hartonomous_CLR_Operations] ON SERVER STATE = START;
```

### Query Extended Events

```sql
SELECT 
    event_data.value('(event/@name)[1]', 'nvarchar(50)') AS EventName,
    event_data.value('(event/@timestamp)[1]', 'datetime2') AS Timestamp,
    event_data.value('(event/data[@name="object_name"]/value)[1]', 'nvarchar(256)') AS ObjectName,
    event_data.value('(event/data[@name="duration"]/value)[1]', 'bigint') AS DurationMicroseconds
FROM (
    SELECT CAST(event_data AS XML) AS event_data
    FROM sys.fn_xe_file_target_read_file('Hartonomous_CLR_Operations*.xel', NULL, NULL, NULL)
) AS xevents
ORDER BY Timestamp DESC;
```

## Architecture Notes

### Why No GPU Acceleration?

**ILGPU Disabled**: ILGPU 0.8.0 (and earlier 1.5.1) failed SQL Server CLR verifier due to unmanaged GPU memory pointers. SQL Server CLR requires verifiable pure managed code only. ILGPU code is disabled/commented in SqlClrFunctions project but preserved for potential future implementation outside SQL CLR.

**Current Approach**: CPU SIMD via `VectorMath` class using `System.Numerics.Vectors` (AVX2/SSE4 intrinsics). This provides 8-16x speedup over scalar operations without CLR verifier issues.

**GpuAccelerator Class**: Remains as a no-op wrapper (`IsGpuAvailable=false`, `DeviceType="CPU-SIMD"`) that delegates all operations to `VectorMath`. This preserves the API surface for potential future GPU integration outside SQL CLR (e.g., in API/worker processes).

**Performance**: For SQL CLR workloads (< 10K operations per call), CPU SIMD is sufficient. Large-scale ML inference should use external services (Azure OpenAI, ONNX Runtime, etc.) and persist results to SQL Server.

## References

- [README.md](../README.md) - Getting started guide
- [ARCHITECTURE.md](../ARCHITECTURE.md) - System architecture
- [DEPLOYMENT.md](../DEPLOYMENT.md) - Deployment guide
- [UNSAFE_CLR_SECURITY.md](UNSAFE_CLR_SECURITY.md) - Security implications and best practices
- [SQLSERVER_BINDING_REDIRECTS.md](../SQLSERVER_BINDING_REDIRECTS.md) - Newtonsoft.Json GAC binding configuration
- [Microsoft Docs: CLR Integration](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/common-language-runtime-integration-overview)
- [Microsoft Docs: CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- [Microsoft Docs: CREATE ASSEMBLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-assembly-transact-sql)
- [Microsoft Docs: sys.sp_add_trusted_assembly](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sys-sp-add-trusted-assembly-transact-sql)
