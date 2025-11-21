#Requires -Version 7.0
<#
.SYNOPSIS
    Setup Azure DevOps agent for Hartonomous CI/CD
.DESCRIPTION
    Verifies prerequisites for Azure DevOps self-hosted agents (same requirements as GitHub)
#>

$ErrorActionPreference = 'Stop'

Write-Host "`n=== Hartonomous Azure DevOps Agent Setup ===" -ForegroundColor Cyan
Write-Host "Target: Azure Pipelines Self-Hosted Agent" -ForegroundColor Gray
Write-Host ""

$checks = @()

# Check 1: .NET SDK
Write-Host "[1/8] Checking .NET SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version
    $majorVersion = [int]($dotnetVersion.Split('.')[0])
    
    if ($majorVersion -ge 10) {
        Write-Host " ? $dotnetVersion" -ForegroundColor Green
        $checks += @{ Name = ".NET SDK"; Status = "?"; Version = $dotnetVersion }
    } else {
        Write-Host " ? Version $dotnetVersion is too old (need 10.0+)" -ForegroundColor Red
        Write-Host "      Install: winget install Microsoft.DotNet.SDK.10" -ForegroundColor Yellow
        $checks += @{ Name = ".NET SDK"; Status = "?"; Version = $dotnetVersion }
    }
} catch {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.DotNet.SDK.10" -ForegroundColor Yellow
    $checks += @{ Name = ".NET SDK"; Status = "?"; Version = "Not found" }
}

# Check 2: SQL Server
Write-Host "[2/8] Checking SQL Server..." -NoNewline
try {
    $sqlVersion = sqlcmd -S localhost -E -Q "SELECT @@VERSION" -h -1 2>&1 | Select-Object -First 1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ? Installed" -ForegroundColor Green
        $checks += @{ Name = "SQL Server"; Status = "?"; Version = "Installed" }
    } else {
        throw "Connection failed"
    }
} catch {
    Write-Host " ? Not installed or not running" -ForegroundColor Red
    Write-Host "      Download: https://www.microsoft.com/sql-server" -ForegroundColor Yellow
    $checks += @{ Name = "SQL Server"; Status = "?"; Version = "Not found" }
}

# Check 3: MSBuild (Visual Studio)
Write-Host "[3/8] Checking MSBuild..." -NoNewline
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    if ($msbuildPath) {
        Write-Host " ? Found" -ForegroundColor Green
        $checks += @{ Name = "MSBuild"; Status = "?"; Version = "VS 2022" }
    } else {
        Write-Host " ? Not found" -ForegroundColor Red
        Write-Host "      Install: winget install Microsoft.VisualStudio.2022.BuildTools" -ForegroundColor Yellow
        $checks += @{ Name = "MSBuild"; Status = "?"; Version = "Not found" }
    }
} else {
    Write-Host " ? Visual Studio not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.VisualStudio.2022.BuildTools" -ForegroundColor Yellow
    $checks += @{ Name = "MSBuild"; Status = "?"; Version = "Not found" }
}

# Check 4: PowerShell 7
Write-Host "[4/8] Checking PowerShell..." -NoNewline
$psVersion = $PSVersionTable.PSVersion.ToString()
if ($PSVersionTable.PSVersion.Major -ge 7) {
    Write-Host " ? $psVersion" -ForegroundColor Green
    $checks += @{ Name = "PowerShell"; Status = "?"; Version = $psVersion }
} else {
    Write-Host " ? Version $psVersion (need 7.0+)" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.PowerShell" -ForegroundColor Yellow
    $checks += @{ Name = "PowerShell"; Status = "?"; Version = $psVersion }
}

# Check 5: Git
Write-Host "[5/8] Checking Git..." -NoNewline
try {
    $gitVersion = git --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ? $gitVersion" -ForegroundColor Green
        $checks += @{ Name = "Git"; Status = "?"; Version = $gitVersion }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Git.Git" -ForegroundColor Yellow
    $checks += @{ Name = "Git"; Status = "?"; Version = "Not found" }
}

# Check 6: Azure CLI
Write-Host "[6/8] Checking Azure CLI..." -NoNewline
try {
    $azVersion = az --version 2>&1 | Select-String "azure-cli" | Select-Object -First 1
    if ($azVersion) {
        Write-Host " ? Installed" -ForegroundColor Green
        $checks += @{ Name = "Azure CLI"; Status = "?"; Version = "Installed" }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.AzureCLI" -ForegroundColor Yellow
    $checks += @{ Name = "Azure CLI"; Status = "?"; Version = "Not found" }
}

# Check 7: Azure Pipelines Agent (vstsagent service)
Write-Host "[7/8] Checking Azure Pipelines Agent..." -NoNewline
$agentService = Get-Service -Name "vstsagent.*" -ErrorAction SilentlyContinue
if ($agentService) {
    if ($agentService.Status -eq 'Running') {
        Write-Host " ? Running" -ForegroundColor Green
        $checks += @{ Name = "Azure Agent"; Status = "?"; Version = "Running" }
    } else {
        Write-Host " ?? Not running" -ForegroundColor Yellow
        Write-Host "      Start with: Start-Service '$($agentService.Name)'" -ForegroundColor Yellow
        $checks += @{ Name = "Azure Agent"; Status = "??"; Version = "Stopped" }
    }
} else {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      See: https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/windows-agent" -ForegroundColor Yellow
    $checks += @{ Name = "Azure Agent"; Status = "?"; Version = "Not found" }
}

# Check 8: SqlServer PowerShell Module (for TrustServerCertificate support)
Write-Host "[8/8] Checking SqlServer Module..." -NoNewline
if (Get-Module -ListAvailable -Name SqlServer) {
    Write-Host " ? Installed" -ForegroundColor Green
    $checks += @{ Name = "SqlServer Module"; Status = "?"; Version = "Installed" }
} else {
    Write-Host " ?? Not installed (will be installed on-demand)" -ForegroundColor Yellow
    Write-Host "      Optional: Install-Module -Name SqlServer -Force -AllowClobber" -ForegroundColor Yellow
    $checks += @{ Name = "SqlServer Module"; Status = "??"; Version = "Not found" }
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
$checks | Format-Table -Property Name, Status, Version -AutoSize

$failCount = ($checks | Where-Object { $_.Status -eq "?" }).Count
$warnCount = ($checks | Where-Object { $_.Status -eq "??" }).Count

Write-Host ""
if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host "? ALL CHECKS PASSED - Agent is ready!" -ForegroundColor Green
    exit 0
} elseif ($failCount -eq 0) {
    Write-Host "?? WARNINGS FOUND ($warnCount) - Agent will work but some features may be limited" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "? CRITICAL ISSUES FOUND ($failCount) - Please install missing prerequisites" -ForegroundColor Red
    exit 1
}
