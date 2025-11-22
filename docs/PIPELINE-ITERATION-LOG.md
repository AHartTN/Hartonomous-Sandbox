# ?? PIPELINE ITERATION LOG

**Session**: 2025-11-21  
**Goal**: Test GitHub Actions & Azure Pipelines end-to-end  
**Approach**: Iterative debugging based on actual errors, not guesswork  

---

## ? COMPLETED

### **1. Scripts Cleanup (69% reduction)**
- Archived 24 scripts to `.archive/`
- Kept 12 essential scripts
- Created comprehensive `scripts/README.md`
- **Result**: Clean, well-documented structure

### **2. Azure Pipelines Extended (Stage 5)**
- Added deployment jobs with explicit tasks
- Systemd service management
- Nginx reverse proxy configuration
- Health checks
- Set to manual trigger (no auto-run)

### **3. GitHub Actions Fixes**
- ? **Fix 1**: Skipped CLR signing (assemblies already signed in DACPAC)
- ? **Fix 2**: Corrected `Deploy-CLRCertificate.ps1` parameters
  - **Problem**: Workflow passed invalid `-Database` parameter
  - **Solution**: Checked actual script, removed unsupported parameter
  - **Method**: Searched MS docs, read actual script code

---

## ?? DEBUGGING APPROACH (NEW!)

### **Before (Guesswork)**:
1. Try something
2. Fails
3. Guess at fix
4. Repeat

### **After (Systematic)**:
1. Read error message carefully
2. Check what the script/tool actually accepts (read the source!)
3. Search Microsoft docs for official guidance
4. Apply correct fix
5. Test

**Key Insight**: "A parameter cannot be found that matches parameter name 'Database'"  
? **Don't guess** - go read `Deploy-CLRCertificate.ps1` and see it only has `-Server` and `-EnableStrictSecurity`!

---

## ?? CURRENT STATUS

### **GitHub Actions Pipeline**:
- **Stage 1**: ? Build DACPAC (passed)
- **Stage 2**: ?? Deploying database (last fix applied)
- **Stage 3**: ?? Waiting
- **Stage 4**: ?? Waiting
- **Stage 5**: ?? Waiting

### **Azure Pipelines**:
- **Status**: Ready, but set to manual trigger
- **Why**: Test GitHub first, then Azure
- **Trigger**: `trigger: none` in `azure-pipelines.yml`

---

## ?? NEXT STEPS

### **Immediate**: Let GitHub Pipeline Complete
```powershell
# Trigger workflow
gh workflow run "CI/CD Pipeline" --ref main -f environment=development

# Watch it run
gh run watch --exit-status
```

**Expected**: All 5 stages pass

### **After GitHub Success**: Test Azure Pipelines
```powershell
# Push to Azure DevOps remote
git push azure main

# Manually trigger in Azure DevOps UI
# (trigger is disabled in YAML)
```

### **Final Step**: Document both pipelines working

---

## ?? LESSONS LEARNED

### **1. Read the Source!**
When a parameter error occurs:
1. ? **DO**: Read the actual script/function definition
2. ? **DON'T**: Guess what parameters might work

### **2. Search Official Docs**
Microsoft Learn is authoritative for:
- PowerShell cmdlets
- Azure tools
- SQL Server features
- Pipeline syntax

### **3. Iterative Debugging Works**
- Small, targeted fixes
- Test after each change
- Learn from each failure
- Document the pattern

---

## ?? SUCCESS CRITERIA

### **GitHub Actions** ? (In Progress)
- [x] Stage 1: Build DACPAC
- [ ] Stage 2: Deploy Database
- [ ] Stage 3: Scaffold Entities
- [ ] Stage 4: Build & Test
- [ ] Stage 5: Publish Applications

### **Azure Pipelines** ?? (Next)
- [ ] Stage 1: Build DACPAC
- [ ] Stage 2: Deploy Database
- [ ] Stage 3: Scaffold Entities
- [ ] Stage 4: Build .NET
- [ ] Stage 5: Deploy to HART-SERVER

### **Documentation** ?
- [x] Scripts cleanup documented
- [x] Debugging approach documented
- [x] Pipeline architecture explained
- [ ] Full success screenshots

---

## ?? RETRY COMMAND

When ready to test again:

```powershell
cd D:\Repositories\Hartonomous

# Trigger GitHub Actions
gh workflow run "CI/CD Pipeline" --ref main -f environment=development

# Watch progress
gh run watch
```

**Monitor at**: https://github.com/AHartTN/Hartonomous-Sandbox/actions

---

**Status**: ?? IN PROGRESS - GitHub Stage 2 deploying with correct parameters
