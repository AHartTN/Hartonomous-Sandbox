# DACPAC Migration Audit Report
**Date:** 2025-01-20  
**Auditor:** GitHub Copilot  
**Status:** 🔴 CRITICAL - Build Failing  

## Executive Summary

**DACPAC build is completely broken** with **1,576 build errors** and **1,292 warnings**. The migration from EF Core migrations to DACPAC source of truth is incomplete and requires significant repair work before database deployment can succeed.

### Key Findings

| Metric | Expected | Found | Status |
|--------|----------|-------|--------|
| **DACPAC Build Status** | Success | **1,576 errors** | 🔴 FAILED |
| **EF Core Configurations** | 82 files | **5 files** | 🔴 77 MISSING |
| **Table Count (DACPAC)** | ~60 | 60 files | ✅ COMPLETE |
| **Index Files** | ~200 files | **0 files** | 🔴 NOT MIGRATED |
| **Billing Tables** | 1 version | **3 versions** | ⚠️ DUPLICATES |
| **Models Table** | Exists | **MISSING** | 🔴 CRITICAL |

### Critical Blockers

1. **Missing `Models` table** - 100+ procedures reference this table, causing cascading failures
2. **Duplicate parameter names** - 300+ SQL71508 errors from duplicate parameter/column declarations
3. **Missing indexes** - ~200 index definitions not yet created in DACPAC
4. **Unresolved CLR references** - 200+ SQL71501 errors for missing CLR assemblies
5. **Missing tables** - TenantAtoms, Neo4jSyncQueue, Neo4jSyncLog, EventAtoms, AttentionGenerationLog, etc.

---

## Detailed Error Analysis

### Error Category Breakdown

| Error Code | Count | Description | Severity |
|------------|-------|-------------|----------|
| **SQL71508** | ~300 | Duplicate element names (parameters, columns) | 🔴 HIGH |
| **SQL71501** | ~200 | Unresolved references to tables/assemblies | 🔴 HIGH |
| **SQL71502** | ~800 | Unresolved references to columns/objects | 🟡 MEDIUM |
| **SQL71509** | ~50 | Duplicate table variable column names | 🟡 MEDIUM |
| **Warnings** | 1,292 | Missing tables, ambiguous references | 🟡 MEDIUM |

### Critical Error #1: Missing `Models` Table

**Impact:** Cascading failures across 100+ database objects

**Evidence:**
```
D:\Repositories\Hartonomous\src\Hartonomous.Database\Tables\provenance.Concepts.sql(16,96,16,96): 
Build error SQL71501: Foreign Key: [provenance].[FK_Concepts_Models_ModelId] has an unresolved 
reference to Column [dbo].[Models].[ModelId].
```

**Affected Objects:**
- `provenance.Concepts` (FK constraint fails)
- `sp_GenerateAudio` (references `fn_SelectModelsForTask.ModelId`)
- `fn_SelectModelsForTask` (queries `Models` table extensively)
- `sp_TextToEmbedding` (references `Models.IsActive`, `Models.ModelName`, `Models.MetadataJson`)
- 50+ additional procedures and functions

**Root Cause:**  
The `Models` table is defined in EF Core migrations but was not migrated to DACPAC. The project still references the old table structure.

**Fix Required:**
1. Create `Tables/dbo.Models.sql` with proper schema:
```sql
CREATE TABLE [dbo].[Models]
(
    [ModelId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [ModelName] NVARCHAR(255) NOT NULL,
    [MetadataJson] NVARCHAR(MAX) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [LastUsed] DATETIME2 NULL
);
```

### Critical Error #2: Duplicate Parameter/Column Names (SQL71508)

**Impact:** 300+ build errors blocking compilation

**Examples:**
```
dbo.fn_GenerateText.@temperature (duplicate parameter)
dbo.clr_GenerateImagePatches.@steps (duplicate parameter)
dbo.clr_GenerateTextSequence.@modelsJson (duplicate parameter)
dbo.VectorEuclideanDistance.@v1 (duplicate parameter)
dbo.sp_InsertBillingUsageRecord_Native.@MessageType (duplicate parameter)
provenance.clr_AtomicStreamSegments.@stream (duplicate parameter)
```

**Root Cause:**  
Files contain duplicate definitions or are included multiple times in the DACPAC project. Likely caused by:
- Manual SQL scripts in `sql/` folder being included alongside DACPAC definitions
- Procedures/functions defined in multiple locations
- Failed cleanup from commit e9f0403 "catastrophe"

**Fix Required:**
1. Identify duplicate file inclusions in `Hartonomous.Database.sqlproj`
2. Remove references to `sql/` folder scripts (should NOT be in DACPAC)
3. Ensure each database object is defined exactly once

### Critical Error #3: Missing Tables Referenced by Procedures

**Missing Tables Identified:**

| Table Name | Referenced By | Urgency |
|------------|---------------|---------|
| `TenantAtoms` | sp_IngestAtom, sp_ExtractMetadata | 🔴 CRITICAL |
| `Models` | 100+ objects | 🔴 CRITICAL |
| `Neo4jSyncQueue` | sp_ForwardToNeo4j_Activated | 🔴 HIGH |
| `Neo4jSyncLog` | sp_ForwardToNeo4j_Activated | 🔴 HIGH |
| `EventAtoms` | sp_GenerateEventsFromStream | 🔴 HIGH |
| `EventGenerationResults` | sp_GenerateEventsFromStream | 🔴 HIGH |
| `StreamOrchestrationResults` | sp_GenerateEventsFromStream | 🔴 HIGH |
| `AttentionGenerationLog` | sp_AttentionInference, sp_GenerateWithAttention | 🔴 HIGH |
| `AttentionInferenceResults` | sp_AttentionInference | 🔴 HIGH |
| `MultiPathReasoning` | sp_MultiPathReasoning | 🟡 MEDIUM |
| `WeightSnapshots` | sp_ListWeightSnapshots | 🟡 MEDIUM |
| `BillingUsageLedger_InMemory` | sp_InsertBillingUsageRecord_Native | 🟡 MEDIUM |
| `OperationProvenance` | sp_AuditProvenanceChain, sp_ReconstructOperationTimeline | 🟡 MEDIUM |
| `ProvenanceValidationResults` | sp_AuditProvenanceChain | 🟡 MEDIUM |
| `ProvenanceAuditResults` | sp_AuditProvenanceChain | 🟡 MEDIUM |
| `GenerationStreamSegments` | sp_TextToEmbedding | 🟡 MEDIUM |
| `AnalyzeQueue` | sp_Analyze | 🟡 MEDIUM |

**Root Cause:**  
These tables exist in:
1. EF Core migrations (`sql/ef-core/`) but not migrated to DACPAC
2. Manual SQL scripts (`sql/`) but not included in DACPAC
3. Documentation as "planned" but never implemented

**Fix Required:**  
Audit all missing tables and either:
- Migrate to DACPAC `Tables/` folder
- Remove procedures that reference them
- Mark procedures as "future implementation"

### Critical Error #4: Unresolved CLR Assembly References (SQL71501)

**Impact:** 200+ CLR functions fail to compile

**Examples:**
```
dbo.fn_GenerateEnsemble has unresolved reference to Assembly [SqlClrFunctions]
dbo.fn_ComputeEmbedding has unresolved reference to Assembly [SqlClrFunctions]
dbo.clr_ParseModelLayer has unresolved reference to Assembly [SqlClrFunctions]
dbo.ReflexionAggregate has unresolved reference to Assembly [SqlClrFunctions]
dbo.ResearchWorkflow has unresolved reference to Assembly [SqlClrFunctions]
dbo.fn_DetermineSla has unresolved reference to Assembly [SqlClrFunctions]
dbo.clr_AudioDownsample has unresolved reference to Assembly [SqlClrFunctions]
dbo.clr_FileExists has unresolved reference to Assembly [SqlClrFunctions]
```

**Root Cause:**  
CLR functions/aggregates declare `EXTERNAL NAME [SqlClrFunctions].[...]` but the assembly reference is not properly configured in the DACPAC project.

**Current State:**
```xml
<!-- Hartonomous.Database.sqlproj has 56 CLR source files linked -->
<Compile Include="..\SqlClr\VectorMath.cs" />
<Compile Include="..\SqlClr\GpuAccelerator.cs" />
<!-- ... 54 more files ... -->
```

**Issue:**  
The DACPAC project includes C# source files but may not be:
1. Building the `SqlClrFunctions.dll` assembly
2. Registering the assembly with SQL Server
3. Linking CLR function definitions to the compiled assembly

**Fix Required:**
1. Verify `SqlClr` project builds `SqlClrFunctions.dll` successfully
2. Add post-deployment script to register assembly:
```sql
CREATE ASSEMBLY [SqlClrFunctions]
FROM 'path\to\SqlClrFunctions.dll'
WITH PERMISSION_SET = UNSAFE;
```
3. Ensure all CLR function definitions use correct `EXTERNAL NAME` syntax

### Critical Error #5: Missing Indexes (~200 Expected)

**Current State:**
```
src/Hartonomous.Database/Indexes/ - EMPTY DIRECTORY
```

**Impact:**  
Database will deploy without indexes, causing:
- Severe performance degradation
- Query timeouts
- Unusable semantic search (needs vector indexes)
- Failed production workloads

**Evidence from Original Plan:**  
From `EF_CORE_VS_DACPAC_SEPARATION.md`:
> "**Index Definitions**: ~200 index files to create for performance optimization"

**Fix Required:**
1. Extract all `CREATE INDEX` statements from:
   - EF Core migrations (`sql/ef-core/`)
   - Manual SQL scripts (`sql/`)
   - Production database schema
2. Create individual files in `Indexes/` folder:
   - `IX_Atoms_TenantId_CreatedUtc.sql`
   - `IX_AtomEmbeddings_SpatialCoarse_COVERING.sql`
   - `IX_TensorAtomCoefficients_ModelId_TensorAtomId.sql`
   - etc. (~200 total)
3. Add `<Build Include="Indexes\*.sql" />` to `.sqlproj`

### Issue #6: Billing Table Duplicates

**Current State:**
```
Tables/dbo.BillingUsageLedger.sql          (main table)
Tables/dbo.BillingUsageLedger_InMemory.sql (memory-optimized variant)
Tables/dbo.BillingUsageLedger_Migrate_to_Ledger.sql (migration script)
```

**Impact:**  
Confusion about production schema, ambiguous references in procedures

**Recommendation:**
1. Determine production architecture:
   - Use **ledger table** for immutable billing compliance?
   - Use **memory-optimized** for high-throughput writes?
   - Use **standard table** for simplicity?
2. Keep ONE production table in `Tables/`
3. Move alternatives to `Scripts/Archive/` for reference
4. Update `sp_InsertBillingUsageRecord_Native` to reference correct table

### Issue #7: Missing EF Core Configurations (77 Files)

**Expected (from documentation):** 82 configuration files mixing schema + ORM concerns  
**Found:** Only 5 files:
- AtomConfiguration.cs
- CodeAtomConfiguration.cs
- ImageConfiguration.cs
- ModelConfiguration.cs
- VideoConfiguration.cs

**Possible Explanations:**
1. **Documentation was inaccurate** - 82 was a planning estimate, not reality
2. **Files were deleted** - commit e9f0403 catastrophe or subsequent repairs
3. **Never existed** - migration plan overestimated scope
4. **Different location** - files moved or restructured

**Action Required:**
1. Search entire src/Hartonomous.Data for any `*Configuration.cs` files
2. Review git history for deletions: `git log --diff-filter=D -- *Configuration.cs`
3. If deleted, determine if intentional or accidental
4. If never existed, update documentation to reflect reality

---

## Commit History Review: The "Catastrophe"

### Catastrophe Sequence

**Commit e9f0403:** `feat(database): DACPAC build success - 0 errors (892→0)`
- **Claimed:** Build success, 892 errors eliminated
- **Reality:** Files were deleted or truncated (subsequent repairs required)

**Commit eba78de:** `fix: Restore ALL deleted database project files from e9f0403 catastrophe`
- **Action:** Restored deleted files from DACPAC project
- **Implication:** Previous commit DELETED files while claiming "success"

**Commit d0b1d81:** `fix: Restore full procedure files from sql/procedures - repair truncation damage`
- **Action:** Restored truncated stored procedure files
- **Implication:** Previous commits TRUNCATED file contents

### Damage Assessment Required

**Action Items:**
1. Review `git show e9f0403` to see what was deleted/changed
2. Review `git show eba78de` to see what was restored
3. Review `git show d0b1d81` to see what was repaired
4. Identify any remaining missing content
5. Verify all 185 procedures are complete (not truncated)
6. Verify all 114 functions are complete
7. Verify all 41 aggregates are complete

---

## Migration Progress Assessment

### ✅ Successfully Migrated

| Component | Count | Status |
|-----------|-------|--------|
| Schemas | 2 | ✅ graph, provenance |
| User-Defined Types | 2 | ✅ ComponentStream, AtomicStream |
| Tables | 60 | ✅ Comprehensive coverage |
| Views | 2 | ✅ Weight history/current |
| Stored Procedures | 185 | ⚠️ Present but errors |
| Functions (CLR + T-SQL) | 114 | ⚠️ Present but CLR errors |
| Aggregates (CLR) | 41 | ⚠️ Present but CLR errors |
| CLR Source Files | 56 | ✅ Linked to project |
| NuGet Dependencies | 9 | ✅ Configured |

### 🔴 Not Migrated / Missing

| Component | Expected | Found | Gap |
|-----------|----------|-------|-----|
| Indexes | ~200 | **0** | -200 |
| Models Table | 1 | **0** | -1 |
| TenantAtoms Table | 1 | **0** | -1 |
| Neo4j Tables | 2 | **0** | -2 |
| Event Tables | 2 | **0** | -2 |
| Attention Tables | 2 | **0** | -2 |
| Provenance Tables | 3 | **0** | -3 |
| Stream Tables | 1 | **0** | -1 |
| EF Core Configs | 82 | **5** | -77 |

### ⚠️ Present but Broken

| Component | Count | Issue |
|-----------|-------|-------|
| Stored Procedures | 185 | Reference missing tables |
| CLR Functions | 60+ | Unresolved assembly references |
| CLR Aggregates | 41 | Unresolved assembly references |
| Billing Tables | 3 | Duplicates / unclear production version |

---

## Root Cause Analysis

### Why is the build failing?

1. **Incomplete Migration**: Tables from EF Core migrations were not fully migrated to DACPAC
   - `Models`, `TenantAtoms`, `Neo4jSyncQueue`, `EventAtoms`, etc.
   
2. **Duplicate Definitions**: Same objects defined in multiple locations
   - DACPAC `Procedures/` folder
   - Legacy `sql/procedures/` folder
   - Both included in build causing SQL71508 errors

3. **Missing Indexes**: ~200 index definitions never created in DACPAC
   - Performance will be catastrophic without indexes
   
4. **CLR Assembly Issues**: Assembly not properly registered or linked
   - 200+ CLR functions fail SQL71501 unresolved reference errors
   
5. **Catastrophe Commits**: File deletions/truncations from e9f0403
   - Repairs attempted but incomplete
   - Unknown extent of remaining damage

### Why are 77 EF Core configurations missing?

**Hypothesis 1:** Documentation error
- Original audit may have counted planned files, not actual files
- 82 could be estimate of tables × avg configs per table

**Hypothesis 2:** Never fully created
- Migration from EF Core to DACPAC was planned but not executed
- Only 5 core entity configurations were ever created

**Hypothesis 3:** Deleted in catastrophe
- Commit e9f0403 or subsequent changes removed them
- Git history should show deletions

**Hypothesis 4:** Intentional cleanup
- If DACPAC is source of truth, EF Core configs should be minimal
- Only need navigation properties, not schema definitions
- 5 configs might be CORRECT state

**Recommendation:**  
After DACPAC stabilization, scaffold EF Core entities FROM deployed database using:
```bash
dotnet ef dbcontext scaffold "connection-string" Microsoft.EntityFrameworkCore.SqlServer
```
This ensures EF Core entities match DACPAC schema exactly.

---

## Repair Plan

### Phase 1: Critical Table Creation (Priority 1)

**Goal:** Create missing tables to unblock 800+ reference errors

**Tasks:**
1. **Create `dbo.Models` table** (CRITICAL)
   ```sql
   CREATE TABLE [dbo].[Models]
   (
       [ModelId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
       [ModelName] NVARCHAR(255) NOT NULL,
       [MetadataJson] NVARCHAR(MAX) NULL,
       [IsActive] BIT NOT NULL DEFAULT 1,
       [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
       [LastUsed] DATETIME2 NULL,
       INDEX IX_Models_IsActive NONCLUSTERED (IsActive) WHERE IsActive = 1,
       INDEX IX_Models_ModelName NONCLUSTERED (ModelName)
   );
   ```

2. **Create `dbo.TenantAtoms` table** (HIGH)
   ```sql
   CREATE TABLE [dbo].[TenantAtoms]
   (
       [TenantId] UNIQUEIDENTIFIER NOT NULL,
       [AtomId] UNIQUEIDENTIFIER NOT NULL,
       [CreatedUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
       PRIMARY KEY (TenantId, AtomId),
       FOREIGN KEY (AtomId) REFERENCES [dbo].[Atoms](AtomId)
   );
   ```

3. **Create Neo4j tables** (HIGH)
   ```sql
   CREATE TABLE [dbo].[Neo4jSyncQueue] (...);
   CREATE TABLE [dbo].[Neo4jSyncLog] (...);
   ```

4. **Create Event tables** (HIGH)
   ```sql
   CREATE TABLE [dbo].[EventAtoms] (...);
   CREATE TABLE [dbo].[EventGenerationResults] (...);
   CREATE TABLE [dbo].[StreamOrchestrationResults] (...);
   ```

5. **Create Attention tables** (HIGH)
   ```sql
   CREATE TABLE [dbo].[AttentionGenerationLog] (...);
   CREATE TABLE [dbo].[AttentionInferenceResults] (...);
   ```

6. **Create remaining 15+ missing tables** (MEDIUM)

**Estimated Time:** 4-6 hours  
**Expected Outcome:** Reduce errors from 1,576 to ~300

### Phase 2: Eliminate Duplicate Definitions (Priority 1)

**Goal:** Fix 300+ SQL71508 duplicate element errors

**Tasks:**
1. **Audit `.sqlproj` file** - Find duplicate `<Build Include>` entries
2. **Remove `sql/` folder references** - These should NOT be in DACPAC
3. **Identify duplicate files** - Same object defined in multiple locations
4. **Consolidate definitions** - Keep DACPAC version, delete legacy
5. **Re-test build** - Verify duplicates eliminated

**Estimated Time:** 2-3 hours  
**Expected Outcome:** Reduce errors from ~300 to ~50

### Phase 3: Fix CLR Assembly References (Priority 2)

**Goal:** Resolve 200+ SQL71501 assembly reference errors

**Tasks:**
1. **Build `SqlClrFunctions.dll`** successfully
2. **Add assembly registration script**:
   ```sql
   -- Scripts/Post-Deployment/RegisterCLRAssemblies.sql
   IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SqlClrFunctions')
       DROP ASSEMBLY [SqlClrFunctions];
   GO
   
   CREATE ASSEMBLY [SqlClrFunctions]
   FROM '$(OutputPath)\SqlClrFunctions.dll'
   WITH PERMISSION_SET = UNSAFE;
   GO
   ```
3. **Verify all CLR functions use correct EXTERNAL NAME**
4. **Test CLR function execution** after deployment

**Estimated Time:** 3-4 hours  
**Expected Outcome:** Reduce errors from ~50 to ~0

### Phase 4: Create Index Definitions (Priority 2)

**Goal:** Migrate ~200 index definitions to DACPAC

**Tasks:**
1. **Extract indexes from production database**:
   ```sql
   SELECT 
       i.name AS IndexName,
       OBJECT_SCHEMA_NAME(i.object_id) + '.' + OBJECT_NAME(i.object_id) AS TableName,
       -- Generate CREATE INDEX script
   FROM sys.indexes i
   WHERE i.is_primary_key = 0 AND i.type > 0;
   ```

2. **Extract indexes from EF Core migrations**:
   - Search `sql/ef-core/` for `CREATE INDEX` statements
   - Parse and convert to individual files

3. **Create `Indexes/` files**:
   ```
   Indexes/
       IX_Atoms_TenantId_CreatedUtc.sql
       IX_AtomEmbeddings_SpatialCoarse.sql
       IX_TensorAtomCoefficients_ModelId.sql
       ... (197 more)
   ```

4. **Update `.sqlproj`**:
   ```xml
   <Build Include="Indexes\*.sql" />
   ```

**Estimated Time:** 6-8 hours  
**Expected Outcome:** Performance-critical indexes in place

### Phase 5: Resolve Billing Table Architecture (Priority 3)

**Goal:** Choose production billing table, archive alternatives

**Tasks:**
1. **Decide production architecture:**
   - Standard table (simple, portable)
   - Ledger table (immutable, compliance, SQL Server 2022+)
   - Memory-optimized (high throughput, requires In-Memory OLTP)

2. **Keep ONE table** in `Tables/dbo.BillingUsageLedger.sql`

3. **Archive alternatives**:
   ```
   Scripts/Archive/
       BillingUsageLedger_InMemory_Alternative.sql
       BillingUsageLedger_Ledger_Alternative.sql
   ```

4. **Update procedures**:
   - `sp_InsertBillingUsageRecord_Native` - reference production table

**Estimated Time:** 1-2 hours  
**Expected Outcome:** Clear billing architecture, no ambiguity

### Phase 6: Validate Catastrophe Repairs (Priority 2)

**Goal:** Ensure all files restored from commits eba78de & d0b1d81 are complete

**Tasks:**
1. **Review catastrophe commits**:
   ```bash
   git show e9f0403 --stat
   git show eba78de --stat
   git show d0b1d81 --stat
   ```

2. **Verify file completeness**:
   - All 185 procedures have complete content (not truncated)
   - All 114 functions have complete content
   - All 41 aggregates have complete content

3. **Compare with pre-catastrophe state**:
   ```bash
   git diff 63a0aa2..HEAD -- src/Hartonomous.Database/
   ```

4. **Restore any remaining missing content** from git history

**Estimated Time:** 2-3 hours  
**Expected Outcome:** Confirmation that all catastrophe damage is repaired

### Phase 7: EF Core Configuration Strategy (Priority 3)

**Goal:** Determine correct state of EF Core configurations

**Tasks:**
1. **Search for all configurations**:
   ```bash
   Get-ChildItem -Path "src\Hartonomous.Data\" -Filter "*Configuration.cs" -Recurse
   ```

2. **Review git history**:
   ```bash
   git log --oneline --all -- src/Hartonomous.Data/Configurations/
   git log --diff-filter=D -- src/Hartonomous.Data/Configurations/*.cs
   ```

3. **Decide configuration strategy:**
   - **Option A:** Scaffold from deployed DACPAC (RECOMMENDED)
   - **Option B:** Manually create 77 missing configurations
   - **Option C:** Keep minimal 5 configs, add only as needed

4. **Document decision** in `EF_CORE_VS_DACPAC_SEPARATION.md`

**Estimated Time:** 2-4 hours  
**Expected Outcome:** Clear EF Core strategy, documented approach

### Phase 8: Deploy & Test (Priority 1)

**Goal:** Deploy DACPAC to test database and verify success

**Tasks:**
1. **Fix all build errors** (Phases 1-3 complete)
2. **Build DACPAC successfully**:
   ```bash
   dotnet build src\Hartonomous.Database\Hartonomous.Database.sqlproj -c Release
   ```
3. **Deploy to test database**:
   ```bash
   SqlPackage.exe /Action:Publish /SourceFile:"Hartonomous.Database.dacpac" /TargetConnectionString:"..."
   ```
4. **Run validation queries**:
   ```sql
   -- Verify all tables exist
   SELECT COUNT(*) FROM sys.tables; -- Should be 60+
   
   -- Verify all CLR functions work
   SELECT dbo.clr_VectorDotProduct(0x..., 0x...);
   
   -- Verify indexes exist
   SELECT COUNT(*) FROM sys.indexes WHERE is_primary_key = 0; -- Should be ~200
   ```
5. **Test critical procedures**:
   ```sql
   EXEC dbo.sp_IngestAtom @AtomId = NEWID(), @Content = 'Test', ...;
   EXEC dbo.sp_SemanticSearch @Query = 'test', @K = 10;
   ```

**Estimated Time:** 2-3 hours  
**Expected Outcome:** Working database deployment, all objects functional

### Phase 9: Scaffold EF Core Entities (Priority 1)

**Goal:** Generate EF Core entities from deployed database (DACPAC as source of truth)

**Tasks:**
1. **Install EF Core tools**:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Scaffold DbContext** from deployed database:
   ```bash
   dotnet ef dbcontext scaffold "Server=localhost;Database=Hartonomous;..." \
       Microsoft.EntityFrameworkCore.SqlServer \
       --output-dir Models \
       --context-dir Data \
       --context HartonomousDbContext \
       --force
   ```

3. **Review generated entities**:
   - Verify all 60+ tables mapped
   - Verify navigation properties correct
   - Verify data annotations match schema

4. **Customize configurations**:
   - Add `DeleteBehavior.Restrict` for critical FKs
   - Configure `[NotMapped]` for computed columns
   - Add value converters for JSON columns

5. **Test EF Core operations**:
   ```csharp
   using var db = new HartonomousDbContext();
   var atom = new Atom { AtomId = Guid.NewGuid(), Content = "Test" };
   db.Atoms.Add(atom);
   await db.SaveChangesAsync();
   ```

**Estimated Time:** 3-4 hours  
**Expected Outcome:** EF Core entities match DACPAC schema, API development unblocked

---

## Estimated Total Repair Time

| Phase | Priority | Time Estimate | Dependencies |
|-------|----------|---------------|--------------|
| 1. Create Missing Tables | P1 | 4-6 hours | None |
| 2. Eliminate Duplicates | P1 | 2-3 hours | None |
| 3. Fix CLR References | P2 | 3-4 hours | Phase 1 |
| 4. Create Indexes | P2 | 6-8 hours | Phase 1 |
| 5. Billing Architecture | P3 | 1-2 hours | None |
| 6. Validate Repairs | P2 | 2-3 hours | Phases 1-2 |
| 7. EF Core Strategy | P3 | 2-4 hours | None |
| 8. Deploy & Test | P1 | 2-3 hours | Phases 1-3 |
| 9. Scaffold EF Core | P1 | 3-4 hours | Phase 8 |
| **TOTAL** | | **25-37 hours** | |

**Realistic Timeline:** 4-5 working days (8-hour days)  
**Aggressive Timeline:** 3 working days (10-12 hour days)

---

## Success Criteria

### Milestone 1: DACPAC Builds Successfully
- [ ] Zero build errors
- [ ] Less than 50 warnings (only informational)
- [ ] All 60+ tables defined
- [ ] All 185 procedures compile
- [ ] All 114 functions compile
- [ ] All 41 aggregates compile
- [ ] ~200 indexes defined

### Milestone 2: Database Deploys Successfully
- [ ] `SqlPackage.exe /Action:Publish` succeeds
- [ ] All tables created
- [ ] All stored procedures created
- [ ] All functions operational (CLR + T-SQL)
- [ ] All aggregates operational
- [ ] All indexes created
- [ ] CLR assembly registered with UNSAFE permission

### Milestone 3: Critical Operations Work
- [ ] `sp_IngestAtom` executes successfully
- [ ] `sp_SemanticSearch` returns results (requires indexes)
- [ ] `sp_GenerateText` executes (requires CLR + Models table)
- [ ] `sp_DiscoverAndBindConcepts` executes (requires provenance tables)
- [ ] Billing procedures execute (requires chosen table architecture)

### Milestone 4: EF Core Entities Generated
- [ ] `dotnet ef dbcontext scaffold` succeeds
- [ ] All 60+ entities generated
- [ ] Navigation properties correctly configured
- [ ] DbContext compiles
- [ ] Basic CRUD operations work via EF Core

### Milestone 5: API Development Unblocked
- [ ] EF Core `DbContext` usable in API controllers
- [ ] Can query `Atoms`, `AtomEmbeddings`, `Models`
- [ ] Can execute stored procedures via EF Core
- [ ] Can perform semantic search queries
- [ ] API integration tests pass

---

## Recommendations

### Immediate Actions (Next 24 Hours)

1. **Create missing `Models` table** - Unblocks 100+ errors
2. **Remove duplicate definitions** - Fix 300+ SQL71508 errors
3. **Create missing tenant/event/attention tables** - Unblocks 500+ errors
4. **Test incremental build progress** - Verify error count decreases

### Short-Term Actions (Next Week)

1. **Complete all table migrations** - Get to 0 SQL71501 table errors
2. **Fix CLR assembly registration** - Get CLR functions working
3. **Create first 50 critical indexes** - Enable basic performance
4. **Deploy to test database** - Validate deployment works
5. **Scaffold EF Core entities** - Unblock API development

### Long-Term Actions (Next Month)

1. **Complete all 200 indexes** - Full performance optimization
2. **Add comprehensive tests** - Validate all procedures work
3. **Document final architecture** - Update all documentation
4. **Production deployment** - Roll out stable DACPAC

### Process Improvements

1. **Pre-commit validation**: Add DACPAC build to CI/CD
2. **Schema versioning**: Track schema changes in migration scripts
3. **Code review**: Require review for all database changes
4. **Testing**: Add integration tests for all stored procedures
5. **Documentation**: Keep `DACPAC_MIGRATION_AUDIT_REPORT.md` updated

---

## Conclusion

The DACPAC migration is **incomplete and broken** with 1,576 build errors. However, the majority of work HAS been done:

✅ **Completed:**
- 60 tables migrated
- 185 procedures migrated
- 114 functions migrated  
- 41 aggregates migrated
- 56 CLR source files linked
- Project structure established

🔴 **Remaining Work:**
- Create 15+ missing tables (4-6 hours)
- Fix 300+ duplicate definition errors (2-3 hours)
- Fix 200+ CLR assembly errors (3-4 hours)
- Create 200 index definitions (6-8 hours)
- Deploy and test (2-3 hours)
- Scaffold EF Core entities (3-4 hours)

**Total Estimated Effort:** 25-37 hours (4-5 working days)

With focused effort over the next week, the DACPAC can be stabilized, deployed, and ready for API development. The foundation is solid; the migration just needs completion.

---

**Next Step:** Begin Phase 1 repairs (create missing tables) immediately to unblock development.
