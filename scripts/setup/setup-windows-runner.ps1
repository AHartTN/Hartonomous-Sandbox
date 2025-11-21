#Requires -Version 7.0
<#
.SYNOPSIS
    Setup Windows self-hosted runner for Hartonomous CI/CD
.DESCRIPTION
    Installs and verifies all prerequisites for HART-DESKTOP runner
#>

$ErrorActionPreference = 'Stop'

Write-Host "`n=== Hartonomous Windows Runner Setup ===" -ForegroundColor Cyan
Write-Host "Target: HART-DESKTOP (Windows)" -ForegroundColor Gray
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

# Check 7: GitHub CLI
Write-Host "[7/8] Checking GitHub CLI..." -NoNewline
try {
    $ghVersion = gh --version 2>&1 | Select-String "gh version" | Select-Object -First 1
    if ($ghVersion) {
        Write-Host " ? $ghVersion" -ForegroundColor Green
        $checks += @{ Name = "GitHub CLI"; Status = "?"; Version = $ghVersion.ToString() }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " ?? Optional (recommended for testing)" -ForegroundColor Yellow
    Write-Host "      Install: winget install GitHub.cli" -ForegroundColor Yellow
    $checks += @{ Name = "GitHub CLI"; Status = "??"; Version = "Not found" }
}

# Check 8: GitHub Actions Runner
Write-Host "[8/8] Checking GitHub Actions Runner..." -NoNewline
$runnerService = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
if ($runnerService) {
    if ($runnerService.Status -eq 'Running') {
        Write-Host " ? Running" -ForegroundColor Green
        $checks += @{ Name = "GitHub Runner"; Status = "?"; Version = "Running" }
    } else {
        Write-Host " ?? Not running" -ForegroundColor Yellow
        Write-Host "      Start with: Start-Service '$($runnerService.Name)'" -ForegroundColor Yellow
        $checks += @{ Name = "GitHub Runner"; Status = "??"; Version = "Stopped" }
    }
} else {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      See: docs/ci-cd/RUNNER-SETUP.md" -ForegroundColor Yellow
    $checks += @{ Name = "GitHub Runner"; Status = "?"; Version = "Not found" }
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
$checks | Format-Table -Property Name, Status, Version -AutoSize

$failCount = ($checks | Where-Object { $_.Status -eq "?" }).Count
$warnCount = ($checks | Where-Object { $_.Status -eq "??" }).Count

Write-Host ""
if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host "? ALL CHECKS PASSED - Runner is ready!" -ForegroundColor Green
    exit 0
} elseif ($failCount -eq 0) {
    Write-Host "?? WARNINGS FOUND - Runner will work but some features may be limited" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "? CRITICAL ISSUES FOUND ($failCount) - Please install missing prerequisites" -ForegroundColor Red
    exit 1
}
