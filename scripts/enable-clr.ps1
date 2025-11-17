#Requires -Version 7.0
param(
    [Parameter()]
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

if (-not (Get-Module -ListAvailable -Name SqlServer)) {
  Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
}

$sql = @"
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
EXEC sp_configure 'clr strict security', 0; RECONFIGURE;
PRINT 'CLR integration enabled successfully';
"@

$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
$sql | Out-File -FilePath $tempFile -Encoding utf8

if ($UseAzureAD -and $AccessToken) {
    Write-Host "Using Azure AD authentication"
    sqlcmd -S $Server -d master -G -P $AccessToken -i $tempFile -C
} else {
    Write-Host "Using Windows integrated authentication"
    sqlcmd -S $Server -d master -E -C -i $tempFile
}

Remove-Item $tempFile -ErrorAction SilentlyContinue
Write-Host "✓ CLR enabled"
