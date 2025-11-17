#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$false)]
    [switch]$UseAzureAD,
    
    [Parameter(Mandatory=$false)]
    [string]$AccessToken
)

$ErrorActionPreference = 'Stop'

Write-Host "Scaffolding entities from database..."
Set-Location $ProjectPath

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
      --force `
      --no-build
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
      --no-build
}

Write-Host "✓ Entities scaffolded"
