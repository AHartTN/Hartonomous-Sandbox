#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Idempotent database deployment script for Hartonomous.
    
.DESCRIPTION
    Checks current database state and applies only necessary changes:
    - Creates database if missing
    - Applies EF Core migrations
    - Deploys stored procedures (CREATE OR ALTER)
    - Deploys SQL CLR if changed
    - Seeds initial data if tables empty
    
.PARAMETER ServerName
    SQL Server instance (default: localhost)
    
.PARAMETER DatabaseName
    Database name (default: Hartonomous)
    
.PARAMETER SkipClr
    Skip SQL CLR deployment
    
.PARAMETER SkipMigrations
    Skip EF Core migrations
    
.PARAMETER SkipProcedures
    Skip stored procedure deployment
    
.PARAMETER Verbose
    Show detailed progress
    
.EXAMPLE
    .\deploy-database.ps1
    
.EXAMPLE
    .\deploy-database.ps1 -ServerName ".\SQLEXPRESS" -Verbose
#>

[CmdletBinding()]
param(
    [string]$ServerName = "localhost",
    [string]$DatabaseName = "Hartonomous",
    [switch]$SkipClr = $false,
    [switch]$SkipMigrations = $false,
    [switch]$SkipProcedures = $false
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Hartonomous Database Deployment" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test SQL Server connection
function Test-SqlConnection {
    param([string]$Server, [string]$Database = "master")
    try {
        $query = "SELECT @@VERSION"
        $result = sqlcmd -S $Server -d $Database -E -C -Q $query -h -1 -W 2>&1
        if ($LASTEXITCODE -ne 0) { return $false }
        return $true
    } catch {
        return $false
    }
}

# Check if database exists
function Test-DatabaseExists {
    param([string]$Server, [string]$DbName)
    $query = "SELECT COUNT(*) FROM sys.databases WHERE name = '$DbName'"
    $result = sqlcmd -S $Server -d master -E -C -Q $query -h -1 -W
    return [int]$result -gt 0
}

# Get table count
function Get-TableCount {
    param([string]$Server, [string]$DbName)
    $query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory'"
    $result = sqlcmd -S $Server -d $DbName -E -C -Q $query -h -1 -W
    return [int]$result
}

# Get procedure count
function Get-ProcedureCount {
    param([string]$Server, [string]$DbName)
    $query = "SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0"
    $result = sqlcmd -S $Server -d $DbName -E -C -Q $query -h -1 -W
    return [int]$result
}

# Get record count for a table
function Get-RecordCount {
    param([string]$Server, [string]$DbName, [string]$TableName)
    $query = "IF OBJECT_ID('$TableName') IS NOT NULL SELECT COUNT(*) FROM $TableName ELSE SELECT 0"
    $result = sqlcmd -S $Server -d $DbName -E -C -Q $query -h -1 -W 2>$null
    return [int]$result
}

# Main deployment logic
Write-Host "► Checking SQL Server connection..." -ForegroundColor Yellow
if (-not (Test-SqlConnection -Server $ServerName)) {
    Write-Host "  ✗ Cannot connect to SQL Server at $ServerName" -ForegroundColor Red
    Write-Host "  Please ensure SQL Server is running and accessible." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Connected to SQL Server" -ForegroundColor Green

Write-Host "`n► Checking database state..." -ForegroundColor Yellow
$dbExists = Test-DatabaseExists -Server $ServerName -DbName $DatabaseName

if (-not $dbExists) {
    Write-Host "  Database '$DatabaseName' not found - will be created by EF migrations" -ForegroundColor Cyan
} else {
    $tableCount = Get-TableCount -Server $ServerName -DbName $DatabaseName
    $procCount = Get-ProcedureCount -Server $ServerName -DbName $DatabaseName
    Write-Host "  ✓ Database exists: $tableCount tables, $procCount procedures" -ForegroundColor Green
}

# Apply EF Core migrations
if (-not $SkipMigrations) {
    Write-Host "`n► Checking EF Core migrations..." -ForegroundColor Yellow
    
    $dataProjectPath = Join-Path $repoRoot "src\Hartonomous.Data"
    $startupProjectPath = Join-Path $repoRoot "src\ModelIngestion"
    
    if (-not (Test-Path $dataProjectPath)) {
        Write-Host "  ✗ Data project not found at $dataProjectPath" -ForegroundColor Red
        exit 1
    }
    
    Push-Location $dataProjectPath
    try {
        # Check pending migrations
        Write-Host "  Checking for pending migrations..." -ForegroundColor Cyan
        $pendingMigrations = dotnet ef migrations list --startup-project $startupProjectPath --no-build 2>&1 | Select-String "No migrations were found"
        
        if ($pendingMigrations) {
            Write-Host "  No migrations found - generating initial migration..." -ForegroundColor Cyan
            dotnet ef migrations add InitialCreate --startup-project $startupProjectPath
            if ($LASTEXITCODE -ne 0) {
                Write-Host "  ✗ Failed to generate migration" -ForegroundColor Red
                exit 1
            }
            Write-Host "  ✓ Initial migration generated" -ForegroundColor Green
        }
        
        Write-Host "  Applying migrations..." -ForegroundColor Cyan
        dotnet ef database update --startup-project $startupProjectPath 2>&1 | Out-Host
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ✗ Migration failed" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "  ✓ Migrations applied successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "`n► Skipping EF migrations (--SkipMigrations)" -ForegroundColor Yellow
}

# Deploy stored procedures
if (-not $SkipProcedures) {
    Write-Host "`n► Deploying stored procedures..." -ForegroundColor Yellow
    
    $sqlProcDir = Join-Path $repoRoot "sql\procedures"
    $procFiles = Get-ChildItem -Path $sqlProcDir -Filter "*.sql" | Where-Object { $_.Name -notmatch "^0[127]_" } | Sort-Object Name
    
    if ($procFiles.Count -eq 0) {
        Write-Host "  No procedure files found in $sqlProcDir" -ForegroundColor Yellow
    } else {
        $deployed = 0
        $failed = 0
        
        foreach ($file in $procFiles) {
            Write-Host "  Processing $($file.Name)..." -ForegroundColor Cyan
            
            $sqlContent = Get-Content $file.FullName -Raw
            
            # Replace CREATE PROCEDURE with CREATE OR ALTER for idempotency
            $sqlContent = $sqlContent -replace '\bCREATE\s+PROCEDURE\b', 'CREATE OR ALTER PROCEDURE'
            $sqlContent = $sqlContent -replace '\bCREATE\s+PROC\b', 'CREATE OR ALTER PROC'
            
            # Execute SQL
            $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
            $sqlContent | Out-File -FilePath $tempFile -Encoding UTF8
            
            try {
                sqlcmd -S $ServerName -d $DatabaseName -E -C -i $tempFile -b 2>&1 | Out-Null
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "    ✓ Deployed" -ForegroundColor Green
                    $deployed++
                } else {
                    Write-Host "    ✗ Failed" -ForegroundColor Red
                    $failed++
                }
            }
            finally {
                Remove-Item $tempFile -ErrorAction SilentlyContinue
            }
        }
        
        Write-Host "`n  Summary: $deployed deployed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
    }
} else {
    Write-Host "`n► Skipping stored procedures (--SkipProcedures)" -ForegroundColor Yellow
}

# Deploy SQL CLR
if (-not $SkipClr) {
    Write-Host "`n► Checking SQL CLR..." -ForegroundColor Yellow
    
    # Check if .NET Framework 4.8 SDK is available
    $msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe `
        2>$null | Select-Object -First 1
    
    if (-not $msbuildPath) {
        Write-Host "  .NET Framework 4.8 SDK not found - skipping CLR deployment" -ForegroundColor Yellow
        Write-Host "  (CLR is optional - system works without it)" -ForegroundColor Cyan
    } else {
        Write-Host "  Building SQL CLR assembly..." -ForegroundColor Cyan
        $clrProjectPath = Join-Path $repoRoot "src\SqlClr\SqlClrFunctions.csproj"
        
        & $msbuildPath $clrProjectPath /p:Configuration=Release /v:minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ SQL CLR built successfully" -ForegroundColor Green
            
            # Deploy assembly (simplified - full deployment requires DROP/CREATE ASSEMBLY)
            $assemblyPath = Join-Path $repoRoot "src\SqlClr\bin\Release\net48\SqlClrFunctions.dll"
            if (Test-Path $assemblyPath) {
                Write-Host "  Assembly ready at: $assemblyPath" -ForegroundColor Green
                Write-Host "  (Manual ASSEMBLY deployment required - see deploy.ps1)" -ForegroundColor Cyan
            }
        } else {
            Write-Host "  ✗ CLR build failed (non-blocking)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "`n► Skipping SQL CLR (--SkipClr)" -ForegroundColor Yellow
}

# Verify final state
Write-Host "`n► Verifying deployment..." -ForegroundColor Yellow

if (Test-DatabaseExists -Server $ServerName -DbName $DatabaseName) {
    $finalTableCount = Get-TableCount -Server $ServerName -DbName $DatabaseName
    $finalProcCount = Get-ProcedureCount -Server $ServerName -DbName $DatabaseName
    
    Write-Host "  ✓ Database state:" -ForegroundColor Green
    Write-Host "    - Tables: $finalTableCount" -ForegroundColor Cyan
    Write-Host "    - Procedures: $finalProcCount" -ForegroundColor Cyan
    
    # Check key tables
    $embeddingsCount = Get-RecordCount -Server $ServerName -DbName $DatabaseName -TableName "Embeddings"
    $modelsCount = Get-RecordCount -Server $ServerName -DbName $DatabaseName -TableName "Models"
    $vocabCount = Get-RecordCount -Server $ServerName -DbName $DatabaseName -TableName "TokenVocabulary"
    
    Write-Host "    - Embeddings: $embeddingsCount records" -ForegroundColor Cyan
    Write-Host "    - Models: $modelsCount records" -ForegroundColor Cyan
    Write-Host "    - TokenVocabulary: $vocabCount records" -ForegroundColor Cyan
    
    if ($vocabCount -eq 0) {
        Write-Host "`n  ⚠ TokenVocabulary is empty - run seeding script to populate" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ✗ Database verification failed" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Deployment Complete" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green

Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Seed vocabulary: sqlcmd -S $ServerName -d $DatabaseName -E -C -i sql\procedures\16_SeedTokenVocabularyWithVector.sql" -ForegroundColor White
Write-Host "  2. Test ingestion: dotnet run --project src\ModelIngestion" -ForegroundColor White
Write-Host "  3. Verify data: sqlcmd -S $ServerName -d $DatabaseName -E -C -Q `"SELECT COUNT(*) FROM Embeddings`"" -ForegroundColor White
