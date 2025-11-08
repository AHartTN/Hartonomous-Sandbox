#!/usr/bin/env pwsh
<#!
.SYNOPSIS
    Deploys the SQL CLR assembly directly against .NET Framework 4.8.1 output.

.DESCRIPTION
    Replaces the retired cross-runtime experiment. Drops any leftover compatibility assemblies
    and publishes only the binaries produced by the SqlClr project along with their required
    NuGet dependencies.
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$BinDirectory = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== SQL CLR Direct Deployment (4.8.1) ===" -ForegroundColor Cyan
Write-Host "Server:    $ServerName" -ForegroundColor Cyan
Write-Host "Database:  $DatabaseName" -ForegroundColor Cyan
Write-Host "Artifacts: $BinDirectory`n" -ForegroundColor Cyan

function Invoke-SqlBatch {
    param([string]$Sql)

    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        $Sql | Out-File -FilePath $tempFile -Encoding UTF8
        $output = & sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd failed: $output"
        }
        if ($output) { Write-Host $output }
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function Reset-ClrObjects {
    $sql = @"
USE [$DatabaseName];

DECLARE @dropSql NVARCHAR(MAX) = N'';

SELECT @dropSql += 'DROP ' + CASE o.type WHEN 'AF' THEN 'AGGREGATE' ELSE 'FUNCTION' END + ' '
    + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
WHERE o.type IN ('AF', 'FN', 'FS', 'FT', 'IF', 'TF');

IF (@dropSql <> N'')
BEGIN
    PRINT 'Dropping CLR-backed functions and aggregates...';
    EXEC sp_executesql @dropSql;
END

SET @dropSql = N'';
SELECT @dropSql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.types
WHERE is_assembly_type = 1;

IF (@dropSql <> N'')
BEGIN
    PRINT 'Dropping CLR-backed types...';
    EXEC sp_executesql @dropSql;
END
"@

    Invoke-SqlBatch -Sql $sql
}

function Remove-UserClrAssemblies {
    $sql = @"
USE [$DatabaseName];

DECLARE @dropSql NVARCHAR(MAX) = N'';

SELECT @dropSql += 'DROP ASSEMBLY [' + a.name + '];' + CHAR(13)
FROM sys.assemblies a
WHERE a.is_user_defined = 1
ORDER BY a.create_date DESC;

IF (@dropSql <> N'')
BEGIN
    PRINT 'Dropping user-defined CLR assemblies...';
    EXEC sp_executesql @dropSql;
END
"@

    Invoke-SqlBatch -Sql $sql
}

function Deploy-Assembly {
    param(
        [string]$Name,
        [string]$Path,
        [string]$PermissionSet
    )

    if (-not (Test-Path $Path)) {
        throw "Assembly not found at path: $Path"
    }

    Write-Host "Deploying: $Name ($PermissionSet)" -ForegroundColor Yellow
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")

    $sql = @"
USE [$DatabaseName];
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$Name')
BEGIN
    DROP ASSEMBLY [$Name];
END;
CREATE ASSEMBLY [$Name]
FROM $hex
WITH PERMISSION_SET = $PermissionSet;
PRINT '  ✓ Created assembly [$Name]';
"@

    Invoke-SqlBatch -Sql $sql
}

function Resolve-AssemblyPath {
    param([string]$AssemblyName)

    $candidate = Join-Path $BinDirectory "$AssemblyName.dll"
    if (Test-Path $candidate) {
        return $candidate
    }

    $packageRoot = Join-Path $env:USERPROFILE ".nuget\packages"
    if (-not (Test-Path $packageRoot)) {
        throw "NuGet package cache not found at $packageRoot. Restore packages and rebuild SqlClr."
    }

    $packageNameMap = @{
        "Microsoft.SqlServer.Types"               = "microsoft.sqlserver.types"
        "System.Runtime.CompilerServices.Unsafe"  = "system.runtime.compilerservices.unsafe"
        "System.Memory"                           = "system.memory"
        "System.Numerics.Vectors"                 = "system.numerics.vectors"
        "System.Text.Json"                        = "system.text.json"
        "MathNet.Numerics"                        = "mathnet.numerics"
    }

    $packageName = $packageNameMap[$AssemblyName]
    if (-not $packageName) {
        if ($AssemblyName -eq "SqlClrFunctions") {
            throw "SqlClrFunctions.dll must exist in $BinDirectory. Build the project first."
        }
        throw "Unknown package mapping for assembly $AssemblyName."
    }

    $searchRoot = Join-Path $packageRoot $packageName
    if (-not (Test-Path $searchRoot)) {
        throw "Package folder not found for $AssemblyName under $searchRoot."
    }

    $dll = Get-ChildItem -Path $searchRoot -Recurse -Filter "$AssemblyName.dll" -File |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if (-not $dll) {
        throw "Unable to resolve a path for $AssemblyName.dll within $searchRoot."
    }

    return $dll.FullName
}

Write-Host "Resetting existing CLR surface area..." -ForegroundColor Cyan
Reset-ClrObjects
Remove-UserClrAssemblies

$assemblyPlan = @(
    @{ Name = "Microsoft.SqlServer.Types";               Permission = "SAFE" },
    @{ Name = "System.Runtime.CompilerServices.Unsafe"; Permission = "SAFE" },
    @{ Name = "System.Memory";                          Permission = "SAFE" },
    @{ Name = "System.Numerics.Vectors";                Permission = "SAFE" },
    @{ Name = "System.Text.Json";                       Permission = "SAFE" },
    @{ Name = "MathNet.Numerics";                       Permission = "SAFE" },
    @{ Name = "SqlClrFunctions";                        Permission = "UNSAFE" }
)

foreach ($plan in $assemblyPlan) {
    $path = if ($plan.Name -eq "SqlClrFunctions") {
        Join-Path $BinDirectory "SqlClrFunctions.dll"
    }
    else {
        Resolve-AssemblyPath -AssemblyName $plan.Name
    }

    Deploy-Assembly -Name $plan.Name -Path $path -PermissionSet $plan.Permission
}

Write-Host "`nVerifying assembly state..." -ForegroundColor Cyan
$verifySql = @"
SELECT name, permission_set_desc
FROM sys.assemblies
WHERE name IN ($(( $assemblyPlan | ForEach-Object { "'" + $_.Name + "'" } ) -join ", "))
ORDER BY name;
"@
Invoke-SqlBatch -Sql $verifySql

Write-Host "`n✓ SQL CLR deployment completed without the legacy compatibility layer." -ForegroundColor Green
