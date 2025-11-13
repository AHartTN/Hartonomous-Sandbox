# Scripts Folder Comprehensive Audit
**Date:** 2025-11-13  
**Auditor:** AI Assistant  
**Purpose:** Full review and audit of all scripts for value, accuracy, currency, and relevance

---

## Executive Summary

**Total Files Audited:** 32 files in `/scripts` + 10 files in `/scripts/deploy`  
**Total Size:** ~11 MB (mostly the generated clr-deploy-generated.sql at 10.8 MB)  
**Last Modified Range:** Nov 6, 2025 - Nov 13, 2025 (very recent, within 1 week)  
**Git Activity:** 61 unique files modified in scripts/ since 2024-01-01

---

## CRITICAL FINDINGS

### 🔴 IMMEDIATE ISSUES

1. **clr-deploy-generated.sql (10.8 MB)** - Generated file, should NOT be in source control
   - Status: TEMPORARY/GENERATED
   - Action: Add to .gitignore, delete from repo
   - Risk: Binary bloat in repository

2. **Multiple CLR deployment scripts with overlapping functionality:**
   - `deploy-clr-secure.ps1` (Nov 11)
   - `deploy-autonomous-clr-functions.sql` (Nov 13)
   - `deploy-autonomous-clr-functions-manual.sql` (Nov 13)
   - `Deploy-Main-Assembly.sql` (Nov 13)
   - `Register-All-CLR-Dependencies.sql` (Nov 13)
   - `Trust-All-CLR-Assemblies.sql` (Nov 13)
   - `Trust-GAC-Assemblies.sql` (Nov 13)
   - Status: FRAGMENTED/OVERLAPPING
   - Risk: No single source of truth, difficult to maintain

3. **No clear idempotent CLR deployment pipeline**
   - Multiple scripts doing partial CLR setup
   - No documented execution order
   - Risk: Non-repeatable deployments

---

## DETAILED FILE ANALYSIS

### Category: CLR Deployment (8 files)
**Status: NEEDS CONSOLIDATION**

| File | Size | Modified | Purpose | Issues | Recommendation |
|------|------|----------|---------|--------|----------------|
| `deploy-clr-secure.ps1` | 17.98 KB | Nov 11 | Multi-assembly CLR deployment | Looks for old bin structure (`src\SqlClr\bin\Release`), DACPAC now contains CLR | UPDATE or DEPRECATE |
| `Trust-All-CLR-Assemblies.sql` | 8.70 KB | Nov 13 | Trust deps + GAC assemblies | Good - comprehensive, idempotent | KEEP - integrate into DACPAC pre-deploy |
| `Trust-GAC-Assemblies.sql` | 5.99 KB | Nov 13 | Trust only deps folder assemblies | Subset of Trust-All-CLR | MERGE into Trust-All |
| `Register-All-CLR-Dependencies.sql` | 8.22 KB | Nov 13 | CREATE ASSEMBLY for all deps | Missing System.Drawing, MathNet, etc. | UPDATE with all dependencies |
| `Deploy-Main-Assembly.sql` | 3.13 KB | Nov 13 | Deploy main Hartonomous.Clr | Manual deployment script | DEPRECATE - DACPAC handles this |
| `deploy-autonomous-clr-functions.sql` | 8.25 KB | Nov 13 | Deploy CLR + functions | Manual deployment script | DEPRECATE - DACPAC handles this |
| `deploy-autonomous-clr-functions-manual.sql` | 14.14 KB | Nov 13 | Manual CLR deployment | Manual deployment script | DEPRECATE - DACPAC handles this |
| `clr-deploy-generated.sql` | 10832 KB | Nov 13 | Generated CLR deployment SQL | GENERATED FILE - binary bloat | DELETE + .gitignore |

**CONSOLIDATION PLAN:**
1. Create single `Setup-CLR-Prerequisites.ps1` script that:
   - Runs `Setup-CLR-Security.sql` (asymmetric key, login, permissions)
   - Runs `Trust-All-CLR-Assemblies.sql` (trust all deps + GAC)
   - Registers ALL dependency assemblies (System.Drawing, MathNet, etc.) in correct order
2. DACPAC pre-deployment script only checks prerequisites exist
3. Delete all manual CLR deployment SQL files

---

### Category: Database Deployment Orchestration (3 files)
**Status: NEEDS ALIGNMENT WITH DACPAC**

| File | Size | Modified | Purpose | Issues | Recommendation |
|------|------|----------|---------|--------|----------------|
| `deploy-database-unified.ps1` | 28.69 KB | Nov 11 | Full database deployment orchestrator | Calls `deploy-clr-secure.ps1` with old bin path | UPDATE CLR phase to use DACPAC approach |
| `deploy-local.ps1` | 7.89 KB | Nov 7 | Local development deployment | Purpose unclear vs unified script | REVIEW - merge or deprecate |
| `deployment-functions.ps1` | 15.88 KB | Nov 6 | Shared deployment functions | Not imported by other scripts? | REVIEW usage or deprecate |

**RECOMMENDATION:**
- Update `deploy-database-unified.ps1` Phase 3 (CLR) to:
  1. Run `Setup-CLR-Prerequisites.ps1`
  2. Continue to Phase 4 (DACPAC deployment which contains CLR assembly)
- Deprecate `deploy-local.ps1` if redundant
- Either use `deployment-functions.ps1` or remove it

---

### Category: Modular Deployment Scripts - `/scripts/deploy/` (10 files)
**Status: WELL-STRUCTURED BUT MAY BE REDUNDANT**

| File | Size | Modified | Purpose | Assessment |
|------|------|----------|---------|------------|
| `01-prerequisites.ps1` | 11.7 KB | Nov 6 | Validate environment | Good - keep |
| `02-database-create.ps1` | 8.6 KB | Nov 6 | Create database | Good - keep |
| `03-filestream.ps1` | 9.4 KB | Nov 6 | Setup FILESTREAM | Good - keep |
| `04-clr-assembly.ps1` | 13.2 KB | Nov 10 | CLR deployment | REVIEW - align with DACPAC |
| `05-ef-migrations.ps1` | 10.3 KB | Nov 6 | Run EF migrations | Good - keep |
| `06-service-broker.ps1` | 11.9 KB | Nov 6 | Service Broker setup | Good - keep |
| `07-verification.ps1` | 16.6 KB | Nov 6 | Post-deployment validation | Good - keep |
| `08-create-procedures.ps1` | 12.8 KB | Nov 7 | Deploy stored procedures | REVIEW - DACPAC contains procedures? |
| `deploy-database.ps1` | 11.6 KB | Nov 7 | Orchestrator for modular scripts | Good - Azure DevOps pipeline orchestrator |
| `README.md` | 18.5 KB | Nov 11 | Documentation | Good - keep updated |

**RECOMMENDATION:**
- These are clean, modular, well-documented Azure DevOps pipeline scripts
- Keep this structure for CI/CD
- Update `04-clr-assembly.ps1` to align with DACPAC approach
- Clarify relationship between `deploy-database.ps1` and `deploy-database-unified.ps1`

---

### Category: DACPAC Build/Sanitization (7 files)
**Status: LEGACY/TRANSITIONAL**

| File | Size | Modified | Purpose | Current Relevance |
|------|------|----------|---------|-------------------|
| `clean-sql-dacpac-deep.ps1` | 3.32 KB | Nov 10 | Deep clean for DACPAC build | Was needed during consolidation | KEEP for now |
| `clean-sql-for-dacpac.ps1` | 2.13 KB | Nov 10 | Clean SQL scripts | Was needed during consolidation | KEEP for now |
| `convert-tables-to-dacpac-format.ps1` | 6.64 KB | Nov 10 | Convert migration tables | Transitional | ARCHIVE after verification |
| `sanitize-dacpac-tables.ps1` | 2.94 KB | Nov 10 | Fix table scripts | Transitional | ARCHIVE after verification |
| `split-procedures-for-dacpac.ps1` | 5.15 KB | Nov 10 | Split procedures | Transitional | ARCHIVE after verification |
| `extract-tables-from-migration.ps1` | 2.16 KB | Nov 10 | Extract from EF migration | Transitional | ARCHIVE after verification |
| `add-go-after-create-table.ps1` | 1.36 KB | Nov 10 | Add GO statements | Transitional | ARCHIVE after verification |

**RECOMMENDATION:**
- These were one-time migration scripts during DACPAC consolidation (commit dcd48ac)
- Move to `/scripts/archive/migration-2025-11/` folder
- Document that these are historical

---

### Category: EF Core Integration (1 file)
**Status: GOOD**

| File | Size | Modified | Purpose | Assessment |
|------|------|----------|---------|------------|
| `generate-table-scripts-from-efcore.ps1` | 5.20 KB | Nov 10 | Extract table definitions from EF | Useful for hybrid EF/DACPAC workflow | KEEP |

---

### Category: Dependency Analysis (5 files)
**Status: USEFUL DEVELOPMENT TOOLS**

| File | Size | Modified | Purpose | Assessment |
|------|------|----------|---------|------------|
| `analyze-all-dependencies.ps1` | 9.22 KB | Nov 9 | Full dependency analysis | Good development tool | KEEP |
| `analyze-dependencies.ps1` | 7.46 KB | Nov 9 | Dependency analysis | Similar to above | MERGE? |
| `map-all-dependencies.ps1` | 2.14 KB | Nov 9 | Map dependency graph | Good development tool | KEEP |
| `validate-package-versions.ps1` | 12.91 KB | Nov 11 | Validate CLR assembly versions | CRITICAL for CLR 4.8.1 compatibility | KEEP - document usage |
| `copy-dependencies.ps1` | 3.74 KB | Nov 11 | Copy deps to dependencies folder | Part of build process | KEEP |

**RECOMMENDATION:**
- Keep all - these are valuable for maintaining CLR assembly compatibility
- Document when/how to use each
- Consider merging the two analyze scripts

---

### Category: Build/Warning Analysis (3 files)
**Status: DEVELOPMENT UTILITIES**

| File | Size | Modified | Purpose | Assessment |
|------|------|----------|---------|------------|
| `dacpac-sanity-check.ps1` | 7.73 KB | Nov 10 | Validate DACPAC build | Good for pre-deployment validation | KEEP |
| `analyze-build-warnings.ps1` | 2.12 KB | Nov 13 | Parse build warnings | Development utility | KEEP |
| `extract-all-warnings.ps1` | 2.30 KB | Nov 13 | Extract warnings from log | Development utility | KEEP or MERGE with above |

---

### Category: Validation/Testing (3 files)
**Status: GOOD**

| File | Size | Modified | Purpose | Assessment |
|------|------|----------|---------|------------|
| `AutonomousSystemValidation.sql` | 8.69 KB | Nov 6 | System health checks | Good - production validation | KEEP |
| `verify-temporal-tables.sql` | 1.37 KB | Nov 6 | Verify temporal table setup | Good - specific validation | KEEP |
| `seed-data.sql` | 7.50 KB | Nov 7 | Insert test/seed data | Development/testing | KEEP |

---

### Category: Standalone CLR Scripts (2 files)
**Status: MANUAL HELPERS**

| File | Size | Modified | Purpose | Assessment |
|------|------|----------|---------|------------|
| `update-clr-assembly.sql` | 1.40 KB | Nov 13 | Update existing CLR assembly | Manual helper | KEEP for dev work |
| `CLR_SECURITY_ANALYSIS.md` | 10.24 KB | Nov 8 | CLR security documentation | Good documentation | KEEP - ensure up to date |

---

## RECOMMENDED ACTIONS

### Phase 1: Immediate Cleanup (Do Now)
1. ✅ Delete `clr-deploy-generated.sql` (10.8 MB)
2. ✅ Add `*-generated.sql` to `.gitignore`
3. ✅ Create archive folder: `/scripts/archive/migration-2025-11/`
4. ✅ Move DACPAC migration scripts to archive

### Phase 2: CLR Deployment Consolidation (This Week)
1. ✅ Create `Setup-CLR-Prerequisites.ps1` (idempotent, comprehensive)
   - Runs Setup-CLR-Security.sql
   - Runs Trust-All-CLR-Assemblies.sql  
   - Registers ALL dependency assemblies in correct order
   - Validates each step
2. ✅ Update DACPAC pre-deployment script to only check prerequisites
3. ✅ Update `deploy-database-unified.ps1` Phase 3 to use new approach
4. ✅ Deprecate/delete manual CLR SQL scripts
5. ✅ Test end-to-end DACPAC deployment

### Phase 3: Documentation (This Week)
1. ✅ Create `/scripts/README.md` with:
   - Purpose of each script category
   - Execution order for different scenarios
   - Development vs Production scripts
2. ✅ Update CLR_SECURITY_ANALYSIS.md with current state
3. ✅ Document the DACPAC deployment pipeline

### Phase 4: Script Consolidation (Next Week)
1. ✅ Merge `analyze-dependencies.ps1` and `analyze-all-dependencies.ps1`
2. ✅ Merge `extract-all-warnings.ps1` and `analyze-build-warnings.ps1`
3. ✅ Clarify `deploy-database.ps1` vs `deploy-database-unified.ps1`
4. ✅ Review and either use or remove `deployment-functions.ps1`

---

## DEPLOYMENT PIPELINE CLARITY NEEDED

### Current State (UNCLEAR)
```
❓ User wants to deploy DACPAC
   ↓
❓ Which script to use?
   • deploy-database-unified.ps1?
   • scripts/deploy/deploy-database.ps1?
   • Manual sqlpackage?
   ↓
❓ CLR already deployed?
   • Trust-All-CLR-Assemblies.sql?
   • Trust-GAC-Assemblies.sql?
   • Register-All-CLR-Dependencies.sql?
   • deploy-clr-secure.ps1?
   ↓
❓ DACPAC contains CLR assembly - does it deploy dependencies?
```

### Desired State (CLEAR)
```
✅ Fresh localhost deployment:
   1. Run: scripts/Setup-CLR-Prerequisites.ps1 -Server localhost
      (One-time: security, trust, register all deps)
   2. Run: sqlpackage /Action:Publish /SourceFile:...dacpac
      (DACPAC deploys Hartonomous.Clr + all schema objects)

✅ Update existing deployment:
   1. Run: sqlpackage /Action:Publish /SourceFile:...dacpac
      (DACPAC updates schema, CLR already trusted)

✅ Azure DevOps CI/CD:
   1. Run: scripts/deploy/deploy-database.ps1
      (Orchestrates modular scripts)
```

---

## IDEMPOTENCY AUDIT

### ✅ IDEMPOTENT Scripts
- `Trust-All-CLR-Assemblies.sql` - checks sys.trusted_assemblies
- `Register-All-CLR-Dependencies.sql` - checks sys.assemblies
- `scripts/deploy/*.ps1` - all modular scripts check before create
- `dacpac-sanity-check.ps1` - read-only

### ⚠️ NOT IDEMPOTENT / UNCLEAR
- `deploy-clr-secure.ps1` - drops all CLR objects first (destructive)
- `deploy-autonomous-clr-functions*.sql` - manual scripts, unclear
- `clr-deploy-generated.sql` - generated, not designed for rerun

### 🔴 ACTION REQUIRED
- Ensure all deployment scripts are idempotent
- Add clear logging of what changed vs what already existed
- Never drop objects without explicit --Force flag

---

## MISSING SCRIPTS (Should Exist)

1. **`Setup-CLR-Prerequisites.ps1`** - Single comprehensive CLR setup
2. **`Validate-Deployment.ps1`** - Post-deployment validation suite
3. **`Rollback-Deployment.ps1`** - Emergency rollback procedures
4. **`scripts/README.md`** - Script inventory and usage guide
5. **`Build-DACPAC.ps1`** - Wrapper around msbuild with correct parameters

---

## TECHNICAL DEBT SUMMARY

| Category | Debt Items | Priority | Effort |
|----------|-----------|----------|--------|
| CLR Deployment | 8 overlapping scripts, no single source of truth | HIGH | 2 days |
| Documentation | Missing README, unclear usage patterns | HIGH | 1 day |
| Idempotency | Some scripts not safe to rerun | MEDIUM | 1 day |
| Archive/Cleanup | Migration scripts mixed with production | MEDIUM | 4 hours |
| Generated Files | In source control | HIGH | 1 hour |
| Script Consolidation | Duplicate functionality | LOW | 1 day |

**Total Estimated Effort:** 5-6 days

---

## FINAL RECOMMENDATIONS

### Keep (24 files)
- All modular deployment scripts in `/scripts/deploy/`
- Dependency analysis and validation tools
- Development utilities (dacpac-sanity-check, build warnings)
- Validation scripts (AutonomousSystemValidation, verify-temporal-tables)
- Trust-All-CLR-Assemblies.sql (consolidate others into this)
- CLR_SECURITY_ANALYSIS.md

### Archive (7 files - migration utilities)
Move to `/scripts/archive/migration-2025-11/`:
- convert-tables-to-dacpac-format.ps1
- sanitize-dacpac-tables.ps1
- split-procedures-for-dacpac.ps1
- extract-tables-from-migration.ps1
- add-go-after-create-table.ps1
- clean-sql-for-dacpac.ps1
- clean-sql-dacpac-deep.ps1

### Delete (7 files - manual/redundant)
- clr-deploy-generated.sql (GENERATED)
- deploy-autonomous-clr-functions.sql (DACPAC handles)
- deploy-autonomous-clr-functions-manual.sql (DACPAC handles)
- Deploy-Main-Assembly.sql (DACPAC handles)
- Trust-GAC-Assemblies.sql (merge into Trust-All)
- Register-All-CLR-Dependencies.sql (merge into new Setup script)
- update-clr-assembly.sql (ad-hoc, not needed)

### Create (5 files - missing essentials)
- Setup-CLR-Prerequisites.ps1
- Validate-Deployment.ps1
- Rollback-Deployment.ps1
- scripts/README.md
- Build-DACPAC.ps1

---

## CONCLUSION

The scripts folder contains valuable automation but suffers from:
1. **Fragmentation** - CLR deployment logic spread across 8+ files
2. **No single source of truth** - Multiple ways to do the same thing
3. **Mixed purpose** - One-time migration scripts alongside production deployment
4. **Documentation gaps** - Unclear which script to use when

**Priority 1:** Consolidate CLR deployment into single idempotent prerequisite setup  
**Priority 2:** Clean up generated files and archive migration scripts  
**Priority 3:** Document clear deployment pathways  
**Priority 4:** Ensure all scripts are idempotent and logged

Once consolidated, the deployment should be:
```powershell
# One-time setup
.\scripts\Setup-CLR-Prerequisites.ps1 -Server localhost

# Deploy/update database
SqlPackage /Action:Publish /SourceFile:Hartonomous.Database.dacpac /TargetServerName:localhost
```

Simple, repeatable, production-ready.
