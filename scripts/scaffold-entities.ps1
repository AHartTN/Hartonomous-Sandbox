#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Scaffolds EF Core entities from deployed database
.DESCRIPTION
    Enterprise-grade EF Core scaffolding with proper error handling.
    - Backs up existing entities
    - Cleans old generated files
    - Scaffolds fresh from database (idempotent via --force)
    - Verifies successful generation
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [switch]$IntegratedSecurity,
    [string]$User,
    [string]$Password,
    [switch]$TrustServerCertificate = $true
)

$ErrorActionPreference = "Stop"

$ProjectPath = "src\Hartonomous.Data.Entities\Hartonomous.Data.Entities.csproj"
$EntitiesDir = "src\Hartonomous.Data.Entities\Entities"
$ContextFile = "src\Hartonomous.Data.Entities\HartonomousDbContext.cs"

# Verify project exists
if (-not (Test-Path $ProjectPath)) {
    throw "Entity project not found at: $ProjectPath"
}

# Build connection string
if ($IntegratedSecurity -or (-not $User)) {
    $connectionString = "Server=$Server;Database=$Database;Integrated Security=True;"
} else {
    $connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;"
}

if ($TrustServerCertificate) {
    $connectionString += "TrustServerCertificate=True;"
}

Write-Host "Preparing to scaffold entities..." -ForegroundColor Cyan

# Backup existing entities if they exist
if (Test-Path $EntitiesDir) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupDir = "src\Hartonomous.Data.Entities\Entities.backup_$timestamp"
    
    Write-Host "  Backing up existing entities to: $backupDir" -ForegroundColor Gray
    Copy-Item -Path $EntitiesDir -Destination $backupDir -Recurse -Force
}

# Delete old generated files to avoid build errors
if (Test-Path $EntitiesDir) {
    Write-Host "  Removing old entity files..." -ForegroundColor Gray
    Remove-Item -Path $EntitiesDir -Recurse -Force
}

if (Test-Path $ContextFile) {
    Write-Host "  Removing old DbContext file..." -ForegroundColor Gray
    Remove-Item -Path $ContextFile -Force
}

# Remove any old backups that may be interfering with build
$oldBackups = Get-ChildItem -Path "src\Hartonomous.Data.Entities" -Directory -Filter "Entities.backup_*"
if ($oldBackups.Count -gt 0) {
    Write-Host "  Removing $($oldBackups.Count) old backup directories..." -ForegroundColor Gray
    $oldBackups | Remove-Item -Recurse -Force
}

# Remove Interface files that may have stale references
$interfacesDir = "src\Hartonomous.Data.Entities\Interfaces"
if (Test-Path $interfacesDir) {
    Write-Host "  Removing old interface files..." -ForegroundColor Gray
    Remove-Item -Path $interfacesDir -Recurse -Force
}

# Remove Configuration files that may have stale references
$configurationsDir = "src\Hartonomous.Data.Entities\Configurations"
if (Test-Path $configurationsDir) {
    Write-Host "  Removing old configuration files..." -ForegroundColor Gray
    Remove-Item -Path $configurationsDir -Recurse -Force
}

# Scaffold fresh entities from database
Write-Host "Scaffolding entities from $Server/$Database..." -ForegroundColor Cyan
Write-Host "  This will generate fresh DbContext and entity classes" -ForegroundColor Gray

dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer `
    --project $ProjectPath `
    --output-dir Entities `
    --context HartonomousDbContext `
    --context-dir . `
    --namespace Hartonomous.Data.Entities `
    --context-namespace Hartonomous.Data.Entities `
    --force `
    --no-onconfiguring `
    --no-pluralize `
    --verbose

if ($LASTEXITCODE -ne 0) {
    throw "Entity scaffolding failed with exit code $LASTEXITCODE"
}

Write-Host "  Scaffolding complete" -ForegroundColor Green

# Verify generation
if (Test-Path $ContextFile) {
    Write-Host "  DbContext: $ContextFile" -ForegroundColor Green
} else {
    throw "DbContext was not generated at expected path: $ContextFile"
}

if (Test-Path $EntitiesDir) {
    $entityFiles = Get-ChildItem $EntitiesDir -Filter "*.cs"
    Write-Host "  Entities: $($entityFiles.Count) classes generated" -ForegroundColor Green
} else {
    throw "Entities directory was not created at: $EntitiesDir"
}

# Verify project builds after scaffolding
Write-Host "Verifying project builds..." -ForegroundColor Cyan
dotnet build $ProjectPath --verbosity quiet --nologo

if ($LASTEXITCODE -ne 0) {
    throw "Entity project failed to build after scaffolding"
}

Write-Host "  Project builds successfully" -ForegroundColor Green

exit 0
