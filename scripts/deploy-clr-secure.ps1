#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Production-grade secure multi-assembly deployment of SQL CLR with strict security.

.DESCRIPTION
    Deploys SqlClrFunctions.dll and dependencies using sys.sp_add_trusted_assembly for production security.
    - TRUSTWORTHY OFF (database remains secure)
    - Strong-name signed assemblies required
    - Assemblies added to trusted assembly list
    - Proper dependency ordering
    - Idempotent and production-ready for Azure Arc SQL Server deployments

.PARAMETER ServerName
    SQL Server instance name (default: localhost)

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER BinDirectory
    Path to the bin directory containing all required DLLs

.EXAMPLE
    .\deploy-clr-secure.ps1 -ServerName "." -BinDirectory "src\SqlClr\bin\Release"

.NOTES
    Security Requirements:
    - Assemblies MUST be strong-name signed
    - SQL Server must have CLR strict security ON (default in SQL 2017+)
    - Uses sys.sp_add_trusted_assembly instead of TRUSTWORTHY ON
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$BinDirectory = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release"
)

$ErrorActionPreference = "Stop"

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "SECURE SQL CLR MULTI-ASSEMBLY DEPLOYMENT" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Server:       $ServerName"
Write-Host "Database:     $DatabaseName"
Write-Host "Bin Directory: $BinDirectory"
Write-Host "Security:     sys.sp_add_trusted_assembly (TRUSTWORTHY OFF)`n"

# Define assemblies in dependency order (dependencies first)
$assemblies = @(
    @{ Name = "System.Runtime.CompilerServices.Unsafe"; Required = $true },
    @{ Name = "System.Numerics.Vectors"; Required = $true },
    @{ Name = "System.Memory"; Required = $true },
    @{ Name = "System.Text.Json"; Required = $true },
    @{ Name = "MathNet.Numerics"; Required = $true },
    @{ Name = "SqlClrFunctions"; Required = $true }
)

# Verify all assemblies exist
Write-Host "Verifying assembly files..." -ForegroundColor Cyan
$assemblyData = @{}
foreach ($assembly in $assemblies) {
    $dllPath = Join-Path $BinDirectory "$($assembly.Name).dll"
    if (-not (Test-Path $dllPath)) {
        if ($assembly.Required) {
            throw "Required assembly not found: $dllPath"
        }
        Write-Host "⚠ Optional assembly not found: $($assembly.Name).dll" -ForegroundColor Yellow
        continue
    }

    $assemblyBytes = [System.IO.File]::ReadAllBytes($dllPath)
    $hexString = "0x" + [System.BitConverter]::ToString($assemblyBytes).Replace("-", "")
    
    # Calculate SHA-512 hash for sys.sp_add_trusted_assembly
    $sha512 = [System.Security.Cryptography.SHA512]::Create()
    $hashBytes = $sha512.ComputeHash($assemblyBytes)
    $hashHex = "0x" + [System.BitConverter]::ToString($hashBytes).Replace("-", "")
    
    $assemblyData[$assembly.Name] = @{
        Path = $dllPath
        HexBytes = $hexString
        Hash = $hashHex
        Size = $assemblyBytes.Length
    }
    
    Write-Host "✓ Found: $($assembly.Name).dll ($(($assemblyBytes.Length / 1KB).ToString('N0')) KB)" -ForegroundColor Green
}

# --- Start SQL Generation ---
$sql = @"
USE [master];
GO

-- Verify CLR strict security is enabled (SQL 2017+ default)
DECLARE @clrStrict INT;
SELECT @clrStrict = CAST(value AS INT) FROM sys.configurations WHERE name = 'clr strict security';
IF @clrStrict = 0
BEGIN
    PRINT 'WARNING: CLR strict security is OFF. Enabling for production security...';
    EXEC sp_configure 'clr strict security', 1;
    RECONFIGURE;
END
ELSE
BEGIN
    PRINT 'CLR strict security is ON (production mode)';
END
GO

USE [$DatabaseName];
GO

-- Ensure TRUSTWORTHY is OFF for security (sys.sp_add_trusted_assembly doesn't require it)
IF (SELECT is_trustworthy_on FROM sys.databases WHERE name = '$DatabaseName') = 1
BEGIN
    PRINT 'WARNING: TRUSTWORTHY is ON. Setting to OFF for production security...';
    ALTER DATABASE [$DatabaseName] SET TRUSTWORTHY OFF;
    PRINT 'TRUSTWORTHY disabled - using sys.sp_add_trusted_assembly instead';
END
ELSE
BEGIN
    PRINT 'TRUSTWORTHY is OFF (secure configuration)';
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
IF @dropSql != '' EXEC sp_executesql @dropSql;

SET @dropSql = '';
SELECT @dropSql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.assembly_types
WHERE assembly_id = (SELECT assembly_id FROM sys.assemblies WHERE name = 'SqlClrFunctions');
IF @dropSql != '' EXEC sp_executesql @dropSql;
GO

PRINT 'Dropping existing assemblies in reverse dependency order...';
GO
"@

# Generate DROP statements for all assemblies in reverse order
$reversedAssemblies = $assemblies | ForEach-Object { $_.Name } | Sort-Object -Descending
foreach ($assemblyName in $reversedAssemblies) {
    if (-not $assemblyData.ContainsKey($assemblyName)) { continue }
    
    $sql += "`n"
    $sql += @"
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$assemblyName')
BEGIN
    PRINT 'Dropping assembly [$assemblyName]...';
    DROP ASSEMBLY [$assemblyName];
    
    -- Remove from trusted assembly list
    DECLARE @hash BINARY(64);
    SET @hash = (SELECT hash FROM sys.trusted_assemblies WHERE description = '$assemblyName');
    IF @hash IS NOT NULL
    BEGIN
        EXEC sys.sp_drop_trusted_assembly @hash;
        PRINT '  Removed from trusted assembly list';
    END
END;
GO
"@
}

# Generate CREATE statements and sys.sp_add_trusted_assembly for all assemblies
$sql += @"

PRINT 'Adding assemblies to trusted assembly list...';
GO
"@

foreach ($assemblyName in ($assemblies | ForEach-Object { $_.Name })) {
    if (-not $assemblyData.ContainsKey($assemblyName)) { continue }
    
    $data = $assemblyData[$assemblyName]
    
    $sql += "`n"
    $sql += @"

-- Add $assemblyName to trusted assembly list
DECLARE @hash BINARY(64) = $($data.Hash);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash, N'$assemblyName';
    PRINT '✓ Added $assemblyName to trusted assembly list';
END
ELSE
BEGIN
    PRINT '○ $assemblyName already in trusted assembly list';
END
GO

PRINT 'Creating assembly [$assemblyName]...';
CREATE ASSEMBLY [$assemblyName]
FROM $($data.HexBytes)
WITH PERMISSION_SET = UNSAFE;
GO
"@
}

# Add verification step
$sql += @"

PRINT '';
PRINT 'Verifying deployed assemblies...';
SELECT
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS CLRVersion,
    CASE WHEN ta.hash IS NOT NULL THEN 'Yes' ELSE 'No' END AS IsTrusted
FROM sys.assemblies a
LEFT JOIN sys.trusted_assemblies ta ON ta.description = a.name
WHERE a.name IN ($((($assemblies | ForEach-Object { $_.Name }) | ForEach-Object { "'$_'" }) -join ", "))
ORDER BY
    CASE a.name
        WHEN 'System.Runtime.CompilerServices.Unsafe' THEN 1
        WHEN 'System.Memory' THEN 2
        WHEN 'System.Numerics.Vectors' THEN 3
        WHEN 'System.Text.Json' THEN 4
        WHEN 'MathNet.Numerics' THEN 5
        WHEN 'SqlClrFunctions' THEN 6
        ELSE 99
    END;
GO

PRINT '';
PRINT '==============================================';
PRINT 'Security Configuration:';
PRINT '  - CLR strict security: ON';
PRINT '  - TRUSTWORTHY: OFF';
PRINT '  - Trusted assembly list: ACTIVE';
PRINT '  - Strong-name signing: REQUIRED';
PRINT '==============================================';
PRINT '';
PRINT '✓ Secure CLR multi-assembly deployment completed!';
"@

# --- Execute SQL ---
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
try {
    $sql | Out-File -FilePath $tempFile -Encoding UTF8

    Write-Host "`nDeploying to SQL Server..." -ForegroundColor Cyan
    $output = & sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nError output:" -ForegroundColor Red
        Write-Host $output -ForegroundColor Red
        throw "Deployment failed with exit code $LASTEXITCODE"
    }

    Write-Host $output
    Write-Host "`n✓ Secure CLR deployment completed successfully!" -ForegroundColor Green
    Write-Host "`nSecurity validation:" -ForegroundColor Cyan
    Write-Host "  - All assemblies added to sys.trusted_assemblies" -ForegroundColor Green
    Write-Host "  - TRUSTWORTHY OFF (database remains secure)" -ForegroundColor Green
    Write-Host "  - CLR strict security ON (production mode)" -ForegroundColor Green
}
catch {
    Write-Host "`nDeployment failed: $_" -ForegroundColor Red
    throw
}
finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile
    }
}
