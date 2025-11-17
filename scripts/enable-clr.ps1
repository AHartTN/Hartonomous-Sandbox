#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Module -ListAvailable -Name SqlServer)) {
  Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
}

$sql = @"
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
EXEC sp_configure 'clr strict security', 0; RECONFIGURE;
PRINT 'CLR integration enabled successfully';
"@

Invoke-Sqlcmd -Query $sql -ServerInstance $Server -Database "master" -TrustServerCertificate
Write-Host "✓ CLR enabled"
