#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates and applies EF Core migrations to Arc-enabled SQL Server.

.DESCRIPTION
    Idempotent Entity Framework Core migration deployment for Azure Arc SQL Server.
    - Uses dotnet ef migrations script --idempotent for safe redeployment
    - Targets Hartonomous.Data.HartonomousDbContext
    - Handles NetTopologySuite spatial types (geometry columns)
    - Verifies __EFMigrationsHistory table for applied migrations
    - Returns JSON with migration status for Azure DevOps pipeline

.PARAMETER ServerName
    SQL Server instance name

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER ProjectPath
    Path to Hartonomous.Data.csproj (contains DbContext)

.PARAMETER StartupProjectPath
    Path to startup project with appsettings.json (optional - uses Infrastructure if not provided)

.PARAMETER ConnectionString
    SQL Server connection string (optional - overrides appsettings.json)

.PARAMETER SqlUser
    SQL authentication username (optional)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.EXAMPLE
    .\05-ef-migrations.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous" -ProjectPath "d:\Repositories\Hartonomous\src\Hartonomous.Data\Hartonomous.Data.csproj"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",
    
    [Parameter(Mandatory)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$false)]
    [string]$StartupProjectPath,
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString,
    
    [Parameter(Mandatory=$false)]
    [string]$SqlUser,
    
    [Parameter(Mandatory=$false)]
    [SecureString]$SqlPassword
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$result = @{
    Step = "EFCoreMigrations"
    Success = $false
    MigrationScriptGenerated = $false
    MigrationsApplied = $false
    PendingMigrations = @()
    AppliedMigrations = @()
    Errors = @()
    Warnings = @()
    Timestamp = (Get-Date -Format "o")
}

function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$Database = "master"
    )
    
    $sqlArgs = @("-S", $ServerName, "-d", $Database, "-C", "-b", "-Q", $Query, "-h", "-1")
    
    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }
    
    $output = & sqlcmd @sqlArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SQL command failed: $output"
    }
    return ($output | Out-String).Trim()
}

try {
    Write-Host "=== EF Core Migrations Script ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray
    Write-Host "Project: $ProjectPath" -ForegroundColor Gray
    
    # Verify project file exists
    Write-Host "`nVerifying project file..." -NoNewline
    if (-not (Test-Path $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }
    Write-Host " Found" -ForegroundColor Green
    
    # Verify dotnet CLI
    Write-Host "Checking dotnet CLI..." -NoNewline
    $dotnetVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet CLI not found or not accessible"
    }
    Write-Host " $dotnetVersion" -ForegroundColor Green
    
    # Check dotnet ef tools
    Write-Host "Checking dotnet ef tools..." -NoNewline
    $efVersion = & dotnet ef --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host " Not found, installing..." -NoNewline
        & dotnet tool install --global dotnet-ef 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install dotnet-ef tool"
        }
        $efVersion = & dotnet ef --version 2>&1
    }
    Write-Host " $efVersion" -ForegroundColor Green
    
    # Build connection string if not provided
    if (-not $ConnectionString) {
        Write-Host "Building connection string..." -NoNewline
        if ($SqlUser) {
            $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
                [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
            )
            $ConnectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$SqlUser;Password=$plainPassword;TrustServerCertificate=True;Encrypt=True;"
        }
        else {
            $ConnectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;"
        }
        Write-Host " Done" -ForegroundColor Green
    }
    
    # Generate idempotent migration script
    Write-Host "Generating migration script..." -NoNewline
    $scriptPath = Join-Path $env:TEMP "migrations_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
    
    $efArgs = @(
        "ef", "migrations", "script",
        "--idempotent",
        "--project", $ProjectPath,
        "--output", $scriptPath,
        "--context", "HartonomousDbContext",
        "--no-build"
    )
    
    if ($StartupProjectPath) {
        $efArgs += @("--startup-project", $StartupProjectPath)
    }
    
    $efOutput = & dotnet @efArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Migration script generation failed: $efOutput"
    }
    
    if (-not (Test-Path $scriptPath)) {
        throw "Migration script file not created: $scriptPath"
    }
    
    $scriptSize = (Get-Item $scriptPath).Length
    $result.MigrationScriptGenerated = $true
    Write-Host " Generated ($([math]::Round($scriptSize / 1KB, 2)) KB)" -ForegroundColor Green
    Write-Host "  Script: $scriptPath" -ForegroundColor Gray
    
    # Read migration script content
    $migrationScript = Get-Content -Path $scriptPath -Raw -Encoding UTF8
    
    # Check for pending migrations by examining script content
    if ($migrationScript -match "CREATE TABLE \[__EFMigrationsHistory\]" -or 
        $migrationScript -match "INSERT INTO \[__EFMigrationsHistory\]") {
        Write-Host "! Migration script contains changes to apply" -ForegroundColor Yellow
    }
    else {
        Write-Host "✓ No pending migrations (script is empty or no-op)" -ForegroundColor Green
    }
    
    # Apply migrations via sqlcmd
    Write-Host "Applying migrations to database..." -NoNewline
    
    $sqlArgs = @("-S", $ServerName, "-d", $DatabaseName, "-C", "-b", "-i", $scriptPath)
    
    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }
    
    $sqlcmdOutput = & sqlcmd @sqlArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Migration application failed: $sqlcmdOutput"
    }
    
    $result.MigrationsApplied = $true
    Write-Host " Done" -ForegroundColor Green
    
    # Query applied migrations
    Write-Host "Verifying migrations..." -NoNewline
    $migrationsQuery = @"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    SELECT MigrationId, ProductVersion 
    FROM [__EFMigrationsHistory]
    ORDER BY MigrationId
    FOR JSON PATH
END
ELSE
BEGIN
    SELECT '[]' AS JsonResult
END
"@
    $migrationsJson = Invoke-SqlCommand -Query $migrationsQuery -Database $DatabaseName
    
    if ($migrationsJson -and $migrationsJson -ne "[]") {
        $appliedMigrations = $migrationsJson | ConvertFrom-Json
        $result.AppliedMigrations = $appliedMigrations | ForEach-Object { $_.MigrationId }
        Write-Host " $($appliedMigrations.Count) migrations applied" -ForegroundColor Green
        
        # Show recent migrations
        $recentMigrations = $appliedMigrations | Select-Object -Last 5
        foreach ($migration in $recentMigrations) {
            Write-Host "  - $($migration.MigrationId) (EF $($migration.ProductVersion))" -ForegroundColor Gray
        }
    }
    else {
        Write-Host " No migrations found" -ForegroundColor Yellow
        $result.Warnings += "No migrations found in __EFMigrationsHistory table"
    }
    
    # Verify spatial types (NetTopologySuite geometry columns)
    Write-Host "Checking spatial types..." -NoNewline
    $spatialQuery = @"
SELECT COUNT(*) 
FROM sys.columns c
INNER JOIN sys.types t ON c.system_type_id = t.system_type_id
WHERE t.name = 'geometry'
"@
    $spatialCount = [int](Invoke-SqlCommand -Query $spatialQuery -Database $DatabaseName)
    
    if ($spatialCount -gt 0) {
        Write-Host " $spatialCount geometry columns found" -ForegroundColor Green
    }
    else {
        Write-Host " No geometry columns (NetTopologySuite not used?)" -ForegroundColor Yellow
        $result.Warnings += "No spatial geometry columns found in database"
    }
    
    # Clean up temp script
    if (Test-Path $scriptPath) {
        Remove-Item $scriptPath -Force
    }
    
    $result.Success = $true
}
catch {
    $result.Success = $false
    $result.Errors += $_.Exception.Message
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Clean up temp script on error
    if ($scriptPath -and (Test-Path $scriptPath)) {
        Remove-Item $scriptPath -Force -ErrorAction SilentlyContinue
    }
}

# Output JSON
$jsonOutput = $result | ConvertTo-Json -Depth 5 -Compress
Write-Host "`n=== JSON Output ===" -ForegroundColor Cyan
Write-Host $jsonOutput

if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "ef-migrations.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
