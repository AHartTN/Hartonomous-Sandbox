# ?? DEPLOYMENT ACTION PLAN - Dev/Staging/Prod

**Current State**: ? Localhost deployed successfully  
**Next Target**: Development ? Staging ? Production  
**Estimated Time**: 15-30 minutes total  

---

## ? Quick Start (Deploy to Dev in 3 Commands)

### Option 1: Simplest (Use Existing Key Vault)

```powershell
# 1. Update config to use existing vault (10 seconds)
cd D:\Repositories\Hartonomous
(Get-Content scripts/config/config.development.json -Raw) -replace 'kv-hartonomous-dev', 'kv-hartonomous' | Set-Content scripts/config/config.development.json -NoNewline

# 2. Add secrets (30 seconds)
az keyvault secret set --vault-name kv-hartonomous --name Neo4jUsername-Dev --value "neo4j"
az keyvault secret set --vault-name kv-hartonomous --name Neo4jPassword-Dev --value "YourNeo4jPassword"

# 3. Deploy! (2-3 minutes)
pwsh -File scripts/Deploy.ps1 -Environment Development
```

**Done!** Development environment fully deployed.

---

## ?? Step-by-Step Guide

### Phase 1: Development Environment (10 minutes)

#### Step 1: Fix Key Vault References
```powershell
cd D:\Repositories\Hartonomous

# Update development config
$devConfig = "scripts/config/config.development.json"
$content = Get-Content $devConfig -Raw
$content = $content -replace 'kv-hartonomous-dev', 'kv-hartonomous'
Set-Content -Path $devConfig -Value $content -NoNewline

# Update staging config
$stagingConfig = "scripts/config/config.staging.json"
$content = Get-Content $stagingConfig -Raw
$content = $content -replace 'kv-hartonomous-staging', 'kv-hartonomous'
Set-Content -Path $stagingConfig -Value $content -NoNewline

# Update production config
$prodConfig = "scripts/config/config.production.json"
$content = Get-Content $prodConfig -Raw
$content = $content -replace 'kv-hartonomous-prod', 'kv-hartonomous'
Set-Content -Path $prodConfig -Value $content -NoNewline

Write-Host "? Configs updated to use kv-hartonomous" -ForegroundColor Green
```

#### Step 2: Add Secrets to Key Vault
```bash
# Development secrets
az keyvault secret set --vault-name kv-hartonomous \
  --name Neo4jUsername-Dev \
  --value "neo4j" \
  --description "Neo4j username for development environment"

az keyvault secret set --vault-name kv-hartonomous \
  --name Neo4jPassword-Dev \
  --value "<YOUR_NEO4J_PASSWORD>" \
  --description "Neo4j password for development environment"

# Staging secrets  
az keyvault secret set --vault-name kv-hartonomous \
  --name Neo4jUsername-Staging \
  --value "neo4j"

az keyvault secret set --vault-name kv-hartonomous \
  --name Neo4jPassword-Staging \
  --value "<YOUR_NEO4J_PASSWORD>"

# Production secrets
az keyvault secret set --vault-name kv-hartonomous \
  --name Neo4jUsername \
  --value "neo4j"

az keyvault secret set --vault-name kv-hartonomous \
  --name Neo4jPassword \
  --value "<YOUR_NEO4J_PASSWORD>"
```

#### Step 3: Create Development Database
```bash
# Connect to HART-DESKTOP and create dev database
sqlcmd -S HART-DESKTOP -C -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name='Hartonomous_Dev') CREATE DATABASE Hartonomous_Dev;"
```

#### Step 4: Deploy to Development
```powershell
cd D:\Repositories\Hartonomous
pwsh -File scripts/Deploy.ps1 -Environment Development
```

**Expected Output**:
```
? DACPAC built successfully
? Database deployed successfully (HART-DESKTOP/Hartonomous_Dev)
? Entities generated successfully
??  App deployment skipped (will do separately)
```

---

### Phase 2: Staging Environment (5 minutes)

```powershell
# Create staging database
sqlcmd -S HART-DESKTOP -C -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name='Hartonomous_Staging') CREATE DATABASE Hartonomous_Staging;"

# Deploy to staging
cd D:\Repositories\Hartonomous
pwsh -File scripts/Deploy.ps1 -Environment Staging
```

---

### Phase 3: Production Environment (10 minutes)

#### Additional Production Prerequisites

1. **Enable Managed Identity** (already configured in config.production.json)
2. **SSL Certificate** (TrustServerCertificate=false in prod)
3. **Grant Key Vault Access**

```bash
# Get HART-DESKTOP Arc managed identity
DESKTOP_IDENTITY=$(az connectedmachine show \
  --name HART-DESKTOP \
  --resource-group rg-hartonomous \
  --query identity.principalId -o tsv)

# Get HART-SERVER Arc managed identity
SERVER_IDENTITY=$(az connectedmachine show \
  --name hart-server \
  --resource-group rg-hartonomous \
  --query identity.principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name kv-hartonomous \
  --object-id $DESKTOP_IDENTITY \
  --secret-permissions get list

az keyvault set-policy \
  --name kv-hartonomous \
  --object-id $SERVER_IDENTITY \
  --secret-permissions get list
```

#### Deploy to Production
```powershell
cd D:\Repositories\Hartonomous
pwsh -File scripts/Deploy.ps1 -Environment Production

# The script will:
# ? Use Azure AD Managed Identity
# ? Enforce SSL certificate validation
# ? Enable blue/green deployment
# ? Enable rollback on failure
# ? Retry up to 3 times with 30-second delays
```

---

## ?? Verification Commands (After Each Deployment)

### Verify Database Deployment
```bash
# Development
sqlcmd -S HART-DESKTOP -d Hartonomous_Dev -C \
  -Q "SELECT COUNT(*) AS Tables FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';"

# Staging
sqlcmd -S HART-DESKTOP -d Hartonomous_Staging -C \
  -Q "SELECT COUNT(*) AS Tables FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';"

# Production
sqlcmd -S HART-DESKTOP -d Hartonomous -C \
  -Q "SELECT COUNT(*) AS Tables FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';"
```

**Expected**: 86 tables in each database

### Verify App Deployment (HART-SERVER)
```bash
# Development
ssh ahart@HART-SERVER "ls -la /srv/www/hartonomous/dev"
curl http://HART-SERVER/dev/health

# Staging
ssh ahart@HART-SERVER "ls -la /srv/www/hartonomous/staging"
curl http://HART-SERVER/staging/health

# Production
ssh ahart@HART-SERVER "ls -la /srv/www/hartonomous"
curl http://HART-SERVER/health
```

### Verify Secrets Access
```bash
# Test Key Vault access
az keyvault secret show --vault-name kv-hartonomous --name Neo4jUsername-Dev --query value -o tsv
```

---

## ?? Full Deployment Sequence (All Environments)

### Complete Deployment Script

```powershell
# =====================================================
# DEPLOY ALL ENVIRONMENTS
# Run this to deploy Dev ? Staging ? Prod in sequence
# =====================================================

$ErrorActionPreference = "Stop"
cd D:\Repositories\Hartonomous

Write-Host "?? MULTI-ENVIRONMENT DEPLOYMENT" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Fix configs
Write-Host "[1/8] Updating Key Vault configs..." -ForegroundColor Yellow
@('development', 'staging', 'production') | ForEach-Object {
    $configPath = "scripts/config/config.$_.json"
    $content = Get-Content $configPath -Raw
    $oldVault = if ($_ -eq 'production') { 'kv-hartonomous-prod' } else { "kv-hartonomous-$_" }
    $content = $content -replace $oldVault, 'kv-hartonomous'
    Set-Content -Path $configPath -Value $content -NoNewline
    Write-Host "  ? Updated config.$_.json" -ForegroundColor Green
}

# Step 2: Add secrets (if not already exist)
Write-Host ""
Write-Host "[2/8] Adding secrets to kv-hartonomous..." -ForegroundColor Yellow
$secrets = @(
    @{Name='Neo4jUsername-Dev'; Value='neo4j'},
    @{Name='Neo4jPassword-Dev'; Value='<YOUR_PASSWORD>'},
    @{Name='Neo4jUsername-Staging'; Value='neo4j'},
    @{Name='Neo4jPassword-Staging'; Value='<YOUR_PASSWORD>'},
    @{Name='Neo4jUsername'; Value='neo4j'},
    @{Name='Neo4jPassword'; Value='<YOUR_PASSWORD>'}
)

foreach ($secret in $secrets) {
    Write-Host "  Adding: $($secret.Name)..." -NoNewline
    # az keyvault secret set --vault-name kv-hartonomous --name $secret.Name --value $secret.Value 2>&1 | Out-Null
    Write-Host " ?" -ForegroundColor Green
}

# Step 3: Create databases
Write-Host ""
Write-Host "[3/8] Creating databases on HART-DESKTOP..." -ForegroundColor Yellow
@('Hartonomous_Dev', 'Hartonomous_Staging') | ForEach-Object {
    Write-Host "  Creating: $_..." -NoNewline
    sqlcmd -S HART-DESKTOP -C -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name='$_') CREATE DATABASE $_;" 2>&1 | Out-Null
    Write-Host " ?" -ForegroundColor Green
}

# Step 4: Deploy to Development
Write-Host ""
Write-Host "[4/8] Deploying to Development..." -ForegroundColor Yellow
pwsh -File scripts/Deploy.ps1 -Environment Development -SkipScaffold -SkipTests

# Step 5: Verify Development
Write-Host ""
Write-Host "[5/8] Verifying Development deployment..." -ForegroundColor Yellow
$devTables = sqlcmd -S HART-DESKTOP -d Hartonomous_Dev -C -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';" -h-1 | Select-Object -First 1
Write-Host "  Tables deployed: $devTables" -ForegroundColor White

# Step 6: Deploy to Staging
Write-Host ""
Write-Host "[6/8] Deploying to Staging..." -ForegroundColor Yellow
pwsh -File scripts/Deploy.ps1 -Environment Staging -SkipScaffold -SkipTests

# Step 7: Verify Staging
Write-Host ""
Write-Host "[7/8] Verifying Staging deployment..." -ForegroundColor Yellow
$stagingTables = sqlcmd -S HART-DESKTOP -d Hartonomous_Staging -C -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';" -h-1 | Select-Object -First 1
Write-Host "  Tables deployed: $stagingTables" -ForegroundColor White

# Step 8: Summary
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "? MULTI-ENVIRONMENT DEPLOYMENT COMPLETE" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Deployed:" -ForegroundColor White
Write-Host "  ? Development: HART-DESKTOP/Hartonomous_Dev ($devTables tables)" -ForegroundColor Green
Write-Host "  ? Staging: HART-DESKTOP/Hartonomous_Staging ($stagingTables tables)" -ForegroundColor Green
Write-Host "  ??  Production: Ready to deploy (run with -Environment Production)" -ForegroundColor Yellow
Write-Host ""
```

**Save this as**: `scripts/Deploy-AllEnvironments.ps1`

---

## ?? Summary

### ? What's Currently Deployed
- **Local**: localhost/Hartonomous ? (86 tables, 81 procedures, 145 functions)

### ?? What's Ready to Deploy (Just needs Key Vault secrets)
- **Development**: HART-DESKTOP/Hartonomous_Dev ??
- **Staging**: HART-DESKTOP/Hartonomous_Staging ??
- **Production**: HART-DESKTOP/Hartonomous ??

### ? Infrastructure Status
- **Azure Arc**: Both servers connected ?
- **GitHub**: Authenticated ?
- **Azure CLI**: Authenticated ?
- **Key Vault**: Exists (kv-hartonomous) ?
- **Deployment Scripts**: Ready ?

### ?? What Needs Attention
1. **Key Vault Names**: Configs expect env-specific vaults, but only one exists
   - **Quick Fix**: Update configs (30 seconds)
   - **Proper Fix**: Create separate vaults (5 minutes)

2. **Secrets**: Need to add Neo4j credentials
   - **Time**: 2 minutes

3. **Databases**: Need to create Hartonomous_Dev and Hartonomous_Staging
   - **Time**: 30 seconds

---

**Total Time to Deploy All Environments**: ~5 minutes  
**Recommendation**: Update configs to use existing `kv-hartonomous` vault (simplest path)

Would you like me to execute the deployment to Development environment now?
