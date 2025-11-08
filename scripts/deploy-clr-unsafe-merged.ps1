#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enterprise-grade deployment of merged SQL CLR assembly as UNSAFE.

.DESCRIPTION
    Deploys the IL-merged SqlClrFunctions.dll (with all dependencies bundled) to SQL Server as UNSAFE.
    Idempotent and production-ready for Azure Arc SQL Server deployments.

.PARAMETER ServerName
    SQL Server instance name (default: localhost)

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER AssemblyPath
    Path to merged SqlClrFunctions.dll

.EXAMPLE
    .\deploy-clr-unsafe-merged.ps1 -ServerName "." -AssemblyPath "src\SqlClr\bin\Release\SqlClrFunctions.dll"
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$AssemblyPath = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll"
)

$ErrorActionPreference = "Stop"

Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "SQL CLR UNSAFE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Server:   $ServerName"
Write-Host "Database: $DatabaseName"
Write-Host "Assembly: $AssemblyPath`n"

# Verify assembly exists
if (-not (Test-Path $AssemblyPath)) {
    throw "Assembly not found: $AssemblyPath"
}

$assemblySize = (Get-Item $AssemblyPath).Length
Write-Host "✓ Assembly found: $([math]::Round($assemblySize / 1KB, 2)) KB`n"

# Read assembly bytes
Write-Host "Reading assembly bytes..." -NoNewline
$assemblyBytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
$hexString = "0x" + [System.BitConverter]::ToString($assemblyBytes).Replace("-", "")
Write-Host " Done ($($assemblyBytes.Length) bytes)`n"

# Create deployment SQL
$sql = @"
USE [$DatabaseName];
GO

-- Drop dependent CLR objects in reverse dependency order
PRINT 'Dropping existing CLR functions...';
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql += 'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type IN ('FN', 'FS', 'FT', 'IF', 'TF');
EXEC sp_executesql @sql;

PRINT 'Dropping existing CLR aggregates...';
SET @sql = '';
SELECT @sql += 'DROP AGGREGATE ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type = 'AF';
EXEC sp_executesql @sql;

PRINT 'Dropping existing CLR types...';
SET @sql = '';
SELECT @sql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.types
WHERE is_assembly_type = 1
  AND schema_id != SCHEMA_ID('sys');  -- Don't drop system types
EXEC sp_executesql @sql;

-- Drop existing assembly
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT 'Dropping existing SqlClrFunctions assembly...';
    DROP ASSEMBLY [SqlClrFunctions];
END;
GO

-- Create merged assembly with UNSAFE permission (for GPU/file system access)
PRINT 'Creating SqlClrFunctions assembly as UNSAFE...';
CREATE ASSEMBLY [SqlClrFunctions]
FROM $hexString
WITH PERMISSION_SET = UNSAFE;
GO

-- Verify deployment
SELECT
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS CLRVersion,
    ASSEMBLYPROPERTY(a.name, 'Culture') AS Culture,
    CAST(ASSEMBLYPROPERTY(a.name, 'VersionMajor') AS VARCHAR) + '.' +
    CAST(ASSEMBLYPROPERTY(a.name, 'VersionMinor') AS VARCHAR) + '.' +
    CAST(ASSEMBLYPROPERTY(a.name, 'VersionBuild') AS VARCHAR) + '.' +
    CAST(ASSEMBLYPROPERTY(a.name, 'VersionRevision') AS VARCHAR) AS Version
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions';
GO

PRINT 'SqlClrFunctions assembly deployed successfully!';
"@

# Write to temp file
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
try {
    $sql | Out-File -FilePath $tempFile -Encoding UTF8

    Write-Host "Deploying to SQL Server..." -ForegroundColor Cyan
    $output = & sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error output:" -ForegroundColor Red
        Write-Host $output -ForegroundColor Red
        throw "Deployment failed with exit code $LASTEXITCODE"
    }

    Write-Host $output
    Write-Host "`n✓ CLR assembly deployed successfully as UNSAFE!" -ForegroundColor Green
}
finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile
    }
}
