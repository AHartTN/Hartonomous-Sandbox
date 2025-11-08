#!/usr/bin/env pwsh
# Quick script to create UNSAFE CLR assembly
param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$AssemblyPath = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release\SqlClrFunctions.dll"
)

$ErrorActionPreference = "Stop"

Write-Host "Reading assembly from: $AssemblyPath"
$assemblyBytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
$hexString = "0x" + [System.BitConverter]::ToString($assemblyBytes).Replace("-", "")
Write-Host "Assembly size: $($assemblyBytes.Length) bytes"

# Create SQL to deploy assembly
$sql = @"
USE [$DatabaseName];

CREATE ASSEMBLY [SqlClrFunctions]
FROM $hexString
WITH PERMISSION_SET = UNSAFE;

SELECT
    a.name,
    a.permission_set_desc,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS clr_version
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions';
"@

# Save to temp file (SQL command line length limit)
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
$sql | Out-File -FilePath $tempFile -Encoding UTF8

Write-Host "Creating assembly with PERMISSION_SET = UNSAFE..."
sqlcmd -S $ServerName -E -C -i $tempFile

Remove-Item $tempFile

Write-Host "Assembly deployment complete!"
