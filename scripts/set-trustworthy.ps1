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
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"

try {
  $sql | Out-File -FilePath $tempFile -Encoding utf8
  
  if ($UseAzureAD -and $AccessToken) {
      Write-Host "Using Azure AD service principal authentication"
      sqlcmd -S $Server -d master -G -P $AccessToken -i $tempFile -C
  } else {
      Write-Host "Using Windows integrated authentication"
      sqlcmd -S $Server -d "master" -E -C -i $tempFile -b
      if ($LASTEXITCODE -ne 0) { 
        throw "Failed to set TRUSTWORTHY" 
      }
  }
  
  Write-Host "✓ TRUSTWORTHY validated"
} finally {
  Remove-Item $tempFile -ErrorAction SilentlyContinue
}
