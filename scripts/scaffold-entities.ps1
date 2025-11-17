#Requires -Version 7.0
param(
    [Parameter()]
    [string]$Server,
    
    [Parameter()]
    [string]$Database,
    
    [Parameter()]
    [string]$ProjectPath,
    
    [Parameter()]
    [switch]$UseAzureAD
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

# Build project first (required for --no-build in dotnet ef)
Write-Host "Building project for scaffolding..."
dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
    throw "Project build failed with exit code $LASTEXITCODE"
}

if ($UseAzureAD) {
    # Azure AD authentication for scaffolding
    # MS Docs pattern: "Authentication=Active Directory Default" uses DefaultAzureCredential
    # 
    # GitHub Actions (CI/CD): Network Service has Arc token permissions → Managed Identity works
    # Local dev: Use Windows Auth instead (already logged in, no prompts)
    
    Write-Host "Using Azure AD Managed Identity for scaffolding (CI/CD)"
    # In GitHub Actions, Network Service can access Arc tokens
    $connectionString = "Server=$Server;Database=$Database;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=True;"
    
    # --no-build: Use already-built assembly (avoids circular dependency, faster scaffolding)
    dotnet ef dbcontext scaffold `
      $connectionString `
      Microsoft.EntityFrameworkCore.SqlServer `
      --output-dir Entities `
      --context-dir . `
      --context HartonomousDbContext `
      --force `
      --no-onconfiguring `
      --no-build
    
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
      --force `
      --no-onconfiguring
    
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core scaffolding failed with exit code $LASTEXITCODE"
    }
}

Write-Host "✓ Entities scaffolded"
