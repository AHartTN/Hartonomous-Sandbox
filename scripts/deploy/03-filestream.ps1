#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Configures FILESTREAM support for Hartonomous database on Arc-enabled SQL Server.

.DESCRIPTION
    Idempotent FILESTREAM setup for Azure Arc SQL Server (Windows/Linux).
    - Verifies SQL Server 2019+ for Linux FILESTREAM support
    - Checks sp_configure 'filestream access level'
    - Creates FILESTREAM filegroup and file if missing
    - Handles platform-specific paths (/var/opt/mssql/data for Linux)
    - Returns JSON with FILESTREAM status for Azure DevOps pipeline

.PARAMETER ServerName
    SQL Server instance name

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER SqlUser
    SQL authentication username (optional)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.EXAMPLE
    .\03-filestream.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlUser,
    
    [Parameter(Mandatory=$false)]
    [SecureString]$SqlPassword
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$result = @{
    Step = "FilestreamSetup"
    Success = $false
    Platform = ""
    SqlVersion = ""
    FilestreamConfigLevel = 0
    FilestreamSupported = $false
    FilegroupExists = $false
    FilegroupCreated = $false
    FilePath = ""
    Errors = @()
    Warnings = @()
    Timestamp = (Get-Date -Format "o")
}

function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$Database = "master"
    )
    
    $sqlArgs = @("-S", $ServerName, "-d", $Database, "-C", "-b", "-Q", $Query, "-h", "-1")
    
    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }
    
    $output = & sqlcmd @sqlArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SQL command failed: $output"
    }
    return ($output | Out-String).Trim()
}

try {
    Write-Host "=== FILESTREAM Setup Script ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray
    
    # Get platform and version info
    Write-Host "`nGathering SQL Server information..." -NoNewline
    $serverInfoQuery = @"
SELECT 
    CASE WHEN @@VERSION LIKE '%Linux%' THEN 'Linux' ELSE 'Windows' END AS Platform,
    CAST(SERVERPROPERTY('ProductVersion') AS VARCHAR) AS VersionString,
    CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) AS MajorVersion,
    CAST(SERVERPROPERTY('ProductMinorVersion') AS INT) AS MinorVersion
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
    $serverInfoJson = Invoke-SqlCommand -Query $serverInfoQuery
    $serverInfo = $serverInfoJson | ConvertFrom-Json
    $result.Platform = $serverInfo.Platform
    $result.SqlVersion = $serverInfo.VersionString
    Write-Host " Done" -ForegroundColor Green
    Write-Host "  Platform: $($serverInfo.Platform), Version: $($serverInfo.VersionString)" -ForegroundColor Gray
    
    # Check FILESTREAM support for Linux
    if ($serverInfo.Platform -eq "Linux") {
        # SQL Server 2019 is version 15.x, 2022 is 16.x, 2025 is 17.x
        if ($serverInfo.MajorVersion -lt 15) {
            $result.FilestreamSupported = $false
            $result.Errors += "FILESTREAM on Linux requires SQL Server 2019 (15.x) or later. Current version: $($serverInfo.VersionString)"
            throw "FILESTREAM not supported on SQL Server $($serverInfo.VersionString) for Linux"
        }
        $result.FilestreamSupported = $true
        Write-Host "✓ SQL Server version supports FILESTREAM on Linux" -ForegroundColor Green
    }
    else {
        $result.FilestreamSupported = $true
        Write-Host "✓ SQL Server on Windows supports FILESTREAM" -ForegroundColor Green
    }
    
    # Check FILESTREAM access level
    Write-Host "Checking FILESTREAM configuration..." -NoNewline
    $filestreamLevelQuery = "SELECT CAST(value AS INT) AS filestream_level FROM sys.configurations WHERE name = 'filestream access level'"
    $filestreamLevel = [int](Invoke-SqlCommand -Query $filestreamLevelQuery)
    $result.FilestreamConfigLevel = $filestreamLevel
    
    if ($filestreamLevel -eq 0) {
        Write-Host " FILESTREAM disabled at server level" -ForegroundColor Yellow
        $result.Warnings += "FILESTREAM is disabled. Enable via: sp_configure 'filestream access level', 2; RECONFIGURE; (requires SQL Server restart)"
        Write-Warning "FILESTREAM must be enabled at the instance level before creating filegroups"
        Write-Warning "Run: EXEC sp_configure 'filestream access level', 2; RECONFIGURE;"
        Write-Warning "Then restart SQL Server service"
        # We'll continue to check/create the filegroup structure for idempotency
    }
    elseif ($filestreamLevel -eq 1) {
        Write-Host " Transact-SQL access enabled" -ForegroundColor Yellow
        $result.Warnings += "FILESTREAM configured for T-SQL access only (level 1). Full support requires level 2."
    }
    else {
        Write-Host " Fully enabled (level $filestreamLevel)" -ForegroundColor Green
    }
    
    # Check if FILESTREAM filegroup exists
    Write-Host "Checking FILESTREAM filegroup..." -NoNewline
    $filegroupCheckQuery = @"
SELECT 
    fg.name AS FilegroupName,
    fg.type_desc,
    df.name AS FileName,
    df.physical_name
FROM sys.filegroups fg
LEFT JOIN sys.database_files df ON fg.data_space_id = df.data_space_id
WHERE fg.type = 'FD' -- FILESTREAM
FOR JSON PATH
"@
    $filegroupJson = Invoke-SqlCommand -Query $filegroupCheckQuery -Database $DatabaseName
    
    if ($filegroupJson -and $filegroupJson -ne "[]") {
        $filegroups = $filegroupJson | ConvertFrom-Json
        $result.FilegroupExists = $true
        $result.FilePath = $filegroups[0].physical_name
        Write-Host " Exists" -ForegroundColor Green
        Write-Host "  Filegroup: $($filegroups[0].FilegroupName)" -ForegroundColor Gray
        Write-Host "  File: $($filegroups[0].FileName)" -ForegroundColor Gray
        Write-Host "  Path: $($filegroups[0].physical_name)" -ForegroundColor Gray
    }
    else {
        Write-Host " Not found" -ForegroundColor Yellow
        Write-Host "Creating FILESTREAM filegroup..." -NoNewline
        
        # Determine filestream path based on platform
        if ($serverInfo.Platform -eq "Linux") {
            $filestreamPath = "/var/opt/mssql/data/${DatabaseName}_filestream"
        }
        else {
            # Get default data path for Windows
            $defaultPathQuery = @"
DECLARE @DataPath NVARCHAR(512)
SELECT @DataPath = CONVERT(NVARCHAR(512), SERVERPROPERTY('InstanceDefaultDataPath'))
SELECT ISNULL(@DataPath, 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\')
"@
            $dataPath = (Invoke-SqlCommand -Query $defaultPathQuery).Trim().TrimEnd('\')
            $filestreamPath = "$dataPath\${DatabaseName}_filestream"
        }
        
        # Create FILESTREAM filegroup and file
        $createFilestreamSql = @"
USE [$DatabaseName];

-- Create FILESTREAM filegroup
ALTER DATABASE [$DatabaseName]
ADD FILEGROUP [${DatabaseName}_FileStream] CONTAINS FILESTREAM;

-- Add FILESTREAM file to the filegroup
ALTER DATABASE [$DatabaseName]
ADD FILE 
(
    NAME = N'${DatabaseName}_FileStream_File',
    FILENAME = N'$filestreamPath'
)
TO FILEGROUP [${DatabaseName}_FileStream];
"@

        Invoke-SqlCommand -Query $createFilestreamSql -Database "master"
        $result.FilegroupCreated = $true
        $result.FilegroupExists = $true
        $result.FilePath = $filestreamPath
        Write-Host " Created" -ForegroundColor Green
        Write-Host "  Path: $filestreamPath" -ForegroundColor Gray
    }
    
    # Verify final state
    $verifyQuery = @"
SELECT 
    COUNT(*) AS FilestreamFilegroupCount
FROM sys.filegroups 
WHERE type = 'FD'
"@
    $filegroupCount = [int](Invoke-SqlCommand -Query $verifyQuery -Database $DatabaseName)
    
    if ($filegroupCount -gt 0) {
        $result.Success = $true
        Write-Host "`n✓ FILESTREAM configuration verified" -ForegroundColor Green
    }
    else {
        throw "FILESTREAM filegroup verification failed"
    }
}
catch {
    $result.Success = $false
    $result.Errors += $_.Exception.Message
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Output JSON
$jsonOutput = $result | ConvertTo-Json -Depth 5 -Compress
Write-Host "`n=== JSON Output ===" -ForegroundColor Cyan
Write-Host $jsonOutput

if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "filestream-setup.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
