# VERSION AND COMPATIBILITY AUDIT

**Generated**: November 11, 2025  
**Purpose**: Document current deployment state and technology stack  
**Methodology**: MS Docs research + codebase analysis

---

## Executive Summary

**Current Deployment State**:

- .NET 10 (RC2) across all modern services
- EF Core 10.0.0-rc.2.25502.107 with SQL Server 2025 features (native VECTOR, native JSON)
- Microsoft.Data.SqlClient 6.1.2 (includes 50x faster vector operations)
- SQL CLR uses .NET Framework 4.8.1 (14 assemblies, CPU SIMD-only, no GPU acceleration)

**Technology Stack Requirements**:

1. **SQL Server 2025 Native VECTOR**: EF Core 10.0+ AND Microsoft.Data.SqlClient 6.1.0+
2. **SQL Server 2025 Native JSON**: EF Core 10.0+ AND compatibility level 170+
3. **SQL CLR**: .NET Framework 4.8.1 only (SQL Server 2025 does NOT support .NET Core CLR)
4. **CPU SIMD**: AVX2/SSE4 via System.Numerics.Vectors (no GPU acceleration)

---

## 1. .NET Runtime Versions

### Current State

| Project Category | Target Framework | EF Core Version |
|-----------------|------------------|-----------------|
| **Modern Services** (Api, Infrastructure, Data, Core, Workers, Admin) | `net10.0` | 10.0.0-rc.2.25502.107 |
| **SQL CLR** (SqlClrFunctions.csproj) | `net481` (.NET Framework 4.8.1) | N/A |
| **Tests** (Unit, Integration, E2E) | `net10.0` | 10.0.0-rc.2.25502.107 |

### Why .NET 10 + EF Core 10?

**SQL Server 2025 Feature Requirements** (per MS Docs):

1. **Native VECTOR Data Type**:
   - **Requires**: EF Core 10.0 (NOT EF Core 8 or 9)
   - **Requires**: Microsoft.Data.SqlClient 6.1.0+
   - **Property Type**: `SqlVector<float>` (introduced in EF Core 10)
   - **Performance**: 50x faster reads, 3.3x faster writes, 19x faster bulk copy vs JSON
   - **Source**: [Vector search in EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew#azure-sql-and-sql-server)

2. **Native JSON Data Type**:
   - **Requires**: EF Core 10.0 + SQL Server 2025 compatibility level 170+
   - **Breaking Change**: Upgrading to EF 10 auto-migrates `nvarchar(max)` JSON columns to `json` type
   - **Performance**: Significant efficiency improvements, safer validation, optimized storage
   - **Source**: [JSON type support in EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew#json-type-support)

3. **SIMD/AVX-512**:
   - **Available in**: .NET 8+ (System.Runtime.Intrinsics.Vector512<T>)
   - **Optimal in**: .NET 10 with AVX10.2 hardware instruction support
   - **Codebase Usage**: `VectorMath.cs` exploits AVX-512 for 16-float parallel operations
   - **Source**: [What's new in .NET 10 runtime](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/runtime)

### SQL CLR Constraint

**SQL Server 2025 CLR integration ONLY supports .NET Framework 4.8.1** (not .NET Core/5/6/7/8/9/10).

**Current Deployment**:

- `SqlClrFunctions.csproj` targets `net481`
- 14 assemblies deployed
- CPU SIMD-only (AVX2/SSE4 via VectorMath class)
- No GPU acceleration (ILGPU disabled/commented due to CLR verifier incompatibility with unmanaged pointers; code preserved for potential future use outside SQL CLR)

SQL Server CLR assemblies run inside the SQL Server process, which uses .NET Framework.

---

## 2. Package Version Audit

### Critical Packages (All Projects)

| Package | Current Version | Latest Version | Status | Action |
|---------|----------------|----------------|--------|--------|
| **Microsoft.EntityFrameworkCore** | 10.0.0-rc.2.25502.107 | 10.0.0-rc.2.25502.107 | ✅ LATEST RC | Monitor for GA release |
| **Microsoft.EntityFrameworkCore.SqlServer** | 10.0.0-rc.2.25502.107 | 10.0.0-rc.2.25502.107 | ✅ LATEST RC | Monitor for GA release |
| **Microsoft.Data.SqlClient** | 6.1.2 | 6.1.2 | ✅ LATEST | None |
| **Microsoft.Extensions.Hosting** | 9.0.10, 10.0.0-rc.2 | 10.0.0-rc.2.25502.107 | ⚠️ MIXED | ⬆️ Upgrade all to 10.0 RC2 |
| **Microsoft.Extensions.Configuration.Json** | 9.0.10 | 10.0.0-rc.2.25502.107 | ⚠️ OUTDATED | ⬆️ Upgrade to 10.0 RC2 |
| **Microsoft.Extensions.Logging** | 9.0.10, 10.0.0-rc.2 | 10.0.0-rc.2.25502.107 | ⚠️ MIXED | ⬆️ Upgrade all to 10.0 RC2 |

### Azure SDK Packages

| Package | Current Version | Latest Version | Status | Action |
|---------|----------------|----------------|--------|--------|
| **Azure.Identity** | 1.17.0 | 1.17.0 | ✅ LATEST | None |
| **Azure.Storage.Blobs** | 12.26.0 | 12.26.0 | ✅ LATEST | None |
| **Azure.Storage.Queues** | 12.24.0 | 12.26.0 | ⚠️ OUTDATED | ⬆️ Upgrade to 12.26.0 |
| **Azure.Messaging.ServiceBus** | 7.20.1 | 7.20.1 | ✅ LATEST | None |
| **Azure.Monitor.OpenTelemetry.AspNetCore** | 1.3.0 | 1.3.0 | ✅ LATEST | None |

### Third-Party Libraries

| Package | Current Version | Latest Version | Status | Action |
|---------|----------------|----------------|--------|--------|
| **Neo4j.Driver** | 5.28.3 | 5.28.3 | ✅ LATEST | None |
| **Newtonsoft.Json** | 13.0.3, 13.0.4 | 13.0.4 | ⚠️ MIXED | ⬆️ Standardize on 13.0.4 |
| **Dapper** | 2.1.35 | 2.1.35 | ✅ LATEST | None |
| **Polly** | 8.4.2 | 8.5.0 | ⚠️ OUTDATED | ⬆️ Upgrade to 8.5.0 |
| **NetTopologySuite** | 2.6.0 | 2.6.0 | ✅ LATEST | None |
| **Microsoft.SqlServer.Types** | 160.1000.6 | 160.1000.6 | ✅ LATEST (SQL Server 2025) | None |

### OpenTelemetry Packages

| Package | Current Version | Latest Version | Status | Action |
|---------|----------------|----------------|--------|--------|
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.9.0, 1.13.1 | 1.13.1 | ⚠️ MIXED | ⬆️ Standardize on 1.13.1 |
| **OpenTelemetry.Extensions.Hosting** | 1.9.0, 1.12.0 | 1.12.0 | ⚠️ MIXED | ⬆️ Upgrade all to 1.12.0 |
| **OpenTelemetry.Instrumentation.AspNetCore** | 1.9.0, 1.12.0 | 1.12.0 | ⚠️ MIXED | ⬆️ Upgrade all to 1.12.0 |
| **OpenTelemetry.Instrumentation.Http** | 1.9.0, 1.12.0 | 1.12.0 | ⚠️ MIXED | ⬆️ Upgrade all to 1.12.0 |

### ML/AI Packages

| Package | Current Version | Latest Version | Status | Action |
|---------|----------------|----------------|--------|--------|
| **Microsoft.ML.OnnxRuntime** | 1.22.0 | 1.22.0 | ✅ LATEST | None |
| **TorchSharp** | 0.105.0 | 0.105.0 | ✅ LATEST | None |
| **MathNet.Numerics** | 5.0.0 | 6.0.0-beta1 | ⚠️ OUTDATED | ⬆️ Upgrade to 6.0 beta1 (or wait for stable) |

### Performance/Testing Packages

| Package | Current Version | Latest Version | Status | Action |
|---------|----------------|----------------|--------|--------|
| **BenchmarkDotNet** | 0.14.0 | 0.14.0 | ✅ LATEST | None |
| **Microsoft.Playwright** | 1.48.0 | 1.49.0 | ⚠️ OUTDATED | ⬆️ Upgrade to 1.49.0 |
| **Testcontainers** | 4.2.0 | 4.2.0 | ✅ LATEST | None |

### SQL CLR Dependencies (.NET Framework 4.8.1)

**Current Deployment**: 14 assemblies (CPU SIMD-only, no GPU acceleration)

**Deployed Assemblies** (in dependency order):

1. `System.Runtime.CompilerServices.Unsafe.dll` - Version 4.5.3
2. `System.Buffers.dll` - Version 4.5.1
3. `System.Numerics.Vectors.dll` - Version 4.5.0
4. `System.Memory.dll` - Version 4.5.4
5. `System.Runtime.InteropServices.RuntimeInformation.dll` - Version 4.3.0
6. `System.Collections.Immutable.dll` - Version 1.7.1
7. `System.Reflection.Metadata.dll` - Version 1.8.1
8. `System.ServiceModel.Internals.dll` - GAC copy
9. `SMDiagnostics.dll` - GAC copy
10. `System.Drawing.dll` - GAC copy
11. `System.Runtime.Serialization.dll` - GAC copy
12. `Newtonsoft.Json.dll` - Version 13.0.3 (deployed for dependency resolution, runtime uses GAC version 13.0.0.0 via binding redirect in sqlservr.exe.config)
13. `MathNet.Numerics.dll` - Version 5.0.0
14. `SqlClrFunctions.dll` - Main assembly

**Vector Operations**: CPU SIMD only (AVX2/SSE4 via `VectorMath` class in `Core/VectorMath.cs`)

**GPU Acceleration**: None. ILGPU disabled/commented due to CLR verifier incompatibility with unmanaged GPU memory pointers. Code preserved for potential future implementation outside SQL CLR (e.g., API/worker processes).

**Deployment Script**: `scripts/deploy-clr-secure.ps1` deploys all 14 assemblies with idempotent cleanup (drops all CLR objects including stored procedures type 'PC' before assemblies).

---

## 3. Dependency Hell Issues

### Issue 1: Version Mixing (.NET 9 vs .NET 10)

**Problem**: Some projects use Microsoft.Extensions.* 9.0.10 packages when targeting .NET 10.

**Affected Projects**:
- `Neo4jSync.csproj`: Uses Microsoft.Extensions.Configuration.Json 9.0.10
- `Hartonomous.Workers.Neo4jSync.csproj`: Uses Microsoft.Extensions.Configuration.Json 9.0.10
- `Hartonomous.Core.csproj`: Uses Microsoft.Extensions.Configuration.Abstractions 9.0.10

**Impact**: Potential runtime incompatibilities, missing .NET 10 features.

**Solution**:
```xml
<!-- BEFORE -->
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.10" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.10" />

<!-- AFTER -->
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0-rc.2.25502.107" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0-rc.2.25502.107" />
```

### Issue 2: OpenTelemetry Version Fragmentation

**Problem**: OpenTelemetry packages range from 1.9.0 to 1.13.1 across projects.

**Affected Projects**:
- `Hartonomous.Admin`: Uses 1.9.0 packages
- `Hartonomous.Api`: Uses 1.12.0-1.13.1 packages

**Impact**: Incompatible telemetry data, missing features, potential crashes.

**Solution**:
```xml
<!-- Standardize ALL projects on latest -->
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.13.1" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
```

### Issue 3: Duplicate SQL CLR Projects

**Problem**: Three versions of SqlClrFunctions.csproj with different packages.

**Projects**:
1. `SqlClrFunctions.csproj`: Uses MathNet.Numerics 5.0.0, Newtonsoft.Json 13.0.3
2. `SqlClrFunctions-CLEAN.csproj`: Uses MathNet.Numerics 5.0.0, NO Newtonsoft.Json
3. `SqlClrFunctions-BACKUP.csproj`: Uses System.Text.Json 6.0.0 AND 8.0.5 (CONFLICT!)

**Impact**: **CRITICAL** - Backup project has duplicate System.Text.Json references with different versions.

**Solution**:
1. **DELETE** `SqlClrFunctions-CLEAN.csproj` and `SqlClrFunctions-BACKUP.csproj`
2. Keep only `SqlClrFunctions.csproj`
3. Upgrade Newtonsoft.Json to 13.0.4
4. Remove System.Text.Json (Newtonsoft.Json is the SQL CLR standard)

### Issue 4: Legacy System.Data.SqlClient

**Problem**: `Hartonomous.UnitTests.csproj` uses legacy `System.Data.SqlClient 4.8.6`.

**Impact**: Missing SQL Server 2025 features (native VECTOR, native JSON, performance improvements).

**Solution**:
```xml
<!-- BEFORE -->
<PackageReference Include="System.Data.SqlClient" Version="4.8.6" />

<!-- AFTER (remove entirely, use Microsoft.Data.SqlClient) -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.2" />
```

---

## 4. Breaking Changes and Migration Considerations

### EF Core 10 RC2 → GA (Expected Q1 2026)

**Expected Changes**:
- API stabilization (no breaking changes expected from RC2)
- Performance improvements
- Bug fixes

**Migration Plan**:
1. Monitor [EF Core 10 release notes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
2. Update packages on GA release day
3. Run full test suite
4. Update documentation

### SQL Server 2025 Native JSON Migration

**Breaking Change** (per MS Docs):
> "If you have an existing table and are using `UseAzureSql()`, upgrading to EF 10 will cause a migration to be generated which alters all existing `nvarchar(max)` JSON columns to `json`."

**Affected Tables**:
- `dbo.Atoms`: `Metadata`, `Semantics` (both `nvarchar(max)` JSON)
- `dbo.AtomGraphNodes`: `Metadata` (`nvarchar(max)` JSON)
- `dbo.AtomGraphEdges`: `Metadata` (`nvarchar(max)` JSON)

**Migration Strategy**:
1. **DO NOT** use `UseAzureSql()` yet (avoid auto-migration)
2. Current config uses `UseSqlServer()` with manual compatibility level (good!)
3. **PLANNED**: Manual migration to native `json` type in P2 (after P0/P1 refactoring)
4. **TEST** on non-production database first
5. **BACKUP** before migration

**Performance Impact** (expected):
- ✅ 20-30% faster JSON queries with `JSON_VALUE()` + `RETURNING` clause
- ✅ Automatic validation (prevents invalid JSON inserts)
- ✅ More efficient storage (compression)

---

## 5. Compatibility Matrix

### .NET 10 + SQL Server 2025 Feature Matrix

| Feature | .NET Runtime Requirement | EF Core Requirement | SqlClient Requirement | SQL Server Requirement | Hartonomous Status |
|---------|-------------------------|--------------------|-----------------------|------------------------|-------------------|
| **Native VECTOR Type** | .NET 9+ (recommended .NET 10) | EF Core 10.0+ | Microsoft.Data.SqlClient 6.1.0+ | SQL Server 2025 | ✅ SUPPORTED |
| **Native JSON Type** | .NET 9+ (recommended .NET 10) | EF Core 10.0+ + compat level 170+ | Microsoft.Data.SqlClient 6.0+ | SQL Server 2025 | ✅ READY (not enabled) |
| **SIMD/AVX-512** | .NET 8+ (optimal .NET 10) | N/A | N/A | N/A | ✅ IMPLEMENTED (VectorMath.cs) |
| **Spatial Indexes** | Any | EF Core 8+ | Any | SQL Server 2016+ | ✅ IMPLEMENTED (Geography/Geometry) |
| **SQL CLR Functions** | .NET Framework 4.8.1 | N/A | N/A | SQL Server 2025 | ✅ IMPLEMENTED (SqlClrFunctions) |
| **Columnstore Indexes** | Any | EF Core 8+ | Any | SQL Server 2016+ | ⚠️ PARTIAL (manual SQL only) |
| **Temporal Tables** | Any | EF Core 6+ | Any | SQL Server 2016+ | ✅ IMPLEMENTED (TensorAtomCoefficients_Temporal) |

### SQL CLR Compatibility (Critical)

| SQL Server Version | Supported .NET Framework | CLR Assembly Version | Hartonomous Status |
|-------------------|-------------------------|----------------------|-------------------|
| SQL Server 2025 | .NET Framework 4.8.1 | 4.0.0.0 | ✅ CORRECT (net481) |
| SQL Server 2022 | .NET Framework 4.8 | 4.0.0.0 | ✅ COMPATIBLE |
| SQL Server 2019 | .NET Framework 4.x | 4.0.0.0 | ✅ COMPATIBLE |

**CRITICAL**: SQL Server CLR does **NOT** support:
- ❌ .NET Core
- ❌ .NET 5/6/7/8/9/10
- ❌ .NET Standard libraries (unless explicitly targeting .NET Framework)

---

## 6. Recommended Upgrade Path

### Phase 1: Immediate Fixes (P0 - This Week)

**Goal**: Eliminate version mixing, standardize on latest packages.

1. **Upgrade Microsoft.Extensions.* packages to 10.0 RC2** (15 files affected):
   ```powershell
   # Neo4jSync.csproj, Hartonomous.Workers.Neo4jSync.csproj, Hartonomous.Core.csproj
   dotnet add package Microsoft.Extensions.Configuration.Json --version 10.0.0-rc.2.25502.107
   dotnet add package Microsoft.Extensions.Hosting --version 10.0.0-rc.2.25502.107
   dotnet add package Microsoft.Extensions.Logging --version 10.0.0-rc.2.25502.107
   dotnet add package Microsoft.Extensions.Configuration.Abstractions --version 10.0.0-rc.2.25502.107
   dotnet add package Microsoft.Extensions.Logging.Abstractions --version 10.0.0-rc.2.25502.107
   ```

2. **Standardize OpenTelemetry packages** (6 files affected):
   ```powershell
   # Hartonomous.Admin.csproj (upgrade from 1.9.0)
   dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.13.1
   dotnet add package OpenTelemetry.Extensions.Hosting --version 1.12.0
   dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.12.0
   dotnet add package OpenTelemetry.Instrumentation.Http --version 1.12.0
   ```

3. **Remove legacy System.Data.SqlClient** (Hartonomous.UnitTests.csproj):
   ```xml
   <!-- REMOVE -->
   <PackageReference Include="System.Data.SqlClient" Version="4.8.6" />
   
   <!-- Already has Microsoft.Data.SqlClient 6.1.2, no action needed -->
   ```

4. **Standardize Newtonsoft.Json to 13.0.4** (5 files affected):
   ```powershell
   # SqlClrFunctions.csproj, Hartonomous.Api.csproj
   dotnet add package Newtonsoft.Json --version 13.0.4
   ```

5. **Delete duplicate SQL CLR projects**:
   ```powershell
   Remove-Item src/SqlClr/SqlClrFunctions-CLEAN.csproj
   Remove-Item src/SqlClr/SqlClrFunctions-BACKUP.csproj
   ```

### Phase 2: Minor Upgrades (P1 - Next Week)

**Goal**: Upgrade non-critical packages to latest stable versions.

1. **Upgrade Azure.Storage.Queues**:
   ```powershell
   dotnet add package Azure.Storage.Queues --version 12.26.0
   ```

2. **Upgrade Polly**:
   ```powershell
   dotnet add package Polly --version 8.5.0
   ```

3. **Upgrade Microsoft.Playwright**:
   ```powershell
   dotnet add package Microsoft.Playwright --version 1.49.0
   ```

4. **Consider MathNet.Numerics 6.0 beta1** (performance improvements):
   ```powershell
   # Only if beta is stable enough for SIMD operations
   dotnet add package MathNet.Numerics --version 6.0.0-beta1
   ```

### Phase 3: Monitor for GA Releases (P2 - Ongoing)

**Goal**: Upgrade to stable releases when available.

1. **EF Core 10 GA** (expected Q1 2026):
   ```powershell
   dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.0
   dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
   ```

2. **.NET 10 GA** (expected Q1 2026):
   - Update `global.json` (if exists)
   - Update all .csproj TargetFramework to `net10.0` (already done!)
   - Update CI/CD pipelines to use .NET 10 SDK

3. **Microsoft.Extensions.* 10.0 GA**:
   - Replace all RC2 versions with GA versions
   - Test thoroughly (should be drop-in replacement)

---

## 7. Package Upgrade Validation Script

**Purpose**: Automate version checking, security scanning, and compatibility validation.

**Location**: `scripts/validate-package-versions.ps1`

**Features**:
1. **Version Check**: Compare current vs latest for all packages
2. **Security Scan**: Check for known vulnerabilities using `dotnet list package --vulnerable`
3. **Dependency Analysis**: Identify transitive dependency conflicts
4. **RC2 Tracking**: Flag all preview/RC packages for GA monitoring
5. **Report Generation**: JSON output for automation

**Usage**:
```powershell
./scripts/validate-package-versions.ps1 -ReportPath "version-report.json" -CheckSecurity

# Example output:
# ✅ 87 packages up-to-date
# ⚠️ 12 packages have newer versions available
# ⚠️ 8 packages are preview/RC (monitor for GA)
# ❌ 2 packages have security vulnerabilities
# 📊 Report saved to version-report.json
```

---

## 8. Documentation Updates Required

### README.md

**Add Section**: "Prerequisites - Detailed Version Requirements"

```markdown
### Prerequisites

**Required**:
- .NET 10 SDK (RC2 or later) - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2025 Preview (CTP3 or later) with:
  - CLR Integration enabled (`sp_configure 'clr enabled', 1`)
  - FILESTREAM enabled
  - Service Broker enabled
  - .NET Framework 4.8.1 runtime (for SQL CLR assemblies)
- PowerShell 7.4+ (for deployment scripts)

**Optional**:
- Neo4j 5.28+ (for graph sync worker)
- Azure Storage Account (for blob ingestion)
- Azure App Configuration (for feature flags)

**SQL Server 2025 Feature Support**:
- ✅ Native VECTOR data type (requires EF Core 10.0 + Microsoft.Data.SqlClient 6.1.0+)
- ✅ Native JSON data type (requires EF Core 10.0 + compatibility level 170+)
- ✅ Spatial indexes (SqlGeography/SqlGeometry)
- ✅ SQL CLR functions (.NET Framework 4.8.1)
```

### DEPLOYMENT.md

**Add Section**: "Version Compatibility Matrix"

```markdown
## Version Compatibility Matrix

| Component | Minimum Version | Recommended Version | Hartonomous Uses |
|-----------|----------------|---------------------|------------------|
| .NET SDK | 10.0.0-rc.2 | 10.0.0 (GA when available) | 10.0.0-rc.2.25502.107 |
| SQL Server | 2025 CTP3 | 2025 RTM (when available) | 2025 Preview |
| EF Core | 10.0.0-rc.2 | 10.0.0 (GA when available) | 10.0.0-rc.2.25502.107 |
| Microsoft.Data.SqlClient | 6.1.0 | 6.1.2+ | 6.1.2 |
| .NET Framework (SQL CLR) | 4.8.1 | 4.8.1 | 4.8.1 |

**Breaking Changes**:
- Upgrading to EF Core 10 with `UseAzureSql()` auto-migrates JSON columns from `nvarchar(max)` to `json` type
- SQL Server 2025 CLR requires .NET Framework 4.8.1 (not .NET Core)
```

### CLR_GUIDE.md

**Add Section**: ".NET Framework Requirement Explained"

```markdown
## Why .NET Framework 4.8.1 for SQL CLR?

**Critical**: SQL Server CLR integration runs assemblies **inside the SQL Server process**, which uses .NET Framework, not .NET Core.

**Architecture**:
```
SQL Server 2025 Process (Windows)
│
├─ Database Engine (native code)
├─ .NET Framework 4.8.1 CLR Host
│  ├─ SqlClrFunctions.dll (your code)
│  ├─ MathNet.Numerics.dll
│  └─ Microsoft.SqlServer.Types.dll
└─ Native SQL Functions
```

**This means**:
- ✅ Your CLR functions CAN use .NET Framework 4.8.1 APIs
- ✅ Your CLR functions CAN use .NET Framework compatible NuGet packages
- ❌ Your CLR functions CANNOT use .NET Core/.NET 5/6/7/8/9/10 APIs
- ❌ Your CLR functions CANNOT use .NET Standard libraries that depend on .NET Core

**Separate Concerns**:
- **Modern .NET Services** (Api, Workers, Infrastructure): Use .NET 10 for modern features
- **SQL CLR Functions** (SqlClrFunctions.csproj): Use .NET Framework 4.8.1 for SQL Server compatibility
- **Communication**: Modern services call CLR functions via SQL queries (no direct interop)
```

---

## 9. Current Deployment State

**Technology Stack**:

- .NET 10 RC2 + EF Core 10 RC2 + SQL Server 2025
- Microsoft.Data.SqlClient 6.1.2 (50x faster vector operations)
- SQL CLR: 14 assemblies, .NET Framework 4.8.1, CPU SIMD-only (no GPU)

**SQL CLR Assembly Deployment**:

1. System.Runtime.CompilerServices.Unsafe 4.5.3
2. System.Buffers 4.5.1
3. System.Numerics.Vectors 4.5.0
4. System.Memory 4.5.4
5. System.Runtime.InteropServices.RuntimeInformation 4.3.0
6. System.Collections.Immutable 1.7.1
7. System.Reflection.Metadata 1.8.1
8. System.ServiceModel.Internals (GAC copy)
9. SMDiagnostics (GAC copy)
10. System.Drawing (GAC copy)
11. System.Runtime.Serialization (GAC copy)
12. Newtonsoft.Json 13.0.3 (deployed for dependency resolution, runtime uses GAC 13.0.0.0 via binding redirect)
13. MathNet.Numerics 5.0.0
14. SqlClrFunctions (main assembly)

**Vector Operations**: CPU SIMD (AVX2/SSE4) via VectorMath class in Core/VectorMath.cs

**GPU Acceleration**: None. ILGPU disabled/commented due to CLR verifier incompatibility with unmanaged pointers. Code preserved for potential future implementation outside SQL CLR.

**Deployment Script**: `scripts/deploy-clr-secure.ps1` handles idempotent cleanup and assembly registration with `sys.sp_add_trusted_assembly`.

**SQL Server Configuration**: Binding redirect for Newtonsoft.Json configured in `deploy/sqlservr.exe.config` to use GAC version 13.0.0.0 at runtime.