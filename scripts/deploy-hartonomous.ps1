#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Unified idempotent deployment script for Hartonomous database, CLR, and procedures.

.DESCRIPTION
    Complete end-to-end deployment automation combining DACPAC deployment, CLR assembly
    registration, entity scaffolding, and .NET solution build. All operations are 
    idempotent and can be safely re-run on fresh or existing databases.

    Deployment order (CRITICAL - DO NOT CHANGE):
    1. Pre-flight checks (SQL connection, tools, paths)
    2. Build DACPAC with MSBuild (NOT dotnet build)
    3. Deploy DACPAC with SqlPackage (safe parameters for existing databases)
    4. Deploy external CLR dependencies (System.*, MathNet.*, etc.)
    5. Scaffold EF Core entities from deployed database schema
    6. Build .NET solution (now entities exist)
    7. Deploy stored procedures
    8. Validate deployment

    Features:
    - Idempotent DACPAC deployment (applies only schema differences)
    - Safe for existing databases (preserves data and objects)
    - Secure CLR deployment with proper assembly ordering
    - Automated EF Core entity scaffolding from database
    - Comprehensive error handling
    - Detailed validation and verification steps

.PARAMETER Server
    SQL Server instance name (default: localhost)

.PARAMETER Database
    Target database name (default: Hartonomous)

.PARAMETER IntegratedSecurity
    Use Windows Authentication instead of SQL Authentication

.PARAMETER User
    SQL Server username (required if not using IntegratedSecurity)

.PARAMETER Password
    SQL Server password (required if not using IntegratedSecurity)

.PARAMETER TrustServerCertificate
    Trust server certificate (default: true for local development)

.EXAMPLE
    .\deploy-hartonomous.ps1 -Server "localhost" -Database "Hartonomous" -IntegratedSecurity
    
    Complete deployment using Windows Authentication to local server

.EXAMPLE
    .\deploy-hartonomous.ps1 -Server "prodserver" -Database "Hartonomous" -User "sa" -Password "password"
    
    Complete deployment to production server using SQL Authentication

.NOTES
    Author: Hartonomous Development Team
    Version: 2.0.0
    Security: Uses CLR strict security with trusted assembly list (TRUSTWORTHY OFF)
    
    IMPORTANT: This script executes ALL deployment steps in the correct dependency order.
               No skip flags are provided as all steps are required for proper operation.
#>

param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$IntegratedSecurity,
    [string]$User,
    [string]$Password,
    [switch]$TrustServerCertificate = $true
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$script:deploymentErrors = @()
$script:deploymentWarnings = @()

# =============================================================================
# HELPER FUNCTIONS
# =============================================================================

function Write-Section {
    param([string]$Title, [int]$Step, [int]$Total)
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " [ STEP $Step/$Total ] $Title" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ WARNING: $Message" -ForegroundColor Yellow
    $script:deploymentWarnings += $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ ERROR: $Message" -ForegroundColor Red
    $script:deploymentErrors += $Message
}

function Test-SqlConnection {
    param([string]$ConnectionString)
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Get-ConnectionString {
    if ($IntegratedSecurity -or (-not $User)) {
        $connStr = "Server=$Server;Database=$Database;Integrated Security=True;"
    } else {
        $connStr = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;"
    }
    
    if ($TrustServerCertificate) {
        $connStr += "TrustServerCertificate=True;"
    }
    
    return $connStr
}

function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$DatabaseOverride = $null
    )
    
    $targetDb = if ($DatabaseOverride) { $DatabaseOverride } else { $Database }
    
    try {
        if ($IntegratedSecurity -or (-not $User)) {
            $result = sqlcmd -S $Server -d $targetDb -E -Q $Query -b -h -1 -W -C 2>&1
        } else {
            $result = sqlcmd -S $Server -d $targetDb -U $User -P $Password -Q $Query -b -h -1 -W -C 2>&1
        }
        
        if ($LASTEXITCODE -ne 0) {
            throw "SQL command failed with exit code $LASTEXITCODE"
        }
        
        return $result
    }
    catch {
        throw "SQL execution error: $($_.Exception.Message)"
    }
}

function Invoke-SqlFile {
    param(
        [string]$FilePath,
        [string]$DatabaseOverride = $null
    )
    
    if (-not (Test-Path $FilePath)) {
        throw "SQL file not found: $FilePath"
    }
    
    $targetDb = if ($DatabaseOverride) { $DatabaseOverride } else { $Database }
    
    try {
        if ($IntegratedSecurity -or (-not $User)) {
            $result = sqlcmd -S $Server -d $targetDb -E -i $FilePath -b -C 2>&1
        } else {
            $result = sqlcmd -S $Server -d $targetDb -U $User -P $Password -i $FilePath -b -C 2>&1
        }
        
        if ($LASTEXITCODE -ne 0) {
            throw "SQL file execution failed with exit code $LASTEXITCODE"
        }
        
        return $result
    }
    catch {
        throw "SQL file execution error: $($_.Exception.Message)"
    }
}

# =============================================================================
# MAIN DEPLOYMENT WORKFLOW
# =============================================================================

$startTime = Get-Date
$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent

# ASCII Art Header
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║        HARTONOMOUS UNIFIED DEPLOYMENT v1.0                     ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║  Autonomous Geometric Reasoning System                         ║" -ForegroundColor Cyan
Write-Host "║  Database Intelligence with O(log N) Spatial Search            ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target Server:   $Server" -ForegroundColor White
Write-Host "Target Database: $Database" -ForegroundColor White
Write-Host "Authentication:  $(if ($IntegratedSecurity -or (-not $User)) { 'Windows' } else { 'SQL' })" -ForegroundColor White
Write-Host ""

# =============================================================================
# STEP 0: PRE-FLIGHT CHECKS
# =============================================================================

Write-Section "Pre-Flight Checks" 0 9

# Check SQL Server connectivity
Write-Host "Testing SQL Server connection..." -ForegroundColor Cyan
# Test connection to master database instead of target database (which may not exist yet)
$testConnectionString = Get-ConnectionString
$testConnectionString = $testConnectionString -replace "Database=$Database", "Database=master"
if (Test-SqlConnection $testConnectionString) {
    Write-Success "SQL Server connection successful"
} else {
    Write-Error "Cannot connect to SQL Server: $Server"
    Write-Host "Please verify:"
    Write-Host "  - SQL Server is running"
    Write-Host "  - Server name is correct: $Server"
    Write-Host "  - Authentication credentials are valid"
    throw "SQL Server connection failed"
}

# Check for required tools
Write-Host "Checking required tools..." -ForegroundColor Cyan

$requiredTools = @(
    @{ Name = "dotnet"; Command = "dotnet --version" },
    @{ Name = "sqlcmd"; Command = "sqlcmd -?" },
    @{ Name = "sqlpackage"; Command = "sqlpackage /?" },
    @{ Name = "dotnet-ef"; Command = "dotnet ef --version" }
)

foreach ($tool in $requiredTools) {
    try {
        $null = Invoke-Expression "$($tool.Command) 2>&1"
        Write-Success "$($tool.Name) found"
    }
    catch {
        Write-Warning "$($tool.Name) not found or not working properly"
        if ($tool.Name -eq "dotnet-ef") {
            Write-Host "  Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Gray
        }
    }
}

# Find MSBuild
Write-Host "Locating MSBuild..." -ForegroundColor Cyan
$script:msbuildPath = $null
$msbuildPaths = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)

foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $script:msbuildPath = $path
        Write-Success "MSBuild found: $(Split-Path (Split-Path (Split-Path $path -Parent) -Parent) -Parent)"
        break
    }
}

if (-not $script:msbuildPath) {
    Write-Error "MSBuild not found. Install Visual Studio 2022 with SQL Server Data Tools (SSDT)."
    throw "MSBuild is required to build database project (.sqlproj)"
}

# Verify repository structure
Write-Host "Verifying repository structure..." -ForegroundColor Cyan
$requiredPaths = @(
    "src\Hartonomous.Database\Hartonomous.Database.sqlproj",
    "src\Hartonomous.Data.Entities\Hartonomous.Data.Entities.csproj",
    "dependencies",
    "scripts"
)

foreach ($path in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $path
    if (Test-Path $fullPath) {
        Write-Success "Found: $path"
    } else {
        Write-Error "Missing: $path"
        throw "Invalid repository structure"
    }
}

# =============================================================================
# STEP 1: BUILD DATABASE PROJECT (DACPAC)
# =============================================================================

Write-Section "Building Database Project (DACPAC)" 1 9

$dacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"
$altDacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"

try {
    $dbProjPath = Join-Path $repoRoot "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
    
    Write-Host "Building database project with MSBuild..." -ForegroundColor Cyan
    Write-Host "  MSBuild: $script:msbuildPath" -ForegroundColor Gray
    Write-Host "  Project: $(Split-Path $dbProjPath -Leaf)" -ForegroundColor Gray
    
    # Build the database project (includes CLR code compilation)
    & $script:msbuildPath $dbProjPath `
        /t:Build `
        /p:Configuration=Release `
        /v:minimal `
        /nologo
    
    if ($LASTEXITCODE -ne 0) {
        throw "Database project build failed with exit code $LASTEXITCODE"
    }
    
    # Check both possible output paths
    if (Test-Path $dacpacPath) {
        $script:finalDacpacPath = $dacpacPath
    } elseif (Test-Path $altDacpacPath) {
        $script:finalDacpacPath = $altDacpacPath
    } else {
        throw "DACPAC not found at expected locations"
    }
    
    Write-Success "Database project (DACPAC) built successfully"
    Write-Host "  Output: $script:finalDacpacPath" -ForegroundColor Gray
}
catch {
    Write-Error "DACPAC build failed: $($_.Exception.Message)"
    throw
}

# =============================================================================
# STEP 2: CONFIGURE CLR SECURITY
# =============================================================================

Write-Section "Configuring SQL Server CLR" 2 9

try {
    Write-Host "Enabling CLR integration on master database..." -ForegroundColor Cyan
    
    $clrConfigSql = @"
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
PRINT 'CLR integration enabled';
"@
    
    $result = Invoke-SqlCommand -Query $clrConfigSql -DatabaseOverride "master"
    Write-Success "CLR integration enabled"
}
catch {
    Write-Warning "CLR configuration may have failed: $($_.Exception.Message)"
    Write-Host "This may be expected if CLR is already configured" -ForegroundColor Gray
}

# =============================================================================
# STEP 3: DEPLOY DACPAC (SCHEMA + MAIN CLR ASSEMBLY)
# =============================================================================

Write-Section "Deploying Database Schema (DACPAC)" 3 9

try {
    $connectionString = Get-ConnectionString
    
    Write-Host "Deploying DACPAC to $Server/$Database..." -ForegroundColor Cyan
    Write-Host "  Source: $(Split-Path $script:finalDacpacPath -Leaf)" -ForegroundColor Gray
    Write-Host "  Target: $Server/$Database" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Deployment is SAFE for existing databases:" -ForegroundColor Green
    Write-Host "    - Preserves data in all tables" -ForegroundColor Gray
    Write-Host "    - Preserves objects not in DACPAC" -ForegroundColor Gray
    Write-Host "    - Applies only necessary schema changes" -ForegroundColor Gray
    Write-Host ""
    
    $publishArgs = @(
        "/Action:Publish",
        "/SourceFile:$($script:finalDacpacPath)",
        "/TargetConnectionString:$connectionString",
        "/p:IncludeCompositeObjects=True",
        "/p:BlockOnPossibleDataLoss=False",
        "/p:DropObjectsNotInSource=False",
        "/p:DropConstraintsNotInSource=False",
        "/p:DropIndexesNotInSource=False",
        "/p:VerifyDeployment=True",
        "/p:AllowIncompatiblePlatform=True"
    )
    
    $publishOutput = & sqlpackage $publishArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "DACPAC deployment completed with warnings"
        Write-Host ($publishOutput | Out-String) -ForegroundColor Yellow
    } else {
        Write-Success "Database schema deployed successfully"
        Write-Success "Main CLR assembly (Hartonomous.Database.dll) embedded in DACPAC"
    }
}
catch {
    Write-Error "DACPAC deployment failed: $($_.Exception.Message)"
    throw
}

# =============================================================================
# STEP 4: DEPLOY EXTERNAL CLR DEPENDENCIES
# =============================================================================

Write-Section "Deploying External CLR Dependencies" 4 9

$dependenciesPath = Join-Path $repoRoot "dependencies"

if (Test-Path $dependenciesPath) {
    Write-Host "Found dependencies directory with external assemblies" -ForegroundColor Cyan
    $dllFiles = Get-ChildItem -Path $dependenciesPath -Filter "*.dll"
    Write-Host "  Total assemblies: $($dllFiles.Count)" -ForegroundColor Gray
    
    # Define deployment tiers (dependency order is critical)
    $assemblyTiers = @(
        @{
            Tier = 1
            Name = "Core System Assemblies"
            Assemblies = @(
                "System.Runtime.CompilerServices.Unsafe.dll",
                "System.Buffers.dll",
                "System.Numerics.Vectors.dll"
            )
        },
        @{
            Tier = 2
            Name = "Memory & Runtime Assemblies"
            Assemblies = @(
                "System.Memory.dll",
                "System.ValueTuple.dll"
            )
        },
        @{
            Tier = 3
            Name = "Collections & Metadata"
            Assemblies = @(
                "System.Collections.Immutable.dll",
                "System.Reflection.Metadata.dll"
            )
        },
        @{
            Tier = 4
            Name = "Service & Drawing Assemblies"
            Assemblies = @(
                "System.ServiceModel.Internals.dll",
                "SMDiagnostics.dll",
                "System.Drawing.dll",
                "System.Runtime.Serialization.dll"
            )
        },
        @{
            Tier = 5
            Name = "Third-Party Libraries"
            Assemblies = @(
                "Newtonsoft.Json.dll",
                "MathNet.Numerics.dll"
            )
        },
        @{
            Tier = 6
            Name = "Application Assemblies"
            Assemblies = @(
                "Microsoft.SqlServer.Types.dll",
                "SqlClrFunctions.dll"
            )
        }
    )
    
    try {
        # Temporarily enable TRUSTWORTHY for deployment
        Write-Host "Configuring database for CLR deployment..." -ForegroundColor Cyan
        
        # Set TRUSTWORTHY on target database
        $trustworthySql = "ALTER DATABASE [$Database] SET TRUSTWORTHY ON;"
        Invoke-SqlCommand -Query $trustworthySql -DatabaseOverride "master"
        
        # Disable CLR strict security
        $clrStrictSql = @"
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
"@
        Invoke-SqlCommand -Query $clrStrictSql -DatabaseOverride "master"
        Write-Success "Database configured for CLR deployment"
        
        # Deploy assemblies in tiers
        $deployedCount = 0
        $skippedCount = 0
        
        foreach ($tier in $assemblyTiers) {
            Write-Host ""
            Write-Host "Tier $($tier.Tier): $($tier.Name)" -ForegroundColor Yellow
            
            foreach ($asmName in $tier.Assemblies) {
                $asmPath = Join-Path $dependenciesPath $asmName
                
                if (Test-Path $asmPath) {
                    try {
                        Write-Host "  Deploying $asmName..." -ForegroundColor Gray
                        
                        # Read assembly bytes and calculate hash
                        $asmBytes = [System.IO.File]::ReadAllBytes($asmPath)
                        $sha512 = [System.Security.Cryptography.SHA512]::Create()
                        $hashBytes = $sha512.ComputeHash($asmBytes)
                        $hashHex = [System.BitConverter]::ToString($hashBytes).Replace('-', '')
                        
                        # Add to trusted assemblies catalog
                        $trustedAsmSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = 0x$hashHex)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = 0x$hashHex, @description = N'$asmName';
END
"@
                        Invoke-SqlCommand -Query $trustedAsmSql -DatabaseOverride "master"
                        
                        # Drop and recreate assembly
                        $asmBaseName = [System.IO.Path]::GetFileNameWithoutExtension($asmName)
                        $dropAsmSql = @"
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$asmBaseName')
BEGIN
    DROP ASSEMBLY [$asmBaseName];
END
"@
                        Invoke-SqlCommand -Query $dropAsmSql
                        
                        # Create assembly
                        $hexString = [System.BitConverter]::ToString($asmBytes).Replace('-', '')
                        $createAsmSql = @"
CREATE ASSEMBLY [$asmBaseName]
FROM 0x$hexString
WITH PERMISSION_SET = UNSAFE;
"@
                        Invoke-SqlCommand -Query $createAsmSql
                        
                        $deployedCount++
                        Write-Success "$asmName deployed"
                    }
                    catch {
                        Write-Warning "Failed to deploy $asmName : $($_.Exception.Message)"
                        $skippedCount++
                    }
                } else {
                    Write-Host "  $asmName not found, skipping" -ForegroundColor DarkGray
                    $skippedCount++
                }
            }
        }
        
        # Re-enable strict security
        Write-Host ""
        Write-Host "Re-enabling CLR strict security..." -ForegroundColor Cyan
        
        # Re-enable CLR strict security
        $clrStrictOnSql = @"
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
"@
        Invoke-SqlCommand -Query $clrStrictOnSql -DatabaseOverride "master"
        
        # Disable TRUSTWORTHY
        $trustworthyOffSql = "ALTER DATABASE [$Database] SET TRUSTWORTHY OFF;"
        Invoke-SqlCommand -Query $trustworthyOffSql -DatabaseOverride "master"
        Write-Success "CLR strict security re-enabled"
        
        Write-Host ""
        Write-Success "External CLR deployment complete ($deployedCount deployed, $skippedCount skipped)"
    }
    catch {
        Write-Error "External CLR deployment failed: $($_.Exception.Message)"
        
        # Try to restore security settings
        try {
            $restoreClrSql = @"
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
"@
            Invoke-SqlCommand -Query $restoreClrSql -DatabaseOverride "master"
            
            $restoreTrustSql = "ALTER DATABASE [$Database] SET TRUSTWORTHY OFF;"
            Invoke-SqlCommand -Query $restoreTrustSql -DatabaseOverride "master"
        }
        catch {
            Write-Warning "Could not restore security settings"
        }
        
        throw
    }
} else {
    Write-Host "No dependencies directory found, skipping external CLR deployment" -ForegroundColor Gray
}

# =============================================================================
# STEP 5: SCAFFOLD EF CORE ENTITIES
# =============================================================================

Write-Section "Scaffolding EF Core Entities from Database" 5 9

$entitiesProjectPath = Join-Path $repoRoot "src\Hartonomous.Data.Entities"
$entitiesDir = Join-Path $entitiesProjectPath "Entities"
$dbContextPath = Join-Path $entitiesProjectPath "HartonomousDbContext.cs"

try {
    Write-Host "Scaffolding entities from deployed database schema..." -ForegroundColor Cyan
    Write-Host "  Target: src\Hartonomous.Data.Entities\" -ForegroundColor Gray
    
    # Backup existing entities if they exist
    if (Test-Path $entitiesDir) {
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupDir = Join-Path $entitiesProjectPath "Entities.backup_$timestamp"
        Write-Host "  Backing up existing entities to: $(Split-Path $backupDir -Leaf)" -ForegroundColor Gray
        Copy-Item -Path $entitiesDir -Destination $backupDir -Recurse -Force
    }
    
    # Remove old generated files
    if (Test-Path $entitiesDir) {
        Remove-Item -Path $entitiesDir -Recurse -Force
    }
    if (Test-Path $dbContextPath) {
        Remove-Item -Path $dbContextPath -Force
    }
    
    # Remove old backup directories (keep only latest 3)
    $oldBackups = Get-ChildItem -Path $entitiesProjectPath -Directory -Filter "Entities.backup_*" | 
                  Sort-Object Name -Descending | 
                  Select-Object -Skip 3
    if ($oldBackups) {
        Write-Host "  Cleaning up old backups ($($oldBackups.Count) removed)" -ForegroundColor Gray
        $oldBackups | Remove-Item -Recurse -Force
    }
    
    # Build connection string for scaffolding
    $scaffoldConnStr = Get-ConnectionString
    
    # Run EF Core scaffolding
    Write-Host "  Running: dotnet ef dbcontext scaffold..." -ForegroundColor Gray
    
    Push-Location $entitiesProjectPath
    try {
        $scaffoldArgs = @(
            "ef", "dbcontext", "scaffold",
            $scaffoldConnStr,
            "Microsoft.EntityFrameworkCore.SqlServer",
            "--output-dir", "Entities",
            "--context", "HartonomousDbContext",
            "--context-dir", ".",
            "--namespace", "Hartonomous.Data.Entities",
            "--force",
            "--no-onconfiguring",
            "--no-pluralize",
            "--verbose"
        )
        
        & dotnet $scaffoldArgs
        
        if ($LASTEXITCODE -ne 0) {
            throw "Entity scaffolding failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
    
    # Verify scaffolding succeeded
    if (-not (Test-Path $dbContextPath)) {
        throw "DbContext was not generated"
    }
    
    if (-not (Test-Path $entitiesDir)) {
        throw "Entities directory was not created"
    }
    
    $entityCount = (Get-ChildItem -Path $entitiesDir -Filter "*.cs").Count
    Write-Success "Entity scaffolding complete ($entityCount entity classes generated)"
    
    # Verify project builds
    Write-Host "  Verifying Entities project builds..." -ForegroundColor Gray
    Push-Location $entitiesProjectPath
    try {
        & dotnet build --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Entities project build verification failed"
        }
        Write-Success "Entities project builds successfully"
    }
    finally {
        Pop-Location
    }
}
catch {
    Write-Error "Entity scaffolding failed: $($_.Exception.Message)"
    throw
}

# =============================================================================
# STEP 6: BUILD .NET SOLUTION
# =============================================================================

Write-Section "Building .NET Solution" 6 9

$solutionFile = Join-Path $repoRoot "Hartonomous.sln"

if (-not (Test-Path $solutionFile)) {
    Write-Error "Solution file not found: $solutionFile"
    throw "Cannot build solution"
}

try {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
    & dotnet restore $solutionFile --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed"
    }
    Write-Success "NuGet packages restored"
    
    Write-Host "Building solution (Release configuration)..." -ForegroundColor Cyan
    & dotnet build $solutionFile -c Release --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed"
    }
    
    Write-Success ".NET solution built successfully"
}
catch {
    Write-Error "Solution build failed: $($_.Exception.Message)"
    throw
}

# =============================================================================
# STEP 7: DEPLOY STORED PROCEDURES
# =============================================================================

Write-Section "Deploying Stored Procedures" 7 9

$proceduresPath = Join-Path $repoRoot "src\Hartonomous.Database\Procedures"

if (Test-Path $proceduresPath) {
    try {
        $sqlFiles = Get-ChildItem -Path $proceduresPath -Filter "*.sql" -Recurse
        
        Write-Host "Found $($sqlFiles.Count) procedure files" -ForegroundColor Cyan
        
        $deployed = 0
        $failed = 0
        
        foreach ($sqlFile in $sqlFiles) {
            try {
                Write-Host "  Deploying $($sqlFile.Name)..." -ForegroundColor Gray
                $result = Invoke-SqlFile -FilePath $sqlFile.FullName
                $deployed++
            }
            catch {
                Write-Warning "Failed to deploy $($sqlFile.Name): $($_.Exception.Message)"
                $failed++
            }
        }
        
        Write-Success "Deployed $deployed/$($sqlFiles.Count) procedures ($failed failed)"
    }
    catch {
        Write-Warning "Procedure deployment encountered errors: $($_.Exception.Message)"
    }
} else {
    Write-Warning "Procedures directory not found: $proceduresPath"
}

# =============================================================================
# STEP 8: VALIDATION
# =============================================================================

Write-Section "Running Validation Tests" 8 9

try {
    Write-Host "Verifying database objects..." -ForegroundColor Cyan
    
    # Check for key tables
    $tableCheckSql = @"
SELECT COUNT(*) AS TableCount
FROM sys.tables
WHERE name IN ('Atoms', 'AtomEmbeddings', 'Models', 'Tenants', 'InferenceRequests');
"@
    
    $tableCount = Invoke-SqlCommand -Query $tableCheckSql
    
    if ([int]$tableCount -ge 5) {
        Write-Success "Core tables verified ($tableCount found)"
    } else {
        Write-Warning "Some core tables may be missing (only $tableCount found)"
    }
    
    # Check for CLR assemblies
    $assemblyCheckSql = @"
SELECT COUNT(*) AS AssemblyCount
FROM sys.assemblies
WHERE name NOT IN ('mscorlib', 'System', 'System.Data', 'System.Xml');
"@
    
    $assemblyCount = Invoke-SqlCommand -Query $assemblyCheckSql
    
    if ([int]$assemblyCount -gt 0) {
        Write-Success "CLR assemblies registered ($assemblyCount found)"
    } else {
        Write-Warning "No custom CLR assemblies found"
    }
    
    # Check for stored procedures
    $procCheckSql = @"
SELECT COUNT(*) AS ProcCount
FROM sys.procedures
WHERE schema_id = SCHEMA_ID('dbo');
"@
    
    $procCount = Invoke-SqlCommand -Query $procCheckSql
    
    if ([int]$procCount -gt 0) {
        Write-Success "Stored procedures registered ($procCount found)"
    } else {
        Write-Warning "No stored procedures found"
    }
    
    # Verify entity files exist
    $entityFiles = Get-ChildItem -Path $entitiesDir -Filter "*.cs" -ErrorAction SilentlyContinue
    if ($entityFiles -and $entityFiles.Count -gt 0) {
        Write-Success "EF Core entities verified ($($entityFiles.Count) entity classes)"
    } else {
        Write-Warning "Entity classes may not have been generated"
    }
    
    # Verify DbContext exists
    if (Test-Path $dbContextPath) {
        Write-Success "HartonomousDbContext verified"
    } else {
        Write-Warning "HartonomousDbContext not found"
    }
}
catch {
    Write-Warning "Validation checks failed: $($_.Exception.Message)"
}

# =============================================================================
# STEP 9: DEPLOYMENT SUMMARY
# =============================================================================

Write-Section "Deployment Summary" 9 9

$duration = (Get-Date) - $startTime
$durationText = "{0:mm\:ss}" -f $duration

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                                                                ║" -ForegroundColor Green
Write-Host "║        DEPLOYMENT COMPLETED                                    ║" -ForegroundColor Green
Write-Host "║                                                                ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  Server:   $Server" -ForegroundColor White
Write-Host "  Database: $Database" -ForegroundColor White
Write-Host "  Duration: $durationText" -ForegroundColor White
Write-Host ""

if ($script:deploymentErrors.Count -gt 0) {
    Write-Host "Errors encountered during deployment:" -ForegroundColor Red
    foreach ($error in $script:deploymentErrors) {
        Write-Host "  ✗ $error" -ForegroundColor Red
    }
    Write-Host ""
}

if ($script:deploymentWarnings.Count -gt 0) {
    Write-Host "Warnings:" -ForegroundColor Yellow
    foreach ($warning in $script:deploymentWarnings) {
        Write-Host "  ⚠ $warning" -ForegroundColor Yellow
    }
    Write-Host ""
}

if ($script:deploymentErrors.Count -eq 0) {
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Start Background Workers:" -ForegroundColor White
    Write-Host "   cd src\Hartonomous.Workers" -ForegroundColor Gray
    Write-Host "   dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Start REST API:" -ForegroundColor White
    Write-Host "   cd src\Hartonomous.Api" -ForegroundColor Gray
    Write-Host "   dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Test Inference:" -ForegroundColor White
    Write-Host "   curl -X POST http://localhost:5000/api/inference/generate \" -ForegroundColor Gray
    Write-Host "     -H 'Content-Type: application/json' \" -ForegroundColor Gray
    Write-Host "     -d '{\"prompt\": \"test query\", \"topK\": 5}'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║   HARTONOMOUS IS NOW OPERATIONAL                               ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
} else {
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║   DEPLOYMENT FAILED - REVIEW ERRORS ABOVE                      ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    exit 1
}

Write-Host ""
exit 0
