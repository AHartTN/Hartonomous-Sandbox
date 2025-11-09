# Phase 1: Critical Fixes

**Priority**: HIGHEST - BLOCKS ALL OTHER WORK
**Estimated Time**: 2-4 hours
**Dependencies**: None

## Overview

These fixes must be completed first. They block all subsequent work.

---

## Task 1.1: Fix SqlClr NuGet Restore

**Status**: ❌ NOT STARTED
**Blocker**: YES - SqlClr project won't build
**Research**: FINDING 1-3 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
SqlClr project references NuGet packages that won't restore:
- `System.Text.Json` v8.0.5
- `MathNet.Numerics` v5.0.0

Old-style .NET Framework 4.8.1 csproj requires special restore process.

### Solution Options

**Option A: Visual Studio** (Recommended)
1. Open `Hartonomous.sln` in Visual Studio 2022
2. Right-click `SqlClr` project → Restore NuGet Packages
3. Build project
4. Verify no errors

**Option B: Command Line**
```powershell
cd D:\Repositories\Hartonomous
nuget.exe restore src/SqlClr/SqlClrFunctions.csproj
# OR if nuget.exe not available:
dotnet restore src/SqlClr/SqlClrFunctions.csproj -p:TargetFramework=net481
```

**Option C: Install .NET Framework SDK**
If restore fails, may need full .NET Framework 4.8.1 Developer Pack:
https://dotnet.microsoft.com/download/dotnet-framework/net481

### Verification
```powershell
cd src/SqlClr
dotnet build -c Release
# Should complete with 0 errors
```

### Files Affected
- `src/SqlClr/SqlClrFunctions.csproj` (packages.config restoration)
- `src/SqlClr/JsonProcessing/JsonSerializerImpl.cs` (uses System.Text.Json)
- `src/SqlClr/MachineLearning/*.cs` (uses MathNet.Numerics)

### Notes
- FINDING 3: These assemblies are "untested" by Microsoft in SQL CLR
- They work but are unsupported by Microsoft CSS
- Alternative: Use only System.Xml for serialization (supported)

---

## Task 1.2: Fix sp_UpdateModelWeightsFromFeedback

**Status**: ❌ NOT STARTED
**Blocker**: YES - Core AGI learning mechanism broken
**Research**: FINDING 27-38 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
Lines 73-92 in `sql/procedures/Feedback.ModelWeightUpdates.sql`:
- Cursor iterates over layers
- Only PRINTs information
- **NEVER actually UPDATEs TensorAtomCoefficients table**
- Result: Model weights never change based on feedback

### Current Code (BROKEN)
```sql
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Layer ' + @currentLayerName + ' (ID: ' + CAST(@currentLayerID AS NVARCHAR(10)) +
          ', ModelID: ' + CAST(@currentModelID AS NVARCHAR(10)) +
          ') - Success count: ' + CAST(@successCount AS NVARCHAR(10)) +
          ', Avg rating: ' + CAST(@avgRating AS NVARCHAR(10)) +
          ', Update magnitude: ' + CAST(@updateMagnitude AS NVARCHAR(20));

    FETCH NEXT FROM layer_cursor INTO @currentLayerID, @currentModelID, @currentLayerName, @successCount, @avgRating, @updateMagnitude;
END;
```

### Solution (FINDING 36)
Replace cursor with set-based UPDATE:

```sql
-- Delete entire cursor block (lines 73-96)
-- Replace with:

-- Apply weight updates based on feedback
UPDATE tac
SET tac.CoefficientValue = tac.CoefficientValue + (
    @LearningRate * 
    @avgRating * 
    @updateMagnitude *
    (SELECT AVG(otac.CoefficientValue) 
     FROM TensorAtomCoefficients otac 
     WHERE otac.TensorAtomID = tac.TensorAtomID)
)
FROM TensorAtomCoefficients tac
INNER JOIN #LayerUpdates lu ON tac.LayerID = lu.LayerID
WHERE ABS(@avgRating * @updateMagnitude) > 0.01;

SET @layersUpdated = @@ROWCOUNT;

PRINT 'Applied weight updates to ' + CAST(@layersUpdated AS NVARCHAR(10)) + ' coefficients';
```

### Verification
```sql
-- Before fix:
SELECT TOP 10 TensorAtomCoefficientID, CoefficientValue 
FROM TensorAtomCoefficients 
ORDER BY LastModified DESC;

-- Execute procedure
EXEC Feedback.sp_UpdateModelWeightsFromFeedback @LearningRate = 0.001;

-- After fix (verify CoefficientValue changed):
SELECT TOP 10 TensorAtomCoefficientID, CoefficientValue 
FROM TensorAtomCoefficients 
ORDER BY LastModified DESC;
```

### Files Affected
- `sql/procedures/Feedback.ModelWeightUpdates.sql` (lines 73-96)

### Notes
- This is THE core AGI learning loop
- Without this, Hartonomous cannot learn from feedback
- FINDING 34: Cursor vs set-based operations (cursors 10-100x slower)

---

## Task 1.3: Fix Sql.Bridge Namespace References

**Status**: ✅ PARTIALLY COMPLETE
**Blocker**: NO - Already fixed in 2 files, but 32 more references exist
**Research**: FINDING 5 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
32 files reference `Hartonomous.Sql.Bridge.*` namespaces.
Sql.Bridge was abandoned .NET Standard 2.0 compatibility layer.
It cannot work with SQL CLR (mixed assembly rejection).

### Already Fixed
- ✅ `src/SqlClr/AutonomousFunctions.cs` 
- ✅ `src/SqlClr/Core/SqlTensorProvider.cs`

### Remaining References
Find all with:
```powershell
cd D:\Repositories\Hartonomous
Get-ChildItem -Recurse -Include *.cs | Select-String "Sql\.Bridge" | Select-Object -Unique Path
```

### Solution Pattern
Replace:
```csharp
using Hartonomous.Sql.Bridge.Contracts;
using Hartonomous.Sql.Bridge.JsonProcessing;
```

With:
```csharp
using SqlClrFunctions.Contracts;
using SqlClrFunctions.JsonProcessing;
```

### Verification
```powershell
# Should return 0 results:
cd src/SqlClr
Get-ChildItem -Recurse -Include *.cs | Select-String "Sql\.Bridge"
```

### Files Affected
Run grep search to find all 32 files

### Notes
- FINDING 5: Sql.Bridge failed because .NET Standard 2.0 → System.Memory → SIMD → mixed assembly
- SQL CLR rejects mixed assemblies
- Local contracts in SqlClr project are correct replacement

---

## Success Criteria

Phase 1 complete when:
- ✅ SqlClr NuGet packages restored
- ✅ SqlClr project builds with 0 errors
- ✅ sp_UpdateModelWeightsFromFeedback actually UPDATEs weights
- ✅ No references to Sql.Bridge remain
- ✅ All changes committed to git

## Next Phase

After Phase 1 complete → `02-SQL-CLR-FIXES.md`
