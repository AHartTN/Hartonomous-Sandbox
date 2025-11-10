# SQL Server CLR Deployment Repair Guide

**CRITICAL REFERENCE DOCUMENT** - Read this before modifying CLR assemblies or deployment scripts.

## Problem Summary

SQL Server CLR deployment was failing with error:
```
Assembly 'system.runtime.serialization, version=4.0.0.0, culture=neutral, 
publickeytoken=b77a5c561934e089.' was not found in the SQL catalog.
```

**Root Cause:** MathNet.Numerics 5.0.0 (required for SIMD matrix operations, SVD, Mahalanobis distance, transformer inference) has dependencies on .NET Framework GAC assemblies that are **NOT** in SQL Server's supported assemblies list (MS KB 922672).

## Supported SQL Server CLR Assemblies (KB 922672)

SQL Server only supports these 13 .NET Framework assemblies without explicit deployment:

1. `Microsoft.VisualBasic.dll`
2. `Mscorlib.dll`
3. `System.Data.dll`
4. `System.dll`
5. `System.Xml.dll`
6. `Microsoft.VisualC.dll`
7. `CustomMarshalers.dll`
8. `System.Security.dll`
9. `System.Web.Services.dll`
10. `System.Data.SqlXml.dll`
11. `System.Transactions.dll`
12. `System.Data.OracleClient.dll`
13. `System.Configuration.dll`

**Any other assembly MUST be explicitly deployed with CREATE ASSEMBLY.**

## Dependency Chain Discovery

### Using ildasm.exe to Analyze Dependencies

```powershell
# 1. Locate ildasm.exe (comes with Windows SDK)
$ildasmPath = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\x64\ildasm.exe"

# 2. Disassemble assembly to IL text format
& $ildasmPath "path\to\assembly.dll" /TEXT /HEADERS /OUT:"output.il"

# 3. Extract assembly extern directives (these are the dependencies)
Select-String -Path "output.il" -Pattern "^\.assembly extern" -Context 0,2
```

### MathNet.Numerics Dependency Tree

Analysis revealed this dependency chain:

```
MathNet.Numerics.dll
├── System.Runtime.Serialization ❌ NOT SUPPORTED
│   ├── System.ServiceModel.Internals ❌ NOT SUPPORTED
│   │   ├── mscorlib ✅ SUPPORTED
│   │   ├── System ✅ SUPPORTED
│   │   └── System.Xml ✅ SUPPORTED
│   ├── SMDiagnostics ❌ NOT SUPPORTED
│   │   ├── mscorlib ✅ SUPPORTED
│   │   ├── System ✅ SUPPORTED
│   │   ├── System.Configuration ✅ SUPPORTED
│   │   ├── System.Xml ✅ SUPPORTED
│   │   └── System.ServiceModel.Internals ❌ (already in chain)
│   ├── mscorlib ✅ SUPPORTED
│   ├── System ✅ SUPPORTED
│   └── System.Xml ✅ SUPPORTED
├── System.Numerics (GAC reference, not NuGet)
└── mscorlib ✅ SUPPORTED
```

**Key Finding:** All terminal dependencies (leaf nodes) resolve to SQL Server supported assemblies. This means the chain is deployable if we deploy the intermediate assemblies in correct order.

## GAC Assembly Location

.NET Framework 4.x GAC location:
```
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\{AssemblyName}\v4.0_{Version}__{PublicKeyToken}\
```

### Required GAC Assemblies

```powershell
# System.ServiceModel.Internals
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.ServiceModel.Internals\v4.0_4.0.0.0__31bf3856ad364e35\System.ServiceModel.Internals.dll

# SMDiagnostics
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\SMDiagnostics\v4.0_4.0.0.0__b77a5c561934e089\SMDiagnostics.dll

# System.Runtime.Serialization
C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Runtime.Serialization\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.Serialization.dll
```

## Solution Implementation

### 1. Extract GAC Assemblies

```powershell
# Copy required assemblies to project dependencies folder
$destDir = "d:\Repositories\Hartonomous\dependencies\"

Copy-Item "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.ServiceModel.Internals\v4.0_4.0.0.0__31bf3856ad364e35\System.ServiceModel.Internals.dll" -Destination $destDir -Force

Copy-Item "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\SMDiagnostics\v4.0_4.0.0.0__b77a5c561934e089\SMDiagnostics.dll" -Destination $destDir -Force

Copy-Item "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Runtime.Serialization\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.Serialization.dll" -Destination $destDir -Force
```

### 2. Update deploy-clr-secure.ps1

**CRITICAL:** Assemblies MUST be deployed in dependency order. Dependencies FIRST, then dependents.

```powershell
# Define assemblies in dependency order (dependencies first)
$assemblies = @(
    @{ Name = "System.ServiceModel.Internals"; Required = $true },  # 1. Base dependency
    @{ Name = "SMDiagnostics"; Required = $true },                  # 2. Depends on ServiceModel.Internals
    @{ Name = "System.Runtime.Serialization"; Required = $true },   # 3. Depends on both above
    @{ Name = "System.Numerics.Vectors"; Required = $true },        # 4. Independent
    @{ Name = "MathNet.Numerics"; Required = $true },               # 5. Depends on Runtime.Serialization
    @{ Name = "Newtonsoft.Json"; Required = $true },                # 6. Independent
    @{ Name = "System.Drawing"; Required = $true },                 # 7. Independent
    @{ Name = "Microsoft.SqlServer.Types"; Required = $true; SystemAssembly = $true }, # 8. System assembly
    @{ Name = "SqlClrFunctions"; Required = $true }                 # 9. Main assembly (depends on all above)
)
```

### 3. Security Configuration

**DO NOT USE TRUSTWORTHY ON** - This is a security anti-pattern.

Instead, use SQL Server 2017+ `sys.sp_add_trusted_assembly`:

```sql
-- Add assembly to trusted assembly list
DECLARE @hash BINARY(64) = {SHA-512 hash of assembly bytes};
EXEC sys.sp_add_trusted_assembly @hash, N'AssemblyName';

-- Create assembly with UNSAFE permission
CREATE ASSEMBLY [AssemblyName]
FROM {assembly bytes as hex}
WITH PERMISSION_SET = UNSAFE;
```

**Security Checklist:**
- ✅ CLR strict security ON (SQL Server 2017+ default)
- ✅ TRUSTWORTHY OFF (database remains secure)
- ✅ All assemblies strong-name signed
- ✅ SHA-512 hashes calculated for sys.sp_add_trusted_assembly
- ✅ PERMISSION_SET = UNSAFE (required for unsupported assemblies)

## EF Core FILESTREAM Fix

Separate issue resolved during deployment: FILESTREAM ROWGUIDCOL constraint error.

### Problem
```
A table that has FILESTREAM columns must have a nonnull unique column 
with the ROWGUIDCOL property.
```

### Root Cause
`HasIndex().IsUnique()` creates a unique INDEX, but SQL Server FILESTREAM requires a UNIQUE CONSTRAINT.

### Solution
Change from:
```csharp
builder.HasIndex(e => e.RowGuid)
    .IsUnique()
    .HasDatabaseName("UX_AtomPayloadStore_RowGuid");
```

To:
```csharp
builder.HasAlternateKey(e => e.RowGuid)
    .HasName("UX_AtomPayloadStore_RowGuid");
```

This generates:
```csharp
table.UniqueConstraint("UX_AtomPayloadStore_RowGuid", x => x.RowGuid)
```

Instead of:
```csharp
table.Index("IX_AtomPayloadStore_RowGuid", x => x.RowGuid, unique: true)
```

**Pattern Source:** Working implementation in `LayerTensorSegmentConfiguration.cs`

## Deployment Validation

### Successful Deployment Output

```
✓ Found: System.ServiceModel.Internals.dll (244 KB)
✓ Found: SMDiagnostics.dll (68 KB)
✓ Found: System.Runtime.Serialization.dll (1,027 KB)
✓ Found: System.Numerics.Vectors.dll (113 KB)
✓ Found: MathNet.Numerics.dll (1,553 KB)
✓ Found: Newtonsoft.Json.dll (695 KB)
✓ Found: System.Drawing.dll (583 KB)
○ Using system assembly: Microsoft.SqlServer.Types
✓ Found: SqlClrFunctions.dll (296 KB)

Creating assembly [System.ServiceModel.Internals]... ✓
Creating assembly [SMDiagnostics]... ✓
Creating assembly [System.Runtime.Serialization]... ✓
Creating assembly [System.Numerics.Vectors]... ✓
Creating assembly [MathNet.Numerics]... ✓
Creating assembly [Newtonsoft.Json]... ✓
Creating assembly [System.Drawing]... ✓
Creating assembly [SqlClrFunctions]... ✓

AssemblyName                      PermissionSet    IsTrusted
--------------------------------  ---------------  ---------
System.ServiceModel.Internals     UNSAFE_ACCESS    Yes
SMDiagnostics                     UNSAFE_ACCESS    Yes
System.Runtime.Serialization      UNSAFE_ACCESS    Yes
System.Numerics.Vectors           UNSAFE_ACCESS    Yes
MathNet.Numerics                  UNSAFE_ACCESS    Yes
Newtonsoft.Json                   UNSAFE_ACCESS    Yes
System.Drawing                    UNSAFE_ACCESS    Yes
Microsoft.SqlServer.Types         UNSAFE_ACCESS    Yes
SqlClrFunctions                   UNSAFE_ACCESS    Yes

Security Configuration:
  - CLR strict security: ON
  - TRUSTWORTHY: OFF
  - Trusted assembly list: ACTIVE
  - Strong-name signing: REQUIRED
```

### Verification Queries

```sql
-- Check deployed assemblies
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS CLRVersion,
    CASE WHEN ta.hash IS NOT NULL THEN 'Yes' ELSE 'No' END AS IsTrusted
FROM sys.assemblies a
LEFT JOIN sys.trusted_assemblies ta ON ta.description = a.name
WHERE a.is_user_defined = 1
ORDER BY a.name;

-- Check for CLR functions/aggregates/types
SELECT 
    SCHEMA_NAME(o.schema_id) AS SchemaName,
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    a.name AS AssemblyName
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions'
ORDER BY o.type_desc, o.name;
```

## Troubleshooting Guide

### Error: "Assembly X was not found in the SQL catalog"

**Diagnosis:**
```powershell
# Check if assembly is in supported list
# If NOT in supported list, must be deployed explicitly

# Analyze the failing assembly's dependencies
& ildasm "path\to\failing-assembly.dll" /TEXT /HEADERS /OUT:"temp.il"
Select-String -Path "temp.il" -Pattern "^\.assembly extern"
```

**Solution:**
1. Identify missing dependency from error message
2. Locate in GAC: `C:\Windows\Microsoft.NET\assembly\GAC_MSIL\{AssemblyName}\`
3. Analyze its dependencies with ildasm
4. Add to deployment script BEFORE any assemblies that depend on it
5. Repeat until all dependencies resolved

### Error: "Assembly is malformed or not a pure .NET assembly"

**Diagnosis:** Assembly contains unmanaged code (mixed assembly).

**Solution:** Mixed assemblies cannot be deployed to SQL Server CLR unless they're in the supported list. Find a pure .NET alternative.

### Error: "PERMISSION_SET = SAFE failed"

**Diagnosis:** Assembly requires external access or unsafe operations.

**Solution:** 
1. Use `PERMISSION_SET = UNSAFE`
2. Add assembly to `sys.trusted_assemblies`
3. Ensure TRUSTWORTHY is OFF (use sys.sp_add_trusted_assembly instead)

### Deployment Order Issues

**Symptom:** Assembly X deployed successfully but Y (which depends on X) fails.

**Diagnosis:** Dependencies deployed in wrong order.

**Solution:** Reorder `$assemblies` array in `deploy-clr-secure.ps1`:
```powershell
# WRONG - dependent before dependency
@{ Name = "DependentAssembly" },
@{ Name = "DependencyAssembly" }

# CORRECT - dependency before dependent
@{ Name = "DependencyAssembly" },
@{ Name = "DependentAssembly" }
```

## How to Add New NuGet Packages

**CRITICAL PROCESS** - Follow these steps EXACTLY:

### 1. Analyze Package Dependencies

```powershell
# After adding package to SqlClrFunctions.csproj
cd src/SqlClr
dotnet restore
dotnet build -c Release

# Check NuGet dependencies
dotnet list package --include-transitive --framework net481

# IMPORTANT: This only shows NuGet packages, not GAC references!
# You MUST use ildasm to see GAC assembly references.
```

### 2. Use ildasm on Compiled Assembly

```powershell
# Build the project
dotnet build SqlClrFunctions.csproj -c Release

# Find the new package DLL in bin/Release or dependencies/
$packageDll = "path\to\NewPackage.dll"

# Disassemble
& "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.2 Tools\x64\ildasm.exe" `
    $packageDll /TEXT /HEADERS /OUT:"temp\NewPackage.il"

# Extract dependencies
Select-String -Path "temp\NewPackage.il" -Pattern "^\.assembly extern" -Context 0,2
```

### 3. Map Full Dependency Tree

For each `.assembly extern` found:
1. Check if it's in SQL Server supported list (see above)
2. If NOT supported, locate in GAC and analyze IT with ildasm
3. Repeat recursively until all terminal dependencies are supported assemblies
4. Document the full tree

### 4. Update Deployment Script

```powershell
# Add new assemblies to $assemblies array in DEPENDENCY ORDER
# Example: If NewPackage depends on System.SomeAssembly
$assemblies = @(
    # ... existing dependencies ...
    @{ Name = "System.SomeAssembly"; Required = $true },  # Dependency FIRST
    @{ Name = "NewPackage"; Required = $true },           # Dependent AFTER
    # ... rest of assemblies ...
)
```

### 5. Copy GAC Assemblies

```powershell
# For each new GAC dependency found
Copy-Item "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\{AssemblyName}\v4.0_*\{AssemblyName}.dll" `
    -Destination "dependencies\" -Force
```

### 6. Test Deployment

```powershell
# Deploy to test database
.\scripts\deploy-clr-secure.ps1 -ServerName "localhost" -DatabaseName "HartonomousTest"

# Verify all assemblies loaded
sqlcmd -S localhost -d HartonomousTest -E -C -Q "
    SELECT name, permission_set_desc 
    FROM sys.assemblies 
    WHERE is_user_defined = 1 
    ORDER BY name"
```

## Critical Files Reference

### Deployment Scripts
- `scripts/deploy-clr-secure.ps1` - Main CLR deployment script
- `scripts/deploy-database-unified.ps1` - Full database deployment including CLR

### EF Core Configuration
- `src/Hartonomous.Data/Configurations/AtomPayloadStoreConfiguration.cs` - FILESTREAM configuration
- `src/Hartonomous.Data/Configurations/LayerTensorSegmentConfiguration.cs` - Reference pattern for FILESTREAM
- `src/Hartonomous.Data/Migrations/20251110122625_InitialCreate.cs` - Current migration

### CLR Source
- `src/SqlClr/SqlClrFunctions.csproj` - Main CLR project (targets net481)
- `src/SqlClr/**/*.cs` - CLR function implementations
- `dependencies/` - Non-NuGet assembly dependencies (GAC assemblies)

### SQL Bindings
- `sql/procedures/Common.ClrBindings.sql` - SQL wrapper functions for CLR
- `sql/procedures/Autonomy.FileSystemBindings.sql` - FILESTREAM CLR bindings

## Known Issues and Warnings

### ⚠️ CRITICAL WARNINGS

1. **DO NOT modify SqlClrFunctions.csproj package versions without re-analyzing full dependency tree**
   - Even patch version changes can introduce new dependencies
   - Always use ildasm to verify

2. **DO NOT use `TRUSTWORTHY ON`**
   - Security anti-pattern
   - Use `sys.sp_add_trusted_assembly` instead
   - Script handles this automatically

3. **DO NOT skip ildasm analysis**
   - `dotnet list package --include-transitive` does NOT show GAC references
   - GAC references are the ones that break SQL Server CLR
   - Only ildasm shows the complete picture

4. **DO NOT change assembly deployment order**
   - Dependencies MUST come before dependents
   - SQL Server loads assemblies sequentially
   - Wrong order = deployment failure

5. **DO NOT deploy to production without testing on clean database**
   - Deployment is idempotent but sensitive
   - Test `DROP DATABASE` → `deploy-database-unified.ps1` cycle
   - Verify all CLR functions work before production deployment

### Known Unsupported Packages

These packages are known to have problematic dependencies:

- **Entity Framework Core** - Cannot be used in SQL CLR (requires .NET Core/.NET 5+)
- **System.Text.Json** - Requires .NET Core/.NET 5+ APIs
- **Any package targeting netstandard2.1+** - .NET Framework 4.8.1 only supports up to netstandard2.0

### Supported Patterns

- **MathNet.Numerics** - Works with GAC dependency chain (as documented)
- **Newtonsoft.Json** - Pure .NET, works directly
- **System.Drawing** - GAC assembly, works with UNSAFE
- **System.Numerics.Vectors** - Works with UNSAFE

## References

### Microsoft Documentation
- [MS KB 922672](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/development/policy-untested-net-framework-assemblies) - Supported .NET Framework assemblies in SQL Server CLR
- [ildasm.exe Documentation](https://learn.microsoft.com/en-us/dotnet/framework/tools/ildasm-exe-il-disassembler) - IL Disassembler reference
- [sys.sp_add_trusted_assembly](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sys-sp-add-trusted-assembly-transact-sql) - SQL Server 2017+ secure CLR deployment
- [CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security) - SQL Server CLR security model
- [EF Core Alternate Keys](https://learn.microsoft.com/en-us/ef/core/modeling/keys#alternate-keys) - HasAlternateKey vs HasIndex

### Internal Documentation
- `docs/SQL_CLR_RESEARCH_FINDINGS.md` - Historical CLR research
- `docs/CLR_DEPLOYMENT.md` - CLR deployment procedures
- `docs/DEPLOYMENT.md` - General deployment guide

## Version History

- **2025-11-10** - Initial repair guide created after successful deployment of 9-assembly chain
- **Commit:** `0860c04` - "CRITICAL: Fix SQL Server CLR deployment with complete .NET Framework dependency chain"

---

**Last Updated:** November 10, 2025  
**Maintained By:** Hartonomous Development Team  
**Severity:** CRITICAL - Required Reading for CLR Modifications
