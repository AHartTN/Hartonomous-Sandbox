# Separation of Concerns Audit - SQL Files

**Generated**: 2025-11-12  
**Purpose**: Identify monolithic SQL files violating SOC principles and requiring breakdown

---

## Executive Summary

Analysis of 224 SQL files in `sql/` folder reveals **CRITICAL separation of concerns violations**. The sql/ folder contains deployment batch scripts with multiple GO statements, while SSDT projects require individual object files.

**Key Finding**: The sql/ folder is organized for **batch deployment** (SQLCMD pattern), but we need **SSDT project structure** (individual object files).

---

## Top 10 Worst Offenders (by GO statement count)

| Rank | File | GO Count | Lines | Objects | Violation Type |
|------|------|----------|-------|---------|----------------|
| **1** | `procedures/Common.ClrBindings.sql` | **156** | 814 | **120 functions** | Monolithic CLR bindings |
| **2** | `procedures/Functions.AggregateVectorOperations.sql` | **42** | 356 | **42 aggregates** | Monolithic aggregates |
| **3** | `procedures/Autonomy.FileSystemBindings.sql` | **18** | 102 | **9 functions** | Monolithic file I/O |
| **4** | `procedures/Analysis.WeightHistory.sql` | **17** | 272 | **~8 objects** | Views + procedures mixed |
| **5** | `Setup_Vector_Indexes.sql` | **17** | 338 | **~9 indexes** | Configuration script |
| **6** | `procedures/Common.CreateSpatialIndexes.sql` | **15** | 448 | **~8 indexes** | Configuration script |
| **7** | `procedures/Admin.WeightRollback.sql` | **14** | 326 | **~7 procedures** | Admin utilities mixed |
| **8** | `cdc/OptimizeCdcConfiguration.sql` | **14** | 113 | **~7 operations** | Configuration script |
| **9** | `procedures/Functions.VectorOperations.sql` | **12** | 90 | **6 functions** | Vector ops batch |
| **10** | `Predict_Integration.sql` | **12** | 340 | **~6 procedures** | ML integration batch |

---

## Violation Categories

### Category 1: Monolithic CLR Function Definitions
**Pattern**: Single file with 100+ CLR function CREATE statements  
**Impact**: CRITICAL - prevents modular deployment, violates DRY/SOLID

#### Files:
1. **Common.ClrBindings.sql** - 156 GO, 120 functions
   - Contains: Vector ops, image processing, audio, generation, semantic, file I/O, tensor ops
   - **Should be**: 120 individual files in `Procedures/dbo.clr_*.sql`
   - **Status**: ⏳ 24/120 files created

2. **Functions.AggregateVectorOperations.sql** - 42 GO, 42 aggregates
   - Contains: VectorMeanVariance, GeometricMedian, StreamingSoftmax, attention, compression, ML aggregates
   - **Should be**: 42 individual files in `Aggregates/dbo.*.sql`

3. **Autonomy.FileSystemBindings.sql** - 18 GO, 9 functions
   - Contains: WriteFileBytes, ReadFileBytes, ExecuteShellCommand, FileExists, etc.
   - **Should be**: 9 individual files in `Procedures/dbo.clr_*.sql`

4. **Functions.VectorOperations.sql** - 12 GO, 6 functions
   - Contains: Basic vector math operations
   - **Should be**: 6 individual files in `Procedures/dbo.*.sql`

**Total Impact**: ~177 individual function files needed

---

### Category 2: Table Group Files (Multiple Tables in One File)
**Pattern**: Single file creating 3-4 related tables  
**Impact**: HIGH - prevents independent table management

#### Files:
1. **tables/Attention.AttentionGenerationTables.sql** - 3 tables
   - `AttentionGenerationLog`
   - `AttentionInferenceResults`
   - `TransformerInferenceResults`
   - **Should be**: 3 individual files in `Tables/dbo.*.sql`

2. **tables/Stream.StreamOrchestrationTables.sql** - 4 tables
   - `StreamOrchestrationResults`
   - `StreamFusionResults`
   - `EventGenerationResults`
   - `EventAtoms`
   - **Should be**: 4 individual files in `Tables/dbo.*.sql`

3. **tables/Provenance.ProvenanceTrackingTables.sql** - 3 tables
   - `OperationProvenance`
   - `ProvenanceValidationResults`
   - `ProvenanceAuditResults`
   - **Should be**: 3 individual files in `Tables/dbo.*.sql`

4. **tables/Reasoning.ReasoningFrameworkTables.sql** - 3 tables
   - `ReasoningChains`
   - `SelfConsistencyResults`
   - `MultiPathReasoning`
   - **Should be**: 3 individual files in `Tables/dbo.*.sql`

**Total Impact**: ~16 individual table files needed

---

### Category 3: Mixed Object Type Files (Views + Procedures)
**Pattern**: Single file with views AND procedures  
**Impact**: HIGH - violates single responsibility

#### Files:
1. **procedures/Analysis.WeightHistory.sql** - 17 GO, ~8 objects
   - Contains: 2-3 views + 4-5 procedures
   - **Should be**: Separate into `Views/` and `Procedures/` folders

2. **procedures/Admin.WeightRollback.sql** - 14 GO, ~7 procedures
   - Admin utility procedures grouped together
   - **Should be**: 7 individual procedure files

3. **procedures/Common.Helpers.sql** - 10 GO, ~5 procedures
   - Helper/utility procedures grouped
   - **Should be**: 5 individual procedure files

**Total Impact**: ~20 individual files needed

---

### Category 4: Configuration/Setup Scripts
**Pattern**: Batch scripts with database configuration (indexes, settings)  
**Impact**: MEDIUM - these are deployment scripts, not database objects

#### Files:
1. **Setup_Vector_Indexes.sql** - 17 GO, creates ~9 DiskANN vector indexes
2. **procedures/Common.CreateSpatialIndexes.sql** - 15 GO, creates ~8 spatial indexes
3. **Setup_FILESTREAM.sql** - 7 GO, FILESTREAM configuration
4. **EnableQueryStore.sql** - Production Query Store config
5. **EnableAutomaticTuning.sql** - Automatic plan forcing
6. **Optimize_ColumnstoreCompression.sql** - 8 GO, columnstore strategy
7. **cdc/OptimizeCdcConfiguration.sql** - 14 GO, CDC settings

**Resolution**: These should move to `Scripts/Post-Deployment/` folder as **deployment scripts**, not individual object files. They're configuration, not database schema objects.

**Total Impact**: ~7 files move to post-deployment scripts

---

### Category 5: Large Multi-Procedure Files
**Pattern**: Files with 6-12 related procedures  
**Impact**: MEDIUM-HIGH - prevents modular changes

#### Files:
1. **procedures/Provenance.Neo4jSyncActivation.sql** - 10 GO
2. **procedures/Semantics.FeatureExtraction.sql** - 10 GO
3. **procedures/Inference.ServiceBrokerActivation.sql** - 10 GO
4. **procedures/Graph.AtomSurface.sql** - 10 GO
5. **procedures/Inference.AdvancedAnalytics.sql** - 8 GO
6. **procedures/Inference.VectorSearchSuite.sql** - 8 GO
7. **procedures/dbo.AgentFramework.sql** - 7 GO
8. **procedures/Spatial.LargeLineStringFunctions.sql** - 6 GO

**Total Impact**: ~60-70 individual procedure files needed

---

### Category 6: Table Files with Multiple Indexes/Constraints
**Pattern**: Table definition + 5-7 index CREATE statements  
**Impact**: LOW - acceptable in SSDT if all for same table

#### Files:
These are **ACCEPTABLE** - single table with multiple indexes is fine:
- `tables/dbo.AtomEmbeddings.sql` - 7 GO (1 table + 6 indexes)
- `tables/dbo.InferenceTracking.sql` - 8 GO (1 table + 7 indexes)
- `tables/dbo.TenantSecurityPolicy.sql` - 7 GO (1 table + 6 security policies)
- `tables/dbo.Weights.sql` - 6 GO (1 table + 5 indexes)

**No action needed** - these follow SSDT best practice (table + all related indexes in same file)

---

## SOC Violation Patterns Analysis

### Pattern 1: "Batch Deployment Mindset"
**Symptom**: Files with DROP IF EXISTS + GO + CREATE + GO repeated 10-50 times  
**Root Cause**: Files written for SQLCMD batch deployment, not SSDT  
**Example**: Common.ClrBindings.sql
```sql
IF OBJECT_ID('dbo.clr_Function1', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_Function1;
GO
CREATE FUNCTION dbo.clr_Function1(...) ...
GO
IF OBJECT_ID('dbo.clr_Function2', 'FN') IS NOT NULL DROP FUNCTION dbo.clr_Function2;
GO
CREATE FUNCTION dbo.clr_Function2(...) ...
GO
-- REPEATED 120 TIMES!
```

**SSDT Pattern** (each function in own file):
```sql
-- File: Procedures/dbo.clr_Function1.sql
CREATE FUNCTION dbo.clr_Function1(...) 
RETURNS ...
AS EXTERNAL NAME ...;
GO
```

---

### Pattern 2: "Functional Grouping Over Structural"
**Symptom**: Files grouped by business domain (Attention, Reasoning, Stream) containing multiple tables  
**Root Cause**: Logical grouping makes sense for understanding, wrong for deployment  
**Example**: Attention.AttentionGenerationTables.sql (3 tables)

**Problem**: 
- Can't deploy one table without others
- Can't track changes to individual tables
- Can't reference individual tables in dependencies

**SSDT Pattern**: Each table in own file, use folder structure for grouping
```
Tables/
  Attention/
    dbo.AttentionGenerationLog.sql
    dbo.AttentionInferenceResults.sql
    dbo.TransformerInferenceResults.sql
```

---

### Pattern 3: "Configuration Scripts as Schema"
**Symptom**: Index creation scripts, Query Store enablement, FILESTREAM setup  
**Root Cause**: Mixing database configuration with schema definition  
**Example**: Setup_Vector_Indexes.sql

**Problem**: These are **post-deployment operations**, not schema objects

**SSDT Pattern**: Move to Scripts/Post-Deployment/
```
Scripts/
  Post-Deployment/
    Setup_Vector_Indexes.sql
    EnableQueryStore.sql
    EnableAutomaticTuning.sql
```

---

## Breakdown Strategy

### Priority 1: Monolithic CLR Files (CRITICAL)
**Files**: 4 files → ~177 individual files  
**Reason**: Blocks entire CLR integration, prevents modular deployment  
**Order**:
1. ✅ Common.ClrBindings.sql (120 functions) - ⏳ 24/120 done
2. Functions.AggregateVectorOperations.sql (42 aggregates)
3. Autonomy.FileSystemBindings.sql (9 functions)
4. Functions.VectorOperations.sql (6 functions)

### Priority 2: Table Group Files (HIGH)
**Files**: 4 files → 16 individual files  
**Reason**: Prevents independent table management, schema change tracking  
**Order**:
1. Attention.AttentionGenerationTables.sql (3 tables)
2. Stream.StreamOrchestrationTables.sql (4 tables)
3. Provenance.ProvenanceTrackingTables.sql (3 tables)
4. Reasoning.ReasoningFrameworkTables.sql (3 tables)

### Priority 3: Configuration Scripts (MEDIUM)
**Files**: ~7 files → Scripts/Post-Deployment/  
**Reason**: Wrong location, should be post-deployment not schema  
**Order**:
1. Setup_Vector_Indexes.sql → Scripts/Post-Deployment/
2. EnableQueryStore.sql → Scripts/Post-Deployment/
3. EnableAutomaticTuning.sql → Scripts/Post-Deployment/
4. Setup_FILESTREAM.sql → Scripts/Post-Deployment/
5. Optimize_ColumnstoreCompression.sql → Scripts/Post-Deployment/
6. Common.CreateSpatialIndexes.sql → Scripts/Post-Deployment/
7. OptimizeCdcConfiguration.sql → Scripts/Post-Deployment/

### Priority 4: Large Multi-Procedure Files (MEDIUM)
**Files**: ~8 files → ~60-70 individual files  
**Reason**: Prevents modular changes, complicates dependency tracking  

### Priority 5: Mixed Object Files (LOW-MEDIUM)
**Files**: 3 files → ~20 individual files  
**Reason**: Views and procedures should be in separate folders  

---

## Impact Assessment

### Total File Creation Required
- **CLR Functions**: ~177 individual files
- **Tables**: ~16 individual files
- **Procedures**: ~80-90 individual files
- **Views**: ~10-15 individual files
- **Post-Deployment Scripts**: ~7 files (move, not create)

**Grand Total**: ~280-305 individual files to replace ~30 monolithic files

### Benefits
1. ✅ **SSDT Compliance**: Proper individual object files
2. ✅ **Source Control**: Granular change tracking per object
3. ✅ **Modular Deployment**: Deploy individual objects independently
4. ✅ **SOLID/DRY**: Each file has single responsibility
5. ✅ **Dependency Clarity**: sqlproj Build items show exact dependencies
6. ✅ **Build Performance**: Incremental builds only recompile changed objects
7. ✅ **Code Review**: Easier to review individual object changes
8. ✅ **Merge Conflicts**: Reduced - changes to different objects in different files

### Risks
1. ⚠️ **Time Investment**: ~280 files to create (but can be scripted)
2. ⚠️ **sqlproj Updates**: Must add ~280 Build items to .sqlproj
3. ⚠️ **Testing**: Must verify all objects deploy correctly
4. ⚠️ **Order Dependencies**: Some objects depend on others (aggregates, UDTs)

---

## Recommended Execution Plan

### Phase 1: CLR Functions (Week 1)
- Complete Common.ClrBindings.sql (96 files remaining)
- Break down Functions.AggregateVectorOperations.sql (42 files)
- Break down Autonomy.FileSystemBindings.sql (9 files)
- Break down Functions.VectorOperations.sql (6 files)
- **Total**: 153 files created
- **Update sqlproj**: Add all 153 Build items

### Phase 2: Table Groups (Week 1)
- Break down 4 table group files (16 tables)
- **Total**: 16 files created
- **Update sqlproj**: Add 16 Build items

### Phase 3: Configuration Scripts (Week 1)
- Move 7 configuration scripts to Scripts/Post-Deployment/
- Update sqlproj to reference post-deployment scripts
- **Total**: 7 files moved

### Phase 4: Large Procedure Files (Week 2)
- Break down 8 multi-procedure files (~60-70 procedures)
- **Total**: ~65 files created
- **Update sqlproj**: Add ~65 Build items

### Phase 5: Mixed Object Files (Week 2)
- Break down 3 mixed files into views + procedures
- **Total**: ~20 files created
- **Update sqlproj**: Add ~20 Build items

### Phase 6: Build & Test (Week 2)
- Build unified sqlproj
- Fix all compilation errors
- Verify deployment
- Delete sql/ folder

---

## Automation Opportunities

### Script 1: Extract CLR Functions
```powershell
# Parse Common.ClrBindings.sql
# Extract each CREATE FUNCTION block
# Generate individual .sql files
# Generate sqlproj Build items XML
```

### Script 2: Extract Tables from Group Files
```powershell
# Parse table group files
# Extract each CREATE TABLE block
# Generate individual .sql files
# Update sqlproj
```

### Script 3: Generate sqlproj ItemGroup
```powershell
# Scan Procedures/ folder
# Generate <Build Include="..."/> for each file
# Insert into .sqlproj
```

---

## Success Criteria

✅ **Zero files in sql/ with more than 2 GO statements** (1 for object, 1 for end)  
✅ **Each database object (table, procedure, function, view) in own file**  
✅ **Configuration scripts in Scripts/Post-Deployment/ folder**  
✅ **sqlproj builds successfully with all objects**  
✅ **No Link attributes in sqlproj (all files physically in project)**  
✅ **sql/ folder can be deleted without data loss**

---

## Next Steps

1. ✅ **COMPLETED**: Analyze all SQL files for SOC violations
2. ⏳ **IN PROGRESS**: Complete Common.ClrBindings.sql breakdown (96/120 remaining)
3. ❌ **TODO**: Break down Functions.AggregateVectorOperations.sql (42 aggregates)
4. ❌ **TODO**: Break down remaining Priority 1 files
5. ❌ **TODO**: Execute Phases 2-6 per execution plan

---

**Analysis Complete**: 2025-11-12  
**Files Analyzed**: 224  
**Violations Found**: ~30 monolithic files  
**Individual Files Required**: ~280-305  
**Current Progress**: 24/305 (7.9%)
