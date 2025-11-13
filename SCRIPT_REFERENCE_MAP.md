# Script Reference Map - Evidence-Based Analysis
**Generated:** 2025-01-13  
**Purpose:** Map all script references to determine what can be safely deleted

## Deployment Workflows

### Production Workflow (deploy-database-unified.ps1)
**Path:** `scripts/deploy-database-unified.ps1`  
**Calls:**
- `scripts/deploy-clr-secure.ps1` (line 459)
- Uses SqlPackage for DACPAC deployment
- Uses dotnet ef for migrations
- No direct .sql file references

**Verdict:** ✅ KEEP - Master orchestrator

---

### CLR Secure Deployment (deploy-clr-secure.ps1)
**Path:** `scripts/deploy-clr-secure.ps1`  
**Called by:** `deploy-database-unified.ps1`  
**Purpose:** Multi-tier CLR assembly deployment with sys.sp_add_trusted_assembly  
**Verdict:** ✅ KEEP - Critical production script

---

### Manual CLR Prerequisites (Setup-CLR-Prerequisites.ps1)
**Path:** `scripts/Setup-CLR-Prerequisites.ps1`  
**Executes:**
1. `src/Hartonomous.Database/Scripts/Setup-CLR-Security.sql` (line 105)
2. `scripts/Trust-All-CLR-Assemblies.sql` (line 123)
3. Dynamically generated SQL for dependency registration (NOT Register-All-CLR-Dependencies.sql)

**Verdict:** ✅ KEEP - Used for manual setup

---

### VS Code Task Workflow (.vscode/tasks.json)
**Manual developer tasks:**

**"Full CLR Deployment (Clean)"** - Sequential task chain:
1. Clean DACPAC
2. Rebuild DACPAC (Release)
3. **Setup CLR Security** → `scripts/Setup-CLR-Security.sql`
4. **Trust All CLR Assemblies** → `scripts/Trust-All-CLR-Assemblies.sql`
5. **Register All CLR Dependencies** → `scripts/Register-All-CLR-Dependencies.sql`
6. **Deploy Main Assembly** → `scripts/Deploy-Main-Assembly.sql`
7. Deploy DACPAC to localhost

**"Quick Deploy (Build + Deploy Only)"**:
1. Build DACPAC (Release)
2. **Deploy Main Assembly** → `scripts/Deploy-Main-Assembly.sql`
3. Deploy DACPAC to localhost

**Other tasks:**
- "Analyze Build Warnings" → `scripts/analyze-build-warnings.ps1`
- "Extract All Warnings" → `scripts/extract-all-warnings.ps1`

**Verdict:** These tasks provide alternative manual deployment workflow

---

## SQL Script Analysis

### ✅ Setup-CLR-Security.sql
**Location:** `scripts/Setup-CLR-Security.sql`  
**Referenced by:**
- `.vscode/tasks.json` → "Setup CLR Security" task (line 88)
- ALSO EXISTS IN: `src/Hartonomous.Database/Scripts/Setup-CLR-Security.sql`

**Question:** Which one is the source of truth?  
**Action Required:** Check if they're identical, consolidate to DACPAC location

---

### ✅ Trust-All-CLR-Assemblies.sql
**Location:** `scripts/Trust-All-CLR-Assemblies.sql`  
**Referenced by:**
- `scripts/Setup-CLR-Prerequisites.ps1` (lines 123, 129)
- `.vscode/tasks.json` → "Trust All CLR Assemblies" task (line 112)

**Verdict:** ✅ KEEP - Actively used in two workflows

---

### ✅ Register-All-CLR-Dependencies.sql
**Location:** `scripts/Register-All-CLR-Dependencies.sql`  
**Referenced by:**
- `.vscode/tasks.json` → "Register All CLR Dependencies" task (line 138)
- `.vscode/tasks.json` → duplicate task (line 170)

**Note:** NOT called by Setup-CLR-Prerequisites.ps1 (generates dynamic SQL instead)

**Verdict:** ✅ KEEP - Used in VS Code manual workflow

---

### ✅ Deploy-Main-Assembly.sql
**Location:** `scripts/Deploy-Main-Assembly.sql`  
**Referenced by:**
- `.vscode/tasks.json` → "Deploy Main Assembly" task (line 164)
- `.vscode/tasks.json` → "Full CLR Deployment (Clean)" task dependency
- `.vscode/tasks.json` → "Quick Deploy" task dependency

**Verdict:** ✅ KEEP - Core manual deployment step

---

### ❓ Trust-GAC-Assemblies.sql
**Location:** `scripts/Trust-GAC-Assemblies.sql`  
**Search results:** No references found in .ps1, .yml, .json files

**Question:** Is this superseded by Trust-All-CLR-Assemblies.sql?  
**Action Required:** Verify if needed or obsolete

---

### ❓ deploy-autonomous-clr-functions.sql
**Location:** `scripts/deploy-autonomous-clr-functions.sql`  
**Size:** Unknown  
**Search results:** Need to search

**Action Required:** Grep for references

---

### ❓ deploy-autonomous-clr-functions-manual.sql
**Location:** `scripts/deploy-autonomous-clr-functions-manual.sql`  
**Search results:** Need to search

**Action Required:** Grep for references

---

### ❓ update-clr-assembly.sql
**Location:** `scripts/update-clr-assembly.sql`  
**Search results:** Need to search

**Action Required:** Grep for references

---

### 🔴 clr-deploy-generated.sql
**Location:** `scripts/clr-deploy-generated.sql`  
**Size:** 11 MB (!)  
**Type:** GENERATED FILE

**Verdict:** 🔴 DELETE + ADD TO .gitignore - This is a build artifact

---

### ❓ seed-data.sql
**Location:** `scripts/seed-data.sql`  
**Search results:** Need to search

**Comparison:** DACPAC has `Scripts/Post-Deployment/Seed_*.sql` files  
**Action Required:** Check if superseded by DACPAC post-deployment scripts

---

### ❓ AutonomousSystemValidation.sql
**Location:** `scripts/AutonomousSystemValidation.sql`  
**Search results:** Need to search

**Action Required:** Grep for references

---

### ✅ verify-temporal-tables.sql
**Location:** `scripts/verify-temporal-tables.sql`  
**Purpose:** Post-deployment validation  
**Search results:** Need to verify if referenced

**Likely verdict:** KEEP - Validation script

---

## PowerShell Script Analysis

### ✅ deploy-database-unified.ps1
**Verdict:** ✅ KEEP - Master orchestrator

---

### ✅ deploy-clr-secure.ps1
**Verdict:** ✅ KEEP - Called by unified script

---

### ✅ Setup-CLR-Prerequisites.ps1
**Verdict:** ✅ KEEP - Manual setup workflow

---

### ❓ deploy-local.ps1
**Size:** 8KB  
**Question:** Is this superseded by deploy-database-unified.ps1?  
**Action Required:** Compare functionality

---

### ✅ dacpac-sanity-check.ps1
**References:** `deploy-database-unified.ps1` at lines 162, 172  
**Verdict:** ✅ KEEP - Validation script

---

### ✅ validate-package-versions.ps1
**Size:** 13KB  
**Purpose:** Build validation  
**Verdict:** ✅ KEEP - Build tooling

---

### ✅ analyze-build-warnings.ps1
**Referenced by:** `.vscode/tasks.json` → "Analyze Build Warnings"  
**Verdict:** ✅ KEEP - Developer tooling

---

### ✅ extract-all-warnings.ps1
**Referenced by:** `.vscode/tasks.json` → "Extract All Warnings"  
**Verdict:** ✅ KEEP - Developer tooling

---

### ❓ deployment-functions.ps1
**Size:** 16KB  
**Type:** Utility library  
**Action Required:** Grep to see what imports it

---

### ❓ copy-dependencies.ps1
**Action Required:** Grep for references

---

### ❓ clean-sql-for-dacpac.ps1, clean-sql-dacpac-deep.ps1
**Action Required:** Check if migration tools (likely obsolete)

---

## DACPAC Project Scripts

### Pre-Deployment Scripts (in DACPAC)
Located in `src/Hartonomous.Database/Scripts/Pre-Deployment/`:
- Enable_AutomaticTuning.sql
- Enable_CDC.sql
- Enable_QueryStore.sql
- **Register_CLR_Assemblies.sql** ⚠️ Compare with scripts/Register-All-CLR-Dependencies.sql
- Script.PreDeployment.sql
- Setup_FILESTREAM_Filegroup.sql
- Setup_InMemory_Filegroup.sql

### Post-Deployment Scripts (in DACPAC)
Located in `src/Hartonomous.Database/Scripts/Post-Deployment/`:
- Multiple spatial index, graph, seed data scripts
- Script.PostDeployment.sql (master post-deployment script)

### Root DACPAC Scripts
- `src/Hartonomous.Database/Scripts/Setup-CLR-Security.sql` ⚠️ DUPLICATE of scripts/Setup-CLR-Security.sql

---

## Key Questions to Resolve

1. **Setup-CLR-Security.sql duplication:**
   - `scripts/Setup-CLR-Security.sql` (referenced by .vscode/tasks.json)
   - `src/Hartonomous.Database/Scripts/Setup-CLR-Security.sql` (referenced by Setup-CLR-Prerequisites.ps1)
   - Are they identical? Which is source of truth?

2. **CLR Registration scripts:**
   - `scripts/Register-All-CLR-Dependencies.sql` (used in VS Code tasks)
   - `src/Hartonomous.Database/Scripts/Pre-Deployment/Register_CLR_Assemblies.sql` (in DACPAC)
   - Are these the same? Consolidate?

3. **Seed data scripts:**
   - `scripts/seed-data.sql`
   - `src/Hartonomous.Database/Scripts/Post-Deployment/Seed_*.sql`
   - Is scripts/seed-data.sql obsolete?

4. **Manual SQL scripts not yet verified:**
   - Trust-GAC-Assemblies.sql
   - deploy-autonomous-clr-functions.sql
   - deploy-autonomous-clr-functions-manual.sql
   - update-clr-assembly.sql
   - AutonomousSystemValidation.sql

5. **PowerShell scripts not yet verified:**
   - deploy-local.ps1 (likely superseded?)
   - deployment-functions.ps1 (may be imported by other scripts)
   - copy-dependencies.ps1
   - clean-sql-for-dacpac.ps1, clean-sql-dacpac-deep.ps1

---

## Next Actions

1. ✅ Compare Setup-CLR-Security.sql in both locations
2. ✅ Compare Register-All-CLR-Dependencies.sql with DACPAC Pre-Deployment version
3. ⏳ Grep for references to unverified SQL scripts
4. ⏳ Grep for references to unverified PowerShell scripts
5. ⏳ Check deploy/SqlClrFunctions.dll - should NOT be in source control
6. ⏳ Create .gitignore entries for generated files
7. ⏳ Update SCRIPTS_CLEANUP_AUDIT.md with verified findings
