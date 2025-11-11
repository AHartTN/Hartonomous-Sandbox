# SQL Server CLR Binding Redirect Instructions

**Purpose:** Document the exact host-level configuration that must be applied to `sqlservr.exe.config` so SQL Server 2025 resolves the ILGPU/MathNet dependency stack shipped with the Hartonomous CLR assembly. Perform these steps manually on every SQL Server host (primary, HA, DR). Elevated OS privileges are required—automations in this repository do not touch the SQL Server installation directory.

## 1. Locate the SQL Server `Binn` Directory

Query the service metadata to determine the running executable path:

```sql
SELECT [filename]
FROM sys.dm_server_services
WHERE servicename LIKE N'SQL Server (%';
```

The configuration file must be named `sqlservr.exe.config` and reside alongside `sqlservr.exe` (for example `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Binn`).

## 2. Apply Binding Redirect Policy

Edit/Create `sqlservr.exe.config` with the following `<runtime>` section. Preserve any existing entries that the instance already relies on.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe"
                          publicKeyToken="b03f5f7f11d50a3a"
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.7.0.0" newVersion="4.7.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Collections.Immutable"
                          publicKeyToken="b03f5f7f11d50a3a"
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-1.7.0.0" newVersion="1.7.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Reflection.Metadata"
                          publicKeyToken="b03f5f7f11d50a3a"
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-1.7.99.99" newVersion="1.8.0.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Buffers"
                          publicKeyToken="cc7b13ffcd2ddd51"
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.5.0.0" newVersion="4.5.1.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Memory"
                          publicKeyToken="cc7b13ffcd2ddd51"
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.5.3.0" newVersion="4.5.4.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Numerics.Vectors"
                          publicKeyToken="b03f5f7f11d50a3a"
                          culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.4.99.99" newVersion="4.5.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
```

## 3. Post-Change Validation

1. Restart the SQL Server service (preferred) or run `DBCC FREESYSTEMCACHE('ALL');` to flush the CLR loader cache.
2. Run the following in the Hartonomous database to ensure the correct assembly versions loaded from the database, not the GAC:

   ```sql
   SELECT olm.name,
          olm.file_version,
          olm.loaded_in_memory
   FROM sys.dm_os_loaded_modules AS olm
   WHERE olm.name LIKE N'%System.Runtime.CompilerServices.Unsafe%';
   ```

   The `name` column should point to the database-scoped assembly (`sys.sysobjvalues` path). If it references `C:\Windows\Microsoft.NET\Framework64`, the binding redirect was not applied correctly.

3. Repeat this verification on every failover target and disaster recovery node. Store a checksum of the deployed `sqlservr.exe.config` alongside other configuration management artifacts.

## 4. Ongoing Maintenance Checklist

- Re-validate the binding redirects after installing SQL Server Cumulative Updates or .NET Framework patches (they may overwrite `sqlservr.exe.config`).
- Include this configuration step in server provisioning runbooks, golden image pipelines, and HA/DR failover exercises.
- Keep historical copies of `sqlservr.exe.config` in secured configuration management storage (do **not** commit to this repository).
