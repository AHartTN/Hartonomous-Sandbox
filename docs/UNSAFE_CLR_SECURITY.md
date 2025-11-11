# UNSAFE CLR Security Model

## Executive Summary

Hartonomous SQL CLR assemblies require **UNSAFE** permission level due to use of:
- **ILGPU** for GPU acceleration (CUDA/OpenCL device access)
- **SIMD intrinsics** (System.Numerics.Vectors AVX2/AVX-512)
- **MathNet.Numerics** (unmanaged memory allocation for matrix operations)
- **FILESTREAM** (memory-mapped file access for tensor payloads)
- **Newtonsoft.Json** (zero-allocation span operations)

This document outlines the security implications, deployment options, and production best practices.

---

## What is UNSAFE CLR?

SQL Server CLR assemblies can have three permission levels:

| Permission | Description | Restrictions |
|------------|-------------|--------------|
| **SAFE** | Verifiable managed code only | No external resource access, no unmanaged code, no P/Invoke |
| **EXTERNAL_ACCESS** | Managed code with external resource access | Files, network, registry (but still managed code only) |
| **UNSAFE** | Unrestricted | Can call unmanaged code, access memory directly, full system access |

**CRITICAL:** As of SQL Server 2017+, `clr strict security` is **ON by default**, which treats SAFE and EXTERNAL_ACCESS as UNSAFE. This means **all CLR assemblies** are now subject to strict trust requirements.

---

## Why Hartonomous Requires UNSAFE

### 1. ILGPU GPU Acceleration (NEW)
- **Library:** ILGPU 1.5.1
- **Reason:** Direct GPU device access (CUDA/OpenCL) requires unmanaged memory allocation
- **Impact:** 100-1000x performance improvement for large vectors (10K+ dimensions)
- **Fallback:** Automatic CPU fallback if GPU unavailable

### 2. SIMD Intrinsics (AVX2/AVX-512)
- **Library:** System.Numerics.Vectors
- **Reason:** Hardware SIMD instructions require unsafe memory operations
- **Impact:** 8-16x performance improvement for vector operations (1998D embeddings)

### 3. MathNet.Numerics
- **Library:** MathNet.Numerics 5.0.0
- **Reason:** Matrix factorization (SVD, QR) uses unmanaged BLAS/LAPACK routines
- **Functions:** Transformer attention, dimensionality reduction, anomaly detection

### 4. FILESTREAM
- **API:** SqlFileStream (memory-mapped files)
- **Reason:** Direct file system access for tensor atom payloads (multi-GB data)
- **Alternative:** Loading multi-GB blobs into SQL memory would cause OOM

### 5. Newtonsoft.Json
- **Library:** Newtonsoft.Json 13.0.4
- **Reason:** High-performance JSON serialization with Span<T> (zero-allocation)
- **Functions:** Model ingestion, generation, semantic analysis

**NONE of these libraries can be used with SAFE or EXTERNAL_ACCESS.**

---

## Security Implications

### Attack Scenario (TRUSTWORTHY ON - CRITICAL RISK)

If `TRUSTWORTHY ON` is enabled and an attacker gains limited database access:

1. **Attacker creates malicious CLR assembly** with UNSAFE permissions
2. **Assembly executes with SQL Server service account privileges** (typically `NT SERVICE\MSSQLSERVER` or domain admin)
3. **Attacker gains full server access:** file system, network shares, Active Directory, other databases
4. **Lateral movement:** Can compromise entire domain from SQL Server host

**Example malicious code:**
```csharp
[SqlProcedure]
public static void EvilCode()
{
    // Attacker now has SQL Server service account privileges
    System.IO.File.WriteAllText(@"C:\Windows\System32\hack.exe", maliciousPayload);
    System.Diagnostics.Process.Start(@"C:\Windows\System32\hack.exe");
}
```

### Mitigation: Trusted Assembly List (SECURE)

Use `sys.sp_add_trusted_assembly` with SHA-512 hash verification:

```sql
-- Secure deployment (PRODUCTION ONLY)
EXEC sys.sp_add_trusted_assembly 
    @hash = 0x<SHA512_HASH_OF_DLL>,
    @description = 'Hartonomous SQL CLR Functions v1.0.0';
```

**Why this is secure:**
- **Hash-based trust:** Only assemblies with matching SHA-512 hash can be loaded
- **Server-level trust:** Applies to all databases, preventing privilege escalation
- **No TRUSTWORTHY flag:** Database cannot grant elevated permissions

---

## Deployment Options

### Option 1: Trusted Assembly (PRODUCTION - RECOMMENDED)

**Script:** `scripts/deploy-clr-secure.ps1`

**Advantages:**
- ✅ Secure (no TRUSTWORTHY ON)
- ✅ Production-ready
- ✅ SHA-512 hash verification
- ✅ Server-level trust (applies to all databases)

**Disadvantages:**
- ❌ Requires `CONTROL SERVER` permission
- ❌ Cannot be used on Azure SQL Database (cloud)
- ❌ Hash must be updated on every DLL rebuild

**Deployment:**
```powershell
.\scripts\deploy-clr-secure.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -DllPath ".\src\SqlClr\bin\Release\SqlClrFunctions.dll"
```

**What it does:**
1. Computes SHA-512 hash of `SqlClrFunctions.dll`
2. Adds hash to trusted assembly list
3. Drops/creates assembly with UNSAFE permissions
4. Creates all CLR function bindings

---

### Option 2: TRUSTWORTHY ON (DEVELOPMENT ONLY - INSECURE)

**Script:** `scripts/deploy-clr-multi-assembly.ps1` (LEGACY)

**Advantages:**
- ✅ Fast iteration (no hash updates)
- ✅ Works for local development

**Disadvantages:**
- ❌ **CRITICAL SECURITY RISK** (see attack scenario above)
- ❌ **NEVER USE IN PRODUCTION**
- ❌ Violates security best practices

**Deployment:**
```powershell
.\scripts\deploy-clr-multi-assembly.ps1 `
    -Server "localhost" `
    -Database "Hartonomous_DEV"
```

**⚠️ WARNING:** Only use on isolated development machines with no production data.

---

## Production Deployment Checklist

### Pre-Deployment
- [ ] Verify CLR strict security is ON: `SELECT * FROM sys.configurations WHERE name = 'clr strict security'` (value = 1)
- [ ] Verify CLR enabled: `SELECT * FROM sys.configurations WHERE name = 'clr enabled'` (value = 1)
- [ ] Compile `SqlClrFunctions.dll` in **Release** mode
- [ ] Test DLL on staging environment first

### Deployment
- [ ] Use `deploy-clr-secure.ps1` (trusted assembly method)
- [ ] Verify TRUSTWORTHY is OFF: `SELECT name, is_trustworthy_on FROM sys.databases WHERE name = 'Hartonomous'` (is_trustworthy_on = 0)
- [ ] Verify assembly deployed: `SELECT * FROM sys.assemblies WHERE name = 'SqlClrFunctions'` (permission_set_desc = 'UNSAFE')
- [ ] Verify trusted assembly registered: `SELECT * FROM sys.trusted_assemblies WHERE description LIKE '%Hartonomous%'`

### Post-Deployment Validation
- [ ] Test GPU detection: `SELECT dbo.clr_VectorDotProduct(0x..., 0x...)`
- [ ] Verify GPU fallback: Test on machine without GPU (should automatically use CPU)
- [ ] Test FILESTREAM functions: `EXEC dbo.clr_StoreTensorAtomPayload @tensorAtomId = 1, @payload = 0x...`
- [ ] Test JSON conversion: `SELECT dbo.clr_BytesToFloatArrayJson(0x...)`

---

## On-Premise Requirement

**UNSAFE CLR is NOT supported on Azure SQL Database** due to security restrictions.

**Supported platforms:**
- ✅ SQL Server 2019+ (on-premise)
- ✅ SQL Server 2022+ (on-premise)
- ✅ SQL Server 2025 (on-premise) - **REQUIRED FOR .NET FRAMEWORK 4.8.1 CLR**
- ✅ Azure SQL Managed Instance (with limitations)
- ❌ Azure SQL Database (cloud) - **NOT SUPPORTED**

**Why on-premise only?**
- Cloud SQL Database does not allow UNSAFE assemblies for security reasons
- Azure SQL Managed Instance supports CLR but with restricted UNSAFE permissions
- Hartonomous requires full UNSAFE for ILGPU/SIMD/FILESTREAM

---

## .NET Framework 4.8.1 Constraint

**CRITICAL:** SQL Server CLR **ONLY** supports .NET Framework 4.8.1. It does NOT support:
- ❌ .NET Core
- ❌ .NET 5+
- ❌ .NET 6/7/8/9/10

**Why?**
- SQL Server CLR is based on .NET Framework CLR runtime (not CoreCLR)
- ILGPU 1.5.1 is the last version supporting .NET Framework 4.8.1
- This is an **architectural limitation**, not a bug

**Implications:**
- Modern .NET services (Hartonomous.Api, Hartonomous.Workers) can use .NET 10 with latest ILGPU
- SQL CLR is limited to .NET Framework 4.8.1 + ILGPU 1.5.1
- GPU acceleration in SQL CLR uses ILGPU 1.5.1 (CUDA 11.x, OpenCL 2.0)

---

## Migration from TRUSTWORTHY ON → Trusted Assembly

If you currently use TRUSTWORTHY ON (legacy development setup), migrate to secure deployment:

### Step 1: Backup Current Assembly
```sql
-- Export current assembly to file
DECLARE @dll VARBINARY(MAX);
SELECT @dll = content FROM sys.assembly_files WHERE assembly_id = (SELECT assembly_id FROM sys.assemblies WHERE name = 'SqlClrFunctions');
-- Save @dll to file system
```

### Step 2: Drop Old Assembly
```sql
-- Drop all CLR functions first
DROP FUNCTION dbo.clr_VectorDotProduct;
DROP FUNCTION dbo.clr_CosineSimilarity;
-- ... (drop all 40+ functions)

-- Drop assembly
DROP ASSEMBLY SqlClrFunctions;
```

### Step 3: Disable TRUSTWORTHY
```sql
ALTER DATABASE Hartonomous SET TRUSTWORTHY OFF;
```

### Step 4: Deploy Using Secure Script
```powershell
.\scripts\deploy-clr-secure.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -DllPath ".\SqlClrFunctions.dll"
```

### Step 5: Verify Security
```sql
-- TRUSTWORTHY should be OFF
SELECT name, is_trustworthy_on FROM sys.databases WHERE name = 'Hartonomous';

-- Assembly should be in trusted list
SELECT * FROM sys.trusted_assemblies WHERE description LIKE '%Hartonomous%';

-- Assembly should be UNSAFE
SELECT name, permission_set_desc FROM sys.assemblies WHERE name = 'SqlClrFunctions';
```

---

## GPU Acceleration Security Considerations

### ILGPU Device Access
- **Risk:** Direct GPU memory access could be exploited for side-channel attacks
- **Mitigation:** ILGPU only accesses GPU memory, not system memory or other processes
- **Isolation:** Each SQL Server session gets isolated GPU memory buffers

### Automatic Fallback
- **Safety:** If GPU is unavailable or ILGPU initialization fails, code automatically falls back to CPU
- **No errors:** Users never see GPU-related errors, ensuring stability

### Performance vs. Security Trade-off
| Configuration | Performance | Security |
|---------------|-------------|----------|
| **GPU + UNSAFE** | ⚡⚡⚡ Fastest (100-1000x) | ⚠️ Requires trusted assembly |
| **CPU SIMD + UNSAFE** | ⚡⚡ Fast (8-16x) | ⚠️ Requires trusted assembly |
| **CPU Scalar (no CLR)** | ⚡ Baseline (1x) | ✅ No UNSAFE required |

**Recommendation:** Use GPU acceleration in production with trusted assembly deployment.

---

## Monitoring and Auditing

### Monitor GPU Usage
```sql
-- Check if GPU is being used
SELECT 
    session_id,
    command,
    cpu_time,
    reads,
    writes
FROM sys.dm_exec_requests
WHERE command LIKE '%clr_Vector%';
```

### Audit CLR Assembly Changes
```sql
-- Enable audit logging
CREATE SERVER AUDIT CLR_Audit
TO FILE (FILEPATH = 'C:\Audit\')
WITH (QUEUE_DELAY = 1000, ON_FAILURE = CONTINUE);

CREATE SERVER AUDIT SPECIFICATION CLR_Audit_Spec
FOR SERVER AUDIT CLR_Audit
    ADD (ASSEMBLY_LOAD_EVENT)
WITH (STATE = ON);
```

### Alert on Trusted Assembly Changes
```sql
-- Create alert for trusted assembly modifications
EXEC msdb.dbo.sp_add_alert 
    @name = 'CLR_Trusted_Assembly_Modified',
    @message_id = 0,
    @severity = 0,
    @enabled = 1,
    @delay_between_responses = 60,
    @include_event_description_in = 1,
    @category_name = 'Security';
```

---

## FAQ

### Q: Why not use SQL Server 2025 VECTOR type instead of CLR?
**A:** Native VECTOR type only supports basic operations (distance, similarity). Hartonomous requires:
- Transformer inference (multi-head attention, feed-forward)
- Dimensionality reduction (SVD, t-SNE)
- Anomaly detection (Isolation Forest, Mahalanobis distance)
- GPU acceleration (100-1000x performance)

### Q: Can I use Azure SQL Database?
**A:** No. UNSAFE CLR is not supported in Azure SQL Database. Use Azure SQL Managed Instance or on-premise SQL Server 2025.

### Q: What if I don't have a GPU?
**A:** GpuAccelerator automatically detects GPU availability and falls back to CPU SIMD code. No configuration required.

### Q: How do I update the assembly after code changes?
**A:** Re-run `deploy-clr-secure.ps1`. It computes the new SHA-512 hash and updates the trusted assembly list.

### Q: What if an attacker modifies SqlClrFunctions.dll?
**A:** The SHA-512 hash check will fail and the assembly will be rejected. This is the core security benefit of trusted assemblies.

---

## References

- [SQL Server CLR Integration Security](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/security/clr-integration-security)
- [sys.sp_add_trusted_assembly](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sys-sp-add-trusted-assembly-transact-sql)
- [CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- [ILGPU Documentation](https://github.com/m4rs-mt/ILGPU)
- [MathNet.Numerics](https://numerics.mathdotnet.com/)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-11-11 | Initial documentation covering UNSAFE requirements, deployment options, security implications |
| 1.1.0 | 2025-11-11 | Added ILGPU GPU acceleration security considerations, device detection, automatic fallback |

---

**Document Owner:** Hartonomous Platform Team  
**Last Updated:** November 11, 2025  
**Classification:** Internal Use Only
