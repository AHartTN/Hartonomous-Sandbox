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

$connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;"

Write-Host "Deploying DACPAC: $DacpacPath"
Write-Host "This DACPAC includes Hartonomous.Clr.dll assembly embedded as hex binary"

& $sqlPackage `
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
