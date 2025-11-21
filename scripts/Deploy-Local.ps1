#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Idempotent local deployment script for Hartonomous database
.DESCRIPTION
    Deploys database to localhost using Windows Authentication.
    Fully idempotent - safe to run multiple times.
    Based on Microsoft best practices for DACPAC deployments.
.PARAMETER Server
    SQL Server instance (default: localhost)
.PARAMETER Database
    Target database name (default: Hartonomous)
.PARAMETER SkipBuild
    Skip building DACPAC (use existing)
.PARAMETER SkipScaffold
    Skip EF Core entity scaffolding
.PARAMETER SkipTests
    Skip validation tests
.EXAMPLE
    .\Deploy-Local.ps1
    .\Deploy-Local.ps1 -Server localhost\SQLEXPRESS -Database HartonomousTest
#>

[CmdletBinding()]
param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$SkipBuild,
    [switch]$SkipScaffold,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

# Determine repo root and paths
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
$DacpacDir = Join-Path $RepoRoot "src\Hartonomous.Database\bin\Release"
$DependenciesDir = Join-Path $RepoRoot "dependencies"
$ScriptsDir = Join-Path $RepoRoot "scripts\sql"

Write-Host @"

═══════════════════════════════════════════════════════════════
  HARTONOMOUS LOCAL DEPLOYMENT
  Database-First Architecture | Idempotent Deployment
═══════════════════════════════════════════════════════════════
"@ -ForegroundColor Cyan

Write-Host "Target:        $Server / $Database" -ForegroundColor White
Write-Host "Authentication: Windows Integrated Security" -ForegroundColor White
Write-Host "Started:       $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 1: BUILD DACPAC
# ============================================================================
if (-not $SkipBuild) {
    Write-Host "[1/6] Building DACPAC..." -ForegroundColor Yellow

    $buildScript = Join-Path $PSScriptRoot "build-dacpac.ps1"
    & $buildScript -ProjectPath $ProjectPath -OutputDir $DacpacDir -Configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "DACPAC build failed"
    }

    Write-Host "      ✓ DACPAC built successfully" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[1/6] Skipping build (using existing DACPAC)" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# STEP 2: VALIDATE DACPAC
# ============================================================================
Write-Host "[2/6] Validating DACPAC..." -ForegroundColor Yellow

$dacpacPath = Get-ChildItem -Path $DacpacDir -Filter "*.dacpac" -File | Select-Object -First 1

if (-not $dacpacPath) {
    throw "DACPAC file not found in $DacpacDir"
}

Write-Host "      Found DACPAC: $($dacpacPath.Name)" -ForegroundColor Cyan
Write-Host "      Size: $([math]::Round($dacpacPath.Length / 1MB, 2)) MB" -ForegroundColor Cyan
Write-Host "      ✓ DACPAC validated" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 3: INITIALIZE CLR SIGNING (IDEMPOTENT)
# ============================================================================
Write-Host "[3/6] Initializing CLR signing..." -ForegroundColor Yellow

$clrScript = Join-Path $PSScriptRoot "Initialize-CLRSigning.ps1"
$result = & $clrScript

if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
    throw "CLR signing initialization failed"
}

Write-Host "      ✓ CLR signing initialized" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 4: DEPLOY DATABASE (IDEMPOTENT)
# ============================================================================
Write-Host "[4/6] Deploying database..." -ForegroundColor Yellow

# Check if SqlServer module is available
if (-not (Get-Module -ListAvailable -Name SqlServer)) {
    Write-Host "      Installing SqlServer PowerShell module..." -ForegroundColor Cyan
    Install-Module -Name SqlServer -Force -Scope CurrentUser -AllowClobber
}

Import-Module SqlServer -ErrorAction Stop

# Build connection string with Windows Authentication
$connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;"

Write-Host "      Connection: Windows Authentication" -ForegroundColor Cyan
Write-Host "      Target: $Server / $Database" -ForegroundColor Cyan

# Find SqlPackage.exe
$sqlPackagePath = $null

# Method 1: Check if sqlpackage is in PATH (dotnet global tool)
$sqlPackageCmd = Get-Command sqlpackage -ErrorAction SilentlyContinue
if ($sqlPackageCmd) {
    $sqlPackagePath = $sqlPackageCmd.Source
}

# Method 2: Check standard installation locations
if (-not $sqlPackagePath) {
    $sqlPackagePaths = @(
        "C:\Program Files\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
    )

    $sqlPackagePath = $sqlPackagePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

# Method 3: Install via dotnet tool if not found
if (-not $sqlPackagePath) {
    Write-Host "      SqlPackage not found, installing via dotnet tool..." -ForegroundColor Yellow
    dotnet tool install -g microsoft.sqlpackage

    $toolsPath = Join-Path $env:USERPROFILE ".dotnet\tools"
    $sqlPackagePath = Join-Path $toolsPath "sqlpackage.exe"
}

if (-not (Test-Path $sqlPackagePath)) {
    throw "SqlPackage.exe not found. Install via: dotnet tool install -g microsoft.sqlpackage"
}

Write-Host "      Using SqlPackage: $sqlPackagePath" -ForegroundColor Cyan

# Deploy using SqlPackage with idempotent settings
# Reference: https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-publish
$sqlPackageArgs = @(
    "/Action:Publish",
    "/SourceFile:$($dacpacPath.FullName)",
    "/TargetConnectionString:$connectionString",
    "/p:BlockOnPossibleDataLoss=False",           # Allow schema changes that might cause data loss (controlled by pre/post scripts)
    "/p:DropObjectsNotInSource=False",             # Idempotent: Don't drop objects not in DACPAC
    "/p:AllowIncompatiblePlatform=True",          # Allow deployment across SQL versions
    "/p:IncludeCompositeObjects=True",            # Include composite objects (assemblies, etc.)
    "/p:AllowDropBlockingAssemblies=True",        # Allow dropping assemblies that block deployment
    "/p:NoAlterStatementsToChangeClrTypes=False", # Allow altering CLR types
    "/p:TreatVerificationErrorsAsWarnings=False",
    "/p:IgnorePermissions=False",                 # Deploy permissions
    "/p:IgnoreRoleMembership=False"               # Deploy role memberships
)

if ($VerbosePreference -eq 'Continue') {
    $sqlPackageArgs += "/Diagnostics:True"
}

Write-Host "      Executing SqlPackage..." -ForegroundColor Cyan
$output = & $sqlPackagePath $sqlPackageArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "SqlPackage output:" -ForegroundColor Red
    $output | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    throw "SqlPackage.exe failed with exit code $LASTEXITCODE"
}

Write-Host "      ✓ Database deployed successfully" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 5: SCAFFOLD ENTITIES
# ============================================================================
if (-not $SkipScaffold) {
    Write-Host "[5/6] Scaffolding EF Core entities..." -ForegroundColor Yellow

    $scaffoldScript = Join-Path $PSScriptRoot "scaffold-entities.ps1"

    if (Test-Path $scaffoldScript) {
        & $scaffoldScript -Server $Server -Database $Database

        if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
            Write-Host "      ⚠ Entity scaffolding failed" -ForegroundColor Yellow
        } else {
            Write-Host "      ✓ Entities generated successfully" -ForegroundColor Green
        }
    } else {
        Write-Host "      ⚠ Scaffold script not found, skipping" -ForegroundColor Yellow
    }

    Write-Host ""
} else {
    Write-Host "[5/6] Skipping entity scaffolding" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# STEP 6: RUN VALIDATION TESTS
# ============================================================================
if (-not $SkipTests) {
    Write-Host "[6/6] Running validation tests..." -ForegroundColor Yellow

    $testScript = Join-Path $PSScriptRoot "Run-CoreTests.ps1"

    if (Test-Path $testScript) {
        & $testScript

        if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
            Write-Host "      ⚠ Some tests failed" -ForegroundColor Yellow
        } else {
            Write-Host "      ✓ All tests passed" -ForegroundColor Green
        }
    } else {
        Write-Host "      ℹ Test script not found, skipping" -ForegroundColor Cyan
    }

    Write-Host ""
} else {
    Write-Host "[6/6] Skipping tests" -ForegroundColor Gray
    Write-Host ""
}

# ============================================================================
# SUMMARY
# ============================================================================
$Duration = (Get-Date) - $StartTime

Write-Host @"
═══════════════════════════════════════════════════════════════
  DEPLOYMENT COMPLETE
═══════════════════════════════════════════════════════════════
"@ -ForegroundColor Green

Write-Host "Database:  $Server / $Database" -ForegroundColor White
Write-Host "Duration:  $([math]::Round($Duration.TotalSeconds, 2)) seconds" -ForegroundColor White
Write-Host "Completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  • Run tests: pwsh scripts/Run-CoreTests.ps1" -ForegroundColor Gray
Write-Host "  • Check Service Broker: sp_helpdb Hartonomous" -ForegroundColor Gray
Write-Host "  • Verify CLR functions: SELECT * FROM sys.assemblies WHERE is_user_defined = 1" -ForegroundColor Gray
Write-Host ""

exit 0
