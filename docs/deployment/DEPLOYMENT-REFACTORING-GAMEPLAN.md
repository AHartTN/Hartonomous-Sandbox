# Hartonomous Deployment Infrastructure Refactoring Gameplan

**Date**: January 21, 2025
**Status**: Planning Phase
**Goal**: Enterprise-grade, idempotent, multi-environment deployment infrastructure

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Issues and Pain Points](#issues-and-pain-points)
4. [Refactoring Goals](#refactoring-goals)
5. [Target Architecture](#target-architecture)
6. [Environment Strategy](#environment-strategy)
7. [Implementation Roadmap](#implementation-roadmap)
8. [Success Criteria](#success-criteria)
9. [Monitoring and Validation](#monitoring-and-validation)

---

## Executive Summary

### Current Deployment Landscape

**Strengths:**
- Scripts are already idempotent (can run multiple times safely)
- CI/CD pipelines exist for both GitHub Actions and Azure Pipelines
- Database deployment works via DACPAC with proper CLR assembly handling
- Multi-target infrastructure: Database (HART-DESKTOP), App (HART-SERVER)

**Challenges:**
- **Too many entry points**: 5+ different deployment scripts with overlapping functionality
- **Inconsistent patterns**: Different scripts use different approaches
- **Environment configuration**: Hardcoded values, no centralized config
- **Limited monitoring**: No integrated health checks or deployment validation
- **Documentation gaps**: Unclear which script to use when
- **Mixed authentication**: Windows Auth vs Azure AD handling is inconsistent

### Refactoring Scope

This refactoring will consolidate, standardize, and enhance the deployment infrastructure to create:

1. **Single unified deployment entry point** per pipeline (GitHub Actions, Azure Pipelines, Local)
2. **Environment-agnostic scripts** that work across all deployment contexts
3. **Centralized configuration management** with environment-specific overrides
4. **Integrated monitoring** using Azure CLI and GitHub CLI
5. **Zero errors, zero warnings** deployment experience
6. **Enterprise-grade patterns** following Microsoft best practices

---

## Current State Analysis

### Deployment Scripts Inventory

#### Master/Orchestrator Scripts (5 scripts - TOO MANY!)

| Script | Purpose | Issues |
|--------|---------|--------|
| **Deploy-Master.ps1** | Single entry point, calls deploy-hartonomous.ps1 | Thin wrapper, adds little value |
| **Deploy-All.ps1** | Complete deployment with signing | Different from Deploy-Master |
| **Deploy-Idempotent.ps1** | Idempotent deployment with environment param | Overlaps with others |
| **deploy-hartonomous.ps1** | Unified deployment (9 steps) | Most comprehensive, should be THE script |
| **Deploy-Database.ps1** | Database-only deployment for CI/CD | Good for modular use |

#### Supporting Scripts (Well-organized)

| Script | Purpose | Status |
|--------|---------|--------|
| **deploy-dacpac.ps1** | DACPAC deployment only | Good, modular |
| **build-dacpac.ps1** | Build database project with MSBuild | Good, modular |
| **scaffold-entities.ps1** | EF Core entity generation | Good, idempotent |
| **Initialize-CLRSigning.ps1** | Create/verify CLR signing cert | Good, idempotent |
| **Sign-CLRAssemblies.ps1** | Sign CLR DLLs | Good, idempotent |
| **Deploy-CLRCertificate.ps1** | Deploy cert to SQL Server | Good, idempotent |

#### Application Deployment

| Script | Purpose | Status |
|--------|---------|--------|
| **deploy-to-hart-server.ps1** | Deploy app layer to Linux server via SSH | Good, but hardcoded values |
| **setup-hart-server.sh** | Linux server setup | Needs review |
| **.service files** | Systemd service definitions | Good, but need templating |

### Current Deployment Flows

#### Flow 1: Local Development (Developer Workstation)

```powershell
# Current:
.\scripts\Deploy-Master.ps1 -Server localhost -Database Hartonomous
  └─> .\scripts\deploy-hartonomous.ps1
      ├─> Pre-flight checks
      ├─> Build DACPAC with MSBuild
      ├─> Configure CLR
      ├─> Deploy DACPAC
      ├─> Deploy external CLR dependencies
      ├─> Scaffold EF Core entities
      ├─> Build .NET solution
      ├─> Deploy stored procedures
      └─> Validation
```

#### Flow 2: GitHub Actions CI/CD

```yaml
# Current:
build-dacpac:
  - Initialize CLR Signing Certificate
  - Build DACPAC with MSBuild
  - Sign CLR Assemblies
  - Upload artifacts

deploy-database:
  - Download artifacts
  - Azure Login
  - Deploy CLR Certificate to SQL Server
  - Install SqlPackage
  - Get Azure AD Access Token
  - Execute Deploy-Database.ps1
  - Verify CLR Assembly Signatures

scaffold-entities:
  - Scaffold EF Core Entities
  - Upload scaffolded entities

build-and-test:
  - Build .NET solution
  - Run tests

build-applications:
  - Publish Hartonomous.Api
  - Upload artifacts
```

#### Flow 3: Azure Pipelines

```yaml
# Current:
BuildDatabase:
  - Install .NET 10 SDK
  - Initialize CLR Signing
  - Build SQL Database Project with MSBuild
  - Sign CLR Assemblies
  - Verify DACPAC
  - Copy dependencies and scripts
  - Publish artifacts

DeployDatabase:
  - Deploy CLR Signing Certificate
  - Grant Agent Permissions
  - Enable CLR Integration
  - Deploy External CLR Assemblies
  - Deploy DACPAC
  - Verify CLR Signatures
  - Set TRUSTWORTHY ON

ScaffoldEntities:
  - Scaffold EF Core entities
  - Publish entities artifact

BuildDotNet:
  - Build .NET solution
  - Run unit tests
  - Publish applications
  - Publish artifacts
```

### Current Configuration Management

**Local Development:**
```powershell
# scripts/local-dev-config.ps1
$LocalDevConfig = @{
    SqlServer = "localhost"
    Database = "Hartonomous"
    UseWindowsAuth = $true
    BuildConfiguration = "Debug"
}
```

**Problems:**
- No environment-specific config files
- Hardcoded server names in scripts
- No secrets management
- No Azure Key Vault integration for production

### Target Infrastructure

| Environment | Database Target | App Target | Purpose |
|-------------|----------------|------------|---------|
| **Local** | localhost | localhost:5000 | Development/testing |
| **Development** | HART-DESKTOP | HART-SERVER:dev | Dev environment testing |
| **Staging** | HART-DESKTOP | HART-SERVER:staging | Pre-production validation |
| **Production** | HART-DESKTOP | HART-SERVER:/srv/www/ | Production workloads |

**Key Infrastructure:**
- **HART-DESKTOP**: Windows Server 2022, SQL Server 2022, Neo4j, Azure Arc-enabled
- **HART-SERVER**: Linux server, /srv/www/ deployment directory, systemd services

---

## Issues and Pain Points

### 1. Too Many Entry Points

**Problem:** 5 different "master" deployment scripts with overlapping functionality creates confusion:
- Which script should I use?
- Do they all do the same thing?
- Can I mix and match?

**Impact:**
- Developer confusion
- Maintenance burden (fix bugs in multiple places)
- Inconsistent behavior across scripts

**Solution:** Consolidate to 3 clear entry points:
1. `Deploy-Local.ps1` - For local development
2. `Deploy-GitHubActions.ps1` - For GitHub Actions pipeline
3. `Deploy-AzurePipelines.ps1` - For Azure DevOps pipeline

### 2. Hardcoded Configuration

**Problem:** Server names, database names, paths hardcoded in scripts

**Examples:**
```powershell
# In deploy-to-hart-server.ps1:
$Server = "deploy@localhost"  # Hardcoded!
$DeployRoot = "/srv/www/hartonomous"  # Hardcoded!

# In various scripts:
$Server = "localhost"
$Database = "Hartonomous"
```

**Impact:**
- Can't easily deploy to different environments
- Manual script editing required for each deployment
- Risk of deploying to wrong environment

**Solution:** Centralized configuration with environment overrides

### 3. Inconsistent Environment Detection

**Problem:** Each script detects environment differently

**Examples:**
```powershell
# Some scripts check:
if ($env:GITHUB_WORKSPACE) { # GitHub Actions }

# Others check:
if ($env:BUILD_BUILDID) { # Azure Pipelines }

# Others don't check at all
```

**Impact:**
- Scripts don't automatically adapt to environment
- Need manual parameters for each environment

**Solution:** Standardized environment detection module

### 4. No Secrets Management

**Problem:** Passwords and connection strings in plain text or environment variables

**Current:**
```json
// appsettings.json
"Neo4j": {
  "Username": "neo4j",
  "Password": "neo4jneo4j"  // Plain text!
}
```

**Impact:**
- Security risk
- Can't commit production configs to git
- Manual secret rotation required

**Solution:** Azure Key Vault integration for all environments

### 5. Limited Monitoring and Validation

**Problem:** Deployments succeed/fail without detailed health checks

**Current:**
- Basic validation queries at end of deployment
- No post-deployment smoke tests
- No integration with Azure Monitor or Application Insights
- No automated rollback on failure

**Impact:**
- Silent failures not detected
- No visibility into deployment health
- Manual verification required

**Solution:** Comprehensive monitoring with Azure CLI/GitHub CLI integration

### 6. App Layer Deployment Gaps

**Problem:** App deployment to HART-SERVER is manual/incomplete

**Current:**
```powershell
# deploy-to-hart-server.ps1 does basic SCP copy
scp -r publish/api/* ${server}:${deployRoot}/api/
```

**Issues:**
- No health checks before/after deployment
- No zero-downtime deployment
- No rollback capability
- Service restart is simplistic

**Solution:** Blue-green deployment with health checks

---

## Refactoring Goals

### Primary Goals

1. **Single Source of Truth**: One canonical deployment flow per pipeline
2. **Idempotency**: All scripts can be run multiple times safely (already mostly achieved)
3. **Environment Agnostic**: Same scripts work on any runner/agent
4. **Zero Errors/Warnings**: Clean builds and deployments every time
5. **Enterprise-Grade**: Follow Microsoft/Azure best practices
6. **Observable**: Full monitoring and health checks
7. **Automated**: No manual intervention required

### Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Deployment scripts | 5 overlapping scripts | 3 clear entry points |
| Configuration files | 1 (local only) | 5 (base + 4 environments) |
| Build warnings | Variable | 0 |
| Deployment errors | Occasional | 0 |
| Secrets in code | Some | 0 (all in Key Vault) |
| Health checks | Basic | Comprehensive |
| Rollback capability | Manual | Automated |
| Deployment time (database) | ~5 min | <3 min |
| Deployment time (app) | ~2 min | <1 min with blue-green |

---

## Target Architecture

### Deployment Entry Points

```
┌─────────────────────────────────────────────────────────────────┐
│                  Hartonomous Deployment Architecture            │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  Deploy-Local    │  │ GitHub Actions   │  │ Azure Pipelines  │
│  .ps1            │  │ Workflow         │  │ Pipeline         │
└────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘
         │                     │                     │
         └──────────────┬──────┴──────────────┬──────┘
                        │                     │
                  ┌─────▼─────┐         ┌─────▼─────┐
                  │ Database  │         │  App      │
                  │ Deploy    │         │  Deploy   │
                  │ Module    │         │  Module   │
                  └─────┬─────┘         └─────┬─────┘
                        │                     │
              ┌─────────┴──────────┐          │
              │                    │          │
         ┌────▼────┐         ┌────▼────┐     │
         │ Build   │         │ Deploy  │     │
         │ DACPAC  │         │ DACPAC  │     │
         └─────────┘         └─────────┘     │
                                   │          │
                             ┌─────▼──────────▼─────┐
                             │  Shared Modules:     │
                             │  - Config            │
                             │  - Environment       │
                             │  - Validation        │
                             │  - Monitoring        │
                             │  - Secrets           │
                             └──────────────────────┘
```

### Module Structure

```
scripts/
├── deploy/
│   ├── Deploy-Local.ps1           # Local development entry point
│   ├── Deploy-GitHubActions.ps1   # GitHub Actions entry point
│   ├── Deploy-AzurePipelines.ps1  # Azure Pipelines entry point
│   └── README.md                  # Which script to use when
│
├── modules/
│   ├── Config.psm1                # Configuration management
│   ├── Environment.psm1           # Environment detection
│   ├── Validation.psm1            # Pre/post validation
│   ├── Monitoring.psm1            # Azure CLI/GitHub CLI integration
│   ├── Secrets.psm1               # Key Vault integration
│   └── Logger.psm1                # Structured logging
│
├── database/
│   ├── Build-DatabaseProject.ps1  # MSBuild wrapper
│   ├── Deploy-DatabaseSchema.ps1  # DACPAC deployment
│   ├── Deploy-CLRAssemblies.ps1   # CLR deployment
│   └── Scaffold-Entities.ps1      # EF Core scaffolding
│
├── application/
│   ├── Build-Applications.ps1     # .NET build
│   ├── Deploy-ToHartServer.ps1    # App deployment
│   ├── Deploy-Workers.ps1         # Worker services
│   └── Health-Check.ps1           # App health validation
│
└── config/
    ├── config.base.json           # Base configuration
    ├── config.local.json          # Local overrides
    ├── config.development.json    # Dev environment
    ├── config.staging.json        # Staging environment
    └── config.production.json     # Production environment
```

### Configuration File Structure

**config/config.base.json** (checked into git, no secrets):
```json
{
  "database": {
    "name": "Hartonomous",
    "timeout": 300,
    "dacpac": {
      "blockOnDataLoss": false,
      "dropObjectsNotInSource": false
    }
  },
  "build": {
    "configuration": "Release",
    "verbosity": "minimal"
  },
  "clr": {
    "enableStrictSecurity": true,
    "trustworthy": false
  },
  "monitoring": {
    "enabled": true,
    "applicationInsightsKey": "${KeyVault:AppInsightsKey}"
  }
}
```

**config/config.local.json** (local dev overrides):
```json
{
  "database": {
    "server": "localhost",
    "authentication": "IntegratedSecurity"
  },
  "application": {
    "deployTarget": "localhost:5000"
  },
  "neo4j": {
    "uri": "bolt://localhost:7687",
    "username": "neo4j",
    "password": "neo4jneo4j"
  },
  "build": {
    "configuration": "Debug"
  },
  "monitoring": {
    "enabled": false
  }
}
```

**config/config.production.json** (production overrides, references Key Vault):
```json
{
  "database": {
    "server": "HART-DESKTOP",
    "authentication": "AzureAD"
  },
  "application": {
    "deployTarget": "ahart@HART-SERVER",
    "deployPath": "/srv/www/hartonomous"
  },
  "neo4j": {
    "uri": "bolt://HART-DESKTOP:7687",
    "username": "${KeyVault:Neo4jUsername}",
    "password": "${KeyVault:Neo4jPassword}"
  },
  "clr": {
    "enableStrictSecurity": true
  },
  "monitoring": {
    "enabled": true,
    "applicationInsightsKey": "${KeyVault:AppInsightsKey}"
  },
  "keyVault": {
    "vaultUri": "https://kv-hartonomous-prod.vault.azure.net/",
    "useManagedIdentity": true
  }
}
```

---

## Environment Strategy

### Environment Detection Logic

**modules/Environment.psm1:**
```powershell
function Get-DeploymentEnvironment {
    <#
    .SYNOPSIS
    Detects current deployment environment automatically.

    .RETURNS
    String: "Local", "Development", "Staging", or "Production"
    #>

    # Check for explicit environment variable (highest priority)
    if ($env:HARTONOMOUS_ENVIRONMENT) {
        return $env:HARTONOMOUS_ENVIRONMENT
    }

    # GitHub Actions detection
    if ($env:GITHUB_WORKSPACE) {
        $ghEnv = $env:GITHUB_ENVIRONMENT
        if ($ghEnv) { return $ghEnv }

        # Infer from branch
        $branch = $env:GITHUB_REF_NAME
        switch ($branch) {
            "main" { return "Production" }
            "staging" { return "Staging" }
            "develop" { return "Development" }
            default { return "Development" }
        }
    }

    # Azure Pipelines detection
    if ($env:BUILD_BUILDID) {
        $azEnv = $env:RELEASE_ENVIRONMENTNAME
        if ($azEnv) { return $azEnv }

        # Infer from branch
        $branch = $env:BUILD_SOURCEBRANCHNAME
        switch ($branch) {
            "main" { return "Production" }
            "staging" { return "Staging" }
            "develop" { return "Development" }
            default { return "Development" }
        }
    }

    # Local development (default)
    return "Local"
}

function Test-IsGitHubActions {
    return $null -ne $env:GITHUB_WORKSPACE
}

function Test-IsAzurePipelines {
    return $null -ne $env:BUILD_BUILDID
}

function Test-IsLocal {
    return -not (Test-IsGitHubActions) -and -not (Test-IsAzurePipelines)
}
```

### Configuration Loading

**modules/Config.psm1:**
```powershell
function Get-DeploymentConfig {
    param(
        [string]$Environment = (Get-DeploymentEnvironment),
        [string]$ConfigRoot = "$PSScriptRoot/../config"
    )

    # Load base config
    $baseConfig = Get-Content "$ConfigRoot/config.base.json" | ConvertFrom-Json

    # Load environment-specific config
    $envConfigPath = "$ConfigRoot/config.$Environment.json"
    if (Test-Path $envConfigPath) {
        $envConfig = Get-Content $envConfigPath | ConvertFrom-Json

        # Deep merge (env config overrides base)
        $config = Merge-Configuration $baseConfig $envConfig
    } else {
        $config = $baseConfig
    }

    # Resolve Key Vault references
    $config = Resolve-KeyVaultReferences $config

    return $config
}

function Resolve-KeyVaultReferences {
    param($Config)

    # Find all ${KeyVault:SecretName} references and replace with actual values
    $configJson = $Config | ConvertTo-Json -Depth 10

    $pattern = '\$\{KeyVault:([^}]+)\}'
    $matches = [regex]::Matches($configJson, $pattern)

    foreach ($match in $matches) {
        $secretName = $match.Groups[1].Value
        $secretValue = Get-KeyVaultSecret -SecretName $secretName
        $configJson = $configJson -replace [regex]::Escape($match.Value), $secretValue
    }

    return $configJson | ConvertFrom-Json
}
```

### Secrets Management

**modules/Secrets.psm1:**
```powershell
function Get-KeyVaultSecret {
    param(
        [Parameter(Mandatory)]
        [string]$SecretName,

        [string]$VaultUri = $script:config.keyVault.vaultUri
    )

    # Try managed identity first (production)
    if ($script:config.keyVault.useManagedIdentity) {
        try {
            $secret = az keyvault secret show `
                --vault-name (Get-VaultNameFromUri $VaultUri) `
                --name $SecretName `
                --query value `
                -o tsv

            if ($secret) { return $secret }
        }
        catch {
            Write-Warning "Managed identity failed, trying service principal..."
        }
    }

    # Fallback to service principal (GitHub Actions / Azure Pipelines)
    if ($env:AZURE_CLIENT_ID) {
        $secret = az keyvault secret show `
            --vault-name (Get-VaultNameFromUri $VaultUri) `
            --name $SecretName `
            --query value `
            -o tsv

        return $secret
    }

    # Local development fallback (not recommended for production secrets)
    Write-Warning "Using local development value for secret: $SecretName"
    return $env:$SecretName
}
```

---

## Implementation Roadmap

### Phase 1: Configuration Consolidation (Week 1)

**Goal:** Create centralized configuration system

**Tasks:**
1. Create `scripts/modules/` directory structure
2. Implement `Config.psm1` with JSON-based configuration
3. Implement `Environment.psm1` with environment detection
4. Implement `Secrets.psm1` with Key Vault integration
5. Create configuration files for each environment
6. Test configuration loading across all environments

**Deliverables:**
- [ ] `scripts/modules/Config.psm1`
- [ ] `scripts/modules/Environment.psm1`
- [ ] `scripts/modules/Secrets.psm1`
- [ ] `scripts/config/config.base.json`
- [ ] `scripts/config/config.local.json`
- [ ] `scripts/config/config.development.json`
- [ ] `scripts/config/config.staging.json`
- [ ] `scripts/config/config.production.json`
- [ ] Unit tests for configuration loading

### Phase 2: Monitoring and Validation (Week 1-2)

**Goal:** Add comprehensive monitoring and validation

**Tasks:**
1. Implement `Validation.psm1` with pre/post-deployment checks
2. Implement `Monitoring.psm1` with Azure CLI/GitHub CLI integration
3. Implement `Logger.psm1` with structured logging
4. Add health check endpoints to applications
5. Create validation test suite

**Deliverables:**
- [ ] `scripts/modules/Validation.psm1`
- [ ] `scripts/modules/Monitoring.psm1`
- [ ] `scripts/modules/Logger.psm1`
- [ ] Health check endpoints in Hartonomous.Api
- [ ] Validation test suite

**Monitoring.psm1 Features:**
```powershell
# GitHub CLI integration
function Test-GitHubActionStatus {
    param([string]$WorkflowName)

    gh run list --workflow=$WorkflowName --limit 1 --json status,conclusion
}

# Azure CLI integration
function Test-AzurePipelineStatus {
    param([string]$PipelineId)

    az pipelines runs list --pipeline-id $PipelineId --top 1
}

# Application Insights integration
function Write-DeploymentTelemetry {
    param(
        [string]$EventName,
        [hashtable]$Properties,
        [hashtable]$Metrics
    )

    az monitor app-insights events show `
        --app $script:config.monitoring.appInsightsName `
        --type customEvents `
        --filter "name eq '$EventName'"
}
```

### Phase 3: Database Deployment Refactoring (Week 2)

**Goal:** Consolidate database deployment scripts

**Tasks:**
1. Refactor `Deploy-Database.ps1` to use new modules
2. Create `scripts/database/` directory structure
3. Split database deployment into logical modules
4. Add comprehensive validation and monitoring
5. Update CI/CD pipelines to use new scripts

**Deliverables:**
- [ ] `scripts/database/Build-DatabaseProject.ps1`
- [ ] `scripts/database/Deploy-DatabaseSchema.ps1`
- [ ] `scripts/database/Deploy-CLRAssemblies.ps1`
- [ ] `scripts/database/Scaffold-Entities.ps1`
- [ ] Updated `Deploy-Database.ps1` (uses modules)
- [ ] Database deployment documentation

### Phase 4: Application Deployment Refactoring (Week 2-3)

**Goal:** Create enterprise-grade app deployment with blue-green pattern

**Tasks:**
1. Create `scripts/application/` directory structure
2. Implement blue-green deployment for HART-SERVER
3. Add health checks before/after deployment
4. Implement automated rollback on failure
5. Create systemd service templates with environment variables

**Deliverables:**
- [ ] `scripts/application/Build-Applications.ps1`
- [ ] `scripts/application/Deploy-ToHartServer.ps1` (blue-green)
- [ ] `scripts/application/Deploy-Workers.ps1`
- [ ] `scripts/application/Health-Check.ps1`
- [ ] Templated systemd service files
- [ ] Application deployment documentation

**Blue-Green Deployment Flow:**
```powershell
# Deploy-ToHartServer.ps1 (simplified)
function Deploy-Application {
    param($Config, $Environment)

    # Determine next deployment slot
    $currentSlot = Get-CurrentDeploymentSlot  # "blue" or "green"
    $nextSlot = if ($currentSlot -eq "blue") { "green" } else { "blue" }

    Write-Log "Deploying to $nextSlot slot..."

    # Deploy to inactive slot
    Deploy-ToSlot -Slot $nextSlot -ArtifactsPath $ArtifactsPath

    # Health check on new slot
    if (-not (Test-ApplicationHealth -Slot $nextSlot)) {
        Write-Error "Health check failed on $nextSlot slot, aborting deployment"
        return $false
    }

    # Switch traffic to new slot
    Switch-TrafficToSlot -Slot $nextSlot

    # Final health check
    if (-not (Test-ApplicationHealth -Slot $nextSlot)) {
        Write-Warning "Health check failed after switch, rolling back..."
        Switch-TrafficToSlot -Slot $currentSlot
        return $false
    }

    Write-Log "Deployment successful, $nextSlot is now active"

    # Optional: Keep old slot running for quick rollback
    return $true
}
```

### Phase 5: Unified Entry Points (Week 3)

**Goal:** Create 3 clear deployment entry points

**Tasks:**
1. Create `Deploy-Local.ps1` for local development
2. Create `Deploy-GitHubActions.ps1` for GitHub Actions
3. Create `Deploy-AzurePipelines.ps1` for Azure Pipelines
4. Update GitHub Actions workflow to use new script
5. Update Azure Pipelines YAML to use new script
6. Deprecate old scripts (add warnings)

**Deliverables:**
- [ ] `scripts/deploy/Deploy-Local.ps1`
- [ ] `scripts/deploy/Deploy-GitHubActions.ps1`
- [ ] `scripts/deploy/Deploy-AzurePipelines.ps1`
- [ ] `scripts/deploy/README.md` (clear usage guide)
- [ ] Updated `.github/workflows/ci-cd.yml`
- [ ] Updated `azure-pipelines.yml`
- [ ] Deprecation warnings in old scripts

**Deploy-Local.ps1 Structure:**
```powershell
#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys Hartonomous to local development environment.

.DESCRIPTION
    Complete local deployment including:
    - Database schema (DACPAC)
    - CLR assemblies
    - EF Core entity scaffolding
    - .NET solution build
    - Optional: Start local services

.PARAMETER SkipBuild
    Skip building the database project (use existing DACPAC)

.PARAMETER SkipScaffold
    Skip EF Core entity scaffolding

.PARAMETER SkipAppBuild
    Skip building .NET applications

.PARAMETER StartServices
    Start local API and worker services after deployment

.EXAMPLE
    .\Deploy-Local.ps1

    Complete fresh deployment to localhost

.EXAMPLE
    .\Deploy-Local.ps1 -SkipBuild -SkipScaffold

    Quick deployment (reuse existing DACPAC and entities)
#>

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipScaffold,
    [switch]$SkipAppBuild,
    [switch]$StartServices
)

$ErrorActionPreference = "Stop"

# Import modules
Import-Module "$PSScriptRoot/../modules/Config.psm1" -Force
Import-Module "$PSScriptRoot/../modules/Environment.psm1" -Force
Import-Module "$PSScriptRoot/../modules/Logger.psm1" -Force
Import-Module "$PSScriptRoot/../modules/Validation.psm1" -Force
Import-Module "$PSScriptRoot/../modules/Monitoring.psm1" -Force

# Load configuration for Local environment
$config = Get-DeploymentConfig -Environment "Local"

Write-Log "Hartonomous Local Deployment" -Level Info
Write-Log "Environment: Local" -Level Info
Write-Log "Target Database: $($config.database.server)\$($config.database.name)" -Level Info

# Pre-flight validation
Write-Log "Running pre-flight checks..." -Level Info
Test-Prerequisites -Config $config

# Database deployment
Write-Log "Deploying database..." -Level Info
& "$PSScriptRoot/../database/Deploy-DatabaseSchema.ps1" `
    -Config $config `
    -SkipBuild:$SkipBuild

# Entity scaffolding
if (-not $SkipScaffold) {
    Write-Log "Scaffolding EF Core entities..." -Level Info
    & "$PSScriptRoot/../database/Scaffold-Entities.ps1" -Config $config
}

# Application build
if (-not $SkipAppBuild) {
    Write-Log "Building .NET applications..." -Level Info
    & "$PSScriptRoot/../application/Build-Applications.ps1" -Config $config
}

# Post-deployment validation
Write-Log "Running post-deployment validation..." -Level Info
Test-DeploymentSuccess -Config $config

# Optional: Start services
if ($StartServices) {
    Write-Log "Starting local services..." -Level Info
    Start-LocalServices -Config $config
}

Write-Log "Local deployment complete!" -Level Success
```

### Phase 6: Azure Repos Integration Research (Week 3-4)

**Goal:** Investigate GitHub Actions calling Azure Repos

**Tasks:**
1. Research GitHub Actions integration with Azure DevOps
2. Test Azure DevOps REST API authentication from GitHub Actions
3. Evaluate options:
   - GitHub Actions trigger Azure Pipelines via API
   - GitHub Actions clone Azure Repos and run locally
   - Hybrid approach: GitHub for build, Azure for deploy
4. Document findings and recommendations
5. Implement POC if feasible

**Azure DevOps API Integration POC:**
```yaml
# .github/workflows/ci-cd.yml
- name: Trigger Azure Pipeline
  env:
    AZURE_DEVOPS_PAT: ${{ secrets.AZURE_DEVOPS_PAT }}
  run: |
    # Trigger Azure DevOps pipeline from GitHub Actions
    az pipelines run \
      --org https://dev.azure.com/YourOrg \
      --project YourProject \
      --name "Hartonomous-Deploy" \
      --branch ${{ github.ref_name }}
```

### Phase 7: Documentation and Training (Week 4)

**Goal:** Comprehensive documentation for new deployment system

**Tasks:**
1. Update all deployment documentation
2. Create runbooks for common scenarios
3. Create troubleshooting guide
4. Record video walkthrough
5. Create architecture diagrams

**Deliverables:**
- [ ] `docs/deployment/DEPLOYMENT-GUIDE-V2.md`
- [ ] `docs/deployment/RUNBOOK.md`
- [ ] `docs/deployment/TROUBLESHOOTING.md`
- [ ] `docs/deployment/ARCHITECTURE.md`
- [ ] Video walkthrough (YouTube/internal)
- [ ] Architecture diagrams (Mermaid/PlantUML)

---

## Success Criteria

### Build and Deployment

- [ ] **Zero build errors** across all projects
- [ ] **Zero build warnings** across all projects
- [ ] **Zero deployment errors** in all environments
- [ ] All deployments are **idempotent** (can run multiple times safely)
- [ ] Deployments complete in **<5 minutes** (database + app)

### Configuration and Environments

- [ ] **Single configuration source** (JSON files + Key Vault)
- [ ] **Four environment configs**: Local, Development, Staging, Production
- [ ] **Zero hardcoded values** in scripts (all from config)
- [ ] **Zero secrets in code** (all in Key Vault or environment variables)
- [ ] Environment automatically detected in CI/CD pipelines

### Monitoring and Observability

- [ ] All deployments emit telemetry to Application Insights
- [ ] Health checks run before and after every deployment
- [ ] Automated rollback on failed health checks
- [ ] Azure CLI and GitHub CLI integration for monitoring
- [ ] Deployment status visible in Azure Portal/GitHub

### Documentation

- [ ] Clear entry point for each scenario (Local/GitHub/Azure)
- [ ] Comprehensive runbooks for operations
- [ ] Troubleshooting guide with common issues
- [ ] Architecture diagrams up to date
- [ ] All scripts have detailed help documentation

---

## Monitoring and Validation

### Pre-Deployment Validation

**Test-Prerequisites function:**
```powershell
function Test-Prerequisites {
    param($Config)

    $checks = @()

    # Check required tools
    $checks += Test-CommandExists "dotnet"
    $checks += Test-CommandExists "sqlpackage"
    $checks += Test-CommandExists "msbuild"
    $checks += Test-CommandExists "az"
    $checks += Test-CommandExists "gh"

    # Check connectivity
    $checks += Test-SqlConnection -Server $Config.database.server
    $checks += Test-Neo4jConnection -Uri $Config.neo4j.uri

    # Check permissions
    $checks += Test-SqlPermissions -Server $Config.database.server

    # Check disk space
    $checks += Test-DiskSpace -MinimumGB 10

    if ($checks -contains $false) {
        throw "Pre-flight checks failed, aborting deployment"
    }
}
```

### Post-Deployment Validation

**Test-DeploymentSuccess function:**
```powershell
function Test-DeploymentSuccess {
    param($Config)

    $tests = @()

    # Database validation
    $tests += Test-DatabaseObjects -Config $Config
    $tests += Test-CLRAssemblies -Config $Config
    $tests += Test-ServiceBroker -Config $Config

    # Application validation
    if ($Config.application.deployTarget) {
        $tests += Test-ApiHealth -Config $Config
        $tests += Test-WorkerHealth -Config $Config
    }

    # Smoke tests
    $tests += Test-SampleQuery -Config $Config
    $tests += Test-SampleIngestion -Config $Config

    $passed = ($tests | Where-Object { $_ -eq $true }).Count
    $failed = ($tests | Where-Object { $_ -eq $false }).Count

    Write-Log "Validation: $passed passed, $failed failed"

    if ($failed -gt 0) {
        Write-Warning "Some validation tests failed"
        return $false
    }

    return $true
}
```

### Continuous Monitoring

**Azure CLI Monitoring:**
```powershell
# scripts/monitoring/Check-DeploymentStatus.ps1
# Check recent deployments in Application Insights
az monitor app-insights query `
    --app $appInsightsName `
    --analytics-query "
        customEvents
        | where name == 'Deployment'
        | where timestamp > ago(1h)
        | project timestamp, name, customDimensions
        | order by timestamp desc
    "

# Check application health
az monitor metrics list `
    --resource $appInsightsResourceId `
    --metric "requests/failed" `
    --interval PT1M `
    --start-time (Get-Date).AddHours(-1) `
    --end-time (Get-Date)
```

**GitHub CLI Monitoring:**
```powershell
# Check recent workflow runs
gh run list --workflow "CI/CD Pipeline" --limit 10

# Get logs from last failed run
gh run view --log-failed

# Re-run failed workflow
gh workflow run "CI/CD Pipeline" --ref main
```

---

## Next Steps

### Immediate Actions (This Week)

1. **Review this gameplan** with the team
2. **Set up Azure Key Vault** for secrets management
3. **Create configuration files** (Phase 1)
4. **Implement Environment detection module** (Phase 1)
5. **Test configuration loading** across all environments

### Short-Term (Next 2 Weeks)

1. Complete Phase 1 and Phase 2
2. Refactor database deployment scripts
3. Begin application deployment refactoring
4. Update CI/CD pipelines to use new modules

### Medium-Term (Next Month)

1. Complete all phases
2. Fully migrate to new deployment system
3. Deprecate old scripts
4. Complete documentation

### Long-Term (Next Quarter)

1. Optimize deployment performance
2. Add advanced monitoring dashboards
3. Implement automated canary deployments
4. Create disaster recovery runbooks

---

## Appendix A: Script Deprecation Plan

### Scripts to Keep (Refactored)

- `deploy-dacpac.ps1` → Refactor, use modules
- `build-dacpac.ps1` → Refactor, use modules
- `scaffold-entities.ps1` → Refactor, use modules
- `Deploy-CLRCertificate.ps1` → Refactor, use modules
- `Initialize-CLRSigning.ps1` → Refactor, use modules
- `Sign-CLRAssemblies.ps1` → Refactor, use modules

### Scripts to Deprecate

- `Deploy-Master.ps1` → Replaced by `Deploy-Local.ps1`
- `Deploy-All.ps1` → Replaced by `Deploy-Local.ps1`
- `Deploy-Idempotent.ps1` → Replaced by `Deploy-Local.ps1`
- `deploy-hartonomous.ps1` → Replaced by `Deploy-Local.ps1`

**Deprecation Warnings:**
```powershell
# Add to top of deprecated scripts
Write-Warning "This script is deprecated and will be removed in v2.0"
Write-Warning "Please use: .\scripts\deploy\Deploy-Local.ps1"
Write-Warning "See: docs/deployment/DEPLOYMENT-GUIDE-V2.md"
Write-Host ""
Write-Host "Continuing in 5 seconds... (Ctrl+C to cancel)"
Start-Sleep -Seconds 5
```

---

## Appendix B: Environment Variables Reference

### Local Development

```powershell
# Optional overrides
$env:HARTONOMOUS_ENVIRONMENT = "Local"
$env:SQL_SERVER = "localhost"
$env:SQL_DATABASE = "Hartonomous"
```

### GitHub Actions

```yaml
env:
  HARTONOMOUS_ENVIRONMENT: ${{ github.event.inputs.environment || 'development' }}
  DOTNET_VERSION: '10.x'
  BUILD_CONFIGURATION: 'Release'
```

### Azure Pipelines

```yaml
variables:
  - name: environment
    value: 'Production'
  - name: dotnetSdkVersion
    value: '10.x'
  - name: buildConfiguration
    value: 'Release'
```

---

## Appendix C: Key Vault Secrets Checklist

### Required Secrets

- [ ] `Neo4jUsername` - Neo4j database username
- [ ] `Neo4jPassword` - Neo4j database password
- [ ] `SqlConnectionString` - SQL Server connection string (if using SQL auth)
- [ ] `AppInsightsKey` - Application Insights instrumentation key
- [ ] `GitHubPAT` - GitHub Personal Access Token (for API access)
- [ ] `AzureDevOpsPAT` - Azure DevOps Personal Access Token
- [ ] `HartServerSshKey` - SSH private key for HART-SERVER deployment

### Secret Naming Convention

- Use PascalCase for secret names
- Prefix environment-specific secrets: `{Environment}-{SecretName}`
- Example: `Production-Neo4jPassword`, `Staging-Neo4jPassword`

---

## Document Control

**Owner**: DevOps Team
**Last Updated**: January 21, 2025
**Version**: 1.0
**Status**: Planning Phase
**Review Date**: January 28, 2025

**Approvals Required**:
- [ ] Technical Lead
- [ ] DevOps Lead
- [ ] Security Team
- [ ] Product Owner
