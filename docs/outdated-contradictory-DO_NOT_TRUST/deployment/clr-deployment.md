# SQL CLR Deployment Guide

**Last Updated**: November 13, 2025  
**SQL Server Version**: 2025  
**.NET Framework**: 4.8.1  
**Assemblies**: 14 total (13 dependencies + 1 application)

---

## Overview

Hartonomous deploys SQL CLR assemblies to provide **high-performance vector operations**, **multimodal processing**, and **AI capabilities** directly within SQL Server. All functions execute in-process with sub-millisecond latency.

**Architecture**:
- **14 assemblies** deployed in strict dependency order
- **UNSAFE permission set** for all assemblies (required for SIMD operations)
- **Trusted assembly security** (SQL Server 2017+ CLR strict security)
- **CPU SIMD acceleration** (AVX2/SSE4) - NO GPU
- **Pure managed code** (.NET Framework 4.8.1 only)

**Key Capabilities**:
- Vector math (dot product, cosine similarity, Euclidean distance)
- Transformer inference (multi-head attention, layer normalization)
- Anomaly detection (isolation forest, Mahalanobis distance)
- Multimodal processing (text/image/audio fusion)
- Spatial operations (high-dimensional → 3D GEOMETRY projection)
- Stream orchestration (atomic event streams)

---

## Prerequisites

### SQL Server Configuration

1. **Enable CLR Integration**:
   ```sql
   sp_configure 'clr enabled', 1;
   RECONFIGURE;
   ```

2. **Enable CLR Strict Security** (SQL Server 2017+):
   ```sql
   sp_configure 'clr strict security', 1;
   RECONFIGURE;
   ```

3. **Verify Configuration**:
   ```sql
   SELECT name, value_in_use 
   FROM sys.configurations 
   WHERE name IN ('clr enabled', 'clr strict security');
   ```

   Expected output:
   ```
   name                  value_in_use
   clr enabled           1
   clr strict security   1
   ```

### Database Configuration

**TRUSTWORTHY OFF** (production security best practice):
```sql
ALTER DATABASE Hartonomous SET TRUSTWORTHY OFF;
```

**IMPORTANT**: We use `sys.sp_add_trusted_assembly` instead of TRUSTWORTHY ON. This provides better security by explicitly trusting specific assemblies rather than the entire database.

### Assembly Requirements

- All assemblies must be **strong-name signed**
- All assemblies must be **pure managed code** (.NET Framework 4.8.1)
- All assemblies extracted from **net4x** folders (NOT netstandard)
- Assembly files located in `dependencies/` directory

---

## Deployment Script

**Primary Script**: `scripts/deploy-clr-secure.ps1`

### Basic Usage

```powershell
# Deploy to local SQL Server
.\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"

# Deploy with rebuild
.\scripts\deploy-clr-secure.ps1 -ServerName "." -DatabaseName "Hartonomous" -Rebuild

# Deploy to remote server
.\scripts\deploy-clr-secure.ps1 -ServerName "prod-sql.domain.com" -DatabaseName "Hartonomous"

# Skip configuration check (for automated deployments)
.\scripts\deploy-clr-secure.ps1 -ServerName "." -SkipConfigCheck
```

### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `ServerName` | `.` | SQL Server instance name |
| `DatabaseName` | `Hartonomous` | Target database name |
| `BinDirectory` | `src\SqlClr\bin\Release` | Path to compiled assembly |
| `DependenciesDirectory` | `dependencies\` | Path to dependency DLLs |
| `Rebuild` | `false` | Rebuild SqlClrFunctions before deployment |
| `SkipConfigCheck` | `false` | Skip SQL Server configuration verification |

---

## Assembly Deployment Order

The script deploys **14 assemblies** in strict dependency order. Dependencies must be deployed before assemblies that reference them.

### TIER 1: Foundation (No Dependencies)

1. **System.Runtime.CompilerServices.Unsafe**
   - Purpose: Low-level unsafe operations support
   - Permission Set: UNSAFE
   - Required by: System.Memory, System.Buffers

2. **System.Buffers**
   - Purpose: Buffer management and pooling
   - Permission Set: UNSAFE
   - Required by: System.Memory

3. **System.Numerics.Vectors**
   - Purpose: SIMD vector operations
   - Permission Set: UNSAFE
   - Required by: Vector math operations

### TIER 2: Memory Management

4. **System.Memory**
   - Purpose: Span<T>, Memory<T> types
   - Permission Set: UNSAFE
   - Dependencies: System.Runtime.CompilerServices.Unsafe, System.Buffers

5. **System.Runtime.InteropServices.RuntimeInformation**
   - Purpose: Runtime information and platform detection
   - Permission Set: UNSAFE

### TIER 3: Language & Reflection Support

6. **System.Collections.Immutable**
   - Purpose: Immutable collections
   - Permission Set: UNSAFE

7. **System.Reflection.Metadata**
   - Purpose: Metadata reading and manipulation
   - Permission Set: UNSAFE

### TIER 4: System Assemblies (from GAC)

8. **System.ServiceModel.Internals**
   - Purpose: Service model internal types
   - Permission Set: UNSAFE
   - Source: Copied from GAC to dependencies/

9. **SMDiagnostics**
   - Purpose: Service model diagnostics
   - Permission Set: UNSAFE
   - Source: Copied from GAC to dependencies/

10. **System.Drawing**
    - Purpose: Image processing and manipulation
    - Permission Set: UNSAFE
    - Source: Copied from GAC to dependencies/

11. **System.Runtime.Serialization**
    - Purpose: Data contract serialization
    - Permission Set: UNSAFE
    - Source: Copied from GAC to dependencies/

### TIER 5: High-Level Computation Libraries

12. **Newtonsoft.Json** (v13.0.4)
    - Purpose: JSON serialization/deserialization
    - Permission Set: UNSAFE
    - Note: Deployed as assembly for dependency resolution, runtime uses GAC version via binding redirect

13. **MathNet.Numerics** (v5.0.0)
    - Purpose: Advanced numerical computations
    - Permission Set: UNSAFE
    - Note: Pure managed surface area only

### TIER 6: Application Assembly

14. **SqlClrFunctions**
    - Purpose: Hartonomous CLR functions (vector ops, inference, etc.)
    - Permission Set: UNSAFE
    - Dependencies: All above assemblies
    - Source: Built from `src/SqlClr/SqlClrFunctions.csproj`

---

## Security Model

### Trusted Assembly Approach

Instead of `TRUSTWORTHY ON`, we use `sys.sp_add_trusted_assembly`:

1. **Calculate SHA-512 hash** of each assembly
2. **Add to trusted assembly list**:
   ```sql
   EXEC sys.sp_add_trusted_assembly @hash = 0x{SHA512_HASH}, @description = N'AssemblyName';
   ```
3. **Deploy assembly** with UNSAFE permission set

**Benefits**:
- Database remains `TRUSTWORTHY OFF`
- Only explicitly trusted assemblies can execute
- Better security posture for production

### Permission Set: UNSAFE

**Why UNSAFE?**
- Required for SIMD operations (System.Numerics.Vectors)
- Required for unsafe pointer operations in vector math
- Required for low-level memory access (Span<T>, Memory<T>)

**Risks**:
- Can access unmanaged code
- Can access file system
- Can call native APIs

**Mitigations**:
- Strong-name signing enforced
- Trusted assembly list (explicit approval)
- Code review before deployment
- No network access in CLR code
- No file system access in production code

---

## Deployment Process

### Step 1: Build SqlClrFunctions

```powershell
# Build Release configuration
dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release
```

Output: `src/SqlClr/bin/Release/net48/SqlClrFunctions.dll`

### Step 2: Extract Dependencies

```powershell
# Extract net4x dependencies (NOT netstandard)
.\scripts\extract-clr-dependencies.ps1 `
    -ProjectPath src/SqlClr/SqlClrFunctions.csproj `
    -OutputDirectory dependencies/ `
    -Verify
```

**Critical**: Dependencies MUST be from net4x folders to avoid assembly binding issues.

### Step 3: Deploy Assemblies

```powershell
# Deploy all 14 assemblies in order
.\scripts\deploy-clr-secure.ps1 -ServerName "." -DatabaseName "Hartonomous"
```

**Deployment Steps** (automated by script):
1. Verify SQL Server configuration (CLR enabled, strict security on)
2. Read each assembly file as binary
3. Calculate SHA-512 hash
4. Check if assembly already exists in database
5. If exists with different hash:
   - Drop existing assembly
   - Remove from trusted assembly list
6. Add new hash to trusted assembly list
7. Deploy assembly with UNSAFE permission set
8. Repeat for all 14 assemblies in dependency order

### Step 4: Verify Deployment

```sql
-- List deployed assemblies
SELECT 
    name,
    permission_set_desc,
    create_date,
    modify_date
FROM sys.assemblies
WHERE is_user_defined = 1
ORDER BY name;

-- Expected: 14 assemblies, all with permission_set_desc = 'UNSAFE'

-- List trusted assemblies
SELECT 
    hash,
    description,
    create_date
FROM sys.trusted_assemblies
ORDER BY description;

-- Expected: 14 entries matching deployed assemblies
```

---

## Assembly Binding Redirects

**Problem**: SQL Server CLR has strict assembly version matching. When SqlClrFunctions references `Newtonsoft.Json v13.0.0` but GAC has `v13.0.4`, deployment fails.

**Solution**: Configure `sqlservr.exe.config` with binding redirects.

### Configuration File

**Location**: 
- SQL Server 2022+: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Binn\sqlservr.exe.config`
- Adjust path for your SQL Server version

### Required Binding Redirects

Add to `<assemblyBinding>` section:

```xml
<dependentAssembly>
  <assemblyIdentity name="Newtonsoft.Json" 
                    publicKeyToken="30ad4fe6b2a6aeed" 
                    culture="neutral" />
  <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" 
                   newVersion="13.0.0.0" />
</dependentAssembly>

<dependentAssembly>
  <assemblyIdentity name="System.Memory" 
                    publicKeyToken="cc7b13ffcd2ddd51" 
                    culture="neutral" />
  <bindingRedirect oldVersion="0.0.0.0-4.0.1.2" 
                   newVersion="4.0.1.2" />
</dependentAssembly>

<!-- Add additional redirects as needed -->
```

**IMPORTANT**: Restart SQL Server after modifying `sqlservr.exe.config`.

See [SQL Server Binding Redirects](../reference/sqlserver-binding-redirects.md) for complete configuration.

---

## CLR Functions Reference

### Vector Operations

**Dot Product**:
```sql
SELECT dbo.VectorDotProduct(@vector1, @vector2)
```

**Cosine Similarity**:
```sql
SELECT dbo.CosineSimilarity(@vector1, @vector2)
```

**Euclidean Distance**:
```sql
SELECT dbo.EuclideanDistance(@vector1, @vector2)
```

### Spatial Operations

**Trilateration Projection** (high-dimensional → 3D GEOMETRY):
```sql
SELECT dbo.TrilaterationProjection(@embedding VARBINARY(MAX)) AS Geometry
```

### Multimodal Processing

**Text/Image/Audio Fusion**:
```sql
SELECT dbo.MultimodalFusion(@textEmbedding, @imageEmbedding, @audioEmbedding)
```

### Additional Functions

- Transformer inference operations
- Anomaly detection functions
- Stream orchestration UDTs
- JSON processing utilities

---

## Troubleshooting

### Assembly Not Found

**Error**: `Could not find assembly 'AssemblyName'`

**Solution**: Verify assembly exists in dependencies folder:
```powershell
Get-ChildItem dependencies/ | Select-Object Name
```

### Assembly Binding Error

**Error**: `Could not load file or assembly 'AssemblyName, Version=x.x.x.x'`

**Solution**: Add binding redirect to `sqlservr.exe.config` and restart SQL Server.

### UNSAFE Permission Denied

**Error**: `CREATE ASSEMBLY for assembly 'AssemblyName' failed because assembly is not authorized for PERMISSION_SET = UNSAFE`

**Solution**: Verify assembly is in trusted assembly list:
```sql
SELECT * FROM sys.trusted_assemblies WHERE description LIKE '%AssemblyName%'
```

If missing, add manually:
```sql
-- Calculate hash from assembly file
DECLARE @hash VARBINARY(64) = 0x{SHA512_HASH};
EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'AssemblyName';
```

### CLR Not Enabled

**Error**: `CLR integration is not enabled`

**Solution**:
```sql
sp_configure 'clr enabled', 1;
RECONFIGURE;
```

### Strict Security Error

**Error**: `Assembly is not trusted`

**Solution**: Enable CLR strict security and use trusted assemblies:
```sql
sp_configure 'clr strict security', 1;
RECONFIGURE;
```

---

## Performance

### SIMD Acceleration

- **AVX2**: Used on modern Intel/AMD CPUs (2013+)
- **SSE4**: Fallback for older CPUs
- **Performance**: 4-8x faster than scalar operations

### Latency

- **Vector dot product** (1536 dimensions): < 0.1ms
- **Cosine similarity**: < 0.15ms
- **Trilateration projection**: < 0.5ms

### Limitations

- **NO GPU acceleration** (ILGPU removed due to CLR verifier incompatibility)
- **CPU-bound**: Operations execute on SQL Server CPU cores
- **Memory**: Limited by SQL Server process memory

---

## Production Deployment

### Azure Arc SQL Server

For Azure Arc deployments, use managed identity authentication:

```powershell
# Deploy using Azure Arc connection
.\scripts\deploy-clr-secure.ps1 `
    -ServerName "arc-sql-server.domain.com" `
    -DatabaseName "Hartonomous" `
    -Credential (Get-Credential)
```

### Automated Deployments

For CI/CD pipelines:

```powershell
# Skip interactive prompts
.\scripts\deploy-clr-secure.ps1 `
    -ServerName $env:SQL_SERVER `
    -DatabaseName $env:DB_NAME `
    -SkipConfigCheck `
    -Rebuild
```

### Rollback

To remove CLR assemblies:

```sql
-- Drop in REVERSE dependency order
DROP ASSEMBLY SqlClrFunctions;
DROP ASSEMBLY [MathNet.Numerics];
DROP ASSEMBLY [Newtonsoft.Json];
-- ... continue in reverse order

-- Remove from trusted assemblies
EXEC sys.sp_drop_trusted_assembly @hash = 0x{SHA512_HASH};
```

---

## Source Code

- **CLR Functions**: `src/SqlClr/SqlClrFunctions.csproj`
- **Deployment Script**: `scripts/deploy-clr-secure.ps1`
- **Dependency Extraction**: `scripts/extract-clr-dependencies.ps1`
- **Dependencies**: `dependencies/` directory

---

## Additional Resources

- [SQL Server Binding Redirects](../reference/sqlserver-binding-redirects.md)
- [CLR Security Model](../security/clr-security.md)
- [Version Compatibility Matrix](../reference/version-compatibility.md)
- [Deployment Architecture](architecture.md)
