<#
.SYNOPSIS
    Configuration management module for Hartonomous deployments.

.DESCRIPTION
    Loads and merges configuration from:
    - Base configuration (config.base.json)
    - Environment-specific overrides (config.{environment}.json)
    - Environment variables
    - Azure Key Vault (for secrets)

.NOTES
    Author: Hartonomous DevOps Team
    Version: 1.0.0
#>

#Requires -Version 7.0

# Import dependencies
Import-Module "$PSScriptRoot\Environment.psm1" -Force
Import-Module "$PSScriptRoot\Secrets.psm1" -Force

# Module state
$script:LoadedConfig = $null
$script:ConfigRoot = $null

<#
.SYNOPSIS
    Loads deployment configuration for the current environment.

.PARAMETER Environment
    Override environment detection (Local, Development, Staging, Production)

.PARAMETER ConfigRoot
    Root directory containing config files (defaults to scripts/config)

.OUTPUTS
    PSCustomObject: Merged configuration

.EXAMPLE
    $config = Get-DeploymentConfig
    # Automatically detects environment and loads config

.EXAMPLE
    $config = Get-DeploymentConfig -Environment Production
    # Explicitly load production config
#>
function Get-DeploymentConfig {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [string]$Environment,
        [string]$ConfigRoot
    )

    # Detect environment if not provided
    if (-not $Environment) {
        $Environment = Get-DeploymentEnvironment
    }

    # Determine config root
    if (-not $ConfigRoot) {
        $ConfigRoot = Join-Path (Split-Path $PSScriptRoot -Parent) "config"
    }

    $script:ConfigRoot = $ConfigRoot

    Write-Verbose "Loading configuration for environment: $Environment"
    Write-Verbose "Config root: $ConfigRoot"

    # Load base configuration
    $baseConfigPath = Join-Path $ConfigRoot "config.base.json"
    if (-not (Test-Path $baseConfigPath)) {
        throw "Base configuration not found: $baseConfigPath"
    }

    $baseConfig = Get-Content $baseConfigPath -Raw | ConvertFrom-Json
    Write-Verbose "Loaded base configuration"

    # Load environment-specific configuration
    $envConfigPath = Join-Path $ConfigRoot "config.$($Environment.ToLower()).json"
    if (Test-Path $envConfigPath) {
        $envConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
        Write-Verbose "Loaded environment configuration: $envConfigPath"

        # Deep merge configurations
        $config = Merge-Configuration -Base $baseConfig -Override $envConfig
    }
    else {
        Write-Verbose "No environment-specific config found, using base only"
        $config = $baseConfig
    }

    # Add environment metadata
    $config | Add-Member -MemberType NoteProperty -Name "_environment" -Value $Environment -Force
    $config | Add-Member -MemberType NoteProperty -Name "_configRoot" -Value $ConfigRoot -Force
    $config | Add-Member -MemberType NoteProperty -Name "_loadedAt" -Value (Get-Date) -Force

    # Initialize secrets module if Key Vault configured
    if ($config.keyVault -and $config.keyVault.vaultUri) {
        Initialize-SecretsModule `
            -VaultUri $config.keyVault.vaultUri `
            -UseManagedIdentity:($config.keyVault.useManagedIdentity -eq $true)
    }

    # Resolve Key Vault references
    $config = Resolve-SecretReferencesInObject -Object $config

    # Cache loaded config
    $script:LoadedConfig = $config

    return $config
}

<#
.SYNOPSIS
    Deep merges two configuration objects.

.PARAMETER Base
    Base configuration object

.PARAMETER Override
    Override configuration object

.OUTPUTS
    PSCustomObject: Merged configuration

.NOTES
    Override values take precedence over base values.
    Objects are merged recursively.
#>
function Merge-Configuration {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Base,

        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Override
    )

    # Create a copy of the base
    $merged = $Base | ConvertTo-Json -Depth 100 | ConvertFrom-Json

    # Merge each property from override
    foreach ($property in $Override.PSObject.Properties) {
        $name = $property.Name
        $value = $property.Value

        # If base has this property and both are objects, merge recursively
        if ($merged.PSObject.Properties[$name] -and
            $merged.$name -is [PSCustomObject] -and
            $value -is [PSCustomObject]) {

            $merged.$name = Merge-Configuration -Base $merged.$name -Override $value
        }
        else {
            # Otherwise, override completely
            if ($merged.PSObject.Properties[$name]) {
                $merged.$name = $value
            }
            else {
                $merged | Add-Member -MemberType NoteProperty -Name $name -Value $value
            }
        }
    }

    return $merged
}

<#
.SYNOPSIS
    Gets a configuration value by path.

.PARAMETER Path
    Dot-notation path (e.g., "database.server")

.PARAMETER Config
    Configuration object (uses cached if not provided)

.PARAMETER Default
    Default value if path not found

.OUTPUTS
    Value at the specified path

.EXAMPLE
    $server = Get-ConfigValue -Path "database.server"
    # Returns: "localhost" (from config)

.EXAMPLE
    $timeout = Get-ConfigValue -Path "api.timeout" -Default 30
    # Returns: 30 (if api.timeout not in config)
#>
function Get-ConfigValue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [PSCustomObject]$Config,
        $Default
    )

    if (-not $Config) {
        $Config = $script:LoadedConfig
    }

    if (-not $Config) {
        throw "No configuration loaded. Call Get-DeploymentConfig first."
    }

    # Split path and navigate
    $parts = $Path -split '\.'
    $current = $Config

    foreach ($part in $parts) {
        if ($current.PSObject.Properties[$part]) {
            $current = $current.$part
        }
        else {
            # Path not found, return default
            return $Default
        }
    }

    return $current
}

<#
.SYNOPSIS
    Validates required configuration values exist.

.PARAMETER RequiredPaths
    Array of required configuration paths

.PARAMETER Config
    Configuration object (uses cached if not provided)

.EXAMPLE
    Test-RequiredConfig -RequiredPaths @(
        "database.server",
        "database.name",
        "keyVault.vaultUri"
    )
#>
function Test-RequiredConfig {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$RequiredPaths,

        [PSCustomObject]$Config
    )

    if (-not $Config) {
        $Config = $script:LoadedConfig
    }

    $missing = @()

    foreach ($path in $RequiredPaths) {
        $value = Get-ConfigValue -Path $path -Config $Config
        if ($null -eq $value -or $value -eq "") {
            $missing += $path
        }
    }

    if ($missing.Count -gt 0) {
        throw "Missing required configuration values: $($missing -join ', ')"
    }

    Write-Verbose "All required configuration values present"
    return $true
}

<#
.SYNOPSIS
    Displays the loaded configuration in a readable format.

.PARAMETER Config
    Configuration object (uses cached if not provided)

.PARAMETER MaskSecrets
    Mask secret values in output

.EXAMPLE
    Show-Configuration
#>
function Show-Configuration {
    [CmdletBinding()]
    param(
        [PSCustomObject]$Config,
        [switch]$MaskSecrets
    )

    if (-not $Config) {
        $Config = $script:LoadedConfig
    }

    if (-not $Config) {
        Write-Warning "No configuration loaded"
        return
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Deployment Configuration" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Environment: $($Config._environment)" -ForegroundColor White
    Write-Host "  Loaded At:   $($Config._loadedAt)" -ForegroundColor Gray
    Write-Host ""

    # Display key configuration values
    if ($Config.database) {
        Write-Host "  Database:" -ForegroundColor Yellow
        Write-Host "    Server:   $($Config.database.server)" -ForegroundColor Gray
        Write-Host "    Name:     $($Config.database.name)" -ForegroundColor Gray
        if ($Config.database.authentication) {
            Write-Host "    Auth:     $($Config.database.authentication)" -ForegroundColor Gray
        }
    }

    if ($Config.application) {
        Write-Host ""
        Write-Host "  Application:" -ForegroundColor Yellow
        if ($Config.application.deployTarget) {
            Write-Host "    Target:   $($Config.application.deployTarget)" -ForegroundColor Gray
        }
        if ($Config.application.deployPath) {
            Write-Host "    Path:     $($Config.application.deployPath)" -ForegroundColor Gray
        }
    }

    if ($Config.keyVault) {
        Write-Host ""
        Write-Host "  Key Vault:" -ForegroundColor Yellow
        if ($Config.keyVault.vaultUri) {
            Write-Host "    URI:      $($Config.keyVault.vaultUri)" -ForegroundColor Gray
        }
        Write-Host "    Managed:  $($Config.keyVault.useManagedIdentity)" -ForegroundColor Gray
    }

    if ($Config.monitoring -and $Config.monitoring.enabled) {
        Write-Host ""
        Write-Host "  Monitoring:" -ForegroundColor Yellow
        Write-Host "    Enabled:  $($Config.monitoring.enabled)" -ForegroundColor Gray
    }

    Write-Host ""
}

<#
.SYNOPSIS
    Exports configuration to a file.

.PARAMETER Path
    Output file path

.PARAMETER Config
    Configuration object (uses cached if not provided)

.PARAMETER IncludeSecrets
    Include resolved secrets (not recommended for security)

.EXAMPLE
    Export-Configuration -Path "C:\temp\config-export.json"
#>
function Export-Configuration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [PSCustomObject]$Config,
        [switch]$IncludeSecrets
    )

    if (-not $Config) {
        $Config = $script:LoadedConfig
    }

    if (-not $Config) {
        throw "No configuration loaded"
    }

    if (-not $IncludeSecrets) {
        Write-Warning "Secrets will be exported. Use with caution!"
    }

    $Config | ConvertTo-Json -Depth 100 | Out-File -FilePath $Path -Encoding utf8
    Write-Verbose "Configuration exported to: $Path"
}

<#
.SYNOPSIS
    Gets the current cached configuration.

.OUTPUTS
    PSCustomObject: Currently loaded configuration or $null

.EXAMPLE
    $config = Get-CachedConfig
#>
function Get-CachedConfig {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param()

    return $script:LoadedConfig
}

# Export module members
Export-ModuleMember -Function @(
    'Get-DeploymentConfig',
    'Get-ConfigValue',
    'Test-RequiredConfig',
    'Show-Configuration',
    'Export-Configuration',
    'Get-CachedConfig'
)
