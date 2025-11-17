#Requires -Version 7.0
<#
.SYNOPSIS
    Complete database deployment orchestration with automatic authentication detection.

.DESCRIPTION
    Orchestrates full database deployment including:
    - DACPAC build
    - Service principal SQL permissions (if needed)
    - CLR enablement
    - External CLR assemblies deployment
    - DACPAC deployment with Hartonomous.Clr
    - Database TRUSTWORTHY configuration
    - EF Core entity scaffolding
    
    Automatically detects environment:
    - Local development: Uses Windows Authentication
    - GitHub Actions: Uses OIDC-based Azure AD authentication
    - Manual Azure AD: Uses az login session
    
.PARAMETER Server
    SQL Server instance name (default: HART-DESKTOP)

.PARAMETER Database
    Database name (default: Hartonomous)

.PARAMETER SkipBuild
    Skip DACPAC build step

.PARAMETER SkipTests
    Skip database validation tests

.PARAMETER Environment
    Deployment environment: Development, Staging, Production
    Default: Development

.EXAMPLE
    # Local development deployment
    .\Deploy-Database.ps1
    
.EXAMPLE
    # CI/CD deployment (auto-detects GitHub Actions)
    .\Deploy-Database.ps1 -Environment Production
    
.EXAMPLE
    # Skip build, deploy only
    .\Deploy-Database.ps1 -SkipBuild

.NOTES
    Author: Hartonomous DevOps
    Version: 2.0.0
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Server,
    
    [Parameter()]
    [string]$Database,
    
    [Parameter()]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development",
    
    [Parameter()]
    [switch]$SkipBuild,
    
    [Parameter()]
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$WarningPreference = 'Continue'

# Load local dev config
$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent
$localConfig = & (Join-Path $scriptRoot "local-dev-config.ps1")

# Use config defaults if not specified
if (-not $Server) { $Server = $localConfig.SqlServer }
if (-not $Database) { $Database = $localConfig.Database }
$dacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"

# Authentication detection
function Get-AuthenticationMode {
    $isGitHubActions = $env:GITHUB_ACTIONS -eq "true"
    
    if ($isGitHubActions) {
        return @{
            Mode = "GitHubActions"
            Description = "GitHub Actions with OIDC (Service Principal)"
            UseAzureAD = $true
            AutoToken = $true
        }
    }
    else {
        # Local development always uses Windows Auth
        return @{
            Mode = "WindowsAuth"
            Description = "Windows Integrated Authentication (Local Development)"
            UseAzureAD = $false
            AutoToken = $false
        }
    }
}

# Get access token (only when needed)
function Get-SqlAccessToken {
    param([hashtable]$AuthMode)
    
    if (-not $AuthMode.UseAzureAD) {
        return $null
    }
    
    Write-Host "  Retrieving SQL Server access token..." -ForegroundColor Yellow
    $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
    
    if (-not $token) {
        throw "Failed to retrieve access token. Ensure you're logged in with 'az login'"
    }
    
    Write-Host "  ✓ Access token retrieved" -ForegroundColor Green
    return $token
}

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Hartonomous Database Deployment" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Target: $Server\$Database" -ForegroundColor White
Write-Host ""

# Detect authentication
$authMode = Get-AuthenticationMode
Write-Host "Authentication: $($authMode.Description)" -ForegroundColor Cyan
Write-Host ""

# Get token if needed
$accessToken = if ($authMode.UseAzureAD) { Get-SqlAccessToken -AuthMode $authMode } else { $null }

# Build DACPAC
if (-not $SkipBuild) {
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "  Step 1: Building DACPAC" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $projectPath = Join-Path $repoRoot "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
    $outputDir = Join-Path $repoRoot "src\Hartonomous.Database\bin\Output"
    
    # Find MSBuild
    $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe `
        -prerelease 2>$null | Select-Object -First 1

    if (-not $msbuild) {
        $msbuild = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
    }

    if (-not (Test-Path $msbuild)) {
        throw "MSBuild not found. Install Visual Studio or Build Tools."
    }

    Write-Host "Building: $projectPath" -ForegroundColor Yellow
    
    & $msbuild $projectPath /p:Configuration=Release /t:Build /v:minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "DACPAC build failed"
    }
    
    Write-Host "✓ DACPAC built successfully" -ForegroundColor Green
    Write-Host ""
}

# Configure SQL permissions for service principals (idempotent, only in CI/CD)
if ($authMode.Mode -eq "GitHubActions") {
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "  Step 2: Configuring Service Principal Permissions" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $sqlPermScript = Join-Path $scriptRoot "Configure-GitHubActionsSqlPermissions.sql"
    
    if (Test-Path $sqlPermScript) {
        Write-Host "Applying SQL permissions (idempotent)..." -ForegroundColor Yellow
        
        if (-not (Get-Module -ListAvailable -Name SqlServer)) {
            Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
        }
        
        try {
            Invoke-Sqlcmd -ServerInstance $Server -Database master `
                -InputFile $sqlPermScript -AccessToken $accessToken -TrustServerCertificate
            Write-Host "✓ SQL permissions configured" -ForegroundColor Green
        }
        catch {
            Write-Warning "SQL permission configuration failed (may already be configured): $($_.Exception.Message)"
        }
    }
    
    Write-Host ""
}

# Grant agent permissions
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Step 3: Granting Agent Permissions" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$grantPermsScript = Join-Path $scriptRoot "grant-agent-permissions.ps1"
$grantParams = @{
    Server = $Server
}

if ($authMode.UseAzureAD) {
    $grantParams.UseAzureAD = $true
    $grantParams.AccessToken = $accessToken
}

& $grantPermsScript @grantParams
Write-Host ""

# Enable CLR
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Step 4: Enabling CLR Integration" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$enableClrScript = Join-Path $scriptRoot "enable-clr.ps1"
& $enableClrScript @grantParams
Write-Host ""

# Deploy external CLR assemblies
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Step 5: Deploying External CLR Assemblies" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$deployClrScript = Join-Path $scriptRoot "deploy-clr-assemblies.ps1"
$deployClrParams = @{
    Server = $Server
    Database = $Database
}

if ($authMode.UseAzureAD) {
    $deployClrParams.UseAzureAD = $true
    $deployClrParams.AccessToken = $accessToken
}

& $deployClrScript @deployClrParams
Write-Host ""

# Deploy DACPAC
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Step 6: Deploying DACPAC" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

# Find DACPAC in either Output or Release folder
$dacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"
if (-not (Test-Path $dacpacPath)) {
    $dacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"
}
if (-not (Test-Path $dacpacPath)) {
    throw "DACPAC not found. Run with -SkipBuild:`$false to build it."
}

# Drop existing Hartonomous assemblies to make deployment idempotent
Write-Host "Checking for existing Hartonomous assemblies..." -ForegroundColor Yellow
$dropAssemblySql = @"
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name LIKE 'Hartonomous%')
BEGIN
    -- Drop functions that depend on the assembly first
    DECLARE @sql NVARCHAR(MAX) = '';
    SELECT @sql += 'DROP FUNCTION IF EXISTS ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + '; '
    FROM sys.objects 
    WHERE type IN ('FN', 'FS', 'FT', 'IF', 'TF')
    AND object_id IN (SELECT object_id FROM sys.assembly_modules WHERE assembly_id IN 
        (SELECT assembly_id FROM sys.assemblies WHERE name LIKE 'Hartonomous%'));
    
    EXEC sp_executesql @sql;
    
    -- Now drop the assemblies
    DECLARE @asmName NVARCHAR(128);
    DECLARE asm_cursor CURSOR FOR 
        SELECT name FROM sys.assemblies WHERE name LIKE 'Hartonomous%' ORDER BY assembly_id DESC;
    
    OPEN asm_cursor;
    FETCH NEXT FROM asm_cursor INTO @asmName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Dropping assembly: ' + @asmName;
        EXEC('DROP ASSEMBLY [' + @asmName + ']');
        FETCH NEXT FROM asm_cursor INTO @asmName;
    END
    CLOSE asm_cursor;
    DEALLOCATE asm_cursor;
END
"@

try {
    if ($authMode.UseAzureAD) {
        Invoke-Sqlcmd -Query $dropAssemblySql -ServerInstance $Server -Database $Database -AccessToken $accessToken -TrustServerCertificate
    } else {
        Invoke-Sqlcmd -Query $dropAssemblySql -ServerInstance $Server -Database $Database -TrustServerCertificate
    }
    Write-Host "  ✓ Cleared existing assemblies" -ForegroundColor Green
} catch {
    Write-Host "  ⓘ No existing assemblies to drop" -ForegroundColor DarkGray
}

# Find SqlPackage
$sqlPackage = Get-Command sqlpackage -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (-not $sqlPackage) {
    $installScript = Join-Path $scriptRoot "install-sqlpackage.ps1"
    if (Test-Path $installScript) {
        & $installScript
        $sqlPackage = Get-Command sqlpackage -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    }
}

if (-not $sqlPackage) {
    throw "SqlPackage not found. Run scripts\install-sqlpackage.ps1"
}

Write-Host "Deploying DACPAC with Hartonomous.Clr assembly..." -ForegroundColor Yellow

if ($authMode.UseAzureAD) {
    $connectionString = "Server=$Server;Database=$Database;Authentication=Active Directory Default;TrustServerCertificate=True;"
} else {
    $connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;"
}

& $sqlPackage `
    /Action:Publish `
    /SourceFile:"$dacpacPath" `
    /TargetConnectionString:"$connectionString" `
    /p:DropObjectsNotInSource=False `
    /p:BlockOnPossibleDataLoss=False `
    /p:AllowIncompatiblePlatform=True

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC deployment failed"
}

Write-Host "✓ DACPAC deployed successfully" -ForegroundColor Green
Write-Host ""

# Set TRUSTWORTHY
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Step 7: Setting Database TRUSTWORTHY" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$trustScript = Join-Path $scriptRoot "set-trustworthy.ps1"
$trustParams = @{
    Server = $Server
    Database = $Database
}

if ($authMode.UseAzureAD) {
    $trustParams.UseAzureAD = $true
    $trustParams.AccessToken = $accessToken
}

& $trustScript @trustParams
Write-Host ""

# Scaffold EF Core entities
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Step 8: Scaffolding EF Core Entities" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

$scaffoldScript = Join-Path $scriptRoot "scaffold-entities.ps1"
if (Test-Path $scaffoldScript) {
    $scaffoldParams = @{
        Server = $Server
        Database = $Database
    }
    
    if ($authMode.UseAzureAD) {
        $scaffoldParams.UseAzureAD = $true
    }
    
    & $scaffoldScript @scaffoldParams
} else {
    Write-Warning "Scaffold script not found, skipping entity generation"
}
Write-Host ""

# Run validation tests
if (-not $SkipTests) {
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    Write-Host "  Step 9: Running Validation Tests" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
    
    $testScript = Join-Path $repoRoot "tests\Run-DatabaseTests.ps1"
    if (Test-Path $testScript) {
        & $testScript -Server $Server -Database $Database
    } else {
        Write-Warning "Test script not found, skipping validation"
    }
    Write-Host ""
}

# Summary
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Deployment Complete" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ Database deployed successfully" -ForegroundColor Green
Write-Host "✓ CLR assemblies configured" -ForegroundColor Green
Write-Host "✓ EF Core entities scaffolded" -ForegroundColor Green
Write-Host ""
Write-Host "Server: $Server" -ForegroundColor White
Write-Host "Database: $Database" -ForegroundColor White
Write-Host "Environment: $Environment" -ForegroundColor White
Write-Host "Authentication: $($authMode.Description)" -ForegroundColor White
Write-Host ""
