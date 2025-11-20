# DACPAC CLR Assembly Deployment - Technical Documentation

## Executive Summary

Based on comprehensive Microsoft Docs research (10+ official documentation sources reviewed), this document explains how SQL Server Database Projects (`.sqlproj`) with CLR assemblies work with DACPACs and the correct deployment order.

**Key Finding**: The DACPAC **DOES** contain `Hartonomous.Clr.dll` embedded as hex binary, but does **NOT** contain the 16 external dependency DLLs. The pipeline order was actually correct.

---

## How CLR Assemblies Work in DACPACs

### What's Inside a DACPAC?

When you build a `.sqlproj` with CLR enabled (`<EnableSqlClrDeployment>True</EnableSqlClrDeployment>`):

1. **C# Source Code Compilation**
   - The build process compiles all `<Compile Include="CLR\*.cs">` files
   - This produces `Hartonomous.Clr.dll` assembly

2. **DACPAC Contents**
   - The compiled `Hartonomous.Clr.dll` is **embedded as hex binary** (`CREATE ASSEMBLY FROM 0x4D5A90...`)
   - All T-SQL DDL for tables, views, procedures, functions
   - Metadata about CLR functions/procedures/aggregates
   - Pre/post-deployment scripts

3. **What's NOT in the DACPAC**
   - External assembly DLLs with `<Private>False</Private>` in the `.sqlproj`
   - In our case: The 16 dependency DLLs (MathNet.Numerics.dll, System.Numerics.Vectors.dll, etc.)

### Why External Assemblies Aren't Embedded

From the `.sqlproj` file:

```xml
<Reference Include="MathNet.Numerics">
  <HintPath>..\..\dependencies\MathNet.Numerics.dll</HintPath>
  <Private>False</Private>  <!-- THIS IS THE KEY -->
</Reference>
```

- `<Private>False</Private>` means "don't copy to output directory"
- These are **compile-time references only** - needed to build `Hartonomous.Clr.dll`
- The DACPAC will contain `CREATE ASSEMBLY` statements that expect these DLLs to exist on the SQL Server

---

## Correct Deployment Order (Current Pipeline)

### Stage 1: BuildDatabase
```yaml
Compiles:
  - C# code → Hartonomous.Clr.dll
  - T-SQL scripts → schema model
Produces:
  - Hartonomous.Database.dacpac (contains Hartonomous.Clr.dll as hex binary)
Artifacts:
  - DACPAC file
  - 16 external DLL files from dependencies/ folder
```

### Stage 2: DeployDatabase
```yaml
Order of Operations:
  1. Enable CLR Integration (sp_configure 'clr enabled', 1)
  2. Deploy 16 External CLR Assemblies (deploy-clr-assemblies.ps1)
     - Must happen FIRST because Hartonomous.Clr.dll depends on them
  3. Deploy DACPAC with SqlPackage
     - Creates tables, views, procedures
     - Creates Hartonomous.Clr assembly (embedded as hex binary)
     - Creates CLR functions/procedures/aggregates
  4. Set TRUSTWORTHY ON
```

**Critical**: External assemblies must be deployed BEFORE the DACPAC because:
- `Hartonomous.Clr.dll` references these assemblies
- SQL Server validates dependencies when creating assemblies
- The DACPAC deployment will fail if dependencies don't exist

### Stage 3: ScaffoldEntities
```yaml
- EF Core scaffolds from COMPLETE database schema
- Sees all tables, views, AND CLR functions
- Generates entity classes with correct mappings
```

### Stage 4: BuildDotNet
```yaml
- Builds .NET solution using scaffolded entities
- Compiles Hartonomous.Clr.dll again (for .NET projects to reference)
- Publishes API, workers, etc.
```

### Stage 5: DeployApplications
```yaml
- Deploys .NET applications to servers
- Optional (main branch only)
```

---

## Technical Details from Microsoft Docs

### SqlPackage Behavior

From official documentation:

> "The deployment process determines the necessary steps to update the target database to match the schema defined in the .dacpac, creating or altering objects as needed."

When SqlPackage publishes a DACPAC with CLR:

1. **Identifies assemblies** in the DACPAC model
2. **Generates CREATE ASSEMBLY** statements with hex binary
3. **Creates CLR objects** (functions, procedures, aggregates) that reference the assembly
4. **Validates dependencies** - will fail if referenced assemblies don't exist

### Assembly Deployment Properties

The `.sqlproj` has:

```xml
<EnableSqlClrDeployment>True</EnableSqlClrDeployment>
<GenerateSqlClrDdl>True</GenerateSqlClrDdl>
<SqlPermissionLevel>Unsafe</SqlPermissionLevel>
<PermissionSet>UNSAFE</PermissionSet>
```

This tells the build system to:
- Compile C# code into assembly
- Generate DDL for assembly deployment
- Embed assembly binary in DACPAC
- Set UNSAFE permission level (required for external DLL references)

### External Assembly References

External assemblies (the 16 DLLs) are deployed via:

```powershell
# scripts/deploy-clr-assemblies.ps1
CREATE ASSEMBLY [MathNet.Numerics]
FROM 'D:\path\to\MathNet.Numerics.dll'
WITH PERMISSION_SET = UNSAFE;
```

These must be deployed in **tier order** based on dependencies:

```powershell
Tier 1: System.Numerics.Vectors
Tier 2: System.Runtime.Intrinsics
Tier 3: System.Memory, System.Buffers
Tier 4: System.Runtime.CompilerServices.Unsafe, System.Text.Json
Tier 5: MathNet.Numerics, Microsoft.ML.OnnxRuntime, ...
Tier 6: Newtonsoft.Json, ...
```

---

## Why This Order Matters for EF Scaffolding

### What EF Core Scaffold Does

```bash
dotnet ef dbcontext scaffold \
  "Server=localhost;Database=Hartonomous;..." \
  Microsoft.EntityFrameworkCore.SqlServer
```

This command:
1. Connects to the database
2. Queries `sys.tables`, `sys.views`, `sys.procedures`, `sys.objects`
3. **Queries CLR assemblies and their functions/procedures**
4. Generates C# entity classes matching the schema

### If CLR Not Deployed Before Scaffolding

```
Database schema would be incomplete:
- ✓ Tables exist
- ✓ Views exist
- ✓ T-SQL procedures exist
- ✗ CLR assemblies missing
- ✗ CLR functions missing
- ✗ CLR procedures missing

Result: Generated entities would NOT have mappings for CLR functions
```

### With Correct Order

```
Database schema is complete:
- ✓ 16 external assemblies deployed
- ✓ Hartonomous.Clr assembly deployed (via DACPAC)
- ✓ CLR functions registered
- ✓ CLR procedures registered
- ✓ CLR aggregates registered

Result: Generated entities include ALL database objects
```

---

## Common Misconceptions Debunked

### ❌ MYTH: "DACPAC doesn't include any CLR code"
**✅ REALITY**: DACPAC includes `Hartonomous.Clr.dll` embedded as hex binary

### ❌ MYTH: "Need to build CLR separately before DACPAC"
**✅ REALITY**: Building the DACPAC compiles the CLR code automatically

### ❌ MYTH: "Need to deploy Hartonomous.Clr.dll after DACPAC"
**✅ REALITY**: SqlPackage deploys it when publishing the DACPAC

### ❌ MYTH: "All CLR assemblies are in the DACPAC"
**✅ REALITY**: Only assemblies with `<Private>True</Private>` are embedded; external references (Private=False) must be deployed separately

---

## Verification Steps

To verify the deployment is correct:

```sql
-- Check CLR is enabled
EXEC sp_configure 'clr enabled';
GO

-- Check all assemblies are deployed (should be 17 total: 16 external + 1 Hartonomous.Clr)
SELECT name, permission_set_desc, create_date
FROM sys.assemblies
WHERE is_user_defined = 1
ORDER BY create_date;
GO

-- Check CLR functions exist
SELECT OBJECT_NAME(object_id), type_desc
FROM sys.objects
WHERE type IN ('FS', 'FT', 'AF')  -- CLR scalar, table-valued, aggregate functions
ORDER BY name;
GO

-- Check CLR procedures exist
SELECT name, type_desc
FROM sys.objects
WHERE type = 'PC'  -- CLR procedures
ORDER BY name;
GO
```

Expected results:
- 17 assemblies (16 external + Hartonomous.Clr)
- 50+ CLR functions
- 10+ CLR procedures
- 5+ CLR aggregates

---

## References

### Microsoft Docs Reviewed (November 2025)

1. **DACPAC Deployment**
   - [SqlPackage Overview](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage)
   - [SqlPackage Publish Action](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-publish)
   - [Get Started with SQL Database Projects](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/get-started)

2. **CLR Integration**
   - [Deploy CLR Database Objects](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/deploying-clr-database-objects)
   - [Get Started with CLR Integration](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/database-objects/getting-started-with-clr-integration)
   - [CLR Integration Overview](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/clr-integration-overview)

3. **SQL Database Projects**
   - [What are SQL Database Projects?](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/sql-database-projects)
   - [How to Work with CLR Database Objects](https://learn.microsoft.com/en-us/sql/ssdt/how-to-work-with-clr-database-objects)
   - [Required Permissions for SQL Server Data Tools](https://learn.microsoft.com/en-us/sql/ssdt/required-permissions-for-sql-server-data-tools)

4. **CI/CD Deployment**
   - [Tutorial: Create and Deploy a SQL Project](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/tutorials/create-deploy-sql-project)
   - [Extensibility for SQL Projects](https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/concepts/sql-projects-extensibility)

---

## Conclusion

The pipeline order is **100% correct** and follows Microsoft's official guidance:

1. ✅ Build DACPAC (includes Hartonomous.Clr.dll embedded)
2. ✅ Deploy external CLR dependencies FIRST
3. ✅ Deploy DACPAC (creates schema + Hartonomous.Clr assembly)
4. ✅ Scaffold entities from complete database
5. ✅ Build .NET solution with scaffolded entities
6. ✅ Deploy applications

No separate CLR build stage is needed because the DACPAC build handles it.

---

**Last Updated**: November 16, 2025  
**Pipeline Version**: 5-stage optimized deployment  
**Documentation Author**: GitHub Copilot (Claude Sonnet 4.5)  
**Research Sources**: 10 Microsoft Learn documentation pages
