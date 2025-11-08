#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enterprise-grade multi-assembly deployment of SQL CLR as UNSAFE.

.DESCRIPTION
    Deploys the SqlClrFunctions.dll and all its dependencies to SQL Server as individual UNSAFE assemblies.
    This script replaces the merged-assembly approach, providing a cleaner and more maintainable deployment.
    It is idempotent and production-ready for Azure Arc SQL Server deployments.

.PARAMETER ServerName
    SQL Server instance name (default: localhost)

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER BinDirectory
    Path to the bin directory containing all required DLLs (e.g., src\SqlClr\bin\Release)

.EXAMPLE
    .\deploy-clr-multi-assembly.ps1 -ServerName "." -BinDirectory "src\SqlClr\bin\Release"
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$BinDirectory = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release"
)

$ErrorActionPreference = "Stop"

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "SQL CLR MULTI-ASSEMBLY UNSAFE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Server:       $ServerName"
Write-Host "Database:     $DatabaseName"
Write-Host "Bin Directory: $BinDirectory`n"

# Define assemblies in dependency order (dependencies first)
$assemblies = @(
    "System.Runtime.CompilerServices.Unsafe",
    "System.Memory",
    "System.Numerics.Vectors",
    "System.Text.Json",
    "MathNet.Numerics",
    "SqlClrFunctions"
)

# --- Start SQL Generation ---
$sql = @"
USE [$DatabaseName];
GO

-- Trustworthy must be ON for UNSAFE assemblies
IF (SELECT is_trustworthy_on FROM sys.databases WHERE name = '$DatabaseName') = 0
BEGIN
    PRINT 'Enabling TRUSTWORTHY setting for database [$DatabaseName]...';
    ALTER DATABASE [$DatabaseName] SET TRUSTWORTHY ON;
END
GO

-- Drop dependent CLR objects from the main assembly
PRINT 'Dropping existing CLR functions, aggregates, and types...';
DECLARE @dropSql NVARCHAR(MAX) = '';
SELECT @dropSql += 'DROP ' + CASE o.type WHEN 'AF' THEN 'AGGREGATE' ELSE 'FUNCTION' END + ' ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF');
EXEC sp_executesql @dropSql;

SET @dropSql = '';
SELECT @dropSql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.types
WHERE is_assembly_type = 1 AND assembly_id = (SELECT assembly_id FROM sys.assemblies WHERE name = 'SqlClrFunctions');
EXEC sp_executesql @dropSql;
GO
"@

# Generate DROP statements for all assemblies in reverse order
$sql += "
PRINT 'Dropping existing assemblies in reverse dependency order...';
GO
"
$reversedAssemblies = $assemblies | Sort-Object -Descending
foreach ($assemblyName in $reversedAssemblies) {
    $sql += @"
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$assemblyName')
BEGIN
    PRINT 'Dropping assembly [$assemblyName]...';
    DROP ASSEMBLY [$assemblyName];
END;
GO
"@
}

# Generate CREATE statements for all assemblies in forward order
$sql += "
PRINT 'Creating new assemblies in dependency order...';
GO
"
foreach ($assemblyName in $assemblies) {
    $dllPath = Join-Path $BinDirectory "$assemblyName.dll"
    if (-not (Test-Path $dllPath)) {
        throw "Assembly not found: $dllPath"
    }
    Write-Host "✓ Found dependency: $assemblyName.dll"

    $assemblyBytes = [System.IO.File]::ReadAllBytes($dllPath)
    $hexString = "0x" + [System.BitConverter]::ToString($assemblyBytes).Replace("-", "")

    $sql += @"
PRINT 'Creating assembly [$assemblyName]...';
CREATE ASSEMBLY [$assemblyName]
FROM $hexString
WITH PERMISSION_SET = UNSAFE;
GO
"@
}

# Add verification step
$sql += @"
PRINT 'Verifying deployed assemblies...';
SELECT
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS CLRVersion
FROM sys.assemblies a
WHERE a.name IN ('$(($assemblies | ForEach-Object { "'$_'" }) -join ", ")');
GO

PRINT 'Multi-assembly deployment script finished.';
"@

# --- Execute SQL ---
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
try {
    $sql | Out-File -FilePath $tempFile -Encoding UTF8

    Write-Host "`nDeploying to SQL Server..." -ForegroundColor Cyan
    $output = & sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error output:" -ForegroundColor Red
        Write-Host $output -ForegroundColor Red
        throw "Deployment failed with exit code $LASTEXITCODE"
    }

    Write-Host $output
    Write-Host "`n✓ CLR multi-assembly deployment completed successfully!" -ForegroundColor Green
}
finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile
    }
}
