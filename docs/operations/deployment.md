# Hartonomous Deployment Guide

**Azure Arc Hybrid Deployment | DACPAC Build & Deploy | CI/CD Workflows**

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Azure Infrastructure Setup](#azure-infrastructure-setup)
5. [Azure Arc Configuration](#azure-arc-configuration)
6. [DACPAC Build Process](#dacpac-build-process)
7. [CLR Assembly Deployment](#clr-assembly-deployment)
8. [Database Deployment](#database-deployment)
9. [GitHub Actions CI/CD](#github-actions-cicd)
10. [Environment Configuration](#environment-configuration)
11. [Certificate Deployment](#certificate-deployment)
12. [Migration Procedures](#migration-procedures)
13. [Troubleshooting](#troubleshooting)

---

## Overview

Hartonomous uses a **hybrid cloud + on-premises deployment model** via Azure Arc:

- **Azure Cloud**: Key Vault, App Configuration, Entra ID, DevOps pipelines
- **On-Premises (Arc-enabled)**: SQL Server 2025 + .NET applications on Windows Services

**Deployment Targets**:
- **HART-DESKTOP**: SQL Server 2025 host (Hartonomous database, CLR assemblies, spatial indexes)
- **hart-server**: Application host (Hartonomous.Api, worker services)

**Key Features**:
- ✅ Entra ID authentication (no SQL logins)
- ✅ Key Vault secrets management
- ✅ DACPAC-based database deployments
- ✅ Self-hosted GitHub Actions runners
- ✅ Zero-downtime deployments with pre/post-deployment scripts

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      AZURE CLOUD                            │
├─────────────────────────────────────────────────────────────┤
│ • Key Vault (connection strings, secrets)                   │
│ • App Configuration (OODA settings, feature flags)          │
│ • Entra ID (app registrations, roles: Admin/Analyst/User)   │
│ • GitHub Actions (hosted workflows)                         │
└───────────────────────┬─────────────────────────────────────┘
                        │ Azure Arc
                        ↓
┌─────────────────────────────────────────────────────────────┐
│                   ON-PREMISES INFRASTRUCTURE                │
├─────────────────────────────────────────────────────────────┤
│ HART-DESKTOP (Windows Server 2025)                          │
│ ├─ SQL Server 2025 RC1                                      │
│ │  ├─ Hartonomous database (DACPAC-deployed)               │
│ │  ├─ CLR assemblies (16 dependencies + main assembly)     │
│ │  ├─ Spatial indexes (R-Tree, 3D Hilbert curves)          │
│ │  ├─ OODA loop procedures (sp_Analyze → sp_Learn)         │
│ │  └─ Service Broker queues (async processing)             │
│ └─ GitHub Actions Runner (database jobs)                    │
│                                                              │
│ hart-server (Linux Ubuntu)                                  │
│ ├─ Hartonomous.Api (Windows Service)                        │
│ │  ├─ Entra ID authentication                               │
│ │  └─ Key Vault integration                                 │
│ ├─ Workers (CES Consumer, Neo4j Sync, OODA Analyzers)       │
│ └─ GitHub Actions Runner (application jobs)                 │
└─────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Hardware Requirements

**SQL Server Host (HART-DESKTOP)**:
- Windows Server 2019+ or Windows 10/11 Pro
- 16GB+ RAM (32GB recommended for large models)
- 100GB+ available disk space
- Multi-core processor (4+ cores, 8+ recommended)

**Application Host (hart-server)**:
- Ubuntu 20.04+ or Windows Server 2019+
- 8GB+ RAM
- 50GB+ available disk space

### Software Requirements

**Development Machine**:
- Visual Studio 2022 Enterprise/Professional OR MSBuild Tools
- .NET 10 SDK
- .NET Framework 4.8.1 (for CLR assemblies)
- PowerShell 7.5+
- Git for Windows

**SQL Server Host**:
- SQL Server 2025 RC1 (or SQL Server 2022+)
- SQL Server Data Tools (SSDT)
- SqlPackage CLI (auto-installed by scripts)
- Azure CLI (`az`)

**Application Host**:
- .NET 10 Runtime
- PowerShell Core 7.5+
- Azure CLI

### Azure Prerequisites

- Azure subscription with Arc-enabled SQL Server instances
- Microsoft Entra ID tenant access
- Permissions to create:
  - Service Principals
  - Federated Identity Credentials
  - Role assignments
  - Resource Groups
  - Key Vaults
  - App Configuration stores

---

## Azure Infrastructure Setup

### 1. Create Azure Resources

Run the infrastructure provisioning script:

```powershell
# Navigate to scripts directory
cd D:\Repositories\Hartonomous\scripts\azure

# Create all Azure resources (10-15 minutes)
.\01-create-infrastructure.ps1 `
    -SubscriptionId "your-subscription-id" `
    -ResourceGroupName "rg-hartonomous-prod" `
    -Location "eastus" `
    -KeyVaultName "kv-hartonomous-prod" `
    -AppConfigName "appconfig-hartonomous-prod" `
    -Environment "Production"
```

**What This Creates**:
1. **Resource Group**: `rg-hartonomous-prod`
2. **Key Vault**: `kv-hartonomous-prod`
   - Connection strings stored as secrets
   - Access policy for service principal
3. **App Configuration**: `appconfig-hartonomous-prod`
   - OODA loop settings (interval: 15 minutes)
   - Inference defaults (temperature: 0.7, top-k: 50)
   - Feature flags (OODA enabled, Neo4j sync enabled)
4. **Entra ID App Registrations**:
   - `Hartonomous-API` (API authentication)
   - `Hartonomous-UI` (Blazor UI authentication)
   - Roles: Admin, Analyst, User

### 2. Configure Key Vault Secrets

Store connection strings in Key Vault:

```powershell
# SQL Server connection string
az keyvault secret set `
    --vault-name "kv-hartonomous-prod" `
    --name "SqlConnectionString" `
    --value "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"

# Neo4j connection string
az keyvault secret set `
    --vault-name "kv-hartonomous-prod" `
    --name "Neo4jConnectionString" `
    --value "bolt://localhost:7687"

# Neo4j credentials
az keyvault secret set `
    --vault-name "kv-hartonomous-prod" `
    --name "Neo4jUsername" `
    --value "neo4j"

az keyvault secret set `
    --vault-name "kv-hartonomous-prod" `
    --name "Neo4jPassword" `
    --value "your-neo4j-password"
```

### 3. Configure App Configuration

Set OODA loop and inference settings:

```powershell
# OODA loop configuration
az appconfig kv set `
    --name "appconfig-hartonomous-prod" `
    --key "Ooda:AnalysisIntervalMinutes" `
    --value "15" `
    --content-type "application/json"

az appconfig kv set `
    --name "appconfig-hartonomous-prod" `
    --key "Ooda:RiskThreshold" `
    --value "0.3" `
    --content-type "application/json"

# Inference defaults
az appconfig kv set `
    --name "appconfig-hartonomous-prod" `
    --key "Inference:DefaultTemperature" `
    --value "0.7" `
    --content-type "application/json"

az appconfig kv set `
    --name "appconfig-hartonomous-prod" `
    --key "Inference:DefaultTopK" `
    --value "50" `
    --content-type "application/json"

# Feature flags
az appconfig feature set `
    --name "appconfig-hartonomous-prod" `
    --feature "OodaLoop" `
    --yes

az appconfig feature set `
    --name "appconfig-hartonomous-prod" `
    --feature "Neo4jSync" `
    --yes
```

---

## Azure Arc Configuration

### 1. Install Azure Arc Agent

On **HART-DESKTOP** (SQL Server host):

```powershell
# Download Arc agent installer
$arcUrl = "https://aka.ms/AzureConnectedMachineAgent"
Invoke-WebRequest -Uri $arcUrl -OutFile "AzureConnectedMachineAgent.msi"

# Install Arc agent
msiexec /i AzureConnectedMachineAgent.msi /quiet

# Connect machine to Azure Arc
azcmagent connect `
    --resource-group "rg-hartonomous-prod" `
    --tenant-id "your-tenant-id" `
    --subscription-id "your-subscription-id" `
    --location "eastus" `
    --tags "Environment=Production,Role=SQLServer"
```

### 2. Install SQL Server Arc Extension

**CRITICAL**: Set `managedIdentityAuthSetting` to `"OUTBOUND AND INBOUND"` (not "OUTBOUND ONLY"):

```powershell
# Install SQL Server extension
az connectedmachine extension create `
    --name "WindowsAgent.SqlServer" `
    --machine-name "HART-DESKTOP" `
    --resource-group "rg-hartonomous-prod" `
    --type "WindowsAgent.SqlServer" `
    --publisher "Microsoft.AzureData" `
    --settings '{
        "SqlManagement": {
            "IsEnabled": true
        },
        "ExcludedSqlInstances": [],
        "AzureAD": [{
            "managedIdentityAuthSetting": "OUTBOUND AND INBOUND"
        }]
    }'
```

**Why OUTBOUND AND INBOUND?**
- `OUTBOUND ONLY`: Arc machine can authenticate TO Azure (telemetry, monitoring)
- `OUTBOUND AND INBOUND`: Arc machine can authenticate TO Azure AND Azure services can authenticate TO SQL Server
- **Required for GitHub Actions** → Service Principal authentication to on-premises SQL Server

### 3. Create Service Principal with Federated Credential

```powershell
# Run service principal setup script
.\scripts\Configure-GitHubActionsServicePrincipals.ps1 `
    -SubscriptionId "your-subscription-id" `
    -ResourceGroupName "rg-hartonomous-prod" `
    -GitHubOrg "AHartTN" `
    -GitHubRepo "Hartonomous-Sandbox" `
    -GitHubBranch "main"
```

**What This Creates**:
1. **Service Principal**: `Hartonomous-GitHub-Actions-SP`
2. **Federated Credential**: OIDC trust with GitHub repository
   - Subject: `repo:AHartTN/Hartonomous-Sandbox:ref:refs/heads/main`
3. **Role Assignments**:
   - `SQL Server Contributor` on Arc-enabled SQL Server
   - `Contributor` on Resource Group
4. **SQL Server Login**: Creates Entra ID login for service principal

### 4. Grant SQL Server Permissions

On **HART-DESKTOP**, run SQL permissions script:

```sql
-- Run this script on SQL Server
-- File: scripts\Configure-GitHubActionsSqlPermissions.sql

USE [master];
GO

-- Create login from Entra ID service principal
CREATE LOGIN [Hartonomous-GitHub-Actions-SP] FROM EXTERNAL PROVIDER;
GO

-- Grant sysadmin role (required for DACPAC deployment)
ALTER SERVER ROLE [sysadmin] ADD MEMBER [Hartonomous-GitHub-Actions-SP];
GO

-- Verify login
SELECT name, type_desc, is_disabled
FROM sys.server_principals
WHERE name = 'Hartonomous-GitHub-Actions-SP';
GO
```

---

## DACPAC Build Process

### Overview

Hartonomous uses **SQL Server Database Projects** (`.sqlproj`) to manage schema as code. The build process:

1. Compiles C# CLR code → `Hartonomous.Clr.dll`
2. Compiles T-SQL scripts → schema model
3. Produces `Hartonomous.Database.dacpac` (contains CLR assembly as hex binary)

### Manual Build

```powershell
# Build DACPAC manually
.\scripts\build-dacpac.ps1

# Output: src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac (335KB)
```

### Build Script Details

**File**: `scripts\build-dacpac.ps1`

```powershell
<#
.SYNOPSIS
    Build Hartonomous.Database.dacpac from SQL project
.DESCRIPTION
    - Finds MSBuild (VS 2022 or Build Tools)
    - Builds .sqlproj with CLR assemblies
    - Outputs DACPAC to bin\Release\
#>

# Find MSBuild
$msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1

if (-not $msbuildPath) {
    throw "MSBuild not found. Install Visual Studio 2022 or Build Tools."
}

# Build DACPAC
$sqlprojPath = "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
& $msbuildPath $sqlprojPath /p:Configuration=Release /p:Platform=AnyCPU /v:minimal

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC build failed with exit code $LASTEXITCODE"
}

Write-Host "✓ DACPAC built successfully" -ForegroundColor Green
```

### What's in the DACPAC?

**Included**:
- ✅ `Hartonomous.Clr.dll` (embedded as hex binary: `CREATE ASSEMBLY FROM 0x4D5A90...`)
- ✅ All T-SQL DDL (tables, views, functions, procedures)
- ✅ Metadata about CLR functions/procedures/aggregates
- ✅ Pre/post-deployment scripts

**NOT Included** (must be deployed separately):
- ❌ 16 external CLR dependency DLLs:
  - `System.Runtime.CompilerServices.Unsafe.dll`
  - `System.Buffers.dll`
  - `System.Numerics.Vectors.dll`
  - `System.Memory.dll`
  - `System.Collections.Immutable.dll`
  - `System.Reflection.Metadata.dll`
  - `MathNet.Numerics.dll`
  - `Microsoft.SqlServer.Types.dll`
  - `Newtonsoft.Json.dll`
  - (plus 7 more - see `docs/operations/clr-dependencies.md`)

**Why External Assemblies Aren't in DACPAC?**

In `.sqlproj` file:
```xml
<Reference Include="MathNet.Numerics">
  <HintPath>..\..\dependencies\MathNet.Numerics.dll</HintPath>
  <Private>False</Private>  <!-- THIS IS THE KEY -->
</Reference>
```

- `<Private>False</Private>` → "Don't copy to output directory"
- These are **compile-time references** only (needed to build `Hartonomous.Clr.dll`)
- DACPAC contains `CREATE ASSEMBLY` statements expecting these DLLs to exist on SQL Server

### Build Verification

```powershell
# Verify DACPAC contents
.\scripts\verify-dacpac.ps1

# Expected output:
# ✓ DACPAC exists (335KB)
# ✓ Contains Hartonomous.Clr assembly
# ✓ Contains 42 tables
# ✓ Contains 18 views
# ✓ Contains 87 stored procedures
# ✓ Contains 23 functions
# ✓ Contains 9 CLR aggregates
```

---

## CLR Assembly Deployment

### Deployment Order (CRITICAL)

External CLR assemblies MUST be deployed **BEFORE** DACPAC deployment because:
- `Hartonomous.Clr.dll` references these assemblies
- SQL Server validates dependencies when creating assemblies
- DACPAC deployment will fail if dependencies don't exist

**Correct Deployment Sequence**:

```
1. Enable CLR Integration
2. Deploy 16 External CLR Assemblies (dependency order matters!)
3. Deploy DACPAC (creates Hartonomous.Clr assembly + tables/procedures)
4. Set TRUSTWORTHY ON
```

### 1. Enable CLR Integration

```sql
-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Disable CLR strict security (required for UNSAFE assemblies)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
```

### 2. Deploy External CLR Assemblies

**Dependency Tier Structure** (MUST deploy in this order):

**Tier 1** (no dependencies):
```sql
CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Runtime.CompilerServices.Unsafe.dll'
WITH PERMISSION_SET = UNSAFE;
GO

CREATE ASSEMBLY [System.Buffers]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Buffers.dll'
WITH PERMISSION_SET = UNSAFE;
GO
```

**Tier 2** (depends on GAC: System.Numerics):
```sql
CREATE ASSEMBLY [System.Numerics.Vectors]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Numerics.Vectors.dll'
WITH PERMISSION_SET = UNSAFE;
GO
```

**Tier 3** (depends on Tier 1 + Tier 2):
```sql
CREATE ASSEMBLY [System.Memory]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Memory.dll'
WITH PERMISSION_SET = UNSAFE;
GO
```

**Tier 4** (depends on Tier 3):
```sql
CREATE ASSEMBLY [System.Collections.Immutable]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Collections.Immutable.dll'
WITH PERMISSION_SET = UNSAFE;
GO

CREATE ASSEMBLY [System.Reflection.Metadata]
FROM 'D:\Repositories\Hartonomous\dependencies\System.Reflection.Metadata.dll'
WITH PERMISSION_SET = UNSAFE;
GO
```

**Tier 5** (independent, use GAC assemblies):
```sql
CREATE ASSEMBLY [MathNet.Numerics]
FROM 'D:\Repositories\Hartonomous\dependencies\MathNet.Numerics.dll'
WITH PERMISSION_SET = UNSAFE;
GO

CREATE ASSEMBLY [Microsoft.SqlServer.Types]
FROM 'D:\Repositories\Hartonomous\dependencies\Microsoft.SqlServer.Types.dll'
WITH PERMISSION_SET = UNSAFE;
GO

CREATE ASSEMBLY [Newtonsoft.Json]
FROM 'D:\Repositories\Hartonomous\dependencies\Newtonsoft.Json.dll'
WITH PERMISSION_SET = UNSAFE;
GO
```

### Automated Deployment Script

**File**: `scripts\deploy-clr-assemblies.ps1`

```powershell
<#
.SYNOPSIS
    Deploy CLR assemblies in correct dependency order
.PARAMETER Server
    SQL Server instance (default: localhost)
.PARAMETER Database
    Database name (default: master)
#>
param(
    [string]$Server = "localhost",
    [string]$Database = "master"
)

$ErrorActionPreference = "Stop"

# Dependency order (CRITICAL - DO NOT CHANGE)
$assemblies = @(
    # Tier 1: No dependencies
    @{ Name = "System.Runtime.CompilerServices.Unsafe"; Path = "System.Runtime.CompilerServices.Unsafe.dll" },
    @{ Name = "System.Buffers"; Path = "System.Buffers.dll" },
    
    # Tier 2: Depends on GAC System.Numerics
    @{ Name = "System.Numerics.Vectors"; Path = "System.Numerics.Vectors.dll" },
    
    # Tier 3: Depends on Tier 1 + Tier 2
    @{ Name = "System.Memory"; Path = "System.Memory.dll" },
    
    # Tier 4: Depends on Tier 3
    @{ Name = "System.Collections.Immutable"; Path = "System.Collections.Immutable.dll" },
    @{ Name = "System.Reflection.Metadata"; Path = "System.Reflection.Metadata.dll" },
    
    # Tier 5: Independent
    @{ Name = "MathNet.Numerics"; Path = "MathNet.Numerics.dll" },
    @{ Name = "Microsoft.SqlServer.Types"; Path = "Microsoft.SqlServer.Types.dll" },
    @{ Name = "Newtonsoft.Json"; Path = "Newtonsoft.Json.dll" }
)

$dependenciesPath = Join-Path $PSScriptRoot "..\dependencies"

foreach ($assembly in $assemblies) {
    $dllPath = Join-Path $dependenciesPath $assembly.Path
    
    if (-not (Test-Path $dllPath)) {
        throw "Assembly not found: $dllPath"
    }
    
    Write-Host "Deploying [$($assembly.Name)]..." -NoNewline
    
    # Read DLL as hex
    $bytes = [System.IO.File]::ReadAllBytes($dllPath)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")
    
    # Create assembly
    $sql = @"
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$($assembly.Name)')
    DROP ASSEMBLY [$($assembly.Name)];

CREATE ASSEMBLY [$($assembly.Name)]
FROM $hex
WITH PERMISSION_SET = UNSAFE;
"@
    
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $sql -QueryTimeout 300
    
    Write-Host " ✓" -ForegroundColor Green
}

Write-Host "`n✓ All CLR assemblies deployed successfully" -ForegroundColor Green
```

### Verification

```sql
-- Verify all assemblies exist
SELECT name, permission_set_desc, is_visible, create_date
FROM sys.assemblies
WHERE name NOT IN ('master', 'msdb', 'model', 'tempdb')
ORDER BY name;

-- Expected 16 assemblies (9 external + main Hartonomous.Clr after DACPAC)
```

---

## Database Deployment

### Overview

DACPAC deployment uses **SqlPackage CLI** for schema updates. The deployment:

1. Compares DACPAC schema to target database
2. Generates T-SQL diff script
3. Executes pre-deployment scripts
4. Applies schema changes
5. Executes post-deployment scripts

### Manual Deployment

```powershell
# Deploy to local SQL Server
.\scripts\deploy-dacpac.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -IntegratedSecurity

# Deploy to remote SQL Server with SQL auth
.\scripts\deploy-dacpac.ps1 `
    -Server "HART-DESKTOP" `
    -Database "Hartonomous" `
    -User "sa" `
    -Password "YourPassword123!"

# Deploy with Azure Arc service principal token
.\scripts\deploy-dacpac.ps1 `
    -Server "HART-DESKTOP" `
    -Database "Hartonomous" `
    -AccessToken $(az account get-access-token --resource https://database.windows.net --query accessToken -o tsv)
```

### Deployment Script Details

**File**: `scripts\deploy-dacpac.ps1`

```powershell
<#
.SYNOPSIS
    Deploy Hartonomous.Database.dacpac to SQL Server
.PARAMETER Server
    SQL Server instance
.PARAMETER Database
    Target database name
.PARAMETER IntegratedSecurity
    Use Windows authentication
.PARAMETER User
    SQL Server username (if not using IntegratedSecurity)
.PARAMETER Password
    SQL Server password
.PARAMETER AccessToken
    Azure AD access token (for Arc-enabled SQL Server)
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [switch]$IntegratedSecurity,
    [string]$User,
    [string]$Password,
    [string]$AccessToken
)

$ErrorActionPreference = "Stop"

# Install SqlPackage if not exists
if (-not (Get-Command sqlpackage -ErrorAction SilentlyContinue)) {
    Write-Host "Installing SqlPackage..." -ForegroundColor Yellow
    & "$PSScriptRoot\install-sqlpackage.ps1"
}

# Find DACPAC
$dacpacPath = "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"
if (-not (Test-Path $dacpacPath)) {
    throw "DACPAC not found at $dacpacPath. Run build-dacpac.ps1 first."
}

# Build connection string
$connectionString = "Server=$Server;Database=$Database;"
if ($IntegratedSecurity) {
    $connectionString += "Integrated Security=true;"
} elseif ($AccessToken) {
    # Azure AD token authentication
    $connectionString += "Authentication=Active Directory Service Principal;"
} elseif ($User -and $Password) {
    $connectionString += "User Id=$User;Password=$Password;"
} else {
    throw "Must specify IntegratedSecurity, AccessToken, or User/Password"
}
$connectionString += "TrustServerCertificate=true;"

# Deploy DACPAC
Write-Host "Deploying DACPAC to $Server.$Database..." -ForegroundColor Yellow

$sqlpackageArgs = @(
    "/Action:Publish"
    "/SourceFile:$dacpacPath"
    "/TargetConnectionString:$connectionString"
    "/p:BlockOnPossibleDataLoss=false"  # Allow schema changes
    "/p:DropObjectsNotInSource=false"   # Don't drop user data
    "/p:ScriptDatabaseOptions=false"
    "/p:IncludeCompositeObjects=true"
)

if ($AccessToken) {
    $sqlpackageArgs += "/AccessToken:$AccessToken"
}

& sqlpackage @sqlpackageArgs

if ($LASTEXITCODE -ne 0) {
    throw "SqlPackage failed with exit code $LASTEXITCODE"
}

# Set TRUSTWORTHY ON (required for CLR)
$trustworthySql = "ALTER DATABASE [$Database] SET TRUSTWORTHY ON;"
if ($AccessToken) {
    # Use sqlcmd with access token
    $trustworthySql | sqlcmd -S $Server -d master -G -P $AccessToken
} else {
    Invoke-Sqlcmd -ServerInstance $Server -Database master -Query $trustworthySql
}

Write-Host "✓ Database deployed successfully" -ForegroundColor Green
```

### Pre/Post-Deployment Scripts

**Pre-Deployment** (`src\Hartonomous.Database\Scripts\PreDeployment.sql`):
- Disable foreign key constraints
- Drop temp objects
- Backup data for migration

**Post-Deployment** (`src\Hartonomous.Database\Scripts\PostDeployment.sql`):
- Re-enable foreign key constraints
- Rebuild indexes
- Update statistics
- Seed reference data

### Deployment Verification

```powershell
# Verify deployment
.\scripts\deployment-summary.ps1 -Server "localhost" -Database "Hartonomous"

# Expected output:
# ✓ Database exists
# ✓ 42 tables created
# ✓ 18 views created
# ✓ 87 stored procedures created
# ✓ 23 functions created
# ✓ 9 CLR aggregates created
# ✓ CLR integration enabled
# ✓ TRUSTWORTHY ON
# ✓ Service Broker enabled
# ✓ Spatial indexes created (4)
```

---

## GitHub Actions CI/CD

### Overview

Hartonomous uses **self-hosted GitHub Actions runners** for CI/CD:

- **HART-DESKTOP** (Windows): Database jobs (build DACPAC, deploy CLR)
- **hart-server** (Linux): Application jobs (build .NET apps, run tests)

**Why Self-Hosted?**
- Free unlimited parallel jobs (vs $15/month per job on Azure DevOps)
- Access to on-premises SQL Server
- Better performance (local builds)
- Same enterprise RBAC (Microsoft Entra Service Principal)

### Runner Installation

#### HART-DESKTOP (Windows)

```powershell
# Download runner
mkdir D:\GitHub\actions-runner
cd D:\GitHub\actions-runner
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-win-x64-2.311.0.zip -OutFile actions-runner-win-x64-2.311.0.zip
Expand-Archive -Path .\actions-runner-win-x64-2.311.0.zip -DestinationPath .

# Configure runner
.\config.cmd `
    --url https://github.com/AHartTN/Hartonomous-Sandbox `
    --token YOUR_RUNNER_TOKEN `
    --name HART-DESKTOP `
    --labels self-hosted,windows,sql-server `
    --work _work

# Install as Windows Service
.\svc.cmd install
.\svc.cmd start

# Verify service
Get-Service -Name "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"
```

#### hart-server (Linux)

```bash
# Download runner
mkdir -p /var/workload/GitHub/actions-runner
cd /var/workload/GitHub/actions-runner
curl -o actions-runner-linux-x64-2.311.0.tar.gz -L https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-linux-x64-2.311.0.tar.gz
tar xzf actions-runner-linux-x64-2.311.0.tar.gz

# Create service user
sudo useradd -r -m -d /home/github-runner github-runner

# Configure runner
sudo -u github-runner ./config.sh \
    --url https://github.com/AHartTN/Hartonomous-Sandbox \
    --token YOUR_RUNNER_TOKEN \
    --name hart-server \
    --labels self-hosted,linux,hart-server \
    --work _work

# Install as systemd service
sudo ./svc.sh install github-runner
sudo ./svc.sh start

# Verify service
sudo systemctl status actions.runner.AHartTN-Hartonomous-Sandbox.hart-server.service
```

### Workflow Configuration

**File**: `.github/workflows/build-and-deploy.yml`

```yaml
name: Build and Deploy Hartonomous

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
  AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}

jobs:
  build-dacpac:
    name: Build DACPAC
    runs-on: [self-hosted, windows, sql-server]
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Build DACPAC
        run: .\scripts\build-dacpac.ps1
        shell: pwsh
      
      - name: Upload DACPAC artifact
        uses: actions/upload-artifact@v4
        with:
          name: dacpac
          path: src/Hartonomous.Database/bin/Release/*.dacpac
  
  deploy-database:
    name: Deploy Database
    runs-on: [self-hosted, windows, sql-server]
    needs: build-dacpac
    
    permissions:
      id-token: write  # Required for OIDC token
      contents: read
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Download DACPAC artifact
        uses: actions/download-artifact@v4
        with:
          name: dacpac
          path: src/Hartonomous.Database/bin/Release/
      
      - name: Azure Login (OIDC)
        uses: azure/login@v1
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      
      - name: Get Azure AD Token
        id: get-token
        run: |
          $token = az account get-access-token --resource https://database.windows.net --query accessToken -o tsv
          echo "::add-mask::$token"
          echo "ACCESS_TOKEN=$token" >> $env:GITHUB_OUTPUT
        shell: pwsh
      
      - name: Deploy CLR Assemblies
        run: |
          .\scripts\deploy-clr-assemblies.ps1 `
            -Server "HART-DESKTOP" `
            -Database "master"
        shell: pwsh
      
      - name: Deploy DACPAC
        run: |
          .\scripts\deploy-dacpac.ps1 `
            -Server "HART-DESKTOP" `
            -Database "Hartonomous" `
            -AccessToken "${{ steps.get-token.outputs.ACCESS_TOKEN }}"
        shell: pwsh
  
  scaffold-entities:
    name: Scaffold EF Core Entities
    runs-on: [self-hosted, windows, sql-server]
    needs: deploy-database
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Scaffold Entities
        run: .\scripts\scaffold-entities.ps1
        shell: pwsh
      
      - name: Commit Generated Files
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add src/Hartonomous.Data.Entities/
          git diff --staged --quiet || git commit -m "Auto-generated EF Core entities"
          git push
        shell: bash
  
  build-and-test:
    name: Build and Test .NET Solution
    runs-on: [self-hosted, linux, hart-server]
    needs: scaffold-entities
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build solution
        run: dotnet build --no-restore --configuration Release
      
      - name: Run tests
        run: dotnet test --no-build --configuration Release --verbosity normal
  
  build-applications:
    name: Build Applications
    runs-on: [self-hosted, linux, hart-server]
    needs: build-and-test
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Publish API
        run: |
          dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj \
            --configuration Release \
            --output publish/api
      
      - name: Publish Workers
        run: |
          dotnet publish src/Hartonomous.Workers.Ingestion/Hartonomous.Workers.Ingestion.csproj \
            --configuration Release \
            --output publish/workers
      
      - name: Upload application artifacts
        uses: actions/upload-artifact@v4
        with:
          name: applications
          path: publish/
```

### GitHub Secrets Configuration

Configure these secrets in GitHub repository settings:

```
AZURE_SUBSCRIPTION_ID: your-subscription-id
AZURE_TENANT_ID: your-tenant-id
AZURE_CLIENT_ID: service-principal-app-id (from Configure-GitHubActionsServicePrincipals.ps1)
```

**Note**: NO client secret stored! Uses **OIDC workload identity federation**.

---

## Environment Configuration

### Development Environment

**File**: `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;",
    "Neo4j": "bolt://localhost:7687"
  },
  "Neo4j": {
    "Username": "neo4j",
    "Password": "local-dev-password"
  },
  "Ooda": {
    "AnalysisIntervalMinutes": 5,
    "RiskThreshold": 0.5,
    "Enabled": true
  },
  "Inference": {
    "DefaultTemperature": 0.7,
    "DefaultTopK": 50,
    "MaxTokens": 2048
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Staging Environment

**File**: `appsettings.Staging.json`

```json
{
  "AzureKeyVault": {
    "VaultUri": "https://kv-hartonomous-staging.vault.azure.net/"
  },
  "AzureAppConfiguration": {
    "Endpoint": "https://appconfig-hartonomous-staging.azconfig.io"
  },
  "Ooda": {
    "AnalysisIntervalMinutes": 10
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=staging-key;IngestionEndpoint=https://eastus-1.in.applicationinsights.azure.com/"
  }
}
```

### Production Environment

**File**: `appsettings.Production.json`

```json
{
  "AzureKeyVault": {
    "VaultUri": "https://kv-hartonomous-prod.vault.azure.net/"
  },
  "AzureAppConfiguration": {
    "Endpoint": "https://appconfig-hartonomous-prod.azconfig.io"
  },
  "Ooda": {
    "AnalysisIntervalMinutes": 15,
    "RiskThreshold": 0.3
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=prod-key;IngestionEndpoint=https://eastus-1.in.applicationinsights.azure.com/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Hartonomous": "Information"
    }
  }
}
```

**Connection Strings** (stored in Key Vault, loaded at runtime):
- `SqlConnectionString`
- `Neo4jConnectionString`
- `Neo4jUsername`
- `Neo4jPassword`

---

## Certificate Deployment

### CLR Assembly Signing Certificate

SQL Server CLR assemblies require strong-name signing for UNSAFE permission sets.

#### 1. Generate Strong-Name Key

```powershell
# Generate strong-name key pair
sn.exe -k "Hartonomous.snk"

# Extract public key
sn.exe -p "Hartonomous.snk" "HartonomousPublicKey.snk"

# Display public key token
sn.exe -t "HartonomousPublicKey.snk"
# Output: Public key token is b77a5c561934e089
```

#### 2. Sign CLR Assembly

**File**: `src\Hartonomous.Database\Hartonomous.Database.sqlproj`

```xml
<PropertyGroup>
  <AssemblyOriginatorKeyFile>..\..\Hartonomous.snk</AssemblyOriginatorKeyFile>
  <SignAssembly>true</SignAssembly>
</PropertyGroup>
```

#### 3. Deploy Certificate to SQL Server

```sql
-- Create certificate from public key
USE master;
GO

CREATE CERTIFICATE HartonomousCertificate
FROM FILE = 'D:\Repositories\Hartonomous\HartonomousPublicKey.snk';
GO

-- Create login from certificate
CREATE LOGIN HartonomousCertLogin
FROM CERTIFICATE HartonomousCertificate;
GO

-- Grant UNSAFE ASSEMBLY permission
GRANT UNSAFE ASSEMBLY TO HartonomousCertLogin;
GO
```

### SSL/TLS Certificates (Production)

For production HTTPS endpoints and SQL Server encrypted connections:

#### 1. Obtain Certificate

**Option A**: Azure Key Vault Certificate
```powershell
# Create self-signed cert in Key Vault
az keyvault certificate create \
    --vault-name "kv-hartonomous-prod" \
    --name "hartonomous-ssl" \
    --policy '@policy.json'
```

**Option B**: Let's Encrypt (free)
```bash
# Install certbot
sudo apt install certbot

# Obtain certificate
sudo certbot certonly --standalone -d api.hartonomous.com
```

#### 2. Deploy to SQL Server

```powershell
# Import certificate to Windows Certificate Store
Import-Certificate -FilePath "hartonomous.pfx" -CertStoreLocation Cert:\LocalMachine\My -Password $securePassword

# Get certificate thumbprint
Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*hartonomous*"}

# Configure SQL Server to use certificate
$thumbprint = "ABC123..."
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQLServer\SuperSocketNetLib" -Name "Certificate" -Value $thumbprint

# Restart SQL Server
Restart-Service MSSQLSERVER
```

---

## Migration Procedures

### Database Schema Migration

#### Version-Based Migration

Hartonomous uses **DACPAC schema-diff deployment** (not EF migrations):

1. **Modify Schema**: Edit `.sql` files in `src\Hartonomous.Database\Tables\`, etc.
2. **Build DACPAC**: Run `build-dacpac.ps1`
3. **Review Changes**: SqlPackage generates diff script automatically
4. **Deploy**: Run `deploy-dacpac.ps1` (applies diff)

**Example: Add New Column**

```sql
-- File: src\Hartonomous.Database\Tables\Atom.sql

ALTER TABLE dbo.Atom
ADD LastAccessedAt DATETIME2 NULL;
GO
```

Build and deploy:
```powershell
.\scripts\build-dacpac.ps1
.\scripts\deploy-dacpac.ps1 -Server localhost -Database Hartonomous -IntegratedSecurity
```

SqlPackage automatically generates:
```sql
-- Auto-generated by SqlPackage
ALTER TABLE [dbo].[Atom] ADD [LastAccessedAt] DATETIME2 NULL;
```

#### Data Migration

For complex data migrations, use **Pre/Post-Deployment Scripts**:

**File**: `src\Hartonomous.Database\Scripts\PostDeployment.sql`

```sql
-- Migrate existing data
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Atom') AND name = 'LastAccessedAt')
BEGIN
    -- Backfill LastAccessedAt from CreatedAt for existing rows
    UPDATE dbo.Atom
    SET LastAccessedAt = CreatedAt
    WHERE LastAccessedAt IS NULL;
END
GO
```

### CLR Assembly Versioning

#### Update CLR Code

1. **Modify CLR Functions**: Edit C# files in `src\Hartonomous.Database\CLR\`
2. **Increment Assembly Version**: Update `AssemblyVersion` in `AssemblyInfo.cs`
3. **Rebuild DACPAC**: Run `build-dacpac.ps1`
4. **Deploy**: DACPAC deployment automatically updates CLR assembly

**Example: Update VectorMath Function**

```csharp
// File: src\Hartonomous.Database\CLR\VectorMath.cs

[SqlFunction(IsDeterministic = true, IsPrecise = false)]
public static SqlDouble DotProduct(SqlBytes vectorA, SqlBytes vectorB)
{
    // Updated implementation with SIMD optimization
    // ...
}
```

DACPAC deployment generates:
```sql
-- Auto-generated: Drop existing function
DROP FUNCTION IF EXISTS dbo.clr_DotProduct;
GO

-- Auto-generated: Recreate assembly (new version)
ALTER ASSEMBLY [Hartonomous.Clr]
FROM 0x4D5A90... (new hex binary)
WITH PERMISSION_SET = UNSAFE;
GO

-- Auto-generated: Recreate function
CREATE FUNCTION dbo.clr_DotProduct(@vectorA VARBINARY(MAX), @vectorB VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Database.CLR.VectorMath].[DotProduct];
GO
```

### Zero-Downtime Deployment

For production environments, use **blue-green deployment**:

#### 1. Deploy to Staging

```powershell
.\scripts\deploy-dacpac.ps1 -Server "HART-STAGING" -Database "Hartonomous" -AccessToken $token
```

#### 2. Run Smoke Tests

```powershell
# Test critical endpoints
Invoke-WebRequest -Uri "https://staging-api.hartonomous.com/health" -UseBasicParsing
Invoke-WebRequest -Uri "https://staging-api.hartonomous.com/health/database" -UseBasicParsing
Invoke-WebRequest -Uri "https://staging-api.hartonomous.com/health/neo4j" -UseBasicParsing

# Test inference
Invoke-RestMethod -Method Post -Uri "https://staging-api.hartonomous.com/api/inference" `
    -Headers @{ "Authorization" = "Bearer $token" } `
    -Body '{"prompt": "Test query", "maxTokens": 10}'
```

#### 3. Swap to Production

**DNS cutover** (Azure Traffic Manager or load balancer):
```powershell
# Point production DNS to new deployment
az network traffic-manager endpoint update `
    --name "staging" `
    --profile-name "hartonomous-tm" `
    --resource-group "rg-hartonomous-prod" `
    --type azureEndpoints `
    --priority 1
```

#### 4. Monitor and Rollback if Needed

```powershell
# Monitor Application Insights
az monitor app-insights metrics show `
    --app "hartonomous-prod" `
    --metric "requests/failed" `
    --start-time "2025-01-01T00:00:00Z" `
    --end-time "2025-01-01T01:00:00Z"

# Rollback if needed (swap DNS back)
az network traffic-manager endpoint update `
    --name "production" `
    --priority 1
```

---

## Troubleshooting

### DACPAC Build Failures

#### Error: MSBuild not found

```
MSBuild not found. Install Visual Studio 2022 or Build Tools.
```

**Solution**:
```powershell
# Install Visual Studio 2022 Community
# OR install Build Tools for Visual Studio 2022
winget install Microsoft.VisualStudio.2022.BuildTools

# Verify MSBuild installation
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -find MSBuild\**\Bin\MSBuild.exe
```

#### Error: CLR assembly compilation failed

```
Error CS0234: The type or namespace name 'Numerics' does not exist in the namespace 'System'
```

**Solution**: Missing .NET Framework 4.8.1 SDK
```powershell
# Install .NET Framework 4.8.1 Developer Pack
winget install Microsoft.DotNet.Framework.DeveloperPack_4.8.1
```

### CLR Deployment Failures

#### Error: Assembly dependency not found

```
Msg 6501, Level 16, State 1
Assembly 'System.Memory' not found.
```

**Solution**: Deploy dependencies in correct order
```powershell
# Deploy external assemblies FIRST
.\scripts\deploy-clr-assemblies.ps1 -Server localhost -Database master

# THEN deploy DACPAC
.\scripts\deploy-dacpac.ps1 -Server localhost -Database Hartonomous -IntegratedSecurity
```

#### Error: UNSAFE ASSEMBLY permission denied

```
Msg 10327, Level 14, State 1
CREATE ASSEMBLY for assembly failed because assembly is malformed or not a pure .NET assembly.
```

**Solution**: Enable CLR and disable strict security
```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

-- Set database TRUSTWORTHY
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
```

### Azure Arc Authentication Failures

#### Error: Service principal authentication failed

```
Login failed for user '<token-identified principal>'.
```

**Solution**: Verify service principal has SQL permissions
```sql
-- Check if login exists
SELECT name, type_desc, is_disabled
FROM sys.server_principals
WHERE name = 'Hartonomous-GitHub-Actions-SP';

-- If missing, run Configure-GitHubActionsSqlPermissions.sql
```

#### Error: Federated credential invalid

```
AADSTS700024: Client assertion is not within its valid time range.
```

**Solution**: Check federated credential subject matches repository
```powershell
# Verify federated credential
az ad app federated-credential show `
    --id $clientId `
    --federated-credential-id $credentialId

# Expected subject: repo:AHartTN/Hartonomous-Sandbox:ref:refs/heads/main
```

### GitHub Actions Runner Issues

#### Runner offline

**Solution**: Restart runner service
```powershell
# Windows
Restart-Service -Name "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# Linux
sudo systemctl restart actions.runner.AHartTN-Hartonomous-Sandbox.hart-server.service
```

#### Runner authentication failed

**Solution**: Re-configure runner
```powershell
# Remove old runner
.\config.cmd remove --token YOUR_REMOVAL_TOKEN

# Reconfigure with new token
.\config.cmd --url https://github.com/AHartTN/Hartonomous-Sandbox --token YOUR_NEW_TOKEN --labels self-hosted,windows,sql-server
```

### Database Connection Issues

#### Error: SQL Server not accepting connections

```
A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

**Solution**: Enable TCP/IP protocol
```powershell
# Enable TCP/IP in SQL Server Configuration Manager
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp' -Name Enabled -Value 1

# Restart SQL Server
Restart-Service MSSQLSERVER
```

#### Error: Certificate validation failed

```
The certificate chain was issued by an authority that is not trusted.
```

**Solution**: Add `TrustServerCertificate=true` to connection string
```
Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;
```

---

## Summary

**Key Deployment Steps**:

1. ✅ **Azure Infrastructure**: Create Key Vault, App Configuration, Entra ID app registrations
2. ✅ **Azure Arc**: Install agent, enable SQL extension with `OUTBOUND AND INBOUND`, create service principal
3. ✅ **DACPAC Build**: Compile CLR code + T-SQL → DACPAC artifact
4. ✅ **CLR Deployment**: Deploy 16 external assemblies in dependency order
5. ✅ **Database Deployment**: SqlPackage publish DACPAC with access token authentication
6. ✅ **CI/CD**: Configure GitHub Actions runners, OIDC workload identity federation

**Production Deployment Timeline**: 4-6 hours (including Azure setup)

**Next Steps**:
- See `docs/operations/monitoring.md` for Application Insights integration
- See `docs/operations/backup-recovery.md` for disaster recovery procedures
- See `docs/operations/kernel-seeding.md` for cognitive kernel bootstrap
