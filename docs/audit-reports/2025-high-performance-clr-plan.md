# High-Performance SQL CLR Deployment Plan

**Objective:** integrate ILGPU-driven GPU acceleration and MathNet-based numerics into the SQL Server CLR sandbox while targeting .NET Framework 4.8.1 and maintaining deterministic assembly resolution.

---

## 1. Context & Non-Negotiable Constraints

- **Runtime:** SQL Server 2025 hosting CLR under .NET Framework 4.8.1 (no .NET Standard facades).
- **Libraries:** ILGPU, MathNet.Numerics, System.Numerics.Vectors.
- **Permissions:** all assemblies must deploy with `PERMISSION_SET = UNSAFE` because ILGPU allocates unmanaged GPU buffers and uses pointer arithmetic.
- **Host Configuration:** `sqlservr.exe.config` must provide binding redirects to pin every System.* dependency to the private versions shipped with the database.
- **Version Locking:** only consume package builds that expose explicit `net4x` targets; remove package options that surface purely through .NET Standard.

Failure to meet any of these constraints results in load failures (MVID mismatches, missing metadata) or security hard blocks (GPU access denied under SAFE/EXTERNAL_ACCESS).

---

## 2. Dependency Selection Matrix

| Assembly | Locked Version | Upstream Consumer | Target Framework Folder | Notes |
|----------|----------------|-------------------|--------------------------|-------|
| ILGPU | 0.9.2 | SQL CLR entry assembly | `lib/net47` | Last ILGPU build with .NET Framework surface; includes CPU and GPU accelerators. |
| MathNet.Numerics | 5.0.0 | SQL CLR entry assembly | `lib/net461` | Provides SVD, matrix ops without extra facade packages when targeting 4.8.1. |
| System.Numerics.Vectors | 4.5.0 | ILGPU + MathNet | `lib/net45` | Ensures deterministic SIMD type identity rather than relying on GAC copy. |
| System.Collections.Immutable | 1.7.1 | ILGPU | `lib/net461` | Required for ILGPU kernel compilation pipeline. |
| System.Reflection.Metadata | 1.8.0 | ILGPU | `lib/net461` | Supplies metadata reader used by ILGPU. |
| System.Runtime.CompilerServices.Unsafe | 4.7.1 | ILGPU + System.Memory | `lib/net461` | Highest-risk collision with GAC; must ship privately. |
| System.Buffers | 4.5.1 | System.Memory | `lib/net461` | Provides span-based buffer helpers. |
| System.Memory | 4.5.4 | ILGPU | `lib/net461` | Enables span and memory abstractions for ILGPU allocators. |

**Acquisition:** pull each package via NuGet, extract the `lib/net4*` DLLs, and copy into `dependencies/sqlclr-ilgpu/` (or an equivalent staging directory used by deployment scripts). Do not reference the .NET Standard TFM folders.

---

## 3. Packaging & Build Pipeline Changes

1. **SqlClr project file:** ensure `SqlClrFunctions.csproj` (or the assembly entry project) references the DLLs from the staged dependency directory with `Private=true` so that the MSBuild output contains every dependency.
2. **Copy script:** extend `scripts/copy-dependencies.ps1` to include the versions above and emit warnings if any DLL originates from a `.NETStandard` folder.
3. **Hashing:** update `scripts/deploy-clr-secure.ps1` so SHA-512 hashes are calculated for the new dependency set when registering trusted assemblies.
4. **Source control:** commit the dependency manifest (JSON or PowerShell data file) that pins package IDs, versions, and expected SHA-512 hashes for integrity verification.

---

## 4. SQL Server Deployment Sequence

1. **Enable CLR and set permissions:**

  ```sql
  EXEC sp_configure 'clr enabled', 1;
  RECONFIGURE;
  EXEC sp_configure 'clr strict security', 1; -- keep enabled for production
  RECONFIGURE;
  ```

1. **Assembly registration order:** follow the dependency-first approach to avoid missing metadata exceptions:
   Grant the SQL login used for deployment the `UNSAFE ASSEMBLY` permission or register hashes via `sys.sp_add_trusted_assembly` when `clr strict security` remains on.

1. **Assembly registration order:** follow the dependency-first approach to avoid missing metadata exceptions:

   | Step | Assembly | Permission | Depends On |
   |------|----------|------------|------------|
   | 1 | `System.Buffers.dll` | UNSAFE | — |
   | 2 | `System.Runtime.CompilerServices.Unsafe.dll` | UNSAFE | 1 |
   | 3 | `System.Memory.dll` | UNSAFE | 1, 2 |
   | 4 | `System.Numerics.Vectors.dll` | UNSAFE | — |
   | 5 | `System.Collections.Immutable.dll` | UNSAFE | 1–3 |
   | 6 | `System.Reflection.Metadata.dll` | UNSAFE | 5 |
   | 7 | `MathNet.Numerics.dll` | UNSAFE | 4 |
   | 8 | `ILGPU.dll` | UNSAFE | 1–7 |
   | 9 | `SqlClrFunctions.dll` (project output) | UNSAFE | 1–8 |

   Use `CREATE ASSEMBLY ... FROM 0x... WITH PERMISSION_SET = UNSAFE;` for each binary. For production, pre-register all hashes with `sys.sp_add_trusted_assembly` before running the CREATE statements.

1. **Automation:** encapsulate the sequence in an idempotent T-SQL script (`sql/procedures/Internal.DeployIlgpuStack.sql`) invoked by PowerShell deployment tooling.

---

## 5. Host-Level Binding Redirects

SQL CLR respects only the configuration file of the hosting process (`sqlservr.exe.config`). Add explicit binding redirects so the loader prefers the database-scoped assemblies rather than GAC copies.

### 5.1 Locate the Binn Directory

Run:

```sql
SELECT [filename]
FROM sys.dm_server_services
WHERE servicename LIKE N'SQL Server (%';
```

Use the returned path to place or update `sqlservr.exe.config` (for example `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Binn`).

### 5.2 Required Configuration Snippet

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <runtime>

    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.7.0.0" newVersion="4.7.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Collections.Immutable" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-1.7.0.0" newVersion="1.7.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Reflection.Metadata" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-1.7.99.99" newVersion="1.8.0.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Buffers" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.5.0.0" newVersion="4.5.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Memory" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.5.3.0" newVersion="4.5.4.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Numerics.Vectors" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.4.99.99" newVersion="4.5.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
```

Restart the SQL Server service (preferred) or run `DBCC FREESYSTEMCACHE('ALL');` to ensure the CLR host reloads the configuration.

### 5.3 Configuration Management

- Track `sqlservr.exe.config` in infrastructure-as-code repositories and replicate across all HA/DR nodes.
- Add a deployment gate that verifies checksum equality before permitting database failover.

---

## 6. Verification & Diagnostics

1. **Assembly resolution smoke test:** create a lightweight stored procedure that executes `SELECT ASSEMBLYPROPERTY('System.Runtime.CompilerServices.Unsafe', 'Version');` to confirm the runtime version resolves to `4.7.1.0`.
2. **GPU availability check:** expose a CLR function (for example `dbo.clr_GetGpuInfo`) that instantiates an ILGPU context and returns the accelerator list. Failures usually indicate missing UNSAFE permissions or driver issues.
3. **MVID collision detection:**

    ```sql
    SELECT olm.name, olm.file_version
    FROM sys.dm_os_loaded_modules AS olm
    WHERE olm.name LIKE N'%System.Runtime.CompilerServices.Unsafe%';
    ```

    Expect the path to point to `sys.sysobjvalues` (database-scoped storage), not `C:\Windows\Microsoft.NET\Framework64`.
4. **Resource leak detection:** configure Extended Events for `clr_allocation_failure` and `clr_virtual_alloc_failure` to monitor for GPU or unmanaged buffer leaks introduced by ILGPU kernels.

---

## 7. Operational Risk Mitigation

- **GAC override risk:** private deployment plus binding redirects prevent SQL Server from loading patched GAC binaries whose MVID differs from the packaged version.
- **UNSAFE surface risk:** enforce code review and automated static analysis for all CLR code paths; ensure every ILGPU accelerator is disposed in `finally` blocks or via `using` statements.
- **Configuration drift:** treat `sqlservr.exe.config` as a tier-0 artifact. Add a drift detector that validates file hashes hourly on primary and secondary replicas.
- **Rollback plan:** maintain a `sqlservr.exe.config.backup` without binding redirects. Keep PowerShell automation (`scripts/deploy-clr-secure.ps1 -Rollback`) ready to drop ILGPU assemblies and restore previous state if GPU workloads destabilize the instance.

---

## 8. High Availability & Disaster Recovery

- **Config sync:** replicate `sqlservr.exe.config` through server imaging or automation (Desired State Configuration, Ansible, etc.).
- **Restores:** include dependency binaries and configuration files in the database recovery runbook; SQL backups alone are insufficient.
- **Failover validation:** before placing a replica into rotation, execute the verification suite described in section 6.

---

## 9. Maintenance Workflow

1. **Dependency updates:** rerun the dependency matrix analysis whenever NuGet packages change; confirm the new version still emits a `net4x` TFM.
2. **Continuous integration:** bake the dependency hash manifest into CI to fail builds if unexpected versions appear.
3. **Post-deployment:** after every production rollout, run the assembly resolution smoke test and ILGPU accelerator probe to confirm success.
4. **Documentation:** log configuration changes and assembly hashes in `audit/` for traceability.

---

## 10. References

- Microsoft Docs: [CLR Integration Overview](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/common-language-runtime-integration-overview)
- Microsoft Docs: [CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- Microsoft Docs: [CREATE ASSEMBLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-assembly-transact-sql)
- ILGPU 0.9.2 Release Notes (GitHub)
- MathNet.Numerics 5.0.0 Framework Targets (official docs)
- System.* package reference assemblies on NuGet (verify `net461` folders)
