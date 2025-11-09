#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy SQL CLR with MathNet.Numerics and Newtonsoft.Json dependencies

.DESCRIPTION
    Deploys only the assemblies that exist in bin/Release
    - Uses sys.sp_add_trusted_assembly (no signing required)
    - Deploys dependencies first, then main assembly
    - TRUSTWORTHY OFF for security

.PARAMETER ServerName
    SQL Server instance (default: .)

.PARAMETER DatabaseName
    Target database (default: Hartonomous)

.PARAMETER BinDirectory
    Path to DLLs (default: d:\Repositories\Hartonomous\src\SqlClr\bin\Release)
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$BinDirectory = "d:\Repositories\Hartonomous\dependencies"
)

$ErrorActionPreference = "Stop"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SQL CLR DEPLOYMENT - MathNet + Newtonsoft" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Server:   $ServerName"
Write-Host "Database: $DatabaseName"
Write-Host "Bin:      $BinDirectory`n"

# Assemblies in dependency order (Microsoft.SqlServer.Types is system assembly, skip it)
$assemblyNames = @(
    "System.Numerics.Vectors",
    "MathNet.Numerics",
    "Newtonsoft.Json",
    "SqlClrFunctions"
)

# System.Runtime.Serialization dependencies - all need UNSAFE
$gacAssemblies = @(
    @{
        Name = "System.ServiceModel.Internals"
        Path = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.ServiceModel.Internals.dll"
        PermissionSet = "UNSAFE"
    },
    @{
        Name = "SMDiagnostics"
        Path = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\SMDiagnostics.dll"
        PermissionSet = "UNSAFE"
    },
    @{
        Name = "System.Runtime.Serialization"
        Path = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.Serialization.dll"
        PermissionSet = "UNSAFE"
    }
)

Write-Host "Verifying assemblies..." -ForegroundColor Cyan
$assemblies = @()
foreach ($name in $assemblyNames) {
    $path = Join-Path $BinDirectory "$name.dll"
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
    
    Write-Host "✓ $name.dll ($(($bytes.Length / 1KB).ToString('N0')) KB)" -ForegroundColor Green
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

-- Drop ALL user assemblies (idempotent cleanup)
DECLARE @asmName NVARCHAR(128);
DECLARE asm_cursor CURSOR FOR
    SELECT name FROM sys.assemblies WHERE is_user_defined = 1 ORDER BY assembly_id DESC;

OPEN asm_cursor;
FETCH NEXT FROM asm_cursor INTO @asmName;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        PRINT '  Dropping assembly [' + @asmName + ']...';
        DECLARE @dropCmd NVARCHAR(200) = 'DROP ASSEMBLY [' + @asmName + ']';
        EXEC sp_executesql @dropCmd;
    END TRY
    BEGIN CATCH
        -- Skip system assemblies or dependencies (will retry in loop)
        PRINT '  Skipping [' + @asmName + '] (has dependencies or system assembly)';
    END CATCH
    
    FETCH NEXT FROM asm_cursor INTO @asmName;
END;

CLOSE asm_cursor;
DEALLOCATE asm_cursor;

-- Second pass: cleanup remaining dependencies
DECLARE asm_cursor2 CURSOR FOR
    SELECT name FROM sys.assemblies WHERE is_user_defined = 1 ORDER BY assembly_id DESC;

OPEN asm_cursor2;
FETCH NEXT FROM asm_cursor2 INTO @asmName;

WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY
        PRINT '  Dropping remaining assembly [' + @asmName + ']...';
        DECLARE @dropCmd2 NVARCHAR(200) = 'DROP ASSEMBLY [' + @asmName + ']';
        EXEC sp_executesql @dropCmd2;
    END TRY
    BEGIN CATCH
        PRINT '  WARNING: Could not drop [' + @asmName + ']: ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM asm_cursor2 INTO @asmName;
END;

CLOSE asm_cursor2;
DEALLOCATE asm_cursor2;

GO

PRINT '';
PRINT 'Adding assemblies to trusted list and deploying...';
GO
"@

# Deploy GAC assemblies first
foreach ($gacAsm in $gacAssemblies) {
    $bytes = [System.IO.File]::ReadAllBytes($gacAsm.Path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")
    $sha512 = [System.Security.Cryptography.SHA512]::Create()
    $hash = "0x" + [System.BitConverter]::ToString($sha512.ComputeHash($bytes)).Replace("-", "")
    
    $sql += @"

-- Deploy GAC assembly $($gacAsm.Name)
DECLARE @hash_$($gacAsm.Name.Replace('.','_')) BINARY(64) = $hash;
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash_$($gacAsm.Name.Replace('.','_')))
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash_$($gacAsm.Name.Replace('.','_')), N'$($gacAsm.Name)';
    PRINT '  Added $($gacAsm.Name) to trusted assembly list';
END

IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$($gacAsm.Name)')
BEGIN
    PRINT 'Creating GAC assembly [$($gacAsm.Name)] with $($gacAsm.PermissionSet)...';
    CREATE ASSEMBLY [$($gacAsm.Name)]
    FROM $hex
    WITH PERMISSION_SET = $($gacAsm.PermissionSet);
END
ELSE
    PRINT 'GAC assembly [$($gacAsm.Name)] already exists';
GO

"@
}

# Deploy assemblies in forward order
foreach ($asm in $assemblies) {
    $hashVar = $asm.Name.Replace('.','_')
    
    # Determine permission set - assemblies with static fields or unsafe code need UNSAFE
    # Per MS docs: "SQL Server disallows the use of static variables and static data members" in SAFE assemblies
    $permissionSet = if ($asm.Name -in @('System.Numerics.Vectors', 'MathNet.Numerics', 'Newtonsoft.Json', 'SqlClrFunctions')) { 'UNSAFE' } else { 'SAFE' }
    
    $sql += @"

-- Deploy $($asm.Name) with $permissionSet
DECLARE @hash_$hashVar BINARY(64) = $($asm.Hash);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash_$hashVar)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash_$hashVar, N'$($asm.Name)';
    PRINT '  Added $($asm.Name) to trusted assembly list';
END

PRINT 'Creating assembly [$($asm.Name)] with $permissionSet...';
CREATE ASSEMBLY [$($asm.Name)]
FROM $($asm.Hex)
WITH PERMISSION_SET = $permissionSet;
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
ORDER BY
    CASE a.name
        WHEN 'System.Numerics.Vectors' THEN 1
        WHEN 'MathNet.Numerics' THEN 2
        WHEN 'Newtonsoft.Json' THEN 3
        WHEN 'SqlClrFunctions' THEN 4
    END;
GO

PRINT '';
PRINT '========================================';
PRINT '✓ CLR deployment completed!';
PRINT '  - CLR strict security: ON';
PRINT '  - TRUSTWORTHY: OFF';
PRINT '  - Trusted assemblies: sys.sp_add_trusted_assembly';
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
    Write-Host "`n✓ Deployment successful!" -ForegroundColor Green
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
