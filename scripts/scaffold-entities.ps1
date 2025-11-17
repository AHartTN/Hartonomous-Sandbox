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

# Restore NuGet packages first
Write-Host "Restoring NuGet packages..."
dotnet restore
if ($LASTEXITCODE -ne 0) {
    throw "NuGet restore failed with exit code $LASTEXITCODE"
}

if ($UseAzureAD) {
    # Azure AD authentication using IDesignTimeDbContextFactory pattern (MS Docs recommended)
    # The factory reads access token from AZURE_SQL_ACCESS_TOKEN environment variable
    Write-Host "Using Azure AD authentication with IDesignTimeDbContextFactory pattern"
    
    # Verify access token is available in environment
    if (-not $env:AZURE_SQL_ACCESS_TOKEN) {
        throw "AZURE_SQL_ACCESS_TOKEN environment variable not set for Azure AD authentication"
    }
    
    Write-Host "Access token available: $($env:AZURE_SQL_ACCESS_TOKEN.Length) characters"
    
    # Create the design-time factory if it doesn't exist (idempotent)
    $projectDir = Split-Path $ProjectPath -Parent
    $factoryPath = Join-Path $projectDir "HartonomousDbContextFactory.cs"
    
    if (-not (Test-Path $factoryPath)) {
        Write-Host "Creating IDesignTimeDbContextFactory for design-time scaffolding..."
        
        $factoryCode = @'
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlClient;
using System;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Design-time factory for creating DbContext during EF Core scaffolding.
/// Reads connection info and Azure AD access token from environment variables.
/// </summary>
public class HartonomousDbContextFactory : IDesignTimeDbContextFactory<HartonomousDbContext>
{
    public HartonomousDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HartonomousDbContext>();
        
        // Get connection parameters from environment
        var server = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "localhost";
        var database = Environment.GetEnvironmentVariable("SQL_DATABASE") ?? "Hartonomous";
        var accessToken = Environment.GetEnvironmentVariable("AZURE_SQL_ACCESS_TOKEN");
        
        var connectionString = $"Server={server};Database={database};Encrypt=True;TrustServerCertificate=True;";
        
        optionsBuilder.UseSqlServer(connectionString);
        
        // Add interceptor to set access token on connection if provided
        if (!string.IsNullOrEmpty(accessToken))
        {
            optionsBuilder.AddInterceptors(new AccessTokenInterceptor(accessToken));
        }
        
        return new HartonomousDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Interceptor that sets Azure AD access token on SQL connections.
/// Required because dotnet ef scaffold doesn''t support access tokens directly.
/// </summary>
public class AccessTokenInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    private readonly string _accessToken;
    
    public AccessTokenInterceptor(string accessToken)
    {
        _accessToken = accessToken;
    }
    
    public override void ConnectionOpened(
        System.Data.Common.DbConnection connection,
        Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
    {
        if (connection is SqlConnection sqlConnection)
        {
            sqlConnection.AccessToken = _accessToken;
        }
        base.ConnectionOpened(connection, eventData);
    }
    
    public override async System.Threading.Tasks.ValueTask ConnectionOpenedAsync(
        System.Data.Common.DbConnection connection,
        Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData,
        System.Threading.CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection sqlConnection)
        {
            sqlConnection.AccessToken = _accessToken;
        }
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}
'@
        
        Set-Content -Path $factoryPath -Value $factoryCode -Encoding UTF8
        Write-Host "✓ Created HartonomousDbContextFactory.cs"
    } else {
        Write-Host "Design-time factory already exists (idempotent)"
    }
    
    # Set environment variables for the factory to discover
    $env:SQL_SERVER = $Server
    $env:SQL_DATABASE = $Database
    
    # Scaffold - EF tools will automatically discover and use IDesignTimeDbContextFactory
    Write-Host "Running dotnet ef dbcontext scaffold..."
    dotnet ef dbcontext scaffold `
      "Server=$Server;Database=$Database;Encrypt=True;TrustServerCertificate=True;" `
      Microsoft.EntityFrameworkCore.SqlServer `
      --output-dir Entities `
      --context-dir . `
      --context HartonomousDbContext `
      --force `
      --no-onconfiguring `
      --project "$ProjectPath"
    
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
