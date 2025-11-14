# SQL CLR Security Architecture

**Last Updated**: November 13, 2025  
**SQL Server Version**: 2025  
**Security Model**: Trusted Assembly (CLR Strict Security)

---

## Overview

Hartonomous SQL CLR assemblies execute with **UNSAFE** permission set to enable SIMD operations and low-level memory access. This document explains the security model, risks, mitigations, and production best practices.

---

## Security Model

### CLR Strict Security (SQL Server 2017+)

**Default Behavior** (2017+):
- All CLR assemblies are **untrusted by default**
- UNSAFE assemblies require explicit approval
- `TRUSTWORTHY ON` is **insufficient** for UNSAFE assemblies

**Trusted Assembly List**:
- Maintained in `sys.trusted_assemblies`
- Assembly hash (SHA-512) stored
- Only trusted assemblies can execute with UNSAFE

### Permission Sets

| Permission Set | Capabilities | Risk Level | Used By |
|----------------|-------------|------------|---------|
| **SAFE** | Pure managed code, no external access | Low | Not used |
| **EXTERNAL_ACCESS** | File system, network access | Medium | Not used |
| **UNSAFE** | Unmanaged code, full system access | **High** | **All 14 assemblies** |

**Why UNSAFE?**
- Required for `System.Numerics.Vectors` SIMD operations
- Required for `Span<T>` and `Memory<T>` unsafe pointer operations
- Required for AVX2/SSE4 intrinsics
- Required for MathNet.Numerics native interop

---

## Threat Model

### Attack Surface

**In-Process Execution**:
- CLR code runs in SQL Server process (`sqlservr.exe`)
- Full access to process memory
- Can call unmanaged code
- Can access file system
- Can access network

**SQL Injection → CLR Execution**:
- SQL injection can call CLR functions
- CLR functions can execute arbitrary code
- **Mitigations**: Input validation, parameterized queries, least privilege

**Assembly Tampering**:
- Malicious DLL replacement before deployment
- Hash verification fails → deployment blocked
- **Mitigations**: Strong-name signing, trusted assembly list, code signing

### Risks

**UNSAFE Permission Risks**:
1. **Memory Corruption**: Unsafe code can corrupt SQL Server memory
2. **File System Access**: Can read/write files on SQL Server host
3. **Network Access**: Can make outbound connections
4. **Native Code Execution**: Can call Win32 APIs, load native DLLs
5. **Process Manipulation**: Can access other threads, modify process state

**Deployment Risks**:
1. **Untrusted Dependencies**: Malicious NuGet packages
2. **Supply Chain Attacks**: Compromised dependency updates
3. **Privilege Escalation**: CLR runs as SQL Server service account

---

## Security Mitigations

### 1. Trusted Assembly List

**Implementation**:

```sql
-- Add assembly to trusted list
DECLARE @hash VARBINARY(64) = HASHBYTES('SHA2_512', @assemblyBytes);
EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'AssemblyName';
```

**Benefits**:
- Only pre-approved assemblies can execute
- Hash verification ensures assembly integrity
- Database remains `TRUSTWORTHY OFF`

**Verification**:

```sql
-- List all trusted assemblies
SELECT hash, description, create_date
FROM sys.trusted_assemblies
ORDER BY description;
```

### 2. Strong-Name Signing

**Purpose**: Verify assembly publisher identity

**Implementation**:
- All assemblies signed with `Hartonomous.snk`
- Signature verified at deployment time
- Prevents assembly spoofing

**Verification**:

```powershell
# Check assembly signature
$asm = [System.Reflection.Assembly]::LoadFile("SqlClrFunctions.dll")
$asm.GetName().GetPublicKey().Length  # Should be > 0
```

### 3. Code Review

**Required Before Deployment**:
- Review all SqlClrFunctions code changes
- Audit third-party dependencies
- Verify no file system access in production code
- Verify no network access in production code
- Review UNSAFE operations (pointers, SIMD)

**Key Files to Review**:
- `src/SqlClr/VectorOperations.cs`
- `src/SqlClr/TransformerInference.cs`
- `src/SqlClr/SpatialOperations.cs`
- `src/SqlClr/MultimodalProcessing.cs`

### 4. TRUSTWORTHY OFF

**Configuration**:

```sql
-- Verify TRUSTWORTHY is OFF
SELECT name, is_trustworthy_on 
FROM sys.databases 
WHERE name = 'Hartonomous';

-- Set TRUSTWORTHY OFF (if needed)
ALTER DATABASE Hartonomous SET TRUSTWORTHY OFF;
```

**Why TRUSTWORTHY OFF?**
- Prevents cross-database ownership chaining
- Limits privilege escalation
- Forces explicit trust via trusted assembly list

### 5. Least Privilege

**SQL Server Service Account**:
- Run SQL Server with **least privilege service account**
- Do NOT use Local System or Administrator
- Use **managed service account** (gMSA) on Windows
- Use **Azure Managed Identity** on Azure Arc

**Database Roles**:
- Grant minimal permissions to application users
- Restrict CLR function execution to specific roles
- Use application roles for web tier access

### 6. Network Isolation

**Production Configuration**:
- SQL Server on isolated network segment
- No internet access from SQL Server host
- Firewall rules restrict inbound/outbound traffic
- VPN or private link for remote access

### 7. Dependency Verification

**Before Deployment**:

```powershell
# Verify dependencies from official sources
Get-ChildItem dependencies/ | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    Write-Host "$($_.Name): $hash"
}
```

**Compare against known good hashes**:
- System.Memory: `<expected hash>`
- Newtonsoft.Json: `<expected hash>`
- MathNet.Numerics: `<expected hash>`

### 8. Audit Logging

**Enable CLR Auditing**:

```sql
-- Enable auditing for CLR assembly changes
CREATE SERVER AUDIT CLR_Assembly_Changes
TO FILE (FILEPATH = 'D:\SQLAudit\', MAXSIZE = 100MB, MAX_ROLLOVER_FILES = 10);

CREATE SERVER AUDIT SPECIFICATION CLR_Assembly_Audit
FOR SERVER AUDIT CLR_Assembly_Changes
ADD (ASSEMBLY_LOAD_EVENT),
ADD (DATABASE_ASSEMBLY_OPERATION_GROUP);

ALTER SERVER AUDIT CLR_Assembly_Changes WITH (STATE = ON);
ALTER SERVER AUDIT SPECIFICATION CLR_Assembly_Audit WITH (STATE = ON);
```

**Monitor CLR Execution**:

```sql
-- Query Extended Events for CLR execution
SELECT 
    event_data.value('(event/@timestamp)[1]', 'datetime') AS EventTime,
    event_data.value('(event/data[@name="statement"]/value)[1]', 'nvarchar(max)') AS Statement
FROM sys.fn_xe_file_target_read_file('CLR_Execution*.xel', NULL, NULL, NULL)
CROSS APPLY (SELECT CAST(event_data AS XML) AS event_data) AS ed
WHERE event_data.value('(event/@name)[1]', 'varchar(50)') = 'clr_execution';
```

---

## Production Deployment Checklist

### Pre-Deployment

- [ ] Code review completed
- [ ] All dependencies verified (SHA-256 hashes match known good values)
- [ ] Strong-name signatures verified
- [ ] No file system access in production code
- [ ] No network access in production code
- [ ] SQL Server service account uses least privilege
- [ ] TRUSTWORTHY OFF verified
- [ ] CLR strict security enabled
- [ ] Audit logging configured

### Deployment

- [ ] Deploy to test environment first
- [ ] Run security scan on deployed assemblies
- [ ] Verify trusted assembly list populated
- [ ] Test all CLR functions with production workload
- [ ] Monitor for errors in SQL Server error log
- [ ] Monitor for security warnings

### Post-Deployment

- [ ] Verify assembly hashes in `sys.trusted_assemblies`
- [ ] Review audit logs for unexpected CLR activity
- [ ] Performance baseline established
- [ ] Security scan clean (no vulnerabilities)
- [ ] Document deployment in change log

---

## Incident Response

### Suspected Compromise

**If CLR assembly tampering is suspected**:

1. **Isolate SQL Server**:
   - Disconnect from network
   - Block all inbound connections
   - Preserve logs

2. **Verify Assembly Integrity**:
   ```sql
   -- Check assembly hashes
   SELECT 
       a.name,
       a.permission_set_desc,
       t.hash,
       t.description
   FROM sys.assemblies a
   LEFT JOIN sys.trusted_assemblies t ON HASHBYTES('SHA2_512', a.content) = t.hash
   WHERE a.is_user_defined = 1;
   ```

3. **Review Audit Logs**:
   ```sql
   -- Check for unauthorized assembly changes
   SELECT * FROM sys.fn_get_audit_file('D:\SQLAudit\*.sqlaudit', DEFAULT, DEFAULT)
   WHERE action_id IN ('ASLR', 'ASLD', 'ASUP')  -- Assembly operations
   ORDER BY event_time DESC;
   ```

4. **Redeploy from Known Good Source**:
   ```powershell
   # Rebuild from source
   git clean -fdx src/SqlClr/
   dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release
   
   # Redeploy
   .\scripts\deploy-clr-secure.ps1 -ServerName "." -Rebuild
   ```

### Malicious CLR Execution

**If malicious CLR function execution is detected**:

1. **Disable CLR**:
   ```sql
   sp_configure 'clr enabled', 0;
   RECONFIGURE;
   ```

2. **Identify Attack Vector**:
   - Review application logs for SQL injection attempts
   - Check for unauthorized database logins
   - Analyze network traffic for C2 communication

3. **Drop Suspicious Assemblies**:
   ```sql
   DROP ASSEMBLY SuspiciousAssembly;
   EXEC sys.sp_drop_trusted_assembly @hash = 0x{HASH};
   ```

4. **Rotate Credentials**:
   - Change SQL Server service account password
   - Rotate application connection strings
   - Revoke compromised database logins

---

## Compliance

### Security Standards

**PCI DSS 4.0**:
- Requirement 6.2: Secure software development
- Requirement 6.3: Security vulnerabilities identified and addressed
- Requirement 10.2: Audit logging of privileged operations

**NIST 800-53**:
- CM-3: Configuration Change Control
- SA-10: Developer Security Testing
- SI-7: Software Integrity

**CIS Benchmark**:
- SQL Server 2022 CIS Benchmark v1.2
- Section 3.2: Surface Area Reduction
- Section 5.2: Audit Configuration

### Attestation

**Monthly Security Review**:
- Review trusted assembly list
- Verify no unauthorized assemblies deployed
- Review audit logs for CLR activity
- Update security documentation

**Annual Penetration Test**:
- Test SQL injection → CLR execution path
- Test assembly tampering detection
- Test incident response procedures

---

## Known Limitations

### ILGPU Removed

**Decision**: ILGPU v1.5.1 removed due to CLR verifier incompatibility.

**Impact**:
- No GPU acceleration for vector operations
- CPU SIMD only (AVX2/SSE4)
- Performance: 4-8x slower than GPU

**Rationale**:
- ILGPU requires UNSAFE + unverifiable MSIL
- SQL Server CLR verifier rejects unverifiable code
- Risk of SQL Server crashes > performance gain

**Alternative**: Offload GPU-accelerated inference to separate process or service that can use native libraries, with results passed back to SQL Server.

### No Native Libraries

**Restriction**: Cannot load native DLLs (e.g., CUDA, OpenBLAS).

**Rationale**:
- SQL Server CLR does not support native interop
- `DllImport` fails with `FileNotFoundException`
- Trusted assembly list only supports managed assemblies

**Alternative**: Use pure managed implementations (MathNet.Numerics, System.Numerics.Vectors).

---

## References

- [SQL Server CLR Integration Security](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/security/clr-integration-security)
- [CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- [Trusted Assemblies](https://learn.microsoft.com/en-us/sql/t-sql/system-stored-procedures/sys-sp-add-trusted-assembly-transact-sql)
- [UNSAFE Assemblies](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/security/clr-integration-code-access-security)

---

## Additional Resources

- [CLR Deployment Guide](../deployment/clr-deployment.md)
- [SQL Server Binding Redirects](../reference/sqlserver-binding-redirects.md)
- [Version Compatibility Matrix](../reference/version-compatibility.md)
