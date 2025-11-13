# Consolidated Execution Plan: DACPAC Consolidation

**Generated**: 2025-11-12  
**Goal**: Consolidate sql/ and src/SqlClr into unified src/Hartonomous.Database  
**Guiding Principles**: SOLID/DRY, separation of concerns, no stepping on our own toes

---

## Executive Summary

**Two Audits Completed**:
1. ✅ Separation of Concerns Audit - 30 monolithic files → 280+ individual files needed
2. ✅ Deduplication & Flow Audit - 10+ cursors, 20+ WHILE loops, duplicate logic identified

**The Smart Order**: Fix structure FIRST, then optimize flows AFTER
- Why? Can't optimize what isn't properly organized
- Structure fixes enable flow optimization
- Avoid refactoring code twice

---

## Phase Organization (4 Phases)

```
Phase 1: STRUCTURE (Separation of Concerns)
  ↓ Creates proper file organization
  
Phase 2: DEDUPLICATION (Remove duplicates)
  ↓ Eliminates redundant definitions
  
Phase 3: FLOW OPTIMIZATION (T-SQL/CLR boundaries)
  ↓ Fixes performance anti-patterns
  
Phase 4: BUILD & CLEANUP (Integration)
  ↓ Unify everything, delete old folders
```

---

## PHASE 1: STRUCTURE - Separation of Concerns (Week 1-2)

**Goal**: Break monolithic batch scripts into individual SSDT-compliant files  
**Why First**: Can't deduplicate or optimize until files are properly separated  
**Output**: ~280 individual .sql files with proper separation

### 1.1 CLR Function Definitions (Priority 1 - CRITICAL)

**Impact**: 78 CLR functions + 42 aggregates = 120 individual files  
**Why Critical**: Blocks entire CLR integration, prevents ANY other CLR work

#### Task 1.1.1: Complete Common.ClrBindings.sql Breakdown
- **Status**: ⏳ 24/78 complete (31%)
- **Remaining**: 54 files
- **Estimated Time**: 2 hours
- **Method**: Script extraction from lines 235-814

**Files to Create**:
```
Procedures/
  dbo.clr_DiscoverConcepts.sql
  dbo.clr_BindConcepts.sql
  dbo.clr_DeconstructImageToPatches.sql
  dbo.clr_GenerateCodeAstVector.sql
  dbo.clr_ParseModelLayer.sql
  dbo.clr_SvdDecompose.sql
  dbo.clr_ProjectToPoint.sql
  dbo.clr_CreateGeometryPointWithImportance.sql
  dbo.clr_ReconstructFromSVD.sql
  dbo.clr_StoreTensorAtomPayload.sql (PROCEDURE, not FUNCTION)
  dbo.clr_JsonFloatArrayToBytes.sql
  dbo.clr_GetTensorAtomPayload.sql
  dbo.clr_GenerateImageFromShapes.sql
  dbo.clr_GenerateAudioFromSpatialSignature.sql
  dbo.clr_SynthesizeModelLayer.sql
  dbo.clr_BytesToFloatArrayJson.sql
  dbo.clr_RunInference.sql
  dbo.fn_GenerateText.sql
  dbo.fn_GenerateImage.sql
  dbo.fn_GenerateAudio.sql
  dbo.fn_GenerateVideo.sql
  dbo.fn_GenerateEnsemble.sql
  dbo.fn_GenerateWithAttention.sql
  dbo.fn_ComputeEmbedding.sql
  dbo.fn_CompareAtoms.sql
  dbo.fn_MergeAtoms.sql
  dbo.fn_clr_AnalyzeSystemState.sql
  dbo.fn_CalculateComplexity.sql
  dbo.fn_DetermineSla.sql
  dbo.fn_EstimateResponseTime.sql
  dbo.fn_ParseModelCapabilities.sql
  dbo.fn_ProjectTo3D.sql
  dbo.clr_EnumerateSegments.sql (TVF)
  dbo.fn_GetComponentCount.sql
  dbo.fn_DecompressComponents.sql
  dbo.fn_GetTimeWindow.sql
  dbo.clr_ParseGGUFTensorCatalog.sql
  dbo.clr_ReadFilestreamChunk.sql
  dbo.clr_CreateMultiLineStringFromWeights.sql
  dbo.clr_FindPrimes.sql
  dbo.clr_ComputeSemanticFeatures.sql
  dbo.clr_BuildPerformanceVector.sql
  dbo.clr_ComputeZScore.sql
  dbo.clr_IsOutlierIQR.sql
  dbo.clr_ExecuteModelInference.sql
  ... (remaining CLR functions)
```

**Automation Approach**:
```powershell
# PowerShell script to extract functions
$content = Get-Content "d:\Repositories\Hartonomous\sql\procedures\Common.ClrBindings.sql" -Raw
$pattern = '(?s)IF OBJECT_ID.*?GO\s*CREATE (FUNCTION|PROCEDURE|AGGREGATE) (dbo\.\w+).*?GO'
$matches = [regex]::Matches($content, $pattern)

foreach ($match in $matches[24..77]) {  # Skip first 24 (already done)
    $name = $match.Groups[2].Value -replace 'dbo\.', ''
    $code = $match.Value -replace '^IF OBJECT_ID.*?GO\s*', ''
    Set-Content "src/Hartonomous.Database/Procedures/$name.sql" $code
}
```

#### Task 1.1.2: Break Down Functions.AggregateVectorOperations.sql
- **Count**: 42 aggregates
- **Estimated Time**: 1.5 hours
- **Method**: Similar extraction script

**Files to Create**: `Aggregates/dbo.*.sql` (42 files)
- VectorMeanVariance.sql
- GeometricMedian.sql
- StreamingSoftmax.sql
- VectorAttentionAggregate.sql
- AutoencoderCompression.sql
- ... (38 more aggregates)

#### Task 1.1.3: Identify and Remove Duplicates (CRITICAL!)
- **Files with Duplicates**:
  - Common.ClrBindings.sql (Lines 559-625): File I/O functions
  - Autonomy.FileSystemBindings.sql (Lines 12-102): SAME 9 functions
  
**Action**: 
1. Create individual files from Common.ClrBindings.sql
2. **DELETE** Autonomy.FileSystemBindings.sql entirely (100% duplicate)
3. Verify no other references to deleted file

**Duplicates to Create ONCE**:
```
Procedures/
  dbo.clr_FileExists.sql
  dbo.clr_DirectoryExists.sql
  dbo.clr_DeleteFile.sql
  dbo.clr_ReadFileBytes.sql
  dbo.clr_ReadFileText.sql
  dbo.clr_WriteFileBytes.sql
  dbo.clr_WriteFileText.sql
  dbo.clr_ExecuteShellCommand.sql (TVF)
```

**Deduplication Rule**: First occurrence wins, all duplicates deleted

---

### 1.2 Table Group Files (Priority 2 - HIGH)

**Impact**: 4 files → 16 individual table files  
**Why High**: Prevents independent table management

#### Task 1.2.1: Break Down Attention Tables
**File**: `sql/tables/Attention.AttentionGenerationTables.sql`

**Create**:
```
Tables/
  dbo.AttentionGenerationLog.sql
  dbo.AttentionInferenceResults.sql
  dbo.TransformerInferenceResults.sql
```

#### Task 1.2.2: Break Down Stream Tables
**File**: `sql/tables/Stream.StreamOrchestrationTables.sql`

**Create**:
```
Tables/
  dbo.StreamOrchestrationResults.sql
  dbo.StreamFusionResults.sql
  dbo.EventGenerationResults.sql
  dbo.EventAtoms.sql
```

#### Task 1.2.3: Break Down Provenance Tables
**File**: `sql/tables/Provenance.ProvenanceTrackingTables.sql`

**Create**:
```
Tables/
  dbo.OperationProvenance.sql
  dbo.ProvenanceValidationResults.sql
  dbo.ProvenanceAuditResults.sql
```

#### Task 1.2.4: Break Down Reasoning Tables
**File**: `sql/tables/Reasoning.ReasoningFrameworkTables.sql`

**Create**:
```
Tables/
  dbo.ReasoningChains.sql
  dbo.SelfConsistencyResults.sql
  dbo.MultiPathReasoning.sql
```

**Estimated Time**: 1 hour (simple extraction, no logic changes)

---

### 1.3 Configuration Scripts (Priority 3 - MEDIUM)

**Impact**: Move 7 files to Scripts/Post-Deployment/  
**Why Medium**: Wrong location but not blocking other work

#### Task 1.3.1: Create Post-Deployment Folder Structure
```
src/Hartonomous.Database/
  Scripts/
    Post-Deployment/
      01_EnableQueryStore.sql
      02_EnableAutomaticTuning.sql
      03_Setup_FILESTREAM.sql
      04_Setup_Vector_Indexes.sql
      05_Optimize_ColumnstoreCompression.sql
      06_CreateSpatialIndexes.sql
      07_OptimizeCdcConfiguration.sql
```

#### Task 1.3.2: Move Files (Don't Break Down)
**Source → Destination**:
- `sql/EnableQueryStore.sql` → `Scripts/Post-Deployment/01_EnableQueryStore.sql`
- `sql/EnableAutomaticTuning.sql` → `Scripts/Post-Deployment/02_EnableAutomaticTuning.sql`
- `sql/Setup_FILESTREAM.sql` → `Scripts/Post-Deployment/03_Setup_FILESTREAM.sql`
- `sql/Setup_Vector_Indexes.sql` → `Scripts/Post-Deployment/04_Setup_Vector_Indexes.sql`
- `sql/Optimize_ColumnstoreCompression.sql` → `Scripts/Post-Deployment/05_Optimize_ColumnstoreCompression.sql`
- `sql/procedures/Common.CreateSpatialIndexes.sql` → `Scripts/Post-Deployment/06_CreateSpatialIndexes.sql`
- `sql/cdc/OptimizeCdcConfiguration.sql` → `Scripts/Post-Deployment/07_OptimizeCdcConfiguration.sql`

**Note**: Keep GO statements in post-deployment scripts (they're batch scripts, not schema objects)

**Estimated Time**: 30 minutes

---

### 1.4 Large Multi-Procedure Files (Priority 4 - MEDIUM)

**Impact**: 8 files → ~60-70 individual procedure files  
**Strategy**: Break down highest-impact files first

#### Files to Break Down:
1. `Analysis.WeightHistory.sql` (17 GO, ~8 objects: views + procedures)
2. `Provenance.Neo4jSyncActivation.sql` (10 GO, procedures)
3. `Semantics.FeatureExtraction.sql` (10 GO, procedures)
4. `Inference.ServiceBrokerActivation.sql` (10 GO, procedures)
5. `Common.Helpers.sql` (10 GO, helper procedures)
6. `Graph.AtomSurface.sql` (10 GO, graph procedures)
7. `Inference.AdvancedAnalytics.sql` (8 GO, analytics procedures)
8. `Inference.VectorSearchSuite.sql` (8 GO, search procedures)

**Method**: 
- Identify each CREATE PROCEDURE/FUNCTION/VIEW
- Extract to individual file
- Name: `Procedures/Schema.ObjectName.sql` or `Views/Schema.ObjectName.sql`

**Estimated Time**: 4 hours

---

### 1.5 Update sqlproj with All New Files

**Critical**: After creating files, must add to .sqlproj

```xml
<ItemGroup>
  <!-- CLR Functions (78 files) -->
  <Build Include="Procedures\dbo.clr_VectorDotProduct.sql" />
  <Build Include="Procedures\dbo.clr_VectorCosineSimilarity.sql" />
  <!-- ... all 78 CLR function files ... -->
  
  <!-- Aggregates (42 files) -->
  <Build Include="Aggregates\dbo.VectorMeanVariance.sql" />
  <Build Include="Aggregates\dbo.GeometricMedian.sql" />
  <!-- ... all 42 aggregate files ... -->
  
  <!-- Tables (16 files) -->
  <Build Include="Tables\dbo.AttentionGenerationLog.sql" />
  <!-- ... all 16 table files ... -->
  
  <!-- Procedures (60-70 files) -->
  <Build Include="Procedures\Analysis.vw_CurrentWeights.sql" />
  <!-- ... all procedure files ... -->
</ItemGroup>

<ItemGroup>
  <!-- Post-Deployment Scripts -->
  <PostDeploy Include="Scripts\Post-Deployment\01_EnableQueryStore.sql" />
  <PostDeploy Include="Scripts\Post-Deployment\02_EnableAutomaticTuning.sql" />
  <!-- ... all 7 post-deploy scripts ... -->
</ItemGroup>
```

**Method**: Generate XML snippet with PowerShell, insert into .sqlproj

**Estimated Time**: 1 hour

---

## PHASE 1 CHECKPOINT

**Deliverables**:
- ✅ 78 individual CLR function files created
- ✅ 42 individual aggregate files created
- ✅ 16 individual table files created
- ✅ 60-70 individual procedure/view files created
- ✅ 7 post-deployment scripts moved
- ✅ sqlproj updated with all Build items
- ✅ Zero monolithic files remaining (except acceptable patterns)
- ✅ Zero duplicate definitions

**Test**: Build sqlproj (expect errors - that's Phase 4)

**Duration**: 2-3 days

---

## PHASE 2: DEDUPLICATION (Week 2)

**Goal**: Eliminate ALL duplicate logic now that files are separated  
**Why Second**: Can't find/fix duplicates until structure is clean

### 2.1 Verify No Duplicate CLR Bindings

**Action**: Scan all `Procedures/dbo.clr_*.sql` files
- Ensure each CLR function defined EXACTLY once
- Check for duplicate EXTERNAL NAME bindings

**Method**:
```powershell
# Find duplicate function names
$functions = Get-ChildItem "src/Hartonomous.Database/Procedures/dbo.clr_*.sql"
$names = $functions | ForEach-Object { 
    (Get-Content $_.FullName | Select-String "CREATE FUNCTION (dbo\.\w+)").Matches.Groups[1].Value 
}
$duplicates = $names | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) { Write-Error "Duplicate functions found: $($duplicates.Name)" }
```

**Expected**: Zero duplicates (already eliminated Autonomy.FileSystemBindings.sql)

**Estimated Time**: 30 minutes

---

### 2.2 Identify T-SQL Function Duplicates

**Check For**:
- Similar logic in multiple procedures
- Helper functions defined in multiple files
- Geometry/math calculations repeated

**Example Found**: `fn_ComputeGeometryRms` defined in `sp_AtomizeAudio.sql`
- Check if similar RMS calculations exist elsewhere
- Check if belongs in separate helper file

**Action**: Create inventory of ALL T-SQL functions
```sql
SELECT OBJECT_SCHEMA_NAME(object_id) + '.' + name AS FunctionName,
       OBJECT_DEFINITION(object_id) AS Definition
FROM sys.objects
WHERE type IN ('FN', 'TF', 'IF')
ORDER BY name;
```

**Compare**: Hash or fuzzy match definitions to find duplicates

**Estimated Time**: 2 hours

---

### 2.3 Eliminate Common Code Patterns

**Identified Patterns**:
1. Error handling boilerplate (40+ procedures) - **KEEP** (standard pattern OK)
2. Tenant filtering (20+ procedures) - **KEEP** (standard pattern OK)
3. Query Store analysis queries (3+ locations) - **CREATE VIEW**

**Action**: Create `Views/dbo.vw_QueryStoreSlowQueries.sql`
```sql
CREATE VIEW dbo.vw_QueryStoreSlowQueries
AS
SELECT 
    q.query_id,
    qt.query_sql_text,
    rs.avg_duration / 1000.0 AS avg_duration_ms,
    rs.count_executions,
    rs.avg_duration * rs.count_executions AS total_impact_ms,
    rs.last_execution_time
FROM sys.query_store_query q
INNER JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
INNER JOIN sys.query_store_plan p ON q.query_id = p.query_id
INNER JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
WHERE rs.last_execution_time >= DATEADD(hour, -24, SYSUTCDATETIME());
```

**Update Procedures**: Replace inline query with `FROM vw_QueryStoreSlowQueries`

**Estimated Time**: 2 hours

---

### 2.4 Compare sql/ vs sqlproj Files

**Goal**: Ensure no missing content after consolidation

**Method**: For each file in sql/
1. Find corresponding file in src/Hartonomous.Database/
2. Compare content (exact match, sqlproj newer, sql/ newer)
3. Merge any unique content from sql/ into sqlproj

**Already Identified**:
- `sql/types/provenance.AtomicStream.sql` - has schema checks (sqlproj missing)
- `sql/types/provenance.ComponentStream.sql` - has schema checks (sqlproj missing)

**Action**: 
1. Update `Types/provenance.AtomicStream.sql` with schema checks from sql/
2. Update `Types/provenance.ComponentStream.sql` with schema checks from sql/

**Comprehensive Scan**:
```powershell
$sqlFiles = Get-ChildItem "d:\Repositories\Hartonomous\sql" -Recurse -Filter "*.sql"
$comparison = @()

foreach ($sqlFile in $sqlFiles) {
    $relativePath = $sqlFile.FullName -replace '.*\\sql\\', ''
    $sqlprojPath = "d:\Repositories\Hartonomous\src\Hartonomous.Database\$relativePath"
    
    if (Test-Path $sqlprojPath) {
        $sqlContent = Get-Content $sqlFile.FullName -Raw
        $sqlprojContent = Get-Content $sqlprojPath -Raw
        
        if ($sqlContent -ne $sqlprojContent) {
            $comparison += [PSCustomObject]@{
                File = $relativePath
                Status = "DIFFERENT"
            }
        }
    } else {
        $comparison += [PSCustomObject]@{
            File = $relativePath
            Status = "MISSING_IN_SQLPROJ"
        }
    }
}

$comparison | Format-Table -AutoSize
```

**Estimated Time**: 4 hours

---

## PHASE 2 CHECKPOINT

**Deliverables**:
- ✅ Zero duplicate CLR function definitions
- ✅ Zero duplicate T-SQL function definitions
- ✅ Common query patterns extracted to views
- ✅ All unique content from sql/ merged into sqlproj
- ✅ Comparison report generated

**Duration**: 2 days

---

## PHASE 3: FLOW OPTIMIZATION (Week 3)

**Goal**: Fix T-SQL/CLR boundary issues for performance  
**Why Third**: Can't optimize until structure is clean and deduplicated

### 3.1 Critical Cursor Elimination

**Target**: 10 procedures with cursors

#### 3.1.1: Fix dbo.sp_OptimizeEmbeddings (ModelManagement.sql)

**Current** (Lines 175-210):
```sql
DECLARE atom_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT AtomId, Content FROM @AtomsToProcess;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @NewEmbedding = dbo.fn_ComputeEmbedding(@CurrentAtomId, @ModelId, @TenantId);
    MERGE dbo.AtomEmbeddings ...
END
```

**Optimized**:
```sql
-- Batch prepare
INSERT INTO #BatchInput (AtomId, Content, ModelType, ApiEndpoint)
SELECT 
    a.AtomId,
    a.CanonicalText,
    m.ModelType,
    JSON_VALUE(m.Config, '$.apiEndpoint')
FROM @AtomsToProcess a
CROSS JOIN dbo.Models m
WHERE m.ModelId = @ModelId;

-- Batch process (new CLR TVF needed - see 3.2)
INSERT INTO dbo.AtomEmbeddings (AtomId, ModelId, EmbeddingVector)
SELECT AtomId, @ModelId, Embedding
FROM dbo.clr_BatchComputeEmbeddings(
    (SELECT * FROM #BatchInput FOR JSON PATH)
);
```

**Files Modified**:
- `Procedures/dbo.sp_OptimizeEmbeddings.sql`

**Dependencies**: Requires 3.2 (batch embedding CLR)

#### 3.1.2-3.1.10: Fix Remaining Cursors

**Files to Fix**:
1. `Procedures/dbo.sp_AtomizeModel.sql`
2. `Procedures/Semantics.sp_UpdateEmbeddingFeatures.sql` (or similar)
3. `Procedures/Stream.sp_GenerateEventsFromStream.sql`
4. `Procedures/Operations.sp_RebuildFragmentedIndexes.sql`
5. `Procedures/dbo.sp_Learn.sql` (autonomous improvement deployment)
6. `Procedures/dbo.sp_DiscoverAndBindConcepts.sql`
7-10: Others identified in audit

**Approach**: 
- Set-based where possible
- CLR TVF for complex iterations
- Batch operations

**Estimated Time**: 8 hours (1 hour per cursor)

---

### 3.2 Critical CLR Refactoring

#### 3.2.1: Refactor fn_ComputeEmbedding

**Problem**: CLR function makes database queries (CRITICAL anti-pattern)

**Current CLR** (EmbeddingFunctions.cs):
```csharp
public static SqlBytes fn_ComputeEmbedding(SqlInt64 atomId, SqlInt32 modelId, SqlInt32 tenantId)
{
    using (var conn = new System.Data.SqlClient.SqlConnection("context connection=true"))
    {
        // DATABASE QUERIES INSIDE CLR - WRONG!
        var contentQuery = @"SELECT TOP 1 CanonicalText FROM dbo.Atoms...";
        // ...
    }
}
```

**New CLR** (pure computation):
```csharp
[SqlFunction(IsDeterministic = false, IsPrecise = false)]
public static SqlBytes ComputeEmbeddingFromText(
    SqlString content,
    SqlString modelType,
    SqlString apiEndpoint)
{
    // NO database access - pure computation
    if (content.IsNull || modelType.IsNull)
        return SqlBytes.Null;
    
    if (modelType.Value == "OpenAI")
        return new SqlBytes(CallOpenAIEmbedding(content.Value, apiEndpoint.Value));
    else if (modelType.Value == "Local")
        return new SqlBytes(GenerateLocalEmbedding(content.Value));
    else
        throw new InvalidOperationException($"Unsupported model type: {modelType.Value}");
}
```

**New CLR TVF** (batch processing):
```csharp
[SqlFunction(
    FillRowMethodName = "FillEmbeddingRow",
    TableDefinition = "AtomId BIGINT, Embedding VARBINARY(MAX)",
    IsDeterministic = false
)]
public static IEnumerable BatchComputeEmbeddings(SqlString batchJson)
{
    var batch = JsonConvert.DeserializeObject<List<BatchInput>>(batchJson.Value);
    
    // Batch API call (OpenAI supports batching)
    var embeddings = CallOpenAIBatchEmbedding(batch);
    
    foreach (var result in embeddings)
        yield return result;
}

public static void FillEmbeddingRow(object obj, out SqlInt64 atomId, out SqlBytes embedding)
{
    var result = (EmbeddingResult)obj;
    atomId = result.AtomId;
    embedding = new SqlBytes(result.Embedding);
}
```

**Files Modified**:
- `src/SqlClr/EmbeddingFunctions.cs`
- `Procedures/dbo.fn_ComputeEmbedding.sql` (update binding)
- `Procedures/dbo.clr_BatchComputeEmbeddings.sql` (new TVF)

**Estimated Time**: 4 hours

---

#### 3.2.2: Create Audio Frame Batch CLR

**New CLR TVF**:
```csharp
[SqlFunction(
    FillRowMethodName = "FillAudioFrameRow",
    TableDefinition = @"
        FrameIndex INT,
        StartTimeSec FLOAT,
        EndTimeSec FLOAT,
        WaveformGeometry GEOMETRY,
        RmsAmplitude FLOAT,
        PeakAmplitude FLOAT"
)]
public static IEnumerable GenerateAudioFrames(
    SqlBytes audioContent,
    SqlInt32 channels,
    SqlInt32 sampleRate,
    SqlInt32 frameWindowMs,
    SqlInt32 overlapMs)
{
    // Process entire audio in one pass
    // Generate all frames at once
    // Yield each frame
}
```

**Update Procedure**:
```sql
-- Replace WHILE loop with single TVF call
INSERT INTO dbo.AudioFrames (ParentAtomId, FrameIndex, ...)
SELECT @AtomId, FrameIndex, StartTimeSec, ...
FROM dbo.clr_GenerateAudioFrames(
    @Content, 
    @ChannelCount, 
    @SampleRate, 
    @FrameWindowMs, 
    @OverlapMs
);
```

**Files Modified**:
- `src/SqlClr/AudioProcessing.cs` (add new method)
- `Procedures/dbo.clr_GenerateAudioFrames.sql` (new TVF binding)
- `Procedures/dbo.sp_AtomizeAudio.sql` (remove WHILE loop)

**Estimated Time**: 6 hours

---

### 3.3 WHILE Loop Elimination

**Target**: 20+ WHILE loops

**Categories**:
1. **Data Processing Loops** (10-12 loops) - **ELIMINATE** → CLR or set-based
2. **Orchestration Loops** (5-7 loops) - **KEEP** (AGI loop, Service Broker, etc.)
3. **Generation Loops** (3-5 loops) - **REFACTOR** → CLR implements algorithm

**High Priority WHILE Loops to Fix**:
1. `Attention.sp_GenerateWithReasoning.sql` - Multi-step reasoning
2. `Reasoning.sp_ChainOfThought.sql` - Sequential generation
3. `Reasoning.sp_TreeOfThought.sql` - **NESTED LOOPS** (critical)
4. `dbo.sp_AtomizeAudio.sql` - Frame processing (already covered in 3.2.2)

**Approach**: Move iterative algorithms into CLR, T-SQL just calls once

**Estimated Time**: 12 hours

---

### 3.4 Create Helper CLR Functions

**New CLR Functions Needed**:
1. `clr_ComputeGeometryRms.sql` - RMS from GEOMETRY
2. `clr_ParseJsonArray.sql` - TVF to parse JSON arrays
3. `clr_TreeOfThoughtSearch.sql` - Complete tree search algorithm

**Files to Create**:
- `src/SqlClr/GeometryHelpers.cs`
- `src/SqlClr/JsonHelpers.cs`
- `src/SqlClr/ReasoningAlgorithms.cs`
- Corresponding SQL bindings

**Estimated Time**: 8 hours

---

## PHASE 3 CHECKPOINT

**Deliverables**:
- ✅ Zero cursors in data processing code
- ✅ Zero WHILE loops for row iteration
- ✅ Batch CLR functions for high-volume operations
- ✅ fn_ComputeEmbedding refactored (no DB queries in CLR)
- ✅ Audio processing optimized (10-100x faster)
- ✅ Embedding computation optimized (50-500x faster)

**Test**: Run performance benchmarks
- Embedding 1000 atoms: <20 seconds (vs 500 before)
- Audio 1000 frames: <10 seconds (vs 300 before)

**Duration**: 5-7 days

---

## PHASE 4: BUILD & CLEANUP (Week 4)

**Goal**: Integrate everything, verify build, delete old folders

### 4.1 Copy CLR C# Files Physically

**Current**: sqlproj uses `<Compile Include="..\SqlClr\*.cs" Link="CLR\*.cs" />`  
**Target**: Physical files in `src/Hartonomous.Database/CLR/`

**Action**:
```powershell
# Copy all C# files
$sourceDir = "d:\Repositories\Hartonomous\src\SqlClr"
$targetDir = "d:\Repositories\Hartonomous\src\Hartonomous.Database\CLR"

Get-ChildItem $sourceDir -Recurse -Filter "*.cs" | ForEach-Object {
    $relativePath = $_.FullName -replace [regex]::Escape($sourceDir), ''
    $targetPath = Join-Path $targetDir $relativePath
    $targetFolder = Split-Path $targetPath -Parent
    
    if (!(Test-Path $targetFolder)) {
        New-Item -ItemType Directory -Path $targetFolder -Force
    }
    
    Copy-Item $_.FullName $targetPath -Force
}
```

**Verify**: 65 C# files copied

**Estimated Time**: 1 hour

---

### 4.2 Update sqlproj - Remove Link Attributes

**Find/Replace in .sqlproj**:
```xml
<!-- BEFORE -->
<Compile Include="..\SqlClr\VectorOperations.cs" Link="CLR\VectorOperations.cs" />

<!-- AFTER -->
<Compile Include="CLR\VectorOperations.cs" />
```

**Method**: PowerShell regex replacement across entire `<ItemGroup>` section

**Estimated Time**: 30 minutes

---

### 4.3 Add Missing NuGet Packages

**Current sqlproj Packages**: (check .sqlproj)  
**Required by CLR**: 
- System.Drawing.Common (image processing)
- Microsoft.SqlServer.Types (spatial/geometry)
- MathNet.Numerics (vector math)
- Newtonsoft.Json (JSON serialization)
- ILGPU (GPU acceleration)

**Action**: Add to .sqlproj
```xml
<ItemGroup>
  <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
  <PackageReference Include="Microsoft.SqlServer.Types" Version="160.1000.6" />
  <PackageReference Include="MathNet.Numerics" Version="5.0.0" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <PackageReference Include="ILGPU" Version="1.5.1" />
  <PackageReference Include="ILGPU.Algorithms" Version="1.5.1" />
</ItemGroup>
```

**Estimated Time**: 30 minutes

---

### 4.4 Build and Fix Errors

**Approach**: Iterative build → fix → build

**Expected Error Categories**:
1. Missing dependencies (CLR references)
2. Incorrect EXTERNAL NAME bindings
3. Type mismatches
4. Missing schema objects
5. Circular dependencies

**Method**:
```powershell
cd "d:\Repositories\Hartonomous\src\Hartonomous.Database"
dotnet clean
dotnet build -c Release /p:RunSqlCodeAnalysis=false > build.log 2>&1
cat build.log | Select-String "error"
```

**Fix each error systematically**:
- Document error
- Identify root cause
- Fix
- Rebuild
- Repeat until clean build

**Estimated Time**: 2-3 days (expect 50-200 errors initially)

---

### 4.5 Verify Deployment

**Test Deployment** (don't delete anything yet):
```powershell
# Generate DACPAC
dotnet build -c Release

# Deploy to test database
SqlPackage.exe /Action:Publish `
  /SourceFile:"bin\Release\Hartonomous.Database.dacpac" `
  /TargetConnectionString:"Server=localhost;Database=Hartonomous_Test;Integrated Security=True" `
  /p:DropObjectsNotInSource=False
```

**Verify**:
- All tables exist
- All procedures exist
- All CLR functions exist
- All indexes exist
- Sample queries work
- Performance acceptable

**Estimated Time**: 1 day

---

### 4.6 Delete Old Folders (FINAL STEP)

**Only after successful build + deployment test**:

```powershell
# Backup first!
Compress-Archive -Path "d:\Repositories\Hartonomous\sql" `
  -DestinationPath "d:\Repositories\Hartonomous\sql_BACKUP_$(Get-Date -Format 'yyyyMMdd').zip"

Compress-Archive -Path "d:\Repositories\Hartonomous\src\SqlClr" `
  -DestinationPath "d:\Repositories\Hartonomous\SqlClr_BACKUP_$(Get-Date -Format 'yyyyMMdd').zip"

# Delete
Remove-Item "d:\Repositories\Hartonomous\sql" -Recurse -Force
Remove-Item "d:\Repositories\Hartonomous\src\SqlClr" -Recurse -Force
```

**Update Solution File**:
```
Remove:
  Project("{...}") = "SqlClr", "src\SqlClr\SqlClrFunctions.csproj", "{...}"
  
Keep:
  Project("{...}") = "Hartonomous.Database", "src\Hartonomous.Database\Hartonomous.Database.sqlproj", "{...}"
```

**Estimated Time**: 1 hour

---

## PHASE 4 CHECKPOINT

**Deliverables**:
- ✅ All CLR C# files physically in Hartonomous.Database/CLR/
- ✅ Zero Link attributes in sqlproj
- ✅ All required NuGet packages added
- ✅ Clean build (zero errors)
- ✅ Successful deployment to test database
- ✅ sql/ folder deleted (backed up)
- ✅ src/SqlClr folder deleted (backed up)
- ✅ Solution file updated

**Final Verification**:
```powershell
# Count objects
dotnet build
SqlPackage.exe /Action:Script `
  /SourceFile:"bin\Release\Hartonomous.Database.dacpac" `
  /OutputPath:"deployment_script.sql"

# Verify counts
Get-Content deployment_script.sql | Select-String "CREATE (TABLE|PROCEDURE|FUNCTION|VIEW|AGGREGATE)" | Measure-Object
```

**Duration**: 5-7 days

---

## Summary Timeline

| Phase | Duration | Output |
|-------|----------|--------|
| Phase 1: Structure | 2-3 days | 280+ individual files, proper separation |
| Phase 2: Deduplication | 2 days | Zero duplicates, content merged |
| Phase 3: Flow Optimization | 5-7 days | Optimized T-SQL/CLR boundaries |
| Phase 4: Build & Cleanup | 5-7 days | Unified DACPAC, old folders deleted |
| **TOTAL** | **14-19 days** | **Complete consolidation** |

---

## Success Criteria

### Structure
- [ ] Zero monolithic files (except post-deployment scripts)
- [ ] Each database object in own file
- [ ] Proper folder organization (Tables/, Procedures/, Views/, etc.)
- [ ] Post-deployment scripts in Scripts/Post-Deployment/

### Deduplication
- [ ] Zero duplicate CLR function definitions
- [ ] Zero duplicate T-SQL function definitions
- [ ] Common patterns extracted to views/helpers

### Flow Optimization
- [ ] Zero cursors for data processing
- [ ] Zero WHILE loops for row iteration
- [ ] Zero database queries in CLR functions
- [ ] Batch processing for high-volume operations
- [ ] Performance benchmarks met (10-500x improvements)

### Build & Integration
- [ ] Clean build (zero errors)
- [ ] Successful deployment
- [ ] All tests pass
- [ ] sql/ folder deleted
- [ ] src/SqlClr folder deleted
- [ ] Single unified project: Hartonomous.Database

---

## Next Steps

**Start**: Phase 1, Task 1.1.1 - Complete Common.ClrBindings.sql breakdown (54 files remaining)

**Command**:
```powershell
# Ready to begin?
Write-Output "Phase 1.1.1: Breaking down Common.ClrBindings.sql"
Write-Output "Creating 54 remaining CLR function files..."
```

Would you like me to begin executing Phase 1.1.1?
