#!/usr/bin/env pwsh
# Deploy CLR assemblies with all dependencies
param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous"
)

$ErrorActionPreference = "Stop"

function Deploy-Assembly {
    param(
        [string]$Name,
        [string]$Path,
        [string]$PermissionSet
    )

    Write-Host "`n===== Deploying $Name =====" -ForegroundColor Cyan
    Write-Host "Path: $Path"

    if (-not (Test-Path $Path)) {
        throw "File not found: $Path"
    }

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")
    Write-Host "Size: $($bytes.Length) bytes"
    Write-Host "Permission: $PermissionSet"

    $sql = "USE [$DatabaseName]; IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$Name') DROP ASSEMBLY [$Name]; CREATE ASSEMBLY [$Name] FROM $hex WITH PERMISSION_SET = $PermissionSet;"

    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        $sql | Out-File -FilePath $tempFile -Encoding UTF8
        $output = & sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error output: $output" -ForegroundColor Red
            throw "Assembly deployment failed"
        }
        Write-Host "✓ $Name deployed successfully" -ForegroundColor Green
    }
    finally {
        if (Test-Path $tempFile) {
            Remove-Item $tempFile
        }
    }
}

# 1. Deploy netstandard.dll (required by bridge)
Deploy-Assembly `
    -Name "netstandard" `
    -Path "C:\Program Files\dotnet\sdk\10.0.100-rc.2.25502.107\Microsoft\Microsoft.NET.Build.Extensions\net461\lib\netstandard.dll" `
    -PermissionSet "SAFE"

# 2. Deploy Bridge assembly (SAFE - .NET Standard 2.0)
Deploy-Assembly `
    -Name "Hartonomous.Sql.Bridge" `
    -Path "D:\Repositories\Hartonomous\src\Hartonomous.Sql.Bridge\bin\Release\netstandard2.0\Hartonomous.Sql.Bridge.dll" `
    -PermissionSet "SAFE"

# 3. Deploy main assembly (UNSAFE - for GPU/file system access)
Deploy-Assembly `
    -Name "SqlClrFunctions" `
    -Path "D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll" `
    -PermissionSet "UNSAFE"

# Verify
Write-Host "`n===== Verification =====" -ForegroundColor Cyan
sqlcmd -S $ServerName -E -C -d $DatabaseName -Q "SELECT name, permission_set_desc FROM sys.assemblies WHERE name IN ('netstandard', 'Hartonomous.Sql.Bridge', 'SqlClrFunctions') ORDER BY name"

Write-Host "`n✓ All assemblies deployed successfully!" -ForegroundColor Green
