#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Configures SQL Server Service Broker for Hartonomous OODA loop autonomy.

.DESCRIPTION
    Idempotent Service Broker setup for Azure Arc SQL Server.
    - Enables Service Broker on database (ALTER DATABASE SET ENABLE_BROKER)
    - Creates message types, contracts, queues, and services for autonomous messaging
    - Verifies Service Broker infrastructure via sys.service_queues and sys.services
    - Returns JSON with Service Broker status for Azure DevOps pipeline

.PARAMETER ServerName
    SQL Server instance name

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER SetupScriptPath
    Path to setup-service-broker.sql script (optional - uses inline setup if not provided)

.PARAMETER SqlUser
    SQL authentication username (optional)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.EXAMPLE
    .\06-service-broker.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [string]$SetupScriptPath,
    
    [Parameter(Mandatory=$false)]
    [string]$SqlUser,
    
    [Parameter(Mandatory=$false)]
    [SecureString]$SqlPassword
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$result = @{
    Step = "ServiceBrokerSetup"
    Success = $false
    ServiceBrokerEnabled = $false
    BrokerInstanceId = ""
    MessageTypes = @()
    Contracts = @()
    Queues = @()
    Services = @()
    Errors = @()
    Warnings = @()
    Timestamp = (Get-Date -Format "o")
}

function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$Database = "master",
        [switch]$IgnoreErrors
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
    if (-not $IgnoreErrors -and $LASTEXITCODE -ne 0) {
        throw "SQL command failed: $output"
    }
    return ($output | Out-String).Trim()
}

try {
    Write-Host "=== Service Broker Setup Script ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray
    
    # Check if Service Broker is enabled
    Write-Host "`nChecking Service Broker status..." -NoNewline
    $brokerQuery = @"
SELECT 
    is_broker_enabled,
    CAST(service_broker_guid AS VARCHAR(36)) AS broker_instance_id
FROM sys.databases 
WHERE name = '$DatabaseName'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
    $brokerJson = Invoke-SqlCommand -Query $brokerQuery -Database "master"
    $brokerInfo = $brokerJson | ConvertFrom-Json
    $result.ServiceBrokerEnabled = $brokerInfo.is_broker_enabled
    $result.BrokerInstanceId = $brokerInfo.broker_instance_id
    
    if (-not $result.ServiceBrokerEnabled) {
        Write-Host " Disabled" -ForegroundColor Yellow
        Write-Host "Enabling Service Broker..." -NoNewline
        
        # Enable Service Broker (may require exclusive database access)
        $enableBrokerSql = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$DatabaseName' AND is_broker_enabled = 0)
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ALTER DATABASE [$DatabaseName] SET ENABLE_BROKER;
    ALTER DATABASE [$DatabaseName] SET MULTI_USER;
END
"@
        try {
            Invoke-SqlCommand -Query $enableBrokerSql -Database "master"
            $result.ServiceBrokerEnabled = $true
            Write-Host " Enabled" -ForegroundColor Green
        }
        catch {
            Write-Host " FAILED" -ForegroundColor Red
            $result.Warnings += "Could not enable Service Broker - database may be in use. Error: $($_.Exception.Message)"
            # Continue with setup anyway - may already be configured
        }
    }
    else {
        Write-Host " Enabled" -ForegroundColor Green
        Write-Host "  Broker Instance ID: $($result.BrokerInstanceId)" -ForegroundColor Gray
    }
    
    # Apply setup script if provided
    if ($SetupScriptPath -and (Test-Path $SetupScriptPath)) {
        Write-Host "Applying setup script..." -NoNewline
        
        $sqlArgs = @("-S", $ServerName, "-d", $DatabaseName, "-C", "-b", "-i", $SetupScriptPath)
        
        if ($SqlUser) {
            $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
                [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
            )
            $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
        } else {
            $sqlArgs += "-E"
        }
        
        $scriptOutput = & sqlcmd @sqlArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Setup script failed: $scriptOutput"
        }
        Write-Host " Done" -ForegroundColor Green
    }
    else {
        # Create basic Service Broker infrastructure inline
        Write-Host "Creating Service Broker infrastructure..." -NoNewline
        
        $setupSql = @"
USE [$DatabaseName];

-- Create message types for OODA loop (Observe, Orient, Decide, Act)
IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'ObservationMessage')
    CREATE MESSAGE TYPE [ObservationMessage] VALIDATION = WELL_FORMED_XML;

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'OrientationMessage')
    CREATE MESSAGE TYPE [OrientationMessage] VALIDATION = WELL_FORMED_XML;

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'DecisionMessage')
    CREATE MESSAGE TYPE [DecisionMessage] VALIDATION = WELL_FORMED_XML;

IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'ActionMessage')
    CREATE MESSAGE TYPE [ActionMessage] VALIDATION = WELL_FORMED_XML;

-- Create contract for OODA messaging
IF NOT EXISTS (SELECT 1 FROM sys.service_contracts WHERE name = 'OODAContract')
    CREATE CONTRACT [OODAContract] (
        [ObservationMessage] SENT BY INITIATOR,
        [OrientationMessage] SENT BY TARGET,
        [DecisionMessage] SENT BY TARGET,
        [ActionMessage] SENT BY TARGET
    );

-- Create queues for each OODA phase
IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'ObservationQueue')
    CREATE QUEUE [ObservationQueue] WITH STATUS = ON, RETENTION = OFF;

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'OrientationQueue')
    CREATE QUEUE [OrientationQueue] WITH STATUS = ON, RETENTION = OFF;

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'DecisionQueue')
    CREATE QUEUE [DecisionQueue] WITH STATUS = ON, RETENTION = OFF;

IF NOT EXISTS (SELECT 1 FROM sys.service_queues WHERE name = 'ActionQueue')
    CREATE QUEUE [ActionQueue] WITH STATUS = ON, RETENTION = OFF;

-- Create services bound to queues
IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'ObservationService')
    CREATE SERVICE [ObservationService] ON QUEUE [ObservationQueue] ([OODAContract]);

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'OrientationService')
    CREATE SERVICE [OrientationService] ON QUEUE [OrientationQueue] ([OODAContract]);

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'DecisionService')
    CREATE SERVICE [DecisionService] ON QUEUE [DecisionQueue] ([OODAContract]);

IF NOT EXISTS (SELECT 1 FROM sys.services WHERE name = 'ActionService')
    CREATE SERVICE [ActionService] ON QUEUE [ActionQueue] ([OODAContract]);
"@
        
        Invoke-SqlCommand -Query $setupSql -Database $DatabaseName
        Write-Host " Done" -ForegroundColor Green
    }
    
    # Verify message types
    Write-Host "Verifying message types..." -NoNewline
    $messageTypesQuery = "SELECT name FROM sys.service_message_types WHERE name LIKE '%Message' FOR JSON PATH"
    $messageTypesJson = Invoke-SqlCommand -Query $messageTypesQuery -Database $DatabaseName
    if ($messageTypesJson -and $messageTypesJson -ne "[]") {
        $messageTypes = $messageTypesJson | ConvertFrom-Json
        $result.MessageTypes = $messageTypes | ForEach-Object { $_.name }
        Write-Host " $($messageTypes.Count) types" -ForegroundColor Green
    }
    else {
        Write-Host " None" -ForegroundColor Yellow
    }
    
    # Verify contracts
    Write-Host "Verifying contracts..." -NoNewline
    $contractsQuery = "SELECT name FROM sys.service_contracts WHERE name NOT IN ('DEFAULT') FOR JSON PATH"
    $contractsJson = Invoke-SqlCommand -Query $contractsQuery -Database $DatabaseName
    if ($contractsJson -and $contractsJson -ne "[]") {
        $contracts = $contractsJson | ConvertFrom-Json
        $result.Contracts = $contracts | ForEach-Object { $_.name }
        Write-Host " $($contracts.Count) contracts" -ForegroundColor Green
    }
    else {
        Write-Host " None" -ForegroundColor Yellow
    }
    
    # Verify queues
    Write-Host "Verifying queues..." -NoNewline
    $queuesQuery = @"
SELECT 
    name,
    is_receive_enabled,
    is_enqueue_enabled
FROM sys.service_queues 
WHERE name NOT LIKE 'sys%'
FOR JSON PATH
"@
    $queuesJson = Invoke-SqlCommand -Query $queuesQuery -Database $DatabaseName
    if ($queuesJson -and $queuesJson -ne "[]") {
        $queues = $queuesJson | ConvertFrom-Json
        $result.Queues = $queues | ForEach-Object { 
            @{
                Name = $_.name
                ReceiveEnabled = $_.is_receive_enabled
                EnqueueEnabled = $_.is_enqueue_enabled
            }
        }
        Write-Host " $($queues.Count) queues" -ForegroundColor Green
        foreach ($queue in $queues) {
            $statusIcon = if ($queue.is_receive_enabled -and $queue.is_enqueue_enabled) { "✓" } else { "!" }
            Write-Host "  $statusIcon $($queue.name)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host " None" -ForegroundColor Yellow
    }
    
    # Verify services
    Write-Host "Verifying services..." -NoNewline
    $servicesQuery = "SELECT name FROM sys.services WHERE name NOT IN ('DEFAULT') FOR JSON PATH"
    $servicesJson = Invoke-SqlCommand -Query $servicesQuery -Database $DatabaseName
    if ($servicesJson -and $servicesJson -ne "[]") {
        $services = $servicesJson | ConvertFrom-Json
        $result.Services = $services | ForEach-Object { $_.name }
        Write-Host " $($services.Count) services" -ForegroundColor Green
        foreach ($service in $services) {
            Write-Host "  - $($service.name)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host " None" -ForegroundColor Yellow
    }
    
    $result.Success = $true
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
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "service-broker.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
