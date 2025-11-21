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

# Default configuration (inline - no external config file needed)
$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent

# Use inline defaults if not specified
if (-not $Server) { $Server = "localhost" }
if (-not $Database) { $Database = "Hartonomous" }
if (-not $ProjectPath) { $ProjectPath = Join-Path $repoRoot "src\Hartonomous.Data.Entities" }

Write-Host "Scaffolding entities from database..."
Set-Location $ProjectPath

# IDEMPOTENCY: Remove old generated files before scaffolding (prevents stale navigation properties)
Write-Host "Cleaning old generated files..."
$entitiesToClean = @("Entities", "Configurations", "Interfaces")
foreach ($dir in $entitiesToClean) {
    $fullPath = Join-Path $ProjectPath $dir
    if (Test-Path $fullPath) {
        Write-Host "  Removing $dir/"
        Remove-Item -Recurse -Force $fullPath
    }
}

# Remove old DbContext files
$dbContextFiles = @("HartonomousDbContext.cs", "HartonomousDbContextFactory.cs")
foreach ($file in $dbContextFiles) {
    $fullPath = Join-Path $ProjectPath $file
    if (Test-Path $fullPath) {
        Write-Host "  Removing $file"
        Remove-Item -Force $fullPath
    }
}

# Build project (empty, just Abstracts) - creates deps.json for dotnet ef
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
