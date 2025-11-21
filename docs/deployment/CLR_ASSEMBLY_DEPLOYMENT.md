# Enterprise CLR Assembly Deployment Roadmap

**Document Status**: Implementation Guide  
**Target Audience**: Database Administrators, DevOps Engineers  
**Prerequisite**: Hartonomous Database Schema Deployed  
**Estimated Duration**: 4-6 hours (first deployment), 1-2 hours (subsequent updates)  
**Last Updated**: November 20, 2025

---

## Executive Summary

This document provides the **production-grade deployment sequence** for the Hartonomous CLR computation layer. The system requires **138 CLR functions** across **16 external assemblies** plus the main `Hartonomous.Clr.dll` assembly. All assemblies must be **cryptographically signed** and deployed in **strict dependency order** to satisfy SQL Server's security requirements and assembly reference validation.

### Critical Requirements

1. **SQL Server 2022+** with CLR Integration enabled
2. **Strong-name signing certificate** (`.pfx` for build, `.cer` for deployment)
3. **UNSAFE ASSEMBLY** permissions (CLR functions use external resources)
4. **Dependency-aware deployment** (external assemblies before main assembly)

---

## Phase 1: Establish Code Access Security Infrastructure

### 1.1 Generate Signing Certificate

**Execute Once Per Environment:**

```powershell
# Navigate to repository root
cd D:\Repositories\Hartonomous

# Generate signing infrastructure
.\scripts\Initialize-CLRSigning.ps1
```

**Artifacts Created:**

- `certificates/HartonomousCLR.pfx` - Private key for build signing (DO NOT COMMIT)
- `certificates/HartonomousCLR.cer` - Public certificate for SQL Server trust
- `.signing-config` - Metadata file tracking certificate thumbprint

**Security Note:** The `.pfx` file contains the private key. Store securely (Azure Key Vault, Hardware Security Module, or local encrypted storage). Only the `.cer` public certificate is deployed to SQL Server.

---

### 1.2 Configure SQL Server Trust

**Deploy Certificate to SQL Server:**

```powershell
.\scripts\Deploy-CLRCertificate.ps1 `
    -Server "localhost" `
    -CertificatePath "certificates\HartonomousCLR.cer"
```

**SQL Actions Performed:**

```sql
USE master;

-- Import public certificate
CREATE CERTIFICATE HartonomousCertificate
FROM FILE = 'D:\Repositories\Hartonomous\certificates\HartonomousCLR.cer';

-- Create login from certificate
CREATE LOGIN HartonomousCertLogin
FROM CERTIFICATE HartonomousCertificate;

-- Grant UNSAFE ASSEMBLY permission
GRANT UNSAFE ASSEMBLY TO HartonomousCertLogin;
```

**Verification:**

```sql
-- Verify certificate exists
SELECT name, pvt_key_encryption_type_desc 
FROM sys.certificates 
WHERE name = 'HartonomousCertificate';

-- Verify login has UNSAFE ASSEMBLY permission
SELECT pr.name, pe.permission_name
FROM sys.server_principals pr
JOIN sys.server_permissions pe ON pr.principal_id = pe.grantee_principal_id
WHERE pr.name = 'HartonomousCertLogin';
```

---

## Phase 2: Build Signed Assemblies

### 2.1 Configure Project Signing

**File**: `src/Hartonomous.Database/Hartonomous.Database.sqlproj`

```xml
<PropertyGroup>
  <!-- Enable strong-name signing -->
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>..\..\certificates\HartonomousCLR.pfx</AssemblyOriginatorKeyFile>
  
  <!-- Optional: Delay signing for build servers without private key -->
  <DelaySign>false</DelaySign>
</PropertyGroup>
```

### 2.2 Build with Signing

```powershell
# Clean previous builds
Remove-Item -Recurse -Force src\Hartonomous.Database\bin\Release -ErrorAction SilentlyContinue

# Build with signing
.\scripts\Build-WithSigning.ps1 -Configuration Release
```

**Build Artifacts:**

- `src/Hartonomous.Database/bin/Release/Hartonomous.Clr.dll` (signed)
- `dependencies/*.dll` (16 external assemblies, signed via NuGet or manual signing)

**Verification:**

```powershell
# Verify assembly is signed
$assembly = [System.Reflection.Assembly]::LoadFile(
    "D:\Repositories\Hartonomous\src\Hartonomous.Database\bin\Release\Hartonomous.Clr.dll"
)
$assembly.GetName().GetPublicKey().Length -gt 0  # Should be True
```

---

## Phase 3: Deploy CLR Assemblies

### 3.1 Deploy External Dependencies (Tier 1-5)

**Critical:** External assemblies MUST be deployed before `Hartonomous.Clr.dll` due to reference validation.

**Dependency Tiers (Execution Order):**

#### Tier 1: No Dependencies

```sql
CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Runtime.CompilerServices.Unsafe.dll'
WITH PERMISSION_SET = SAFE;

CREATE ASSEMBLY [System.Buffers]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Buffers.dll'
WITH PERMISSION_SET = SAFE;
```

#### Tier 2: Depends on GAC System.Numerics

```sql
CREATE ASSEMBLY [System.Numerics.Vectors]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Numerics.Vectors.dll'
WITH PERMISSION_SET = SAFE;
```

#### Tier 3: Depends on Tier 1 + Tier 2

```sql
CREATE ASSEMBLY [MathNet.Numerics]
FROM 'D:\Repositories\Hartonomous\dependencies\MathNet.Numerics.dll'
WITH PERMISSION_SET = UNSAFE;

CREATE ASSEMBLY [System.Memory]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Memory.dll'
WITH PERMISSION_SET = SAFE;
```

#### Tier 4: Depends on Tier 1-3

```sql
CREATE ASSEMBLY [Newtonsoft.Json]
FROM 'D:\Repositories\Hartonomous\dependencies\Newtonsoft.Json.dll'
WITH PERMISSION_SET = UNSAFE;
```

#### Tier 5: Depends on All Previous Tiers

```sql
CREATE ASSEMBLY [System.Runtime.Intrinsics]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Runtime.Intrinsics.dll'
WITH PERMISSION_SET = SAFE;
```

**Automated Deployment Script:**

```powershell
.\scripts\deploy-clr-assemblies.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -DependenciesPath "dependencies"
```

---

### 3.2 Deploy Main Assembly

**After all external dependencies are registered:**

```sql
USE Hartonomous;
GO

CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Database\bin\Release\Hartonomous.Clr.dll'
WITH PERMISSION_SET = UNSAFE;
GO
```

**Verification:**

```sql
-- Verify all assemblies deployed
SELECT 
    name, 
    permission_set_desc, 
    create_date,
    clr_name
FROM sys.assemblies
WHERE is_user_defined = 1
ORDER BY create_date;

-- Expected: 17 assemblies (16 dependencies + Hartonomous.Clr)
```

---

### 3.3 Create SQL Function Wrappers

**Deploy CLR function wrappers:**

```sql
-- Scalar Function Example
CREATE FUNCTION dbo.clr_CosineSimilarity(
    @vector1 VARBINARY(MAX),
    @vector2 VARBINARY(MAX)
)
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.DistanceMetrics].[CosineSimilarity];
GO

-- Aggregate Function Example
CREATE AGGREGATE dbo.clr_VectorAverage(
    @input VARBINARY(MAX)
)
RETURNS VARBINARY(MAX)
EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.VectorAggregates].[VectorAverageAggregate];
GO

-- Stored Procedure Example
CREATE PROCEDURE dbo.sp_AnalyzeSystem(
    @tenantId INT,
    @analysisScope NVARCHAR(256),
    @lookbackHours INT,
    @analysisId UNIQUEIDENTIFIER OUTPUT,
    @totalInferences INT OUTPUT,
    @avgDurationMs FLOAT OUTPUT,
    @anomalyCount INT OUTPUT,
    @anomaliesJson NVARCHAR(MAX) OUTPUT,
    @patternsJson NVARCHAR(MAX) OUTPUT
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AutonomousFunctions].[sp_AnalyzeSystem];
GO
```

**Automated Wrapper Deployment:**

```powershell
# Deploy all SQL wrappers
Get-ChildItem src\Hartonomous.Database\Functions\clr_*.sql | 
    ForEach-Object { sqlcmd -S localhost -d Hartonomous -i $_.FullName }

Get-ChildItem src\Hartonomous.Database\Aggregates\clr_*.sql | 
    ForEach-Object { sqlcmd -S localhost -d Hartonomous -i $_.FullName }

Get-ChildItem src\Hartonomous.Database\Procedures\sp_*.sql | 
    Where-Object { $_.Name -match 'CLR' } |
    ForEach-Object { sqlcmd -S localhost -d Hartonomous -i $_.FullName }
```

---

## Phase 4: Validation & Testing

### 4.1 Smoke Tests

**Test 1: Scalar Function**

```sql
-- Test vector similarity calculation
DECLARE @vec1 VARBINARY(MAX) = 0x3F8000003F8000003F800000; -- [1.0, 1.0, 1.0]
DECLARE @vec2 VARBINARY(MAX) = 0x3F8000003F8000003F800000;

SELECT dbo.clr_CosineSimilarity(@vec1, @vec2) AS Similarity;
-- Expected: 1.0 (identical vectors)
```

**Test 2: Aggregate Function**

```sql
-- Test vector averaging
SELECT dbo.clr_VectorAverage(EmbeddingVector)
FROM dbo.AtomEmbedding
WHERE TenantId = 0
  AND AtomId IN (1, 2, 3);
-- Expected: VARBINARY(MAX) representing average vector
```

**Test 3: Stored Procedure**

```sql
-- Test autonomous analysis
DECLARE @analysisId UNIQUEIDENTIFIER;
DECLARE @totalInferences INT;
DECLARE @avgDurationMs FLOAT;
DECLARE @anomalyCount INT;
DECLARE @anomaliesJson NVARCHAR(MAX);
DECLARE @patternsJson NVARCHAR(MAX);

EXEC dbo.sp_AnalyzeSystem
    @tenantId = 0,
    @analysisScope = 'full',
    @lookbackHours = 24,
    @analysisId = @analysisId OUTPUT,
    @totalInferences = @totalInferences OUTPUT,
    @avgDurationMs = @avgDurationMs OUTPUT,
    @anomalyCount = @anomalyCount OUTPUT,
    @anomaliesJson = @anomaliesJson OUTPUT,
    @patternsJson = @patternsJson OUTPUT;

-- Verify outputs populated
SELECT @analysisId, @totalInferences, @avgDurationMs, @anomalyCount;
SELECT @anomaliesJson AS AnomaliesJSON;
SELECT @patternsJson AS PatternsJSON;
```

---

### 4.2 Performance Validation

**Benchmark CLR vs T-SQL:**

```sql
-- T-SQL Cosine Similarity (scalar loops)
DECLARE @start DATETIME2 = SYSUTCDATETIME();
DECLARE @result FLOAT;

SELECT @result = (
    SUM(v1.Value * v2.Value) / 
    (SQRT(SUM(v1.Value * v1.Value)) * SQRT(SUM(v2.Value * v2.Value)))
)
FROM (VALUES (1.0), (2.0), (3.0)) AS v1(Value)
CROSS JOIN (VALUES (1.0), (2.0), (3.0)) AS v2(Value);

SELECT DATEDIFF(MICROSECOND, @start, SYSUTCDATETIME()) AS T_SQL_Microseconds;

-- CLR Cosine Similarity (SIMD-optimized)
SET @start = SYSUTCDATETIME();

SELECT dbo.clr_CosineSimilarity(
    0x3F8000004000000040400000,  -- [1.0, 2.0, 3.0]
    0x3F8000004000000040400000   -- [1.0, 2.0, 3.0]
);

SELECT DATEDIFF(MICROSECOND, @start, SYSUTCDATETIME()) AS CLR_Microseconds;

-- Expected: CLR 2-4x faster due to SIMD vectorization
```

---

## Phase 5: CI/CD Integration

### 5.1 Azure DevOps Pipeline

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

variables:
  - group: 'Hartonomous-Production'  # Contains SqlServer variable

steps:
  # Install Windows SDK (for signtool.exe)
  - task: PowerShell@2
    displayName: 'Install Windows SDK'
    inputs:
      targetType: 'inline'
      script: 'choco install windows-sdk-10 -y'
      
  # Initialize signing infrastructure
  - task: PowerShell@2
    displayName: 'Initialize CLR Signing'
    inputs:
      filePath: 'scripts/Initialize-CLRSigning.ps1'
      
  # Build with automatic signing
  - task: PowerShell@2
    displayName: 'Build with Signing'
    inputs:
      filePath: 'scripts/Build-WithSigning.ps1'
      arguments: '-Configuration Release'
      
  # Deploy certificate to SQL Server
  - task: PowerShell@2
    displayName: 'Deploy CLR Certificate'
    inputs:
      filePath: 'scripts/Deploy-CLRCertificate.ps1'
      arguments: '-Server $(SqlServer)'
      
  # Deploy CLR assemblies
  - task: PowerShell@2
    displayName: 'Deploy CLR Assemblies'
    inputs:
      filePath: 'scripts/deploy-clr-assemblies.ps1'
      arguments: '-Server $(SqlServer) -Database Hartonomous'
```

---

### 5.2 GitHub Actions Workflow

```yaml
name: Deploy CLR Assemblies

on:
  push:
    branches: [ main ]
    paths:
      - 'src/Hartonomous.Database/CLR/**'
      - 'dependencies/**'

jobs:
  build-and-deploy:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
        
    - name: Install Windows SDK
      run: choco install windows-sdk-10 -y
        
    - name: Initialize CLR Signing
      run: .\scripts\Initialize-CLRSigning.ps1
      
    - name: Build with Signing
      run: .\scripts\Build-WithSigning.ps1 -Configuration Release
      
    - name: Deploy Certificate
      run: .\scripts\Deploy-CLRCertificate.ps1 -Server ${{ secrets.SQL_SERVER }}
      
    - name: Deploy Assemblies
      run: .\scripts\deploy-clr-assemblies.ps1 -Server ${{ secrets.SQL_SERVER }} -Database Hartonomous
```

---

## Phase 6: Troubleshooting

### 6.1 Common Errors

**Error: "Assembly references assembly 'System.Collections.Immutable', which is not present"**

**Cause:** Missing external dependency  
**Solution:** Deploy external assembly first:

```sql
CREATE ASSEMBLY [System.Collections.Immutable]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Collections.Immutable.dll'
WITH PERMISSION_SET = SAFE;
```

---

**Error: "CREATE ASSEMBLY failed because the assembly is built for an unsupported version of the Common Language Runtime"**

**Cause:** CLR project targeting wrong .NET Framework version  
**Solution:** Verify `Hartonomous.Database.csproj` targets .NET Framework 4.8.1:

```xml
<TargetFramework>net481</TargetFramework>
```

---

**Error: "Not authorized for PERMISSION_SET = UNSAFE"**

**Cause:** SQL Server login lacks UNSAFE ASSEMBLY permission  
**Solution:** Run certificate deployment script:

```powershell
.\scripts\Deploy-CLRCertificate.ps1 -Server localhost
```

---

**Error: "Could not load type 'Hartonomous.Clr.DistanceMetrics.CosineSimilarity'"**

**Cause:** Namespace mismatch between T-SQL wrapper and C# implementation  
**Solution:** Verify EXTERNAL NAME matches class structure exactly (case-sensitive):

```sql
-- Correct: Matches C# namespace Hartonomous.Clr, class DistanceMetrics, method CosineSimilarity
EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.DistanceMetrics].[CosineSimilarity];
```

---

### 6.2 Diagnostic Queries

**Verify Assembly Dependencies:**

```sql
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc,
    ar.referenced_assembly_name
FROM sys.assemblies a
LEFT JOIN sys.assembly_references ar ON a.assembly_id = ar.assembly_id
WHERE a.is_user_defined = 1
ORDER BY a.name, ar.referenced_assembly_name;
```

**Verify CLR Functions Registered:**

```sql
SELECT 
    OBJECT_NAME(object_id) AS FunctionName,
    type_desc,
    create_date,
    modify_date
FROM sys.objects
WHERE type IN ('FT', 'FS', 'AF', 'PC')  -- CLR scalar, TVF, aggregate, procedure
ORDER BY FunctionName;
```

**Verify Signing Status:**

```sql
EXEC sp_helptext 'dbo.clr_CosineSimilarity';
-- Should show EXTERNAL NAME clause (indicates CLR function)

SELECT 
    name,
    ASSEMBLYPROPERTY(name, 'PublicKey') AS PublicKey,
    ASSEMBLYPROPERTY(name, 'VersionMajor') AS VersionMajor,
    ASSEMBLYPROPERTY(name, 'VersionMinor') AS VersionMinor
FROM sys.assemblies
WHERE name = 'Hartonomous.Clr';
```

---

## Phase 7: Rollback Procedures

### 7.1 Uninstall CLR Assemblies

```sql
USE Hartonomous;
GO

-- Drop CLR function wrappers
DROP FUNCTION IF EXISTS dbo.clr_CosineSimilarity;
DROP AGGREGATE IF EXISTS dbo.clr_VectorAverage;
DROP PROCEDURE IF EXISTS dbo.sp_AnalyzeSystem;
-- Repeat for all 138 CLR functions...

-- Drop main assembly
DROP ASSEMBLY IF EXISTS [Hartonomous.Clr];

-- Drop external dependencies (reverse dependency order)
DROP ASSEMBLY IF EXISTS [System.Runtime.Intrinsics];
DROP ASSEMBLY IF EXISTS [Newtonsoft.Json];
DROP ASSEMBLY IF EXISTS [MathNet.Numerics];
DROP ASSEMBLY IF EXISTS [System.Memory];
DROP ASSEMBLY IF EXISTS [System.Numerics.Vectors];
DROP ASSEMBLY IF EXISTS [System.Buffers];
DROP ASSEMBLY IF EXISTS [System.Runtime.CompilerServices.Unsafe];
GO
```

### 7.2 Remove SQL Server Trust

```sql
USE master;
GO

-- Revoke UNSAFE ASSEMBLY permission
REVOKE UNSAFE ASSEMBLY FROM HartonomousCertLogin;

-- Drop certificate login
DROP LOGIN HartonomousCertLogin;

-- Drop certificate
DROP CERTIFICATE HartonomousCertificate;
GO
```

---

## Appendix A: Complete Function Catalog

### Scalar Functions (49)

- Distance Metrics: `clr_CosineSimilarity`, `clr_EuclideanDistance`, `clr_ManhattanDistance`
- Vector Operations: `clr_VectorDotProduct`, `clr_VectorNorm`
- Spatial: `clr_ComputeHilbertValue`, `clr_InverseHilbert`, `fn_ProjectTo3D`
- Embeddings: `clr_GenerateCodeAstVector`, `clr_ProjectToPoint`

### Aggregate Functions (23)

- Vector Aggregates: `clr_VectorAverage`, `clr_CentroidAggregate`
- ML Aggregates: `clr_IsolationForestAggregate`, `clr_LOFAggregate`
- Reasoning: `clr_ChainOfThoughtAggregate`, `clr_TreeOfThoughtAggregate`

### Stored Procedures (6)

- Autonomous Operations: `sp_AnalyzeSystem`, `sp_LearnFromPerformance`, `sp_ExecuteActions`

---

## Document Metadata

**Version**: 1.0  
**Last Updated**: November 20, 2025  
**Related Documents**:

- [Enterprise Rollout Plan](../planning/ENTERPRISE_ROLLOUT_PLAN.md)
- [Database Schema Implementation](../architecture/catalog-management.md)
- [CLR Signing Analysis](../../scripts/README-CLR-SIGNING.md)

**Prerequisites**:

- SQL Server 2022+ (CLR Strict Security enabled)
- Windows SDK 10+ (signtool.exe)
- PowerShell 7+
