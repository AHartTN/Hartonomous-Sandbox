# SIMD Restoration Status - 2025-11-08

## Goal
Restore System.Numerics.Vectors SIMD optimizations to SqlClr that were incorrectly removed in commit 1e60112.

## What Was Done This Session

### 1. Confirmed SIMD Support in SQL CLR
- System.Numerics.Vectors 4.5.0 IS supported - it's pure MSIL (processorarchitecture=msil), not a mixed assembly
- SQL Server accepts it with warning but loads successfully
- The removal in commit 1e60112 was based on incorrect assumption

### 2. Restored SIMD Code
- **VectorMath.cs**: Re-added SIMD acceleration using System.Numerics.Vector<float>:
  - DotProduct: SIMD vectorized
  - Norm: SIMD vectorized
  - Added ComputeCentroid(float[][], float[]) overload for BehavioralAggregates compatibility
- **LandmarkProjection.cs**: Fixed via git checkout from commit before removal
  - Fixed variable scope issue (i declared in two places)
- **TSNEProjection.cs**: Restored from git
- **BehavioralAggregates.cs**: Restored from git
- **TransformerInference.cs**: 
  - Restored from git
  - Fixed Softmax() method - MathNet.Numerics Matrix<float> doesn't have RowMaximums()
  - Implemented manual row-wise max, exp, sum, normalize

### 3. Build Status
SqlClr compiles successfully with SIMD optimizations restored (7 warnings, 0 errors)

### 4. Fixed Deployment Script (deploy-clr-secure.ps1)
- **SQL Server 2025 compatibility**: Changed sys.types to sys.assembly_types (schema changed in 2025)
- **PowerShell string concatenation**: Added explicit newlines before each SQL block to prevent "GOIF" errors
- **Assembly order**: Moved System.Numerics.Vectors before System.Memory (dependency order)

## Current Blocker: NuGet Package Version Hell

### The Core Problem
SQL Server CLR requires EXACT assembly version matches in IL metadata. Cannot use binding redirects or have multiple versions of the same assembly.

### Discovered Dependencies (via PowerShell inspection of bin\Release DLLs)

```
System.Memory.dll references:
  - System.Runtime.CompilerServices.Unsafe 4.0.4.1
  - System.Buffers 4.0.3.0

System.Text.Json.dll references:
  - System.Runtime.CompilerServices.Unsafe 6.0.0.0
  - System.Memory 4.0.1.2
  - System.Buffers 4.0.3.0

System.Text.Encodings.Web.dll references:
  - System.Runtime.CompilerServices.Unsafe 6.0.0.0
  - System.Memory 4.0.1.2
  - System.Buffers 4.0.3.0
```

**CONFLICT**: System.Memory wants Unsafe 4.0.4.1 but System.Text.Json wants Unsafe 6.0.0.0

### Current Package Versions in SqlClrFunctions.csproj
- System.Memory 4.5.5
- System.Text.Json 8.0.5
- System.Runtime.CompilerServices.Unsafe 4.5.3 (was 6.0.0, changed during debugging)
- System.Numerics.Vectors 4.5.0
- MathNet.Numerics 5.0.0

### Research Done (via NuGet.org dependencies tab)

**System.Text.Json versions for .NET Framework 4.6.1/4.6.2**:
- 8.0.5: Requires Unsafe >= 6.0.0
- 6.0.0: Requires Unsafe >= 6.0.0  
- 5.0.2: Requires Unsafe >= 5.0.0
- 4.7.2: Requires Unsafe >= 4.7.1

**System.Memory 4.5.5 for .NET Framework 4.6.1**:
- Requires Unsafe >= 4.5.3
- Requires System.Numerics.Vectors >= 4.5.0
- Requires System.Buffers >= 4.5.1

### SQL Server CLR Assembly Loading Constraints
From Microsoft Docs research: "The .NET Framework CLR demands an exact match to load a strong-named assembly" - no binding redirects supported in SQL CLR.

## Failed Deployment Attempts

### Attempt 1: Deploy with Unsafe 6.0.0
Error: "Assembly 'system.runtime.compilerservices.unsafe, version=4.0.4.1' was not found in the SQL catalog"
- System.Memory's IL metadata references 4.0.4.1 but we deployed 6.0.0.0

### Attempt 2: Try to deploy both Unsafe versions
- User correctly pointed out SQL Server doesn't support multiple versions of same assembly
- Cannot have both Unsafe 4.0.4.1 and 6.0.0.0

## Solution Options (Not Yet Implemented)

### Option 1: Downgrade System.Text.Json to 4.7.2
- Would need Unsafe >= 4.7.1 (compatible with Memory's >= 4.5.3 requirement)
- But need to verify actual IL references in the DLL

### Option 2: Find System.Memory version that uses Unsafe 6.0.0
- Research needed on NuGet to find if such a version exists

### Option 3: Remove System.Text.Json dependency
- Evaluate if JSON serialization is critical for SQL CLR
- Use alternative JSON library or implement manual serialization

## Files Modified This Session
- src/SqlClr/SqlClrFunctions.csproj (Unsafe version changed 6.0.0 → 4.5.3)
- src/SqlClr/Core/VectorMath.cs (SIMD restored + overload added)
- src/SqlClr/Core/LandmarkProjection.cs (restored with scope fix)
- src/SqlClr/MachineLearning/TSNEProjection.cs (restored from git)
- src/SqlClr/BehavioralAggregates.cs (restored from git)
- src/SqlClr/TensorOperations/TransformerInference.cs (restored + Softmax fixed)
- scripts/deploy-clr-secure.ps1 (SQL 2025 fixes + newlines + assembly order)

## Git Status
All changes staged but NOT committed - waiting for deployment to succeed before commit.

## Next Session Action Items
1. Research System.Text.Json 4.7.x actual DLL dependencies (not just NuGet metadata)
2. OR research newer System.Memory versions
3. Find compatible version set where ALL DLLs reference the SAME Unsafe version
4. Update csproj with compatible versions
5. Clean + rebuild
6. Verify DLL references with PowerShell
7. Deploy to SQL Server
8. Commit successful SIMD restoration
