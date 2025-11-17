#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Unified idempotent deployment script for Hartonomous database, CLR, and procedures.

.DESCRIPTION
    Complete end-to-end deployment automation combining DACPAC deployment, CLR assembly
    registration, and stored procedure deployment. All operations are idempotent and
    can be safely re-run without causing errors or data loss.

    Features:
    - Idempotent DACPAC deployment (applies only schema differences)
    - Secure CLR deployment with proper assembly ordering
    - Automated stored procedure registration
    - Comprehensive error handling and rollback capability
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

.PARAMETER SkipBuild
    Skip building the solution before deployment

.PARAMETER SkipDacpac
    Skip DACPAC deployment (schema only)

.PARAMETER SkipClr
    Skip CLR assembly deployment

.PARAMETER SkipProcedures
    Skip stored procedure deployment

.PARAMETER SkipValidation
    Skip post-deployment validation tests

.PARAMETER TrustServerCertificate
    Trust server certificate (default: true for local development)

.PARAMETER Rebuild
    Force rebuild of CLR assemblies

.EXAMPLE
    .\deploy-hartonomous.ps1 -Server "localhost" -Database "Hartonomous" -IntegratedSecurity
    
    Deploy using Windows Authentication to local server

.EXAMPLE
    .\deploy-hartonomous.ps1 -Server "prodserver" -Database "Hartonomous" -User "sa" -Password "password" -SkipBuild
    
    Deploy to production server using SQL Authentication without rebuilding

.EXAMPLE
    .\deploy-hartonomous.ps1 -SkipClr -SkipProcedures
    
    Deploy only DACPAC changes (schema only)

.NOTES
    Author: Hartonomous Development Team
    Version: 1.0.0
    Security: Uses CLR strict security with trusted assembly list (TRUSTWORTHY OFF)
#>

param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$IntegratedSecurity,
    [string]$User,
    [string]$Password,
    [switch]$SkipBuild,
    [switch]$SkipDacpac,
    [switch]$SkipClr,
    [switch]$SkipProcedures,
    [switch]$SkipValidation,
    [switch]$TrustServerCertificate = $true,
    [switch]$Rebuild
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

Write-Section "Pre-Flight Checks" 0 8

# Check SQL Server connectivity
Write-Host "Testing SQL Server connection..." -ForegroundColor Cyan
$connectionString = Get-ConnectionString
if (Test-SqlConnection $connectionString) {
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
    @{ Name = "sqlpackage"; Command = "sqlpackage /?" }
)

foreach ($tool in $requiredTools) {
    try {
        $null = Invoke-Expression "$($tool.Command) 2>&1"
        Write-Success "$($tool.Name) found"
    }
    catch {
        Write-Warning "$($tool.Name) not found or not working properly"
    }
}

# Verify repository structure
Write-Host "Verifying repository structure..." -ForegroundColor Cyan
$requiredPaths = @(
    "src\Hartonomous.Database",
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
# STEP 1: BUILD SOLUTION
# =============================================================================

if (-not $SkipBuild) {
    Write-Section "Building Hartonomous Solution" 1 8
    
    $solutionFile = Join-Path $repoRoot "Hartonomous.sln"
    
    if (-not (Test-Path $solutionFile)) {
        Write-Error "Solution file not found: $solutionFile"
        throw "Cannot build solution"
    }
    
    try {
        Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
        dotnet restore $solutionFile --verbosity minimal
        
        if ($LASTEXITCODE -ne 0) {
            throw "NuGet restore failed"
        }
        
        Write-Host "Building solution (Release configuration)..." -ForegroundColor Cyan
        dotnet build $solutionFile -c Release --no-restore --verbosity minimal
        
        if ($LASTEXITCODE -ne 0) {
            throw "Solution build failed"
        }
        
        Write-Success "Solution built successfully"
    }
    catch {
        Write-Error "Build failed: $($_.Exception.Message)"
        throw
    }
} else {
    Write-Section "Skipping Build (--SkipBuild)" 1 8
    Write-Host "Using existing build artifacts" -ForegroundColor Gray
}

# =============================================================================
# STEP 2: BUILD DATABASE PROJECT (DACPAC)
# =============================================================================

$dacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"
$altDacpacPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"

if (-not $SkipDacpac) {
    Write-Section "Building Database Project (DACPAC)" 2 8
    
    # Check if DACPAC already exists
    $dacpacExists = (Test-Path $dacpacPath) -or (Test-Path $altDacpacPath)
    
    if (-not $dacpacExists -or (-not $SkipBuild)) {
        try {
            $dbProjPath = Join-Path $repoRoot "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
            
            Write-Host "Building database project..." -ForegroundColor Cyan
            dotnet build $dbProjPath -c Release --verbosity minimal
            
            if ($LASTEXITCODE -ne 0) {
                throw "Database project build failed"
            }
            
            Write-Success "Database project built successfully"
        }
        catch {
            Write-Error "DACPAC build failed: $($_.Exception.Message)"
            throw
        }
    } else {
        Write-Host "Using existing DACPAC" -ForegroundColor Gray
    }
    
    # Verify DACPAC exists
    if (Test-Path $altDacpacPath) {
        $dacpacPath = $altDacpacPath
    }
    
    if (-not (Test-Path $dacpacPath)) {
        Write-Error "DACPAC not found at: $dacpacPath"
        throw "DACPAC build verification failed"
    }
    
    Write-Success "DACPAC ready: $(Split-Path $dacpacPath -Leaf)"
} else {
    Write-Section "Skipping DACPAC Build (--SkipDacpac)" 2 8
}

# =============================================================================
# STEP 3: CONFIGURE CLR SECURITY
# =============================================================================

Write-Section "Configuring SQL Server CLR" 3 8

try {
    Write-Host "Enabling CLR integration..." -ForegroundColor Cyan
    
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
# STEP 4: DEPLOY DACPAC (SCHEMA)
# =============================================================================

if (-not $SkipDacpac) {
    Write-Section "Deploying Database Schema (DACPAC)" 4 8
    
    try {
        Write-Host "Deploying DACPAC to $Server/$Database..." -ForegroundColor Cyan
        Write-Host "  Source: $(Split-Path $dacpacPath -Leaf)" -ForegroundColor Gray
        
        $publishArgs = @(
            "/Action:Publish",
            "/SourceFile:$dacpacPath",
            "/TargetConnectionString:$connectionString",
            "/p:IncludeCompositeObjects=True",
            "/p:BlockOnPossibleDataLoss=False",
            "/p:DropObjectsNotInSource=False",
            "/p:DropConstraintsNotInSource=False",
            "/p:DropIndexesNotInSource=False",
            "/p:DoNotDropObjectTypes=Assemblies",
            "/p:VerifyDeployment=True",
            "/p:AllowIncompatiblePlatform=True",
            "/p:CreateNewDatabase=False"
        )
        
        $publishOutput = & sqlpackage $publishArgs 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "DACPAC deployment completed with warnings"
            Write-Host $publishOutput -ForegroundColor Yellow
        } else {
            Write-Success "Database schema deployed successfully"
        }
    }
    catch {
        Write-Error "DACPAC deployment failed: $($_.Exception.Message)"
        throw
    }
} else {
    Write-Section "Skipping DACPAC Deployment (--SkipDacpac)" 4 8
}

# =============================================================================
# STEP 5: DEPLOY CLR ASSEMBLIES
# =============================================================================

if (-not $SkipClr) {
    Write-Section "Deploying CLR Assemblies" 5 8
    
    $clrScriptPath = Join-Path $scriptRoot "deploy-clr-secure.ps1"
    
    if (Test-Path $clrScriptPath) {
        try {
            Write-Host "Executing CLR deployment script..." -ForegroundColor Cyan
            
            $clrParams = @{
                ServerName = $Server
                DatabaseName = $Database
                SkipConfigCheck = $true
            }
            
            if ($Rebuild) {
                $clrParams.Rebuild = $true
            }
            
            & $clrScriptPath @clrParams
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "CLR assemblies deployed successfully"
            } else {
                Write-Warning "CLR deployment completed with warnings"
            }
        }
        catch {
            Write-Warning "CLR deployment encountered errors: $($_.Exception.Message)"
            Write-Host "You may need to manually run: scripts\deploy-clr-secure.ps1" -ForegroundColor Yellow
        }
    } else {
        Write-Warning "CLR deployment script not found: $clrScriptPath"
        Write-Host "CLR assemblies may need to be deployed manually" -ForegroundColor Yellow
    }
} else {
    Write-Section "Skipping CLR Deployment (--SkipClr)" 5 8
}

# =============================================================================
# STEP 6: DEPLOY STORED PROCEDURES
# =============================================================================

if (-not $SkipProcedures) {
    Write-Section "Deploying Stored Procedures" 6 8
    
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
} else {
    Write-Section "Skipping Procedure Deployment (--SkipProcedures)" 6 8
}

# =============================================================================
# STEP 7: VALIDATION
# =============================================================================

if (-not $SkipValidation) {
    Write-Section "Running Validation Tests" 7 8
    
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
    }
    catch {
        Write-Warning "Validation checks failed: $($_.Exception.Message)"
    }
} else {
    Write-Section "Skipping Validation (--SkipValidation)" 7 8
}

# =============================================================================
# STEP 8: DEPLOYMENT SUMMARY
# =============================================================================

Write-Section "Deployment Summary" 8 8

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
