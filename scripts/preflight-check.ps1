# ============================================================================
# GitHub Actions Pre-Flight Check
# Validates environment and configuration before first workflow run
# ============================================================================

[CmdletBinding()]
param(
    [switch]$CheckAzureAuth,
    [switch]$CheckSQLConnection,
    [switch]$CheckRunner,
    [switch]$All
)

$ErrorActionPreference = 'Stop'
$WarningPreference = 'Continue'

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GitHub Actions Pre-Flight Check for Hartonomous" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$issues = @()
$warnings = @()

# Helper function to check command availability
function Test-CommandExists {
    param([string]$Command)
    
    $exists = Get-Command $Command -ErrorAction SilentlyContinue
    return $null -ne $exists
}

# Check PowerShell version
Write-Host "Checking PowerShell version..." -NoNewline
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 7) {
    Write-Host " ✓ $($psVersion.ToString())" -ForegroundColor Green
} else {
    Write-Host " ⚠ $($psVersion.ToString())" -ForegroundColor Yellow
    $warnings += "PowerShell 7+ recommended (current: $($psVersion.ToString()))"
}

# Check .NET SDK
Write-Host "Checking .NET SDK..." -NoNewline
if (Test-CommandExists 'dotnet') {
    $dotnetVersion = (dotnet --version)
    Write-Host " ✓ $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host " ✗ NOT FOUND" -ForegroundColor Red
    $issues += ".NET SDK not installed - Required for building"
}

# Check MSBuild
Write-Host "Checking MSBuild..." -NoNewline
if (Test-CommandExists 'msbuild') {
    $msbuildPath = (Get-Command msbuild).Source
    Write-Host " ✓ Found" -ForegroundColor Green
    Write-Host "  Path: $msbuildPath" -ForegroundColor DarkGray
} else {
    Write-Host " ✗ NOT FOUND" -ForegroundColor Red
    $issues += "MSBuild not found - Required for DACPAC builds"
}

# Check Azure CLI
Write-Host "Checking Azure CLI..." -NoNewline
if (Test-CommandExists 'az') {
    $azVersion = (az version --query '\"azure-cli\"' -o tsv)
    Write-Host " ✓ $azVersion" -ForegroundColor Green
} else {
    Write-Host " ✗ NOT FOUND" -ForegroundColor Red
    $issues += "Azure CLI not installed - Required for authentication"
}

# Check SqlPackage
Write-Host "Checking SqlPackage..." -NoNewline
if (Test-CommandExists 'sqlpackage') {
    $sqlpackageVersion = (sqlpackage /version 2>$null | Select-String -Pattern "\d+\.\d+\.\d+" | Select-Object -First 1)
    Write-Host " ✓ $($sqlpackageVersion.Matches.Value)" -ForegroundColor Green
} else {
    Write-Host " ⚠ NOT FOUND (will be auto-installed)" -ForegroundColor Yellow
    $warnings += "SqlPackage not found - will be installed automatically during deployment"
}

# Check SQL Server PowerShell module
Write-Host "Checking SqlServer PowerShell module..." -NoNewline
if (Get-Module -ListAvailable -Name SqlServer) {
    $sqlModule = Get-Module -ListAvailable -Name SqlServer | Select-Object -First 1
    Write-Host " ✓ $($sqlModule.Version)" -ForegroundColor Green
} else {
    Write-Host " ⚠ NOT FOUND" -ForegroundColor Yellow
    $warnings += "SqlServer PowerShell module not installed - may be needed for advanced operations"
}

Write-Host ""
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan

# Azure Authentication Check
if ($CheckAzureAuth -or $All) {
    Write-Host ""
    Write-Host "Testing Azure Authentication..." -ForegroundColor Cyan
    
    try {
        $account = az account show 2>$null | ConvertFrom-Json
        if ($account) {
            Write-Host " ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
            Write-Host "  Subscription: $($account.name) ($($account.id))" -ForegroundColor DarkGray
            
            # Test token retrieval
            Write-Host "  Testing SQL token retrieval..." -NoNewline
            $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv 2>$null
            if ($token) {
                Write-Host " ✓" -ForegroundColor Green
            } else {
                Write-Host " ✗" -ForegroundColor Red
                $issues += "Unable to retrieve SQL Server access token"
            }
        } else {
            Write-Host " ✗ Not logged in to Azure" -ForegroundColor Red
            $issues += "Not logged in to Azure CLI (run 'az login')"
        }
    } catch {
        Write-Host " ✗ Azure CLI authentication failed" -ForegroundColor Red
        $issues += "Azure CLI authentication check failed: $($_.Exception.Message)"
    }
}

# SQL Connection Check
if ($CheckSQLConnection -or $All) {
    Write-Host ""
    Write-Host "Testing SQL Server Connection..." -ForegroundColor Cyan
    
    $sqlServer = $env:SQL_SERVER
    $sqlDatabase = $env:SQL_DATABASE
    
    if (-not $sqlServer) {
        Write-Host " ⚠ SQL_SERVER environment variable not set" -ForegroundColor Yellow
        $warnings += "Set SQL_SERVER environment variable for connection testing"
    } elseif (-not $sqlDatabase) {
        Write-Host " ⚠ SQL_DATABASE environment variable not set" -ForegroundColor Yellow
        $warnings += "Set SQL_DATABASE environment variable for connection testing"
    } else {
        Write-Host "  Server: $sqlServer" -ForegroundColor DarkGray
        Write-Host "  Database: $sqlDatabase" -ForegroundColor DarkGray
        
        try {
            # Test if SqlServer module is available
            if (Get-Module -ListAvailable -Name SqlServer) {
                Import-Module SqlServer -ErrorAction Stop
                
                # Get access token
                $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv 2>$null
                
                if ($token) {
                    Write-Host "  Testing connection with access token..." -NoNewline
                    $query = "SELECT SUSER_NAME() AS CurrentUser, @@VERSION AS SqlVersion"
                    $result = Invoke-Sqlcmd -ServerInstance $sqlServer -Database $sqlDatabase -AccessToken $token -Query $query -ErrorAction Stop
                    
                    Write-Host " ✓" -ForegroundColor Green
                    Write-Host "  Connected as: $($result.CurrentUser)" -ForegroundColor Green
                } else {
                    Write-Host " ⚠ No access token available" -ForegroundColor Yellow
                    $warnings += "Cannot test SQL connection without Azure access token"
                }
            } else {
                Write-Host " ⚠ SqlServer module not available for testing" -ForegroundColor Yellow
            }
        } catch {
            Write-Host " ✗ Connection failed" -ForegroundColor Red
            $issues += "SQL Server connection test failed: $($_.Exception.Message)"
        }
    }
}

# GitHub Runner Check
if ($CheckRunner -or $All) {
    Write-Host ""
    Write-Host "Checking GitHub Actions Runner..." -ForegroundColor Cyan
    
    $runnerServices = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
    
    if ($runnerServices) {
        foreach ($service in $runnerServices) {
            $status = if ($service.Status -eq 'Running') { '✓' } else { '✗' }
            $color = if ($service.Status -eq 'Running') { 'Green' } else { 'Red' }
            
            Write-Host "  $status $($service.Name)" -ForegroundColor $color
            Write-Host "    Status: $($service.Status)" -ForegroundColor DarkGray
            Write-Host "    Startup: $($service.StartType)" -ForegroundColor DarkGray
            
            if ($service.Status -ne 'Running') {
                $issues += "GitHub Actions runner service is not running: $($service.Name)"
            }
        }
    } else {
        Write-Host " ⚠ No GitHub Actions runner services found" -ForegroundColor Yellow
        $warnings += "GitHub Actions runner not installed or not running as service"
    }
    
    # Check for runner in common locations
    $runnerPaths = @(
        "C:\actions-runner",
        "$env:USERPROFILE\actions-runner"
    )
    
    foreach ($path in $runnerPaths) {
        if (Test-Path $path) {
            Write-Host ""
            Write-Host "  Runner found at: $path" -ForegroundColor Green
            
            $configPath = Join-Path $path ".runner"
            if (Test-Path $configPath) {
                $config = Get-Content $configPath | ConvertFrom-Json
                Write-Host "  Repository: $($config.GitHubUrl)" -ForegroundColor DarkGray
                Write-Host "  Name: $($config.AgentName)" -ForegroundColor DarkGray
            }
            break
        }
    }
}

# Check GitHub environment variables
Write-Host ""
Write-Host "GitHub Environment Variables:" -ForegroundColor Cyan
$githubVars = @{
    'GITHUB_WORKSPACE' = $env:GITHUB_WORKSPACE
    'GITHUB_REPOSITORY' = $env:GITHUB_REPOSITORY
    'GITHUB_RUN_NUMBER' = $env:GITHUB_RUN_NUMBER
}

$inGitHubActions = $false
foreach ($var in $githubVars.GetEnumerator()) {
    if ($var.Value) {
        Write-Host "  ✓ $($var.Key): $($var.Value)" -ForegroundColor Green
        $inGitHubActions = $true
    } else {
        Write-Host "  ○ $($var.Key): Not set" -ForegroundColor DarkGray
    }
}

if (-not $inGitHubActions) {
    Write-Host ""
    Write-Host "  Note: Not running in GitHub Actions environment" -ForegroundColor Yellow
    Write-Host "  This is normal when running locally" -ForegroundColor DarkGray
}

# Summary
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($issues.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host ""
    Write-Host " ✓ All checks passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Your environment is ready for GitHub Actions." -ForegroundColor Green
    Write-Host "  You can now push your workflows and they should run successfully." -ForegroundColor Green
    Write-Host ""
} else {
    if ($issues.Count -gt 0) {
        Write-Host ""
        Write-Host " Critical Issues ($($issues.Count)):" -ForegroundColor Red
        foreach ($issue in $issues) {
            Write-Host "  ✗ $issue" -ForegroundColor Red
        }
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host " Warnings ($($warnings.Count)):" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "  ⚠ $warning" -ForegroundColor Yellow
        }
    }
    
    if ($issues.Count -gt 0) {
        Write-Host ""
        Write-Host "  Please resolve the critical issues before running workflows." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  Next Steps:" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Review docs/GITHUB-ACTIONS-MIGRATION.md" -ForegroundColor White
Write-Host "  2. Configure GitHub secrets (AZURE_CLIENT_ID, etc.)" -ForegroundColor White
Write-Host "  3. Install GitHub Actions runner if not already done" -ForegroundColor White
Write-Host "  4. Push workflows: git push origin main" -ForegroundColor White
Write-Host "  5. Monitor workflow execution in GitHub Actions tab" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
