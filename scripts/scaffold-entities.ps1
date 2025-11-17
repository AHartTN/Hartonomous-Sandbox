#Requires -Version 7.0
param(
    [Parameter()]
    [string]$Server,
    
    [Parameter()]
    [string]$Database,
    
    [Parameter()]
    [string]$ProjectPath,
    
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
if (-not $ProjectPath) { $ProjectPath = Join-Path (Split-Path $scriptRoot -Parent) $localConfig.EFCoreProject }

Write-Host "Scaffolding entities from database..."
Set-Location $ProjectPath

# Restore NuGet packages first
Write-Host "Restoring NuGet packages..."
dotnet restore
if ($LASTEXITCODE -ne 0) {
    throw "NuGet restore failed with exit code $LASTEXITCODE"
}

# Build the project to generate deps.json
Write-Host "Building project..."
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

if ($UseAzureAD -and $AccessToken) {
    # Azure AD with access token - build connection string with token
    Write-Host "Using Azure AD service principal authentication"
    $connectionString = "Server=$Server;Database=$Database;Encrypt=True;TrustServerCertificate=True;"
    # EF Core doesn't support access tokens directly in connection string
    # Use environment variable for token
    $env:AZURE_SQL_ACCESS_TOKEN = $AccessToken
    
    dotnet ef dbcontext scaffold `
      $connectionString `
      Microsoft.EntityFrameworkCore.SqlServer `
      --output-dir Entities `
      --context-dir . `
      --context HartonomousDbContext `
      --force
    
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core scaffolding failed with exit code $LASTEXITCODE"
    }
} else {
    # Windows integrated authentication
    Write-Host "Using Windows integrated authentication"
    dotnet ef dbcontext scaffold `
      "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;" `
      Microsoft.EntityFrameworkCore.SqlServer `
      --output-dir Entities `
      --context-dir . `
      --context HartonomousDbContext `
      --force
    
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core scaffolding failed with exit code $LASTEXITCODE"
    }
}

Write-Host "✓ Entities scaffolded"
