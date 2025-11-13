# Scripts Cleanup Audit & Execution Plan

**Date:** November 13, 2025  
**Purpose:** Consolidate scripts to align with DACPAC-first architecture  
**Status:** Ready for Execution

---

## Executive Summary

### Current State
- **Scripts directory:** 36 files (mix of active, legacy, obsolete)
- **Deploy directory:** 9 files (deployment artifacts)
- **Azure Pipelines:** Outdated, references removed workflows
- **Primary Issues:**
  - Duplicate/redundant scripts from migration iterations
  - Legacy EF Core migration scripts (obsolete)
  - Multiple deployment approaches (needs consolidation)
  - Azure pipeline references IIS deployment (not used)

### Target State
- **Single source of truth:** DACPAC (`src/Hartonomous.Database/`)
- **Single deployment script:** `deploy-database-unified.ps1` (enhanced)
- **CLR deployment:** `deploy-clr-secure.ps1` (production-ready)
- **Clean Azure pipeline:** DACPAC build → Deploy → Test
- **All scripts:** Idempotent, documented, tested

---

## Script Inventory & Categorization

### ✅ **KEEP - Essential Scripts**

#### Core Deployment
1. **`scripts/deploy-database-unified.ps1`** ✅
   - **Purpose:** Master deployment orchestrator
   - **Status:** KEEP - Primary deployment script
   - **Action:** Enhance with better error handling, add validation
   - **Dependencies:** Calls deploy-clr-secure.ps1, uses dotnet ef, sqlpackage

2. **`scripts/deploy-clr-secure.ps1`** ✅
   - **Purpose:** Production CLR assembly deployment with strict security
   - **Status:** KEEP - Critical for CLR deployment
   - **Action:** No changes needed (already production-ready)
   - **Features:** Trusted assemblies, dependency ordering, idempotent

3. **`scripts/deployment-functions.ps1`** ✅
   - **Purpose:** Reusable PowerShell functions for SQL operations
   - **Status:** KEEP - Utility library
   - **Action:** Review if still used by other scripts
   - **Functions:** Test-SqlConnection, Invoke-SqlQuery, etc.

#### Database Validation
4. **`scripts/dacpac-sanity-check.ps1`** ✅
   - **Purpose:** Validates DACPAC build and structure
   - **Status:** KEEP - Pre-deployment validation
   - **Action:** Integrate into CI/CD pipeline

5. **`scripts/verify-temporal-tables.sql`** ✅
   - **Purpose:** Validates temporal table configuration
   - **Status:** KEEP - Post-deployment validation
   - **Action:** None needed

#### System Configuration
6. **`scripts/Setup-CLR-Prerequisites.ps1`** ✅
   - **Purpose:** Configures SQL Server for CLR (sp_configure)
   - **Status:** KEEP - Server setup utility
   - **Action:** Document that this is one-time server setup

7. **`scripts/validate-package-versions.ps1`** ✅
   - **Purpose:** Ensures NuGet package consistency
   - **Status:** KEEP - Build validation
   - **Action:** Run in CI/CD before DACPAC build

### ⚠️ **REVIEW - Potentially Obsolete**

#### Legacy Table Migration Scripts
8. **`scripts/convert-tables-to-dacpac-format.ps1`** ⚠️
   - **Purpose:** Converted EF migrations to DACPAC
   - **Status:** MIGRATION COMPLETED - Archive or delete
   - **Action:** Move to `scripts/archive/migration-tools/`

9. **`scripts/extract-tables-from-migration.ps1`** ⚠️
   - **Purpose:** Extracted table definitions from EF migrations
   - **Status:** MIGRATION COMPLETED - Archive or delete
   - **Action:** Move to `scripts/archive/migration-tools/`

10. **`scripts/generate-table-scripts-from-efcore.ps1`** ⚠️
    - **Purpose:** Generated SQL from EF Core models
    - **Status:** MIGRATION COMPLETED - Archive or delete
    - **Action:** Move to `scripts/archive/migration-tools/`

11. **`scripts/sanitize-dacpac-tables.ps1`** ⚠️
    - **Purpose:** Cleaned up table scripts during migration
    - **Status:** MIGRATION COMPLETED - Archive or delete
    - **Action:** Move to `scripts/archive/migration-tools/`

#### Legacy Procedure Scripts
12. **`scripts/split-procedures-for-dacpac.ps1`** ⚠️
    - **Purpose:** Split procedure files during migration
    - **Status:** MIGRATION COMPLETED - Archive or delete
    - **Action:** Move to `scripts/archive/migration-tools/`

13. **`scripts/add-go-after-create-table.ps1`** ⚠️
    - **Purpose:** Added GO batch separators during migration
    - **Status:** MIGRATION COMPLETED - Archive or delete
    - **Action:** Move to `scripts/archive/migration-tools/`

#### Analysis/Diagnostic Scripts
14. **`scripts/analyze-all-dependencies.ps1`** ⚠️
    - **Purpose:** Analyzed assembly dependencies
    - **Status:** COMPLETED - Archive
    - **Action:** Move to `scripts/archive/diagnostics/`

15. **`scripts/analyze-dependencies.ps1`** ⚠️
    - **Purpose:** Analyzed project dependencies
    - **Status:** COMPLETED - Archive
    - **Action:** Move to `scripts/archive/diagnostics/`

16. **`scripts/analyze-build-warnings.ps1`** ⚠️
    - **Purpose:** Analyzed DACPAC build warnings
    - **Status:** KEEP if still useful for debugging
    - **Action:** Keep or move to `scripts/diagnostics/`

17. **`scripts/extract-all-warnings.ps1`** ⚠️
    - **Purpose:** Extracted warnings from build output
    - **Status:** KEEP if still useful for debugging
    - **Action:** Keep or move to `scripts/diagnostics/`

18. **`scripts/map-all-dependencies.ps1`** ⚠️
    - **Purpose:** Mapped all project dependencies
    - **Status:** COMPLETED - Archive
    - **Action:** Move to `scripts/archive/diagnostics/`

### 🔴 **DELETE - Obsolete/Redundant**

#### Duplicate Deployment Scripts
19. **`scripts/deploy-local.ps1`** 🔴
    - **Status:** OBSOLETE - Superseded by deploy-database-unified.ps1
    - **Action:** DELETE
    - **Reason:** Older version, functionality merged into unified script

20. **`scripts/deploy/deploy-autonomous-clr-functions.sql`** 🔴
    - **Status:** OBSOLETE - Part of DACPAC now
    - **Action:** DELETE
    - **Reason:** CLR functions are in DACPAC

21. **`scripts/deploy-autonomous-clr-functions-manual.sql`** 🔴
    - **Status:** OBSOLETE - Part of DACPAC now
    - **Action:** DELETE
    - **Reason:** Manual deployment not needed

#### Legacy CLR Scripts
22. **`scripts/clr-deploy-generated.sql`** 🔴
    - **Status:** OBSOLETE - deploy-clr-secure.ps1 handles this
    - **Action:** DELETE
    - **Reason:** Replaced by PowerShell script

23. **`scripts/update-clr-assembly.sql`** 🔴
    - **Status:** OBSOLETE - Part of deploy-clr-secure.ps1
    - **Action:** DELETE
    - **Reason:** Handled by PowerShell deployment

24. **`scripts/Deploy-Main-Assembly.sql`** 🔴
    - **Status:** OBSOLETE - Part of deploy-clr-secure.ps1
    - **Action:** DELETE
    - **Reason:** Handled by PowerShell deployment

25. **`scripts/Register-All-CLR-Dependencies.sql`** 🔴
    - **Status:** OBSOLETE - Part of deploy-clr-secure.ps1
    - **Action:** DELETE
    - **Reason:** Dependency registration automated

26. **`scripts/Trust-All-CLR-Assemblies.sql`** 🔴
    - **Status:** OBSOLETE - Part of deploy-clr-secure.ps1
    - **Action:** DELETE
    - **Reason:** Trust registration automated

27. **`scripts/Trust-GAC-Assemblies.sql`** 🔴
    - **Status:** OBSOLETE - Part of deploy-clr-secure.ps1
    - **Action:** DELETE
    - **Reason:** GAC assemblies handled by secure script

#### Legacy Cleanup Scripts
28. **`scripts/clean-sql-for-dacpac.ps1`** 🔴
    - **Status:** OBSOLETE - Migration complete
    - **Action:** DELETE
    - **Reason:** DACPAC migration finished

29. **`scripts/clean-sql-dacpac-deep.ps1`** 🔴
    - **Status:** OBSOLETE - Migration complete
    - **Action:** DELETE
    - **Reason:** DACPAC migration finished

30. **`scripts/copy-dependencies.ps1`** 🔴
    - **Status:** OBSOLETE - Handled by deploy-clr-secure.ps1
    - **Action:** DELETE
    - **Reason:** Dependency management automated

#### Test/Seed Data
31. **`scripts/seed-data.sql`** ⚠️
    - **Status:** REVIEW - Is this used?
    - **Action:** If used, move to `Scripts/Post-Deployment/` in DACPAC
    - **Reason:** Should be part of post-deployment scripts

32. **`scripts/AutonomousSystemValidation.sql`** ⚠️
    - **Status:** REVIEW - Is this test data or validation?
    - **Action:** If validation, keep; if test data, move to tests/
    - **Reason:** Unclear purpose

### Documentation Files (Keep)
33. **`scripts/CLR_SECURITY_ANALYSIS.md`** ✅
    - **Status:** KEEP - Important documentation
    - **Action:** Move to `docs/`

34. **`scripts/EfCoreSchemaExtractor/`** ⚠️
    - **Status:** REVIEW - Is this still used?
    - **Action:** If used for EF Core scaffolding, keep; otherwise archive

---

## Deploy Directory Audit

### ✅ **KEEP - Essential**

1. **`deploy/deploy-local-dev.ps1`** ✅
   - **Purpose:** Local dev quick deployment
   - **Status:** KEEP - Simplifies dev workflow
   - **Action:** Update to use deploy-database-unified.ps1

2. **`deploy/deploy-to-hart-server.ps1`** ⚠️
   - **Purpose:** Production deployment script
   - **Status:** REVIEW - Is this still used?
   - **Action:** Ensure it calls deploy-database-unified.ps1

3. **`deploy/setup-hart-server.sh`** ✅
   - **Purpose:** Server provisioning (Ubuntu)
   - **Status:** KEEP - Server setup
   - **Action:** None needed

4. **`deploy/sqlservr.exe.config`** ✅
   - **Purpose:** SQL Server binding redirects configuration
   - **Status:** KEEP - Required for CLR assemblies
   - **Action:** Document in deployment guide

### Systemd Service Files (Keep)
5. **`deploy/hartonomous-api.service`** ✅
6. **`deploy/hartonomous-ces-consumer.service`** ✅
7. **`deploy/hartonomous-model-ingestion.service`** ✅
8. **`deploy/hartonomous-neo4j-sync.service`** ✅
   - **Status:** KEEP ALL - Linux service deployment
   - **Action:** None needed

### 🔴 **DELETE - Build Artifacts**
9. **`deploy/SqlClrFunctions.dll`** 🔴
   - **Status:** BUILD ARTIFACT - Should not be in source control
   - **Action:** DELETE, add to .gitignore
   - **Reason:** Binary files should not be committed

---

## Azure Pipelines Cleanup

### Current Issues in `azure-pipelines.yml`

1. **Malformed YAML** 🔴
   - Duplicate trigger blocks, broken structure
   - **Action:** Complete rewrite

2. **IIS Deployment** 🔴
   - References IIS web deployment (not used)
   - **Action:** Remove IIS tasks

3. **Windows Service Deployment** 🔴
   - Complex Windows service creation (not primary deployment)
   - **Action:** Simplify or move to separate pipeline

4. **Outdated Database Deployment** 🔴
   - References old deployment scripts
   - **Action:** Use deploy-database-unified.ps1

5. **Missing DACPAC Build** 🔴
   - Doesn't build DACPAC properly
   - **Action:** Add DACPAC build stage

### Target Pipeline Structure

```yaml
trigger:
  branches:
    include:
      - main
  paths:
    exclude:
      - docs/**
      - README.md

stages:
  - stage: Build
    jobs:
      - job: BuildDACPAC
        # Build DACPAC from src/Hartonomous.Database/
      - job: BuildServices
        # Build .NET services

  - stage: Test
    jobs:
      - job: ValidateDACPAC
        # Run dacpac-sanity-check.ps1
      - job: UnitTests
        # Run .NET tests

  - stage: DeployToArc
    jobs:
      - job: DeployDatabase
        # Call deploy-database-unified.ps1 for Arc SQL Server
      - job: DeployServices
        # Deploy services to Hart Server

  - stage: Verify
    jobs:
      - job: HealthCheck
        # Run post-deployment verification
```

---

## Execution Plan

### Phase 1: Archive Legacy Scripts (1 hour)

```powershell
# Create archive directories
New-Item -ItemType Directory -Force -Path "scripts/archive/migration-tools"
New-Item -ItemType Directory -Force -Path "scripts/archive/diagnostics"
New-Item -ItemType Directory -Force -Path "scripts/archive/legacy-deployment"

# Move migration scripts
Move-Item "scripts/convert-tables-to-dacpac-format.ps1" "scripts/archive/migration-tools/"
Move-Item "scripts/extract-tables-from-migration.ps1" "scripts/archive/migration-tools/"
Move-Item "scripts/generate-table-scripts-from-efcore.ps1" "scripts/archive/migration-tools/"
Move-Item "scripts/sanitize-dacpac-tables.ps1" "scripts/archive/migration-tools/"
Move-Item "scripts/split-procedures-for-dacpac.ps1" "scripts/archive/migration-tools/"
Move-Item "scripts/add-go-after-create-table.ps1" "scripts/archive/migration-tools/"

# Move diagnostic scripts
Move-Item "scripts/analyze-all-dependencies.ps1" "scripts/archive/diagnostics/"
Move-Item "scripts/analyze-dependencies.ps1" "scripts/archive/diagnostics/"
Move-Item "scripts/map-all-dependencies.ps1" "scripts/archive/diagnostics/"
```

### Phase 2: Delete Obsolete Scripts (30 minutes)

```powershell
# Delete obsolete deployment scripts
Remove-Item "scripts/deploy-local.ps1" -Force
Remove-Item "scripts/deploy-autonomous-clr-functions.sql" -Force
Remove-Item "scripts/deploy-autonomous-clr-functions-manual.sql" -Force

# Delete obsolete CLR scripts
Remove-Item "scripts/clr-deploy-generated.sql" -Force
Remove-Item "scripts/update-clr-assembly.sql" -Force
Remove-Item "scripts/Deploy-Main-Assembly.sql" -Force
Remove-Item "scripts/Register-All-CLR-Dependencies.sql" -Force
Remove-Item "scripts/Trust-All-CLR-Assemblies.sql" -Force
Remove-Item "scripts/Trust-GAC-Assemblies.sql" -Force

# Delete obsolete cleanup scripts
Remove-Item "scripts/clean-sql-for-dacpac.ps1" -Force
Remove-Item "scripts/clean-sql-dacpac-deep.ps1" -Force
Remove-Item "scripts/copy-dependencies.ps1" -Force

# Delete build artifacts from deploy/
Remove-Item "deploy/SqlClrFunctions.dll" -Force
```

### Phase 3: Move Documentation (15 minutes)

```powershell
# Move security analysis to docs
Move-Item "scripts/CLR_SECURITY_ANALYSIS.md" "docs/"

# Update internal references in documentation
# (Manual step - update links in other docs)
```

### Phase 4: Update deploy-database-unified.ps1 (2 hours)

**Enhancements needed:**
1. Add comprehensive error handling with rollback
2. Add validation checkpoints
3. Add dry-run mode improvements
4. Add progress reporting
5. Add deployment summary report
6. Ensure all operations are idempotent

### Phase 5: Rewrite azure-pipelines.yml (3 hours)

**Create new pipeline:**
1. Clean YAML structure
2. DACPAC build + validation
3. Arc SQL deployment using deploy-database-unified.ps1
4. Service deployment to Hart Server
5. Health checks and verification
6. Artifact publishing

### Phase 6: Update Documentation (1 hour)

**Update files:**
1. `README.md` - Update deployment instructions
2. `docs/DEPLOYMENT.md` - Reference new scripts only
3. `docs/CLR_GUIDE.md` - Reference deploy-clr-secure.ps1
4. Add `docs/SCRIPTS_REFERENCE.md` - Document all kept scripts

### Phase 7: Testing (2 hours)

1. Test deploy-database-unified.ps1 on local SQL Server
2. Test deploy-database-unified.ps1 with -DryRun
3. Test full deployment to test database
4. Verify all DACPAC objects deployed correctly
5. Verify CLR functions operational
6. Verify Service Broker active

### Phase 8: Finalize (1 hour)

1. Update .gitignore to exclude build artifacts
2. Commit changes with detailed message
3. Update issue/PR with completion status

**Total Estimated Time:** 10-12 hours

---

## Final Scripts Structure

```
scripts/
├── README.md                          # Scripts documentation
├── deploy-database-unified.ps1        # ✅ Master deployment orchestrator
├── deploy-clr-secure.ps1              # ✅ CLR deployment with strict security
├── deployment-functions.ps1           # ✅ Reusable PowerShell utilities
├── dacpac-sanity-check.ps1            # ✅ Pre-deployment validation
├── Setup-CLR-Prerequisites.ps1        # ✅ One-time server setup
├── validate-package-versions.ps1      # ✅ Build-time validation
├── verify-temporal-tables.sql         # ✅ Post-deployment validation
├── analyze-build-warnings.ps1         # ✅ Diagnostic utility
├── extract-all-warnings.ps1           # ✅ Diagnostic utility
├── seed-data.sql                      # ⚠️ Review - Move to DACPAC?
├── AutonomousSystemValidation.sql     # ⚠️ Review - Test or validation?
├── EfCoreSchemaExtractor/             # ⚠️ Review - Still used?
└── archive/
    ├── migration-tools/
    │   ├── convert-tables-to-dacpac-format.ps1
    │   ├── extract-tables-from-migration.ps1
    │   ├── generate-table-scripts-from-efcore.ps1
    │   ├── sanitize-dacpac-tables.ps1
    │   ├── split-procedures-for-dacpac.ps1
    │   └── add-go-after-create-table.ps1
    └── diagnostics/
        ├── analyze-all-dependencies.ps1
        ├── analyze-dependencies.ps1
        └── map-all-dependencies.ps1

deploy/
├── deploy-local-dev.ps1               # ✅ Local dev quick deploy
├── deploy-to-hart-server.ps1          # ⚠️ Review - Still used?
├── setup-hart-server.sh               # ✅ Server provisioning
├── sqlservr.exe.config                # ✅ SQL Server config
├── hartonomous-api.service            # ✅ Systemd services
├── hartonomous-ces-consumer.service   # ✅
├── hartonomous-model-ingestion.service # ✅
└── hartonomous-neo4j-sync.service     # ✅
```

---

## Success Criteria

- [ ] All obsolete scripts deleted
- [ ] All legacy scripts archived with README
- [ ] `deploy-database-unified.ps1` enhanced and tested
- [ ] `azure-pipelines.yml` rewritten and validated
- [ ] Documentation updated to reference only current scripts
- [ ] All deployment scripts are idempotent
- [ ] Full deployment test passed on test database
- [ ] All build artifacts excluded from source control
- [ ] Scripts directory has clear organization
- [ ] Each script has clear purpose documentation

---

## Risk Mitigation

1. **Create git branch before changes**
   ```powershell
   git checkout -b cleanup/consolidate-scripts
   ```

2. **Archive before delete**
   - All deletions first moved to `archive/`
   - Can be restored if needed

3. **Test before commit**
   - Run full deployment on test database
   - Verify all objects created correctly

4. **Incremental commits**
   - Commit after each phase
   - Easy rollback if issues found

---

## Next Steps

1. **Review this audit** - Confirm categorization decisions
2. **Execute Phase 1** - Archive legacy scripts
3. **Execute Phase 2** - Delete obsolete scripts  
4. **Execute Phases 3-8** - Enhancements and testing
5. **Final validation** - Complete deployment test

**Ready to proceed?**
