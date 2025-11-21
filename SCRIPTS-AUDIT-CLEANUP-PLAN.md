# ?? SCRIPTS AUDIT & CLEANUP PLAN

**Objective**: Clean up scripts/, keep only what's needed, organize what remains  
**Method**: Audit usage, archive unused, refactor duplicates  

---

## ?? AUDIT RESULTS

### **Scripts ACTUALLY Used by azure-pipelines.yml** ?

| Script | Used By | Purpose | Keep? |
|--------|---------|---------|-------|
| `Initialize-CLRSigning.ps1` | Stage 1 | Create/verify CLR signing certificate | ? YES |
| `build-dacpac.ps1` | Stage 1 | Build database DACPAC | ? YES |
| `Sign-CLRAssemblies.ps1` | Stage 1 | Sign CLR DLLs | ? YES |
| `verify-dacpac.ps1` | Stage 1 | Verify DACPAC integrity | ? YES |
| `Deploy-CLRCertificate.ps1` | Stage 2 | Deploy cert to SQL Server | ? YES |
| `grant-agent-permissions.ps1` | Stage 2 | Grant pipeline agent SQL perms | ? YES |
| `enable-clr.ps1` | Stage 2 | Enable CLR in SQL Server | ? YES |
| `deploy-clr-assemblies.ps1` | Stage 2 | Deploy external CLR DLLs | ? YES |
| `install-sqlpackage.ps1` | Stage 2 | Install SqlPackage tool | ? YES |
| `set-trustworthy.ps1` | Stage 2 | Set TRUSTWORTHY ON | ? YES |
| `scaffold-entities.ps1` | Stage 3 | Scaffold EF Core entities | ? YES |

**Total Active**: 11 scripts

---

### **Modules (Support Scripts)** ?

| Module | Used By | Purpose | Keep? |
|--------|---------|---------|-------|
| `modules/Config.psm1` | Deploy scripts | Load configuration | ? YES |
| `modules/Environment.psm1` | Deploy scripts | Detect environment | ? YES |
| `modules/Logger.psm1` | Deploy scripts | Logging functions | ? YES |
| `modules/Monitoring.psm1` | Deploy scripts | Telemetry | ? YES |
| `modules/Secrets.psm1` | Deploy scripts | Key Vault access | ? YES |
| `modules/Validation.psm1` | Deploy scripts | Pre-flight checks | ? YES |

**Total Modules**: 6 (all needed by orchestration scripts)

---

### **Orchestration Scripts** ?? EVALUATE

| Script | Purpose | Used By | Status |
|--------|---------|---------|--------|
| `Deploy.ps1` | Master orchestrator | ? Local dev, manual runs | ? KEEP (local dev) |
| `deploy/Deploy-GitHubActions.ps1` | GitHub Actions orchestrator | `.github/workflows/ci-cd.yml` | ? KEEP (needs refactor) |
| `deploy/Deploy-AzurePipelines.ps1` | Azure Pipelines orchestrator | Not used yet | ?? EVALUATE |

**Decision**: 
- `Deploy.ps1` - **KEEP** (essential for local F5 debugging)
- `Deploy-GitHubActions.ps1` - **KEEP** (but refactor to call explicit tasks)
- `Deploy-AzurePipelines.ps1` - **ARCHIVE** (azure-pipelines.yml has explicit tasks now)

---

### **Utility Scripts** ?? EVALUATE

| Script | Purpose | Used? | Decision |
|--------|---------|-------|----------|
| `deployment-summary.ps1` | Print summary | ? Stub only | ??? DELETE |
| `local-dev-config.ps1` | Local dev config | ? Replaced by configs | ??? DELETE |

---

### **Neo4j Scripts** ? KEEP (separate concern)

| Script | Purpose | Keep? |
|--------|---------|-------|
| `neo4j/Deploy-Neo4jSchema.ps1` | Deploy graph schema | ? YES |
| `neo4j/schemas/CoreSchema.cypher` | Neo4j schema | ? YES |
| `neo4j/queries/ProvenanceQueries.cypher` | Example queries | ? YES |
| `neo4j/*.md` | Documentation | ? YES |

**Decision**: Keep entire `neo4j/` folder (separate domain)

---

### **Operations Scripts** ? KEEP (admin tools)

| Script | Purpose | Keep? |
|--------|---------|-------|
| `operations/Seed-HartonomousRepo.ps1` | Seed test data | ? YES |
| `operations/Test-RLHFCycle.ps1` | Test RLHF loop | ? YES |

**Decision**: Keep `operations/` (admin/testing tools)

---

### **SQL Scripts** ? KEEP (database assets)

| Script | Purpose | Keep? |
|--------|---------|-------|
| `sql/Enable-ServiceBroker-Idempotent.sql` | Enable Service Broker | ? YES |
| `sql/Create-AutonomousAgentJob.sql` | Create SQL Agent job | ? YES |
| `sql/user-suggested/*.sql` | User experiments | ? YES |
| Other SQL scripts | Database utilities | ? YES |

**Decision**: Keep entire `sql/` folder (database utilities)

---

## ?? CLEANUP ACTIONS

### **1. DELETE (No longer needed)**
```
? scripts/deployment-summary.ps1 (stub, replaced by pipeline tasks)
? scripts/local-dev-config.ps1 (replaced by config/*.json)
? scripts/deploy/Deploy-AzurePipelines.ps1 (azure-pipelines.yml has explicit tasks)
```

### **2. ARCHIVE (Not currently used, but might be useful)**
```
?? None identified - everything else is actively used
```

### **3. REFACTOR (Improve quality)**
```
?? scripts/Deploy.ps1 - Simplify, remove duplication
?? scripts/deploy/Deploy-GitHubActions.ps1 - Align with azure-pipelines.yml pattern
?? scripts/build-dacpac.ps1 - Add better error handling
```

### **4. ORGANIZE (Better structure)**
```
scripts/
??? build/                    ? NEW: Build-time scripts
?   ??? build-dacpac.ps1
?   ??? verify-dacpac.ps1
?   ??? Sign-CLRAssemblies.ps1
??? setup/                    ? NEW: One-time setup scripts
?   ??? Initialize-CLRSigning.ps1
?   ??? install-sqlpackage.ps1
??? database/                 ? NEW: Database deployment scripts
?   ??? Deploy-CLRCertificate.ps1
?   ??? enable-clr.ps1
?   ??? grant-agent-permissions.ps1
?   ??? deploy-clr-assemblies.ps1
?   ??? set-trustworthy.ps1
??? scaffold/                 ? NEW: Code generation
?   ??? scaffold-entities.ps1
??? deploy/                   ? KEEP: Orchestration
?   ??? Deploy.ps1
?   ??? Deploy-GitHubActions.ps1
??? modules/                  ? KEEP: PowerShell modules
??? neo4j/                    ? KEEP: Neo4j specific
??? operations/               ? KEEP: Admin tools
??? sql/                      ? KEEP: SQL utilities
??? README.md                 ? NEW: Explain structure
```

---

## ?? REFACTORING PRIORITIES

### **Priority 1: Clean Up Deploy.ps1** (Master orchestrator)
**Current**: 500+ lines, does everything  
**Target**: 150 lines, calls modules and specific scripts  

**Changes**:
- Extract database deployment to `database/Deploy-Database.ps1`
- Extract entity scaffolding logic
- Use modules consistently
- Remove dead code

### **Priority 2: Align Deploy-GitHubActions.ps1**
**Current**: Calls `deploy-to-hart-server.ps1` (doesn't exist)  
**Target**: Calls explicit tasks like azure-pipelines.yml  

**Changes**:
- Remove reference to missing script
- Align with pipeline pattern (explicit steps)
- Keep minimal orchestration only

### **Priority 3: Improve Individual Scripts**
- Add proper error handling
- Add validation
- Add usage examples in comments
- Consistent formatting

---

## ? FINAL STRUCTURE

```
scripts/
??? README.md                              ? Explains structure, when to use what
??? Deploy.ps1                             ? Local dev orchestrator (F5 debugging)
?
??? build/                                 ? Build-time only
?   ??? build-dacpac.ps1
?   ??? verify-dacpac.ps1
?   ??? Sign-CLRAssemblies.ps1
?
??? setup/                                 ? One-time infrastructure setup
?   ??? Initialize-CLRSigning.ps1
?   ??? install-sqlpackage.ps1
?
??? database/                              ? Database deployment
?   ??? Deploy-Database.ps1               ? NEW: Extracted from Deploy.ps1
?   ??? Deploy-CLRCertificate.ps1
?   ??? enable-clr.ps1
?   ??? grant-agent-permissions.ps1
?   ??? deploy-clr-assemblies.ps1
?   ??? set-trustworthy.ps1
?
??? scaffold/                              ? Code generation
?   ??? scaffold-entities.ps1
?
??? deploy/                                ? CI/CD orchestration
?   ??? Deploy-GitHubActions.ps1          ? GitHub Actions wrapper
?
??? modules/                               ? Reusable PowerShell modules
?   ??? Config.psm1
?   ??? Environment.psm1
?   ??? Logger.psm1
?   ??? Monitoring.psm1
?   ??? Secrets.psm1
?   ??? Validation.psm1
?
??? neo4j/                                 ? Neo4j graph database
?   ??? Deploy-Neo4jSchema.ps1
?   ??? schemas/
?   ??? queries/
?   ??? *.md
?
??? operations/                            ? Admin/testing tools
?   ??? Seed-HartonomousRepo.ps1
?   ??? Test-RLHFCycle.ps1
?
??? sql/                                   ? SQL utilities
    ??? Enable-ServiceBroker-Idempotent.sql
    ??? Create-AutonomousAgentJob.sql
    ??? user-suggested/
```

**Total Scripts**: ~30 (down from ~50+ including duplicates)  
**All have clear purpose**: Every script justifies its existence  
**Easy to navigate**: Organized by function  

---

## ?? EXECUTION PLAN

### **Step 1: Delete Obsolete** (2 minutes)
```powershell
Remove-Item scripts/deployment-summary.ps1
Remove-Item scripts/local-dev-config.ps1
Remove-Item scripts/deploy/Deploy-AzurePipelines.ps1
```

### **Step 2: Reorganize** (5 minutes)
```powershell
New-Item -ItemType Directory scripts/build
New-Item -ItemType Directory scripts/setup
New-Item -ItemType Directory scripts/database
New-Item -ItemType Directory scripts/scaffold

# Move files to new structure
Move-Item scripts/build-dacpac.ps1 scripts/build/
Move-Item scripts/verify-dacpac.ps1 scripts/build/
# ... etc
```

### **Step 3: Update Pipeline References** (10 minutes)
Update `azure-pipelines.yml` to reference new paths:
```yaml
# Before:
filePath: 'scripts/build-dacpac.ps1'

# After:
filePath: 'scripts/build/build-dacpac.ps1'
```

### **Step 4: Refactor Deploy.ps1** (15 minutes)
Extract logic, use modules, simplify

### **Step 5: Create README** (5 minutes)
Document structure and usage

---

**Ready to execute?**
