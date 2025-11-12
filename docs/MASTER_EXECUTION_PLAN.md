# Master Execution Plan - Database Project Consolidation

**Date:** November 12, 2025  
**Current Status:** Phase 1.1-1.2 Complete (131 files created)  
**Branch:** main (synced with origin)  
**Related Docs:**
- [CONSOLIDATED_EXECUTION_PLAN.md](CONSOLIDATED_EXECUTION_PLAN.md)
- [EF_CORE_VS_DACPAC_SEPARATION.md](EF_CORE_VS_DACPAC_SEPARATION.md)
- [SCRIPT_CATEGORIZATION.md](SCRIPT_CATEGORIZATION.md)
- [DEDUPLICATION_AND_FLOW_AUDIT.md](DEDUPLICATION_AND_FLOW_AUDIT.md)

---

## 🎯 Overall Objective

**Primary Goal:** Consolidate all database schema into `Hartonomous.Database.sqlproj` as single source of truth

**Success Criteria:**
- ✅ DACPAC builds successfully with all schema objects
- ✅ All indexes, constraints, procedures in database project
- ✅ EF Core configurations simplified (navigation properties only)
- ✅ No duplication between sql/ and Database project
- ✅ Delete sql/ and src/SqlClr/ folders (replaced by DACPAC + CLR project)

---

## 📊 Current Progress (as of Nov 12, 2025)

### Completed ✅

**Phase 1.1: CLR Function Extraction**
- ✅ 1.1.1: Extracted 78 CLR wrapper functions (57 clr_* + 20 fn_* + 1 agg_*)
- ✅ 1.1.2: Moved 40 aggregates to Aggregates/ folder
- ✅ 1.1.3: Deleted duplicate Autonomy.FileSystemBindings.sql

**Phase 1.2: Table Group Breakdown**
- ✅ Created 13 individual table files from 4 group files
  - Attention tables (4 files)
  - Stream tables (4 files)
  - Provenance tables (3 files)
  - Reasoning tables (2 files)

**Total:** 131 individual schema files created and organized

### Git Status

```
Uncommitted Changes:
- 131 new files (CLR functions, aggregates, tables)
- 52 deleted files (multi-procedure files, duplicates)
- 5 modified files (fn_BindConcepts, fn_DiscoverConcepts, types)
- 4 documentation files (this plan + audits)

Last Commit: 63a0aa2 "Pre-consolidation checkpoint..."
Branch: main (synced with origin/main)
```

---

## 🗂️ Remaining Work - Prioritized Order

### **Phase 1: Schema File Organization** (Continue)

#### **Priority 1: Phase 1.3 - Script Categorization** ⏳
*Estimated: 3-4 hours*

**Why First:** Must understand what goes in DACPAC vs external deployment before proceeding

**Sub-phases:**

1. **1.3b: Extract Service Broker Objects** (30 min)
   - Create 18 individual files from setup-service-broker.sql
   - 4 MESSAGE TYPEs → `ServiceBroker/MessageTypes/dbo.*.sql`
   - 4 CONTRACTs → `ServiceBroker/Contracts/dbo.*.sql`
   - 5 QUEUEs → `ServiceBroker/Queues/dbo.*.sql`
   - 5 SERVICEs → `ServiceBroker/Services/dbo.*.sql`

2. **1.3c: Create Pre-Deployment Scripts** (1 hour)
   - `Scripts/Pre-Deployment/Setup_FILESTREAM_Filegroup.sql`
   - `Scripts/Pre-Deployment/Enable_CDC.sql`
   - `Scripts/Pre-Deployment/Enable_QueryStore.sql`
   - `Scripts/Pre-Deployment/Enable_AutomaticTuning.sql`
   - Use IF NOT EXISTS pattern for idempotency

3. **1.3d: Move Post-Deployment Scripts** (30 min)
   - `Scripts/Post-Deployment/Setup_Vector_Indexes.sql` (already idempotent)
   - `Scripts/Post-Deployment/Optimize_ColumnstoreCompression.sql` (already idempotent)
   - Verify all have proper IF NOT EXISTS checks

4. **1.3e: Delete EF Core Migration Files** (15 min)
   - Delete `sql/ef-core/Hartonomous.Schema.sql`
   - Delete `src/Hartonomous.Data/Migrations/*.cs` (3 files)
   - Keep DbContext and Configurations (separate task to simplify them)

#### **Priority 2: Phase 1.4 - Break Down Large Procedure Files** ⏳
*Estimated: 4-6 hours*

**Why Second:** Need all procedures as individual files before deduplication

**Scope:** Extract ~60-70 procedures from multi-procedure files

**Files to Process:**
```
sql/procedures/
├── Common.*.sql (multiple procedures per file)
├── Autonomy.*.sql (multiple procedures per file)
├── dbo.AnalysisWorkflows.sql (contains multiple workflow procedures)
└── ... (identify all multi-procedure files)
```

**Process:**
1. Read each multi-procedure file
2. Split by `CREATE PROCEDURE` boundaries
3. Create individual files: `Procedures/dbo.{ProcedureName}.sql`
4. Verify syntax (GO statements, dependencies)

#### **Priority 3: Phase 1.5 - Update .sqlproj Build Items** ⏳
*Estimated: 1 hour*

**Why Third:** Need complete file inventory before bulk-adding to project

**Tasks:**
1. Generate list of all new files (use PowerShell script)
2. Add `<Build Include="..." />` items to Hartonomous.Database.sqlproj
3. Organize by folder structure
4. Test build to catch missing dependencies

---

### **Phase 2: EF Core / DACPAC Separation** 🆕
*Estimated: 6-8 hours*

**Why After Phase 1:** Need all DACPAC files in place before extracting schema from EF Core

**See:** [EF_CORE_VS_DACPAC_SEPARATION.md](EF_CORE_VS_DACPAC_SEPARATION.md)

**Sub-phases:**

#### **2.1: Extract Indexes to DACPAC** (3-4 hours)
- Create `src/Hartonomous.Database/Indexes/` folder
- Generate ~200 index files from EF Core configurations
- Naming: `{schema}.{table}.{indexName}.sql`
- Add all to .sqlproj as Build items

#### **2.2: Add Check Constraints** (1 hour)
- Add to table definitions inline
- Extract from EF Core `.HasCheckConstraint()` calls
- ~5 constraints total

#### **2.3: Update Table Metadata** (1 hour)
- Change `NVARCHAR(MAX)` → `JSON` for SQL Server 2025
- Verify all DEFAULT constraints present
- ~25 tables affected

#### **2.4: Simplify EF Core Configurations** (2 hours)
- Remove schema definitions (defaults, indexes, types)
- Keep navigation properties and relationships
- Reduce from ~90 lines → ~30 lines per config
- Test that relationships still work

---

### **Phase 3: Deduplication Pass**
*Estimated: 4-6 hours*

**Why After Phase 2:** Need complete inventory of all schema before identifying duplicates

**See:** [DEDUPLICATION_AND_FLOW_AUDIT.md](DEDUPLICATION_AND_FLOW_AUDIT.md)

**Tasks:**
1. Scan all procedure files for duplicate logic
2. Identify procedures with identical functionality
3. Consolidate or mark for deletion
4. Update dependencies to point to canonical version

---

### **Phase 4: Flow Optimization**
*Estimated: 8-12 hours*

**Why After Phase 3:** Optimize only the procedures we're keeping

**Tasks:**
1. Refactor CURSOR-based procedures to set-based logic
2. Replace WHILE loops with CTEs or set operations
3. Optimize fn_ComputeEmbedding calls
4. Add indexes for performance-critical queries

---

### **Phase 5: Build & Cleanup**
*Estimated: 2-3 hours*

**Why Last:** Final step after all schema is consolidated

**Tasks:**
1. Copy CLR C# code to CLR project (if separate)
2. Build DACPAC successfully
3. Deploy to test database
4. Verify all objects present
5. Delete old folders:
   - `sql/` (replaced by DACPAC)
   - `src/SqlClr/` (moved to CLR project or DACPAC)
6. Final commit

---

## 📅 Recommended Execution Order

### **Session 1: Complete Phase 1.3** (3-4 hours)
- Extract Service Broker objects
- Create pre/post-deployment scripts
- Delete EF Core migrations
- **Commit:** "feat(database): complete script categorization - Service Broker + deployment scripts"

### **Session 2: Phase 1.4-1.5** (5-7 hours)
- Break down large procedure files
- Update .sqlproj with all Build items
- **Commit:** "feat(database): extract all procedures to individual files + update project"

### **Session 3: Phase 2.1-2.2 (Indexes & Constraints)** (4-5 hours)
- Extract all indexes from EF Core to DACPAC
- Add check constraints
- **Commit:** "feat(database): migrate indexes and constraints from EF Core to DACPAC"

### **Session 4: Phase 2.3-2.4 (Simplify EF Core)** (3 hours)
- Update table metadata (JSON types)
- Simplify EF Core configurations
- **Commit:** "refactor(ef-core): simplify configurations - focus on navigation properties"

### **Session 5: Phase 3 (Deduplication)** (4-6 hours)
- Identify and remove duplicates
- **Commit:** "refactor(database): remove duplicate procedures and consolidate logic"

### **Session 6: Phase 4 (Optimization)** (8-12 hours) - *Optional, can defer*
- Optimize cursors and loops
- **Commit:** "perf(database): optimize procedures - replace cursors with set-based operations"

### **Session 7: Phase 5 (Build & Cleanup)** (2-3 hours)
- Build DACPAC successfully
- Delete old folders
- **Commit:** "feat(database): complete consolidation - DACPAC as single source of truth"

---

## 🎯 Immediate Next Steps (Start Here)

### 1. Commit Current Work ✅

```powershell
# Stage all changes
git add .

# Commit with descriptive message
git commit -m "feat(database): Phase 1.1-1.2 complete - 131 schema files extracted

- Extracted 78 CLR wrapper functions to individual files
- Organized 40 aggregates in dedicated folder
- Created 13 table files from group definitions
- Removed duplicate Autonomy.FileSystemBindings.sql
- Added comprehensive documentation for remaining work"

# Push to remote
git push origin main
```

### 2. Begin Phase 1.3b: Service Broker Extraction

**File to Process:** `scripts/setup-service-broker.sql`

**Output Structure:**
```
src/Hartonomous.Database/
└── ServiceBroker/
    ├── MessageTypes/
    │   ├── dbo.AnalyzeMessage.sql
    │   ├── dbo.HypothesizeMessage.sql
    │   ├── dbo.ActMessage.sql
    │   └── dbo.LearnMessage.sql
    ├── Contracts/
    │   ├── dbo.AnalyzeContract.sql
    │   ├── dbo.HypothesizeContract.sql
    │   ├── dbo.ActContract.sql
    │   └── dbo.LearnContract.sql
    ├── Queues/
    │   ├── dbo.InitiatorQueue.sql
    │   ├── dbo.AnalyzeQueue.sql
    │   ├── dbo.HypothesizeQueue.sql
    │   ├── dbo.ActQueue.sql
    │   └── dbo.LearnQueue.sql
    └── Services/
        ├── dbo.Initiator.sql
        ├── dbo.AnalyzeService.sql
        ├── dbo.HypothesizeService.sql
        ├── dbo.ActService.sql
        └── dbo.LearnService.sql
```

### 3. Create Pre-Deployment Scripts (1.3c)

**Pattern for idempotency:**
```sql
-- Pre-Deployment/Setup_FILESTREAM_Filegroup.sql
USE [$(DatabaseName)]
GO

IF NOT EXISTS (SELECT * FROM sys.filegroups WHERE name = N'FILESTREAM_FG')
BEGIN
    ALTER DATABASE [$(DatabaseName)] 
    ADD FILEGROUP FILESTREAM_FG CONTAINS FILESTREAM;
    
    PRINT 'FILESTREAM filegroup created';
END
ELSE
BEGIN
    PRINT 'FILESTREAM filegroup already exists';
END
GO
```

---

## 📈 Progress Tracking

### Overall Completion: ~25%

| Phase | Status | Completion | Est. Hours Remaining |
|-------|--------|-----------|---------------------|
| 1.1 CLR Extraction | ✅ Complete | 100% | 0 |
| 1.2 Table Breakdown | ✅ Complete | 100% | 0 |
| 1.3 Script Categorization | ⏳ In Progress | 0% | 3-4 |
| 1.4 Procedure Breakdown | ❌ Not Started | 0% | 4-6 |
| 1.5 Update .sqlproj | ❌ Not Started | 0% | 1 |
| 2 EF Core Separation | ❌ Not Started | 0% | 6-8 |
| 3 Deduplication | ❌ Not Started | 0% | 4-6 |
| 4 Optimization | ❌ Not Started | 0% | 8-12 (optional) |
| 5 Build & Cleanup | ❌ Not Started | 0% | 2-3 |

**Total Estimated:** 28-42 hours (excluding optional optimization)

---

## ⚠️ Risk Mitigation

### Backup Points

**Commit after each major phase** to enable rollback:
- ✅ After Phase 1.3: Script categorization complete
- ✅ After Phase 1.4-1.5: All files extracted and project updated
- ✅ After Phase 2: EF Core separation complete
- ✅ After Phase 3: Deduplication complete
- ✅ After Phase 5: Final build successful

### Testing Checkpoints

**After each commit:**
1. Build DACPAC (`MSBuild Hartonomous.Database.sqlproj`)
2. Check for errors in Output window
3. Verify no missing dependencies
4. Test deploy to local SQL Server (if available)

### Rollback Plan

If build fails or deployment breaks:
```powershell
# Revert to last good commit
git log --oneline -10  # Find last working commit
git reset --hard <commit-hash>
git push --force origin main  # Only if no one else is working on branch
```

---

## 📝 Documentation Updates

### Files to Update as Work Progresses

1. **This file** - Update progress tracking section
2. **TODO.md** - Check off completed items
3. **CHANGELOG.md** - Add entries for each phase
4. **README.md** - Update deployment instructions once DACPAC is final

---

## 🎬 Ready to Execute?

**Current recommendation:** Start with Phase 1.3b (Service Broker extraction)

Shall I proceed with extracting the Service Broker objects now?
