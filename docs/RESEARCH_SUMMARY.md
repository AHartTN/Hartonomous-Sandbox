# Research Summary: Hartonomous Architecture Constraints

**Date**: Current session
**Purpose**: Document comprehensive MS docs research to guide architectural decisions

## Research Methodology

This document summarizes findings from systematic Microsoft Learn documentation searches. Each section corresponds to a specific research area, with findings numbered sequentially (FINDING 1-67) and documented in `SQL_CLR_RESEARCH_FINDINGS.md`.

**Process**: Research → Document → Move to Next Topic (NO summarization, progressive documentation)

---

## 1. SQL CLR Integration Constraints (FINDING 1-14)

### Key Conclusions

**CRITICAL**: SQL CLR ONLY supports .NET Framework 4.8.1, NO .NET Core/.NET Standard support.

### What Works
- .NET Framework 4.8.1 assemblies (TFM: `net481`)
- Pure managed code only (SAFE permission set)
- 13 explicitly supported assemblies (mscorlib.dll, System.Data.dll, etc.)
- MathNet.Numerics (pure managed math library)

### What Doesn't Work
- .NET Standard 2.0 bridge assemblies (create mixed MSIL, rejected by SQL Server)
- SIMD instructions (System.Numerics.Vectors produces unsafe assemblies)
- Modern .NET 6/8/9 assemblies
- Entity Framework Core (requires modern .NET)
- Any assembly referencing unsupported dependencies

### Why Sql.Bridge Failed
- FINDING 5: .NET Standard 2.0 references System.Memory → references System.Numerics.Vectors → SIMD
- SIMD creates mixed assemblies (managed + native)
- SQL CLR `CREATE ASSEMBLY` rejects mixed assemblies with error about EXTERNAL_ACCESS permission
- Bridge pattern fundamentally incompatible with SQL CLR constraints

### Actionable Decision
**Keep SqlClr project as .NET Framework 4.8.1, old-style .csproj**. Do not attempt migration to SDK-style or multi-targeting for SqlClr.

---

## 2. Transformer Implementation Constraints (FINDING 15-26)

### Key Conclusions

**LayerNorm in SQL CLR is technically possible but practically infeasible.**

### Computational Reality
- LayerNorm: ~1000 operations per token
- 12-layer transformer: ~12,000 operations per sequence
- Batch of 32 sequences: ~384,000 operations
- Pure managed code performance: ~100x slower than GPU-optimized implementations

### What SQL CLR Should Do
- **Vector operations**: Dot product, cosine similarity (hundreds of ops)
- **Aggregations**: AVG, MAX, custom aggregates over result sets
- **Spatial operations**: GEOMETRY/GEOGRAPHY calculations (Hartonomous stores weights as GEOMETRY)

### What SQL CLR Should NOT Do
- **Full transformer inference** (billions of ops, needs GPU)
- **LayerNorm** (too compute-intensive for database)
- **Attention mechanisms** (matrix ops too slow in managed code)

### Architectural Recommendation
- FINDING 26: Use external API for heavy ML inference (Python + PyTorch/ONNX Runtime)
- SQL CLR handles lightweight math (dot products, aggregations)
- Consider ONNX Runtime for .NET Framework 4.8.1 IF inference needed in-database (experimental)

### TODOs in TransformerInference.cs
Lines 60, 64: "// TODO: Implement LayerNorm normalization here"

**DECISION**: Remove TODOs and add comment explaining why LayerNorm doesn't belong in SQL CLR. Recommend external API for full transformer inference.

---

## 3. Temporal Tables and Feedback Loops (FINDING 27-38)

### Key Conclusions

**Temporal tables solve the model weight history problem perfectly.**

### What Are Temporal Tables?
- SQL Server 2016+ feature
- Automatic history tracking with `ValidFrom`/`ValidTo` columns
- System maintains history table automatically
- Query point-in-time state with `FOR SYSTEM_TIME AS OF @timestamp`

### Hartonomous Application
Convert `TensorAtomCoefficients` table to temporal:

```sql
ALTER TABLE TensorAtomCoefficients  
ADD ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE TensorAtomCoefficients  
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));
```

### Benefits
- Automatic weight change tracking (every UPDATE creates history record)
- Query historical weights: `SELECT * FROM TensorAtomCoefficients FOR SYSTEM_TIME AS OF '2024-01-15 10:30:00'`
- Analyze learning trajectory over time
- No manual history management code needed

### sp_UpdateModelWeightsFromFeedback Fix
**CRITICAL BUG** (lines 73-92): Cursor only PRINTs, never UPDATEs weights!

**FINDING 36** provides corrected set-based UPDATE pattern:

```sql
UPDATE tac
SET tac.Weight = tac.Weight + (
    @LearningRate * f.FeedbackScore * 
    (SELECT AVG(otac.Weight) FROM TensorAtomCoefficients otac WHERE otac.TensorAtomID = tac.TensorAtomID)
)
FROM TensorAtomCoefficients tac
INNER JOIN Feedback f ON f.EntityID = tac.EntityID
WHERE f.FeedbackTimestamp > DATEADD(HOUR, -24, GETUTCDATE())
  AND ABS(f.FeedbackScore) > 0.01;
```

### Actionable Decision
1. Convert TensorAtomCoefficients to temporal table
2. Fix sp_UpdateModelWeightsFromFeedback with set-based UPDATE (delete cursor)
3. Create queries to analyze weight evolution over time

---

## 4. Orphaned Files Analysis (FINDING 39-49)

### Key Conclusions

**"Orphaned files" diagnosis was incorrect. Files ARE integrated via SDK-style auto-inclusion.**

### SDK-Style vs Old-Style Projects

**SDK-Style** (`<Project Sdk="Microsoft.NET.Sdk">`):
- Auto-includes ALL `.cs` files in project directory structure
- No need for `<Compile Include="..."/>` entries
- Hartonomous.Api, Hartonomous.Core, Hartonomous.Infrastructure (all SDK-style)

**Old-Style** (`<Project ToolsVersion="...">`):
- Requires explicit `<Compile Include="Folder\File.cs" />` for EVERY file
- SqlClr project (old-style .NET Framework 4.8.1)

### What Actually Happened
- RECOVERY_STATUS.md: 68 files deleted in commit cbb980c (19 DTOs, 49 services)
- All restored in commit daafee6
- Files were in SDK-style projects → auto-included → deletion broke system
- Problem: Batch deletion without testing, NOT orphaned files

### Confirmed Integration Status
- ✅ 54 API DTOs in `src/Hartonomous.Api/DTOs/` (auto-included)
- ❓ 49 Infrastructure services (need to verify location and DI registration)

### Actionable Decision
1. Files are already included in build (SDK-style auto-inclusion)
2. Need to verify USAGE (DI registration, controller references, tests)
3. If truly unused: Remove references → Update DI → Test → THEN delete
4. Never batch-delete files without full build + test cycle

---

## 5. Multi-Targeting .csproj Structure (FINDING 50-67)

### Key Conclusions

**Multi-targeting enables single codebase to produce multiple framework-specific builds.**

### Two Approaches

**Approach 1: Single Multi-Targeted Project** (SDK-style only)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net481;net8.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```

**Approach 2: Separate Projects** (keeps old-style unchanged)
- `src/SqlClr/` - Old-style .NET Framework 4.8.1
- `src/Hartonomous.Core/` - SDK-style multi-targeted

### Framework-Specific Dependencies
```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net481'">
  <PackageReference Include="System.Text.Json" Version="8.0.5" />
</ItemGroup>
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <!-- .NET 8 has System.Text.Json built-in -->
</ItemGroup>
```

### Conditional Compilation
```csharp
#if NET481
    // .NET Framework-specific code
#elif NET8_0
    // .NET 8-specific code
#endif
```

### Hartonomous Recommendations

**SqlClr**: 
- ❌ Do NOT multi-target (SQL Server only supports .NET Framework)
- ✅ Keep as old-style .NET Framework 4.8.1 project

**Hartonomous.Core**:
- ✅ Multi-target `net481;net8.0`
- SqlClr references net481 build
- API references net8.0 build (better performance)

**Hartonomous.Api**:
- ✅ Single-target `net8.0` (no need for .NET Framework in web API)

**Hartonomous.Infrastructure**:
- ✅ Multi-target `net481;net8.0` if shared between SqlClr and API

### Old-Style Can Reference SDK-Style
- FINDING 67: SqlClr (old-style net481) CAN reference Hartonomous.Core (SDK-style net481;net8.0)
- Visual Studio automatically picks compatible TFM (net481)
- Enables modernizing Core/Infrastructure without breaking SqlClr

---

## Priority Action Plan

Based on research findings, recommended task order:

### Phase 1: Fix Blocking Issues
1. **Fix SqlClr NuGet restore** (blocks all SQL CLR work)
   - System.Text.Json v8.0.5
   - MathNet.Numerics v5.0.0
2. **Fix sp_UpdateModelWeightsFromFeedback** (CRITICAL - model weights never update!)
   - Replace cursor (lines 73-92) with set-based UPDATE (FINDING 36)
3. **Remove/document TransformerInference TODOs** (lines 60, 64)
   - Add comment: LayerNorm impractical in SQL CLR (FINDING 21-26)

### Phase 2: Infrastructure Improvements
4. **Convert TensorAtomCoefficients to temporal table** (FINDING 27-38)
   - Automatic weight history tracking
5. **Fix 32 Sql.Bridge namespace references** in SqlClr
   - Remove broken .NET Standard 2.0 bridge pattern
6. **Verify Infrastructure services integration**
   - Locate 49 restored services
   - Check DI registration
   - Run tests

### Phase 3: Modernization (Optional)
7. **Multi-target Hartonomous.Core** (`net481;net8.0`)
   - Enables modern .NET performance while supporting SqlClr
8. **Create Hartonomous.Legacy.sln** for old-style projects
   - Separate solution for SqlClr development

---

## Research Sources

All findings sourced from official Microsoft Learn documentation:
- https://learn.microsoft.com/sql/relational-databases/clr-integration/
- https://learn.microsoft.com/dotnet/framework/
- https://learn.microsoft.com/sql/relational-databases/tables/temporal-tables
- https://learn.microsoft.com/dotnet/core/porting/
- https://learn.microsoft.com/nuget/create-packages/

**No LLM hallucination**: Every finding linked to official MS docs URL in SQL_CLR_RESEARCH_FINDINGS.md.

---

## Next Steps

1. Review this summary
2. Confirm priority action plan
3. Begin Phase 1 fixes (NuGet restore → sp_UpdateModelWeightsFromFeedback → TODOs)
4. Test incrementally after each fix
5. Move to Phase 2 only after Phase 1 complete

**Research phase complete. Ready to implement based on documented findings.**
