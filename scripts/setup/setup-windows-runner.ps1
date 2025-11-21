#Requires -Version 7.0
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Setup Windows self-hosted runner for Hartonomous CI/CD
.DESCRIPTION
    Automatically installs and configures all prerequisites for HART-DESKTOP runner
#>

$ErrorActionPreference = 'Continue'  # Allow script to continue on errors

Write-Host "`n=== Hartonomous Windows Runner Setup ===" -ForegroundColor Cyan
Write-Host "Target: HART-DESKTOP (Windows)" -ForegroundColor Gray
Write-Host "This script will automatically install missing prerequisites" -ForegroundColor Gray
Write-Host ""

$checks = @()
$installedCount = 0

# Check 1: .NET SDK
Write-Host "[1/8] Checking .NET SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version 2>$null
    $majorVersion = [int]($dotnetVersion.Split('.')[0])
    
    if ($majorVersion -ge 10) {
        Write-Host " [OK] $dotnetVersion (already installed)" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = ".NET SDK"; Status = "OK"; Version = $dotnetVersion }
    } else {
        Write-Host " [WARN] Version $dotnetVersion is too old (need 10.0+)" -ForegroundColor Yellow
        Write-Host "      Installing .NET 10..." -ForegroundColor Yellow
        
        winget install --id Microsoft.DotNet.SDK.10 --silent --accept-package-agreements --accept-source-agreements
        
        $dotnetVersion = dotnet --version 2>$null
        Write-Host "      [OK] Installed .NET $dotnetVersion" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = ".NET SDK"; Status = "OK"; Version = "$dotnetVersion (installed)" }
        $installedCount++
    }
} catch {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      Installing .NET 10..." -ForegroundColor Yellow
    
    winget install --id Microsoft.DotNet.SDK.10 --silent --accept-package-agreements --accept-source-agreements
    
    $dotnetVersion = dotnet --version 2>$null
    Write-Host "      [OK] Installed .NET $dotnetVersion" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = ".NET SDK"; Status = "OK"; Version = "$dotnetVersion (installed)" }
    $installedCount++
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
    Write-Host " [WARN] Not installed or not running" -ForegroundColor Yellow
    Write-Host "      SQL Server requires manual installation" -ForegroundColor Yellow
    Write-Host "      Download: https://www.microsoft.com/sql-server" -ForegroundColor Gray
    $checks += [PSCustomObject]@{ Name = "SQL Server"; Status = "WARN"; Version = "Manual install needed" }
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
        Write-Host " [FAIL] Not found in Visual Studio" -ForegroundColor Red
        Write-Host "      Installing Visual Studio Build Tools..." -ForegroundColor Yellow
        
        winget install --id Microsoft.VisualStudio.2022.BuildTools --silent --override "--quiet --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.Component.MSBuild --add Microsoft.VisualStudio.Component.Roslyn.Compiler"
        
        Write-Host "      [OK] Installed Build Tools" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "MSBuild"; Status = "OK"; Version = "Installed" }
        $installedCount++
    }
} else {
    Write-Host " [FAIL] Visual Studio not installed" -ForegroundColor Red
    Write-Host "      Installing Visual Studio Build Tools..." -ForegroundColor Yellow
    
    winget install --id Microsoft.VisualStudio.2022.BuildTools --silent --override "--quiet --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.Component.MSBuild --add Microsoft.VisualStudio.Component.Roslyn.Compiler"
    
    Write-Host "      [OK] Installed Build Tools" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "MSBuild"; Status = "OK"; Version = "Installed" }
    $installedCount++
}

# Check 4: PowerShell 7
Write-Host "[4/8] Checking PowerShell..." -NoNewline
$psVersion = $PSVersionTable.PSVersion.ToString()
if ($PSVersionTable.PSVersion.Major -ge 7) {
    Write-Host " [OK] $psVersion (already installed)" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "PowerShell"; Status = "OK"; Version = $psVersion }
} else {
    Write-Host " [FAIL] Version $psVersion (need 7.0+)" -ForegroundColor Red
    Write-Host "      Installing PowerShell 7..." -ForegroundColor Yellow
    
    winget install --id Microsoft.PowerShell --silent --accept-package-agreements --accept-source-agreements
    
    Write-Host "      [OK] Installed PowerShell 7" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "PowerShell"; Status = "OK"; Version = "Installed" }
    $installedCount++
}

# Check 5: Git
Write-Host "[5/8] Checking Git..." -NoNewline
try {
    $gitVersion = git --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK] $gitVersion (already installed)" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "Git"; Status = "OK"; Version = $gitVersion }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      Installing Git..." -ForegroundColor Yellow
    
    winget install --id Git.Git --silent --accept-package-agreements --accept-source-agreements
    
    # Refresh PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    
    $gitVersion = git --version 2>&1
    Write-Host "      [OK] Installed Git" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "Git"; Status = "OK"; Version = "Installed" }
    $installedCount++
}

# Check 6: Azure CLI
Write-Host "[6/8] Checking Azure CLI..." -NoNewline
try {
    $azVersion = az version 2>&1 | ConvertFrom-Json
    if ($azVersion.'azure-cli') {
        Write-Host " [OK] $($azVersion.'azure-cli') (already installed)" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "Azure CLI"; Status = "OK"; Version = $azVersion.'azure-cli' }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " [FAIL] Not installed" -ForegroundColor Red
    Write-Host "      Installing Azure CLI..." -ForegroundColor Yellow
    
    winget install --id Microsoft.AzureCLI --silent --accept-package-agreements --accept-source-agreements
    
    # Refresh PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    
    Write-Host "      [OK] Installed Azure CLI" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "Azure CLI"; Status = "OK"; Version = "Installed" }
    $installedCount++
}

# Check 7: GitHub CLI
Write-Host "[7/8] Checking GitHub CLI..." -NoNewline
try {
    $ghVersion = gh --version 2>&1 | Select-String "gh version" | Select-Object -First 1
    if ($ghVersion) {
        Write-Host " [OK] $($ghVersion.ToString()) (already installed)" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "GitHub CLI"; Status = "OK"; Version = $ghVersion.ToString().Trim() }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " [WARN] Not installed (optional)" -ForegroundColor Yellow
    Write-Host "      Installing GitHub CLI..." -ForegroundColor Yellow
    
    winget install --id GitHub.cli --silent --accept-package-agreements --accept-source-agreements
    
    # Refresh PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
    
    Write-Host "      [OK] Installed GitHub CLI" -ForegroundColor Green
    $checks += [PSCustomObject]@{ Name = "GitHub CLI"; Status = "OK"; Version = "Installed" }
    $installedCount++
}

# Check 8: GitHub Actions Runner
Write-Host "[8/8] Checking GitHub Actions Runner..." -NoNewline
$runnerService = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
if ($runnerService) {
    if ($runnerService.Status -eq 'Running') {
        Write-Host " [OK] Running" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "GitHub Runner"; Status = "OK"; Version = "Running" }
    } else {
        Write-Host " [WARN] Not running (starting...)" -ForegroundColor Yellow
        Start-Service -Name $runnerService.Name
        Write-Host "      [OK] Started runner service" -ForegroundColor Green
        $checks += [PSCustomObject]@{ Name = "GitHub Runner"; Status = "OK"; Version = "Started" }
        $installedCount++
    }
} else {
    Write-Host " [WARN] Not installed" -ForegroundColor Yellow
    Write-Host "      GitHub Actions Runner requires manual setup with token" -ForegroundColor Yellow
    Write-Host "      See: docs/ci-cd/RUNNER-SETUP.md" -ForegroundColor Gray
    $checks += [PSCustomObject]@{ Name = "GitHub Runner"; Status = "WARN"; Version = "Manual setup needed" }
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
$checks | Format-Table -Property Name, Status, Version -AutoSize

$failCount = ($checks | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($checks | Where-Object { $_.Status -eq "WARN" }).Count
$okCount = ($checks | Where-Object { $_.Status -eq "OK" }).Count

Write-Host ""
Write-Host "=== Installation Summary ===" -ForegroundColor Cyan
Write-Host "  [OK] Ready to use: $okCount components" -ForegroundColor Green
Write-Host "  [OK] Newly installed: $installedCount components" -ForegroundColor Green
if ($warnCount -gt 0) {
    Write-Host "  [WARN] Manual setup needed: $warnCount components" -ForegroundColor Yellow
}
Write-Host ""

if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host "[SUCCESS] ALL PREREQUISITES INSTALLED - Runner is ready!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Restart your terminal to refresh PATH" -ForegroundColor Gray
    Write-Host "  2. Run GitHub Actions workflows" -ForegroundColor Gray
    exit 0
} elseif ($failCount -eq 0) {
    Write-Host "[SUCCESS] SETUP COMPLETE with $warnCount warnings" -ForegroundColor Yellow
    Write-Host "  Some components require manual configuration (see above)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Restart your terminal to refresh PATH" -ForegroundColor Gray
    Write-Host "  2. Complete manual setups listed above" -ForegroundColor Gray
    exit 0
} else {
    Write-Host "[FAILURE] $failCount critical issues found" -ForegroundColor Red
    exit 1
}
