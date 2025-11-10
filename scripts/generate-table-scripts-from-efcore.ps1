<#
.SYNOPSIS
Generate CREATE TABLE SQL scripts from EF Core DbContext

.DESCRIPTION
Uses Entity Framework Core migrations to scaffold the database and extract
CREATE TABLE scripts into individual .sql files for the SQL Database Project.

.PARAMETER OutputPath
Path to output the generated SQL files. Defaults to src/Hartonomous.Database/Tables/Generated

.PARAMETER ConnectionString
SQL Server connection string. Defaults to LocalDB.
#>

param(
    [string]$OutputPath = "src/Hartonomous.Database/Tables/Generated",
    [string]$ConnectionString = "Server=(localdb)\mssqllocaldb;Database=Hartonomous_SchemaGen;Trusted_Connection=True;TrustServerCertificate=True;"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Generating Table Scripts from EF Core ===" -ForegroundColor Cyan

# Create output directory
$OutputDir = Join-Path $PSScriptRoot ".." $OutputPath
if (Test-Path $OutputDir) {
    Write-Host "Cleaning existing output directory: $OutputDir" -ForegroundColor Yellow
    Remove-Item "$OutputDir\*.sql" -Force
} else {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Project paths
$DataProject = Join-Path $PSScriptRoot ".." "src" "Hartonomous.Data" "Hartonomous.Data.csproj"

Write-Host "Data Project: $DataProject" -ForegroundColor Gray
Write-Host "Output Path: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Generate migration script using EF Core
Write-Host "Generating SQL script from EF Core migrations..." -ForegroundColor Yellow

try {
    # Use EF Core to generate the full schema script
    $ScriptPath = Join-Path (Split-Path $DataProject -Parent) "..\..\temp-full-schema.sql"
    
    dotnet ef migrations script --project $DataProject --no-build --output $ScriptPath --context HartonomousDbContext
    
    if (-not (Test-Path $ScriptPath)) {
        throw "Failed to generate migration script"
    }
    
    Write-Host "Generated migration script: $ScriptPath" -ForegroundColor Green
    
    # Parse the script and extract CREATE TABLE statements
    Write-Host "Parsing CREATE TABLE statements..." -ForegroundColor Yellow
    
    $content = Get-Content $ScriptPath -Raw
    
    # Regex to match CREATE TABLE statements with optional schema prefix
    # Matches both [schema].[table] and [table] patterns
    $tablePattern = '(?s)CREATE\s+TABLE\s+(?:\[([^\]]+)\]\.)?\[([^\]]+)\]\s*\((.*?)\);'
    
    $tables = [regex]::Matches($content, $tablePattern)
    
    Write-Host "Found $($tables.Count) CREATE TABLE statements" -ForegroundColor Green
    
    foreach ($match in $tables) {
        $schema = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { "dbo" }
        $tableName = $match.Groups[2].Value
        $tableDefinition = $match.Value
        
        # Create schema directory if needed
        $schemaDir = Join-Path $OutputDir $schema
        if (-not (Test-Path $schemaDir)) {
            New-Item -ItemType Directory -Path $schemaDir -Force | Out-Null
        }
        
        # Write individual table script
        $fileName = "$schema.$tableName.sql"
        $filePath = Join-Path $schemaDir "$tableName.sql"
        
        # Format the script with proper indentation
        $formattedScript = @"
-- Table: [$schema].[$tableName]
-- Generated from EF Core DbContext
-- Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

$tableDefinition
GO
"@
        
        Set-Content -Path $filePath -Value $formattedScript -Encoding UTF8
        Write-Host "  Created: $fileName" -ForegroundColor Gray
    }
    
    # Extract CREATE INDEX statements
    $indexPattern = '(?s)CREATE\s+(?:UNIQUE\s+)?(?:NONCLUSTERED\s+)?INDEX\s+\[([^\]]+)\]\s+ON\s+\[([^\]]+)\]\.\[([^\]]+)\][^;]+;'
    $indexes = [regex]::Matches($content, $indexPattern)
    
    if ($indexes.Count -gt 0) {
        Write-Host "`nFound $($indexes.Count) CREATE INDEX statements" -ForegroundColor Green
        
        $indexDir = Join-Path $OutputDir "Indexes"
        New-Item -ItemType Directory -Path $indexDir -Force | Out-Null
        
        foreach ($match in $indexes) {
            $indexName = $match.Groups[1].Value
            $schema = $match.Groups[2].Value
            $tableName = $match.Groups[3].Value
            $indexDefinition = $match.Value
            
            $fileName = "$schema.$tableName.$indexName.sql"
            $filePath = Join-Path $indexDir $fileName
            
            $formattedScript = @"
-- Index: [$indexName] on [$schema].[$tableName]
-- Generated from EF Core DbContext

$indexDefinition
GO
"@
            
            Set-Content -Path $filePath -Value $formattedScript -Encoding UTF8
            Write-Host "  Created: Indexes\$fileName" -ForegroundColor Gray
        }
    }
    
    # Cleanup
    Remove-Item $ScriptPath -Force
    
    Write-Host "`n=== Table Generation Complete ===" -ForegroundColor Green
    Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan
    Write-Host "Total tables: $($tables.Count)" -ForegroundColor Cyan
    Write-Host "Total indexes: $($indexes.Count)" -ForegroundColor Cyan
    
} catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
