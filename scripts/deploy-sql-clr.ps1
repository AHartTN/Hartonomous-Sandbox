#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy SQL CLR with System.Text.Json + MathNet.Numerics

.DESCRIPTION
    Deploys ALL dependencies from dependencies folder in correct order
    - System.Text.Json 8.0.5 (Microsoft first-party JSON)
    - MathNet.Numerics 5.0.0 (SIMD/AVX math)
    - ALL transitive dependencies
    - Uses sys.sp_add_trusted_assembly (no signing required)
    - PERMISSION_SET = UNSAFE (required for SIMD/AVX)
    - TRUSTWORTHY OFF for security

.PARAMETER ServerName
    SQL Server instance (default: .)

.PARAMETER DatabaseName
    Target database (default: Hartonomous)

.PARAMETER DependenciesDir
    Path to dependencies folder (default: d:\Repositories\Hartonomous\dependencies)
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$DependenciesDir = "d:\Repositories\Hartonomous\dependencies"
)

$ErrorActionPreference = "Stop"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SQL CLR DEPLOYMENT - System.Text.Json" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Server:       $ServerName"
Write-Host "Database:     $DatabaseName"
Write-Host "Dependencies: $DependenciesDir`n"

# EXACT deployment order (dependencies first)
$assemblyNames = @(
    "System.Runtime.CompilerServices.Unsafe",
    "System.Numerics.Vectors",
    "System.Buffers",
    "System.Memory",
    # SKIP System.ValueTuple - it's a forwarder assembly to mscorlib which creates binding policy conflicts
    # .NET Framework 4.7+ already has System.ValueTuple types in mscorlib
    "System.Threading.Tasks.Extensions",
    "Microsoft.Bcl.AsyncInterfaces",
    "System.Text.Encodings.Web",
    "System.Text.Json",
    "System.Xml.Linq",
    "System.Runtime.Serialization",
    "MathNet.Numerics",
    "SqlClrFunctions"
)

Write-Host "Verifying assemblies..." -ForegroundColor Cyan
$assemblies = @()
foreach ($name in $assemblyNames) {
    $path = Join-Path $DependenciesDir "$name.dll"
    if (-not (Test-Path $path)) {
        Write-Host "✗ Missing: $name.dll" -ForegroundColor Red
        throw "Required assembly not found: $path"
    }
    
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")
    
    $sha512 = [System.Security.Cryptography.SHA512]::Create()
    $hash = "0x" + [System.BitConverter]::ToString($sha512.ComputeHash($bytes)).Replace("-", "")
    
    $assemblies += @{
        Name = $name
        Path = $path
        Hex = $hex
        Hash = $hash
        Size = $bytes.Length
    }
    
    Write-Host "✓ $name.dll ($(($bytes.Length / 1KB).ToString('N1')) KB)" -ForegroundColor Green
}

# Generate SQL deployment script
$sql = @"
USE [master];
GO

-- Enable CLR integration
DECLARE @clrEnabled INT;
SELECT @clrEnabled = CAST(value AS INT) FROM sys.configurations WHERE name = 'clr enabled';
IF @clrEnabled = 0
BEGIN
    PRINT 'Enabling CLR integration...';
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
END
ELSE
    PRINT 'CLR integration already enabled';
GO

-- Enable CLR strict security (SQL 2017+ default)
DECLARE @clrStrict INT;
SELECT @clrStrict = CAST(value AS INT) FROM sys.configurations WHERE name = 'clr strict security';
IF @clrStrict = 0
BEGIN
    PRINT 'Enabling CLR strict security...';
    EXEC sp_configure 'clr strict security', 1;
    RECONFIGURE;
END
ELSE
    PRINT 'CLR strict security already enabled';
GO

USE [$DatabaseName];
GO

-- Ensure TRUSTWORTHY is OFF for security
IF (SELECT is_trustworthy_on FROM sys.databases WHERE name = '$DatabaseName') = 1
BEGIN
    PRINT 'Setting TRUSTWORTHY OFF for security...';
    ALTER DATABASE [$DatabaseName] SET TRUSTWORTHY OFF;
END
ELSE
    PRINT 'TRUSTWORTHY is OFF (secure)';
GO

-- Drop existing CLR objects and assemblies
PRINT 'Cleaning existing assemblies...';
DECLARE @dropSql NVARCHAR(MAX) = '';

-- Drop CLR functions/aggregates
SELECT @dropSql += 'DROP ' + CASE o.type WHEN 'AF' THEN 'AGGREGATE' ELSE 'FUNCTION' END + ' ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF');
IF @dropSql != '' EXEC sp_executesql @dropSql;

-- Drop assemblies in reverse order
"@

# Drop assemblies in reverse order
foreach ($name in ($assemblyNames | Sort-Object -Descending)) {
    $sql += @"

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$name')
BEGIN
    PRINT '  Dropping assembly [$name]...';
    DROP ASSEMBLY [$name];
END;
"@
}

$sql += @"

GO

PRINT '';
PRINT 'Adding assemblies to trusted list and deploying...';
GO
"@

# Deploy assemblies in forward order
foreach ($asm in $assemblies) {
    $varName = $asm.Name -replace '[.-]', '_'
    $sql += @"

-- Deploy $($asm.Name)
DECLARE @hash_$varName BINARY(64) = $($asm.Hash);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash_$varName)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash_$varName, N'$($asm.Name)';
    PRINT '  Added $($asm.Name) to trusted assembly list';
END

PRINT 'Creating assembly [$($asm.Name)]...';
CREATE ASSEMBLY [$($asm.Name)]
FROM $($asm.Hex)
WITH PERMISSION_SET = UNSAFE;
GO

"@
}

$sql += @"

PRINT '';
PRINT 'Verifying deployment...';
SELECT
    a.name AS AssemblyName,
    a.permission_set_desc AS PermissionSet,
    CASE WHEN ta.hash IS NOT NULL THEN 'Yes' ELSE 'No' END AS IsTrusted,
    (SELECT COUNT(*) FROM sys.assembly_modules am WHERE am.assembly_id = a.assembly_id) AS ObjectCount
FROM sys.assemblies a
LEFT JOIN sys.trusted_assemblies ta ON ta.description = a.name
WHERE a.name IN ($((($assemblyNames | ForEach-Object { "'$_'" }) -join ", ")))
ORDER BY a.name;
GO

PRINT '';
PRINT '========================================';
PRINT '✓ CLR deployment completed!';
PRINT '  - JSON Library: System.Text.Json 8.0.5';
PRINT '  - Math Library: MathNet.Numerics 5.0.0';
PRINT '  - Total Assemblies: $($assemblyNames.Count)';
PRINT '  - CLR strict security: ON';
PRINT '  - TRUSTWORTHY: OFF';
PRINT '  - Permission Set: UNSAFE';
PRINT '========================================';
GO
"@

# Execute deployment
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
try {
    $sql | Out-File -FilePath $tempFile -Encoding UTF8
    
    Write-Host "`nDeploying to SQL Server..." -ForegroundColor Cyan
    $output = & sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nDeployment failed:" -ForegroundColor Red
        Write-Host $output -ForegroundColor Red
        throw "sqlcmd failed with exit code $LASTEXITCODE"
    }
    
    Write-Host $output
    Write-Host "`n✓ DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
}
catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    throw
}
finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile
    }
}
