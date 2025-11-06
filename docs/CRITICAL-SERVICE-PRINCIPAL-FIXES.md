# CRITICAL: Service Principal Cleanup and Configuration

## PROBLEM IDENTIFIED
I made role assignments that are:
1. **Unnecessary** - Hartonomous SP doesn't need most of these roles
2. **Incorrect** - Services should use Arc machine managed identities, not the SP
3. **Bloat** - Extra permissions are security holes

## WHAT HARTONOMOUS ACTUALLY NEEDS

### 1. Hartonomous Service Principal (`c25ed11d-c712-4574-8897-6a3a0c8dbb7f`)
**Purpose**: Azure DevOps deployment group agent authentication ONLY

**Current Roles** (what I just added):
- ✅ App Configuration Data Reader - appconfig-hartonomous (**KEEP** - DevOps agent might need this for deployment validation)
- ❌ Key Vault Secrets User - kv-hartonomous (**REMOVE** - agents don't need Key Vault access)
- ❌ Storage Blob Data Contributor - hartonomousstorage (**REMOVE** - agents don't deploy to storage)
- ❌ Monitoring Metrics Publisher - hartonomous-insights (**REMOVE** - agents don't publish metrics)
- ✅ Azure Connected Machine Onboarding - rg-hartonomous (**KEEP** - for Arc management)

**Required Azure DevOps Permission** (BLOCKING):
- ⚠️ **Administrator role** on "Primary Local" deployment group - **MUST BE GRANTED MANUALLY IN PORTAL**

### 2. HART-SERVER Managed Identity (`50c98169-43ea-4ee7-9daa-d752ed328994`)
**Purpose**: Runtime authentication for .NET services (API, CesConsumer, Neo4jSync, ModelIngestion)

**Current Roles** (what I just added):
- ❌ Key Vault Secrets User - kv-hartonomous (**KEEP BUT WRONG APPROACH** - see below)
- ❌ App Configuration Data Reader - appconfig-hartonomous (**KEEP BUT WRONG APPROACH** - see below)
- ❌ Storage Blob Data Contributor - hartonomousstorage (**KEEP BUT WRONG APPROACH** - see below)
- ❌ Monitoring Metrics Publisher - hartonomous-insights (**KEEP BUT WRONG APPROACH** - see below)

**CORRECT APPROACH**: App Configuration's managed identity should have Key Vault access, NOT individual services

### 3. HART-DESKTOP Managed Identity (`505c61a6-bcd6-4f22-aee5-5c6c0094ae0d`)
**Purpose**: Unclear - HART-DESKTOP runs SQL Server, not .NET services

**Current Roles** (what I just added):
- ❌ Key Vault Secrets User - kv-hartonomous (**REMOVE** - SQL Server doesn't use Azure SDK auth)
- ❌ App Configuration Data Reader - appconfig-hartonomous (**REMOVE** - SQL Server doesn't read App Config)

## CORRECT ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│ .NET Services on HART-SERVER                                │
│ (API, CesConsumer, Neo4jSync, ModelIngestion)               │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ DefaultAzureCredential
                  │ (uses HART-SERVER MI: 50c98169...)
                  ▼
┌─────────────────────────────────────────────────────────────┐
│ Azure App Configuration                                     │
│ Endpoint: https://appconfig-hartonomous.azconfig.io         │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  │ App Config Managed Identity
                  │ (needs Key Vault Secrets User on kv-hartonomous)
                  ▼
┌─────────────────────────────────────────────────────────────┐
│ Azure Key Vault: kv-hartonomous                             │
│ - Neo4j:Password                                            │
│ - ApplicationInsights:ConnectionString                      │
│ - HuggingFace:ApiToken                                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Direct Service Access (via HART-SERVER MI)                  │
├─────────────────────────────────────────────────────────────┤
│ - Azure Storage (BlobServiceClient, QueueServiceClient)     │
│ - Application Insights (connection string from Key Vault)   │
└─────────────────────────────────────────────────────────────┘
```

## CLEANUP COMMANDS

### Remove Unnecessary Hartonomous SP Roles
```powershell
# Remove Key Vault access (agents don't need it)
az role assignment delete `
  --assignee c25ed11d-c712-4574-8897-6a3a0c8dbb7f `
  --role "Key Vault Secrets User" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.KeyVault/vaults/kv-hartonomous"

# Remove Storage access (agents don't deploy blobs)
az role assignment delete `
  --assignee c25ed11d-c712-4574-8897-6a3a0c8dbb7f `
  --role "Storage Blob Data Contributor" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.Storage/storageAccounts/hartonomousstorage"

# Remove Monitoring access (agents don't publish metrics)
az role assignment delete `
  --assignee c25ed11d-c712-4574-8897-6a3a0c8dbb7f `
  --role "Monitoring Metrics Publisher" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.Insights/components/hartonomous-insights"
```

### Remove ALL HART-DESKTOP MI Roles (it doesn't need Azure resources)
```powershell
# Remove Key Vault access
az role assignment delete `
  --assignee 505c61a6-bcd6-4f22-aee5-5c6c0094ae0d `
  --role "Key Vault Secrets User" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.KeyVault/vaults/kv-hartonomous"

# Remove App Configuration access
az role assignment delete `
  --assignee 505c61a6-bcd6-4f22-aee5-5c6c0094ae0d `
  --role "App Configuration Data Reader" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.AppConfiguration/configurationStores/appconfig-hartonomous"
```

## CORRECT CONFIGURATION

### Step 1: Grant App Configuration MI access to Key Vault
```powershell
# Get App Config managed identity principal ID
$appConfigMI = az appconfig show --name appconfig-hartonomous --resource-group rg-hartonomous --query identity.principalId -o tsv

# Grant Key Vault access to App Config MI
az role assignment create `
  --assignee $appConfigMI `
  --role "Key Vault Secrets User" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.KeyVault/vaults/kv-hartonomous"
```

### Step 2: HART-SERVER MI only needs these roles
```powershell
# App Configuration Data Reader (to read config)
az role assignment create `
  --assignee 50c98169-43ea-4ee7-9daa-d752ed328994 `
  --role "App Configuration Data Reader" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.AppConfiguration/configurationStores/appconfig-hartonomous"

# Storage Blob Data Contributor (for BlobServiceClient/QueueServiceClient)
az role assignment create `
  --assignee 50c98169-43ea-4ee7-9daa-d752ed328994 `
  --role "Storage Blob Data Contributor" `
  --scope "/subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.Storage/storageAccounts/hartonomousstorage"
```

### Step 3: Azure DevOps Deployment Group Permission (MANUAL)
1. Navigate to https://dev.azure.com/aharttn/Hartonomous/_settings/deploymentgroups
2. Select "Primary Local" deployment group
3. Click "Security" tab
4. Click "Add" button
5. Search for "Hartonomous" service principal (c25ed11d-c712-4574-8897-6a3a0c8dbb7f)
6. Grant "Administrator" role
7. Save

## FINAL STATE

### Hartonomous SP Roles (Azure DevOps agent auth only):
- ✅ App Configuration Data Reader (for deployment validation)
- ✅ Azure Connected Machine Onboarding (for Arc management)
- ✅ **Deployment Group Administrator** (manual grant in DevOps portal)

### HART-SERVER MI Roles (service runtime):
- ✅ App Configuration Data Reader (read config)
- ✅ Storage Blob Data Contributor (write blobs/queues)

### App Configuration MI Roles:
- ✅ Key Vault Secrets User (read secrets for Key Vault references)

### HART-DESKTOP MI Roles:
- (NONE - SQL Server doesn't use Azure SDK authentication)

## VALIDATION

After cleanup and correct configuration:
```powershell
# Verify Hartonomous SP
az role assignment list --assignee c25ed11d-c712-4574-8897-6a3a0c8dbb7f -o table

# Verify HART-SERVER MI
az role assignment list --assignee 50c98169-43ea-4ee7-9daa-d752ed328994 -o table

# Verify HART-DESKTOP MI (should be empty)
az role assignment list --assignee 505c61a6-bcd6-4f22-aee5-5c6c0094ae0d -o table

# Verify App Config MI
$appConfigMI = az appconfig show --name appconfig-hartonomous --resource-group rg-hartonomous --query identity.principalId -o tsv
az role assignment list --assignee $appConfigMI -o table
```

## PRINCIPLE OF LEAST PRIVILEGE

**Rule**: Each identity gets ONLY the minimum permissions it needs to function.

- **DevOps agents** = App Config read + Arc management + DevOps group admin
- **Services** = App Config read + Storage access
- **App Config** = Key Vault read (for Key Vault references)
- **SQL Server** = Nothing (uses Windows integrated auth to local SQL Server)
