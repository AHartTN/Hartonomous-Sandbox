# Hartonomous Deployment Scripts v2.0

**Enterprise-Grade, Idempotent, Observable Deployments**

---

## Quick Start

### Which Script Should I Use?

| Scenario | Script | Command |
|----------|--------|---------|
| **Local Development** | `Deploy-Local.ps1` | `.\scripts\deploy\Deploy-Local.ps1` |
| **GitHub Actions CI/CD** | `Deploy-GitHubActions.ps1` | Automatically called by workflow |
| **Azure Pipelines CI/CD** | `Deploy-AzurePipelines.ps1` | Automatically called by pipeline |

---

## Deploy-Local.ps1

### Purpose
Complete local deployment for development and testing on your workstation.

### Prerequisites
- Windows or PowerShell 7+ on macOS/Linux
- SQL Server (localhost)
- .NET 10 SDK
- SqlPackage CLI
- Optional: Neo4j (localhost:7687)

### Usage

```powershell
# Full deployment (fresh start)
.\scripts\deploy\Deploy-Local.ps1

# Skip building (use existing DACPAC)
.\scripts\deploy\Deploy-Local.ps1 -SkipBuild

# Skip scaffolding (use existing entities)
.\scripts\deploy\Deploy-Local.ps1 -SkipScaffold

# Quick redeploy (skip build and scaffold)
.\scripts\deploy\Deploy-Local.ps1 -SkipBuild -SkipScaffold

# Deploy and start services
.\scripts\deploy\Deploy-Local.ps1 -StartServices

# Force deployment without confirmation
.\scripts\deploy\Deploy-Local.ps1 -Force
```

### What It Does

1. **Pre-Flight Validation** - Checks tools, disk space, connectivity
2. **Build DACPAC** - Compiles database project with MSBuild
3. **Deploy Database** - Deploys schema, CLR assemblies, Service Broker
4. **Scaffold Entities** - Generates EF Core entities from schema
5. **Build Solution** - Compiles .NET projects
6. **Post-Deployment Validation** - Verifies deployment success
7. **Optional: Start Services** - Launches API and workers

### Configuration
Uses: `scripts/config/config.local.json`

Key settings:
- Database: `localhost`
- Authentication: Windows Integrated Security
- CLR Strict Security: Disabled (for local dev)
- TRUSTWORTHY: Enabled (for local dev)
- Monitoring: Disabled (no telemetry)

---

## Deploy-GitHubActions.ps1

### Purpose
Automated deployment from GitHub Actions workflows.

### Prerequisites
- Self-hosted GitHub Actions runner on HART-DESKTOP
- Azure CLI configured with service principal
- Secrets configured in GitHub repository

### GitHub Secrets Required

| Secret | Description | Example |
|--------|-------------|---------|
| `AZURE_CLIENT_ID` | Service principal client ID | `12345678-1234-1234-1234-123456789012` |
| `AZURE_TENANT_ID` | Azure AD tenant ID | `87654321-4321-4321-4321-210987654321` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `abcd1234-abcd-1234-abcd-1234abcd1234` |
| `SQL_SERVER` | Database server name | `HART-DESKTOP` |
| `SQL_DATABASE` | Database name | `Hartonomous` |

### Usage

Called automatically by `.github/workflows/ci-cd.yml`:

```yaml
- name: Deploy to Environment
  run: |
    .\scripts\deploy\Deploy-GitHubActions.ps1 -Environment ${{ github.event.inputs.environment }}
```

Manual invocation (from runner):
```powershell
.\scripts\deploy\Deploy-GitHubActions.ps1
# Automatically detects environment from branch name

.\scripts\deploy\Deploy-GitHubActions.ps1 -Environment Production
# Explicitly deploy to production
```

### What It Does

1. **Azure Authentication** - Authenticates with service principal
2. **Pre-Flight Validation** - Verifies connectivity and permissions
3. **Database Deployment** - Deploys to HART-DESKTOP
4. **Application Deployment** - Deploys to HART-SERVER (if enabled)
5. **Post-Deployment Validation** - Health checks and smoke tests
6. **Telemetry** - Sends deployment events to Application Insights

### Environment Detection

| Branch | Environment |
|--------|-------------|
| `main` | Production |
| `staging` | Staging |
| `develop` | Development |
| Feature branches | Development |

Override with: `-Environment Production`

### Configuration
Uses:
- `scripts/config/config.base.json` (base settings)
- `scripts/config/config.{environment}.json` (environment overrides)

---

## Deploy-AzurePipelines.ps1

### Purpose
Automated deployment from Azure DevOps Pipelines.

### Prerequisites
- Azure Pipelines agent with Azure CLI
- Service connection configured in Azure DevOps
- Pipeline variables configured

### Pipeline Variables Required

| Variable | Description | Example |
|----------|-------------|---------|
| `environment` | Target environment | `Production` |
| `sqlServer` | Database server name | `HART-DESKTOP` |
| `sqlDatabase` | Database name | `Hartonomous` |
| `azureSubscription` | Service connection name | `Hartonomous-Production` |

### Usage

Called automatically by `azure-pipelines.yml`:

```yaml
- task: PowerShell@2
  displayName: 'Deploy to $(environment)'
  inputs:
    filePath: 'scripts/deploy/Deploy-AzurePipelines.ps1'
    arguments: '-Environment $(environment)'
```

Manual invocation (from agent):
```powershell
.\scripts\deploy\Deploy-AzurePipelines.ps1
# Automatically detects environment from pipeline

.\scripts\deploy\Deploy-AzurePipelines.ps1 -Environment Production
# Explicitly deploy to production
```

### What It Does

Same as Deploy-GitHubActions.ps1, but:
- Integrates with Azure Pipelines artifacts
- Uses Azure service connections
- Sends telemetry to Azure Monitor

---

## Architecture Overview

### Module System

All deployment scripts use shared PowerShell modules:

```
scripts/modules/
├── Logger.psm1         # Structured logging with color output
├── Environment.psm1    # Auto-detect Local/Dev/Staging/Prod
├── Config.psm1         # Load and merge JSON configurations
├── Secrets.psm1        # Azure Key Vault integration
├── Validation.psm1     # Pre/post-deployment health checks
└── Monitoring.psm1     # Azure CLI + GitHub CLI integration
```

### Configuration System

Hierarchical JSON configuration with environment overrides:

```
scripts/config/
├── config.base.json         # Base settings (all environments)
├── config.local.json        # Local development overrides
├── config.development.json  # Development environment
├── config.staging.json      # Staging environment
└── config.production.json   # Production environment
```

**Configuration Merging:**
1. Load `config.base.json`
2. Merge `config.{environment}.json` overrides
3. Resolve `${KeyVault:SecretName}` references
4. Apply environment variable overrides

### Secrets Management

**Local Development:**
```json
{
  "neo4j": {
    "username": "neo4j",
    "password": "neo4jneo4j"
  }
}
```

**Production:**
```json
{
  "neo4j": {
    "username": "${KeyVault:Neo4jUsername}",
    "password": "${KeyVault:Neo4jPassword}"
  }
}
```

Secrets are automatically resolved from:
1. **Azure Key Vault** (production)
2. **Environment variables** (CI/CD)
3. **Configuration file** (local dev only)

---

## Environment Configuration

### Local Environment

**Target:**
- Database: `localhost`
- Application: `localhost:5000`

**Authentication:**
- Windows Integrated Security
- No Azure Key Vault

**Features:**
- Debug builds
- Verbose logging
- TRUSTWORTHY enabled
- CLR Strict Security disabled

### Development Environment

**Target:**
- Database: `HART-DESKTOP`
- Application: `HART-SERVER:/srv/www/hartonomous/dev`

**Authentication:**
- Azure AD authentication
- Azure Key Vault for secrets

**Features:**
- Release builds
- Application Insights telemetry
- Blue-green deployment disabled
- All workers enabled

### Staging Environment

**Target:**
- Database: `HART-DESKTOP`
- Application: `HART-SERVER:/srv/www/hartonomous/staging`

**Authentication:**
- Azure AD authentication
- Azure Key Vault for secrets

**Features:**
- Release builds
- Full telemetry and monitoring
- Blue-green deployment enabled
- Automated rollback on failure
- All workers enabled

### Production Environment

**Target:**
- Database: `HART-DESKTOP`
- Application: `HART-SERVER:/srv/www/hartonomous`

**Authentication:**
- Azure AD authentication
- Azure Key Vault with managed identity

**Features:**
- Release builds
- Full telemetry and monitoring
- Blue-green deployment enabled
- Automated rollback on failure
- Health checks before/after deployment
- Retry logic (3 attempts)

---

## Common Tasks

### Deploy to Local Development

```powershell
cd D:\Repositories\Hartonomous
.\scripts\deploy\Deploy-Local.ps1
```

### Quick Redeploy (Local)

```powershell
# Only redeploy DACPAC, skip rebuild and scaffold
.\scripts\deploy\Deploy-Local.ps1 -SkipBuild -SkipScaffold
```

### Test Deployment Configuration

```powershell
# Load and display configuration
Import-Module .\scripts\modules\Config.psm1 -Force
$config = Get-DeploymentConfig -Environment Local
Show-Configuration -Config $config
```

### Check Prerequisites

```powershell
# Verify all required tools are installed
Import-Module .\scripts\modules\Validation.psm1 -Force
Test-RequiredTools
```

### View Recent Deployments (GitHub Actions)

```powershell
Import-Module .\scripts\modules\Monitoring.psm1 -Force
Show-GitHubWorkflowStatus
```

### Test Key Vault Access

```powershell
Import-Module .\scripts\modules\Secrets.psm1 -Force
Initialize-SecretsModule -VaultUri "https://kv-hartonomous-prod.vault.azure.net/" -UseManagedIdentity
Test-KeyVaultAccess
```

---

## Troubleshooting

### Issue: "Module not found"

**Cause:** PowerShell can't find the module files.

**Solution:**
```powershell
# Verify modules exist
ls scripts\modules\*.psm1

# Import modules explicitly
Import-Module .\scripts\modules\Logger.psm1 -Force
```

### Issue: "Cannot connect to database"

**Cause:** SQL Server not running or authentication failed.

**Solution:**
```powershell
# Test connectivity
sqlcmd -S localhost -Q "SELECT @@VERSION"

# Check authentication
sqlcmd -S localhost -E -Q "SELECT SUSER_NAME()"
```

### Issue: "Azure CLI not authenticated"

**Cause:** Not logged into Azure.

**Solution:**
```powershell
# Login to Azure
az login

# Verify authentication
az account show
```

### Issue: "Key Vault access denied"

**Cause:** Insufficient permissions on Key Vault.

**Solution:**
```powershell
# Check access
az keyvault secret list --vault-name kv-hartonomous-prod

# Grant access (requires Owner/Contributor role)
az keyvault set-policy `
  --name kv-hartonomous-prod `
  --object-id <your-object-id> `
  --secret-permissions get list
```

### Issue: "DACPAC build failed"

**Cause:** MSBuild errors in database project.

**Solution:**
```powershell
# Build manually to see detailed errors
cd src\Hartonomous.Database
msbuild Hartonomous.Database.sqlproj /t:Build /v:detailed
```

---

## Migration from v1.0

### Old Scripts (Deprecated)

The following scripts are **deprecated** and will be removed in v3.0:

- `Deploy-Master.ps1` → Use `Deploy-Local.ps1`
- `Deploy-All.ps1` → Use `Deploy-Local.ps1`
- `Deploy-Idempotent.ps1` → Use `Deploy-Local.ps1`
- `deploy-hartonomous.ps1` → Use `Deploy-Local.ps1`

### Migration Steps

1. **Update CI/CD workflows** to use new scripts:
   ```yaml
   # Old
   - run: .\scripts\Deploy-Master.ps1

   # New
   - run: .\scripts\deploy\Deploy-GitHubActions.ps1
   ```

2. **Move hardcoded values to configuration**:
   ```json
   // scripts/config/config.local.json
   {
     "database": {
       "server": "localhost",  // Was hardcoded in script
       "name": "Hartonomous"
     }
   }
   ```

3. **Update secrets to use Key Vault**:
   ```json
   // scripts/config/config.production.json
   {
     "neo4j": {
       "password": "${KeyVault:Neo4jPassword}"  // Was in appsettings.json
     }
   }
   ```

---

## Best Practices

### 1. Use the Correct Entry Point

- **Local development** → `Deploy-Local.ps1`
- **GitHub Actions** → `Deploy-GitHubActions.ps1`
- **Azure Pipelines** → `Deploy-AzurePipelines.ps1`

### 2. Never Hardcode Secrets

```json
// ✗ BAD (hardcoded)
{
  "neo4j": {
    "password": "mypassword123"
  }
}

// ✓ GOOD (Key Vault reference)
{
  "neo4j": {
    "password": "${KeyVault:Neo4jPassword}"
  }
}
```

### 3. Test Locally Before CI/CD

```powershell
# Always test locally first
.\scripts\deploy\Deploy-Local.ps1 -Force

# Then commit and push
git add .
git commit -m "feat: Update deployment configuration"
git push
```

### 4. Use Dry Run for Testing

```powershell
# Simulate deployment without making changes
.\scripts\deploy\Deploy-Local.ps1 -DryRun
```

### 5. Monitor Deployments

```powershell
# Check GitHub Actions status
Import-Module .\scripts\modules\Monitoring.psm1 -Force
Show-GitHubWorkflowStatus

# Check application health
Test-DeploymentHealth -Config $config
```

---

## Support

### Documentation
- Full guide: `docs/deployment/DEPLOYMENT-GUIDE-V2.md`
- Refactoring plan: `docs/deployment/DEPLOYMENT-REFACTORING-GAMEPLAN.md`
- Idempotency audit: `docs/deployment/IDEMPOTENCY-AUDIT.md`

### Getting Help
1. Check the [Troubleshooting](#troubleshooting) section
2. Review module documentation: `Get-Help <FunctionName> -Detailed`
3. Check logs: `logs/deployment-{environment}.log`
4. Open an issue on GitHub

---

## Version History

### v2.0.0 (November 2025)
- Complete refactoring with module system
- Centralized JSON configuration
- Azure Key Vault integration
- Multi-environment support
- GitHub Actions and Azure Pipelines integration
- Comprehensive monitoring and validation

### v1.0.0 (October 2025)
- Initial deployment scripts
- Basic idempotency
- CLR assembly deployment
- EF Core scaffolding

---

**Questions? Check the documentation or ask the DevOps team!**
