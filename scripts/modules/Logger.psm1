<#
.SYNOPSIS
    Structured logging module for Hartonomous deployments.

.DESCRIPTION
    Provides consistent, structured logging across all deployment scripts with:
    - Color-coded output by severity level
    - Timestamp prefixes
    - Optional file logging
    - Application Insights integration
    - Deployment context tracking

.NOTES
    Author: Hartonomous DevOps Team
    Version: 1.0.0
#>

#Requires -Version 7.0

# Module state
$script:LogLevel = "Info"
$script:LogFile = $null
$script:DeploymentContext = @{
    DeploymentId = [Guid]::NewGuid().ToString()
    StartTime = Get-Date
    Environment = $null
    User = $env:USERNAME
    Machine = $env:COMPUTERNAME
}

# Log levels (in order of severity)
enum LogLevel {
    Debug = 0
    Info = 1
    Success = 2
    Warning = 3
    Error = 4
}

<#
.SYNOPSIS
    Writes a log message with specified level and formatting.

.PARAMETER Message
    The message to log

.PARAMETER Level
    The severity level (Debug, Info, Success, Warning, Error)

.PARAMETER NoTimestamp
    Suppress timestamp prefix

.PARAMETER NoNewline
    Don't add newline after message

.EXAMPLE
    Write-Log "Deployment started" -Level Info
    Write-Log "Operation successful" -Level Success
    Write-Log "Configuration issue detected" -Level Warning
#>
function Write-Log {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [AllowEmptyString()]
        [string]$Message,

        [Parameter(Position = 1)]
        [ValidateSet('Debug', 'Info', 'Success', 'Warning', 'Error')]
        [string]$Level = 'Info',

        [switch]$NoTimestamp,
        [switch]$NoNewline
    )

    # Check if we should log this level
    $currentLevel = [LogLevel]::$script:LogLevel
    $messageLevel = [LogLevel]::$Level

    if ($messageLevel -lt $currentLevel) {
        return  # Skip lower severity messages
    }

    # Build log entry
    $timestamp = if ($NoTimestamp) { "" } else { "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] " }

    $prefix = switch ($Level) {
        'Debug'   { '→' }
        'Info'    { '•' }
        'Success' { '✓' }
        'Warning' { '⚠' }
        'Error'   { '✗' }
    }

    $color = switch ($Level) {
        'Debug'   { 'Gray' }
        'Info'    { 'Cyan' }
        'Success' { 'Green' }
        'Warning' { 'Yellow' }
        'Error'   { 'Red' }
    }

    $logEntry = "$timestamp$prefix $Message"

    # Console output
    if ($NoNewline) {
        Write-Host $logEntry -ForegroundColor $color -NoNewline
    } else {
        Write-Host $logEntry -ForegroundColor $color
    }

    # File logging (if enabled)
    if ($script:LogFile) {
        Add-Content -Path $script:LogFile -Value $logEntry
    }

    # Application Insights telemetry (if configured)
    if ($script:DeploymentContext.AppInsightsKey) {
        Send-Telemetry -Message $Message -Level $Level
    }
}

<#
.SYNOPSIS
    Writes a section header for organization.

.PARAMETER Title
    The section title

.PARAMETER Step
    Optional step number

.PARAMETER TotalSteps
    Optional total number of steps

.EXAMPLE
    Write-LogSection "Database Deployment" -Step 1 -TotalSteps 5
#>
function Write-LogSection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,

        [int]$Step = 0,
        [int]$TotalSteps = 0
    )

    $separator = "═" * 70
    Write-Host ""
    Write-Host $separator -ForegroundColor Cyan

    if ($Step -gt 0 -and $TotalSteps -gt 0) {
        Write-Host " [ STEP $Step/$TotalSteps ] $Title" -ForegroundColor Cyan
    } else {
        Write-Host " $Title" -ForegroundColor Cyan
    }

    Write-Host $separator -ForegroundColor Cyan
    Write-Host ""
}

<#
.SYNOPSIS
    Sets the minimum log level to display.

.PARAMETER Level
    Minimum severity level to log (Debug, Info, Success, Warning, Error)

.EXAMPLE
    Set-LogLevel -Level Debug  # Show all messages
    Set-LogLevel -Level Warning  # Only show warnings and errors
#>
function Set-LogLevel {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('Debug', 'Info', 'Success', 'Warning', 'Error')]
        [string]$Level
    )

    $script:LogLevel = $Level
    Write-Log "Log level set to: $Level" -Level Debug
}

<#
.SYNOPSIS
    Enables logging to a file.

.PARAMETER Path
    Path to the log file

.PARAMETER Append
    Append to existing file instead of overwriting

.EXAMPLE
    Enable-FileLogging -Path "C:\Logs\deployment.log"
#>
function Enable-FileLogging {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [switch]$Append
    )

    # Create directory if it doesn't exist
    $directory = Split-Path -Path $Path -Parent
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    # Create or clear log file
    if (-not $Append -and (Test-Path $Path)) {
        Remove-Item -Path $Path -Force
    }

    $script:LogFile = $Path
    Write-Log "File logging enabled: $Path" -Level Debug
}

<#
.SYNOPSIS
    Sets deployment context information.

.PARAMETER Environment
    Deployment environment (Local, Development, Staging, Production)

.PARAMETER DeploymentId
    Optional custom deployment ID (GUID generated if not provided)

.EXAMPLE
    Set-DeploymentContext -Environment Production
#>
function Set-DeploymentContext {
    [CmdletBinding()]
    param(
        [string]$Environment,
        [string]$DeploymentId,
        [string]$AppInsightsKey
    )

    if ($Environment) {
        $script:DeploymentContext.Environment = $Environment
    }

    if ($DeploymentId) {
        $script:DeploymentContext.DeploymentId = $DeploymentId
    }

    if ($AppInsightsKey) {
        $script:DeploymentContext.AppInsightsKey = $AppInsightsKey
    }
}

<#
.SYNOPSIS
    Gets the current deployment context.

.OUTPUTS
    Hashtable with deployment context information
#>
function Get-DeploymentContext {
    return $script:DeploymentContext
}

<#
.SYNOPSIS
    Measures execution time of a script block and logs the result.

.PARAMETER Name
    Name of the operation being measured

.PARAMETER ScriptBlock
    The code to execute and measure

.EXAMPLE
    Measure-Operation -Name "Database Deployment" -ScriptBlock {
        # Deployment code here
    }
#>
function Measure-Operation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [scriptblock]$ScriptBlock
    )

    Write-Log "Starting: $Name" -Level Info
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        & $ScriptBlock
        $stopwatch.Stop()
        $duration = $stopwatch.Elapsed.ToString("mm\:ss\.fff")
        Write-Log "Completed: $Name (Duration: $duration)" -Level Success
        return $true
    }
    catch {
        $stopwatch.Stop()
        $duration = $stopwatch.Elapsed.ToString("mm\:ss\.fff")
        Write-Log "Failed: $Name (Duration: $duration)" -Level Error
        Write-Log "Error: $($_.Exception.Message)" -Level Error
        throw
    }
}

<#
.SYNOPSIS
    Sends telemetry to Application Insights (if configured).

.PARAMETER Message
    The telemetry message

.PARAMETER Level
    Severity level

.NOTES
    This is a placeholder for Application Insights integration.
    In production, use the Azure CLI or Application Insights SDK.
#>
function Send-Telemetry {
    [CmdletBinding()]
    param(
        [string]$Message,
        [string]$Level
    )

    # Only send telemetry if App Insights is configured
    if (-not $script:DeploymentContext.AppInsightsKey) {
        return
    }

    # TODO: Implement Application Insights telemetry
    # For now, this is a placeholder
    # In production, you would use:
    # - Azure CLI: az monitor app-insights events show
    # - Application Insights SDK
    # - Custom Events API
}

<#
.SYNOPSIS
    Writes a summary table of deployment results.

.PARAMETER Results
    Hashtable of operation names and their results (true/false)

.EXAMPLE
    $results = @{
        "Build DACPAC" = $true
        "Deploy Database" = $true
        "Scaffold Entities" = $false
    }
    Write-DeploymentSummary -Results $results
#>
function Write-DeploymentSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Results
    )

    Write-Host ""
    Write-LogSection "Deployment Summary"

    $passed = ($Results.Values | Where-Object { $_ -eq $true }).Count
    $failed = ($Results.Values | Where-Object { $_ -eq $false }).Count
    $total = $Results.Count

    foreach ($entry in $Results.GetEnumerator() | Sort-Object Name) {
        $status = if ($entry.Value) { '✓' } else { '✗' }
        $color = if ($entry.Value) { 'Green' } else { 'Red' }
        Write-Host "  $status $($entry.Key)" -ForegroundColor $color
    }

    Write-Host ""
    Write-Host "  Total: $total | Passed: $passed | Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { 'Green' } else { 'Yellow' })

    $duration = (Get-Date) - $script:DeploymentContext.StartTime
    Write-Host "  Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Cyan
    Write-Host ""

    return ($failed -eq 0)
}

# Export module members
Export-ModuleMember -Function @(
    'Write-Log',
    'Write-LogSection',
    'Set-LogLevel',
    'Enable-FileLogging',
    'Set-DeploymentContext',
    'Get-DeploymentContext',
    'Measure-Operation',
    'Write-DeploymentSummary'
)
