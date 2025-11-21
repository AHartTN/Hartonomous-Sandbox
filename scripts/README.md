# Scripts Directory

**Enterprise-grade PowerShell deployment scripts for Hartonomous**

---

## ?? Structure

```
scripts/
??? README.md                          ? This file
?
??? build-dacpac.ps1                   ? Build database DACPAC
??? verify-dacpac.ps1                  ? Validate DACPAC integrity
?
??? Initialize-CLRSigning.ps1          ? Create/manage CLR signing certificate  
??? Sign-CLRAssemblies.ps1             ? Sign CLR assemblies
??? Deploy-CLRCertificate.ps1          ? Deploy certificate to SQL Server
??? deploy-clr-assemblies.ps1          ? Deploy external CLR dependencies
?
??? Deploy-Database.ps1                ? Unified database deployment
??? Deploy.ps1                         ? Local development orchestrator
?
??? grant-agent-permissions.ps1        ? Grant SQL permissions to pipeline agents
??? install-sqlpackage.ps1             ? Install SqlPackage CLI tool
??? scaffold-entities.ps1              ? Generate EF Core entities from database
??? Run-CoreTests.ps1                  ? Quick validation tests
?
??? neo4j/
?   ??? Deploy-Neo4jSchema.ps1         ? Deploy Neo4j graph schema
?
??? operations/
?   ??? Seed-HartonomousRepo.ps1       ? Seed test data
?   ??? Test-RLHFCycle.ps1             ? Test RLHF feedback loop
?
??? .archive/                          ? Archived scripts (24 scripts)
    ??? (one-time setup, legacy, duplicates)
```

---

## ?? Usage by Scenario

### **Local Development (F5 Debugging)**

```powershell
# Full deployment to localhost
.\scripts\Deploy.ps1

# Just build and deploy database
.\scripts\build-dacpac.ps1
.\scripts\Deploy-Database.ps1 -Server localhost -Database Hartonomous

# Scaffold entities after schema changes
.\scripts\scaffold-entities.ps1 -Server localhost -Database Hartonomous
```

### **CI/CD Pipeline (Azure Pipelines)**

Scripts are automatically called by `azure-pipelines.yml`:

- **Stage 1 (Build)**: `build-dacpac.ps1`, `Initialize-CLRSigning.ps1`, `Sign-CLRAssemblies.ps1`, `verify-dacpac.ps1`
- **Stage 2 (Deploy DB)**: `Deploy-CLRCertificate.ps1`, `grant-agent-permissions.ps1`, `deploy-clr-assemblies.ps1`, `install-sqlpackage.ps1`
- **Stage 3 (Scaffold)**: `scaffold-entities.ps1`
- **Stage 5 (Deploy Apps)**: Inline Bash tasks (no scripts)

### **Testing & Validation**

```powershell
# Run quick validation tests
.\scripts\Run-CoreTests.ps1

# Test RLHF cycle
.\scripts\operations\Test-RLHFCycle.ps1
```

### **Neo4j Graph Database**

```powershell
# Deploy graph schema
.\scripts\neo4j\Deploy-Neo4jSchema.ps1 -Neo4jUri "bolt://localhost:7687"
```

---

## ?? Script Descriptions

### **Build Scripts**

#### `build-dacpac.ps1`
Builds the database DACPAC from SQL project using MSBuild.

**Called by**: Azure Pipelines Stage 1, local dev  
**Output**: `src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac`

#### `verify-dacpac.ps1`
Validates DACPAC integrity and structure.

**Called by**: Azure Pipelines Stage 1

---

### **CLR Signing Scripts**

#### `Initialize-CLRSigning.ps1`
Creates self-signed certificate for CLR assembly signing. Idempotent (safe to run multiple times).

**Called by**: Azure Pipelines Stage 1, local dev  
**Output**: Certificate in `Cert:\LocalMachine\My`, exports to `certificates/`

#### `Sign-CLRAssemblies.ps1`
Auto-discovers and signs CLR assemblies with certificate.

**Called by**: Azure Pipelines Stage 1  
**How**: Scans build output, signs unsigned DLLs with `signtool.exe`

#### `Deploy-CLRCertificate.ps1`
Deploys signing certificate to SQL Server, enables CLR Strict Security.

**Called by**: Azure Pipelines Stage 2  
**What**: Creates certificate, login, grants UNSAFE ASSEMBLY permission

#### `deploy-clr-assemblies.ps1`
Deploys external CLR dependency assemblies to SQL Server in correct dependency order.

**Called by**: Azure Pipelines Stage 2  
**Dependencies**: 16 external DLLs (MathNet.Numerics, System.Memory, etc.)

---

### **Database Deployment Scripts**

#### `Deploy-Database.ps1`
Unified database deployment: CLR + DACPAC in one command.

**Called by**: Local dev, manual deployments  
**What**: Orchestrates entire database deployment flow

#### `Deploy.ps1`
Local development orchestrator for full-stack deployment.

**Called by**: Local F5 debugging  
**What**: Deploys database, scaffolds entities, builds solution

---

### **Utility Scripts**

#### `grant-agent-permissions.ps1`
Grants SQL Server permissions to pipeline agent service accounts.

**Called by**: Azure Pipelines Stage 2  
**What**: Grants `dbowner` to Network Service or pipeline service principal

#### `install-sqlpackage.ps1`
Installs SqlPackage CLI tool if not present.

**Called by**: Azure Pipelines Stage 2  
**What**: Downloads and installs latest SqlPackage via dotnet tool

#### `scaffold-entities.ps1`
Generates EF Core entity classes from deployed database schema.

**Called by**: Azure Pipelines Stage 3, local dev  
**Output**: `src/Hartonomous.Data.Entities/*.cs` files

#### `Run-CoreTests.ps1`
Quick validation tests (database connectivity, basic queries).

**Called by**: Manual testing, validation  
**Duration**: ~10 seconds

---

### **Neo4j Scripts**

#### `neo4j/Deploy-Neo4jSchema.ps1`
Deploys Neo4j graph database schema (constraints, indexes, reference data).

**Called by**: Manual deployment, setup  
**What**: Creates 9 constraints, 13 indexes, 15 reference nodes

---

### **Operations Scripts**

#### `operations/Seed-HartonomousRepo.ps1`
Seeds test data for development and testing.

**Called by**: Manual setup, testing  
**What**: Inserts sample atoms, embeddings, relationships

#### `operations/Test-RLHFCycle.ps1`
Tests RLHF (Reinforcement Learning from Human Feedback) loop.

**Called by**: Manual testing, validation  
**What**: Simulates observe ? orient ? decide ? act cycle

---

## ??? Archived Scripts

24 scripts moved to `.archive/` directory:

- **Legacy orchestrators**: Deploy-All.ps1, deploy-hartonomous.ps1, Deploy-Idempotent.ps1, Deploy-Master.ps1
- **Duplicate scripts**: Build-WithSigning.ps1, deploy-dacpac.ps1, Deploy-Local.ps1, deploy-local-dev.ps1
- **One-time setup**: Configure-GitHubActionsServicePrincipals.ps1, Grant-ArcManagedIdentityAccess.ps1, 01-create-infrastructure.ps1
- **Testing utilities**: Test-HartonomousDeployment.ps1, Test-PipelineConfiguration.ps1, Validate-Build.ps1
- **Code generation**: generate-clr-wrappers.ps1, Audit-Legacy-Code.ps1, Purge-Legacy-Code.ps1

**Why archived**: Replaced by pipeline explicit tasks, duplicates, or one-time use only.

**Can be restored**: If needed, scripts are in `.archive/` (not deleted).

---

## ?? Quick Reference

| Task | Command |
|------|---------|
| **Build database** | `.\build-dacpac.ps1` |
| **Deploy database** | `.\Deploy-Database.ps1 -Server localhost` |
| **Scaffold entities** | `.\scaffold-entities.ps1 -Server localhost` |
| **Full local deploy** | `.\Deploy.ps1` |
| **Run tests** | `.\Run-CoreTests.ps1` |
| **Deploy Neo4j** | `.\neo4j\Deploy-Neo4jSchema.ps1` |

---

## ?? Related Documentation

- **Pipeline**: `azure-pipelines.yml` - Complete CI/CD workflow
- **Deployment**: `docs/operations/deployment.md` - Deployment guide
- **CLR Signing**: `scripts/README-CLR-SIGNING.md` - CLR signing infrastructure
- **Neo4j**: `scripts/neo4j/README.md` - Neo4j schema documentation

---

**Last Updated**: 2025-11-21  
**Status**: ? Production-Ready  
**Scripts**: 12 active, 24 archived

