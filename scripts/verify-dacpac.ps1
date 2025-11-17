#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$DacpacPath
)

$ErrorActionPreference = 'Stop'

if (Test-Path $DacpacPath) {
  Write-Host "✓ DACPAC built successfully: $DacpacPath"
  $hash = (Get-FileHash $DacpacPath -Algorithm SHA256).Hash
  Write-Host "SHA256: $hash"
} else {
  Write-Error "✗ DACPAC not found at $DacpacPath"
  exit 1
}
