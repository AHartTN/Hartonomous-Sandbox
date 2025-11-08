#!/usr/bin/env pwsh
# Deploy both CLR assemblies (bridge + main) as UNSAFE
param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous"
)

$ErrorActionPreference = "Stop"

# Deploy Bridge assembly first
$bridgePath = "D:\Repositories\Hartonomous\src\Hartonomous.Sql.Bridge\bin\Release\netstandard2.0\Hartonomous.Sql.Bridge.dll"
Write-Host "===== Deploying Hartonomous.Sql.Bridge ====="
Write-Host "Reading: $bridgePath"
$bridgeBytes = [System.IO.File]::ReadAllBytes($bridgePath)
$bridgeHex = "0x" + [System.BitConverter]::ToString($bridgeBytes).Replace("-", "")
Write-Host "Size: $($bridgeBytes.Length) bytes"

$bridgeSql = @"
USE [$DatabaseName];

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Hartonomous.Sql.Bridge')
BEGIN
    DROP ASSEMBLY [Hartonomous.Sql.Bridge];
END;

CREATE ASSEMBLY [Hartonomous.Sql.Bridge]
FROM $bridgeHex
WITH PERMISSION_SET = SAFE;

SELECT 'Bridge: ' + a.name + ' (' + a.permission_set_desc + ')' AS Result
FROM sys.assemblies a
WHERE a.name = 'Hartonomous.Sql.Bridge';
"@

$tempFile1 = [System.IO.Path]::GetTempFileName() + ".sql"
$bridgeSql | Out-File -FilePath $tempFile1 -Encoding UTF8
sqlcmd -S $ServerName -E -C -i $tempFile1
Remove-Item $tempFile1

# Deploy main assembly
$mainPath = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll"
Write-Host "`n===== Deploying SqlClrFunctions ====="
Write-Host "Reading: $mainPath"
$mainBytes = [System.IO.File]::ReadAllBytes($mainPath)
$mainHex = "0x" + [System.BitConverter]::ToString($mainBytes).Replace("-", "")
Write-Host "Size: $($mainBytes.Length) bytes"

$mainSql = @"
USE [$DatabaseName];

CREATE ASSEMBLY [SqlClrFunctions]
FROM $mainHex
WITH PERMISSION_SET = UNSAFE;

SELECT
    a.name,
    a.permission_set_desc,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS clr_version
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions';
"@

$tempFile2 = [System.IO.Path]::GetTempFileName() + ".sql"
$mainSql | Out-File -FilePath $tempFile2 -Encoding UTF8
sqlcmd -S $ServerName -E -C -i $tempFile2
Remove-Item $tempFile2

Write-Host "`n===== Assembly deployment complete! ====="
