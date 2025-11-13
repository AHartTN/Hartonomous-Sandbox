# Script Cleanup Decision Matrix
**Generated:** 2025-01-13  
**Method:** Code-based verification (grep search across .ps1, .sh, .yml, .json files)  
**Principle:** Evidence only, no documentation assumptions

---

## ✅ KEEP - Production Critical

| Script | Referenced By | Purpose |
|--------|--------------|---------|
| `deploy-database-unified.ps1` | Master deployment script | Orchestrates: EF migrations → CLR → DACPAC → Service Broker |
| `deploy-clr-secure.ps1` | `deploy-database-unified.ps1:459` | Multi-tier CLR assembly deployment |
| `Setup-CLR-Prerequisites.ps1` | Manual setup workflow | Executes CLR security + trust assemblies |
| `Trust-All-CLR-Assemblies.sql` | `.vscode/tasks.json`, `Setup-CLR-Prerequisites.ps1:123,129` | Trust CLR assemblies for UNSAFE code |
| `Register-All-CLR-Dependencies.sql` | `.vscode/tasks.json:138,170` | Register dependency DLLs in SQL catalog |
| `Deploy-Main-Assembly.sql` | `.vscode/tasks.json` (Full CLR Deployment task, Quick Deploy task) | Deploy main Hartonomous.Clr assembly |

---

## ✅ KEEP - Developer Tooling

| Script | Referenced By | Purpose |
|--------|--------------|---------|
| `dacpac-sanity-check.ps1` | References `deploy-database-unified.ps1:162,172` | Deployment validation |
| `validate-package-versions.ps1` | Build tooling (13KB) | NuGet package validation |
| `analyze-build-warnings.ps1` | `.vscode/tasks.json` | Extract build warnings |
| `extract-all-warnings.ps1` | `.vscode/tasks.json` | Build diagnostics |
| `verify-temporal-tables.sql` | Post-deployment validation | Verify temporal table configuration |

---

## ⚠️ INVESTIGATE - Partially Referenced

| Script | Status | Evidence |
|--------|--------|----------|
| `deployment-functions.ps1` | Referenced by `azure-pipelines.yml:329` AND contains reference to `deploy-autonomous-clr-functions.sql:378` | May be obsolete (broken YAML), but has utility functions |
| `deploy-autonomous-clr-functions.sql` | Referenced by `deployment-functions.ps1:378` | Indirect reference through deployment-functions.ps1 |
| `deploy-local.ps1` | Self-reference only, references `seed-data.sql:193` | Likely superseded by `deploy-database-unified.ps1` |
| `seed-data.sql` | Referenced by `deploy-local.ps1:193` | Check if superseded by DACPAC `Post-Deployment/Seed_*.sql` |

**Actions needed:**
1. Compare `deployment-functions.ps1` with `deploy-database-unified.ps1` - is it obsolete?
2. Compare `seed-data.sql` with DACPAC post-deployment seed scripts
3. If `deploy-local.ps1` is obsolete, both it and `seed-data.sql` can be archived

---

## 🔴 DELETE - No References Found

| Script | Verification | Reason |
|--------|--------------|--------|
| `Trust-GAC-Assemblies.sql` | ✅ No matches in .ps1,.sh,.yml,.json | Likely superseded by Trust-All-CLR-Assemblies.sql |
| `deploy-autonomous-clr-functions-manual.sql` | ✅ No matches | Obsolete manual deployment |
| `update-clr-assembly.sql` | ✅ No matches | Obsolete |
| `AutonomousSystemValidation.sql` | ✅ No matches | Obsolete validation script |
| `copy-dependencies.ps1` | ✅ No matches | Migration tool, obsolete |
| `clr-deploy-generated.sql` | **11 MB GENERATED FILE** | Should be .gitignored |

**Also found:**
- `scripts/Setup-CLR-Security.sql` - **DOES NOT EXIST** but referenced in `.vscode/tasks.json:88`
- `08-create-procedures.ps1` - **DOES NOT EXIST** but referenced `deploy-autonomous-clr-functions.sql`

---

## 🔧 BROKEN REFERENCES - Fix Required

### .vscode/tasks.json issues:

**Task "Setup CLR Security" (line 88):**
```jsonc
"command": "sqlcmd",
"args": [
    "-i",
    "${workspaceFolder}\\scripts\\Setup-CLR-Security.sql"  // ❌ FILE DOES NOT EXIST
]
```

**Fix:** Should reference `src\Hartonomous.Database\Scripts\Setup-CLR-Security.sql` OR copy that file to `scripts/`

---

## 📂 DACPAC vs scripts/ Comparison

### Scripts that ONLY exist in DACPAC:

**Pre-Deployment Scripts:**
- `src/Hartonomous.Database/Scripts/Pre-Deployment/Register_CLR_Assemblies.sql`
  - **Compare with:** `scripts/Register-All-CLR-Dependencies.sql`
  - **Question:** Are these the same? Consolidate?

**Post-Deployment Scripts:**
- `src/Hartonomous.Database/Scripts/Post-Deployment/Seed_AgentTools.sql`
- `src/Hartonomous.Database/Scripts/Post-Deployment/Seed_TopicKeywords.sql`
- `src/Hartonomous.Database/Scripts/Post-Deployment/Seed.TopicKeywords.sql`
  - **Compare with:** `scripts/seed-data.sql`
  - **Question:** Is scripts/seed-data.sql obsolete?

### Scripts in BOTH locations:
- `src/Hartonomous.Database/Scripts/Setup-CLR-Security.sql` ✅ EXISTS
- `scripts/Setup-CLR-Security.sql` ❌ DOES NOT EXIST (but .vscode/tasks.json expects it)

---

## 🚨 deploy/ Directory Issues

**File found:** `deploy/SqlClrFunctions.dll`  
**Problem:** Build artifact in source control  
**Action:** DELETE + add to .gitignore

---

## Recommended Action Plan

### Phase 1: Fix Broken References
1. ✅ Copy `src/Hartonomous.Database/Scripts/Setup-CLR-Security.sql` → `scripts/Setup-CLR-Security.sql`
   - OR update `.vscode/tasks.json` to point to DACPAC location
2. ✅ Update .gitignore:
   ```gitignore
   # Generated files
   scripts/clr-deploy-generated.sql
   deploy/SqlClrFunctions.dll
   *.dacpac
   ```

### Phase 2: Archive Likely Obsolete
Move to `scripts/archive/legacy-deployment/`:
- `deploy-local.ps1` (after verifying deploy-database-unified.ps1 supersedes it)
- `seed-data.sql` (after comparing with DACPAC seed scripts)
- `deployment-functions.ps1` (after verifying not used by azure-pipelines.yml)
- `deploy-autonomous-clr-functions.sql` (if deployment-functions.ps1 is obsolete)

### Phase 3: Delete Confirmed Obsolete
- `Trust-GAC-Assemblies.sql`
- `deploy-autonomous-clr-functions-manual.sql`
- `update-clr-assembly.sql`
- `AutonomousSystemValidation.sql`
- `copy-dependencies.ps1`
- `clr-deploy-generated.sql` (after adding to .gitignore)

### Phase 4: Clean .vscode/tasks.json
Remove duplicate tasks (multiple "Rebuild DACPAC", etc.)

### Phase 5: Rewrite azure-pipelines.yml
- Fix malformed YAML (duplicate triggers section)
- Use DACPAC-first deployment
- Call `deploy-database-unified.ps1`
- Remove IIS deployment (use Docker/Azure App Service)

---

## Verification Checklist

Before deleting ANY file:
- [ ] Grep searched in .ps1, .sh, .yml, .yaml, .json files
- [ ] Checked .vscode/tasks.json for task references
- [ ] Checked azure-pipelines.yml
- [ ] Compared with DACPAC Pre/Post-Deployment scripts
- [ ] Verified not sourced by other PowerShell scripts (`. $PSScriptRoot\...`)

---

## Summary Statistics

**Total scripts in scripts/ folder:** 24  
**Scripts with verified references:** 10  
**Scripts with no references:** 6  
**Scripts requiring investigation:** 4  
**Scripts already archived:** 9 (migration-tools: 6, diagnostics: 3)  
**Build artifacts to .gitignore:** 2  
**Broken task references:** 1  

**Next action:** Compare deployment-functions.ps1 vs deploy-database-unified.ps1 to determine if obsolete
