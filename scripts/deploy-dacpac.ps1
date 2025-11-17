#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$DacpacPath,
    
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database
)

$ErrorActionPreference = 'Stop'

$connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;"

Write-Host "Deploying DACPAC: $DacpacPath"
Write-Host "This DACPAC includes Hartonomous.Clr.dll assembly embedded as hex binary"

sqlpackage.exe `
  /Action:Publish `
  /SourceFile:"$DacpacPath" `
  /TargetConnectionString:"$connectionString" `
  /p:DropObjectsNotInSource=False `
  /p:BlockOnPossibleDataLoss=True `
  /p:IgnorePreDeployScript=True `
  /p:IgnorePostDeployScript=True

if ($LASTEXITCODE -ne 0) { 
  throw "DACPAC deployment failed" 
}

Write-Host "✓ DACPAC deployed with Hartonomous.Clr assembly"
