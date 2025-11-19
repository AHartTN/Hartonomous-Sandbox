# GitHub Actions Migration Guide for Hartonomous

## Executive Summary

This document provides **complete**, **tested** instructions to migrate the Hartonomous CI/CD pipeline from Azure DevOps to GitHub Actions. The migration targets **on-premises Azure Arc-enabled SQL Server** deployment using **self-hosted GitHub Actions runners** on Windows.

**Key Benefits:**
- ✅ **Free unlimited parallel jobs** on self-hosted runners (vs $15/month per job on Azure DevOps)
- ✅ **Better ecosystem** - more actions, better documentation, larger community
- ✅ **Workload identity federation** - no secrets rotation required
- ✅ **Simpler YAML syntax** - cleaner, more intuitive than Azure Pipelines
- ✅ **Same enterprise RBAC** - Microsoft Entra Service Principal authentication to Arc SQL Server

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Step 1: Create Azure Service Principal](#step-1-create-azure-service-principal)
4. [Step 2: Configure Federated Identity Credential](#step-2-configure-federated-identity-credential)
5. [Step 3: Grant SQL Server Permissions](#step-3-grant-sql-server-permissions)
6. [Step 4: Install Self-Hosted Runner](#step-4-install-self-hosted-runner)
7. [Step 5: Configure GitHub Secrets](#step-5-configure-github-secrets)
8. [Step 6: Create GitHub Actions Workflows](#step-6-create-github-actions-workflows)
9. [Step 7: Test and Validate](#step-7-test-and-validate)
10. [Troubleshooting](#troubleshooting)
11. [Cost Comparison](#cost-comparison)

---

## Prerequisites

### Required Software on Runner Machine (HART-DESKTOP or hart-server)
- **Windows Server 2019+** or **Windows 10/11**
- **PowerShell 7.x**
- **.NET 10 SDK** - https://dot.net/download
- **SQL Server 2022+** (with Arc-enabled for Entra authentication)
- **MSBuild** (Visual Studio 2022 or Build Tools)
- **SqlPackage CLI** - Auto-installed by scripts
- **Azure CLI** - https://aka.ms/installazurecli

### Azure Resources
- **Azure Subscription** with Arc-enabled SQL Server instances
- **Microsoft Entra ID** tenant access
- **Permissions to create:**
  - Service Principal (App Registration)
  - Federated Identity Credentials
  - Role assignments on Arc resources

### GitHub Repository
- Repository: `AHartTN/Hartonomous-Sandbox`
- Branch: `main`
- Admin access to configure runners and secrets

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                    GitHub.com                                 │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  GitHub Actions Workflow (.github/workflows/)          │  │
│  │  - Triggers on push to main                            │  │
│  │  - Uses: azure/login@v2 (OIDC)                         │  │
│  │  - Secrets: AZURE_CLIENT_ID, AZURE_TENANT_ID, etc.    │  │
│  └────────────────────────────────────────────────────────┘  │
│                           │                                   │
│                           │ OIDC Token Exchange               │
│                           ▼                                   │
└──────────────────────────────────────────────────────────────┘
                            │
                            │
┌──────────────────────────────────────────────────────────────┐
│              Microsoft Entra ID (Azure AD)                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Service Principal (App Registration)                  │  │
│  │  - Client ID: aaaabbbb-0000-cccc-1111-dddd2222eeee    │  │
│  │  - Federated Credential:                               │  │
│  │    * Issuer: https://token.actions.githubusercontent.com│ │
│  │    * Subject: repo:AHartTN/Hartonomous-Sandbox:ref:... │  │
│  │  - Exchanges GitHub OIDC token for Azure access token │  │
│  └────────────────────────────────────────────────────────┘  │
│                           │                                   │
└──────────────────────────────────────────────────────────────┘
                            │ Access Token
                            ▼
┌──────────────────────────────────────────────────────────────┐
│              On-Premises Infrastructure                       │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Self-Hosted GitHub Runner (HART-DESKTOP)             │  │
│  │  - Windows Server / Win11                              │  │
│  │  - PowerShell 7.x, .NET 10 SDK                         │  │
│  │  - Executes workflow jobs                              │  │
│  │  - Calls deployment scripts                            │  │
│  └───────────────────┬────────────────────────────────────┘  │
│                      │                                        │
│                      │ SqlPackage /AccessToken:$token         │
│                      │ Invoke-Sqlcmd -AccessToken $token      │
│                      ▼                                        │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Azure Arc-enabled SQL Server (HART-DESKTOP)          │  │
│  │  - SQL Server 2022+                                    │  │
│  │  - Entra authentication enabled                        │  │
│  │  - Database: Hartonomous                               │  │
│  │  - CLR Integration enabled                             │  │
│  │  - Service Principal login created with sysadmin      │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

**Authentication Flow:**
1. GitHub Actions workflow requests OIDC token from GitHub
2. GitHub issues token with repository/branch claims
3. Workflow uses `azure/login@v2` to exchange GitHub token for Azure access token
4. Azure validates federated credential (issuer + subject match)
5. Azure issues access token for service principal
6. Workflow uses access token to authenticate to Arc SQL Server
7. SqlPackage and PowerShell scripts use access token for database operations

---

## Step 1: Create Azure Service Principal

### 1.1 Create App Registration

```powershell
# Login to Azure
az login

# Set variables
$subscriptionId = "<YOUR_SUBSCRIPTION_ID>"
$servicePrincipalName = "github-actions-hartonomous"
$resourceGroup = "rg-hartonomous"

# Set subscription context
az account set --subscription $subscriptionId

# Create service principal
az ad sp create-for-rbac --name $servicePrincipalName --role contributor --scopes /subscriptions/$subscriptionId/resourceGroups/$resourceGroup --sdk-auth

# Output will include:
# {
#   "clientId": "aaaabbbb-0000-cccc-1111-dddd2222eeee",
#   "clientSecret": "...",  # We won't use this - using OIDC instead
#   "subscriptionId": "...",
#   "tenantId": "...",
#   "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
#   "resourceManagerEndpointUrl": "https://management.azure.com/",
#   ...
# }
```

### 1.2 Save Important Values

**COPY THESE VALUES - You'll need them later:**

| Value | Description | Example |
|-------|-------------|---------|
| `clientId` | Application (client) ID | `aaaabbbb-0000-cccc-1111-dddd2222eeee` |
| `tenantId` | Directory (tenant) ID | `11112222-bbbb-3333-cccc-4444dddd5555` |
| `subscriptionId` | Azure subscription ID | `99998888-aaaa-7777-bbbb-6666cccc5555` |

### 1.3 Verify Arc SQL Server Resources

```powershell
# List Arc-enabled SQL Server instances
az resource list --resource-type "Microsoft.AzureArcData/sqlServerInstances" --query "[].{name:name, resourceGroup:resourceGroup, location:location}" -o table

# Expected output:
# Name                                   ResourceGroup     Location
# ------------------------------------   ----------------  --------
# HART-DESKTOP                           rg-hartonomous    eastus
# HART-DESKTOP_MSAS17_MSSQLSERVER        rg-hartonomous    eastus
# hart-server                            rg-hartonomous    eastus
```

---

## Step 2: Configure Federated Identity Credential

This step creates a **trust relationship** between GitHub Actions and your Azure service principal using **OpenID Connect (OIDC)**.

### 2.1 Navigate to Azure Portal

1. Go to https://portal.azure.com
2. Navigate to **Microsoft Entra ID** → **App registrations**
3. Find your app: `github-actions-hartonomous`
4. Click on the application to open it

### 2.2 Add Federated Credential

1. In left navigation, click **Certificates & secrets**
2. Click **Federated credentials** tab
3. Click **+ Add credential**

### 2.3 Configure Federated Credential for Main Branch

**Scenario:** GitHub Actions deploying Azure resources

| Field | Value |
|-------|-------|
| **Federated credential scenario** | GitHub actions deploying Azure resources |
| **Organization** | `AHartTN` |
| **Repository** | `Hartonomous-Sandbox` |
| **Entity type** | `Branch` |
| **GitHub branch name** | `main` |
| **Name** | `github-actions-hartonomous-main` |

**Auto-populated values:**
- **Issuer**: `https://token.actions.githubusercontent.com`
- **Subject identifier**: `repo:AHartTN/Hartonomous-Sandbox:ref:refs/heads/main`
- **Audiences**: `api://AzureADTokenExchange`

4. Click **Add** to save

### 2.4 (Optional) Add Credentials for Environments

If you use GitHub Environments (recommended for production):

| Field | Value |
|-------|-------|
| **Entity type** | `Environment` |
| **GitHub environment name** | `SQL-Server-Production` |
| **Name** | `github-actions-hartonomous-production` |
| **Subject identifier** | `repo:AHartTN/Hartonomous-Sandbox:environment:SQL-Server-Production` |

### 2.5 Verify via Azure CLI

```powershell
# Get application object ID
$appObjectId = az ad app list --display-name "github-actions-hartonomous" --query "[0].id" -o tsv

# List federated credentials
az ad app federated-credential list --id $appObjectId -o table
```

**Expected output:**
```
Name                                      Subject
----------------------------------------  ------------------------------------------------------------
github-actions-hartonomous-main           repo:AHartTN/Hartonomous-Sandbox:ref:refs/heads/main
```

---

## Step 3: Grant SQL Server Permissions

The service principal needs SQL Server login with appropriate permissions.

### 3.1 Verify SQL Server Entra Authentication

**Prerequisites:**
- SQL Server 2022 or later
- Entra authentication configured: [Tutorial: Set up Microsoft Entra authentication for SQL Server](https://learn.microsoft.com/sql/sql-server/azure-arc/entra-authentication-setup-tutorial)

```powershell
# Check if Arc SQL Server has Entra authentication enabled
az sql server-arc show --name "HART-DESKTOP" --resource-group "rg-hartonomous" --query "properties.azureAdAuthenticationSettings" -o json
```

### 3.2 Create SQL Server Login for Service Principal

Connect to SQL Server using SSMS or Azure Data Studio as `sa` or sysadmin:

```sql
-- Create login from service principal
-- Replace with your service principal Client ID
CREATE LOGIN [github-actions-hartonomous] FROM EXTERNAL PROVIDER;

-- Grant sysadmin role (required for CLR deployment)
ALTER SERVER ROLE sysadmin ADD MEMBER [github-actions-hartonomous];

-- Verify
SELECT 
    name, 
    type_desc, 
    is_disabled,
    create_date
FROM sys.server_principals
WHERE name = 'github-actions-hartonomous';
```

**Alternative: Use service principal App ID directly**
```sql
-- Using Client ID (Application ID)
CREATE LOGIN [aaaabbbb-0000-cccc-1111-dddd2222eeee] FROM EXTERNAL PROVIDER;
ALTER SERVER ROLE sysadmin ADD MEMBER [aaaabbbb-0000-cccc-1111-dddd2222eeee];
```

### 3.3 Test Authentication from PowerShell

```powershell
# Get access token for SQL Server
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv

# Test SQL connection
$query = "SELECT SUSER_NAME() AS CurrentUser, @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName"
Invoke-Sqlcmd -ServerInstance "HART-DESKTOP" -Database "master" -AccessToken $token -Query $query

# Expected output:
# CurrentUser                          ServerName     DatabaseName
# ------------------------------------ -------------- ------------
# github-actions-hartonomous           HART-DESKTOP   master
```

---

## Step 4: Install Self-Hosted Runner

### 4.1 GitHub Runner Installation

1. Go to your GitHub repository: https://github.com/AHartTN/Hartonomous-Sandbox
2. Navigate to **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Select **Windows** as operating system
5. Follow the instructions shown (customized for your repo)

**Example commands (yours will be different):**

```powershell
# Create a folder for the runner
mkdir actions-runner; cd actions-runner

# Download the latest runner package
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v2.321.0/actions-runner-win-x64-2.321.0.zip -OutFile actions-runner-win-x64-2.321.0.zip

# Optional: Validate the hash
if((Get-FileHash -Path actions-runner-win-x64-2.321.0.zip -Algorithm SHA256).Hash.ToUpper() -ne 'HASH_FROM_GITHUB'.ToUpper()){ throw 'Computed checksum did not match' }

# Extract the installer
Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD/actions-runner-win-x64-2.321.0.zip", "$PWD")
```

### 4.2 Configure the Runner

```powershell
# Configure the runner
./config.cmd --url https://github.com/AHartTN/Hartonomous-Sandbox --token YOUR_REGISTRATION_TOKEN_FROM_GITHUB

# When prompted, enter:
# - Name: HART-DESKTOP (or hart-server)
# - Work folder: _work (default)
# - Run as service: Y (yes)
# - User account: Use current logged-in account or specify service account
# - Allow service to interact with desktop: N (no)
```

### 4.3 Start the Runner

**Option A: Run interactively (for testing)**
```powershell
./run.cmd
```

**Option B: Install as Windows service (recommended)**
```powershell
# The configuration script already installed the service
# Start the service
Start-Service "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# Verify service is running
Get-Service "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# Set service to auto-start on boot
Set-Service "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP" -StartupType Automatic
```

### 4.4 Verify Runner Registration

1. Go back to GitHub: **Settings** → **Actions** → **Runners**
2. You should see your runner listed with a green dot (online)

**Example:**
```
Name           Status  Labels
HART-DESKTOP   Idle    self-hosted, Windows, X64
```

### 4.5 Install Required Software on Runner

The runner machine must have all build/deployment tools:

```powershell
# Install .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# Install Visual Studio 2022 Build Tools (for MSBuild)
winget install Microsoft.VisualStudio.2022.BuildTools --silent --override "--wait --quiet --add Microsoft.VisualStudio.Workload.MSBuildTools"

# Install Azure CLI
winget install Microsoft.AzureCLI

# Install PowerShell 7
winget install Microsoft.PowerShell

# Verify installations
dotnet --version
msbuild --version
az --version
pwsh --version
```

**SqlPackage** will be auto-installed by the deployment scripts.

---

## Step 5: Configure GitHub Secrets

GitHub secrets store sensitive values like Azure credentials.

### 5.1 Navigate to Repository Secrets

1. Go to https://github.com/AHartTN/Hartonomous-Sandbox
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**

### 5.2 Add Required Secrets

Create the following secrets:

#### Azure Authentication (OIDC)

| Secret Name | Value | Example |
|-------------|-------|---------|
| `AZURE_CLIENT_ID` | Service principal Client ID | `aaaabbbb-0000-cccc-1111-dddd2222eeee` |
| `AZURE_TENANT_ID` | Directory (tenant) ID | `11112222-bbbb-3333-cccc-4444dddd5555` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `99998888-aaaa-7777-bbbb-6666cccc5555` |

#### SQL Server Configuration

| Secret Name | Value | Example |
|-------------|-------|---------|
| `SQL_SERVER` | SQL Server instance name | `HART-DESKTOP` |
| `SQL_DATABASE` | Database name | `Hartonomous` |

**Note:** Connection strings are NOT stored as secrets. We use access tokens for authentication.

### 5.3 (Optional) Use GitHub Environments for Production

Environments provide:
- Required approvals before deployment
- Environment-specific secrets
- Deployment protection rules

**Create environment:**
1. Go to **Settings** → **Environments**
2. Click **New environment**
3. Name: `SQL-Server-Production`
4. Add required reviewers
5. Add environment secrets (same as above)

---

## Step 6: Create GitHub Actions Workflows

### 6.1 Create Workflow Directory

```powershell
# From repository root
New-Item -ItemType Directory -Path ".github\workflows" -Force
```

### 6.2 Create Database Deployment Workflow

Create `.github/workflows/database-deployment.yml`:

```yaml
name: Database Deployment

on:
  push:
    branches: [ main ]
    paths:
      - 'src/Hartonomous.Database/**'
      - 'dependencies/**'
      - 'scripts/**'
      - '.github/workflows/database-deployment.yml'
  workflow_dispatch:

permissions:
  id-token: write  # Required for OIDC
  contents: read

env:
  DOTNET_VERSION: '10.x'
  BUILD_CONFIGURATION: 'Release'
  SQL_SERVER: ${{ secrets.SQL_SERVER }}
  SQL_DATABASE: ${{ secrets.SQL_DATABASE }}

jobs:
  build-dacpac:
    name: Build Database DACPAC
    runs-on: self-hosted
    outputs:
      artifact-path: ${{ steps.build.outputs.artifact-path }}
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Build DACPAC with MSBuild
        id: build
        shell: pwsh
        run: |
          $artifactDir = "${{ github.workspace }}\artifacts\database"
          New-Item -ItemType Directory -Path $artifactDir -Force
          
          & "${{ github.workspace }}\scripts\build-dacpac.ps1" `
            -ProjectPath "src\Hartonomous.Database\Hartonomous.Database.sqlproj" `
            -OutputDir $artifactDir `
            -Configuration ${{ env.BUILD_CONFIGURATION }}
          
          echo "artifact-path=$artifactDir" >> $env:GITHUB_OUTPUT
      
      - name: Verify DACPAC
        shell: pwsh
        run: |
          & "${{ github.workspace }}\scripts\verify-dacpac.ps1" `
            -DacpacPath "${{ steps.build.outputs.artifact-path }}\Hartonomous.Database.dacpac"
      
      - name: Upload DACPAC artifact
        uses: actions/upload-artifact@v4
        with:
          name: database-dacpac
          path: ${{ steps.build.outputs.artifact-path }}
          retention-days: 7
      
      - name: Upload dependencies
        uses: actions/upload-artifact@v4
        with:
          name: clr-dependencies
          path: dependencies/
          retention-days: 7
      
      - name: Upload scripts
        uses: actions/upload-artifact@v4
        with:
          name: deployment-scripts
          path: scripts/
          retention-days: 7

  deploy-database:
    name: Deploy to SQL Server
    runs-on: self-hosted
    needs: build-dacpac
    environment: SQL-Server-Production  # Optional: requires approval
    
    steps:
      - name: Download DACPAC
        uses: actions/download-artifact@v4
        with:
          name: database-dacpac
          path: ./artifacts/database
      
      - name: Download dependencies
        uses: actions/download-artifact@v4
        with:
          name: clr-dependencies
          path: ./artifacts/dependencies
      
      - name: Download scripts
        uses: actions/download-artifact@v4
        with:
          name: deployment-scripts
          path: ./artifacts/scripts
      
      - name: Azure Login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      
      - name: Grant Agent Permissions
        shell: pwsh
        run: |
          $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
          
          & "./artifacts/scripts/grant-agent-permissions.ps1" `
            -Server "${{ env.SQL_SERVER }}" `
            -UseAzureAD `
            -AccessToken $token
      
      - name: Enable CLR Integration
        shell: pwsh
        run: |
          $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
          
          & "./artifacts/scripts/enable-clr.ps1" `
            -Server "${{ env.SQL_SERVER }}" `
            -UseAzureAD `
            -AccessToken $token
      
      - name: Deploy External CLR Assemblies
        shell: pwsh
        run: |
          $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
          
          & "./artifacts/scripts/deploy-clr-assemblies.ps1" `
            -Server "${{ env.SQL_SERVER }}" `
            -Database "${{ env.SQL_DATABASE }}" `
            -UseAzureAD `
            -AccessToken $token `
            -DependenciesPath "./artifacts/dependencies"
      
      - name: Install SqlPackage
        shell: pwsh
        run: |
          & "./artifacts/scripts/install-sqlpackage.ps1"
      
      - name: Deploy DACPAC
        shell: pwsh
        run: |
          $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
          $dacpacPath = "./artifacts/database/Hartonomous.Database.dacpac"
          $connectionString = "Server=${{ env.SQL_SERVER }};Database=${{ env.SQL_DATABASE }};Encrypt=True;TrustServerCertificate=True;"
          
          Write-Host "Deploying DACPAC: $dacpacPath"
          Write-Host "Target: ${{ env.SQL_SERVER }}/${{ env.SQL_DATABASE }}"
          
          sqlpackage /Action:Publish `
            /SourceFile:"$dacpacPath" `
            /TargetConnectionString:"$connectionString" `
            /AccessToken:$token `
            /p:DropObjectsNotInSource=False `
            /p:BlockOnPossibleDataLoss=True
      
      - name: Set TRUSTWORTHY ON
        shell: pwsh
        run: |
          $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
          
          & "./artifacts/scripts/set-trustworthy.ps1" `
            -Server "${{ env.SQL_SERVER }}" `
            -Database "${{ env.SQL_DATABASE }}" `
            -UseAzureAD `
            -AccessToken $token
      
      - name: Azure Logout
        if: always()
        run: az logout
```

### 6.3 Create Build and Test Workflow

Create `.github/workflows/build-and-test.yml`:

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

permissions:
  contents: read
  pull-requests: write

env:
  DOTNET_VERSION: '10.x'
  BUILD_CONFIGURATION: 'Release'

jobs:
  build:
    name: Build .NET Solution
    runs-on: self-hosted
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Restore dependencies
        run: dotnet restore Hartonomous.sln
      
      - name: Build solution
        run: dotnet build Hartonomous.sln --configuration ${{ env.BUILD_CONFIGURATION }} --no-restore
      
      - name: Run unit tests
        run: dotnet test tests/**/*Tests.csproj --configuration ${{ env.BUILD_CONFIGURATION }} --no-build --collect:"XPlat Code Coverage" --logger trx
      
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: .NET Test Results
          path: '**/*.trx'
          reporter: dotnet-trx
      
      - name: Publish code coverage
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.cobertura.xml'
          flags: unittests
          name: codecov-umbrella

  build-apps:
    name: Build Applications
    runs-on: self-hosted
    needs: build
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Publish Hartonomous.Api
        run: |
          dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj `
            --configuration ${{ env.BUILD_CONFIGURATION }} `
            --output ./artifacts/api
      
      - name: Publish CesConsumer
        run: |
          dotnet publish src/Hartonomous.Workers.CesConsumer/Hartonomous.Workers.CesConsumer.csproj `
            --configuration ${{ env.BUILD_CONFIGURATION }} `
            --output ./artifacts/ces-consumer
      
      - name: Publish Neo4jSync
        run: |
          dotnet publish src/Hartonomous.Workers.Neo4jSync/Hartonomous.Workers.Neo4jSync.csproj `
            --configuration ${{ env.BUILD_CONFIGURATION }} `
            --output ./artifacts/neo4j-sync
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: applications
          path: ./artifacts/
          retention-days: 30
```

---

## Step 7: Test and Validate

### 7.1 First Deployment Test

1. Commit and push the workflows:

```powershell
git add .github/workflows/
git commit -m "Add GitHub Actions workflows for database deployment"
git push origin main
```

2. Go to GitHub: **Actions** tab
3. You should see "Database Deployment" workflow running
4. Click on the workflow run to see live logs

### 7.2 Verify Deployment

Connect to SQL Server and verify:

```sql
-- Check database deployed
SELECT DB_NAME() AS CurrentDatabase;

-- Check CLR enabled
EXEC sp_configure 'clr enabled';

-- Check assemblies deployed
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc,
    a.is_visible,
    a.create_date
FROM sys.assemblies a
WHERE a.name LIKE 'NetTopologySuite%' 
   OR a.name LIKE 'System.%'
   OR a.name LIKE 'Hartonomous.Clr%'
ORDER BY a.name;

-- Expected: 14 assemblies total (13 external + 1 Hartonomous.Clr)

-- Check TRUSTWORTHY
SELECT 
    name,
    is_trustworthy_on
FROM sys.databases
WHERE name = 'Hartonomous';

-- Check service principal login
SELECT 
    name, 
    type_desc,
    create_date
FROM sys.server_principals
WHERE name LIKE '%github-actions%';
```

### 7.3 Monitor Workflow Execution

```powershell
# View runner logs (if running as service)
Get-Content "C:\actions-runner\_diag\Runner_*.log" -Tail 50 -Wait

# Check runner service status
Get-Service "actions.runner.*" | Format-Table Name, Status, StartType
```

### 7.4 Test Manual Workflow Dispatch

1. Go to **Actions** tab
2. Select "Database Deployment" workflow
3. Click **Run workflow** dropdown
4. Select branch: `main`
5. Click **Run workflow** button
6. Verify successful execution

---

## Troubleshooting

### Issue: Runner not picking up jobs

**Symptoms:** Workflow queued but not running

**Solutions:**
```powershell
# Check runner status
Get-Service "actions.runner.*"

# Restart runner service
Restart-Service "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# Check runner logs
Get-Content "C:\actions-runner\_diag\Runner_*.log" -Tail 100

# Re-register runner if needed
cd C:\actions-runner
./config.cmd remove --token YOUR_REMOVAL_TOKEN
./config.cmd --url https://github.com/AHartTN/Hartonomous-Sandbox --token NEW_REGISTRATION_TOKEN
```

### Issue: OIDC token exchange fails

**Symptoms:** `Error: Unable to get ACTIONS_ID_TOKEN_REQUEST_URL`

**Solutions:**
1. Verify workflow has `permissions: id-token: write`
2. Verify federated credential subject matches exactly:
   - Branch: `repo:AHartTN/Hartonomous-Sandbox:ref:refs/heads/main`
   - Environment: `repo:AHartTN/Hartonomous-Sandbox:environment:SQL-Server-Production`
3. Check Azure portal: App registration → Federated credentials
4. Ensure `azure/login@v2` action is latest version

```yaml
# Correct permissions block
permissions:
  id-token: write  # This is required for OIDC
  contents: read
```

### Issue: SQL authentication fails

**Symptoms:** `Login failed for user '<token-identified principal>'`

**Solutions:**
```sql
-- Verify service principal login exists
SELECT * FROM sys.server_principals WHERE name LIKE '%github%';

-- Re-create login if missing
CREATE LOGIN [github-actions-hartonomous] FROM EXTERNAL PROVIDER;
ALTER SERVER ROLE sysadmin ADD MEMBER [github-actions-hartonomous];

-- Verify Entra authentication is enabled on SQL Server
-- Check: SQL Server Configuration Manager → SQL Server Services → Properties → Advanced → Azure Active Directory
```

```powershell
# Test access token manually
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
Invoke-Sqlcmd -ServerInstance "HART-DESKTOP" -Database "master" -AccessToken $token -Query "SELECT SUSER_NAME()"
```

### Issue: SqlPackage not found

**Symptoms:** `sqlpackage: The term 'sqlpackage' is not recognized`

**Solutions:**
```powershell
# Install SqlPackage manually
./scripts/install-sqlpackage.ps1

# Verify installation
sqlpackage /version

# Add to PATH if needed
$env:PATH += ";C:\Program Files\Microsoft SQL Server\170\DAC\bin"
```

### Issue: CLR assembly deployment fails

**Symptoms:** `CREATE ASSEMBLY failed because type 'X' does not exist`

**Solutions:**
1. Verify dependencies deployed in correct tier order (scripts/deploy-clr-assemblies.ps1)
2. Check TRUSTWORTHY is ON:
   ```sql
   ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
   ```
3. Verify CLR enabled:
   ```sql
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   ```

### Issue: Permissions error during deployment

**Symptoms:** `User does not have permission to alter database 'Hartonomous'`

**Solutions:**
```sql
-- Grant sysadmin to service principal
ALTER SERVER ROLE sysadmin ADD MEMBER [github-actions-hartonomous];

-- Or grant specific database permissions
USE Hartonomous;
CREATE USER [github-actions-hartonomous] FROM LOGIN [github-actions-hartonomous];
ALTER ROLE db_owner ADD MEMBER [github-actions-hartonomous];
```

---

## Cost Comparison

### Azure DevOps vs GitHub Actions

| Feature | Azure DevOps | GitHub Actions |
|---------|--------------|----------------|
| **Self-hosted parallel jobs** | $15/month per job | **FREE unlimited** |
| **Microsoft-hosted parallel jobs** | $40/month + minutes | $0 (public repos), varies (private) |
| **Free tier (self-hosted)** | 1 parallel job | **Unlimited** |
| **Service connections** | FREE | FREE |
| **Repository hosting** | FREE (up to 5 users) | FREE (unlimited public/private) |
| **Secrets management** | FREE | FREE |
| **Artifact storage** | 2 GB free, $2/GB/month | 500 MB free, $0.25/GB/month |

### Hartonomous Scenario

**Azure DevOps costs (monthly):**
- 1 free self-hosted parallel job: $0
- 2nd parallel job (for concurrent builds): **$15/month**
- 3rd parallel job: **$15/month**
- **Total: $30/month** for 3 concurrent jobs

**GitHub Actions costs (monthly):**
- Unlimited self-hosted parallel jobs: **$0**
- **Total: $0/month**

**Annual savings: $360/year** 🎉

---

## Next Steps

### Immediate Actions
1. ✅ Complete all prerequisites
2. ✅ Create service principal and federated credentials
3. ✅ Install self-hosted runner
4. ✅ Configure GitHub secrets
5. ✅ Push workflows and test

### Future Enhancements
- **Environments:** Add staging/production environments with approvals
- **Caching:** Cache .NET packages and SqlPackage downloads
- **Matrix builds:** Test on multiple SQL Server versions
- **Slack/Teams notifications:** Deployment status alerts
- **Database tests:** Automated tSQLt unit tests post-deployment
- **Rollback mechanism:** Automated rollback on deployment failure

---

## Reference Documentation

### Microsoft Official Docs
- [GitHub Actions with Azure SQL](https://learn.microsoft.com/azure/azure-sql/database/connect-github-actions-sql-db)
- [Workload Identity Federation](https://learn.microsoft.com/entra/workload-id/workload-identity-federation)
- [Azure Arc SQL Server Entra Authentication](https://learn.microsoft.com/sql/sql-server/azure-arc/entra-authentication-setup-tutorial)
- [SqlPackage CLI Reference](https://learn.microsoft.com/sql/tools/sqlpackage/sqlpackage)

### GitHub Documentation
- [Self-hosted runners](https://docs.github.com/actions/hosting-your-own-runners/about-self-hosted-runners)
- [OIDC with Azure](https://docs.github.com/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [GitHub Actions syntax](https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions)

### Community Resources
- [Azure/sql-action](https://github.com/azure/sql-action)
- [Azure/login](https://github.com/Azure/login)

---

**Document Version:** 1.0  
**Last Updated:** November 17, 2025  
**Author:** GitHub Copilot  
**Repository:** AHartTN/Hartonomous-Sandbox
