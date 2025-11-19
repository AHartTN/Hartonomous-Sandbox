# CLR Deployment Guide

**SQL Server CLR Assembly Deployment & Critical Dependency Issues**

## ⚠️ CRITICAL KNOWN ISSUE

**System.Collections.Immutable.dll Incompatibility**

**Problem**: The CLR assemblies reference `System.Collections.Immutable.dll` and `System.Reflection.Metadata.dll`, which are **.NET Standard 2.0** libraries NOT supported by SQL Server's CLR host.

**Impact**:
- `CREATE ASSEMBLY` will FAIL in clean SQL Server environments
- Error message: "Assembly 'System.Collections.Immutable' references assembly 'netstandard, Version=2.0.0.0', which is not present in the current database"
- CLR assembly not reliably deployable as currently configured
- Blocks production deployment until resolved

**Root Cause**:
- SQL Server CLR host maintains a strict limited list of approved .NET Framework libraries
- Modern NuGet-based libraries like `System.Collections.Immutable` are not on the approved list
- The CLR host does not support .NET Standard 2.0 assemblies

**Priority**: **TOP-PRIORITY TECHNICAL DEBT** - Must be resolved before production deployment

**Resolution Strategies**:

**Option 1: Refactor Code (RECOMMENDED)**
- Remove all code dependencies on `System.Collections.Immutable`
- Replace `ImmutableArray<T>` with `T[]` or `List<T>`
- Replace `ImmutableDictionary<K,V>` with `Dictionary<K,V>` (thread-safety managed explicitly)
- Effort: ~3-5 days for 49 CLR functions

**Option 2: Out-of-Process Worker Service**
- Move affected CLR functions to external worker service (gRPC or HTTP API)
- Keep simple CLR functions (distance metrics, vector math) in-database
- Complex operations (model parsing, advanced algorithms) call external service
- Pros: Modern dependency support, easier debugging, better scaling
- Cons: Network latency (~5-10ms per call), operational complexity
- Effort: ~1-2 weeks for architecture + implementation

**Option 3: Hybrid Approach**
- Refactor hot-path functions (distance metrics, embeddings) to remove dependencies
- Move cold-path functions (model parsing, rare operations) to worker service
- Balances performance and maintainability
- Effort: ~1 week

**Current Workaround**:
- Manual `CREATE ASSEMBLY` for dependencies in development environments (not production-ready)
- Requires disabling `clr strict security` (security risk)

**Tracking**: See [GitHub Issue #47](https://github.com/your-repo/issues/47)

---

## Prerequisites

### SQL Server Configuration

**Required Version**: SQL Server 2022 (16.x) or later
- **Reason**: Native `VECTOR` type support (introduced in SQL Server 2022)
- **Edition**: Enterprise, Developer, or Evaluation (Standard lacks some features)

**CLR Configuration**:

```sql
-- Enable CLR integration (server-level)
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Enable CLR strict security (SQL Server 2017+)
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
```

**Trustworthy Database**:

```sql
-- Required for assemblies with EXTERNAL_ACCESS or UNSAFE
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
```

⚠️ **Security Warning**: `TRUSTWORTHY ON` allows CLR assemblies to access external resources (files, network). Only enable for databases you fully control. Alternative: Sign assemblies with certificate/asymmetric key (see below).

### .NET SDK

**Required**: .NET 8.0 SDK
- Download: https://dot.net/download/8.0
- Verify: `dotnet --version` (should output `8.0.x`)

**CLR Target Framework**: `net8.0`
- SQL Server 2022+ CLR host supports .NET 6.0+ (via CoreCLR)
- Older versions (SQL Server 2019 and below) require `net48` (classic CLR)

### Build Tools

**MSBuild**: Included with .NET SDK
**NuGet**: Included with .NET SDK

**PowerShell**: Version 7.0+
- Download: https://github.com/PowerShell/PowerShell/releases
- Verify: `$PSVersionTable.PSVersion`

---

## Build Process

### 1. Clone Repository

```powershell
git clone https://github.com/your-org/hartonomous.git
cd hartonomous
```

### 2. Restore Dependencies

```powershell
dotnet restore Hartonomous.sln
```

⚠️ **Expected Warning**: You will see warnings about `System.Collections.Immutable` not being supported by SQL CLR. This is the CRITICAL issue described above.

### 3. Build CLR Assembly

```powershell
dotnet build src/Hartonomous.Clr/Hartonomous.Clr.csproj -c Release
```

**Output**: `src/Hartonomous.Clr/bin/Release/net8.0/Hartonomous.Clr.dll`

### 4. Sign Assembly (Optional, Recommended for Production)

**Why Sign?**: SQL Server `clr strict security` requires assemblies with `EXTERNAL_ACCESS` or `UNSAFE` to be signed with a certificate or asymmetric key.

**Generate Strong Name Key**:

```powershell
# Create strong name key file
sn -k SqlClrKey.snk

# Extract public key
sn -p SqlClrKey.snk SqlClrKey.PublicKey
sn -tp SqlClrKey.PublicKey
# Note the public key token (e.g., "89e2a245c8c9a8df")
```

**Update Project File**:

```xml
<!-- Hartonomous.Clr.csproj -->
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>..\..\SqlClrKey.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
```

**Rebuild**:

```powershell
dotnet build src/Hartonomous.Clr/Hartonomous.Clr.csproj -c Release
```

**Create Certificate in SQL Server**:

```sql
USE master;
GO

-- Load binary of signed assembly
DECLARE @assemblyBinary VARBINARY(MAX) = (
    SELECT BulkColumn 
    FROM OPENROWSET(
        BULK 'C:\path\to\Hartonomous.Clr.dll', 
        SINGLE_BLOB
    ) AS assembly
);

-- Create asymmetric key from assembly
CREATE ASYMMETRIC KEY SqlClrKey FROM ASSEMBLY @assemblyBinary;

-- Create login from key
CREATE LOGIN SqlClrKeyLogin FROM ASYMMETRIC KEY SqlClrKey;

-- Grant EXTERNAL ACCESS ASSEMBLY permission
GRANT EXTERNAL ACCESS ASSEMBLY TO SqlClrKeyLogin;

-- Grant UNSAFE ASSEMBLY permission (only if needed)
GRANT UNSAFE ASSEMBLY TO SqlClrKeyLogin;
```

---

## Deployment

### Automated Deployment Script

**Script**: `scripts/deploy-clr-assemblies.ps1`

```powershell
param(
    [string]$SqlServer = "localhost",
    [string]$Database = "Hartonomous",
    [string]$AssemblyPath = "src/Hartonomous.Clr/bin/Release/net8.0/Hartonomous.Clr.dll"
)

# Read assembly binary
$assemblyBinary = [System.IO.File]::ReadAllBytes((Resolve-Path $AssemblyPath))
$assemblyHex = '0x' + [System.BitConverter]::ToString($assemblyBinary).Replace('-', '')

# Generate CREATE ASSEMBLY statement
$createAssembly = @"
USE [$Database];
GO

-- Drop existing assembly if exists
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'HartonomousClr')
BEGIN
    DROP ASSEMBLY HartonomousClr;
END
GO

-- Create assembly
CREATE ASSEMBLY HartonomousClr
FROM $assemblyHex
WITH PERMISSION_SET = SAFE;  -- Start with SAFE, escalate if needed
GO
"@

# Execute
Invoke-Sqlcmd -ServerInstance $SqlServer -Database $Database -Query $createAssembly -ErrorAction Stop

Write-Host "Assembly deployed successfully!" -ForegroundColor Green
```

**Usage**:

```powershell
.\scripts\deploy-clr-assemblies.ps1 -SqlServer "localhost" -Database "Hartonomous"
```

### Manual Deployment

**Step 1: Drop Existing Assembly** (if redeploying)

```sql
USE Hartonomous;
GO

-- Drop all functions/procedures using the assembly first
DROP FUNCTION IF EXISTS dbo.clr_CosineSimilarity;
DROP FUNCTION IF EXISTS dbo.clr_EuclideanDistance;
-- ... (drop all 49 CLR functions)

-- Drop assembly
DROP ASSEMBLY IF EXISTS HartonomousClr;
GO
```

**Step 2: Create Assembly**

```sql
-- Read binary file
DECLARE @assemblyBinary VARBINARY(MAX) = (
    SELECT BulkColumn 
    FROM OPENROWSET(
        BULK 'C:\path\to\Hartonomous.Clr.dll', 
        SINGLE_BLOB
    ) AS assembly
);

-- Create assembly
CREATE ASSEMBLY HartonomousClr
FROM @assemblyBinary
WITH PERMISSION_SET = SAFE;
GO
```

**Step 3: Create CLR Functions**

```sql
-- Distance Metrics
CREATE FUNCTION dbo.clr_CosineSimilarity(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.DistanceMetrics.CosineSimilarity].ComputeSimilarity;
GO

CREATE FUNCTION dbo.clr_EuclideanDistance(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.DistanceMetrics.EuclideanDistance].ComputeDistance;
GO

-- ... (create remaining 47 CLR functions)
```

**Full Script**: See `src/Hartonomous.Database/Scripts/Post-Deployment/CreateClrFunctions.sql`

---

## Permission Sets Explained

SQL Server CLR supports three permission sets (from least to most privileged):

### SAFE (Default, RECOMMENDED)

**Allowed**:
- Computation (math, string operations)
- Memory access (within SQL process)
- Internal data access (query database)

**Prohibited**:
- Network access
- File system access
- Registry access
- Native code interop

**Use Cases**:
- Distance metrics (cosine similarity, Euclidean distance)
- Vector math (dot product, projections)
- SVD compression (matrix operations)
- Spatial algorithms (trilateration, Hilbert indexing)

**Example Functions**:
- `clr_CosineSimilarity`
- `clr_EuclideanDistance`
- `clr_LandmarkProjection_ProjectTo3D`
- `clr_HilbertIndex`
- `clr_SvdCompress`

### EXTERNAL_ACCESS (Elevated)

**Allowed**: All SAFE permissions, plus:
- Network access (HTTP, TCP, gRPC)
- File system access (read/write)
- Environment variables

**Use Cases**:
- GPU worker communication (gRPC calls)
- Model file parsing (read local .gguf files)
- External API calls

**Example Functions**:
- `clr_CallGpuWorker` (gRPC to GPU service)
- `clr_LoadModelFromFile` (read GGUF/SafeTensors)

**Security**:
- Requires `TRUSTWORTHY ON` OR assembly signing + certificate
- Audit all code paths thoroughly

### UNSAFE (Extremely High Risk)

**Allowed**: All permissions, including:
- Native code interop (P/Invoke, COM)
- Unmanaged memory access
- Thread creation

**Use Cases**:
- Native library wrappers (CUDA, cuBLAS)
- Low-level performance optimizations

**Security**:
- **DO NOT USE unless absolutely necessary**
- Requires rigorous code review
- Can crash SQL Server process
- Requires assembly signing + certificate in `master` database

---

## Troubleshooting

### Error: "Assembly references assembly 'netstandard, Version=2.0.0.0'"

**Cause**: CRITICAL dependency issue (System.Collections.Immutable)

**Solution**: See "CRITICAL KNOWN ISSUE" section at top of this document. You must either:
1. Refactor code to remove dependency
2. Move affected functions to out-of-process worker service
3. Use temporary workaround (not production-ready)

**Temporary Workaround** (development only):

```sql
-- Load System.Collections.Immutable manually
DECLARE @immutableBinary VARBINARY(MAX) = (
    SELECT BulkColumn FROM OPENROWSET(
        BULK 'C:\path\to\System.Collections.Immutable.dll', 
        SINGLE_BLOB
    ) AS assembly
);

CREATE ASSEMBLY SystemCollectionsImmutable
FROM @immutableBinary
WITH PERMISSION_SET = SAFE;
GO

-- Repeat for System.Reflection.Metadata.dll
```

This approach is NOT production-ready because:
- Requires manual deployment of dependencies
- No versioning guarantee
- May conflict with SQL Server's internal libraries
- Not supported by Microsoft

### Error: "CREATE ASSEMBLY failed because assembly 'HartonomousClr' is not authorized for PERMISSION_SET = EXTERNAL_ACCESS"

**Cause**: `clr strict security` is enabled (default in SQL Server 2017+), but assembly is not signed.

**Solution Option 1: Sign Assembly** (recommended)

Follow steps in "4. Sign Assembly" section above.

**Solution Option 2: Disable clr strict security** (development only)

```sql
-- Disable strict security (NOT RECOMMENDED FOR PRODUCTION)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
```

⚠️ **Warning**: This disables important security protections. Only use in isolated development environments.

### Error: "Assembly 'HartonomousClr' was not found"

**Cause**: Assembly creation failed or was rolled back.

**Solution**:

```sql
-- Check if assembly exists
SELECT * FROM sys.assemblies WHERE name = 'HartonomousClr';

-- If not found, check for errors during CREATE ASSEMBLY
-- Look for dependency issues (System.Collections.Immutable)
```

### Error: "Could not load type 'Hartonomous.Clr.DistanceMetrics.CosineSimilarity' from assembly 'HartonomousClr'"

**Cause**: Namespace mismatch between T-SQL function definition and C# class.

**Solution**:

```csharp
// Verify C# class structure
namespace Hartonomous.Clr.DistanceMetrics
{
    public class CosineSimilarity
    {
        [Microsoft.SqlServer.Server.SqlFunction]
        public static SqlDouble ComputeSimilarity(SqlBytes vector1, SqlBytes vector2)
        {
            // Implementation
        }
    }
}
```

```sql
-- Ensure EXTERNAL NAME matches: AssemblyName.[Namespace.ClassName].MethodName
CREATE FUNCTION dbo.clr_CosineSimilarity(...)
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.DistanceMetrics.CosineSimilarity].ComputeSimilarity;
```

### Error: "Execution of user code in the .NET Framework is disabled"

**Cause**: CLR integration not enabled.

**Solution**:

```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

### Performance: Functions Running Slowly

**Cause**: Serialization overhead, data access pattern, or algorithm complexity.

**Diagnostics**:

```sql
-- Enable Query Store to track CLR function performance
ALTER DATABASE Hartonomous SET QUERY_STORE = ON;

-- Find slow CLR function calls
SELECT 
    q.query_id,
    qt.query_sql_text,
    rs.avg_duration / 1000.0 AS avg_duration_ms,
    rs.count_executions
FROM sys.query_store_query q
INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
INNER JOIN sys.query_store_plan qsp ON q.query_id = qsp.query_id
INNER JOIN sys.query_store_runtime_stats rs ON qsp.plan_id = rs.plan_id
WHERE qt.query_sql_text LIKE '%clr_%'
ORDER BY rs.avg_duration DESC;
```

**Optimizations**:

1. **Reduce Serialization**: Pass large data (embeddings) as `VARBINARY(MAX)` instead of individual parameters
2. **Batch Operations**: Process multiple vectors in single CLR call instead of row-by-row
3. **SIMD Vectorization**: Use `System.Numerics.Vectors` for parallel math operations
4. **Algorithm Complexity**: Ensure O(K) refinement stays small (K < 100 recommended)

---

## Verification

### Test CLR Functions

```sql
USE Hartonomous;
GO

-- Test cosine similarity
DECLARE @vec1 VARBINARY(MAX) = 0x3F8000003F0000003E800000;  -- [1.0, 0.5, 0.25]
DECLARE @vec2 VARBINARY(MAX) = 0x3F8000003F0000003E800000;  -- [1.0, 0.5, 0.25]

SELECT dbo.clr_CosineSimilarity(@vec1, @vec2) AS similarity;
-- Expected: 1.0 (identical vectors)

-- Test Euclidean distance
SELECT dbo.clr_EuclideanDistance(@vec1, @vec2) AS distance;
-- Expected: 0.0 (identical vectors)

-- Test Hilbert indexing
DECLARE @point GEOMETRY = geometry::Point(10.5, 20.3, 15.7, 0);
SELECT dbo.clr_HilbertIndex(@point) AS hilbert_index;
-- Expected: Integer value (e.g., 458231)
```

### List Deployed Functions

```sql
-- List all CLR functions
SELECT 
    o.name AS FunctionName,
    a.name AS AssemblyName,
    am.assembly_method AS MethodName,
    p.permission_set_desc AS PermissionSet
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
INNER JOIN sys.assemblies p ON a.name = p.name
WHERE o.type = 'FS'  -- CLR scalar function
ORDER BY o.name;
```

### Check Assembly Permissions

```sql
-- View assembly permission set
SELECT 
    name,
    permission_set_desc,
    create_date,
    is_visible
FROM sys.assemblies
WHERE name = 'HartonomousClr';
```

---

## Production Deployment Checklist

- [ ] **Resolve CRITICAL dependency issue** (System.Collections.Immutable)
- [ ] Sign assembly with strong name key
- [ ] Create certificate in `master` database
- [ ] Enable `clr strict security`
- [ ] Set `TRUSTWORTHY OFF` (use certificate-based permissions instead)
- [ ] Test all 49 CLR functions with production-like data
- [ ] Run performance benchmarks (O(K) refinement should be <20ms for K=50)
- [ ] Configure monitoring (Query Store, Extended Events)
- [ ] Document rollback procedure
- [ ] Create deployment runbook for operations team
- [ ] Review all EXTERNAL_ACCESS functions for security risks
- [ ] Ensure no UNSAFE functions (or rigorous code review if required)

---

## Next Steps

1. **Resolve CRITICAL Issue**: See "System.Collections.Immutable Incompatibility" section
2. **Deploy Database Schema**: Run DACPAC or migration scripts
3. **Deploy CLR Assembly**: Follow "Automated Deployment Script" section
4. **Test Basic Queries**: Verify O(log N) + O(K) pattern works
5. **Deploy Worker Services**: See Worker Services documentation
6. **Configure Neo4j Sync**: See Neo4j Provenance documentation
7. **Enable OODA Loop**: See OODA Architecture documentation

---

## Summary

CLR deployment requires careful attention to:
- **CRITICAL ISSUE**: System.Collections.Immutable incompatibility (top priority)
- Permission sets (SAFE preferred, EXTERNAL_ACCESS requires signing)
- Assembly signing for production (`clr strict security`)
- Performance verification (O(K) should be <20ms)
- Security auditing (all EXTERNAL_ACCESS code paths)

**Current Status**: Development-ready with workarounds, production deployment **blocked** by dependency issue. Estimated resolution: 3-5 days (refactor) or 1-2 weeks (worker service).
