#!/usr/bin/env pwsh
# Deploy CLR assemblies (bridge + main) as UNSAFE - FIXED VERSION
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

    Write-Host "===== Deploying $Name =====" -ForegroundColor Cyan
    Write-Host "Path: $Path"
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")
    Write-Host "Size: $($bytes.Length) bytes"
    Write-Host "Permission: $PermissionSet"

    $sql = "USE [$DatabaseName]; IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$Name') DROP ASSEMBLY [$Name]; CREATE ASSEMBLY [$Name] FROM $hex WITH PERMISSION_SET = $PermissionSet;"

    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        $sql | Out-File -FilePath $tempFile -Encoding UTF8
        $output = sqlcmd -S $ServerName -E -C -i $tempFile -b 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "SQL command failed: $output"
        }
        Write-Host "✓ $Name deployed successfully" -ForegroundColor Green
    }
    finally {
        if (Test-Path $tempFile) {
            Remove-Item $tempFile
        }
    }
}

# Deploy Bridge assembly (SAFE - .NET Standard)
Deploy-Assembly `
    -Name "Hartonomous.Sql.Bridge" `
    -Path "D:\Repositories\Hartonomous\src\Hartonomous.Sql.Bridge\bin\Release\netstandard2.0\Hartonomous.Sql.Bridge.dll" `
    -PermissionSet "SAFE"

# Deploy main assembly (UNSAFE - for GPU/file system access)
Deploy-Assembly `
    -Name "SqlClrFunctions" `
    -Path "D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll" `
    -PermissionSet "UNSAFE"

# Verify
Write-Host "`n===== Verification =====" -ForegroundColor Cyan
sqlcmd -S $ServerName -E -C -d $DatabaseName -Q "SELECT name, permission_set_desc FROM sys.assemblies WHERE name IN ('Hartonomous.Sql.Bridge', 'SqlClrFunctions') ORDER BY name"

Write-Host "`n✓ Assembly deployment complete!" -ForegroundColor Green
