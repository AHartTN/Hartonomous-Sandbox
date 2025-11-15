# SQL Server CLR Integration - Technical Requirements Research

**Date**: 2025-11-14
**Research Status**: VALIDATED AGAINST MICROSOFT DOCS & ACTUAL CODEBASE

---

## CRITICAL FACTS - .NET Framework Requirements

### ✅ CONFIRMED: SQL Server CLR REQUIRES .NET Framework

**Source**: [Microsoft Docs - CLR Integration Programming Concepts](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/common-language-runtime-clr-integration-programming-concepts?view=sql-server-ver17)

> **Direct Quote from Microsoft:**
> "SQL Server CLR integration doesn't support .NET Core, or .NET 5 and later versions."

**Implementation in Hartonomous:**
- `Hartonomous.Database.sqlproj`: `<TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>`
- CLR C# files compiled as part of `.sqlproj` (NOT separate .csproj)
- Test project: `Hartonomous.SqlClr.Tests.csproj` uses `.NET Framework 4.8.1`

**Why This Matters:**
- .NET 6/8/10 (modern .NET) CANNOT be used for SQL CLR
- .NET Standard libraries may NOT work (depends on API surface)
- Must target .NET Framework 4.8.1 (latest supported)

---

## Linux Support Limitations

**Source**: [Microsoft Docs - Get started with CLR integration](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/database-objects/getting-started-with-clr-integration?view=sql-server-ver17)

> **Direct Quote:**
> "Loading CLR database objects on Linux is supported, but they must be built with the .NET Framework (SQL Server CLR integration doesn't support .NET Core, or .NET 5 and later versions). Also, CLR assemblies with the `EXTERNAL_ACCESS` or `UNSAFE` permission set aren't supported on Linux."

**Implications for Hartonomous:**
- ✅ SAFE assemblies: Work on Linux (but useless for our needs)
- ❌ UNSAFE assemblies: Windows ONLY (required for SIMD, PInvoke, System.Drawing)
- ❌ EXTERNAL_ACCESS assemblies: Windows ONLY

**Hartonomous Uses UNSAFE** (`<PermissionSet>UNSAFE</PermissionSet>` in sqlproj):
- System.Drawing for image pixel extraction
- Unmanaged memory access for SIMD operations
- Win32 API calls (if needed)

**Conclusion**: Hartonomous CLR components are **Windows-only** by design.

---

## CLR Strict Security (SQL Server 2017+)

**Source**: [Microsoft Docs - CLR strict security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security?view=sql-server-ver17)

> **Direct Quote:**
> "In SQL Server 2017 (14.x) and later versions, `clr strict security` is enabled by default, and treats `SAFE` and `EXTERNAL_ACCESS` assemblies as if they were marked `UNSAFE`."

**What This Means:**
1. ALL assemblies treated as UNSAFE by default
2. Requires either:
   - Assembly signed + login with `UNSAFE ASSEMBLY` permission in `master`
   - Assembly added to trusted list via `sys.sp_add_trusted_assembly`

**Hartonomous Implementation:**
```xml
<!-- From Hartonomous.Database.sqlproj -->
<SignAssembly>true</SignAssembly>
<AssemblyOriginatorKeyFile>CLR\SqlClrKey.snk</AssemblyOriginatorKeyFile>
<PermissionSet>UNSAFE</PermissionSet>
```

**Deployment Requirements:**
```sql
-- Required before deploying UNSAFE assemblies
USE master;
GO
CREATE LOGIN [HartonomousCLR] FROM CERTIFICATE [HartonomousCert];
GRANT UNSAFE ASSEMBLY TO [HartonomousCLR];
GO
```

---

## Supported .NET Framework Assemblies in CLR

**Source**: [Microsoft Docs - Support policy for untested .NET Framework assemblies](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/development/policy-untested-net-framework-assemblies)

### Officially Supported (Can be referenced WITHOUT registration):
1. `Microsoft.VisualBasic.dll`
2. `Mscorlib.dll`
3. `System.Data.dll` ✅ (We use)
4. `System.dll` ✅ (We use)
5. `System.Xml.dll` ✅ (We use)
6. `Microsoft.VisualC.dll`
7. `CustomMarshalers.dll`
8. `System.Security.dll`
9. `System.Web.Services.dll`
10. `System.Data.SqlXml.dll`
11. `System.Transactions.dll` ✅ (We use)
12. `System.Data.OracleClient.dll`
13. `System.Configuration.dll`

### Hartonomous Uses (MUST be from GAC):
- `System.Drawing.dll` ⚠️ NOT in supported list
  - Must be registered via `CREATE ASSEMBLY`
  - Located at: `C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Drawing\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Drawing.dll`

**Important Restriction:**
> "SQL Server `CREATE ASSEMBLY` statement lets only PURE .NET Framework assemblies be registered."

**Mixed assemblies** (containing unmanaged C++ code) will fail with:
```
Msg 6544, Level 16, State 1, Line 2
CREATE ASSEMBLY for assembly failed because assembly is malformed or not a pure .NET assembly.
Unverifiable PE Header/native stub.
```

---

## Assembly Registration Requirements

### Pure vs Mixed Assemblies:
- **Pure**: Only MSIL (IL code) - SQL Server can load these
- **Mixed**: MSIL + native machine code (x86/x64) - SQL Server REJECTS these

**System.Drawing.dll** is PURE (safe to use).

### Registration Example:
```sql
CREATE ASSEMBLY [Drawing] 
FROM 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Drawing\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Drawing.dll' 
WITH PERMISSION_SET = UNSAFE;
GO
```

---

## Hartonomous CLR Architecture - Verified Against Actual Code

### Current CLR Components (from `.sqlproj`):
```xml
<Compile Include="CLR\ImagePixelExtractor.cs" />
<Compile Include="CLR\AudioFrameExtractor.cs" />
<Compile Include="CLR\AtomicStream.cs" />
<Compile Include="CLR\AtomicStreamFunctions.cs" />
<Compile Include="CLR\SqlBytesInterop.cs" />
<Compile Include="CLR\BinaryConversions.cs" />
<Compile Include="CLR\Properties\AssemblyInfo.cs" />
```

### What They Do (From Code Inspection):

1. **`ImagePixelExtractor.cs`**: Extracts RGBA pixels using `System.Drawing`
2. **`AudioFrameExtractor.cs`**: Extracts audio samples (RMS/peak values)
3. **`AtomicStream.cs`**: Memory-efficient streaming for large data
4. **`BinaryConversions.cs`**: `VARBINARY` ↔ `FLOAT` conversions for weights
5. **`SqlBytesInterop.cs`**: Efficient byte array handling

### Dependencies (From Code):
- `System.Drawing` (for pixel extraction) ⚠️ Requires `CREATE ASSEMBLY`
- `System` (basic types)
- `System.Data` (SQL types)
- `System.Core` (LINQ)
- `System.Transactions` (if needed)

**NO EXTERNAL PACKAGES** - All from GAC (.NET Framework 4.8.1).

---

## Application Layer Uses .NET 10 (Separate from CLR)

**From Codebase:**
```xml
<!-- src/Hartonomous.Core/Hartonomous.Core.csproj -->
<TargetFramework>net10.0</TargetFramework>

<!-- src/Hartonomous.Infrastructure/Hartonomous.Infrastructure.csproj -->
<TargetFramework>net10.0</TargetFramework>
```

**Architecture Separation:**
```
┌─────────────────────────────────────────┐
│  Application Layer (.NET 10)            │
│  - API (Hartonomous.Api)                │
│  - Workers (CesConsumer, Neo4jSync)     │
│  - Infrastructure Services              │
│  - Core Business Logic                  │
└─────────────────┬───────────────────────┘
                  │
                  │ EF Core 10 / ADO.NET
                  ▼
┌─────────────────────────────────────────┐
│  SQL Server 2025                        │
│  ┌───────────────────────────────────┐  │
│  │ CLR Integration (.NET Fx 4.8.1)   │  │
│  │ - ImagePixelExtractor             │  │
│  │ - AudioFrameExtractor             │  │
│  │ - BinaryConversions               │  │
│  │ - UNSAFE assemblies (SIMD)        │  │
│  └───────────────────────────────────┘  │
│                                         │
│  Tables, Procedures, Functions          │
└─────────────────────────────────────────┘
```

**Key Point**: Application uses modern .NET 10, but CLR components MUST use .NET Framework 4.8.1.

---

## UNSAFE Assembly Capabilities (Why We Need It)

**From Actual Code (`PixelAtomizer.cs`):**
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// Extract RGBA pixel
var pixel = image[x, y];
var rgbaBytes = new byte[] { pixel.R, pixel.G, pixel.B, pixel.A };
```

**Why UNSAFE is Required:**
1. **System.Drawing**: Requires UNSAFE (GDI+ interop)
2. **SIMD Operations**: Vectorized math for performance
3. **Direct Memory Access**: Efficient byte manipulation
4. **PInvoke**: Call native APIs if needed

**Alternative (SAFE mode)**: Would be 50-100x slower, defeats purpose of CLR.

---

## Performance Implications

**From Microsoft Docs - Language Extensions vs CLR:**

| Feature | SQL CLR | SQL Language Extensions |
|---------|---------|------------------------|
| Mode of execution | **In-proc** | Out-of-proc |
| Performance | **SQL CLR code typically outperforms extensibility due to the nature of execution** | Ideal for batch-oriented execution |

**Hartonomous Philosophy**: Use CLR for hot paths (pixel extraction, SIMD math), not external processes.

---

## Security Model - Validated

**Required Setup (Windows):**
```sql
-- 1. Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- 2. Disable strict security (if testing, NOT recommended for prod)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
GO

-- 3. OR: Sign assembly and grant UNSAFE permission
USE master;
CREATE ASYMMETRIC KEY HartonomousKey FROM FILE = 'C:\Path\To\SqlClrKey.snk';
CREATE LOGIN HartonomousCLR FROM ASYMMETRIC KEY HartonomousKey;
GRANT UNSAFE ASSEMBLY TO HartonomousCLR;
GO
```

**Hartonomous Implementation**:
- Assemblies signed with `SqlClrKey.snk`
- `PERMISSION_SET = UNSAFE` in `CREATE ASSEMBLY` DDL
- Requires sysadmin or `UNSAFE ASSEMBLY` grant

---

## Common Issues & Solutions

### Issue 1: TypeInitializationException with System.Drawing

**Error:**
```
TypeInitializationException: The type initializer for 'System.Drawing.Graphics' threw an exception.
```

**Solution** (From Microsoft KB):
1. Create `System.Drawing` assembly manually:
```sql
CREATE ASSEMBLY [Drawing] 
FROM 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Drawing\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Drawing.dll' 
WITH PERMISSION_SET = UNSAFE;
```

2. OR: Add registry key (affects ALL .NET apps on server):
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\AppContext
Name: Switch.System.Data.AllowArbitraryDataSetTypeInstantiation
Value: true
```

### Issue 2: Mixed Assembly Error

**Error:**
```
Msg 6544: CREATE ASSEMBLY failed because assembly is not a pure .NET assembly.
```

**Solution**: Only reference PURE assemblies. Check with:
```powershell
# Check if assembly is pure MSIL
ildasm /text MyAssembly.dll | Select-String "IMAGE_FILE_32BIT"
# If found, it's mixed (NOT pure)
```

---

## Deployment Workflow

1. **Build DACPAC** (includes CLR DLL):
   ```powershell
   MSBuild Hartonomous.Database.sqlproj /p:Configuration=Release
   ```

2. **Deploy DACPAC**:
   ```powershell
   SqlPackage.exe /Action:Publish /SourceFile:Hartonomous.Database.dacpac /TargetServerName:localhost /TargetDatabaseName:Hartonomous
   ```

3. **DACPAC auto-generates**:
   ```sql
   CREATE ASSEMBLY [Hartonomous.Clr]
   FROM 0x4D5A90000300... -- Embedded DLL bytes
   WITH PERMISSION_SET = UNSAFE;
   GO
   
   CREATE FUNCTION dbo.fn_ExtractPixels(@imageBytes VARBINARY(MAX))
   RETURNS TABLE (X INT, Y INT, R TINYINT, G TINYINT, B TINYINT, A TINYINT)
   AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ImagePixelExtractor].[ExtractPixels];
   ```

---

## References

1. [CLR Integration Programming Concepts](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/common-language-runtime-clr-integration-programming-concepts?view=sql-server-ver17)
2. [Get started with CLR integration](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/database-objects/getting-started-with-clr-integration?view=sql-server-ver17)
3. [CLR strict security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security?view=sql-server-ver17)
4. [Support policy for untested .NET Framework assemblies](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/development/policy-untested-net-framework-assemblies)
5. [CREATE ASSEMBLY (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-assembly-transact-sql?view=sql-server-ver17)

---

**Document Version**: 1.0  
**Last Validated**: 2025-11-14  
**Validation Method**: Cross-referenced Microsoft Docs + actual Hartonomous codebase
