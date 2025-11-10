# SQL CLR Deployment

**Technical documentation for deploying .NET Framework 4.8.1 CLR assemblies with UNSAFE permissions to SQL Server 2025.**

## Current Deployment State

**Deployed Assemblies (7 total):**

| Assembly | Source Path | Purpose | Permission |
| --- | --- | --- | --- |
| `System.Numerics.Vectors.dll` | `dependencies/System.Numerics.Vectors.dll` | SIMD intrinsics used by `VectorMath` | `UNSAFE` |
| `MathNet.Numerics.dll` | `dependencies/MathNet.Numerics.dll` | Linear algebra + SVD routines | `UNSAFE` |
| `Newtonsoft.Json.dll` | `dependencies/Newtonsoft.Json.dll` | JSON serialization helpers for CLR functions | `UNSAFE` |
| `System.ServiceModel.Internals.dll` | copied from `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` via `scripts/copy-dependencies.ps1` | Required by `System.Runtime.Serialization` | `UNSAFE` |
| `SMDiagnostics.dll` | copied from `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` | WCF diagnostics plumbing | `UNSAFE` |
| `System.Runtime.Serialization.dll` | copied from `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` | Binary formatter + contract serializers | `UNSAFE` |
| `SqlClrFunctions.dll` | `dependencies/SqlClrFunctions.dll` (built from `src/SqlClr`) | Hartonomous CLR entry point | `UNSAFE` |

To verify the current state run:

```sql
SELECT name, permission_set_desc 
FROM sys.assemblies 
WHERE is_user_defined = 1;
```

**SQL Server Configuration (after `deploy-database-unified.ps1`):**

```sql
clr enabled = 1
clr strict security = 0  -- Unified script disables it for local development
```

> ℹ️ **Production Guidance:** `scripts/deploy-clr-secure.ps1` keeps `clr strict security = 1` and registers hashes with `sys.sp_add_trusted_assembly`. Use the secure script for hardened environments.

## Why UNSAFE Permission is Required

### 1. CLR Strict Security Behaviour

`scripts/deploy-database-unified.ps1` streams assemblies with `CREATE ASSEMBLY ... FROM 0x...` and temporarily sets `clr strict security = 0`. When we switch back to the hardened path (`scripts/deploy-clr-secure.ps1`) SQL Server 2025 will treat every user assembly as `UNSAFE` unless the SHA-512 hash is registered through `sys.sp_add_trusted_assembly`. The Hartonomous CLR surface depends on that higher privilege to keep pointer math, SIMD intrinsics, and P/Invoke available. The secure script keeps strict security enabled and is the target configuration for production.

**Reference:** [CLR strict security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)

### 2. System.Numerics.Vectors — SIMD Intrinsics

`VectorMath` inside `src/SqlClr/Core/VectorMath.cs` uses `unsafe` blocks to load AVX2 registers directly.

- Direct pointer arithmetic via `fixed` statements
- Unmanaged memory reads/writes for `Avx.LoadVector256`
- Hardware intrinsics that require unverifiable IL

```csharp
private static unsafe float DotProductAvx2(ReadOnlySpan<float> a, ReadOnlySpan<float> b, int length)
{
    Vector256<float> vSum = Vector256<float>.Zero;
    int simdLength = length & ~7;

    fixed (float* pA = a, pB = b)
    {
        for (var i = 0; i < simdLength; i += 8)
        {
            var va = Avx.LoadVector256(pA + i);
            var vb = Avx.LoadVector256(pB + i);
            vSum = Avx.Add(vSum, Avx.Multiply(va, vb));
        }
    }

    return HorizontalSum(vSum);
}
```

### 3. Newtonsoft.Json — High-Performance Serialization

`Newtonsoft.Json` exposes zero-allocation readers that rely on spans and unsafe casts.

- Zero-allocation span operations
- Unsafe pointer casts for UTF-8 encoding
- Direct memory manipulation on pooled buffers

These `Span&lt;T&gt;` operations compile to unverifiable IL and therefore require `UNSAFE`.

### 4. MathNet.Numerics — Native Providers

`MathNet.Numerics` optionally bridges to Intel MKL or OpenBLAS via P/Invoke. Even when the native provider is absent, the assembly contains unverifiable interop stubs that force the `UNSAFE` permission.

### 5. Framework Dependencies

- `System.Runtime.Serialization.dll`: binary formatter pieces rely on unmanaged serialization buffers.
- `SMDiagnostics.dll`: WCF tracing uses unsafe formatting helpers.
- `System.ServiceModel.Internals.dll`: supplies native channel glue for the serialization stack.

## Deployment Process

### Local Development (`deploy-database-unified.ps1`)

1. Build the CLR project:

   ```powershell
   dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release
   ```

2. Run the unified deployment script:

   ```powershell
   .\scripts\deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous" -SkipCLR:$false
   ```

3. The script:

   - Enables CLR (`clr enabled = 1`) and temporarily disables strict security (`clr strict security = 0`).
   - Reads each DLL into hex and executes `CREATE ASSEMBLY ... WITH PERMISSION_SET = UNSAFE`.
   - Leaves `TRUSTWORTHY` OFF but does **not** register hashes with `sys.trusted_assemblies`.
   - Executes binding scripts under `sql/procedures/Common.ClrBindings.sql` to publish functions, aggregates, and UDTs.

### Hardened Deployment (`deploy-clr-secure.ps1`)

This script is the production target once strong-name signing is in place (see `scripts/CLR_SECURITY_ANALYSIS.md`). It expects the assemblies in `src/SqlClr/bin/Release` to be signed.

1. Build signed binaries (pending work tracked in `docs/REFACTOR_TARGET.md`).
2. Execute:

   ```powershell
   .\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "Hartonomous" -BinDirectory "src\SqlClr\bin\Release"
   ```

3. The script keeps `clr strict security = 1`, adds each assembly hash via `sys.sp_add_trusted_assembly`, guarantees `TRUSTWORTHY OFF`, and recreates all CLR objects in dependency order.

## Performance Characteristics

### SIMD Acceleration (System.Numerics.Vectors)

- **AVX2**: Processes 8 floats per instruction
- **AVX-512**: Processes 16 floats per instruction (when available)
- **Speedup**: 8-16x over scalar operations for large vectors

**Benchmark (1998-dimension vectors):**

- Scalar: ~2000 ops/sec
- AVX2 SIMD: ~18,500 ops/sec (9.25x)
- AVX-512 SIMD: ~35,000 ops/sec (17.5x)

### JSON Serialization (Newtonsoft.Json)

- **ref readonly optimization**: Zero-allocation for read-only scenarios
- **UTF-8 direct encoding**: Unsafe pointer operations avoid string allocations
- **Throughput**: ~500 MB/sec for large payloads

## Security Considerations

### Current Development Posture

- `deploy-database-unified.ps1` disables `clr strict security` and streams assemblies directly with `CREATE ASSEMBLY ... FROM 0x...`.
- Assemblies are **not** strong-name signed today; hashes are not registered with `sys.trusted_assemblies`.
- `TRUSTWORTHY` remains OFF and should stay that way to avoid elevating database principals.
- Operational safeguards (network ACLs, SQL permissions) must compensate for the broader `UNSAFE` surface.

### Hardened Target

1. Strong-name sign `SqlClrFunctions.dll` and dependencies (tracked in `scripts/CLR_SECURITY_ANALYSIS.md`).
2. Use `deploy-clr-secure.ps1` to keep `clr strict security = 1`, register SHA-512 hashes through `sys.sp_add_trusted_assembly`, and ensure `TRUSTWORTHY OFF`.
3. Limit execution to the SQL login mapped to the Hartonomous service identity; grant `UNSAFE ASSEMBLY` explicitly.
4. Monitor CLR events via the `AI_Operations` Extended Event session defined in `scripts/deploy/04-clr-assembly.ps1`.

### Why We Still Target Strict Security

- SQL Server treats all user assemblies as `UNSAFE` when strict security is enabled; we rely on those capabilities for SIMD, P/Invoke, and FILESTREAM interop.
- Keeping strict security ON prevents elevation through untrusted databases and aligns with Microsoft guidance.
- Gap closure work (strong-name + trusted hashes) is documented in `docs/REFACTOR_TARGET.md` and `scripts/CLR_SECURITY_ANALYSIS.md`.

## Troubleshooting

### Error: "Assembly is not authorized for PERMISSION_SET = UNSAFE"

**Cause:** `clr strict security` is ON without a matching entry in `sys.trusted_assemblies`, or the executing login lacks `UNSAFE ASSEMBLY`.

**Fix:**

- Development: rerun `deploy-database-unified.ps1 -SkipCLR:$false` (it disables strict security before creating assemblies).
- Hardened: compute the SHA-512 hash and register it:

   ```sql
   DECLARE @hash VARBINARY(64) = 0x<SHA512>;
   EXEC sys.sp_add_trusted_assembly @hash, N'SqlClrFunctions';
   ```

- Ensure the login has `UNSAFE ASSEMBLY`:

   ```sql
   USE master;
   GRANT UNSAFE ASSEMBLY TO [HartonomousServiceLogin];
   ```

### Error: "Could not load assembly 'System.Numerics.Vectors'"

**Cause:** A prerequisite DLL is missing, copied to the wrong path, or was dropped during a redeploy.

**Fix:** Deploy in the documented order:

1. `System.Numerics.Vectors`
2. `MathNet.Numerics`
3. `Newtonsoft.Json`
4. `System.ServiceModel.Internals`, `SMDiagnostics`, `System.Runtime.Serialization`
5. `SqlClrFunctions`

If you are using `deploy-database-unified.ps1`, confirm that `scripts/copy-dependencies.ps1` populated the `dependencies/` folder before running the deploy script.

### Error: "The database owner SID recorded in the master database does not match"

**Cause:** The database was restored from another server and the owner SID differs.

**Fix:**

```sql
USE Hartonomous;
EXEC sp_changedbowner 'sa';
```

## Future Enhancements

### Optional GPU Acceleration (ILGPU)

**Status:** Planned, not yet implemented

**Architecture:**

- ILGPU.NET for GPU kernel compilation
- Fallback to CPU SIMD when GPU unavailable
- Requires UNSAFE for GPU memory interop

**Stub exists:** `GpuVectorAccelerator.cs` with TODO comments

**When implemented:**

- Deployment: Add `ILGPU.dll` and `ILGPU.Algorithms.dll` to assembly list
- Performance: 100-1000x for large batch operations (>1M vectors)
- Hardware: Requires NVIDIA/AMD GPU with compute capability

**Documentation:** See [REFACTOR_TARGET.md](REFACTOR_TARGET.md) for planned GPU integration.

## References

- [SQL Server CLR Integration](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/common-language-runtime-integration-overview)
- [CLR strict security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- [CREATE ASSEMBLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-assembly-transact-sql)
- [sys.sp_add_trusted_assembly](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sys-sp-add-trusted-assembly-transact-sql)
- [System.Numerics.Vectors](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vectors)
- [Newtonsoft.Json Performance](https://www.newtonsoft.com/json/help/html/Performance.htm)
