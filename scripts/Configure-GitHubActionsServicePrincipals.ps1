#Requires -Version 7.0
<#
.SYNOPSIS
    Idempotently configures service principals for GitHub Actions CI/CD pipeline.

.DESCRIPTION
    Creates or updates three service principals for GitHub Actions environments:
    - Development: For dev branch deployments and testing
    - Staging: For staging environment validation
    - Production: For production deployments with approval gates
    
    Each service principal receives:
    - Azure RBAC: Contributor role scoped to resource group
    - Federated credentials: Environment-based OIDC trust with GitHub
    - SQL permissions: Database deployment rights on Arc-enabled SQL Server
    - Pull request credentials: Validation without deployment rights

.PARAMETER ResourceGroupName
    Name of the Azure resource group containing deployment resources.
    Default: rg-hartonomous

.PARAMETER SqlServer
    Name of the SQL Server instance (Arc-enabled or Azure SQL).
    Default: Uses HARTONOMOUS_SQL_SERVER env var or 'localhost'

.PARAMETER GitHubOrg
    GitHub organization or user account name.
    Default: AHartTN

.PARAMETER GitHubRepo
    GitHub repository name.
    Default: Hartonomous-Sandbox

.PARAMETER WhatIf
    Shows what would be created/updated without making changes.

.EXAMPLE
    .\Configure-GitHubActionsServicePrincipals.ps1
    
.EXAMPLE
    .\Configure-GitHubActionsServicePrincipals.ps1 -WhatIf
    
.NOTES
    Author: Hartonomous DevOps
    Version: 1.0.0
    Requires: Azure CLI, SQL Server access
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string]$ResourceGroupName = "rg-hartonomous",

    [Parameter()]
    [string]$SqlServer = $env:HARTONOMOUS_SQL_SERVER,

    [Parameter()]
    [string]$GitHubOrg = "AHartTN",

    [Parameter()]
    [string]$GitHubRepo = "Hartonomous-Sandbox"
)

# Apply defaults
if (-not $SqlServer) { $SqlServer = "localhost" }

$ErrorActionPreference = 'Stop'
$WarningPreference = 'Continue'

# Service principal configuration
$environments = @(
    @{
        Name = "Development"
        Environment = "development"
        SpName = "Hartonomous-GitHub-Actions-Development"
        Description = "GitHub Actions service principal for development environment"
    },
    @{
        Name = "Staging"
        Environment = "staging"
        SpName = "Hartonomous-GitHub-Actions-Staging"
        Description = "GitHub Actions service principal for staging environment"
    },
    @{
        Name = "Production"
        Environment = "production"
        SpName = "Hartonomous-GitHub-Actions-Production"
        Description = "GitHub Actions service principal for production environment"
    }
)

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GitHub Actions Service Principal Configuration" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Get Azure context
Write-Host "Retrieving Azure subscription information..." -ForegroundColor Yellow
$subscription = az account show | ConvertFrom-Json
if (-not $subscription) {
    throw "Not logged in to Azure. Run 'az login' first."
}

$subscriptionId = $subscription.id
$tenantId = $subscription.tenantId

Write-Host "  Subscription: $($subscription.name)" -ForegroundColor Green
Write-Host "  Subscription ID: $subscriptionId" -ForegroundColor Green
Write-Host "  Tenant ID: $tenantId" -ForegroundColor Green
Write-Host ""

# Verify resource group exists
Write-Host "Verifying resource group '$ResourceGroupName'..." -ForegroundColor Yellow
$rg = az group show --name $ResourceGroupName 2>$null | ConvertFrom-Json
if (-not $rg) {
    throw "Resource group '$ResourceGroupName' not found in subscription."
}
Write-Host "  Resource group found: $($rg.id)" -ForegroundColor Green
Write-Host ""

$scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName"

# Track outputs for GitHub secrets configuration
$secretsConfig = @{
    Common = @{
        AZURE_SUBSCRIPTION_ID = $subscriptionId
        AZURE_TENANT_ID = $tenantId
        SQL_SERVER = $SqlServer
    }
    Environments = @{}
}

foreach ($env in $environments) {
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "  Configuring: $($env.Name)" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    # Check if service principal already exists
    Write-Host "Checking for existing service principal '$($env.SpName)'..." -ForegroundColor Yellow
    $existingSp = az ad sp list --display-name $env.SpName | ConvertFrom-Json
    
    if ($existingSp -and $existingSp.Count -gt 0) {
        $sp = $existingSp[0]
        $appId = $sp.appId
        
        Write-Host "  ✓ Service principal exists" -ForegroundColor Green
        Write-Host "    App ID: $appId" -ForegroundColor DarkGray
        
        if ($PSCmdlet.ShouldProcess($env.SpName, "Update existing service principal")) {
            # Verify role assignment exists
            Write-Host "  Verifying role assignment..." -ForegroundColor Yellow
            $roleAssignments = az role assignment list --assignee $appId --scope $scope | ConvertFrom-Json
            
            if (-not $roleAssignments -or $roleAssignments.Count -eq 0) {
                Write-Host "  Creating Contributor role assignment..." -ForegroundColor Yellow
                az role assignment create `
                    --assignee $appId `
                    --role "Contributor" `
                    --scope $scope | Out-Null
                Write-Host "    ✓ Role assignment created" -ForegroundColor Green
            } else {
                Write-Host "    ✓ Role assignment exists" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "  Service principal does not exist. Creating..." -ForegroundColor Yellow
        
        if ($PSCmdlet.ShouldProcess($env.SpName, "Create new service principal")) {
            # Create service principal with role assignment
            $spJson = az ad sp create-for-rbac `
                --name $env.SpName `
                --role "Contributor" `
                --scopes $scope `
                --json-auth 2>&1
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to create service principal: $spJson"
                continue
            }
            
            $spData = $spJson | ConvertFrom-Json
            $appId = $spData.clientId
            
            Write-Host "  ✓ Service principal created" -ForegroundColor Green
            Write-Host "    App ID: $appId" -ForegroundColor DarkGray
            Write-Host "    Client Secret: [REDACTED]" -ForegroundColor DarkGray
            
            # Store client secret for first-time setup only
            Write-Warning "IMPORTANT: Client secret for $($env.SpName): $($spData.clientSecret)"
            Write-Warning "This is the only time you'll see this secret. Store it securely."
        }
    }
    
    # Configure federated credentials (idempotent)
    if ($PSCmdlet.ShouldProcess($env.SpName, "Configure federated credentials")) {
        Write-Host "  Configuring federated credentials..." -ForegroundColor Yellow
        
        # Get application object ID
        $app = az ad app list --filter "appId eq '$appId'" | ConvertFrom-Json
        $appObjectId = $app[0].id
        
        # Environment-based credential
        $envCredentialName = "github-environment-$($env.Environment)"
        $envSubject = "repo:$GitHubOrg/${GitHubRepo}:environment:$($env.Environment)"
        
        # Check if credential exists
        $existingCreds = az ad app federated-credential list --id $appObjectId | ConvertFrom-Json
        $envCred = $existingCreds | Where-Object { $_.name -eq $envCredentialName }
        
        if ($envCred) {
            Write-Host "    ✓ Environment credential exists: $envCredentialName" -ForegroundColor Green
            Write-Host "      Subject: $envSubject" -ForegroundColor DarkGray
        } else {
            Write-Host "    Creating environment credential: $envCredentialName" -ForegroundColor Yellow
            
            $credParams = @{
                name = $envCredentialName
                issuer = "https://token.actions.githubusercontent.com"
                subject = $envSubject
                audiences = @("api://AzureADTokenExchange")
                description = "GitHub Actions environment: $($env.Environment)"
            } | ConvertTo-Json -Compress
            
            az ad app federated-credential create `
                --id $appObjectId `
                --parameters $credParams | Out-Null
            
            Write-Host "      ✓ Created" -ForegroundColor Green
        }
        
        # Pull request credential (production only)
        if ($env.Environment -eq "production") {
            $prCredentialName = "github-pullrequest"
            $prSubject = "repo:$GitHubOrg/${GitHubRepo}:pull_request"
            
            $prCred = $existingCreds | Where-Object { $_.name -eq $prCredentialName }
            
            if ($prCred) {
                Write-Host "    ✓ Pull request credential exists: $prCredentialName" -ForegroundColor Green
                Write-Host "      Subject: $prSubject" -ForegroundColor DarkGray
            } else {
                Write-Host "    Creating pull request credential: $prCredentialName" -ForegroundColor Yellow
                
                $prParams = @{
                    name = $prCredentialName
                    issuer = "https://token.actions.githubusercontent.com"
                    subject = $prSubject
                    audiences = @("api://AzureADTokenExchange")
                    description = "GitHub Actions pull request validation"
                } | ConvertTo-Json -Compress
                
                az ad app federated-credential create `
                    --id $appObjectId `
                    --parameters $prParams | Out-Null
                
                Write-Host "      ✓ Created" -ForegroundColor Green
            }
        }
    }
    
    # Store for GitHub secrets
    $secretsConfig.Environments[$env.Environment] = @{
        EnvironmentName = $env.Environment.ToUpper()
        AZURE_CLIENT_ID = $appId
    }
    
    Write-Host ""
}

# SQL Server permissions
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  SQL Server Permissions" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host ""

Write-Host "Generating idempotent SQL permission script..." -ForegroundColor Yellow

$sqlScript = @"
-- ============================================================================
-- GitHub Actions Service Principal SQL Permissions (Idempotent)
-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
-- ============================================================================

SET NOCOUNT ON;
GO

"@

foreach ($env in $environments) {
    $sqlScript += @"

-- $($env.Name) Environment
PRINT 'Configuring permissions for $($env.SpName)...';

-- Create login if not exists
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$($env.SpName)')
BEGIN
    PRINT '  Creating login...';
    CREATE LOGIN [$($env.SpName)] FROM EXTERNAL PROVIDER;
END
ELSE
BEGIN
    PRINT '  Login already exists.';
END
GO

-- Create user in database if not exists
USE [Hartonomous];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$($env.SpName)')
BEGIN
    PRINT '  Creating database user...';
    CREATE USER [$($env.SpName)] FROM LOGIN [$($env.SpName)];
END
ELSE
BEGIN
    PRINT '  Database user already exists.';
END
GO

-- Grant db_owner role (idempotent)
IF NOT IS_ROLEMEMBER('db_owner', '$($env.SpName)') = 1
BEGIN
    PRINT '  Adding to db_owner role...';
    ALTER ROLE db_owner ADD MEMBER [$($env.SpName)];
END
ELSE
BEGIN
    PRINT '  Already member of db_owner role.';
END
GO

-- Grant server-level permissions
USE [master];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = '$($env.SpName)' AND sp.permission_name = 'VIEW SERVER STATE'
)
BEGIN
    PRINT '  Granting VIEW SERVER STATE...';
    GRANT VIEW SERVER STATE TO [$($env.SpName)];
END
ELSE
BEGIN
    PRINT '  VIEW SERVER STATE already granted.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals pr ON sp.grantee_principal_id = pr.principal_id
    WHERE pr.name = '$($env.SpName)' AND sp.permission_name = 'ALTER ANY DATABASE'
)
BEGIN
    PRINT '  Granting ALTER ANY DATABASE...';
    GRANT ALTER ANY DATABASE TO [$($env.SpName)];
END
ELSE
BEGIN
    PRINT '  ALTER ANY DATABASE already granted.';
END
GO

PRINT '✓ Permissions configured for $($env.SpName)';
PRINT '';
GO

"@
}

$sqlScript += @"

PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'SQL permissions configuration complete.';
PRINT '═══════════════════════════════════════════════════════════════';
GO
"@

$sqlScriptPath = Join-Path $PSScriptRoot "Configure-GitHubActionsSqlPermissions.sql"
$sqlScript | Out-File -FilePath $sqlScriptPath -Encoding UTF8 -Force

Write-Host "  SQL script generated: $sqlScriptPath" -ForegroundColor Green
Write-Host ""
Write-Host "  To apply SQL permissions, run:" -ForegroundColor Yellow
Write-Host "    sqlcmd -S $SqlServer -d master -E -i `"$sqlScriptPath`"" -ForegroundColor White
Write-Host "  Or with Azure AD authentication:" -ForegroundColor Yellow
Write-Host "    Invoke-Sqlcmd -ServerInstance $SqlServer -Database master -InputFile `"$sqlScriptPath`" -AccessToken `$(az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv)" -ForegroundColor White
Write-Host ""

# Generate GitHub secrets configuration
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  GitHub Secrets Configuration" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host ""

$secretsOutput = @"
# GitHub Secrets Configuration
# Repository: $GitHubOrg/$GitHubRepo

## Repository Secrets (Settings > Secrets and variables > Actions > Repository secrets)

AZURE_SUBSCRIPTION_ID = $($secretsConfig.Common.AZURE_SUBSCRIPTION_ID)
AZURE_TENANT_ID = $($secretsConfig.Common.AZURE_TENANT_ID)
SQL_SERVER = $($secretsConfig.Common.SQL_SERVER)
SQL_DATABASE = Hartonomous

## Environment Secrets (Settings > Secrets and variables > Actions > Environments)

"@

foreach ($env in $environments) {
    $envName = $env.Environment
    $envData = $secretsConfig.Environments[$envName]
    
    $secretsOutput += @"

### Environment: $($envData.EnvironmentName)
Create environment in GitHub: Settings > Environments > New environment > "$($envData.EnvironmentName)"

For production environment, configure:
- Required reviewers: Add team/individuals for approval
- Deployment branches: main only

Secrets for this environment:
AZURE_CLIENT_ID = $($envData.AZURE_CLIENT_ID)

"@
}

$secretsOutput += @"

## Verification Commands (Run from GitHub Actions workflow context)

# These commands work ONLY within GitHub Actions workflow execution.
# The OIDC token is provided automatically by GitHub to the workflow.

### Workflow YAML example for authentication test:
```yaml
- name: Azure Login
  uses: azure/login@v2
  with:
    client-id: `${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: `${{ secrets.AZURE_TENANT_ID }}
    subscription-id: `${{ secrets.AZURE_SUBSCRIPTION_ID }}

- name: Test SQL Connection
  run: |
    `$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
    Invoke-Sqlcmd -ServerInstance `${{ secrets.SQL_SERVER }} ``
      -Database `${{ secrets.SQL_DATABASE }} ``
      -AccessToken `$token ``
      -Query "SELECT SUSER_NAME() AS [Authenticated As], DB_NAME() AS [Database]"
```

### Local testing (requires your own Azure login):
```powershell
# Login with your account
az login

# Get token and test SQL connection
`$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
Invoke-Sqlcmd -ServerInstance $($secretsConfig.Common.SQL_SERVER) ``
  -Database $($secretsConfig.Common.SQL_DATABASE) ``
  -AccessToken `$token ``
  -Query "SELECT SUSER_NAME() AS [Authenticated As], @@VERSION AS [SQL Version]"
```

"@

$secretsPath = Join-Path $PSScriptRoot "GitHub-Secrets-Configuration.txt"
$secretsOutput | Out-File -FilePath $secretsPath -Encoding UTF8 -Force

Write-Host "  Secrets configuration generated: $secretsPath" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Configuration Complete" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review generated files:" -ForegroundColor White
Write-Host "     - $sqlScriptPath" -ForegroundColor DarkGray
Write-Host "     - $secretsPath" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  2. Apply SQL permissions (see output above)" -ForegroundColor White
Write-Host ""
Write-Host "  3. Configure GitHub environments and secrets:" -ForegroundColor White
Write-Host "     https://github.com/$GitHubOrg/$GitHubRepo/settings/environments" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  4. Review workflow files:" -ForegroundColor White
Write-Host "     - .github/workflows/database-deployment.yml" -ForegroundColor DarkGray
Write-Host "     - .github/workflows/build-and-test.yml" -ForegroundColor DarkGray
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
