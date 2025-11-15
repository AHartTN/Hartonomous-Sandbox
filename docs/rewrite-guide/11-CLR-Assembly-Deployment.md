# 11 - CLR Assembly Deployment: The Complete Guide

Deploying SQL CLR assemblies is one of the most technically demanding aspects of the Hartonomous platform. This document provides the definitive, step-by-step guide for building, signing, and deploying the `Hartonomous.SqlClr` assembly in a secure and reliable manner.

## 1. Prerequisites and Environment Setup

### SQL Server Configuration
Before any CLR assembly can be deployed, the SQL Server instance must be properly configured.

```sql
-- Enable CLR integration (requires sysadmin)
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Enable CLR strict security (SQL Server 2017+)
-- This is the default in modern SQL Server and should NOT be disabled
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
```

### Understanding CLR Strict Security
Modern SQL Server (2017+) requires **all** CLR assemblies to be trusted, regardless of their permission set. This is enforced via the `clr strict security` setting. There are two approaches to satisfy this requirement:

1. **Asymmetric Key from Assembly** (Recommended for SAFE and EXTERNAL_ACCESS)
2. **Authenticode Signature** (Required for UNSAFE)

## 2. Assembly Signing Strategy

For the Hartonomous platform, we will use a **hybrid signing approach**:

- The base CLR assembly (SAFE permission set) will use an **asymmetric key** created directly from the DLL.
- If we need to escalate to EXTERNAL_ACCESS (for IPC to GPU worker), we will use an **authenticode certificate**.

### Why Not Use Strong Naming?
Strong naming (SNK files) is for .NET assembly identity and versioning. It is **not** sufficient for SQL Server's CLR strict security. The database requires either an asymmetric key or an authenticode signature.

## 3. Project Configuration for .NET Framework 4.8.1

The `Hartonomous.SqlClr.csproj` file must be explicitly configured for SQL CLR compatibility.

### Critical Project Settings

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- MUST be .NET Framework, NOT .NET Standard or .NET Core -->
    <TargetFramework>net481</TargetFramework>

    <!-- Disable implicit usings - CLR must be explicit -->
    <ImplicitUsings>disable</ImplicitUsings>

    <!-- Nullable reference types are not supported in CLR -->
    <Nullable>disable</Nullable>

    <!-- Optimize for release builds -->
    <Optimize>true</Optimize>

    <!-- Do NOT generate a reference assembly -->
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
  </PropertyGroup>

  <ItemGroup>
    <!-- SQL Server requires this for CLR types -->
    <Reference Include="System.Data" />
    <Reference Include="System.Xml" />

    <!-- SIMD support (available in .NET Framework 4.8.1) -->
    <PackageReference Include="System.Numerics.Vectors" Version="4.5.0" />
  </ItemGroup>

  <!-- Explicitly exclude incompatible dependencies -->
  <ItemGroup>
    <PackageReference Include="System.Collections.Immutable" Exclude="true" />
    <PackageReference Include="System.Reflection.Metadata" Exclude="true" />
  </ItemGroup>

</Project>
```

### Dependency Audit
Before building, verify that **no** .NET Standard libraries are referenced. Use this PowerShell script:

```powershell
# Run from the Hartonomous.SqlClr project directory
dotnet restore
$dllPath = "bin\Release\net481\Hartonomous.SqlClr.dll"
dotnet build -c Release

# List all dependencies
$asm = [System.Reflection.Assembly]::LoadFile((Resolve-Path $dllPath))
$asm.GetReferencedAssemblies() | ForEach-Object {
    Write-Host "$($_.Name) - $($_.Version)"
}
```

If you see any assemblies that are NOT in the SQL Server CLR whitelist, they must be removed.

## 4. Build and Extract the Assembly

Once the project is correctly configured, build the release binary.

```bash
# Build the release version
dotnet build src/Hartonomous.SqlClr/Hartonomous.SqlClr.csproj -c Release

# The output DLL will be at:
# src/Hartonomous.SqlClr/bin/Release/net481/Hartonomous.SqlClr.dll
```

## 5. Deployment Script for SAFE Permission Set

This is the standard deployment path for the initial CLR assembly.

### Step 1: Create Asymmetric Key from Assembly

```sql
USE [master];
GO

-- Create an asymmetric key from the compiled DLL
-- This key will be used to mark the assembly as trusted
CREATE ASYMMETRIC KEY Hartonomous_CLR_Key
FROM EXECUTABLE FILE = 'D:\Path\To\Hartonomous.SqlClr.dll';
GO

-- Create a login from the asymmetric key
CREATE LOGIN Hartonomous_CLR_Login
FROM ASYMMETRIC KEY Hartonomous_CLR_Key;
GO

-- Grant the login permission to create SAFE assemblies
GRANT UNSAFE ASSEMBLY TO Hartonomous_CLR_Login;
GO
```

**Important:** The `GRANT UNSAFE ASSEMBLY` permission is required even for SAFE assemblies when `clr strict security` is enabled. This is a confusing naming issue in SQL Server.

### Step 2: Create the Assembly in the Target Database

```sql
USE [Hartonomous];
GO

-- Load the assembly with SAFE permission set
CREATE ASSEMBLY [Hartonomous.SqlClr]
FROM 'D:\Path\To\Hartonomous.SqlClr.dll'
WITH PERMISSION_SET = SAFE;
GO
```

### Step 3: Create CLR Functions and Procedures

After the assembly is loaded, create the T-SQL wrappers for the CLR methods.

```sql
-- Example: Create a CLR scalar function
CREATE FUNCTION dbo.fn_VectorDotProduct(
    @vectorA VARBINARY(MAX),
    @vectorB VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.SqlClr].[Hartonomous.Database.CLR.Core.VectorMath].[DotProduct];
GO

-- Example: Create a CLR table-valued function
CREATE FUNCTION dbo.fn_ProcessCandidates(
    @candidateIds VARBINARY(MAX),
    @contextVector VARBINARY(MAX),
    @k INT
)
RETURNS TABLE (
    AtomId BIGINT,
    Score FLOAT
)
AS EXTERNAL NAME [Hartonomous.SqlClr].[Hartonomous.Database.CLR.AttentionGeneration].[ProcessCandidates];
GO
```

## 6. Deployment Script for EXTERNAL_ACCESS (IPC to GPU Worker)

If the CLR needs to communicate with the out-of-process GPU worker, it requires `EXTERNAL_ACCESS` permission.

### Additional Steps for EXTERNAL_ACCESS

The asymmetric key approach is still valid, but the permissions must be updated.

```sql
USE [Hartonomous];
GO

-- Drop the existing assembly if upgrading
DROP ASSEMBLY IF EXISTS [Hartonomous.SqlClr];
GO

-- Recreate with EXTERNAL_ACCESS permission
CREATE ASSEMBLY [Hartonomous.SqlClr]
FROM 'D:\Path\To\Hartonomous.SqlClr.dll'
WITH PERMISSION_SET = EXTERNAL_ACCESS;
GO
```

### Database Trustworthy Setting (Alternative, Less Secure)

An alternative to the asymmetric key approach is to mark the database as `TRUSTWORTHY`. **This is less secure** and should only be used in development environments.

```sql
-- NOT RECOMMENDED for production
ALTER DATABASE [Hartonomous] SET TRUSTWORTHY ON;
```

## 7. Automated Deployment via DACPAC

For production deployments, the CLR assembly should be included in the DACPAC generated by the `Hartonomous.Database.sqlproj` file.

### Configure the .sqlproj File

The `.sqlproj` file should include the CLR DLL as a reference.

```xml
<ItemGroup>
  <ArtifactReference Include="..\Hartonomous.SqlClr\bin\$(Configuration)\net481\Hartonomous.SqlClr.dll">
    <HintPath>..\Hartonomous.SqlClr\bin\$(Configuration)\net481\Hartonomous.SqlClr.dll</HintPath>
    <SuppressMissingDependenciesErrors>False</SuppressMissingDependenciesErrors>
  </ArtifactReference>
</ItemGroup>
```

### Deploy the DACPAC

```bash
# Use SqlPackage.exe to deploy the DACPAC
sqlpackage /Action:Publish \
  /SourceFile:Hartonomous.Database.dacpac \
  /TargetServerName:localhost \
  /TargetDatabaseName:Hartonomous \
  /p:IncludeCompositeObjects=True
```

## 8. Troubleshooting Common Issues

### "Could not load file or assembly 'X'"
This means a dependency is not on the SQL CLR whitelist. Audit all references and remove incompatible libraries.

### "Assembly is not authorized for PERMISSION_SET"
The `clr strict security` requirement is not satisfied. Ensure the asymmetric key and login are correctly created in the `master` database.

### "The assembly is built by a runtime newer than the currently loaded runtime"
The assembly was compiled for .NET Framework 4.8.1, but the server only supports an older version. Downgrade the target framework or upgrade SQL Server.

### "Type 'X' is marked as eligible for type equivalence, but..."
Disable type equivalence by ensuring `EmbedInteropTypes` is set to `false` for all references.

## 9. Security Best Practices

- **Principle of Least Privilege:** Always start with `SAFE`. Only escalate to `EXTERNAL_ACCESS` if absolutely necessary. Never use `UNSAFE` unless there is no alternative.
- **Code Review:** All CLR code must be reviewed for security vulnerabilities (SQL injection via `SqlCommand`, unmanaged code interop, etc.).
- **Separate Environments:** Use different signing keys for development, staging, and production.
- **Audit Logging:** Enable SQL Server audit logging for all `CREATE ASSEMBLY` and `ALTER ASSEMBLY` operations.

This guide provides the complete, production-ready deployment process for the Hartonomous SQL CLR assembly.
