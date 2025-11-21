<#
.SYNOPSIS
    Environment detection module for Hartonomous deployments.

.DESCRIPTION
    Automatically detects the current deployment environment and provides
    context about the runtime environment (CI/CD platform, agent type, etc.)

.NOTES
    Author: Hartonomous DevOps Team
    Version: 1.0.0
#>

#Requires -Version 7.0

<#
.SYNOPSIS
    Detects the current deployment environment.

.DESCRIPTION
    Determines the environment based on:
    1. Explicit HARTONOMOUS_ENVIRONMENT variable (highest priority)
    2. CI/CD platform environment variables
    3. Git branch name
    4. Defaults to "Local" if no indicators found

.OUTPUTS
    String: "Local", "Development", "Staging", or "Production"

.EXAMPLE
    $env = Get-DeploymentEnvironment
    # Returns: "Production"
#>
function Get-DeploymentEnvironment {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    # Priority 1: Explicit environment variable
    if ($env:HARTONOMOUS_ENVIRONMENT) {
        Write-Verbose "Environment detected from HARTONOMOUS_ENVIRONMENT: $env:HARTONOMOUS_ENVIRONMENT"
        return $env:HARTONOMOUS_ENVIRONMENT
    }

    # Priority 2: GitHub Actions
    if (Test-IsGitHubActions) {
        # Check for GitHub environment
        if ($env:GITHUB_ENVIRONMENT) {
            Write-Verbose "Environment detected from GITHUB_ENVIRONMENT: $env:GITHUB_ENVIRONMENT"
            return $env:GITHUB_ENVIRONMENT
        }

        # Infer from branch name
        $branch = $env:GITHUB_REF_NAME
        if ($branch) {
            $inferredEnv = Get-EnvironmentFromBranch -BranchName $branch
            Write-Verbose "Environment inferred from GitHub branch '$branch': $inferredEnv"
            return $inferredEnv
        }
    }

    # Priority 3: Azure Pipelines
    if (Test-IsAzurePipelines) {
        # Check for release environment name
        if ($env:RELEASE_ENVIRONMENTNAME) {
            Write-Verbose "Environment detected from RELEASE_ENVIRONMENTNAME: $env:RELEASE_ENVIRONMENTNAME"
            return $env:RELEASE_ENVIRONMENTNAME
        }

        # Infer from branch name
        $branch = $env:BUILD_SOURCEBRANCHNAME
        if ($branch) {
            $inferredEnv = Get-EnvironmentFromBranch -BranchName $branch
            Write-Verbose "Environment inferred from Azure Pipelines branch '$branch': $inferredEnv"
            return $inferredEnv
        }
    }

    # Priority 4: Git branch (local with git)
    if (Test-Path ".git") {
        try {
            $branch = git rev-parse --abbrev-ref HEAD 2>$null
            if ($branch) {
                $inferredEnv = Get-EnvironmentFromBranch -BranchName $branch
                Write-Verbose "Environment inferred from git branch '$branch': $inferredEnv"
                return $inferredEnv
            }
        }
        catch {
            # Git not available or not in a repo
        }
    }

    # Default: Local development
    Write-Verbose "No environment indicators found, defaulting to Local"
    return "Local"
}

<#
.SYNOPSIS
    Infers environment from git branch name.

.PARAMETER BranchName
    The git branch name

.OUTPUTS
    String: Environment name

.EXAMPLE
    Get-EnvironmentFromBranch -BranchName "main"
    # Returns: "Production"
#>
function Get-EnvironmentFromBranch {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BranchName
    )

    switch -Regex ($BranchName) {
        '^main$|^master$' { return 'Production' }
        '^staging$|^stage$' { return 'Staging' }
        '^develop$|^dev$|^development$' { return 'Development' }
        '^release/.*' { return 'Staging' }
        '^hotfix/.*' { return 'Production' }
        default { return 'Development' }
    }
}

<#
.SYNOPSIS
    Checks if running in GitHub Actions.

.OUTPUTS
    Boolean

.EXAMPLE
    if (Test-IsGitHubActions) {
        Write-Host "Running in GitHub Actions"
    }
#>
function Test-IsGitHubActions {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    return $null -ne $env:GITHUB_WORKSPACE
}

<#
.SYNOPSIS
    Checks if running in Azure Pipelines.

.OUTPUTS
    Boolean

.EXAMPLE
    if (Test-IsAzurePipelines) {
        Write-Host "Running in Azure Pipelines"
    }
#>
function Test-IsAzurePipelines {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    return $null -ne $env:BUILD_BUILDID
}

<#
.SYNOPSIS
    Checks if running in local development environment.

.OUTPUTS
    Boolean

.EXAMPLE
    if (Test-IsLocal) {
        Write-Host "Running locally"
    }
#>
function Test-IsLocal {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    return -not (Test-IsGitHubActions) -and -not (Test-IsAzurePipelines)
}

<#
.SYNOPSIS
    Gets comprehensive environment information.

.OUTPUTS
    Hashtable with environment details

.EXAMPLE
    $envInfo = Get-EnvironmentInfo
    Write-Host "Environment: $($envInfo.Environment)"
    Write-Host "Platform: $($envInfo.Platform)"
#>
function Get-EnvironmentInfo {
    [CmdletBinding()]
    [OutputType([hashtable])]
    param()

    $info = @{
        Environment = Get-DeploymentEnvironment
        IsLocal = Test-IsLocal
        IsGitHubActions = Test-IsGitHubActions
        IsAzurePipelines = Test-IsAzurePipelines
        Platform = $null
        Runner = $null
        BuildId = $null
        BuildNumber = $null
        Repository = $null
        Branch = $null
        Commit = $null
        User = $env:USERNAME
        Machine = $env:COMPUTERNAME
        OS = $PSVersionTable.OS
        PSVersion = $PSVersionTable.PSVersion.ToString()
    }

    # Platform-specific details
    if (Test-IsGitHubActions) {
        $info.Platform = 'GitHub Actions'
        $info.Runner = $env:RUNNER_NAME
        $info.BuildId = $env:GITHUB_RUN_ID
        $info.BuildNumber = $env:GITHUB_RUN_NUMBER
        $info.Repository = $env:GITHUB_REPOSITORY
        $info.Branch = $env:GITHUB_REF_NAME
        $info.Commit = $env:GITHUB_SHA
    }
    elseif (Test-IsAzurePipelines) {
        $info.Platform = 'Azure Pipelines'
        $info.Runner = $env:AGENT_NAME
        $info.BuildId = $env:BUILD_BUILDID
        $info.BuildNumber = $env:BUILD_BUILDNUMBER
        $info.Repository = $env:BUILD_REPOSITORY_NAME
        $info.Branch = $env:BUILD_SOURCEBRANCHNAME
        $info.Commit = $env:BUILD_SOURCEVERSION
    }
    else {
        $info.Platform = 'Local Development'
        $info.Runner = $env:COMPUTERNAME

        # Try to get git info if available
        if (Test-Path ".git") {
            try {
                $info.Branch = git rev-parse --abbrev-ref HEAD 2>$null
                $info.Commit = git rev-parse HEAD 2>$null
                $info.Repository = git config --get remote.origin.url 2>$null
            }
            catch {
                # Git not available
            }
        }
    }

    return $info
}

<#
.SYNOPSIS
    Displays environment information in a formatted table.

.EXAMPLE
    Show-EnvironmentInfo
#>
function Show-EnvironmentInfo {
    [CmdletBinding()]
    param()

    $info = Get-EnvironmentInfo

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Environment Information" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Environment:  $($info.Environment)" -ForegroundColor White
    Write-Host "  Platform:     $($info.Platform)" -ForegroundColor White
    Write-Host "  Runner:       $($info.Runner)" -ForegroundColor Gray

    if ($info.BuildId) {
        Write-Host "  Build ID:     $($info.BuildId)" -ForegroundColor Gray
    }

    if ($info.Repository) {
        Write-Host "  Repository:   $($info.Repository)" -ForegroundColor Gray
    }

    if ($info.Branch) {
        Write-Host "  Branch:       $($info.Branch)" -ForegroundColor Gray
    }

    if ($info.Commit) {
        $shortCommit = $info.Commit.Substring(0, [Math]::Min(8, $info.Commit.Length))
        Write-Host "  Commit:       $shortCommit" -ForegroundColor Gray
    }

    Write-Host "  User:         $($info.User)" -ForegroundColor Gray
    Write-Host "  Machine:      $($info.Machine)" -ForegroundColor Gray
    Write-Host "  PowerShell:   $($info.PSVersion)" -ForegroundColor Gray
    Write-Host ""
}

<#
.SYNOPSIS
    Validates that the current environment is one of the allowed environments.

.PARAMETER AllowedEnvironments
    Array of allowed environment names

.EXAMPLE
    Assert-AllowedEnvironment -AllowedEnvironments @('Development', 'Staging')
    # Throws if current environment is Production or Local
#>
function Assert-AllowedEnvironment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$AllowedEnvironments
    )

    $currentEnv = Get-DeploymentEnvironment

    if ($currentEnv -notin $AllowedEnvironments) {
        throw "Current environment '$currentEnv' is not allowed. Allowed environments: $($AllowedEnvironments -join ', ')"
    }

    Write-Verbose "Environment validation passed: $currentEnv is allowed"
}

<#
.SYNOPSIS
    Gets the workspace root directory.

.DESCRIPTION
    Returns the repository root directory based on the environment:
    - GitHub Actions: GITHUB_WORKSPACE
    - Azure Pipelines: BUILD_SOURCESDIRECTORY
    - Local: Current directory or git root

.OUTPUTS
    String: Full path to workspace root

.EXAMPLE
    $root = Get-WorkspaceRoot
#>
function Get-WorkspaceRoot {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    # GitHub Actions
    if ($env:GITHUB_WORKSPACE) {
        return $env:GITHUB_WORKSPACE
    }

    # Azure Pipelines
    if ($env:BUILD_SOURCESDIRECTORY) {
        return $env:BUILD_SOURCESDIRECTORY
    }

    # Local development - try to find git root
    if (Test-Path ".git") {
        try {
            $gitRoot = git rev-parse --show-toplevel 2>$null
            if ($gitRoot) {
                return $gitRoot
            }
        }
        catch {
            # Git not available
        }
    }

    # Fallback to current directory
    return (Get-Location).Path
}

# Export module members
Export-ModuleMember -Function @(
    'Get-DeploymentEnvironment',
    'Get-EnvironmentFromBranch',
    'Test-IsGitHubActions',
    'Test-IsAzurePipelines',
    'Test-IsLocal',
    'Get-EnvironmentInfo',
    'Show-EnvironmentInfo',
    'Assert-AllowedEnvironment',
    'Get-WorkspaceRoot'
)
