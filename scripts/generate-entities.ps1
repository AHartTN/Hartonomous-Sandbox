<#
.SYNOPSIS
    Generates EF Core entity classes from the deployed Hartonomous database.

.DESCRIPTION
    This script scaffolds Entity Framework Core entities from the SQL Server database
    schema after DACPAC deployment. It follows database-first approach with support
    for partial classes, custom configurations, and idempotent regeneration.

.PARAMETER Server
    SQL Server instance name (default: localhost).

.PARAMETER Database
    Database name (default: Hartonomous).

.PARAMETER ProjectPath
    Path to the Hartonomous.Data project (default: auto-detected).

.PARAMETER ConnectionString
    Optional override for the connection string.

.PARAMETER Force
    Force overwrite of existing files without prompting.

.PARAMETER WhatIf
    Show what would be done without making changes.

.EXAMPLE
    .\generate-entities.ps1

.EXAMPLE
    .\generate-entities.ps1 -Server "localhost" -Database "Hartonomous" -Force

.EXAMPLE
    .\generate-entities.ps1 -ConnectionString "Server=localhost;Database=Hartonomous;Trusted_Connection=True" -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string]$Server = "localhost",

    [Parameter()]
    [string]$Database = "Hartonomous",

    [Parameter()]
    [string]$ProjectPath,

    [Parameter()]
    [string]$ConnectionString,

    [Parameter()]
    [switch]$Force,

    [Parameter()]
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# ============ Configuration ============

$ScriptRoot = $PSScriptRoot
$WorkspaceRoot = Split-Path -Parent $ScriptRoot

# Auto-detect project path if not specified
if (-not $ProjectPath) {
    $ProjectPath = Join-Path $WorkspaceRoot "src" "Hartonomous.Data"
}

if (-not (Test-Path $ProjectPath)) {
    throw "Project path not found: $ProjectPath"
}

# Build connection string if not provided
if (-not $ConnectionString) {
    $ConnectionString = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
}

# EF Core scaffolding parameters
$ScaffoldParams = @{
    Provider = "Microsoft.EntityFrameworkCore.SqlServer"
    ContextName = "HartonomousDbContext"
    ContextDir = "."
    ContextNamespace = "Hartonomous.Data"
    OutputDir = "Entities"
    Namespace = "Hartonomous.Data.Entities"
    UseDatabaseNames = $true
    NoOnConfiguring = $true
    Force = $Force.IsPresent
}

# ============ Functions ============

function Write-Header {
    param([string]$Message)
    Write-Information ""
    Write-Information ("=" * 80)
    Write-Information $Message
    Write-Information ("=" * 80)
}

function Test-DotNetEfTool {
    Write-Information "Checking for dotnet-ef CLI tool..."
    
    $efVersion = dotnet tool list --global | Select-String "dotnet-ef"
    
    if (-not $efVersion) {
        Write-Warning "dotnet-ef tool not found. Installing..."
        dotnet tool install --global dotnet-ef --version 10.0.0-rc.2.25502.107
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install dotnet-ef tool"
        }
        
        Write-Information "✓ dotnet-ef tool installed successfully"
    } else {
        Write-Information "✓ dotnet-ef tool found: $($efVersion.ToString().Trim())"
    }
}

function Test-DatabaseConnection {
    Write-Information "Testing database connection..."
    
    try {
        $sqlConnection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $sqlConnection.Open()
        
        $command = $sqlConnection.CreateCommand()
        $command.CommandText = "SELECT DB_NAME() AS CurrentDatabase, @@VERSION AS Version"
        $reader = $command.ExecuteReader()
        
        if ($reader.Read()) {
            $dbName = $reader["CurrentDatabase"]
            $version = $reader["Version"].ToString().Split("`n")[0]
            Write-Information "✓ Connected to database: $dbName"
            Write-Information "  SQL Server: $version"
        }
        
        $reader.Close()
        $sqlConnection.Close()
    }
    catch {
        throw "Failed to connect to database: $_"
    }
}

function Backup-ExistingEntities {
    $entitiesPath = Join-Path $ProjectPath $ScaffoldParams.OutputDir
    
    if (Test-Path $entitiesPath) {
        $backupPath = Join-Path $ProjectPath "_backup_entities_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        
        Write-Information "Creating backup of existing entities..."
        Copy-Item -Path $entitiesPath -Destination $backupPath -Recurse -Force
        
        Write-Information "✓ Backup created: $backupPath"
        return $backupPath
    }
    
    return $null
}

function Invoke-Scaffolding {
    Write-Header "Scaffolding Entity Classes"
    
    Push-Location $ProjectPath
    
    try {
        # Build the dotnet ef scaffold command
        $cmd = "dotnet ef dbcontext scaffold"
        $cmd += " `"$ConnectionString`""
        $cmd += " $($ScaffoldParams.Provider)"
        $cmd += " --context $($ScaffoldParams.ContextName)"
        $cmd += " --context-dir $($ScaffoldParams.ContextDir)"
        $cmd += " --context-namespace $($ScaffoldParams.ContextNamespace)"
        $cmd += " --output-dir $($ScaffoldParams.OutputDir)"
        $cmd += " --namespace $($ScaffoldParams.Namespace)"
        
        if ($ScaffoldParams.UseDatabaseNames) {
            $cmd += " --use-database-names"
        }
        
        if ($ScaffoldParams.NoOnConfiguring) {
            $cmd += " --no-onconfiguring"
        }
        
        if ($ScaffoldParams.Force) {
            $cmd += " --force"
        }
        
        if ($WhatIf) {
            Write-Information "Would execute: $cmd"
            return @{ Success = $true; WhatIf = $true }
        }
        
        Write-Information "Executing: $cmd"
        Write-Information ""
        
        $output = Invoke-Expression $cmd 2>&1
        $exitCode = $LASTEXITCODE
        
        # Display output
        $output | ForEach-Object { Write-Information $_ }
        
        if ($exitCode -ne 0) {
            throw "Scaffolding failed with exit code: $exitCode"
        }
        
        # Parse output for statistics
        $entityCount = ($output | Select-String "entity type").Count
        
        Write-Information ""
        Write-Information "✓ Scaffolding completed successfully"
        Write-Information "  Entities generated: $entityCount"
        
        return @{
            Success = $true
            EntityCount = $entityCount
            Output = $output
        }
    }
    finally {
        Pop-Location
    }
}

function New-PartialClassStructure {
    Write-Header "Creating Partial Class Structure"
    
    $contextPartialPath = Join-Path $ProjectPath "$($ScaffoldParams.ContextName).Partial.cs"
    
    if (-not (Test-Path $contextPartialPath)) {
        $partialContent = @"
using Microsoft.EntityFrameworkCore;

namespace $($ScaffoldParams.ContextNamespace);

/// <summary>
/// Partial class for HartonomousDbContext custom extensions.
/// This file is preserved during scaffolding regeneration.
/// </summary>
public partial class $($ScaffoldParams.ContextName)
{
    /// <summary>
    /// Partial method for additional model configuration.
    /// Called after OnModelCreating completes.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Add custom Fluent API configurations here
        // These will override scaffolded configurations
        
        // Example: Configure global query filters
        // modelBuilder.Entity<YourEntity>().HasQueryFilter(e => !e.IsDeleted);
        
        // Example: Configure value converters
        // modelBuilder.Entity<YourEntity>()
        //     .Property(e => e.VectorData)
        //     .HasConversion(/* your converter */);
    }
}
"@
        
        if ($PSCmdlet.ShouldProcess($contextPartialPath, "Create partial class file")) {
            Set-Content -Path $contextPartialPath -Value $partialContent -Encoding UTF8
            Write-Information "✓ Created: $contextPartialPath"
        }
    } else {
        Write-Information "ℹ Partial class already exists: $contextPartialPath"
    }
}

function Get-GenerationSummary {
    param(
        [hashtable]$Result,
        [string]$BackupPath
    )
    
    Write-Header "Generation Summary"
    
    if ($Result.WhatIf) {
        Write-Information "✓ WhatIf mode - no changes made"
        return
    }
    
    if ($Result.Success) {
        Write-Information "✓ Entity scaffolding completed successfully"
        Write-Information "  Database: $Database"
        Write-Information "  Entities: $($Result.EntityCount)"
        Write-Information "  Location: $ProjectPath"
        
        if ($BackupPath) {
            Write-Information "  Backup: $BackupPath"
        }
        
        # Show git diff summary if in a git repository
        $gitRoot = git rev-parse --show-toplevel 2>$null
        if ($gitRoot -and (Test-Path $gitRoot)) {
            Write-Information ""
            Write-Information "Git Changes:"
            
            Push-Location $ProjectPath
            try {
                $gitDiff = git diff --stat 2>&1
                if ($gitDiff) {
                    $gitDiff | ForEach-Object { Write-Information "  $_" }
                } else {
                    Write-Information "  No changes detected"
                }
            }
            finally {
                Pop-Location
            }
        }
        
        Write-Information ""
        Write-Information "Next Steps:"
        Write-Information "  1. Review generated entities in: $($ScaffoldParams.OutputDir)/"
        Write-Information "  2. Add custom logic to: $($ScaffoldParams.ContextName).Partial.cs"
        Write-Information "  3. Build solution: dotnet build"
        Write-Information "  4. Run tests to verify: dotnet test"
    }
}

function Invoke-BuildVerification {
    Write-Header "Build Verification"
    
    if ($WhatIf) {
        Write-Information "Skipping build verification in WhatIf mode"
        return
    }
    
    Write-Information "Building Hartonomous.Data project..."
    
    Push-Location $ProjectPath
    try {
        $buildOutput = dotnet build --nologo --verbosity minimal 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-Information "✓ Build succeeded"
        } else {
            Write-Warning "Build failed with exit code: $exitCode"
            $buildOutput | ForEach-Object { Write-Warning $_ }
        }
    }
    finally {
        Pop-Location
    }
}

# ============ Main Execution ============

try {
    Write-Header "EF Core Entity Generation for Hartonomous"
    
    Write-Information "Configuration:"
    Write-Information "  Server: $Server"
    Write-Information "  Database: $Database"
    Write-Information "  Project: $ProjectPath"
    Write-Information "  WhatIf: $WhatIf"
    Write-Information ""
    
    # Prerequisites
    Test-DotNetEfTool
    Test-DatabaseConnection
    
    # Backup existing entities
    $backupPath = Backup-ExistingEntities
    
    # Scaffold entities
    $result = Invoke-Scaffolding
    
    # Create partial class structure
    New-PartialClassStructure
    
    # Verify build
    Invoke-BuildVerification
    
    # Summary
    Get-GenerationSummary -Result $result -BackupPath $backupPath
    
    Write-Information ""
    Write-Information "✓ Entity generation completed successfully"
    
    exit 0
}
catch {
    Write-Error "Entity generation failed: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
