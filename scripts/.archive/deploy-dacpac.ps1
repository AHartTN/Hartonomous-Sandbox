#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys a Hartonomous DACPAC to SQL Server.

.DESCRIPTION
    Enterprise-grade DACPAC deployment with support for:
    - Windows integrated authentication (local dev)
    - Azure AD service principal authentication (CI/CD)
    - Pre/Post deployment script execution (CLR assemblies, Service Broker)

.PARAMETER DacpacPath
    Path to the DACPAC file. Defaults to Release build output.

.PARAMETER Server
    SQL Server instance. Defaults to local-dev-config.ps1 value.

.PARAMETER Database
    Target database name. Defaults to local-dev-config.ps1 value.

.PARAMETER DependenciesPath
    Path to CLR assembly dependencies for Post-Deployment scripts.
    Required when IncludePrePostScripts is enabled.

.PARAMETER UseAzureAD
    Use Azure AD authentication with access token.

.PARAMETER AccessToken
    Azure AD access token (required when UseAzureAD is set).

.PARAMETER IgnorePrePostScripts
    Skip Pre/Post deployment scripts. Use this for schema-only updates.

.EXAMPLE
    .\deploy-dacpac.ps1  # Local dev with defaults
    .\deploy-dacpac.ps1 -Server "sql.example.com" -UseAzureAD -AccessToken $token
#>
param(
    [Parameter()]
    [string]$DacpacPath,

    [Parameter()]
    [string]$Server,

    [Parameter()]
    [string]$Database,

    [Parameter()]
    [string]$DependenciesPath,

    [Parameter()]
    [switch]$UseAzureAD,

    [Parameter()]
    [string]$AccessToken,

    [Parameter()]
    [switch]$IgnorePrePostScripts
)

$ErrorActionPreference = 'Stop'

# Load local dev config
$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent
$localConfig = & (Join-Path $scriptRoot "local-dev-config.ps1")

# Use config defaults if not specified
if (-not $Server) { $Server = $localConfig.SqlServer }
if (-not $Database) { $Database = $localConfig.Database }
if (-not $DacpacPath) { 
    # Default to the built DACPAC location
    $DacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"
}

# Find SqlPackage
$sqlPackage = Get-Command sqlpackage -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (-not $sqlPackage) {
    $sqlPackage = "${env:ProgramFiles}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
}
if (-not (Test-Path $sqlPackage)) {
    $sqlPackage = "${env:ProgramFiles(x86)}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
}
if (-not (Test-Path $sqlPackage)) {
    throw "SqlPackage.exe not found. Install SQL Server Data Tools or DacFx."
}

Write-Host "Using SqlPackage: $sqlPackage"
Write-Host "Deploying DACPAC: $DacpacPath"
Write-Host "This DACPAC includes Hartonomous.Clr.dll assembly embedded as hex binary"

# Build SqlPackage arguments
$baseArgs = @(
    "/Action:Publish",
    "/SourceFile:$DacpacPath",
    "/p:DropObjectsNotInSource=False",
    "/p:DropConstraintsNotInSource=True",
    "/p:BlockOnPossibleDataLoss=False"  # Allow schema changes during development
)

# Handle Pre/Post deployment scripts
if ($IgnorePrePostScripts) {
    Write-Host "Skipping Pre/Post deployment scripts (schema-only deployment)"
    $baseArgs += "/p:IgnorePreDeployScript=True"
    $baseArgs += "/p:IgnorePostDeployScript=True"
} else {
    Write-Host "Including Pre/Post deployment scripts"
    # DependenciesPath required for Post-Deployment CLR assembly deployment
    if (-not $DependenciesPath) {
        $DependenciesPath = Join-Path $repoRoot "dependencies"
    }
    if (-not (Test-Path $DependenciesPath)) {
        Write-Warning "DependenciesPath not found: $DependenciesPath"
        Write-Warning "CLR assembly dependencies may fail to deploy."
    } else {
        Write-Host "CLR Dependencies Path: $DependenciesPath"
        $baseArgs += "/v:DependenciesPath=$DependenciesPath"
    }
    $baseArgs += "/v:DatabaseName=$Database"
}

if ($UseAzureAD -and $AccessToken) {
    Write-Host "Using Azure AD service principal authentication"
    $baseArgs += "/TargetServerName:$Server"
    $baseArgs += "/TargetDatabaseName:$Database"
    $baseArgs += "/AccessToken:$AccessToken"
} else {
    Write-Host "Using Windows integrated authentication"
    $connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;"
    $baseArgs += "/TargetConnectionString:$connectionString"
}

Write-Host "Executing SqlPackage with $(($baseArgs | Measure-Object).Count) arguments..."
& $sqlPackage @baseArgs

if ($LASTEXITCODE -ne 0) { 
  throw "DACPAC deployment failed" 
}

Write-Host "✓ DACPAC deployed with Hartonomous.Clr assembly"
