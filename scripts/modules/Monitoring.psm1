<#
.SYNOPSIS
    Monitoring and observability module for Hartonomous deployments.

.DESCRIPTION
    Integrates with:
    - Azure CLI (Application Insights, Azure Monitor)
    - GitHub CLI (workflow status, run logs)
    - Deployment telemetry

.NOTES
    Author: Hartonomous DevOps Team
    Version: 1.0.0
#>

#Requires -Version 7.0

# Import dependencies
Import-Module "$PSScriptRoot\Logger.psm1" -Force
Import-Module "$PSScriptRoot\Environment.psm1" -Force

<#
.SYNOPSIS
    Records deployment telemetry event.

.PARAMETER EventName
    Name of the event

.PARAMETER Properties
    Custom properties (hashtable)

.PARAMETER Metrics
    Custom metrics (hashtable)

.PARAMETER Config
    Deployment configuration

.EXAMPLE
    Send-DeploymentEvent -EventName "DeploymentStarted" -Properties @{
        Environment = "Production"
        Component = "Database"
    }
#>
function Send-DeploymentEvent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$EventName,

        [hashtable]$Properties = @{},
        [hashtable]$Metrics = @{},
        [PSCustomObject]$Config
    )

    # Add standard properties
    $envInfo = Get-EnvironmentInfo
    $Properties["environment"] = $envInfo.Environment
    $Properties["platform"] = $envInfo.Platform
    $Properties["user"] = $envInfo.User
    $Properties["machine"] = $envInfo.Machine

    if ($envInfo.BuildId) {
        $Properties["buildId"] = $envInfo.BuildId
    }

    # Log locally
    Write-Log "Event: $EventName" -Level Debug

    # Send to Application Insights (if configured)
    if ($Config -and $Config.monitoring -and $Config.monitoring.enabled) {
        Send-AppInsightsTelemetry -EventName $EventName -Properties $Properties -Metrics $Metrics -Config $Config
    }
}

<#
.SYNOPSIS
    Sends telemetry to Application Insights.

.PARAMETER EventName
    Event name

.PARAMETER Properties
    Event properties

.PARAMETER Metrics
    Event metrics

.PARAMETER Config
    Deployment configuration

.NOTES
    Requires Azure CLI and Application Insights configuration
#>
function Send-AppInsightsTelemetry {
    [CmdletBinding()]
    param(
        [string]$EventName,
        [hashtable]$Properties,
        [hashtable]$Metrics,
        [PSCustomObject]$Config
    )

    # Check if Azure CLI is available
    if (-not (Test-CommandExists -CommandName "az")) {
        Write-Verbose "Azure CLI not available, skipping telemetry"
        return
    }

    # Check if App Insights is configured
    if (-not $Config.monitoring.appInsightsName) {
        Write-Verbose "Application Insights not configured"
        return
    }

    # TODO: Implement Application Insights custom events API
    # For now, this is a placeholder
    Write-Verbose "Telemetry: $EventName"
}

<#
.SYNOPSIS
    Gets recent GitHub Actions workflow runs.

.PARAMETER WorkflowName
    Name of the workflow (defaults to "CI/CD Pipeline")

.PARAMETER Limit
    Number of runs to retrieve

.OUTPUTS
    Array of workflow run objects

.EXAMPLE
    $runs = Get-GitHubWorkflowRuns -Limit 5
#>
function Get-GitHubWorkflowRuns {
    [CmdletBinding()]
    param(
        [string]$WorkflowName = "CI/CD Pipeline",
        [int]$Limit = 10
    )

    # Check if GitHub CLI is available
    if (-not (Test-CommandExists -CommandName "gh")) {
        Write-Warning "GitHub CLI not installed"
        return @()
    }

    try {
        $runs = gh run list `
            --workflow "$WorkflowName" `
            --limit $Limit `
            --json status,conclusion,createdAt,headBranch,displayTitle,databaseId `
            | ConvertFrom-Json

        return $runs
    }
    catch {
        Write-Warning "Failed to get GitHub workflow runs: $($_.Exception.Message)"
        return @()
    }
}

<#
.SYNOPSIS
    Shows GitHub Actions workflow status.

.PARAMETER WorkflowName
    Name of the workflow

.EXAMPLE
    Show-GitHubWorkflowStatus
#>
function Show-GitHubWorkflowStatus {
    [CmdletBinding()]
    param(
        [string]$WorkflowName = "CI/CD Pipeline"
    )

    $runs = Get-GitHubWorkflowRuns -WorkflowName $WorkflowName -Limit 5

    if ($runs.Count -eq 0) {
        Write-Log "No workflow runs found" -Level Warning
        return
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Recent GitHub Actions Runs: $WorkflowName" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    foreach ($run in $runs) {
        $status = $run.status
        $conclusion = $run.conclusion
        $branch = $run.headBranch
        $title = $run.displayTitle

        $statusIcon = switch ($conclusion) {
            "success" { "✓" }
            "failure" { "✗" }
            "cancelled" { "⊘" }
            default { "•" }
        }

        $statusColor = switch ($conclusion) {
            "success" { "Green" }
            "failure" { "Red" }
            "cancelled" { "Yellow" }
            default { "Gray" }
        }

        Write-Host "  $statusIcon [$status] $title" -ForegroundColor $statusColor
        Write-Host "    Branch: $branch | ID: $($run.databaseId)" -ForegroundColor Gray
    }

    Write-Host ""
}

<#
.SYNOPSIS
    Gets Azure Pipeline runs.

.PARAMETER PipelineId
    Pipeline ID (optional)

.PARAMETER Limit
    Number of runs to retrieve

.OUTPUTS
    Array of pipeline run objects

.EXAMPLE
    $runs = Get-AzurePipelineRuns -Limit 5
#>
function Get-AzurePipelineRuns {
    [CmdletBinding()]
    param(
        [int]$PipelineId,
        [int]$Limit = 10
    )

    # Check if Azure CLI is available
    if (-not (Test-CommandExists -CommandName "az")) {
        Write-Warning "Azure CLI not installed"
        return @()
    }

    try {
        $args = @("pipelines", "runs", "list", "--top", $Limit)

        if ($PipelineId) {
            $args += "--pipeline-id"
            $args += $PipelineId
        }

        $runs = & az @args | ConvertFrom-Json

        return $runs
    }
    catch {
        Write-Warning "Failed to get Azure Pipeline runs: $($_.Exception.Message)"
        return @()
    }
}

<#
.SYNOPSIS
    Shows Azure Pipeline status.

.PARAMETER PipelineId
    Pipeline ID

.EXAMPLE
    Show-AzurePipelineStatus
#>
function Show-AzurePipelineStatus {
    [CmdletBinding()]
    param(
        [int]$PipelineId
    )

    $runs = Get-AzurePipelineRuns -PipelineId $PipelineId -Limit 5

    if ($runs.Count -eq 0) {
        Write-Log "No pipeline runs found" -Level Warning
        return
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Recent Azure Pipeline Runs" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    foreach ($run in $runs) {
        $status = $run.status
        $result = $run.result
        $branch = $run.sourceBranch

        $statusIcon = switch ($result) {
            "succeeded" { "✓" }
            "failed" { "✗" }
            "canceled" { "⊘" }
            default { "•" }
        }

        $statusColor = switch ($result) {
            "succeeded" { "Green" }
            "failed" { "Red" }
            "canceled" { "Yellow" }
            default { "Gray" }
        }

        Write-Host "  $statusIcon [$status] Run #$($run.id)" -ForegroundColor $statusColor
        Write-Host "    Branch: $branch" -ForegroundColor Gray
    }

    Write-Host ""
}

<#
.SYNOPSIS
    Checks deployment health across all environments.

.PARAMETER Config
    Deployment configuration

.EXAMPLE
    Test-DeploymentHealth -Config $config
#>
function Test-DeploymentHealth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Deployment Health Check" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    # Check CI/CD pipeline status
    if (Test-IsGitHubActions) {
        Show-GitHubWorkflowStatus
    }
    elseif (Test-IsAzurePipelines) {
        Show-AzurePipelineStatus
    }

    # Check application endpoints (if configured)
    if ($Config.application -and $Config.application.healthCheckUrl) {
        Test-ApplicationEndpoint -Url $Config.application.healthCheckUrl
    }

    # Check database connectivity
    if ($Config.database) {
        Test-DatabaseConnectivity -Config $Config
    }
}

<#
.SYNOPSIS
    Tests an application endpoint.

.PARAMETER Url
    Endpoint URL

.OUTPUTS
    Boolean
#>
function Test-ApplicationEndpoint {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    Write-Log "Testing application endpoint: $Url" -Level Info

    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop

        if ($response.StatusCode -eq 200) {
            Write-Log "  ✓ Application endpoint healthy" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Application endpoint returned HTTP $($response.StatusCode)" -Level Error
            return $false
        }
    }
    catch {
        Write-Log "  ✗ Application endpoint error: $($_.Exception.Message)" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
    Tests database connectivity.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-DatabaseConnectivity {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    $server = $Config.database.server
    $database = $Config.database.name

    Write-Log "Testing database connectivity: $server\$database" -Level Info

    try {
        $query = "SELECT 1"
        $result = sqlcmd -S $server -d $database -Q $query -h -1 -C 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Log "  ✓ Database connectivity healthy" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Database connectivity failed" -Level Error
            return $false
        }
    }
    catch {
        Write-Log "  ✗ Database connectivity error: $($_.Exception.Message)" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
    Helper function to test if a command exists.

.PARAMETER CommandName
    Command name to test

.OUTPUTS
    Boolean
#>
function Test-CommandExists {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [string]$CommandName
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    return $null -ne $command
}

# Export module members
Export-ModuleMember -Function @(
    'Send-DeploymentEvent',
    'Get-GitHubWorkflowRuns',
    'Show-GitHubWorkflowStatus',
    'Get-AzurePipelineRuns',
    'Show-AzurePipelineStatus',
    'Test-DeploymentHealth',
    'Test-ApplicationEndpoint',
    'Test-DatabaseConnectivity'
)
