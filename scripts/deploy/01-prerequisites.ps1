#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Validates prerequisites for Hartonomous database deployment to Azure Arc-enabled SQL Server.

.DESCRIPTION
    Checks SQL Server connectivity, version, Arc agent status, CLR configuration, and deployment environment.
    Returns structured JSON for Azure DevOps pipeline consumption.
    
    Works with:
    - SQL Server 2025 on Linux (Ubuntu 22.04) - Azure Arc-enabled
    - SQL Server 2022+ on Windows - Azure Arc-enabled
    
.PARAMETER ServerName
    SQL Server instance name (e.g., 'hart-server' or 'localhost')

.PARAMETER DatabaseName
    Target database name

.PARAMETER SqlUser
    SQL Server authentication username (optional - uses integrated auth if not provided)

.PARAMETER SqlPassword
    SQL Server authentication password (required if SqlUser provided)

.EXAMPLE
    ./01-prerequisites.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,

    [string]$SqlUser,
    [SecureString]$SqlPassword
)

$ErrorActionPreference = "Stop"
$results = @{
    ServerName = $ServerName
    DatabaseName = $DatabaseName
    Timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    Checks = @{}
    Passed = $false
    Errors = @()
}

function Test-SqlCommand {
    param([string]$Query)
    
    $sqlArgs = @("-S", $ServerName, "-d", "master", "-C", "-b", "-Q", $Query, "-h", "-1")
    
    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }
    
    $output = & sqlcmd @sqlArgs 2>&1
    return @{
        Success = $LASTEXITCODE -eq 0
        Output = ($output | Out-String).Trim()
    }
}

try {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Hartonomous Prerequisites Check"
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    # Check 1: sqlcmd availability
    Write-Host "✓ Checking sqlcmd..." -NoNewline
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw "sqlcmd not found. Install SQL Server command-line tools."
    }
    Write-Host " OK" -ForegroundColor Green
    $results.Checks.SqlCmd = @{ Status = "OK"; Version = (sqlcmd -? 2>&1 | Select-String "Version" | Select-Object -First 1).ToString() }

    # Check 2: dotnet CLI
    Write-Host "✓ Checking dotnet CLI..." -NoNewline
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet CLI not found. Install .NET SDK 9.0 or later."
    }
    $dotnetVersion = dotnet --version
    Write-Host " OK (v$dotnetVersion)" -ForegroundColor Green
    $results.Checks.DotNet = @{ Status = "OK"; Version = $dotnetVersion }

    # Check 3: SQL Server connectivity
    Write-Host "✓ Checking SQL Server connection to $ServerName..." -NoNewline
    $versionCheck = Test-SqlCommand -Query "SELECT @@VERSION"
    if (-not $versionCheck.Success) {
        throw "Cannot connect to SQL Server: $($versionCheck.Output)"
    }
    Write-Host " OK" -ForegroundColor Green
    
    $sqlVersion = $versionCheck.Output
    $results.Checks.SqlServerConnection = @{ 
        Status = "OK"
        Version = $sqlVersion
    }

    # Check 4: SQL Server version (must be 2019+ for FILESTREAM on Linux, 2017+ for CLR strict security)
    Write-Host "✓ Checking SQL Server version..." -NoNewline
    $versionQuery = "SELECT SERVERPROPERTY('ProductVersion') AS Version, SERVERPROPERTY('ProductLevel') AS Level, SERVERPROPERTY('Edition') AS Edition, SERVERPROPERTY('EngineEdition') AS EngineEdition"
    $versionInfo = Test-SqlCommand -Query $versionQuery
    
    if ($versionInfo.Output -match "(\d+)\.(\d+)\.(\d+)") {
        $majorVersion = [int]$Matches[1]
        if ($majorVersion -lt 14) {
            throw "SQL Server version $majorVersion not supported. Requires SQL Server 2017 (14.x) or later."
        }
        Write-Host " OK (v$majorVersion)" -ForegroundColor Green
        $results.Checks.SqlServerVersion = @{
            Status = "OK"
            MajorVersion = $majorVersion
            FullVersion = $Matches[0]
        }
    } else {
        throw "Cannot parse SQL Server version from: $($versionInfo.Output)"
    }

    # Check 5: Platform (Windows vs Linux)
    Write-Host "✓ Detecting SQL Server platform..." -NoNewline
    $platformQuery = "SELECT CASE WHEN @@VERSION LIKE '%Linux%' THEN 'Linux' WHEN @@VERSION LIKE '%Windows%' THEN 'Windows' ELSE 'Unknown' END AS Platform"
    $platformInfo = Test-SqlCommand -Query $platformQuery
    $platform = $platformInfo.Output.Trim()
    Write-Host " $platform" -ForegroundColor Green
    $results.Checks.Platform = @{
        Status = "OK"
        Platform = $platform
    }

    # Check 6: CLR enabled
    Write-Host "✓ Checking CLR enabled..." -NoNewline
    $clrQuery = "SELECT value_in_use FROM sys.configurations WHERE name = 'clr enabled'"
    $clrEnabled = Test-SqlCommand -Query $clrQuery
    $clrValue = [int]$clrEnabled.Output.Trim()
    
    if ($clrValue -eq 0) {
        Write-Host " DISABLED (will enable during deployment)" -ForegroundColor Yellow
        $results.Checks.ClrEnabled = @{
            Status = "WILL_ENABLE"
            CurrentValue = $clrValue
        }
    } else {
        Write-Host " OK" -ForegroundColor Green
        $results.Checks.ClrEnabled = @{
            Status = "OK"
            CurrentValue = $clrValue
        }
    }

    # Check 7: CLR strict security
    Write-Host "✓ Checking CLR strict security..." -NoNewline
    $clrStrictQuery = "SELECT value_in_use FROM sys.configurations WHERE name = 'clr strict security'"
    $clrStrict = Test-SqlCommand -Query $clrStrictQuery
    $clrStrictValue = [int]$clrStrict.Output.Trim()
    Write-Host " $(if ($clrStrictValue -eq 1) { 'ENABLED (requires signed assembly or TRUSTWORTHY)' } else { 'DISABLED' })" -ForegroundColor $(if ($clrStrictValue -eq 1) { 'Yellow' } else { 'Green' })
    $results.Checks.ClrStrictSecurity = @{
        Status = "OK"
        Enabled = ($clrStrictValue -eq 1)
        Note = if ($clrStrictValue -eq 1) { "MS Docs: Sign assembly or use sys.sp_add_trusted_assembly" } else { "Not recommended to disable in production" }
    }

    # Check 8: FILESTREAM support (on Linux requires SQL 2019+)
    Write-Host "✓ Checking FILESTREAM support..." -NoNewline
    if ($platform -eq "Linux" -and $majorVersion -lt 15) {
        Write-Host " NOT SUPPORTED (Linux requires SQL Server 2019+)" -ForegroundColor Red
        $results.Checks.FilestreamSupport = @{
            Status = "NOT_SUPPORTED"
            Platform = $platform
            Version = $majorVersion
            Note = "FILESTREAM on Linux requires SQL Server 2019 (15.x) or later"
        }
        $results.Errors += "FILESTREAM not supported on Linux with SQL Server version $majorVersion"
    } else {
        $filestreamQuery = "SELECT SERVERPROPERTY('FilestreamConfiguredLevel') AS ConfigLevel, SERVERPROPERTY('FilestreamShareName') AS ShareName, SERVERPROPERTY('FilestreamEffectiveLevel') AS EffectiveLevel"
        $filestreamInfo = Test-SqlCommand -Query $filestreamQuery
        Write-Host " OK (will configure if needed)" -ForegroundColor Green
        $results.Checks.FilestreamSupport = @{
            Status = "OK"
            Platform = $platform
            ConfigInfo = $filestreamInfo.Output
        }
    }

    # Check 9: Database exists
    Write-Host "✓ Checking if database '$DatabaseName' exists..." -NoNewline
    $dbExistsQuery = "SELECT DB_ID('$DatabaseName') AS DbId"
    $dbExists = Test-SqlCommand -Query $dbExistsQuery
    $dbId = $dbExists.Output.Trim()
    
    if ($dbId -eq "NULL" -or [string]::IsNullOrWhiteSpace($dbId)) {
        Write-Host " NO (will create)" -ForegroundColor Yellow
        $results.Checks.DatabaseExists = @{
            Status = "WILL_CREATE"
            Exists = $false
        }
    } else {
        Write-Host " YES (DB_ID=$dbId)" -ForegroundColor Green
        $results.Checks.DatabaseExists = @{
            Status = "OK"
            Exists = $true
            DatabaseId = [int]$dbId
        }
    }

    # Check 10: Azure Arc agent (best effort - may not be accessible from SQL)
    Write-Host "✓ Checking Azure Arc integration..." -NoNewline
    if ($platform -eq "Linux") {
        $arcAgentCheck = Get-Command azcmagent -ErrorAction SilentlyContinue
        if ($arcAgentCheck) {
            Write-Host " Arc agent detected" -ForegroundColor Green
            $results.Checks.AzureArc = @{
                Status = "DETECTED"
                AgentPath = $arcAgentCheck.Source
            }
        } else {
            Write-Host " Arc agent not detected (may not be in PATH)" -ForegroundColor Yellow
            $results.Checks.AzureArc = @{
                Status = "UNKNOWN"
                Note = "Azure Arc agent not found in PATH - deployment will continue"
            }
        }
    } else {
        Write-Host " Skipped (check via Azure portal)" -ForegroundColor Gray
        $results.Checks.AzureArc = @{
            Status = "SKIPPED"
            Platform = $platform
        }
    }

    # Final summary
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Prerequisites Check: PASSED"
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Environment Summary:" -ForegroundColor Yellow
    Write-Host "  SQL Server: $ServerName" -ForegroundColor Gray
    Write-Host "  Platform: $platform" -ForegroundColor Gray
    Write-Host "  Version: SQL Server v$majorVersion" -ForegroundColor Gray
    Write-Host "  CLR: $(if ($clrValue -eq 1) { 'Enabled' } else { 'Will Enable' })" -ForegroundColor Gray
    Write-Host "  CLR Strict Security: $(if ($clrStrictValue -eq 1) { 'Enabled (Secure)' } else { 'Disabled' })" -ForegroundColor Gray
    Write-Host "  Database: $DatabaseName $(if ($results.Checks.DatabaseExists.Exists) { '(Exists)' } else { '(Will Create)' })" -ForegroundColor Gray
    Write-Host ""

    $results.Passed = $true

} catch {
    $results.Passed = $false
    $results.Errors += $_.Exception.Message
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Prerequisites Check: FAILED"
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""
}

# Output JSON for pipeline consumption
$jsonOutput = $results | ConvertTo-Json -Depth 10
Write-Host "JSON Output:" -ForegroundColor Cyan
Write-Host $jsonOutput
Write-Host ""

# Write to file if in pipeline
if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "prerequisites-check.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding UTF8
    Write-Host "✓ Results written to: $outputPath" -ForegroundColor Green
}

# Exit with appropriate code
if (-not $results.Passed) {
    exit 1
}

exit 0
