#Requires -Version 7.0
<#
.SYNOPSIS
    Setup Windows self-hosted runner for Hartonomous CI/CD
.DESCRIPTION
    Installs and verifies all prerequisites for HART-DESKTOP runner
#>

$ErrorActionPreference = 'Continue'  # Allow script to continue on errors

Write-Host "`n=== Hartonomous Windows Runner Setup ===" -ForegroundColor Cyan
Write-Host "Target: HART-DESKTOP (Windows)" -ForegroundColor Gray
Write-Host ""

$checks = @()

# Check 1: .NET SDK
Write-Host "[1/8] Checking .NET SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version 2>$null
    $majorVersion = [int]($dotnetVersion.Split('.')[0])
    
    if ($majorVersion -ge 10) {
        Write-Host " [OK] $dotnetVersion" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = ".NET SDK"; Status = "OK"; Version = $dotnetVersion }
    } else {
        Write-Host " [FAIL] Version $dotnetVersion is too old (need 10.0+)" -ForegroundColor Red
        Write-Host "      Install: winget install Microsoft.DotNet.SDK.10" -ForegroundColor Yellow
        $checks += [PSCustomObject]@{ Name = ".NET SDK"; Status = "FAIL"; Version = $dotnetVersion }
    }
} catch {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.DotNet.SDK.10" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = ".NET SDK"; Status = "FAIL"; Version = "Not found" }
}

# Check 2: SQL Server
Write-Host "[2/8] Checking SQL Server..." -NoNewline
try {
    $null = sqlcmd -S localhost -E -Q "SELECT 1" -h -1 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK] Installed" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "SQL Server"; Status = "OK"; Version = "Installed" }
    } else {
        throw "Connection failed"
    }
} catch {
    Write-Host " [FAIL] Not installed or not running" -ForegroundColor Red
    Write-Host "      Download: https://www.microsoft.com/sql-server" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "SQL Server"; Status = "FAIL"; Version = "Not found" }
}

# Check 3: MSBuild (Visual Studio)
Write-Host "[3/8] Checking MSBuild..." -NoNewline
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null | Select-Object -First 1
    if ($msbuildPath) {
        Write-Host " [OK] Found" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "MSBuild"; Status = "OK"; Version = "VS 2022" }
    } else {
        Write-Host " [FAIL] Not found" -ForegroundColor Red
        Write-Host "      Install: winget install Microsoft.VisualStudio.2022.BuildTools" -ForegroundColor Yellow
        $checks += [PSCustomObject]@{ Name = "MSBuild"; Status = "FAIL"; Version = "Not found" }
    }
} else {
    Write-Host " [FAIL] Visual Studio not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.VisualStudio.2022.BuildTools" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "MSBuild"; Status = "FAIL"; Version = "Not found" }
}

# Check 4: PowerShell 7
Write-Host "[4/8] Checking PowerShell..." -NoNewline
$psVersion = $PSVersionTable.PSVersion.ToString()
if ($PSVersionTable.PSVersion.Major -ge 7) {
    Write-Host " [OK] $psVersion" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "PowerShell"; Status = "OK"; Version = $psVersion }
} else {
    Write-Host " [FAIL] Version $psVersion (need 7.0+)" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.PowerShell" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "PowerShell"; Status = "FAIL"; Version = $psVersion }
}

# Check 5: Git
Write-Host "[5/8] Checking Git..." -NoNewline
try {
    $gitVersion = git --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK] $gitVersion" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "Git"; Status = "OK"; Version = $gitVersion }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Git.Git" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "Git"; Status = "FAIL"; Version = "Not found" }
}

# Check 6: Azure CLI
Write-Host "[6/8] Checking Azure CLI..." -NoNewline
try {
    $azVersion = az version 2>&1 | ConvertFrom-Json
    if ($azVersion.'azure-cli') {
        Write-Host " [OK] $($azVersion.'azure-cli')" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "Azure CLI"; Status = "OK"; Version = $azVersion.'azure-cli' }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      Install: winget install Microsoft.AzureCLI" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "Azure CLI"; Status = "FAIL"; Version = "Not found" }
}

# Check 7: GitHub CLI
Write-Host "[7/8] Checking GitHub CLI..." -NoNewline
try {
    $ghVersion = gh --version 2>&1 | Select-String "gh version" | Select-Object -First 1
    if ($ghVersion) {
        Write-Host " [OK] $($ghVersion.ToString())" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "GitHub CLI"; Status = "OK"; Version = $ghVersion.ToString().Trim() }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " [WARN] Optional (recommended for testing)" -ForegroundColor Yellow
    Write-Host "      Install: winget install GitHub.cli" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "GitHub CLI"; Status = "WARN"; Version = "Not found" }
}

# Check 8: GitHub Actions Runner
Write-Host "[8/8] Checking GitHub Actions Runner..." -NoNewline
$runnerService = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
if ($runnerService) {
    if ($runnerService.Status -eq 'Running') {
        Write-Host " [OK] Running" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "GitHub Runner"; Status = "OK"; Version = "Running" }
    } else {
        Write-Host " [WARN] Not running" -ForegroundColor Yellow
        Write-Host "      Start with: Start-Service '$($runnerService.Name)'" -ForegroundColor Yellow
        $checks += [PSCustomObject]@{ Name = "GitHub Runner"; Status = "WARN"; Version = "Stopped" }
    }
} else {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      See: docs/ci-cd/RUNNER-SETUP.md" -ForegroundColor Yellow
    $checks += [PSCustomObject]@{ Name = "GitHub Runner"; Status = "FAIL"; Version = "Not found" }
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
$checks | Format-Table -Property Name, Status, Version -AutoSize

$failCount = ($checks | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($checks | Where-Object { $_.Status -eq "WARN" }).Count

Write-Host ""
if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host "[SUCCESS] ALL CHECKS PASSED - Runner is ready!" -ForegroundColor Green
    exit 0
} elseif ($failCount -eq 0) {
    Write-Host "[WARNING] $warnCount warnings found - Runner will work but some features may be limited" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "[FAILURE] $failCount critical issues found - Please install missing prerequisites" -ForegroundColor Red
    exit 1
}
