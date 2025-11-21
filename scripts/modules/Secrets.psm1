<#
.SYNOPSIS
    Secrets management module for Hartonomous deployments.

.DESCRIPTION
    Provides secure secrets retrieval from:
    - Azure Key Vault (production)
    - Environment variables (CI/CD)
    - Local development fallbacks

    Supports both managed identity and service principal authentication.

.NOTES
    Author: Hartonomous DevOps Team
    Version: 1.0.0
#>

#Requires -Version 7.0

# Module state
$script:KeyVaultCache = @{}
$script:KeyVaultConfig = $null

<#
.SYNOPSIS
    Initializes the secrets module with Key Vault configuration.

.PARAMETER VaultUri
    Azure Key Vault URI (e.g., https://kv-hartonomous-prod.vault.azure.net/)

.PARAMETER UseManagedIdentity
    Use Azure managed identity for authentication (recommended for production)

.EXAMPLE
    Initialize-SecretsModule -VaultUri "https://kv-hartonomous-prod.vault.azure.net/" -UseManagedIdentity
#>
function Initialize-SecretsModule {
    [CmdletBinding()]
    param(
        [string]$VaultUri,
        [switch]$UseManagedIdentity
    )

    $script:KeyVaultConfig = @{
        VaultUri = $VaultUri
        UseManagedIdentity = $UseManagedIdentity
        VaultName = if ($VaultUri) { Get-VaultNameFromUri -Uri $VaultUri } else { $null }
    }

    Write-Verbose "Secrets module initialized"
    Write-Verbose "  Vault URI: $VaultUri"
    Write-Verbose "  Use Managed Identity: $UseManagedIdentity"
}

<#
.SYNOPSIS
    Retrieves a secret from Azure Key Vault or environment variables.

.PARAMETER SecretName
    Name of the secret to retrieve

.PARAMETER VaultName
    Optional Key Vault name override

.PARAMETER Default
    Default value if secret not found

.PARAMETER Required
    Throw error if secret not found

.OUTPUTS
    String: Secret value

.EXAMPLE
    $password = Get-Secret -SecretName "Neo4jPassword" -Required
#>
function Get-Secret {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SecretName,

        [string]$VaultName,
        [string]$Default,
        [switch]$Required
    )

    # Check cache first
    if ($script:KeyVaultCache.ContainsKey($SecretName)) {
        Write-Verbose "Secret '$SecretName' retrieved from cache"
        return $script:KeyVaultCache[$SecretName]
    }

    # Determine vault name
    if (-not $VaultName) {
        $VaultName = $script:KeyVaultConfig.VaultName
    }

    # Try Azure Key Vault if configured
    if ($VaultName) {
        $secretValue = Get-KeyVaultSecret -VaultName $VaultName -SecretName $SecretName
        if ($secretValue) {
            # Cache for reuse
            $script:KeyVaultCache[$SecretName] = $secretValue
            Write-Verbose "Secret '$SecretName' retrieved from Key Vault"
            return $secretValue
        }
    }

    # Try environment variable
    $envValue = [Environment]::GetEnvironmentVariable($SecretName)
    if ($envValue) {
        Write-Verbose "Secret '$SecretName' retrieved from environment variable"
        return $envValue
    }

    # Try with common prefixes
    $prefixes = @('HARTONOMOUS_', 'AZURE_', 'SQL_', 'NEO4J_')
    foreach ($prefix in $prefixes) {
        $prefixedName = "$prefix$SecretName"
        $envValue = [Environment]::GetEnvironmentVariable($prefixedName)
        if ($envValue) {
            Write-Verbose "Secret '$SecretName' retrieved from environment variable '$prefixedName'"
            return $envValue
        }
    }

    # Use default if provided
    if ($Default) {
        Write-Verbose "Secret '$SecretName' not found, using default value"
        return $Default
    }

    # Throw if required
    if ($Required) {
        throw "Required secret '$SecretName' not found in Key Vault or environment variables"
    }

    Write-Verbose "Secret '$SecretName' not found, returning null"
    return $null
}

<#
.SYNOPSIS
    Retrieves a secret from Azure Key Vault using Azure CLI.

.PARAMETER VaultName
    Key Vault name

.PARAMETER SecretName
    Secret name

.OUTPUTS
    String: Secret value or $null if not found

.NOTES
    Requires Azure CLI (az) to be installed and authenticated
#>
function Get-KeyVaultSecret {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$VaultName,

        [Parameter(Mandatory = $true)]
        [string]$SecretName
    )

    # Check if Azure CLI is available
    $azCommand = Get-Command az -ErrorAction SilentlyContinue
    if (-not $azCommand) {
        Write-Verbose "Azure CLI not found, cannot retrieve Key Vault secrets"
        return $null
    }

    try {
        # Try to get the secret
        $secret = az keyvault secret show `
            --vault-name $VaultName `
            --name $SecretName `
            --query value `
            -o tsv 2>$null

        if ($LASTEXITCODE -eq 0 -and $secret) {
            return $secret
        }

        Write-Verbose "Secret '$SecretName' not found in Key Vault '$VaultName'"
        return $null
    }
    catch {
        Write-Verbose "Error retrieving secret from Key Vault: $($_.Exception.Message)"
        return $null
    }
}

<#
.SYNOPSIS
    Extracts Key Vault name from URI.

.PARAMETER Uri
    Key Vault URI (e.g., https://kv-hartonomous-prod.vault.azure.net/)

.OUTPUTS
    String: Key Vault name

.EXAMPLE
    Get-VaultNameFromUri -Uri "https://kv-hartonomous-prod.vault.azure.net/"
    # Returns: "kv-hartonomous-prod"
#>
function Get-VaultNameFromUri {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri
    )

    if ($Uri -match '^https://([^.]+)\.vault\.azure\.net/?$') {
        return $Matches[1]
    }

    throw "Invalid Key Vault URI format: $Uri"
}

<#
.SYNOPSIS
    Resolves ${KeyVault:SecretName} references in a string.

.PARAMETER Value
    String containing Key Vault references

.OUTPUTS
    String: Resolved value

.EXAMPLE
    $resolved = Resolve-SecretReferences -Value "Server=localhost;Password=${KeyVault:SqlPassword}"
    # Returns connection string with actual password
#>
function Resolve-SecretReferences {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [AllowEmptyString()]
        [string]$Value
    )

    process {
        if ([string]::IsNullOrEmpty($Value)) {
            return $Value
        }

        # Pattern: ${KeyVault:SecretName}
        $pattern = '\$\{KeyVault:([^}]+)\}'
        $matches = [regex]::Matches($Value, $pattern)

        if ($matches.Count -eq 0) {
            return $Value
        }

        $resolved = $Value

        foreach ($match in $matches) {
            $secretName = $match.Groups[1].Value
            $secretValue = Get-Secret -SecretName $secretName -Required

            # Replace the reference with the actual value
            $resolved = $resolved -replace [regex]::Escape($match.Value), $secretValue
        }

        return $resolved
    }
}

<#
.SYNOPSIS
    Resolves Key Vault references in an object (recursive).

.PARAMETER Object
    Object (hashtable or PSCustomObject) containing Key Vault references

.OUTPUTS
    Object with resolved values

.EXAMPLE
    $config = @{
        ConnectionStrings = @{
            Database = "Server=localhost;Password=${KeyVault:SqlPassword}"
        }
    }
    $resolved = Resolve-SecretReferencesInObject -Object $config
#>
function Resolve-SecretReferencesInObject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $Object
    )

    if ($null -eq $Object) {
        return $null
    }

    # Handle hashtables
    if ($Object -is [hashtable]) {
        $resolved = @{}
        foreach ($key in $Object.Keys) {
            $value = $Object[$key]

            if ($value -is [string]) {
                $resolved[$key] = Resolve-SecretReferences -Value $value
            }
            elseif ($value -is [hashtable] -or $value -is [PSCustomObject]) {
                $resolved[$key] = Resolve-SecretReferencesInObject -Object $value
            }
            else {
                $resolved[$key] = $value
            }
        }
        return $resolved
    }

    # Handle PSCustomObjects
    if ($Object -is [PSCustomObject]) {
        $resolved = [PSCustomObject]@{}
        foreach ($property in $Object.PSObject.Properties) {
            $value = $property.Value

            if ($value -is [string]) {
                $resolved | Add-Member -MemberType NoteProperty -Name $property.Name -Value (Resolve-SecretReferences -Value $value)
            }
            elseif ($value -is [hashtable] -or $value -is [PSCustomObject]) {
                $resolved | Add-Member -MemberType NoteProperty -Name $property.Name -Value (Resolve-SecretReferencesInObject -Object $value)
            }
            else {
                $resolved | Add-Member -MemberType NoteProperty -Name $property.Name -Value $value
            }
        }
        return $resolved
    }

    # For other types, return as-is
    return $Object
}

<#
.SYNOPSIS
    Tests Azure CLI authentication and Key Vault access.

.PARAMETER VaultName
    Key Vault name to test

.OUTPUTS
    Boolean: True if authenticated and can access vault

.EXAMPLE
    if (Test-KeyVaultAccess -VaultName "kv-hartonomous-prod") {
        Write-Host "Key Vault access confirmed"
    }
#>
function Test-KeyVaultAccess {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [string]$VaultName
    )

    if (-not $VaultName) {
        $VaultName = $script:KeyVaultConfig.VaultName
    }

    if (-not $VaultName) {
        Write-Verbose "No Key Vault name provided"
        return $false
    }

    # Check if Azure CLI is available
    $azCommand = Get-Command az -ErrorAction SilentlyContinue
    if (-not $azCommand) {
        Write-Verbose "Azure CLI not installed"
        return $false
    }

    try {
        # Try to list secrets (doesn't require reading values)
        $result = az keyvault secret list `
            --vault-name $VaultName `
            --query "[0].id" `
            -o tsv 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Verbose "Key Vault access confirmed: $VaultName"
            return $true
        }

        Write-Verbose "Cannot access Key Vault: $VaultName"
        return $false
    }
    catch {
        Write-Verbose "Error testing Key Vault access: $($_.Exception.Message)"
        return $false
    }
}

<#
.SYNOPSIS
    Clears the secrets cache.

.EXAMPLE
    Clear-SecretsCache
#>
function Clear-SecretsCache {
    [CmdletBinding()]
    param()

    $script:KeyVaultCache = @{}
    Write-Verbose "Secrets cache cleared"
}

# Export module members
Export-ModuleMember -Function @(
    'Initialize-SecretsModule',
    'Get-Secret',
    'Resolve-SecretReferences',
    'Resolve-SecretReferencesInObject',
    'Test-KeyVaultAccess',
    'Clear-SecretsCache'
)
