#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Enterprise deployment: SQL CLR → .NET Standard 2.0 Bridge → .NET 10 architecture

.DESCRIPTION
    Preserves the revolutionary architecture:
    - SQL CLR (.NET Framework 4.8.1) for SQL Server compatibility
    - .NET Standard 2.0 bridge for cross-platform compatibility
    - .NET 10 features for modern performance/SIMD

    Deploys in correct order:
    1. netstandard.dll facade (runtime version, not reference)
    2. Hartonomous.Sql.Bridge.dll (.NET Standard 2.0)
    3. SqlClrFunctions.dll (UNSAFE for GPU/file system)
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== SQL CLR → .NET Standard 2.0 → .NET 10 Deployment ===" -ForegroundColor Cyan

# Find the netstandard.dll FACADE (not reference assembly!)
$netstandardPath = "C:\Program Files\dotnet\sdk\10.0.100-rc.2.25502.107\Microsoft\Microsoft.NET.Build.Extensions\net461\lib\netstandard.dll"

if (-not (Test-Path $netstandardPath)) {
    throw "netstandard.dll facade not found at: $netstandardPath"
}

function Deploy-Assembly {
    param([string]$Name, [string]$Path, [string]$PermissionSet)

    Write-Host "`nDeploying: $Name ($PermissionSet)" -ForegroundColor Yellow
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")

    $sql = @"
USE [$DatabaseName];
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$Name') DROP ASSEMBLY [$Name];
CREATE ASSEMBLY [$Name] FROM $hex WITH PERMISSION_SET = $PermissionSet;
SELECT name, permission_set_desc FROM sys.assemblies WHERE name = '$Name';
"@

    $temp = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        $sql | Out-File -FilePath $temp -Encoding UTF8
        $output = & sqlcmd -S $ServerName -E -C -i $temp -b 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Failed: $output" }
        Write-Host $output -ForegroundColor Green
    } finally {
        Remove-Item $temp -ErrorAction SilentlyContinue
    }
}

# Step 1: Deploy netstandard facade
Deploy-Assembly -Name "netstandard" -Path $netstandardPath -PermissionSet "SAFE"

# Step 2: Deploy bridge
Deploy-Assembly -Name "Hartonomous.Sql.Bridge" `
    -Path "D:\Repositories\Hartonomous\src\Hartonomous.Sql.Bridge\bin\Release\netstandard2.0\Hartonomous.Sql.Bridge.dll" `
    -PermissionSet "SAFE"

# Step 3: Deploy main assembly (UNSAFE for GPU/file system)
Deploy-Assembly -Name "SqlClrFunctions" `
    -Path "D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll" `
    -PermissionSet "UNSAFE"

Write-Host "`n✓ Complete: SQL CLR → .NET Standard Bridge → .NET 10 architecture deployed!" -ForegroundColor Green
