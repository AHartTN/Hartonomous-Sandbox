#Requires -Version 7.0
param(
    [Parameter()]
    [string]$DacpacPath,
    
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

if ($UseAzureAD -and $AccessToken) {
    Write-Host "Using Azure AD service principal authentication"
    & $sqlPackage `
      /Action:Publish `
      /SourceFile:"$DacpacPath" `
      /TargetServerName:"$Server" `
      /TargetDatabaseName:"$Database" `
      /AccessToken:"$AccessToken" `
      /p:DropObjectsNotInSource=False `
      /p:BlockOnPossibleDataLoss=True `
      /p:IgnorePreDeployScript=True `
      /p:IgnorePostDeployScript=True
} else {
    Write-Host "Using Windows integrated authentication"
    $connectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;"
    & $sqlPackage `
      /Action:Publish `
      /SourceFile:"$DacpacPath" `
      /TargetConnectionString:"$connectionString" `
      /p:DropObjectsNotInSource=False `
      /p:BlockOnPossibleDataLoss=True `
      /p:IgnorePreDeployScript=True `
      /p:IgnorePostDeployScript=True
}

if ($LASTEXITCODE -ne 0) { 
  throw "DACPAC deployment failed" 
}

Write-Host "✓ DACPAC deployed with Hartonomous.Clr assembly"
