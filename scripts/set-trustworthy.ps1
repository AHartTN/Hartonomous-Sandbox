#Requires -Version 7.0
param(
    [Parameter()]
    [string]$Server,
    
    [Parameter()]
    [string]$Database,
    
    [Parameter()]
    [switch]$UseAzureAD,
    
    [Parameter()]
    [string]$AccessToken
)

$ErrorActionPreference = 'Stop'

# Load local dev config
$scriptRoot = $PSScriptRoot
$localConfig = & (Join-Path $scriptRoot "local-dev-config.ps1")

# Use config defaults if not specified
if (-not $Server) { $Server = $localConfig.SqlServer }
if (-not $Database) { $Database = $localConfig.Database }

$sql = "USE master; ALTER DATABASE [$Database] SET TRUSTWORTHY ON; PRINT 'TRUSTWORTHY enabled for $Database';"

if ($UseAzureAD -and $AccessToken) {
    Write-Host "Using Azure AD authentication"
    Invoke-Sqlcmd -ServerInstance $Server -Database master -Query $sql -AccessToken $AccessToken -TrustServerCertificate
} else {
    Write-Host "Using Windows integrated authentication"
    Invoke-Sqlcmd -ServerInstance $Server -Database master -Query $sql -TrustServerCertificate
}

Write-Host "✓ TRUSTWORTHY enabled"
