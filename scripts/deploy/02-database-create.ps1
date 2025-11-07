#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates the Hartonomous database if it doesn't exist on Arc-enabled SQL Server.

.DESCRIPTION
    Idempotent database creation script for Azure Arc SQL Server deployment.
    - Checks if database exists via sys.databases
    - Creates database with appropriate settings for Linux/Windows SQL Server
    - Configures database options for In-Memory OLTP, graph tables, temporal tables
    - Returns JSON with database status for Azure DevOps pipeline

.PARAMETER ServerName
    SQL Server instance name (e.g., hart-server.local or localhost)

.PARAMETER DatabaseName
    Name of the database to create (default: Hartonomous)

.PARAMETER SqlUser
    SQL authentication username (optional - uses Windows/integrated auth if not provided)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.EXAMPLE
    .\02-database-create.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous"
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

# Results object for JSON output
$result = @{
    Step = "DatabaseCreate"
    Success = $false
    DatabaseExists = $false
    DatabaseCreated = $false
    Platform = ""
    Settings = @{}
    Errors = @()
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
    Write-Host "=== Database Creation Script ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray
    
    # Detect platform
    Write-Host "`nDetecting platform..." -NoNewline
    $platform = Invoke-SqlCommand -Query "SELECT CASE WHEN @@VERSION LIKE '%Linux%' THEN 'Linux' ELSE 'Windows' END"
    $result.Platform = $platform
    Write-Host " $platform" -ForegroundColor Green
    
    # Check if database exists
    Write-Host "Checking if database exists..." -NoNewline
    $dbCheckQuery = @"
SELECT CASE WHEN EXISTS(SELECT 1 FROM sys.databases WHERE name = '$DatabaseName') 
       THEN 1 ELSE 0 END
"@
    $dbExists = [int](Invoke-SqlCommand -Query $dbCheckQuery)
    $result.DatabaseExists = ($dbExists -eq 1)
    
    if ($result.DatabaseExists) {
        Write-Host " Database already exists" -ForegroundColor Yellow
        
        # Get database settings
        $settingsQuery = @"
SELECT 
    compatibility_level,
    recovery_model_desc,
    is_memory_optimized_elevate_to_snapshot_on,
    is_query_store_on
FROM sys.databases 
WHERE name = '$DatabaseName'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $settingsJson = Invoke-SqlCommand -Query $settingsQuery
        $result.Settings = $settingsJson | ConvertFrom-Json
        
        $result.Success = $true
        $result.DatabaseCreated = $false
    }
    else {
        Write-Host " Database does not exist" -ForegroundColor Yellow
        Write-Host "Creating database..." -NoNewline
        
        # Determine data paths based on platform
        if ($platform -eq "Linux") {
            # SQL Server on Linux standard paths
            $dataPath = "/var/opt/mssql/data"
            $logPath = "/var/opt/mssql/data"
        }
        else {
            # Query default paths from SQL Server on Windows
            $defaultPathQuery = @"
DECLARE @DataPath NVARCHAR(512), @LogPath NVARCHAR(512)
SELECT @DataPath = CONVERT(NVARCHAR(512), SERVERPROPERTY('InstanceDefaultDataPath'))
SELECT @LogPath = CONVERT(NVARCHAR(512), SERVERPROPERTY('InstanceDefaultLogPath'))
SELECT ISNULL(@DataPath, 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\') AS DataPath,
       ISNULL(@LogPath, 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\') AS LogPath
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
            $pathsJson = Invoke-SqlCommand -Query $defaultPathQuery
            $paths = $pathsJson | ConvertFrom-Json
            $dataPath = $paths.DataPath.TrimEnd('\')
            $logPath = $paths.LogPath.TrimEnd('\')
        }
        
        # Create database with platform-appropriate paths
        $createDbSql = @"
CREATE DATABASE [$DatabaseName]
ON PRIMARY 
(
    NAME = N'${DatabaseName}_data',
    FILENAME = N'$dataPath/${DatabaseName}_data.mdf',
    SIZE = 1GB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 512MB
)
LOG ON 
(
    NAME = N'${DatabaseName}_log',
    FILENAME = N'$logPath/${DatabaseName}_log.ldf',
    SIZE = 512MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 256MB
);

-- Set database options for modern SQL Server features
ALTER DATABASE [$DatabaseName] SET COMPATIBILITY_LEVEL = 160; -- SQL Server 2022+
ALTER DATABASE [$DatabaseName] SET RECOVERY FULL;
ALTER DATABASE [$DatabaseName] SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE [$DatabaseName] SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE [$DatabaseName] SET AUTO_UPDATE_STATISTICS_ASYNC ON;
ALTER DATABASE [$DatabaseName] SET PAGE_VERIFY CHECKSUM;
ALTER DATABASE [$DatabaseName] SET READ_COMMITTED_SNAPSHOT ON;

-- Enable In-Memory OLTP support
ALTER DATABASE [$DatabaseName] SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;

-- Enable Query Store for performance monitoring
ALTER DATABASE [$DatabaseName] SET QUERY_STORE = ON;
ALTER DATABASE [$DatabaseName] SET QUERY_STORE (
    OPERATION_MODE = READ_WRITE,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    INTERVAL_LENGTH_MINUTES = 60,
    MAX_STORAGE_SIZE_MB = 1024,
    QUERY_CAPTURE_MODE = AUTO
);
"@

        Invoke-SqlCommand -Query $createDbSql
        Write-Host " Created" -ForegroundColor Green
        
        # Verify creation
        $verifyQuery = @"
SELECT 
    d.name,
    d.database_id,
    d.compatibility_level,
    d.recovery_model_desc,
    d.is_memory_optimized_elevate_to_snapshot_on,
    d.is_query_store_on,
    mf.physical_name as data_file_path
FROM sys.databases d
LEFT JOIN sys.master_files mf ON d.database_id = mf.database_id AND mf.type = 0
WHERE d.name = '$DatabaseName'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $verifyJson = Invoke-SqlCommand -Query $verifyQuery
        $result.Settings = $verifyJson | ConvertFrom-Json
        
        $result.Success = $true
        $result.DatabaseExists = $true
        $result.DatabaseCreated = $true
        
        Write-Host "✓ Database created successfully" -ForegroundColor Green
        Write-Host "  - Compatibility Level: $($result.Settings.compatibility_level)" -ForegroundColor Gray
        Write-Host "  - Recovery Model: $($result.Settings.recovery_model_desc)" -ForegroundColor Gray
        Write-Host "  - Query Store: $($result.Settings.is_query_store_on)" -ForegroundColor Gray
        Write-Host "  - Data File: $($result.Settings.data_file_path)" -ForegroundColor Gray
    }
}
catch {
    $result.Success = $false
    $result.Errors += $_.Exception.Message
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Output JSON for pipeline
$jsonOutput = $result | ConvertTo-Json -Depth 5 -Compress
Write-Host "`n=== JSON Output ===" -ForegroundColor Cyan
Write-Host $jsonOutput

# Write to artifact staging directory if in Azure DevOps
if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "database-create.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
