#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$false)]
    [switch]$UseAzureAD,
    
    [Parameter(Mandatory=$false)]
    [string]$AccessToken
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Module -ListAvailable -Name SqlServer)) {
  Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
}

$sql = @"
USE [master];
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
EXEC sp_configure 'clr strict security', 0; RECONFIGURE;
PRINT 'CLR integration enabled';
"@

if ($UseAzureAD -and $AccessToken) {
    Write-Host "Using Azure AD service principal authentication"
    Invoke-Sqlcmd -Query $sql -ServerInstance $Server -AccessToken $AccessToken -TrustServerCertificate
} else {
    Write-Host "Using Windows integrated authentication"
    Invoke-Sqlcmd -Query $sql -ServerInstance $Server -TrustServerCertificate
}

Write-Host "✓ Agent permissions granted"
