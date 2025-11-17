#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys Hartonomous DACPAC to SQL Server
.DESCRIPTION
    Uses SqlPackage to deploy the DACPAC. Deployment is inherently idempotent:
    SqlPackage compares DACPAC schema to target database and only applies differences.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [switch]$IntegratedSecurity,
    [string]$User,
    [string]$Password,
    [switch]$TrustServerCertificate = $true
)

$ErrorActionPreference = "Stop"

$DacpacPath = "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"
$AltDacpacPath = "src\Hartonomous.Database\src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"

# Verify DACPAC exists (check both possible paths)
if (Test-Path $AltDacpacPath) {
    $DacpacPath = $AltDacpacPath
} elseif (-not (Test-Path $DacpacPath)) {
    throw "DACPAC not found at: $DacpacPath or $AltDacpacPath`nRun build-dacpac.ps1 first"
}

# Build connection string
if ($IntegratedSecurity -or (-not $User)) {
    $connectionString = "Server=$Server;Database=$Database;Integrated Security=True;"
} else {
    $connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;"
}

if ($TrustServerCertificate) {
    $connectionString += "TrustServerCertificate=True;"
}

# Find SqlPackage
$sqlPackagePath = $null

# Check dotnet tool first
$dotnetSqlPackage = Get-Command sqlpackage -ErrorAction SilentlyContinue
if ($dotnetSqlPackage) {
    $sqlPackagePath = $dotnetSqlPackage.Source
}

# Fallback to installed paths
if (-not $sqlPackagePath) {
    $sqlPackagePaths = @(
        "${env:ProgramFiles}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
    )
    
    foreach ($path in $sqlPackagePaths) {
        if (Test-Path $path) {
            $sqlPackagePath = $path
            break
        }
    }
}

if (-not $sqlPackagePath) {
    throw "SqlPackage not found.`nInstall via: dotnet tool install -g microsoft.sqlpackage`nOr install SQL Server Data Tools (SSDT)"
}

# Deploy (SqlPackage Publish action is idempotent by design)
Write-Host "Deploying DACPAC to $Server/$Database..." -ForegroundColor Cyan
Write-Host "  SqlPackage: $sqlPackagePath" -ForegroundColor Gray
Write-Host "  DACPAC: $DacpacPath" -ForegroundColor Gray

& $sqlPackagePath /Action:Publish `
    /SourceFile:$DacpacPath `
    /TargetConnectionString:$connectionString `
    /p:IncludeCompositeObjects=True `
    /p:BlockOnPossibleDataLoss=False `
    /p:DropObjectsNotInSource=True `
    /p:DropConstraintsNotInSource=True `
    /p:DropIndexesNotInSource=True `
    /p:DoNotDropObjectTypes=Assemblies `
    /p:VerifyDeployment=True `
    /p:AllowIncompatiblePlatform=True

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC deployment failed with exit code $LASTEXITCODE"
}

Write-Host "  Deployment complete" -ForegroundColor Green

exit 0
