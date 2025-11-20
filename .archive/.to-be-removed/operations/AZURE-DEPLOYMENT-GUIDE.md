# ?? HARTONOMOUS AZURE PRODUCTION DEPLOYMENT GUIDE

**Target Environment**: Azure Arc Hybrid (HART-DESKTOP + HART-SERVER)  
**Security**: Entra ID + External ID (B2C) + Azure Key Vault  
**CI/CD**: Azure DevOps Pipelines  
**Timeline**: 2-3 days to production

---

## ?? PREREQUISITES

### Required Azure Resources
- ? Azure subscription (ed614e1a-7d8b-4608-90c8-66e86c37080b)
- ? Owner permissions (logged into `az cli`)
- ? Azure Arc agents on HART-DESKTOP and HART-SERVER
- ? Azure DevOps deployment group configured
- ? Azure B2C tenant for external users

### Required Software (HART-DESKTOP)
- ? SQL Server 2025
- ? .NET 8.0 SDK
- ? Azure DevOps Pipeline Agent
- ? PowerShell 7+

### Required Software (HART-SERVER)
- ? .NET 8.0 Runtime
- ? IIS or standalone hosting
- ? Ollama (for embeddings)
- ? Neo4j (optional, for provenance)

---

## ?? DEPLOYMENT PHASES

### **PHASE 1: Azure Infrastructure Setup** (2-4 hours)

#### Step 1.1: Create Azure Resources

```powershell
# Run infrastructure setup script
.\scripts\azure\01-create-infrastructure.ps1

# This creates:
# - Resource Group: rg-hartonomous-prod
# - Key Vault: kv-hartonomous-production
# - App Configuration: appconfig-hartonomous-production
# - Entra ID App Registrations (API + Blazor UI)
```

**Output**:
```
Resources Created:
  - Key Vault: kv-hartonomous-production
  - App Configuration: appconfig-hartonomous-production
  - API App Registration: <guid>
  - Blazor UI App Registration: <guid>
```

#### Step 1.2: Update Secrets in Key Vault

```powershell
# Update SQL Server connection string (if different)
az keyvault secret set `
    --vault-name kv-hartonomous-production `
    --name "SqlServerConnectionString" `
    --value "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;"

# Set Neo4j password (if using)
az keyvault secret set `
    --vault-name kv-hartonomous-production `
    --name "Neo4jPassword" `
    --value "your-neo4j-password"

# Verify secrets
az keyvault secret list --vault-name kv-hartonomous-production --query "[].name"
```

#### Step 1.3: Grant Service Principals Access to Key Vault

```powershell
# Get API app service principal object ID
$apiAppId = az keyvault secret show --vault-name kv-hartonomous-production --name EntraApiClientId --query value -o tsv
$apiSpObjectId = az ad sp show --id $apiAppId --query id -o tsv

# Grant Key Vault access to API service principal
az keyvault set-policy `
    --name kv-hartonomous-production `
    --object-id $apiSpObjectId `
    --secret-permissions get list

# Grant access to HART-SERVER (if using managed identity)
# First, enable system-assigned managed identity on HART-SERVER Arc resource
# Then grant access to Key Vault
```

---

### **PHASE 2: Azure DevOps Pipelines** (2-3 hours)

#### Step 2.1: Create Service Connection

1. Go to Azure DevOps ? Project Settings ? Service connections
2. Click "+ New service connection"
3. Select "Azure Resource Manager"
4. Choose "Service principal (automatic)"
5. Select subscription: **Azure Developer Subscription**
6. Name: `Azure Developer Subscription`
7. Grant access to all pipelines

#### Step 2.2: Create Pipeline - Database

1. In Azure DevOps, go to Pipelines ? Create Pipeline
2. Select "Azure Repos Git" (or GitHub if using that)
3. Select your repository
4. Choose "Existing Azure Pipelines YAML file"
5. Select `.azure-pipelines/database-pipeline.yml`
6. Click "Run"

**First Run**: Will build DACPAC and deploy to HART-DESKTOP SQL Server

#### Step 2.3: Create Pipeline - Applications

1. Create new pipeline
2. Select `.azure-pipelines/app-pipeline.yml`
3. Click "Run"

**First Run**: Will build API + Workers and deploy to HART-SERVER

---

### **PHASE 3: Application Configuration** (1-2 hours)

#### Step 3.1: Update API Program.cs

Replace `src/Hartonomous.Api/Program.cs` with enhanced version:

```powershell
# Backup current Program.cs
Copy-Item src/Hartonomous.Api/Program.cs src/Hartonomous.Api/Program.cs.bak

# Use enhanced version with Entra ID + Key Vault
Copy-Item src/Hartonomous.Api/Program.EntraId.cs src/Hartonomous.Api/Program.cs
```

#### Step 3.2: Add NuGet Packages to API

```powershell
cd src/Hartonomous.Api

# Azure Key Vault
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity

# Azure App Configuration
dotnet add package Microsoft.Extensions.Configuration.AzureAppConfiguration

# Entra ID (Azure AD)
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Application Insights
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

#### Step 3.3: Configure Blazor Admin UI (Optional)

```powershell
cd src/Hartonomous.Admin

dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

Update `appsettings.json`:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<from Key Vault>",
    "ClientId": "<from Key Vault>",
    "CallbackPath": "/signin-oidc"
  }
}
```

---

### **PHASE 4: Deploy & Validate** (1 hour)

#### Step 4.1: Trigger Full Deployment

```powershell
# Commit and push changes
git add .
git commit -m "feat: Add Azure infrastructure and CI/CD pipelines"
git push origin main

# This will trigger:
# 1. Database pipeline (builds + deploys DACPAC to HART-DESKTOP)
# 2. App pipeline (builds + deploys API & Workers to HART-SERVER)
```

#### Step 4.2: Verify Deployment on HART-SERVER

```powershell
# SSH/RDP to HART-SERVER

# Check Windows Services
Get-Service | Where-Object {$_.Name -like "Hartonomous*"}

# Expected output:
# HartonomousApi         Running
# HartonomousCesWorker   Running
# HartonomousNeo4jWorker Running

# Test API health endpoint
Invoke-RestMethod -Uri "http://localhost:5000/api/admin/health"
```

#### Step 4.3: Run End-to-End Validation

```powershell
# On HART-DESKTOP (SQL Server)
sqlcmd -S localhost -d Hartonomous -E -i tests/complete-validation.sql

# Expected output:
#   Total Tests: 6
#   Passed: 6 ?
#   Failed: 0 ?
#   STATUS: ? OPERATIONAL (100%)
```

---

## ?? SECURITY CONFIGURATION

### Entra ID App Roles Assignment

1. Go to Azure Portal ? Entra ID ? App registrations
2. Find "Hartonomous API (Production)"
3. Go to "App roles" ? Verify 3 roles exist:
   - Administrator
   - Analyst
   - User

4. Assign users to roles:
   - Go to Enterprise Applications ? Hartonomous API
   - Click "Users and groups" ? Add user/group
   - Select user ? Select role ? Assign

### Configure External ID (B2C) for Public Access

If you want external users to access the API:

```powershell
# Get B2C tenant name
$b2cTenant = "<your-b2c-tenant>.onmicrosoft.com"

# Create B2C app registration
az ad app create `
    --display-name "Hartonomous Public API" `
    --sign-in-audience AzureADandPersonalMicrosoftAccount `
    --web-redirect-uris "https://hart-server/signin-b2c"

# Configure user flows in Azure Portal ? External ID
```

---

## ?? MONITORING & OBSERVABILITY

### Application Insights Setup

```powershell
# Create Application Insights
az monitor app-insights component create `
    --app HartonomousInsights `
    --location eastus `
    --resource-group rg-hartonomous-prod `
    --application-type web

# Get instrumentation key
$instrumentationKey = az monitor app-insights component show `
    --app HartonomousInsights `
    --resource-group rg-hartonomous-prod `
    --query instrumentationKey -o tsv

# Store in Key Vault
az keyvault secret set `
    --vault-name kv-hartonomous-production `
    --name "ApplicationInsightsKey" `
    --value $instrumentationKey
```

Update `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "" // Populated from Key Vault
  }
}
```

---

## ?? TESTING CHECKLIST

### Pre-Production Validation

- [ ] Database deployed to HART-DESKTOP SQL Server
- [ ] All 6 smoke tests passing
- [ ] CLR functions operational
- [ ] OODA loop procedures exist and execute
- [ ] API deployed to HART-SERVER as Windows Service
- [ ] Workers running as Windows Services
- [ ] Entra ID authentication working
- [ ] Key Vault secrets accessible
- [ ] App Configuration values readable
- [ ] Application Insights receiving telemetry

### Post-Deployment Smoke Tests

```bash
# Test 1: Health Check
curl http://localhost:5000/api/admin/health

# Test 2: Authenticated Request (requires token)
# Get token from: https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/authorize
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/models

# Test 3: Ingest Data
curl -X POST http://localhost:5000/api/sources/ingest/text \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"content": "Test ingestion", "sourceUri": "test://deploy"}'

# Test 4: Run Inference
curl -X POST http://localhost:5000/api/inference/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"prompt": "test query", "topK": 5}'

# Test 5: Trigger OODA Analysis
curl -X POST http://localhost:5000/api/admin/ooda/analyze \
  -H "Authorization: Bearer <token>"
```

---

## ?? TROUBLESHOOTING

### Issue: CLR Functions Not Working

```sql
-- Check CLR configuration
EXEC sp_configure 'clr enabled';
SELECT is_trustworthy_on FROM sys.databases WHERE name = 'Hartonomous';

-- Fix if needed
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
```

### Issue: API Can't Connect to SQL Server

```powershell
# Verify connection string in Key Vault
az keyvault secret show --vault-name kv-hartonomous-production --name SqlServerConnectionString

# Test from HART-SERVER
sqlcmd -S HART-DESKTOP -d Hartonomous -E -Q "SELECT @@VERSION"
```

### Issue: Authentication Failing

```powershell
# Verify app registration
az ad app show --id <api-client-id>

# Check service principal
az ad sp show --id <api-client-id>

# Verify Key Vault access
az keyvault secret show --vault-name kv-hartonomous-production --name EntraApiClientId
```

---

## ?? NEXT STEPS AFTER DEPLOYMENT

1. **Load Sample Data**: Run ingestion pipeline with real data
2. **Performance Testing**: Benchmark with 1M+ embeddings
3. **Blazor UI**: Deploy Admin dashboard to HART-SERVER
4. **External API**: Configure B2C for public access
5. **Monitoring**: Set up Grafana dashboards
6. **Backup**: Configure SQL Server backup schedule

---

## ?? SUCCESS CRITERIA

You'll know deployment is successful when:

? Azure pipelines run green (both database and app)  
? All Windows Services running on HART-SERVER  
? 6/6 smoke tests passing on HART-DESKTOP  
? API health endpoint returns 200 OK  
? Authentication requires valid Entra ID token  
? Secrets loaded from Azure Key Vault  
? Application Insights receiving telemetry  
? OODA loop executing every 15 minutes  

**Estimated Deployment Time**: 4-6 hours (first time), 30 minutes (subsequent)

---

**Ready to deploy?**

```powershell
# Start with Phase 1
.\scripts\azure\01-create-infrastructure.ps1
```
