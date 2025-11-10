<#
.SYNOPSIS
Extract CREATE TABLE scripts from EF Core migration C# code

.DESCRIPTION
Parses the migration Up() method to extract table definitions and convert them to SQL
#>

param(
    [string]$MigrationFile = "src/Hartonomous.Data/Migrations/20251110135023_FullSchema.cs",
    [string]$OutputPath = "src/Hartonomous.Database/Tables/Generated"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Extracting Tables from Migration ===" -ForegroundColor Cyan

# Read migration file
$content = Get-Content (Join-Path $PSScriptRoot ".." $MigrationFile) -Raw

# Extract all CreateTable calls
$pattern = 'migrationBuilder\.CreateTable\s*\(\s*name:\s*"([^"]+)",\s*(?:schema:\s*"([^"]+)",\s*)?columns:.*?constraints:'
$matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)

Write-Host "Found $($matches.Count) CreateTable statements" -ForegroundColor Green

# Create output directory
$OutputDir = Join-Path $PSScriptRoot ".." $OutputPath
if (Test-Path $OutputDir) {
    Write-Host "Cleaning existing output directory" -ForegroundColor Yellow
    Remove-Item "$OutputDir\*" -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Track tables
$tables = @()

foreach ($match in $matches) {
    $tableName = $match.Groups[1].Value
    $schema = if ($match.Groups[2].Success) { $match.Groups[2].Value } else { "dbo" }
    
    $tables += [PSCustomObject]@{
        Schema = $schema
        Table = $tableName
    }
    
    Write-Host "  - [$schema].[$tableName]" -ForegroundColor Gray
}

# Export table list
$tableList = $tables | Sort-Object Schema, Table
Write-Host "`n=== Table Summary ===" -ForegroundColor Cyan
Write-Host "Total tables: $($tableList.Count)" -ForegroundColor Green
Write-Host "`nBy schema:" -ForegroundColor Yellow
$tableList | Group-Object Schema | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count) tables" -ForegroundColor Gray
}

# Export to JSON for further processing
$jsonPath = Join-Path $OutputDir "table-list.json"
$tableList | ConvertTo-Json | Set-Content $jsonPath
Write-Host "`nTable list saved to: $jsonPath" -ForegroundColor Cyan
