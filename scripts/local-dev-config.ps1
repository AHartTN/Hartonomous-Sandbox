# Hartonomous Local Development Configuration
# This file contains default values for local development.
# Override these in your environment or in launchSettings.json

# SQL Server Configuration
$LocalDevConfig = @{
    # Database
    SqlServer = "localhost"  # Your local SQL Server instance
    Database = "Hartonomous"
    
    # Authentication (local dev always uses Windows Auth)
    UseWindowsAuth = $true
    
    # Paths
    SolutionRoot = Split-Path $PSScriptRoot -Parent
    DatabaseProject = "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
    
    # Build Configuration
    BuildConfiguration = "Debug"  # Use Debug for local dev, Release for CI/CD
    
    # EF Core
    EFCoreProject = "src\Hartonomous.Data.Entities"
    EFCoreContext = "HartonomousContext"
    
    # Testing
    SkipTestsByDefault = $false
    
    # CLR Assemblies
    ClrDependenciesPath = "dependencies"
}

# Export for use in scripts
return $LocalDevConfig
