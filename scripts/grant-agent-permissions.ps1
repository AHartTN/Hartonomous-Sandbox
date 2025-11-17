#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
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

$sql = @"
USE [master];
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
EXEC sp_configure 'clr strict security', 0; RECONFIGURE;
PRINT 'CLR integration enabled';
"@

$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
$sql | Out-File -FilePath $tempFile -Encoding utf8

if ($UseAzureAD -and $AccessToken) {
    Write-Host "Using Azure AD service principal authentication"
    sqlcmd -S $Server -d master -G -P $AccessToken -i $tempFile -C
} else {
    Write-Host "Using Windows integrated authentication"
    sqlcmd -S $Server -d master -E -C -i $tempFile
}

Remove-Item $tempFile -ErrorAction SilentlyContinue
Write-Host "✓ Agent permissions granted"
