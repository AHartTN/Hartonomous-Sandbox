#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local development deployment script for Hartonomous.

.DESCRIPTION
    Sets up and deploys Hartonomous for local development without Azure dependencies.
    - Applies SQL migrations to local SQL Server
    - Deploys SQL CLR assembly
    - Sets up Service Broker and CDC
    - Configures Neo4j (optional)
    
.PARAMETER SqlServer
    SQL Server instance (default: localhost)
    
.PARAMETER Database
    Database name (default: Hartonomous)
    
.PARAMETER Neo4jUri
    Neo4j connection URI (default: bolt://localhost:7687)
    
.PARAMETER SkipNeo4j
    Skip Neo4j setup
    
.PARAMETER SkipMigrations
    Skip EF Core migrations

.EXAMPLE
    .\deploy-local-dev.ps1
    
.EXAMPLE
    .\deploy-local-dev.ps1 -SqlServer "localhost\SQLEXPRESS" -SkipNeo4j
#>

[CmdletBinding()]
param(
    [string]$SqlServer = "localhost",
    [string]$Database = "Hartonomous",
    [string]$Neo4jUri = "bolt://localhost:7687",
    [switch]$SkipNeo4j,
    [switch]$SkipMigrations
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Hartonomous Local Development Deployment" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Function to test SQL connection
function Test-SqlConnection {
    param([string]$ConnectionString)
    
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $conn.Open()
        $conn.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Build connection string
$connectionString = "Server=$SqlServer;Database=$Database;Integrated Security=true;TrustServerCertificate=true;"
Write-Host "Testing SQL Server connection..." -ForegroundColor Yellow
Write-Host "  Server: $SqlServer" -ForegroundColor Gray
Write-Host "  Database: $Database" -ForegroundColor Gray

if (-not (Test-SqlConnection -ConnectionString $connectionString)) {
    Write-Error "Cannot connect to SQL Server. Ensure SQL Server is running and accessible."
    exit 1
}
Write-Host "✓ SQL Server connection successful" -ForegroundColor Green
Write-Host ""

# Step 1: Build Solution
Write-Host "[1/6] Building solution..." -ForegroundColor Yellow
Push-Location $repoRoot
try {
    dotnet build Hartonomous.sln --configuration Release --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✓ Build successful" -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# Step 2: Apply EF Core Migrations
if (-not $SkipMigrations) {
    Write-Host "[2/6] Applying EF Core migrations..." -ForegroundColor Yellow
    
    # Generate idempotent migration script
    $migrationsScript = "$repoRoot\sql\migrations_local.sql"
    Push-Location "$repoRoot\src\Hartonomous.Data"
    try {
        dotnet ef migrations script --idempotent --context HartonomousDbContext --output $migrationsScript
        if ($LASTEXITCODE -ne 0) {
            throw "Migration script generation failed"
        }
    }
    finally {
        Pop-Location
    }
    
    # Apply migrations
    sqlcmd -S $SqlServer -d $Database -E -i $migrationsScript -b
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migration application failed"
        exit 1
    }
    
    Write-Host "✓ Migrations applied" -ForegroundColor Green
}
else {
    Write-Host "[2/6] Skipping migrations" -ForegroundColor Gray
}
Write-Host ""

# Step 3: Deploy SQL CLR Assembly
Write-Host "[3/6] Deploying SQL CLR assembly..." -ForegroundColor Yellow

# Enable CLR integration
$enableClrSql = @"
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
"@
$enableClrSql | sqlcmd -S $SqlServer -d $Database -E -b

# Get DLL path
$dllPath = "$repoRoot\src\SqlClr\bin\Release\SqlClrFunctions.dll"
if (-not (Test-Path $dllPath)) {
    Write-Error "SqlClrFunctions.dll not found at: $dllPath"
    exit 1
}

# Read DLL as hex
$dllBytes = [System.IO.File]::ReadAllBytes($dllPath)
$hexString = ($dllBytes | ForEach-Object { $_.ToString("X2") }) -join ''

# Deploy assembly
$deploySql = @"
USE [$Database];

IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'SqlClrFunctions')
    DROP ASSEMBLY SqlClrFunctions;

CREATE ASSEMBLY SqlClrFunctions
FROM 0x$hexString
WITH PERMISSION_SET = UNSAFE;
"@

$deploySql | sqlcmd -S $SqlServer -d $Database -E -b
if ($LASTEXITCODE -ne 0) {
    Write-Error "CLR assembly deployment failed"
    exit 1
}

Write-Host "✓ SQL CLR assembly deployed" -ForegroundColor Green
Write-Host ""

# Step 4: Execute SQL setup scripts
Write-Host "[4/6] Executing SQL setup scripts..." -ForegroundColor Yellow

$sqlScripts = @(
    "$repoRoot\scripts\enable-cdc.sql",
    "$repoRoot\scripts\setup-service-broker.sql",
    "$repoRoot\scripts\verify-temporal-tables.sql"
)

foreach ($script in $sqlScripts) {
    if (Test-Path $script) {
        $scriptName = Split-Path -Leaf $script
        Write-Host "  Executing $scriptName..." -ForegroundColor Gray
        sqlcmd -S $SqlServer -d $Database -E -i $script -b
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Script $scriptName failed (may be expected)"
        }
    }
}

Write-Host "✓ SQL setup scripts executed" -ForegroundColor Green
Write-Host ""

# Step 5: Neo4j Setup (optional)
if (-not $SkipNeo4j) {
    Write-Host "[5/6] Setting up Neo4j schema..." -ForegroundColor Yellow
    
    $neo4jSchema = "$repoRoot\neo4j\schemas\CoreSchema.cypher"
    if (Test-Path $neo4jSchema) {
        Write-Host "  Applying CoreSchema.cypher..." -ForegroundColor Gray
        Write-Host "  Neo4j URI: $Neo4jUri" -ForegroundColor Gray
        
        # Try to apply schema (requires cypher-shell to be in PATH)
        $cypherShell = Get-Command cypher-shell -ErrorAction SilentlyContinue
        if ($cypherShell) {
            & cypher-shell -a $Neo4jUri -u neo4j -p neo4jneo4j -f $neo4jSchema
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Neo4j schema applied" -ForegroundColor Green
            }
            else {
                Write-Warning "Neo4j schema application failed. Apply manually if needed."
            }
        }
        else {
            Write-Warning "cypher-shell not found. Apply Neo4j schema manually:"
            Write-Host "  cypher-shell -a $Neo4jUri -u neo4j -p neo4jneo4j -f $neo4jSchema" -ForegroundColor Gray
        }
    }
}
else {
    Write-Host "[5/6] Skipping Neo4j setup" -ForegroundColor Gray
}
Write-Host ""

# Step 6: Verify deployment
Write-Host "[6/6] Verifying deployment..." -ForegroundColor Yellow

$verifySql = @"
SELECT 
    'Assemblies' as Component,
    COUNT(*) as Count
FROM sys.assemblies 
WHERE name = 'SqlClrFunctions'
UNION ALL
SELECT 
    'Migrations',
    COUNT(*)
FROM __EFMigrationsHistory
UNION ALL
SELECT
    'Tables',
    COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
"@

Write-Host "  Database components:" -ForegroundColor Gray
$verifySql | sqlcmd -S $SqlServer -d $Database -E -h-1 -W

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "✓ Local deployment complete!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Start the API: cd src\Hartonomous.Api && dotnet run" -ForegroundColor Gray
Write-Host "  2. Access Swagger UI: https://localhost:7001/swagger" -ForegroundColor Gray
Write-Host "  3. Run integration tests: dotnet test Hartonomous.Tests.sln" -ForegroundColor Gray
Write-Host ""
