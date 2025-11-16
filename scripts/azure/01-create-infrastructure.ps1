#!/usr/bin/env pwsh
# =====================================================
# Hartonomous Azure Infrastructure Setup
# =====================================================
# Creates: Key Vault, App Configuration, Entra ID App Registrations

param(
    [string]$SubscriptionId = "ed614e1a-7d8b-4608-90c8-66e86c37080b",
    [string]$ResourceGroup = "rg-hartonomous-prod",
    [string]$Location = "eastus",
    [string]$Environment = "Production"
)

$ErrorActionPreference = "Stop"

Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   HARTONOMOUS AZURE INFRASTRUCTURE SETUP               ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Set subscription context
Write-Host "Setting Azure subscription context..." -ForegroundColor Yellow
az account set --subscription $SubscriptionId

# Create Resource Group
Write-Host "Creating resource group: $ResourceGroup..." -ForegroundColor Yellow
az group create `
    --name $ResourceGroup `
    --location $Location `
    --tags Environment=$Environment Project=Hartonomous

Write-Host "? Resource group created" -ForegroundColor Green
Write-Host ""

# =====================================================
# 1. AZURE KEY VAULT
# =====================================================
Write-Host "Creating Azure Key Vault..." -ForegroundColor Yellow

$keyVaultName = "kv-hartonomous-$Environment".ToLower()

az keyvault create `
    --name $keyVaultName `
    --resource-group $ResourceGroup `
    --location $Location `
    --enable-rbac-authorization false `
    --enabled-for-deployment true `
    --enabled-for-template-deployment true `
    --tags Environment=$Environment

Write-Host "? Key Vault created: $keyVaultName" -ForegroundColor Green

# Grant current user access
$currentUser = az ad signed-in-user show --query id -o tsv

az keyvault set-policy `
    --name $keyVaultName `
    --object-id $currentUser `
    --secret-permissions get list set delete `
    --key-permissions get list create delete `
    --certificate-permissions get list create delete

Write-Host "? Key Vault access granted to current user" -ForegroundColor Green
Write-Host ""

# =====================================================
# 2. AZURE APP CONFIGURATION
# =====================================================
Write-Host "Creating Azure App Configuration..." -ForegroundColor Yellow

$appConfigName = "appconfig-hartonomous-$Environment".ToLower()

az appconfig create `
    --name $appConfigName `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku Standard `
    --tags Environment=$Environment

Write-Host "? App Configuration created: $appConfigName" -ForegroundColor Green

# Get connection string
$appConfigConnectionString = az appconfig credential list `
    --name $appConfigName `
    --resource-group $ResourceGroup `
    --query "[0].connectionString" -o tsv

Write-Host "? App Configuration connection string retrieved" -ForegroundColor Green
Write-Host ""

# =====================================================
# 3. STORE SECRETS IN KEY VAULT
# =====================================================
Write-Host "Storing configuration in Key Vault..." -ForegroundColor Yellow

# Store App Configuration connection string
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "AppConfigConnectionString" `
    --value $appConfigConnectionString

# Placeholder secrets (update these later)
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "SqlServerConnectionString" `
    --value "Server=HART-DESKTOP;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;"

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "OllamaBaseUrl" `
    --value "http://localhost:11434"

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "Neo4jUri" `
    --value "bolt://localhost:7687"

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "Neo4jUsername" `
    --value "neo4j"

Write-Host "? Secrets stored in Key Vault" -ForegroundColor Green
Write-Host ""

# =====================================================
# 4. ENTRA ID APP REGISTRATIONS
# =====================================================
Write-Host "Creating Entra ID App Registrations..." -ForegroundColor Yellow

# API App Registration (Internal - Entra ID)
Write-Host "  Creating API app registration..." -ForegroundColor Cyan

$apiAppName = "Hartonomous API ($Environment)"

$apiApp = az ad app create `
    --display-name $apiAppName `
    --sign-in-audience AzureADMyOrg `
    --web-redirect-uris "https://hart-server/signin-oidc" "https://localhost:5001/signin-oidc" `
    --enable-id-token-issuance true `
    --enable-access-token-issuance true | ConvertFrom-Json

$apiAppId = $apiApp.appId
$apiObjectId = $apiApp.id

Write-Host "  ? API App Registration created: $apiAppId" -ForegroundColor Green

# Create service principal
az ad sp create --id $apiAppId

# Add API permissions (Microsoft Graph - User.Read)
az ad app permission add `
    --id $apiAppId `
    --api 00000003-0000-0000-c000-000000000000 `
    --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope

# Create app roles for RBAC
$appRoles = @'
[
  {
    "allowedMemberTypes": ["User"],
    "description": "Administrators have full access to all Hartonomous features",
    "displayName": "Administrator",
    "id": "00000000-0000-0000-0000-000000000001",
    "isEnabled": true,
    "value": "Admin"
  },
  {
    "allowedMemberTypes": ["User"],
    "description": "Can trigger OODA analysis and view metrics",
    "displayName": "Analyst",
    "id": "00000000-0000-0000-0000-000000000002",
    "isEnabled": true,
    "value": "Analyst"
  },
  {
    "allowedMemberTypes": ["User"],
    "description": "Can query inference and ingest data",
    "displayName": "User",
    "id": "00000000-0000-0000-0000-000000000003",
    "isEnabled": true,
    "value": "User"
  }
]
'@

$appRoles | Out-File -FilePath "temp-app-roles.json" -Encoding utf8
az ad app update --id $apiAppId --app-roles @temp-app-roles.json
Remove-Item "temp-app-roles.json"

Write-Host "  ? App roles configured (Admin, Analyst, User)" -ForegroundColor Green

# Blazor Admin UI App Registration
Write-Host "  Creating Blazor Admin UI app registration..." -ForegroundColor Cyan

$blazorAppName = "Hartonomous Admin UI ($Environment)"

$blazorApp = az ad app create `
    --display-name $blazorAppName `
    --sign-in-audience AzureADMyOrg `
    --web-redirect-uris "https://hart-server:7000/signin-oidc" "https://localhost:7000/signin-oidc" `
    --enable-id-token-issuance true | ConvertFrom-Json

$blazorAppId = $blazorApp.appId

az ad sp create --id $blazorAppId

# Grant Blazor app access to API
az ad app permission add `
    --id $blazorAppId `
    --api $apiAppId `
    --api-permissions $($apiApp.oauth2PermissionScopes[0].id)=Scope

Write-Host "  ? Blazor Admin UI App Registration created: $blazorAppId" -ForegroundColor Green

# Store app IDs in Key Vault
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "EntraApiClientId" `
    --value $apiAppId

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "EntraBlazorClientId" `
    --value $blazorAppId

# Get tenant ID
$tenantId = az account show --query tenantId -o tsv

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "EntraTenantId" `
    --value $tenantId

Write-Host "? Entra ID app registrations complete" -ForegroundColor Green
Write-Host ""

# =====================================================
# 5. APP CONFIGURATION - FEATURE FLAGS
# =====================================================
Write-Host "Setting up App Configuration feature flags..." -ForegroundColor Yellow

# OODA Loop settings
az appconfig kv set `
    --name $appConfigName `
    --key "Hartonomous:OodaLoop:AnalysisIntervalMinutes" `
    --value "15" `
    --yes

az appconfig kv set `
    --name $appConfigName `
    --key "Hartonomous:OodaLoop:AutoExecuteLowRiskActions" `
    --value "true" `
    --yes

# Inference settings
az appconfig kv set `
    --name $appConfigName `
    --key "Hartonomous:Inference:DefaultTemperature" `
    --value "0.7" `
    --yes

az appconfig kv set `
    --name $appConfigName `
    --key "Hartonomous:Inference:DefaultTopK" `
    --value "10" `
    --yes

az appconfig kv set `
    --name $appConfigName `
    --key "Hartonomous:Inference:SpatialPoolSize" `
    --value "1000" `
    --yes

# Feature flags
az appconfig feature set `
    --name $appConfigName `
    --feature "OodaLoopEnabled" `
    --yes

az appconfig feature set `
    --name $appConfigName `
    --feature "Neo4jSyncEnabled" `
    --yes

az appconfig feature set `
    --name $appConfigName `
    --feature "SpatialProjectionEnabled" `
    --yes

Write-Host "? App Configuration settings applied" -ForegroundColor Green
Write-Host ""

# =====================================================
# SUMMARY
# =====================================================
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?         INFRASTRUCTURE SETUP COMPLETE ?                ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""
Write-Host "Resources Created:" -ForegroundColor Cyan
Write-Host "  - Resource Group: $ResourceGroup" -ForegroundColor White
Write-Host "  - Key Vault: $keyVaultName" -ForegroundColor White
Write-Host "  - App Configuration: $appConfigName" -ForegroundColor White
Write-Host "  - API App Registration: $apiAppId" -ForegroundColor White
Write-Host "  - Blazor UI App Registration: $blazorAppId" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Update API appsettings.json with Key Vault reference" -ForegroundColor White
Write-Host "2. Update Blazor appsettings.json with Entra ID settings" -ForegroundColor White
Write-Host "3. Create Azure DevOps pipelines" -ForegroundColor White
Write-Host ""
Write-Host "Configuration Values:" -ForegroundColor Cyan
Write-Host "  Tenant ID: $tenantId" -ForegroundColor White
Write-Host "  API Client ID: $apiAppId" -ForegroundColor White
Write-Host "  Blazor Client ID: $blazorAppId" -ForegroundColor White
Write-Host "  Key Vault: https://$keyVaultName.vault.azure.net/" -ForegroundColor White
Write-Host ""
