# Multi-Environment Deployment Strategy
**Phase 2.3: Worker Services Deployment Across All Environments**

**Last Updated**: November 21, 2025  
**Status**: Active Implementation  
**Related**: ENTERPRISE_ROLLOUT_PLAN.md Phase 2.3

---

## Executive Summary

This document defines the holistic deployment strategy for Hartonomous across **three environments**:

1. **Localhost Development** - Your workstation (HART-DESKTOP) for rapid development/testing
2. **GitHub CI/CD** - Automated build/test pipeline with GitHub Actions
3. **Azure Arc Hybrid Production** - HART-DESKTOP managed as Azure Arc-enabled server

The critical components requiring multi-environment coordination are:

- **SQL Server Service Broker** (Neo4jSyncQueue, IngestionQueue)
- **Neo4j Graph Database** (provenance schema)
- **.NET Worker Services** (Neo4jSyncWorker, CesConsumer)
- **Connection strings and configuration** (environment-specific)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEPLOYMENT ENVIRONMENTS                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────┐       ┌──────────────────────────┐       ┌──────────────────────────┐
│  1. LOCALHOST DEV        │       │  2. GITHUB CI/CD         │       │  3. AZURE ARC PRODUCTION │
│  (HART-DESKTOP)          │       │  (Build & Test Only)     │       │  (HART-DESKTOP Managed)  │
├──────────────────────────┤       ├──────────────────────────┤       ├──────────────────────────┤
│ SQL: localhost           │──────▶│ SQL: None                │──────▶│ SQL: HART-DESKTOP        │
│ Neo4j: localhost:7687    │       │ Neo4j: None              │       │ Neo4j: HART-DESKTOP:7687 │
│ Workers: VS Code Tasks   │       │ Workers: Artifact Build  │       │ Workers: Windows Service │
│ Config: appsettings.json │       │ Config: Not deployed     │       │ Config: appsettings.Prod │
│                          │       │                          │       │                          │
│ Purpose: Development     │       │ Purpose: CI/CD Pipeline  │       │ Purpose: Production      │
│ Deploy: Manual F5        │       │ Deploy: Auto on push     │       │ Deploy: Azure DevOps     │
└──────────────────────────┘       └──────────────────────────┘       └──────────────────────────┘
         ▲                                    ▲                                    ▲
         │                                    │                                    │
         └────────────────────────────────────┴────────────────────────────────────┘
                             Code Push → GitHub → Build → Deploy
```

---

## Environment 1: Localhost Development

### Current State (What Works Today)

✅ **SQL Server**: `localhost`, Database: `Hartonomous`  
✅ **Neo4j**: `bolt://localhost:7687`, Credentials: `neo4j/neo4jneo4j`  
✅ **Service Broker**: Objects exist in database schema  
✅ **Neo4j Schema**: Deployed via `Deploy-Neo4jSchema.ps1`  
✅ **Workers**: Can run via VS Code task or `dotnet run`

### Configuration Files

**File**: `src/Hartonomous.Workers.Neo4jSync/appsettings.json`
```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "neo4jneo4j",
    "Enabled": true
  }
}
```

### VS Code Task (Already Configured)

**File**: `.vscode/tasks.json`
```json
{
  "label": "Run Neo4jSyncWorker",
  "type": "shell",
  "command": "dotnet",
  "args": [
    "run",
    "--project",
    "src/Hartonomous.Workers.Neo4jSync/Hartonomous.Workers.Neo4jSync.csproj",
    "-c",
    "Release"
  ],
  "isBackground": true
}
```

**To Start Worker**:
1. Press `Ctrl+Shift+P` → "Tasks: Run Task"
2. Select "Run Neo4jSyncWorker"
3. Worker polls `Neo4jSyncQueue` every 5 seconds

### Service Broker Objects (Already Exist)

The database schema already includes:

```sql
-- Message Type
CREATE MESSAGE TYPE [Neo4jSyncRequest] VALIDATION = WELL_FORMED_XML;

-- Contract  
CREATE CONTRACT [Neo4jSyncContract] ([Neo4jSyncRequest] SENT BY INITIATOR);

-- Queue
CREATE QUEUE [dbo].[Neo4jSyncQueue] WITH STATUS = ON, RETENTION = OFF;

-- Service
CREATE SERVICE [Neo4jSyncService] 
  ON QUEUE [dbo].[Neo4jSyncQueue] 
  ([Neo4jSyncContract]);
```

**Location**: `src/Hartonomous.Database/ServiceBroker/`

### Testing Locally

#### Step 1: Verify Service Broker Enabled

```sql
SELECT name, is_broker_enabled 
FROM sys.databases 
WHERE name = 'Hartonomous';
-- Expected: is_broker_enabled = 1
```

#### Step 2: Verify Service Broker Objects Exist

```sql
-- Check Queue
SELECT name, is_activation_enabled, activation_procedure 
FROM sys.service_queues 
WHERE name = 'Neo4jSyncQueue';

-- Check Service
SELECT name FROM sys.services WHERE name = 'Neo4jSyncService';

-- Check Contract
SELECT name FROM sys.service_contracts WHERE name = 'Neo4jSyncContract';
```

#### Step 3: Start Worker

```powershell
# From repository root
cd src\Hartonomous.Workers.Neo4jSync
dotnet run -c Release
```

**Expected Output**:
```
Neo4j Sync Worker starting...
Connected to Neo4j at bolt://localhost:7687
Polling Neo4jSyncQueue for messages...
```

#### Step 4: Send Test Message

```sql
-- Create test atom in SQL Server
INSERT INTO dbo.Atom (TenantId, ContentType, ContentHash, SyncType)
VALUES (0, 'text/plain', '0xABCDEF123456', 'TEST');

DECLARE @AtomId BIGINT = SCOPE_IDENTITY();

-- Enqueue sync message to Neo4j
EXEC dbo.sp_EnqueueNeo4jSync 
  @EntityType = 'Atom', 
  @EntityId = @AtomId, 
  @SyncType = 'CREATE';
```

#### Step 5: Verify Worker Processed Message

**Worker Logs** (should show):
```
Received message from Neo4jSyncQueue
EntityType: Atom, EntityId: 12345, SyncType: CREATE
Executing Cypher: MERGE (a:Atom {id: 12345, tenantId: 0}) ...
✓ Atom synced to Neo4j successfully
```

#### Step 6: Query Neo4j

```cypher
MATCH (a:Atom)
WHERE a.id = 12345
RETURN a;
```

**Expected**: Atom node with properties from SQL Server

---

## Environment 2: GitHub CI/CD

### Purpose

GitHub Actions automates:
1. **Build** - Compile database DACPAC and .NET projects
2. **Test** - Run unit/integration tests
3. **Artifact Creation** - Publish deployable artifacts
4. **No Deployment** - GitHub does NOT deploy workers or databases

### Current Pipeline (`.github/workflows/ci-cd.yml`)

#### Stage 1: Build Database DACPAC

```yaml
- name: Build DACPAC with MSBuild
  shell: pwsh
  run: |
    # Discovers MSBuild, builds Hartonomous.Database.sqlproj
    & $msbuildPath $projectPath /p:Configuration=Release /t:Build /v:minimal
```

**Output**: `artifacts/database/Hartonomous.Database.dacpac`

#### Stage 2: Deploy Database (Self-Hosted Agent)

```yaml
- name: Deploy CLR Signing Certificate to SQL Server
  shell: pwsh
  run: |
    & ./scripts/Deploy-CLRCertificate.ps1 `
      -Server "${{ secrets.SQL_SERVER }}" `
      -Database "${{ secrets.SQL_DATABASE }}" `
      -EnableStrictSecurity $true
```

**Target**: `${{ secrets.SQL_SERVER }}` (configured as HART-DESKTOP in GitHub Secrets)

#### Stage 3: Scaffold EF Core Entities

```yaml
- name: Scaffold EF Core Entities
  shell: pwsh
  run: |
    & $scriptPath -Server "${{ env.SQL_SERVER }}" -Database "${{ env.SQL_DATABASE }}" -UseAzureAD
```

#### Stage 4: Build .NET Solution

```yaml
- name: Build solution
  run: |
    dotnet build Hartonomous.Tests.sln `
      --configuration ${{ env.BUILD_CONFIGURATION }} `
      --no-restore
```

#### Stage 5: Build Applications (Workers)

```yaml
- name: Publish Hartonomous.Api
  run: |
    dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj `
      --configuration Release --output artifacts/api
```

**Also publishes**:
- `Hartonomous.Workers.CesConsumer` → `artifacts/ces-consumer/`
- `Hartonomous.Workers.Neo4jSync` → `artifacts/neo4j-sync/`
- `Hartonomous.Workers.EmbeddingGenerator` → `artifacts/embedding-generator/`

#### Stage 6: Upload Artifacts

```yaml
- name: Upload application artifacts
  uses: actions/upload-artifact@v4
  with:
    name: applications
    path: artifacts/
    retention-days: 7
```

### GitHub Secrets Required

Must be configured in **Settings → Secrets and variables → Actions**:

| Secret Name | Value | Purpose |
|-------------|-------|---------|
| `SQL_SERVER` | `HART-DESKTOP` | Target SQL Server instance |
| `SQL_DATABASE` | `Hartonomous` | Target database name |
| `AZURE_CLIENT_ID` | `<guid>` | Service principal for Azure AD auth |
| `AZURE_TENANT_ID` | `<guid>` | Azure AD tenant |
| `AZURE_SUBSCRIPTION_ID` | `<guid>` | Azure subscription |

### Self-Hosted GitHub Runner

**Required**: GitHub Actions runner installed on HART-DESKTOP

**Setup**:
```powershell
# Download GitHub Actions runner
cd C:\actions-runner
.\config.cmd --url https://github.com/AHartTN/Hartonomous-Sandbox `
  --token <YOUR_REGISTRATION_TOKEN> `
  --name "hart-desktop-runner" `
  --labels "self-hosted,windows,sql-server"

# Install as Windows Service
.\svc.cmd install
.\svc.cmd start
```

**Why Self-Hosted?**
- Access to `localhost` SQL Server
- Access to `localhost` Neo4j
- Can execute database deployments
- Can run scaffolding against local database

### What GitHub DOES NOT Do

❌ Does not deploy worker services as Windows Services  
❌ Does not configure Neo4j schema (that's manual/Azure DevOps)  
❌ Does not manage connection strings for production  
❌ Does not handle secrets (uses Azure Key Vault in production)

---

## Environment 3: Azure Arc Hybrid Production

### Architecture

**HART-DESKTOP** is both:
1. **Your development machine** (localhost)
2. **Production server** (managed via Azure Arc)

Azure Arc enables:
- ✅ **Centralized Management** - Manage on-prem server via Azure Portal
- ✅ **Azure Policy** - Enforce governance and compliance
- ✅ **Azure Monitor** - Collect telemetry to Log Analytics workspace
- ✅ **Azure Automation** - Run PowerShell runbooks for deployments
- ✅ **Managed Identity** - Authenticate to Azure services (Key Vault, App Config)

### Azure Arc Setup

#### Step 1: Install Azure Arc Agent

```powershell
# Download installation script from Azure Portal
# Navigate to: Azure Arc → Servers → Add → Generate script

# Example installation command
$env:SUBSCRIPTION_ID = "<subscription-id>"
$env:RESOURCE_GROUP = "rg-hartonomous-prod"
$env:TENANT_ID = "<tenant-id>"
$env:LOCATION = "eastus"
$env:AUTH_TYPE = "principal"
$env:CLOUD = "AzureCloud"

# Run installation script (generated from Azure Portal)
.\OnboardingScript.ps1
```

**Result**: HART-DESKTOP appears in Azure Portal under **Azure Arc → Servers**

#### Step 2: Enable Managed Identity

```powershell
# After onboarding, enable system-assigned managed identity
az connectedmachine update `
  --name "HART-DESKTOP" `
  --resource-group "rg-hartonomous-prod" `
  --assign-identity
```

**Result**: HART-DESKTOP has a managed identity that can authenticate to Azure services

#### Step 3: Grant Managed Identity Access to Key Vault

```powershell
# Get managed identity principal ID
$principalId = az connectedmachine show `
  --name "HART-DESKTOP" `
  --resource-group "rg-hartonomous-prod" `
  --query "identity.principalId" -o tsv

# Grant Key Vault access
az keyvault set-policy `
  --name "kv-hartonomous-production" `
  --object-id $principalId `
  --secret-permissions get list
```

**Result**: Worker services on HART-DESKTOP can retrieve secrets from Key Vault using managed identity

#### Step 4: Grant Managed Identity Access to App Configuration

```powershell
# Assign "App Configuration Data Reader" role
az role assignment create `
  --assignee $principalId `
  --role "App Configuration Data Reader" `
  --scope "/subscriptions/<subscription-id>/resourceGroups/rg-hartonomous-prod/providers/Microsoft.AppConfiguration/configurationStores/appconfig-hartonomous-production"
```

**Result**: Worker services can read configuration from Azure App Configuration

### Production Configuration

#### Worker appsettings.Production.json

**File**: `src/Hartonomous.Workers.Neo4jSync/appsettings.Production.json` (create this)

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "Azure": {
    "UseManagedIdentity": true
  },
  "AzureAppConfiguration": {
    "Enabled": true,
    "Endpoint": "https://appconfig-hartonomous-production.azconfig.io"
  },
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://kv-hartonomous-production.vault.azure.net/"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=<key>;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/"
  },
  "Neo4j": {
    "Uri": "bolt://HART-DESKTOP:7687",
    "Username": "", 
    "Password": "",
    "Enabled": true
  }
}
```

**Key Differences from Development**:
- `Server=HART-DESKTOP` instead of `localhost`
- `UseManagedIdentity=true` for Azure services
- App Configuration and Key Vault enabled
- Application Insights telemetry enabled
- Neo4j credentials **stored in Key Vault** (empty here)

#### Azure App Configuration (Centralized Config)

Store production settings in Azure App Configuration:

```powershell
# Set Neo4j credentials in App Config (references Key Vault)
az appconfig kv set `
  --name "appconfig-hartonomous-production" `
  --key "Neo4j:Uri" `
  --value "bolt://HART-DESKTOP:7687" `
  --yes

az appconfig kv set-keyvault `
  --name "appconfig-hartonomous-production" `
  --key "Neo4j:Username" `
  --secret-identifier "https://kv-hartonomous-production.vault.azure.net/secrets/Neo4jUsername" `
  --yes

az appconfig kv set-keyvault `
  --name "appconfig-hartonomous-production" `
  --key "Neo4j:Password" `
  --secret-identifier "https://kv-hartonomous-production.vault.azure.net/secrets/Neo4jPassword" `
  --yes
```

**Result**: Workers retrieve Neo4j credentials from Key Vault via App Configuration

#### Azure Key Vault (Secret Storage)

Store sensitive credentials:

```powershell
# Store Neo4j credentials
az keyvault secret set `
  --vault-name "kv-hartonomous-production" `
  --name "Neo4jUsername" `
  --value "neo4j"

az keyvault secret set `
  --vault-name "kv-hartonomous-production" `
  --name "Neo4jPassword" `
  --value "<STRONG_PRODUCTION_PASSWORD>"

# Store SQL Server credentials (if using SQL auth)
az keyvault secret set `
  --vault-name "kv-hartonomous-production" `
  --name "SqlConnectionString" `
  --value "Server=HART-DESKTOP;Database=Hartonomous;User Id=HartonomousApp;Password=<STRONG_PASSWORD>"
```

### Deploying Workers as Windows Services

#### Step 1: Publish Worker

```powershell
# Build and publish Neo4jSyncWorker
cd src\Hartonomous.Workers.Neo4jSync
dotnet publish -c Release -o C:\Hartonomous\Workers\Neo4jSync
```

#### Step 2: Install as Windows Service

```powershell
# Install sc.exe (Service Control) utility
sc.exe create "HartonomousNeo4jSync" `
  binPath= "C:\Hartonomous\Workers\Neo4jSync\Hartonomous.Workers.Neo4jSync.exe" `
  start= auto `
  DisplayName= "Hartonomous Neo4j Sync Worker"

# Start service
sc.exe start "HartonomousNeo4jSync"

# Verify running
Get-Service -Name "HartonomousNeo4jSync"
```

**Expected Output**:
```
Status   Name                  DisplayName
------   ----                  -----------
Running  HartonomousNeo4jSync  Hartonomous Neo4j Sync Worker
```

#### Step 3: Configure Service Recovery

```powershell
# Auto-restart on failure
sc.exe failure "HartonomousNeo4jSync" reset= 86400 actions= restart/60000/restart/60000/restart/60000

# View logs
Get-EventLog -LogName Application -Source "Hartonomous.Workers.Neo4jSync" -Newest 50
```

### Neo4j Production Deployment

#### Deploy Schema to Production Neo4j

```powershell
# Run deployment script against production Neo4j
.\scripts\neo4j\Deploy-Neo4jSchema.ps1 `
  -Neo4jUri "bolt://HART-DESKTOP:7687" `
  -Neo4jUser "neo4j" `
  -Neo4jPassword "<PRODUCTION_PASSWORD>" `
  -Database "neo4j" `
  -Verbose
```

**Output**:
```
[1/6] Testing Neo4j Connection... ✓
[2/6] Backing up existing constraints and indexes... ✓
[3/6] Creating Constraints (idempotent)... Created/Verified 9 of 9 constraints
[4/6] Creating Indexes (idempotent)... Created/Verified 13 of 13 indexes
[5/6] Initializing Reference Data (idempotent)... Created/Verified 15 of 15 reference nodes
[6/6] Verifying Deployment... ✓
✓ Neo4j Schema Deployment Complete!
```

---

## Service Broker Configuration (All Environments)

### Objects Deployed via DACPAC

The Service Broker objects are **already defined** in the database project and deploy automatically:

**Files**:
- `src/Hartonomous.Database/ServiceBroker/MessageTypes/Neo4jSyncRequest.sql`
- `src/Hartonomous.Database/ServiceBroker/Contracts/Neo4jSyncContract.sql`
- `src/Hartonomous.Database/ServiceBroker/Queues/dbo.Neo4jSyncQueue.sql`
- `src/Hartonomous.Database/ServiceBroker/Services/Neo4jSyncService.sql`

### Post-Deployment Activation

**File**: `src/Hartonomous.Database/Scripts/Post-Deployment/Configure.Neo4jSyncActivation.sql`

```sql
ALTER QUEUE [dbo].[Neo4jSyncQueue]
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = dbo.sp_ForwardToNeo4j_Activated,
    MAX_QUEUE_READERS = 3,
    EXECUTE AS OWNER
);
```

**This runs automatically** during DACPAC deployment.

### Verification Query (All Environments)

```sql
-- Verify Service Broker objects exist
SELECT 
  'MessageType' AS ObjectType, name AS ObjectName
FROM sys.service_message_types 
WHERE name = 'Neo4jSyncRequest'

UNION ALL

SELECT 
  'Contract', name
FROM sys.service_contracts 
WHERE name = 'Neo4jSyncContract'

UNION ALL

SELECT 
  'Queue', name
FROM sys.service_queues 
WHERE name = 'Neo4jSyncQueue'

UNION ALL

SELECT 
  'Service', name
FROM sys.services 
WHERE name = 'Neo4jSyncService';
```

**Expected**: 4 rows (MessageType, Contract, Queue, Service)

---

## Connection String Strategy

### Localhost Development

**Pattern**: `Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;`

**Why**:
- Uses Windows Authentication (your user account)
- No password required
- Trusts self-signed certificate (dev SQL Server)

### GitHub CI/CD (Self-Hosted Runner)

**Pattern**: `Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;`

**Why**:
- Runner runs on HART-DESKTOP, so `localhost` works
- Uses service account running the runner
- Same as development (since it's the same machine)

### Azure Arc Production

**Pattern 1** (Windows Authentication):
```
Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;
```

**Pattern 2** (SQL Authentication via Key Vault):
```
Server=HART-DESKTOP;Database=Hartonomous;User Id=HartonomousApp;Password=<from-key-vault>;Encrypt=True;TrustServerCertificate=False;
```

**Recommendation**: Use Windows Authentication with managed service account for production workers.

---

## Deployment Flow Summary

### 1. Code Change (Any Developer)

```
┌─────────────────┐
│ Edit Code       │
│ (VS Code)       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ git commit      │
│ git push origin │
│ main            │
└────────┬────────┘
         │
         ▼ (Triggers GitHub Actions)
```

### 2. GitHub CI/CD Pipeline

```
┌──────────────────────┐
│ Build Database       │ ──▶ Hartonomous.Database.dacpac
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Deploy to HART-      │ ──▶ SQL Server updated with schema changes
│ DESKTOP SQL Server   │     (Service Broker objects included)
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Scaffold Entities    │ ──▶ EF Core entities generated from schema
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Build .NET Solution  │ ──▶ All projects compiled
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Run Tests            │ ──▶ Unit/integration tests executed
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Publish Workers      │ ──▶ Artifacts uploaded (7-day retention)
└──────────────────────┘
```

### 3. Manual Deployment to Production

**Currently**: Manual steps (will automate later with Azure DevOps)

```
┌──────────────────────┐
│ Download artifacts   │
│ from GitHub          │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Stop Windows Service │ ──▶ sc.exe stop "HartonomousNeo4jSync"
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Copy new binaries    │ ──▶ Replace files in C:\Hartonomous\Workers\
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Start Windows Service│ ──▶ sc.exe start "HartonomousNeo4jSync"
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Verify worker logs   │ ──▶ Check Event Viewer or Application Insights
└──────────────────────┘
```

---

## Testing Strategy

### Phase 2.3 Validation Checklist

#### ✅ **Test 1: Localhost Worker Integration**

**Steps**:
1. Start worker: Run VS Code task "Run Neo4jSyncWorker"
2. Send message:
   ```sql
   INSERT INTO dbo.Atom (TenantId, ContentType, ContentHash)
   VALUES (0, 'text/plain', 0x123456);
   EXEC dbo.sp_EnqueueNeo4jSync @EntityType='Atom', @EntityId=SCOPE_IDENTITY();
   ```
3. Verify worker logs show "Received message from Neo4jSyncQueue"
4. Query Neo4j:
   ```cypher
   MATCH (a:Atom) WHERE a.id = <atom-id> RETURN a;
   ```

**Pass Criteria**: Atom appears in Neo4j with correct properties

#### ✅ **Test 2: GitHub CI/CD Build**

**Steps**:
1. Push code to `main` branch
2. Monitor GitHub Actions workflow
3. Verify all stages complete successfully
4. Check artifacts uploaded

**Pass Criteria**: Build succeeds, DACPAC deployed, workers compiled

#### ✅ **Test 3: Production Worker as Windows Service**

**Steps**:
1. Install worker as Windows Service (see above)
2. Send test message to production database
3. Verify service processes message
4. Check Application Insights telemetry

**Pass Criteria**: Message processed, telemetry logged, atom in Neo4j

---

## Phase 2.3 Implementation Steps

### Step 1: Verify Service Broker Objects Exist (Localhost)

```sql
-- Run verification query
SELECT 'Neo4jSyncRequest' AS MessageType, COUNT(*) AS Exists 
FROM sys.service_message_types WHERE name = 'Neo4jSyncRequest'
UNION ALL
SELECT 'Neo4jSyncContract', COUNT(*) FROM sys.service_contracts WHERE name = 'Neo4jSyncContract'
UNION ALL
SELECT 'Neo4jSyncQueue', COUNT(*) FROM sys.service_queues WHERE name = 'Neo4jSyncQueue'
UNION ALL
SELECT 'Neo4jSyncService', COUNT(*) FROM sys.services WHERE name = 'Neo4jSyncService';
```

**Expected**: Each object should have `Exists = 1`

**If Missing**: Run DACPAC deployment manually
```powershell
.\scripts\deploy-dacpac.ps1 -Server "localhost" -Database "Hartonomous"
```

### Step 2: Test Localhost Worker (Development)

```powershell
# Terminal 1: Start worker
cd src\Hartonomous.Workers.Neo4jSync
dotnet run -c Release

# Terminal 2: Send test message
sqlcmd -S localhost -d Hartonomous -E -Q "
INSERT INTO dbo.Atom (TenantId, ContentType, ContentHash) VALUES (0, 'text/plain', 0xABCDEF);
DECLARE @AtomId BIGINT = SCOPE_IDENTITY();
EXEC dbo.sp_EnqueueNeo4jSync @EntityType='Atom', @EntityId=@AtomId;
SELECT @AtomId AS AtomId;
"

# Terminal 3: Query Neo4j
cypher-shell -a bolt://localhost:7687 -u neo4j -p neo4jneo4j "
MATCH (a:Atom) RETURN a ORDER BY a.id DESC LIMIT 1;
"
```

### Step 3: Create Production Configuration Files

**Create**: `src/Hartonomous.Workers.Neo4jSync/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "Azure": {
    "UseManagedIdentity": true
  },
  "AzureAppConfiguration": {
    "Enabled": true,
    "Endpoint": "https://appconfig-hartonomous-production.azconfig.io"
  },
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://kv-hartonomous-production.vault.azure.net/"
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Neo4j": {
    "Uri": "bolt://HART-DESKTOP:7687",
    "Username": "",
    "Password": "",
    "Enabled": true
  }
}
```

**Commit**: `git add . && git commit -m "feat: Add production config for workers"`

### Step 4: Setup Azure Arc Agent (If Not Already Done)

```powershell
# Navigate to Azure Portal: Azure Arc → Servers → Add
# Generate onboarding script
# Run script on HART-DESKTOP
```

### Step 5: Configure Azure Resources

```powershell
# Create Key Vault secrets
az keyvault secret set --vault-name "kv-hartonomous-production" `
  --name "Neo4jUsername" --value "neo4j"

az keyvault secret set --vault-name "kv-hartonomous-production" `
  --name "Neo4jPassword" --value "<STRONG_PASSWORD>"

# Configure App Configuration
az appconfig kv set --name "appconfig-hartonomous-production" `
  --key "Neo4j:Uri" --value "bolt://HART-DESKTOP:7687" --yes

az appconfig kv set-keyvault --name "appconfig-hartonomous-production" `
  --key "Neo4j:Username" `
  --secret-identifier "https://kv-hartonomous-production.vault.azure.net/secrets/Neo4jUsername" --yes

az appconfig kv set-keyvault --name "appconfig-hartonomous-production" `
  --key "Neo4j:Password" `
  --secret-identifier "https://kv-hartonomous-production.vault.azure.net/secrets/Neo4jPassword" --yes
```

### Step 6: Deploy Neo4j Schema to Production

```powershell
# Run deployment script against production
.\scripts\neo4j\Deploy-Neo4jSchema.ps1 `
  -Neo4jUri "bolt://HART-DESKTOP:7687" `
  -Neo4jUser "neo4j" `
  -Neo4jPassword "<PRODUCTION_PASSWORD>" `
  -Verbose
```

### Step 7: Build and Deploy Worker as Windows Service

```powershell
# Build worker
cd src\Hartonomous.Workers.Neo4jSync
dotnet publish -c Release -o C:\Hartonomous\Workers\Neo4jSync

# Install as Windows Service
sc.exe create "HartonomousNeo4jSync" `
  binPath= "C:\Hartonomous\Workers\Neo4jSync\Hartonomous.Workers.Neo4jSync.exe" `
  start= auto `
  DisplayName= "Hartonomous Neo4j Sync Worker"

# Configure environment variable for production
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:DOTNET_ENVIRONMENT = "Production"

# Start service
sc.exe start "HartonomousNeo4jSync"

# Verify running
Get-Service -Name "HartonomousNeo4jSync"
```

### Step 8: Test Production Worker

```sql
-- Send test message in production
INSERT INTO dbo.Atom (TenantId, ContentType, ContentHash)
VALUES (0, 'text/plain', 0xABCDEF789);

DECLARE @AtomId BIGINT = SCOPE_IDENTITY();

EXEC dbo.sp_EnqueueNeo4jSync 
  @EntityType = 'Atom', 
  @EntityId = @AtomId, 
  @SyncType = 'CREATE';

SELECT @AtomId AS TestAtomId;
```

**Verify**:
1. Check Windows Event Viewer: Application log for worker entries
2. Check Application Insights: Search for telemetry from worker
3. Query Neo4j: `MATCH (a:Atom) WHERE a.id = <test-atom-id> RETURN a;`

### Step 9: Configure GitHub Secrets (If Not Done)

In GitHub repository settings:

- `SQL_SERVER` = `HART-DESKTOP`
- `SQL_DATABASE` = `Hartonomous`
- `AZURE_CLIENT_ID` = `<service-principal-id>`
- `AZURE_TENANT_ID` = `<tenant-id>`
- `AZURE_SUBSCRIPTION_ID` = `<subscription-id>`

### Step 10: End-to-End Validation

```sql
-- Test complete flow: SQL → Service Broker → Worker → Neo4j
-- 1. Insert test data
INSERT INTO dbo.Atom (TenantId, ContentType, ContentHash, CreatedAt)
VALUES (0, 'application/json', 0xDEADBEEF, GETUTCDATE());

DECLARE @AtomId BIGINT = SCOPE_IDENTITY();

-- 2. Enqueue sync message
EXEC dbo.sp_EnqueueNeo4jSync @EntityType='Atom', @EntityId=@AtomId;

-- 3. Wait 10 seconds for worker to process

-- 4. Verify in Neo4j
-- Run in cypher-shell:
-- MATCH (a:Atom) WHERE a.id = <atom-id> RETURN a;

-- 5. Query lineage (if parent exists)
-- MATCH path = (child:Atom {id: <atom-id>})-[:DERIVED_FROM*]->(ancestor)
-- RETURN ancestor.id, length(path) as depth;
```

---

## Troubleshooting

### Issue: Worker Not Receiving Messages

**Symptoms**: Worker runs but shows no "Received message" logs

**Diagnosis**:
```sql
-- Check if messages are stuck in queue
SELECT COUNT(*) AS PendingMessages FROM dbo.Neo4jSyncQueue WITH (NOLOCK);
```

**Solutions**:
1. Verify Service Broker enabled: `ALTER DATABASE Hartonomous SET ENABLE_BROKER;`
2. Check queue activation: `SELECT is_activation_enabled FROM sys.service_queues WHERE name = 'Neo4jSyncQueue';`
3. Verify worker connection string is correct
4. Check worker has permissions: `GRANT RECEIVE ON dbo.Neo4jSyncQueue TO [WorkerServiceAccount];`

### Issue: Neo4j Connection Failed

**Symptoms**: Worker logs show "Failed to connect to Neo4j"

**Diagnosis**:
```powershell
# Test Neo4j connectivity
cypher-shell -a bolt://localhost:7687 -u neo4j -p neo4jneo4j "RETURN 1;"
```

**Solutions**:
1. Verify Neo4j is running: `Get-Service -Name Neo4j* | Select-Object Name, Status`
2. Check firewall allows port 7687
3. Verify credentials in appsettings.json
4. Check Neo4j logs: `C:\Neo4j\logs\neo4j.log`

### Issue: Managed Identity Not Working

**Symptoms**: Worker can't access Key Vault or App Configuration

**Diagnosis**:
```powershell
# Verify managed identity exists
az connectedmachine show --name "HART-DESKTOP" --resource-group "rg-hartonomous-prod" --query "identity"
```

**Solutions**:
1. Ensure Azure Arc agent is installed and connected
2. Verify managed identity has correct role assignments
3. Check firewall allows outbound HTTPS to Azure
4. Run worker with verbose logging: `$env:AZURE_IDENTITY_TRACE = "true"`

---

## Next Steps

### Immediate (Phase 2.3 Completion)

1. ✅ Verify Service Broker objects exist (localhost)
2. ✅ Test worker locally with test message
3. ✅ Create production appsettings files
4. ✅ Deploy Neo4j schema to production
5. ✅ Install worker as Windows Service
6. ✅ Test production worker end-to-end

### Near-Term (Phase 3 Preparation)

1. Implement API endpoints for ingestion
2. Add master/detail graph navigation queries
3. Configure Azure DevOps pipeline for automated deployments
4. Setup Application Insights dashboards for monitoring

### Long-Term (Phase 4+ Optimization)

1. Implement OODA loop autonomous agents
2. Configure SQL Server Agent jobs for scheduled tasks
3. Setup alerting for worker failures
4. Performance baseline establishment
5. Load testing and optimization

---

## Related Documents

- [ENTERPRISE_ROLLOUT_PLAN.md](../planning/ENTERPRISE_ROLLOUT_PLAN.md) - Overall roadmap
- [WORKER-DEPLOYMENT-GUIDE.md](../scripts/deploy/WORKER-DEPLOYMENT-GUIDE.md) - Detailed worker deployment
- [Neo4j README.md](../../scripts/neo4j/README.md) - Neo4j schema documentation
- [Neo4j DEPLOYMENT_SUMMARY.md](../../scripts/neo4j/DEPLOYMENT_SUMMARY.md) - What was deployed
- [azure-pipelines.yml](../../azure-pipelines.yml) - Azure DevOps pipeline
- [.github/workflows/ci-cd.yml](../../.github/workflows/ci-cd.yml) - GitHub Actions workflow

---

## Document Control

**Owner**: System Architect  
**Last Updated**: November 21, 2025  
**Review Cycle**: Weekly during Phase 2 implementation  
**Status**: Active Implementation
