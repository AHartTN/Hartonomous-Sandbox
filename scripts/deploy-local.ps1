#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-command local development database deployment for Hartonomous.

.DESCRIPTION
    Automated local database setup for developers. Executes complete deployment pipeline:
    1. Builds CLR assembly (SqlClrFunctions.dll)
    2. Runs deploy-database.ps1 orchestrator
    3. Optionally seeds test data

    Targets localhost SQL Server with integrated (Windows) authentication.

.PARAMETER ServerName
    SQL Server instance name (default: localhost)

.PARAMETER DatabaseName
    Database name (default: Hartonomous)

.PARAMETER SkipBuild
    Skip CLR assembly build (use existing DLL)

.PARAMETER SkipSeed
    Skip seeding test data after deployment

.PARAMETER DropExisting
    Drop existing database before deployment (DESTRUCTIVE - for clean slate)

.PARAMETER WhatIf
    Show what would be executed without actually running deployment

.EXAMPLE
    .\scripts\deploy-local.ps1

    Deploy to localhost with default settings

.EXAMPLE
    .\scripts\deploy-local.ps1 -DropExisting

    Drop existing database and deploy fresh

.EXAMPLE
    .\scripts\deploy-local.ps1 -ServerName "DESKTOP-PC\SQLEXPRESS"

    Deploy to named SQL Server instance
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory=$false)]
    [string]$ServerName = "localhost",

    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory=$false)]
    [switch]$SkipSeed,

    [Parameter(Mandatory=$false)]
    [switch]$DropExisting
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Resolve repository root
$scriptRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = $scriptRoot

Write-Host @"
╔════════════════════════════════════════════════════════════════════════════╗
║                                                                            ║
║           HARTONOMOUS LOCAL DEVELOPMENT DATABASE DEPLOYMENT                ║
║                                                                            ║
╚════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Server: $ServerName" -ForegroundColor Gray
Write-Host "  Database: $DatabaseName" -ForegroundColor Gray
Write-Host "  Authentication: Windows Integrated" -ForegroundColor Gray
Write-Host "  Repository: $repoRoot" -ForegroundColor Gray
Write-Host ""

# Step 1: Drop existing database (if requested)
if ($DropExisting) {
    Write-Host "WARNING: Dropping existing database!" -ForegroundColor Red -NoNewline
    Write-Host " (5 second countdown)" -ForegroundColor Yellow

    for ($i = 5; $i -gt 0; $i--) {
        Write-Host "  $i..." -ForegroundColor Yellow
        Start-Sleep -Seconds 1
    }

    if ($PSCmdlet.ShouldProcess($DatabaseName, "Drop database")) {
        Write-Host "Dropping database..." -NoNewline

        $dropQuery = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DatabaseName];
    PRINT 'Database dropped';
END
ELSE
BEGIN
    PRINT 'Database does not exist';
END
"@

        $output = sqlcmd -S $ServerName -E -C -Q $dropQuery 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " Done" -ForegroundColor Green
        } else {
            Write-Host " Failed" -ForegroundColor Red
            Write-Host "  $output" -ForegroundColor Red
        }
    }
}

# Step 2: Build CLR Assembly
if (-not $SkipBuild) {
    Write-Host "Building CLR assembly..." -NoNewline

    $clrProjectPath = Join-Path $repoRoot "src\SqlClr\SqlClrFunctions.csproj"

    if (-not (Test-Path $clrProjectPath)) {
        throw "CLR project not found: $clrProjectPath"
    }

    if ($PSCmdlet.ShouldProcess("SqlClrFunctions.csproj", "Build Release configuration")) {
        $buildOutput = dotnet build $clrProjectPath --configuration Release 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Host " Failed" -ForegroundColor Red
            Write-Host $buildOutput -ForegroundColor Red
            throw "CLR build failed"
        }

        Write-Host " Done" -ForegroundColor Green
    }
} else {
    Write-Host "Skipping CLR build (using existing DLL)" -ForegroundColor Yellow
}

# Verify CLR assembly exists
$assemblyPath = Join-Path $repoRoot "src\SqlClr\bin\Release\net8.0\SqlClrFunctions.dll"
if (-not (Test-Path $assemblyPath)) {
    throw "CLR assembly not found: $assemblyPath (run without -SkipBuild)"
}

$assemblySize = [math]::Round((Get-Item $assemblyPath).Length / 1KB, 2)
Write-Host "  Assembly: $assemblyPath ($assemblySize KB)" -ForegroundColor Gray

# Step 3: Execute deployment orchestrator
Write-Host ""
Write-Host "Executing deployment orchestrator..." -ForegroundColor Cyan

$deployScriptPath = Join-Path $repoRoot "scripts\deploy\deploy-database.ps1"
$dataProjectPath = Join-Path $repoRoot "src\Hartonomous.Data\Hartonomous.Data.csproj"

if (-not (Test-Path $deployScriptPath)) {
    throw "Deployment script not found: $deployScriptPath"
}

if (-not (Test-Path $dataProjectPath)) {
    throw "Data project not found: $dataProjectPath"
}

if ($PSCmdlet.ShouldProcess("deploy-database.ps1", "Execute deployment orchestrator")) {
    & pwsh -File $deployScriptPath `
        -ServerName $ServerName `
        -DatabaseName $DatabaseName `
        -ScriptsDirectory (Join-Path $repoRoot "scripts\deploy") `
        -AssemblyPath $assemblyPath `
        -ProjectPath $dataProjectPath `
        -Verbose

    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed with exit code $LASTEXITCODE"
    }
}

# Step 4: Seed test data (optional)
if (-not $SkipSeed) {
    Write-Host ""
    Write-Host "Seeding test data..." -ForegroundColor Cyan

    $seedScriptPath = Join-Path $repoRoot "scripts\seed-data.sql"

    if (Test-Path $seedScriptPath) {
        if ($PSCmdlet.ShouldProcess("seed-data.sql", "Execute seed data script")) {
            $seedOutput = sqlcmd -S $ServerName -d $DatabaseName -E -C -i $seedScriptPath 2>&1

            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Test data seeded" -ForegroundColor Green
            } else {
                Write-Host "⚠ Seed data script failed (non-fatal)" -ForegroundColor Yellow
                Write-Host "  $seedOutput" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "! No seed data script found at $seedScriptPath" -ForegroundColor Yellow
        Write-Host "  Skipping test data seeding" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "Skipping test data seeding" -ForegroundColor Yellow
}

# Step 5: Verification summary
Write-Host ""
Write-Host ("=" * 80) -ForegroundColor Green
Write-Host "✓ LOCAL DEVELOPMENT DATABASE DEPLOYED SUCCESSFULLY" -ForegroundColor Green
Write-Host ("=" * 80) -ForegroundColor Green
Write-Host ""
Write-Host "Connection String:" -ForegroundColor Yellow
Write-Host "  Server=$ServerName;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Update appsettings.json with connection string (if needed)" -ForegroundColor Gray
Write-Host "  2. Run application: dotnet run --project src/Hartonomous.Api" -ForegroundColor Gray
Write-Host "  3. Run tests: dotnet test" -ForegroundColor Gray
Write-Host ""

exit 0
