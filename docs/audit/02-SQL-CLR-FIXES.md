# Phase 2: SQL CLR Fixes

**Priority**: HIGH
**Estimated Time**: 1-2 hours
**Dependencies**: Phase 1 complete (NuGet restore working)

## Overview

Apply research findings to SqlClr codebase. Remove impractical TODOs and document constraints.

---

## Task 2.1: Remove LayerNorm TODOs

**Status**: ❌ NOT STARTED
**Research**: FINDING 15-26 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
`src/SqlClr/TensorOperations/TransformerInference.cs` lines 60, 64:
```csharp
// TODO: Add LayerNorm
// TODO: Add second LayerNorm
```

### Why These TODOs Are Impractical

**FINDING 21**: LayerNorm computational requirements:
- ~1000 operations per token
- 12-layer transformer = 12,000 ops per sequence
- Batch of 32 sequences = 384,000 operations
- Pure managed code = ~100x slower than GPU

**FINDING 24**: SQL CLR should handle:
- Vector dot products (hundreds of ops)
- Aggregations (AVG, MAX, custom)
- Spatial operations (GEOMETRY calculations)

**FINDING 24**: SQL CLR should NOT handle:
- Full transformer inference (billions of ops)
- LayerNorm (too compute-intensive)
- Attention mechanisms (matrix ops too slow)

### Solution

Replace TODOs with explanatory comments:

**File**: `src/SqlClr/TensorOperations/TransformerInference.cs`

**Line 60** - Replace:
```csharp
// TODO: Add LayerNorm
```

With:
```csharp
// LayerNorm NOT implemented in SQL CLR
// Reason: ~1000 ops/token * 12 layers = 12K ops/sequence
// Pure managed code performance insufficient for this workload
// Use external API (Python + PyTorch/ONNX) for full transformer inference
// SQL CLR best suited for lightweight ops: dot product, aggregations, spatial
// See: docs/SQL_CLR_RESEARCH_FINDINGS.md FINDING 21-26
```

**Line 64** - Replace:
```csharp
// TODO: Add second LayerNorm
```

With:
```csharp
// Second LayerNorm also omitted - see comment at line 60
```

### Verification
```powershell
cd src/SqlClr
Get-ChildItem -Recurse -Include *.cs | Select-String "TODO.*LayerNorm"
# Should return 0 results
```

### Files Affected
- `src/SqlClr/TensorOperations/TransformerInference.cs`

---

## Task 2.2: Document SQL CLR Capabilities

**Status**: ❌ NOT STARTED
**Research**: FINDING 1-14 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
No documentation in SqlClr project about what works/doesn't work.

### Solution

Create `src/SqlClr/README.md`:

```markdown
# SQL CLR Functions - Constraints and Capabilities

## .NET Framework Requirements

- **Framework**: .NET Framework 4.8.1 ONLY
- **No Support For**: .NET Core, .NET Standard, .NET 6/8/9
- **Assembly Type**: Pure managed code only (SAFE permission)

## Supported Operations

✅ Vector math (dot product, cosine similarity)
✅ Custom aggregates (weighted averages, spatial)
✅ GEOMETRY/GEOGRAPHY operations
✅ JSON processing (System.Text.Json)
✅ Matrix operations (MathNet.Numerics)

## NOT Supported

❌ SIMD intrinsics (creates mixed assemblies)
❌ P/Invoke / unmanaged code
❌ .NET Standard 2.0 bridge assemblies
❌ Heavy ML inference (use external API)

## Performance Guidelines

**Good fit**: Hundreds to thousands of operations
**Poor fit**: Millions+ operations (use Python/C++ external API)

**Example - Good**:
- Dot product of two 1536-dim vectors
- Custom aggregate over 10K rows
- Spatial distance calculations

**Example - Poor**:
- LayerNorm (12K+ ops per sequence)
- Full transformer inference
- BERT/GPT model execution

## References

- Official docs: https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/
- Research findings: docs/SQL_CLR_RESEARCH_FINDINGS.md
- Architecture: docs/ARCHITECTURE.md
```

### Verification
File exists at `src/SqlClr/README.md`

---

## Task 2.3: Verify All SqlClr Builds

**Status**: ❌ NOT STARTED
**Dependencies**: Task 1.1 (NuGet restore), Task 1.3 (Sql.Bridge fixes)

### Build Commands

```powershell
cd D:\Repositories\Hartonomous\src\SqlClr

# Clean
dotnet clean

# Restore
dotnet restore

# Build Debug
dotnet build -c Debug

# Build Release
dotnet build -c Release
```

### Success Criteria

All builds complete with:
- ✅ 0 Errors
- ⚠️  Warnings OK (untested assembly warnings expected)
- ✅ Output: `bin/Release/net481/SqlClrFunctions.dll`

### Common Issues

**Issue**: "Type or namespace 'Text' does not exist in namespace 'System'"
**Fix**: Task 1.1 not complete - NuGet restore failed

**Issue**: "Type or namespace 'Bridge' does not exist"
**Fix**: Task 1.3 not complete - Sql.Bridge references remain

**Issue**: "Type or namespace 'Numerics' does not exist in namespace 'MathNet'"
**Fix**: Task 1.1 not complete - MathNet.Numerics not restored

### Files Affected
- All `.cs` files in `src/SqlClr/`

---

## Task 2.4: Add XML Comments to Public APIs

**Status**: ❌ NOT STARTED
**Optional**: YES (improves maintainability)

### Problem
SQL CLR functions lack XML documentation.

### Solution
Add `<summary>` tags to all `[SqlFunction]` and `[SqlAggregate]` methods.

Example:
```csharp
/// <summary>
/// Computes cosine similarity between two vectors stored as GEOMETRY points.
/// Returns value in range [-1, 1] where 1 = identical, 0 = orthogonal, -1 = opposite.
/// </summary>
/// <param name="vector1">First vector as GEOMETRY POINT</param>
/// <param name="vector2">Second vector as GEOMETRY POINT</param>
/// <returns>Cosine similarity score</returns>
[SqlFunction(IsDeterministic = true, IsPrecise = false)]
public static SqlDouble CosineSimilarity(SqlGeometry vector1, SqlGeometry vector2)
```

### Verification
All public methods have XML comments

---

## Success Criteria

Phase 2 complete when:
- ✅ LayerNorm TODOs removed with explanations
- ✅ SqlClr README.md created
- ✅ All SqlClr builds (Debug + Release) with 0 errors
- ✅ XML comments added (optional)
- ✅ Changes committed to git

## Next Phase

After Phase 2 complete → `03-TEMPORAL-TABLES.md`
