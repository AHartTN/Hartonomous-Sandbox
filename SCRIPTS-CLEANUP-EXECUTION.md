# ?? SCRIPTS CLEANUP - EXECUTION PLAN

**Focus**: PowerShell scripts in `scripts/` only  
**Criteria**: Keep what pipelines use + essential utilities  

---

## ? KEEP (Used by azure-pipelines.yml)

| Script | Used By | Action |
|--------|---------|--------|
| `build-dacpac.ps1` | Stage 1 | ? KEEP |
| `Initialize-CLRSigning.ps1` | Stage 1 | ? KEEP |
| `Sign-CLRAssemblies.ps1` | Stage 1 | ? KEEP |
| `verify-dacpac.ps1` | Stage 1 | ? KEEP |
| `Deploy-CLRCertificate.ps1` | Stage 2 | ? KEEP |
| `grant-agent-permissions.ps1` | Stage 2 | ? KEEP |
| `deploy-clr-assemblies.ps1` | Stage 2 | ? KEEP |
| `install-sqlpackage.ps1` | Stage 2 | ? KEEP |
| `scaffold-entities.ps1` | Stage 3 | ? KEEP |

**Total: 9 scripts actively used**

---

## ? KEEP (Essential Utilities)

| Script | Purpose | Justification |
|--------|---------|---------------|
| `Deploy-Database.ps1` | Master DB deployer | Called by local dev |
| `Deploy.ps1` | Local dev orchestrator | For F5 debugging |
| `Run-CoreTests.ps1` | Quick validation | Testing utility |
| `neo4j/Deploy-Neo4jSchema.ps1` | Neo4j schema | Separate domain |
| `operations/Seed-HartonomousRepo.ps1` | Test data | Admin tool |
| `operations/Test-RLHFCycle.ps1` | RLHF testing | Admin tool |

**Total: 6 essential utilities**

---

## ?? ARCHIVE (Evaluate Later)

| Script | Why Archive | Action |
|--------|-------------|--------|
| `Deploy-All.ps1` | Duplicate of Deploy.ps1 | ?? ARCHIVE |
| `deploy-dacpac.ps1` | Small wrapper, rarely used directly | ?? ARCHIVE |
| `deploy-hartonomous.ps1` | Legacy, 35KB monster | ?? ARCHIVE |
| `Deploy-Idempotent.ps1` | Legacy alternative | ?? ARCHIVE |
| `Deploy-Master.ps1` | Legacy orchestrator | ?? ARCHIVE |
| `deploy/Deploy-AzurePipelines.ps1` | Pipeline now has explicit tasks | ?? ARCHIVE |
| `deploy/deploy-local-dev.ps1` | Duplicate of Deploy.ps1 | ?? ARCHIVE |
| `deploy/Deploy-Local.ps1` | Another duplicate | ?? ARCHIVE |
| `azure/MASTER-DEPLOY.ps1` | Azure-specific, rarely used | ?? ARCHIVE |
| `azure/01-create-infrastructure.ps1` | One-time setup | ?? ARCHIVE |
| `Audit-Legacy-Code.ps1` | One-time audit tool | ?? ARCHIVE |
| `Purge-Legacy-Code.ps1` | One-time cleanup tool | ?? ARCHIVE |
| `Test-HartonomousDeployment.ps1` | Large test script | ?? ARCHIVE |
| `Test-HartonomousDeployment-Simple.ps1` | Duplicate test | ?? ARCHIVE |
| `Test-PipelineConfiguration.ps1` | One-time validation | ?? ARCHIVE |
| `Validate-Build.ps1` | Duplicate of verify-dacpac | ?? ARCHIVE |
| `preflight-check.ps1` | Duplicate checks | ?? ARCHIVE |
| `generate-clr-wrappers.ps1` | Code gen, rarely used | ?? ARCHIVE |
| `Configure-GitHubActionsServicePrincipals.ps1` | One-time setup | ?? ARCHIVE |
| `Configure-ServiceBrokerActivation.ps1` | One-time setup | ?? ARCHIVE |
| `Grant-ArcManagedIdentityAccess.ps1` | One-time setup | ?? ARCHIVE |
| `Build-WithSigning.ps1` | Orchestrator, but pipeline does it better | ?? ARCHIVE |
| `deploy/Deploy-GitHubActions.ps1` | GitHub has explicit tasks now | ?? ARCHIVE |
| `deploy/deploy-to-hart-server.ps1` | Referenced but doesn't exist functionality | ?? ARCHIVE |

**Total: 24 scripts to archive**

---

## ??? DELETE (Already removed)

| Script | Reason |
|--------|--------|
| `deployment-summary.ps1` | Just printed text, no logic |
| `local-dev-config.ps1` | Replaced by configs |

**Total: 2 deleted**

---

## ?? FINAL STRUCTURE

```
scripts/
??? README.md                          ? Document remaining scripts
?
??? build-dacpac.ps1                   ? Pipeline Stage 1
??? Initialize-CLRSigning.ps1          ? Pipeline Stage 1
??? Sign-CLRAssemblies.ps1             ? Pipeline Stage 1
??? verify-dacpac.ps1                  ? Pipeline Stage 1
?
??? Deploy-CLRCertificate.ps1          ? Pipeline Stage 2
??? grant-agent-permissions.ps1        ? Pipeline Stage 2
??? deploy-clr-assemblies.ps1          ? Pipeline Stage 2
??? install-sqlpackage.ps1             ? Pipeline Stage 2
?
??? scaffold-entities.ps1              ? Pipeline Stage 3
?
??? Deploy-Database.ps1                ? Local dev
??? Deploy.ps1                         ? Local dev orchestrator
??? Run-CoreTests.ps1                  ? Testing
?
??? neo4j/
?   ??? Deploy-Neo4jSchema.ps1         ? Neo4j domain
?
??? operations/
?   ??? Seed-HartonomousRepo.ps1       ? Admin tool
?   ??? Test-RLHFCycle.ps1             ? Admin tool
?
??? .archive/                          ?? 24 archived scripts
    ??? Deploy-All.ps1
    ??? deploy-dacpac.ps1
    ??? deploy-hartonomous.ps1
    ??? ... (21 more)
```

**Active Scripts: 15** (down from 39)  
**Archived: 24**  
**Deleted: 2**  

---

## ?? EXECUTION

```powershell
# Move to archive
$toArchive = @(
    'Deploy-All.ps1',
    'deploy-dacpac.ps1',
    'deploy-hartonomous.ps1',
    'Deploy-Idempotent.ps1',
    'Deploy-Master.ps1',
    'Audit-Legacy-Code.ps1',
    'Purge-Legacy-Code.ps1',
    'Test-HartonomousDeployment.ps1',
    'Test-HartonomousDeployment-Simple.ps1',
    'Test-PipelineConfiguration.ps1',
    'Validate-Build.ps1',
    'preflight-check.ps1',
    'generate-clr-wrappers.ps1',
    'Configure-GitHubActionsServicePrincipals.ps1',
    'Configure-ServiceBrokerActivation.ps1',
    'Grant-ArcManagedIdentityAccess.ps1',
    'Build-WithSigning.ps1',
    'deploy\Deploy-AzurePipelines.ps1',
    'deploy\deploy-local-dev.ps1',
    'deploy\Deploy-Local.ps1',
    'deploy\Deploy-GitHubActions.ps1',
    'deploy\deploy-to-hart-server.ps1',
    'azure\MASTER-DEPLOY.ps1',
    'azure\01-create-infrastructure.ps1'
)

foreach ($script in $toArchive) {
    Move-Item "scripts\$script" "scripts\.archive\" -Force
}
```

Ready to execute?
