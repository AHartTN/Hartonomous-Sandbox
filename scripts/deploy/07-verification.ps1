#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies complete Hartonomous database deployment on Arc-enabled SQL Server.

.DESCRIPTION
    Comprehensive post-deployment verification for Azure Arc SQL Server.
    - Verifies database exists and settings are correct
    - Checks CLR assembly deployment (SqlClrFunctions with UNSAFE permission)
    - Verifies FILESTREAM filegroup configuration
    - Checks Service Broker infrastructure (queues, services active)
    - Verifies EF Core migrations applied (__EFMigrationsHistory)
    - Checks spatial indexes (NetTopologySuite R-tree indexes)
    - Returns JSON with verification results for Azure DevOps pipeline

.PARAMETER ServerName
    SQL Server instance name

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER SqlUser
    SQL authentication username (optional)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.PARAMETER FailOnWarnings
    Treat warnings as failures (default: false)

.EXAMPLE
    .\07-verification.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous"
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
    [SecureString]$SqlPassword,
    
    [Parameter(Mandatory=$false)]
    [switch]$FailOnWarnings
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$result = @{
    Step = "DeploymentVerification"
    Success = $false
    Checks = @{
        Database = @{ Passed = $false; Details = @{} }
        CLR = @{ Passed = $false; Details = @{} }
        FILESTREAM = @{ Passed = $false; Details = @{} }
        ServiceBroker = @{ Passed = $false; Details = @{} }
        Migrations = @{ Passed = $false; Details = @{} }
        SpatialIndexes = @{ Passed = $false; Details = @{} }
    }
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
    Write-Host "=== Database Deployment Verification ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray
    
    # Check 1: Database exists and configuration
    Write-Host "`n[1/6] Verifying database..." -NoNewline
    try {
        $dbQuery = @"
SELECT 
    d.name,
    d.compatibility_level,
    d.recovery_model_desc,
    d.is_memory_optimized_elevate_to_snapshot_on,
    d.is_query_store_on,
    d.is_broker_enabled
FROM sys.databases d
WHERE d.name = '$DatabaseName'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $dbJson = Invoke-SqlCommand -Query $dbQuery -Database "master"
        $dbInfo = $dbJson | ConvertFrom-Json
        
        $result.Checks.Database.Passed = $true
        $result.Checks.Database.Details = $dbInfo
        Write-Host " ✓ PASS" -ForegroundColor Green
        Write-Host "  Compatibility Level: $($dbInfo.compatibility_level)" -ForegroundColor Gray
        Write-Host "  Recovery Model: $($dbInfo.recovery_model_desc)" -ForegroundColor Gray
    }
    catch {
        $result.Checks.Database.Passed = $false
        $result.Errors += "Database check failed: $($_.Exception.Message)"
        Write-Host " ✗ FAIL" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check 2: CLR assembly
    Write-Host "[2/6] Verifying CLR assembly..." -NoNewline
    try {
        $clrQuery = @"
SELECT 
    a.name,
    a.clr_name,
    a.permission_set_desc,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS clr_version,
    (SELECT COUNT(*) FROM sys.assembly_modules am WHERE am.assembly_id = a.assembly_id) AS object_count
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $clrJson = Invoke-SqlCommand -Query $clrQuery -Database $DatabaseName
        
        if ($clrJson -and $clrJson -ne "null") {
            $clrInfo = $clrJson | ConvertFrom-Json
            $result.Checks.CLR.Passed = $true
            $result.Checks.CLR.Details = $clrInfo
            Write-Host " ✓ PASS" -ForegroundColor Green
            Write-Host "  Assembly: $($clrInfo.clr_name)" -ForegroundColor Gray
            Write-Host "  Permission Set: $($clrInfo.permission_set_desc)" -ForegroundColor Gray
            Write-Host "  CLR Objects: $($clrInfo.object_count)" -ForegroundColor Gray
            
            if ($clrInfo.permission_set_desc -ne "UNSAFE") {
                $result.Warnings += "CLR assembly permission set is $($clrInfo.permission_set_desc), expected UNSAFE for ILGPU support"
                Write-Host "  ! Warning: Permission set not UNSAFE" -ForegroundColor Yellow
            }
        }
        else {
            $result.Checks.CLR.Passed = $false
            $result.Errors += "SqlClrFunctions assembly not found in sys.assemblies"
            Write-Host " ✗ FAIL" -ForegroundColor Red
            Write-Host "  SqlClrFunctions assembly not deployed" -ForegroundColor Red
        }
    }
    catch {
        $result.Checks.CLR.Passed = $false
        $result.Errors += "CLR check failed: $($_.Exception.Message)"
        Write-Host " ✗ FAIL" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check 3: FILESTREAM
    Write-Host "[3/6] Verifying FILESTREAM..." -NoNewline
    try {
        $filestreamQuery = @"
SELECT 
    fg.name AS filegroup_name,
    df.name AS file_name,
    df.physical_name,
    df.state_desc
FROM sys.filegroups fg
INNER JOIN sys.database_files df ON fg.data_space_id = df.data_space_id
WHERE fg.type = 'FD'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $filestreamJson = Invoke-SqlCommand -Query $filestreamQuery -Database $DatabaseName
        
        if ($filestreamJson -and $filestreamJson -ne "null") {
            $filestreamInfo = $filestreamJson | ConvertFrom-Json
            $result.Checks.FILESTREAM.Passed = $true
            $result.Checks.FILESTREAM.Details = $filestreamInfo
            Write-Host " ✓ PASS" -ForegroundColor Green
            Write-Host "  Filegroup: $($filestreamInfo.filegroup_name)" -ForegroundColor Gray
            Write-Host "  Path: $($filestreamInfo.physical_name)" -ForegroundColor Gray
            Write-Host "  State: $($filestreamInfo.state_desc)" -ForegroundColor Gray
        }
        else {
            $result.Checks.FILESTREAM.Passed = $false
            $result.Warnings += "FILESTREAM filegroup not configured"
            Write-Host " ✗ FAIL" -ForegroundColor Red
            Write-Host "  No FILESTREAM filegroup found" -ForegroundColor Red
        }
    }
    catch {
        $result.Checks.FILESTREAM.Passed = $false
        $result.Errors += "FILESTREAM check failed: $($_.Exception.Message)"
        Write-Host " ✗ FAIL" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check 4: Service Broker
    Write-Host "[4/6] Verifying Service Broker..." -NoNewline
    try {
        $brokerQuery = @"
SELECT 
    (SELECT COUNT(*) FROM sys.service_queues WHERE name NOT LIKE 'sys%') AS queue_count,
    (SELECT COUNT(*) FROM sys.services WHERE name != 'DEFAULT') AS service_count,
    (SELECT COUNT(*) FROM sys.service_message_types WHERE name LIKE '%Message') AS message_type_count,
    (SELECT is_broker_enabled FROM sys.databases WHERE name = '$DatabaseName') AS broker_enabled
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $brokerJson = Invoke-SqlCommand -Query $brokerQuery -Database $DatabaseName
        $brokerInfo = $brokerJson | ConvertFrom-Json
        
        $result.Checks.ServiceBroker.Passed = $brokerInfo.broker_enabled -and 
                                               $brokerInfo.queue_count -gt 0 -and 
                                               $brokerInfo.service_count -gt 0
        $result.Checks.ServiceBroker.Details = $brokerInfo
        
        if ($result.Checks.ServiceBroker.Passed) {
            Write-Host " ✓ PASS" -ForegroundColor Green
            Write-Host "  Enabled: $($brokerInfo.broker_enabled)" -ForegroundColor Gray
            Write-Host "  Queues: $($brokerInfo.queue_count)" -ForegroundColor Gray
            Write-Host "  Services: $($brokerInfo.service_count)" -ForegroundColor Gray
            Write-Host "  Message Types: $($brokerInfo.message_type_count)" -ForegroundColor Gray
        }
        else {
            Write-Host " ✗ FAIL" -ForegroundColor Red
            if (-not $brokerInfo.broker_enabled) {
                Write-Host "  Service Broker not enabled" -ForegroundColor Red
            }
            if ($brokerInfo.queue_count -eq 0) {
                Write-Host "  No queues configured" -ForegroundColor Red
            }
            if ($brokerInfo.service_count -eq 0) {
                Write-Host "  No services configured" -ForegroundColor Red
            }
        }
    }
    catch {
        $result.Checks.ServiceBroker.Passed = $false
        $result.Errors += "Service Broker check failed: $($_.Exception.Message)"
        Write-Host " ✗ FAIL" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check 5: EF Core migrations
    Write-Host "[5/6] Verifying EF migrations..." -NoNewline
    try {
        $migrationsQuery = @"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    SELECT 
        (SELECT COUNT(*) FROM [__EFMigrationsHistory]) AS migration_count,
        (SELECT TOP 1 MigrationId FROM [__EFMigrationsHistory] ORDER BY MigrationId DESC) AS latest_migration,
        (SELECT TOP 1 ProductVersion FROM [__EFMigrationsHistory] ORDER BY MigrationId DESC) AS ef_version
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
END
ELSE
BEGIN
    SELECT '{"migration_count":0}' AS JsonResult
END
"@
        $migrationsJson = Invoke-SqlCommand -Query $migrationsQuery -Database $DatabaseName
        $migrationsInfo = $migrationsJson | ConvertFrom-Json
        
        $result.Checks.Migrations.Passed = $migrationsInfo.migration_count -gt 0
        $result.Checks.Migrations.Details = $migrationsInfo
        
        if ($result.Checks.Migrations.Passed) {
            Write-Host " ✓ PASS" -ForegroundColor Green
            Write-Host "  Migrations Applied: $($migrationsInfo.migration_count)" -ForegroundColor Gray
            Write-Host "  Latest: $($migrationsInfo.latest_migration)" -ForegroundColor Gray
            Write-Host "  EF Version: $($migrationsInfo.ef_version)" -ForegroundColor Gray
        }
        else {
            Write-Host " ✗ FAIL" -ForegroundColor Red
            Write-Host "  No migrations applied or __EFMigrationsHistory table missing" -ForegroundColor Red
            $result.Errors += "No EF Core migrations found"
        }
    }
    catch {
        $result.Checks.Migrations.Passed = $false
        $result.Errors += "Migrations check failed: $($_.Exception.Message)"
        Write-Host " ✗ FAIL" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check 6: Spatial indexes (NetTopologySuite)
    Write-Host "[6/6] Verifying spatial indexes..." -NoNewline
    try {
        $spatialQuery = @"
SELECT 
    (SELECT COUNT(*) FROM sys.columns c 
     INNER JOIN sys.types t ON c.system_type_id = t.system_type_id 
     WHERE t.name = 'geometry') AS geometry_column_count,
    (SELECT COUNT(*) FROM sys.indexes 
     WHERE type_desc = 'SPATIAL') AS spatial_index_count
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
        $spatialJson = Invoke-SqlCommand -Query $spatialQuery -Database $DatabaseName
        $spatialInfo = $spatialJson | ConvertFrom-Json
        
        # Spatial indexes are optional but geometry columns should exist
        $result.Checks.SpatialIndexes.Passed = $spatialInfo.geometry_column_count -gt 0
        $result.Checks.SpatialIndexes.Details = $spatialInfo
        
        if ($result.Checks.SpatialIndexes.Passed) {
            Write-Host " ✓ PASS" -ForegroundColor Green
            Write-Host "  Geometry Columns: $($spatialInfo.geometry_column_count)" -ForegroundColor Gray
            Write-Host "  Spatial Indexes: $($spatialInfo.spatial_index_count)" -ForegroundColor Gray
            
            if ($spatialInfo.spatial_index_count -eq 0) {
                $result.Warnings += "No spatial indexes found - consider creating for query performance"
                Write-Host "  ! No spatial indexes (performance warning)" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host " ! WARN" -ForegroundColor Yellow
            Write-Host "  No geometry columns found (NetTopologySuite spatial types not used)" -ForegroundColor Yellow
            $result.Warnings += "No spatial geometry columns found"
        }
    }
    catch {
        $result.Checks.SpatialIndexes.Passed = $false
        $result.Warnings += "Spatial index check failed: $($_.Exception.Message)"
        Write-Host " ! WARN" -ForegroundColor Yellow
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Overall success determination
    $criticalChecksPassed = $result.Checks.Database.Passed -and 
                            $result.Checks.CLR.Passed -and 
                            $result.Checks.Migrations.Passed
    
    $result.Success = $criticalChecksPassed
    
    # If FailOnWarnings flag is set and warnings exist, mark as failure
    if ($FailOnWarnings -and $result.Warnings.Count -gt 0) {
        $result.Success = $false
    }
    
    Write-Host "`n=== Verification Summary ===" -ForegroundColor Cyan
    $passCount = ($result.Checks.Values | Where-Object { $_.Passed }).Count
    $totalCount = $result.Checks.Count
    
    Write-Host "Checks Passed: $passCount / $totalCount" -ForegroundColor $(if ($result.Success) { "Green" } else { "Yellow" })
    Write-Host "Errors: $($result.Errors.Count)" -ForegroundColor $(if ($result.Errors.Count -gt 0) { "Red" } else { "Green" })
    Write-Host "Warnings: $($result.Warnings.Count)" -ForegroundColor $(if ($result.Warnings.Count -gt 0) { "Yellow" } else { "Green" })
    
    if ($result.Errors.Count -gt 0) {
        Write-Host "`nErrors:" -ForegroundColor Red
        foreach ($errorMsg in $result.Errors) {
            Write-Host "  - $errorMsg" -ForegroundColor Red
        }
    }
    
    if ($result.Warnings.Count -gt 0) {
        Write-Host "`nWarnings:" -ForegroundColor Yellow
        foreach ($warningMsg in $result.Warnings) {
            Write-Host "  - $warningMsg" -ForegroundColor Yellow
        }
    }
    
    if ($result.Success) {
        Write-Host "`n✓ Deployment verification PASSED" -ForegroundColor Green
    }
    else {
        Write-Host "`n✗ Deployment verification FAILED" -ForegroundColor Red
    }
}
catch {
    $result.Success = $false
    $result.Errors += $_.Exception.Message
    Write-Host "`n✗ Verification script failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Output JSON
$jsonOutput = $result | ConvertTo-Json -Depth 10 -Compress
Write-Host "`n=== JSON Output ===" -ForegroundColor Cyan
Write-Host $jsonOutput

if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "verification.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
