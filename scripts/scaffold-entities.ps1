#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath
)

$ErrorActionPreference = 'Stop'

Write-Host "Scaffolding entities from database..."

Set-Location $ProjectPath

dotnet ef dbcontext scaffold `
  "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;" `
  Microsoft.EntityFrameworkCore.SqlServer `
  --output-dir Entities `
  --context-dir . `
  --context HartonomousDbContext `
  --force `
  --no-build

Write-Host "`u2713 Entities scaffolded"
