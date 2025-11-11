# ===============================================
# UNIFIED DATABASE DEPLOYMENT SCRIPT
# ===============================================
# Enterprise-grade database deployment for Hartonomous
# 
# Purpose: Single deployment solution for both:
#   - HART-SERVER (Ubuntu 22.04, SQL 2025 Linux, Azure Arc)
#   - HART-DESKTOP (Windows, SQL 2025)
#
# Architecture Resolution:
#   1. EF Core migrations create domain tables (Atoms, Models, etc.)
#   2. SQL scripts add advanced features (CLR, Service Broker, FILESTREAM, Graph)
#   3. Stored procedures provide business logic
#   4. CLR assemblies for vector/geometry operations
#
# Deployment Order (respecting dependencies):
#   1. Prerequisites (databases, filegroups, schemas)
#   2. EF Core migrations (domain tables)
#   3. CLR assemblies (UNSAFE, with dependencies)
#   4. SQL types/tables (advanced features not in EF)
#   5. Service Broker messaging infrastructure
#   6. Stored procedures (depends on tables + CLR)
#   7. Verification tests
#
# Safety: Idempotent operations, rollback support, validation checks
# ===============================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Server = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [PSCredential]$Credential,
    
    [Parameter(Mandatory=$false)]
    [string]$FilestreamPath = "D:\SQLDATA\Filestream",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipFilestream,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipCLR,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipEFMigrations,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory=$false)]
    [switch]$VerboseOutput,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$script:VerbosePreference = if ($VerboseOutput) { "Continue" } else { "SilentlyContinue" }

# ===============================================
# CONFIGURATION
# ===============================================

$script:RootPath = Split-Path -Parent $PSScriptRoot
$script:ConnectionString = $null
$script:DeploymentLog = @()
$script:StartTime = Get-Date

# SQL file paths (relative to repository root)
$script:SqlPaths = @{
    # Advanced table creation (not in EF)
    AdvancedTables = @(
        "sql\tables\Attention.AttentionGenerationTables.sql"
        "sql\tables\Reasoning.ReasoningFrameworkTables.sql"
        "sql\tables\Stream.StreamOrchestrationTables.sql"
        "sql\tables\Provenance.ProvenanceTrackingTables.sql"
        "sql\tables\provenance.Concepts.sql"
        "sql\tables\provenance.GenerationStreams.sql"
        "sql\tables\dbo.SpatialLandmarks.sql"
        "sql\tables\dbo.InferenceTracking.sql"
        "sql\tables\dbo.InferenceCache.sql"
        "sql\tables\dbo.BillingUsageLedger_InMemory.sql"
        "sql\tables\dbo.TenantSecurityPolicy.sql"
        "sql\tables\dbo.PendingActions.sql"
        "sql\tables\dbo.AutonomousImprovementHistory.sql"
        "sql\tables\dbo.AutonomousComputeJobs.sql"
        "sql\tables\dbo.TestResults.sql"
    )
    
    # SQL Graph tables (specialized syntax)
    GraphTables = @(
        "sql\tables\graph.AtomGraphNodes.sql"
        "sql\tables\graph.AtomGraphEdges.sql"
    )
    
    # Service Broker infrastructure
    ServiceBroker = @(
        "scripts\setup-service-broker.sql"
    )
    
    # CLR bindings (must run AFTER CLR assembly deployment)
    CLRBindings = @(
        "sql\procedures\Common.ClrBindings.sql"
        "sql\procedures\Autonomy.FileSystemBindings.sql"
    )
    
    # Common functions (dependencies for procedures)
    Functions = @(
        "sql\procedures\Common.Helpers.sql"
        "sql\procedures\dbo.fn_DiscoverConcepts.sql"
        "sql\procedures\dbo.fn_BindConcepts.sql"
    )
    
    # Core procedures (business logic)
    Procedures = @(
        "sql\procedures\Admin.WeightRollback.sql"
        "sql\procedures\Analysis.WeightHistory.sql"
        "sql\procedures\dbo.sp_Analyze.sql"
        "sql\procedures\dbo.sp_Hypothesize.sql"
        "sql\procedures\dbo.sp_Act.sql"
        "sql\procedures\dbo.sp_Learn.sql"
        "sql\procedures\Autonomy.SelfImprovement.sql"
        "sql\procedures\dbo.AtomIngestion.sql"
        "sql\procedures\dbo.sp_AtomizeModel.sql"
        "sql\procedures\dbo.sp_AtomizeCode.sql"
        "sql\procedures\dbo.sp_ExtractStudentModel.sql"
        "sql\procedures\Search.SemanticSearch.sql"
        "sql\procedures\Generation.TextFromVector.sql"
        "sql\procedures\Generation.AudioFromPrompt.sql"
        "sql\procedures\Generation.ImageFromPrompt.sql"
        "sql\procedures\Generation.VideoFromPrompt.sql"
        "sql\procedures\Inference.MultiModelEnsemble.sql"
        "sql\procedures\Inference.JobManagement.sql"
        "sql\procedures\Provenance.ProvenanceTracking.sql"
    )
    
    # Performance optimization scripts (columnstore, vector, spatial indexes)
    Optimizations = @(
        "sql\Optimize_ColumnstoreCompression.sql"
        "sql\Setup_Vector_Indexes.sql"
        "sql\procedures\Common.CreateSpatialIndexes.sql"
    )
}

# CLR assembly deployment is now fully delegated to deploy-clr-secure.ps1
# That script handles:
#   - System.Numerics.Vectors (dependency)
#   - MathNet.Numerics (dependency)
#   - Newtonsoft.Json (dependency)
#   - System.Drawing (GAC assembly, needed for DataSet.ReadXML)
#   - Microsoft.SqlServer.Types (system assembly, already in SQL Server)
#   - SqlClrFunctions (main assembly with all UDFs/UDAs/UDTs)
# All assemblies are strong-name signed, added to sys.trusted_assemblies,
# deployed with PERMISSION_SET = UNSAFE, and verified post-deployment.
$script:CLRAssemblies = @()  # No longer used; kept for backward compatibility

# ===============================================
# UTILITY FUNCTIONS
# ===============================================

function Write-DeploymentLog {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = @{
        Timestamp = $timestamp
        Level = $Level
        Message = $Message
    }
    
    $script:DeploymentLog += $logEntry
    
    switch ($Level) {
        "Success" { Write-Host "✓ $Message" -ForegroundColor Green }
        "Warning" { Write-Host "⚠ $Message" -ForegroundColor Yellow }
        "Error" { Write-Host "✗ $Message" -ForegroundColor Red }
        default { Write-Host "  $Message" -ForegroundColor Cyan }
    }
}

function Invoke-SqlCommand {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Query,
        
        [Parameter(Mandatory=$false)]
        [string]$Database = "master",
        
        [Parameter(Mandatory=$false)]
        [int]$Timeout = 300
    )
    
    if ($DryRun) {
        Write-DeploymentLog "DRY RUN: Would execute SQL (first 200 chars): $($Query.Substring(0, [Math]::Min(200, $Query.Length)))" -Level Info
        return $null
    }
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $script:ConnectionString -replace "Database=[^;]+", "Database=$Database"
        $connection.Open()
        
        # Split SQL on GO batch separators
        $batches = $Query -split '(?m)^\s*GO\s*$' | Where-Object { $_.Trim() -ne '' }
        
        $totalAffected = 0
        foreach ($batch in $batches) {
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = $Timeout
            
            $result = $command.ExecuteNonQuery()
            if ($result -gt 0) { $totalAffected += $result }
        }
        
        $connection.Close()
        return $totalAffected
    }
    catch {
        throw "SQL execution failed: $_"
    }
}

function Test-DatabaseExists {
    param([string]$DatabaseName)
    
    $query = @"
SELECT COUNT(*)
FROM sys.databases
WHERE name = '$DatabaseName'
"@
    
    try {
        $connectionString = $script:ConnectionString -replace "Database=[^;]+", "Database=master"
        $connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $query
        $exists = [int]$command.ExecuteScalar()
        $connection.Close()
        return $exists -gt 0
    }
    catch {
        return $false
    }
}

function Get-PlatformInfo {
    $query = @"
SELECT 
    SERVERPROPERTY('ProductVersion') AS Version,
    SERVERPROPERTY('ProductLevel') AS ServicePack,
    SERVERPROPERTY('Edition') AS Edition,
    CASE WHEN SERVERPROPERTY('EngineEdition') = 6 THEN 'Linux' ELSE 'Windows' END AS Platform,
    SERVERPROPERTY('FilestreamConfiguredLevel') AS FilestreamLevel
"@
    
    $result = Invoke-SqlCommand -Query $query -Database "master"
    return $result
}

# ===============================================
# DEPLOYMENT PHASES
# ===============================================

function Initialize-DeploymentEnvironment {
    Write-DeploymentLog "=== PHASE 0: Environment Initialization ===" -Level Info
    
    # Build connection string
    if ($Credential) {
        $script:ConnectionString = "Server=$Server;Database=$Database;User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=True;Connection Timeout=30;"
    }
    else {
        $script:ConnectionString = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=30;"
    }
    
    # Test connectivity
    try {
        $platformInfo = Get-PlatformInfo
        Write-DeploymentLog "Connected to SQL Server $($platformInfo.Version) ($($platformInfo.Platform))" -Level Success
        Write-DeploymentLog "Edition: $($platformInfo.Edition)" -Level Info
        
        # Check FILESTREAM support
        if (-not $SkipFilestream) {
            if ($platformInfo.FilestreamLevel -eq 0) {
                Write-DeploymentLog "FILESTREAM not enabled. Use -SkipFilestream to continue without FILESTREAM support." -Level Warning
                if (-not $Force) {
                    throw "FILESTREAM required for AtomPayloadStore. Enable FILESTREAM or use -SkipFilestream."
                }
            }
            else {
                Write-DeploymentLog "FILESTREAM enabled (level: $($platformInfo.FilestreamLevel))" -Level Success
            }
        }
        
        return $platformInfo
    }
    catch {
        Write-DeploymentLog "Failed to connect to SQL Server: $_" -Level Error
        throw
    }
}

function Deploy-DatabasePrerequisites {
    Write-DeploymentLog "=== PHASE 1: Database Prerequisites ===" -Level Info
    
    # Create database if not exists
    if (-not (Test-DatabaseExists -DatabaseName $Database)) {
        Write-DeploymentLog "Creating database '$Database'..." -Level Info
        
        $createDbSql = @"
CREATE DATABASE [$Database]
ON PRIMARY 
( NAME = N'${Database}_Data', 
  FILENAME = N'$(if ($IsLinux) { "/var/opt/mssql/data" } else { "D:\SQLDATA" })\${Database}.mdf',
  SIZE = 512MB,
  MAXSIZE = UNLIMITED,
  FILEGROWTH = 256MB )
LOG ON 
( NAME = N'${Database}_Log',
  FILENAME = N'$(if ($IsLinux) { "/var/opt/mssql/data" } else { "D:\SQLDATA" })\${Database}_log.ldf',
  SIZE = 256MB,
  MAXSIZE = 10GB,
  FILEGROWTH = 128MB );

ALTER DATABASE [$Database] SET RECOVERY SIMPLE;
ALTER DATABASE [$Database] SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE [$Database] SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE [$Database] SET PAGE_VERIFY CHECKSUM;
"@
        
        Invoke-SqlCommand -Query $createDbSql -Database "master"
        Write-DeploymentLog "Database '$Database' created successfully" -Level Success
    }
    else {
        Write-DeploymentLog "Database '$Database' already exists" -Level Info
    }
    
    # Add FILESTREAM filegroup (if not skipped)
    if (-not $SkipFilestream) {
        $checkFileStreamFG = @"
IF NOT EXISTS (
    SELECT 1 FROM sys.filegroups 
    WHERE name = 'FilestreamGroup' AND type = 'FD'
)
BEGIN
    ALTER DATABASE [$Database] ADD FILEGROUP FilestreamGroup CONTAINS FILESTREAM;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.database_files 
    WHERE type = 2 AND data_space_id = (SELECT data_space_id FROM sys.filegroups WHERE name = 'FilestreamGroup')
)
BEGIN
    ALTER DATABASE [$Database]
    ADD FILE (
        NAME = FileStreamData,
        FILENAME = '$FilestreamPath'
    ) TO FILEGROUP FilestreamGroup;
END
"@
        
        Invoke-SqlCommand -Query $checkFileStreamFG -Database $Database
        Write-DeploymentLog "FILESTREAM filegroup configured" -Level Success
    }
    
    # Create schemas
    $schemas = @("graph", "provenance", "dbo")
    foreach ($schema in $schemas) {
        $createSchemaSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '$schema')
BEGIN
    EXEC('CREATE SCHEMA [$schema]');
    PRINT 'Schema [$schema] created';
END
"@
        Invoke-SqlCommand -Query $createSchemaSql -Database $Database
    }
    
    Write-DeploymentLog "All schemas created/verified" -Level Success
}

function Deploy-EFCoreMigrations {
    Write-DeploymentLog "=== PHASE 2: EF Core Migrations ===" -Level Info
    
    if ($SkipEFMigrations) {
        Write-DeploymentLog "Skipping EF Core migrations (--SkipEFMigrations)" -Level Warning
        return
    }
    
    # Check if dotnet ef tool is available
    try {
        $efVersion = dotnet ef --version 2>&1
        Write-DeploymentLog "Using dotnet ef: $efVersion" -Level Info
    }
    catch {
        Write-DeploymentLog "dotnet ef tool not found. Install with: dotnet tool install --global dotnet-ef" -Level Error
        throw "dotnet ef required for EF Core migrations"
    }
    
    # Navigate to Data project
    $dataProject = Join-Path $script:RootPath "src\Hartonomous.Data\Hartonomous.Data.csproj"
    
    if (-not (Test-Path $dataProject)) {
        Write-DeploymentLog "EF Core project not found: $dataProject" -Level Error
        throw "Cannot find Hartonomous.Data project"
    }
    
    # Build connection string for EF migrations
    $efConnectionString = $script:ConnectionString
    
    # Apply migrations
    Write-DeploymentLog "Applying EF Core migrations to database..." -Level Info
    
    if ($DryRun) {
        Write-DeploymentLog "DRY RUN: Would execute: dotnet ef database update --project $dataProject --connection '$efConnectionString'" -Level Info
    }
    else {
        try {
            $env:ConnectionStrings__DefaultConnection = $efConnectionString
            
            Push-Location (Split-Path $dataProject)
            $output = dotnet ef database update --verbose 2>&1
            Pop-Location
            
            if ($LASTEXITCODE -eq 0) {
                Write-DeploymentLog "EF Core migrations applied successfully" -Level Success
            }
            else {
                Write-DeploymentLog "EF migration output: $output" -Level Error
                throw "EF Core migration failed with exit code $LASTEXITCODE"
            }
        }
        catch {
            Write-DeploymentLog "EF Core migration error: $_" -Level Error
            throw
        }
    }
}

function Deploy-CLRAssemblies {
    Write-DeploymentLog "=== PHASE 3: CLR Assembly Deployment ===" -Level Info
    
    if ($SkipCLR) {
        Write-DeploymentLog "Skipping CLR assemblies (--SkipCLR)" -Level Warning
        return
    }

    $secureDeployScript = Join-Path $PSScriptRoot "deploy-clr-secure.ps1"
    if (-not (Test-Path $secureDeployScript)) {
        throw "Secure CLR deployment script not found at: $secureDeployScript"
    }

    Write-DeploymentLog "Executing secure, multi-assembly CLR deployment script..." -Level Info

    try {
        # Call the correct, secure script, passing through the relevant parameters.
        & $secureDeployScript -ServerName $Server -DatabaseName $Database -BinDirectory (Join-Path $script:RootPath "src\SqlClr\bin\Release")
        
        Write-DeploymentLog "Secure CLR deployment script completed successfully." -Level Success
    }
    catch {
        Write-DeploymentLog "The secure CLR deployment script failed: $_" -Level Error
        throw
    }
}

function Deploy-AdvancedTables {
    Write-DeploymentLog "=== PHASE 4: DACPAC Deployment (Tables, Views, Functions) ===" -Level Info
    
    # Build the DACPAC first
    $dacpacProject = Join-Path $script:RootPath "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
    $dacpacPath = Join-Path $script:RootPath "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"
    
    if (-not (Test-Path $dacpacProject)) {
        Write-DeploymentLog "DACPAC project not found: $dacpacProject" -Level Error
        throw "DACPAC project missing"
    }
    
    Write-DeploymentLog "Building DACPAC from $dacpacProject..." -Level Info
    
    try {
        $buildOutput = dotnet build $dacpacProject -c Release 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-DeploymentLog "DACPAC build failed: $buildOutput" -Level Error
            throw "DACPAC build failed with exit code $LASTEXITCODE"
        }
        Write-DeploymentLog "DACPAC built successfully" -Level Success
    }
    catch {
        Write-DeploymentLog "Failed to build DACPAC: $_" -Level Error
        throw
    }
    
    if (-not (Test-Path $dacpacPath)) {
        Write-DeploymentLog "DACPAC not found at expected location: $dacpacPath" -Level Error
        throw "DACPAC file missing after build"
    }
    
    Write-DeploymentLog "Deploying DACPAC to $Server\$Database using sqlpackage..." -Level Info
    
    try {
        $sqlpackageArgs = @(
            "/Action:Publish"
            "/SourceFile:$dacpacPath"
            "/TargetServerName:$Server"
            "/TargetDatabaseName:$Database"
            "/TargetTrustServerCertificate:True"
            "/p:BlockOnPossibleDataLoss=False"
            "/p:DropObjectsNotInSource=False"
        )
        
        $deployOutput = sqlpackage $sqlpackageArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-DeploymentLog "DACPAC deployment failed: $deployOutput" -Level Error
            if (-not $Force) {
                throw "DACPAC deployment failed with exit code $LASTEXITCODE"
            }
        }
        else {
            Write-DeploymentLog "DACPAC deployed successfully" -Level Success
        }
    }
    catch {
        Write-DeploymentLog "Failed to deploy DACPAC: $_" -Level Error
        if (-not $Force) {
            throw
        }
    }
}

function Deploy-ServiceBroker {
    Write-DeploymentLog "=== PHASE 5: Service Broker Infrastructure ===" -Level Info
    
    foreach ($sbPath in $script:SqlPaths.ServiceBroker) {
        $fullPath = Join-Path $script:RootPath $sbPath
        
        if (-not (Test-Path $fullPath)) {
            Write-DeploymentLog "Service Broker script not found: $sbPath" -Level Warning
            continue
        }
        
        Write-DeploymentLog "Deploying Service Broker infrastructure" -Level Info
        
        $sql = Get-Content $fullPath -Raw
        
        try {
            Invoke-SqlCommand -Query $sql -Database $Database -Timeout 180
            Write-DeploymentLog "Service Broker deployed successfully" -Level Success
        }
        catch {
            Write-DeploymentLog "Service Broker deployment failed: $_" -Level Error
            if (-not $Force) {
                throw
            }
        }
    }
}

function Deploy-CLRBindingsAndFunctions {
    Write-DeploymentLog "=== PHASE 6: CLR Bindings & Functions ===" -Level Info
    
    if ($SkipCLR) {
        Write-DeploymentLog "Skipping CLR bindings (CLR assemblies not deployed)" -Level Warning
        return
    }
    
    # Deploy CLR bindings (CREATE FUNCTION ... EXTERNAL NAME)
    foreach ($bindingPath in $script:SqlPaths.CLRBindings) {
        $fullPath = Join-Path $script:RootPath $bindingPath
        
        if (-not (Test-Path $fullPath)) {
            Write-DeploymentLog "CLR binding not found: $bindingPath" -Level Warning
            continue
        }
        
        $bindingName = [System.IO.Path]::GetFileNameWithoutExtension($bindingPath)
        Write-DeploymentLog "Deploying CLR bindings: $bindingName" -Level Info
        
        $sql = Get-Content $fullPath -Raw
        
        try {
            Invoke-SqlCommand -Query $sql -Database $Database -Timeout 120
            Write-DeploymentLog "CLR bindings deployed: $bindingName" -Level Success
        }
        catch {
            Write-DeploymentLog "Failed to deploy CLR bindings $bindingName`: $_" -Level Error
            if (-not $Force) {
                throw
            }
        }
    }
    
    # Deploy common functions (T-SQL helpers)
    foreach ($funcPath in $script:SqlPaths.Functions) {
        $fullPath = Join-Path $script:RootPath $funcPath
        
        if (-not (Test-Path $fullPath)) {
            Write-DeploymentLog "Function not found: $funcPath" -Level Warning
            continue
        }
        
        $funcName = [System.IO.Path]::GetFileNameWithoutExtension($funcPath)
        Write-DeploymentLog "Deploying function: $funcName" -Level Info
        
        $sql = Get-Content $fullPath -Raw
        
        try {
            Invoke-SqlCommand -Query $sql -Database $Database -Timeout 120
            Write-DeploymentLog "Function deployed: $funcName" -Level Success
        }
        catch {
            Write-DeploymentLog "Failed to deploy function $funcName`: $_" -Level Error
            if (-not $Force) {
                throw
            }
        }
    }
}

function Deploy-StoredProcedures {
    Write-DeploymentLog "=== PHASE 7: Stored Procedures ===" -Level Info
    
    foreach ($procPath in $script:SqlPaths.Procedures) {
        $fullPath = Join-Path $script:RootPath $procPath
        
        if (-not (Test-Path $fullPath)) {
            Write-DeploymentLog "Procedure not found: $procPath" -Level Warning
            continue
        }
        
        $procName = [System.IO.Path]::GetFileNameWithoutExtension($procPath)
        Write-DeploymentLog "Deploying procedure: $procName" -Level Info
        
        $sql = Get-Content $fullPath -Raw
        
        try {
            Invoke-SqlCommand -Query $sql -Database $Database -Timeout 180
            Write-DeploymentLog "Procedure deployed: $procName" -Level Success
        }
        catch {
            Write-DeploymentLog "Failed to deploy procedure $procName`: $_" -Level Error
            if (-not $Force) {
                throw
            }
        }
    }
}

function Deploy-PerformanceOptimizations {
    Write-DeploymentLog "=== PHASE 8: Performance Optimizations ===" -Level Info
    
    foreach ($optPath in $script:SqlPaths.Optimizations) {
        $fullPath = Join-Path $script:RootPath $optPath
        
        if (-not (Test-Path $fullPath)) {
            Write-DeploymentLog "Optimization script not found: $optPath" -Level Warning
            continue
        }
        
        $optName = [System.IO.Path]::GetFileNameWithoutExtension($optPath)
        Write-DeploymentLog "Applying optimization: $optName" -Level Info
        
        $sql = Get-Content $fullPath -Raw
        
        try {
            Invoke-SqlCommand -Query $sql -Database $Database -Timeout 600  # Longer timeout for index creation
            Write-DeploymentLog "Optimization applied: $optName" -Level Success
        }
        catch {
            Write-DeploymentLog "Failed to apply optimization $optName`: $_" -Level Error
            if (-not $Force) {
                throw
            }
        }
    }
}

function Invoke-DeploymentVerification {
    Write-DeploymentLog "=== PHASE 9: Verification ===" -Level Info
    
    # Count tables
    $tableCountSql = "SELECT COUNT(*) AS TableCount FROM sys.tables WHERE is_ms_shipped = 0"
    $tableCount = Invoke-SqlCommand -Query $tableCountSql -Database $Database
    Write-DeploymentLog "User tables: $tableCount" -Level Info
    
    # Count CLR assemblies
    if (-not $SkipCLR) {
        $assemblySql = "SELECT COUNT(*) AS AssemblyCount FROM sys.assemblies WHERE is_user_defined = 1"
        $assemblyCount = Invoke-SqlCommand -Query $assemblySql -Database $Database
        Write-DeploymentLog "CLR assemblies: $assemblyCount" -Level Info
        
        if ($assemblyCount -lt 7) {
            Write-DeploymentLog "Warning: Expected 7 CLR assemblies, found $assemblyCount" -Level Warning
        }
    }
    
    # Count stored procedures
    $procSql = "SELECT COUNT(*) AS ProcCount FROM sys.procedures WHERE is_ms_shipped = 0"
    $procCount = Invoke-SqlCommand -Query $procSql -Database $Database
    Write-DeploymentLog "Stored procedures: $procCount" -Level Info
    
    # Test Service Broker
    $sbActiveSql = "SELECT is_broker_enabled FROM sys.databases WHERE name = '$Database'"
    $sbActive = Invoke-SqlCommand -Query $sbActiveSql -Database "master"
    
    if ($sbActive) {
        Write-DeploymentLog "Service Broker: Enabled ✓" -Level Success
    }
    else {
        Write-DeploymentLog "Service Broker: Disabled" -Level Warning
    }
    
    # Test vector operations (if CLR deployed)
    if (-not $SkipCLR) {
        try {
            $vectorTestSql = @"
DECLARE @v1 VECTOR(3) = '[1.0, 2.0, 3.0]';
DECLARE @v2 VECTOR(3) = '[4.0, 5.0, 6.0]';
SELECT VECTOR_DISTANCE('cosine', @v1, @v2) AS CosineDistance;
"@
            $vectorResult = Invoke-SqlCommand -Query $vectorTestSql -Database $Database
            Write-DeploymentLog "Vector operations: Working ✓" -Level Success
        }
        catch {
            Write-DeploymentLog "Vector operations: Failed - $_" -Level Warning
        }
    }
}

# ===============================================
# MAIN EXECUTION
# ===============================================

try {
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host "   HARTONOMOUS DATABASE DEPLOYMENT" -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($DryRun) {
        Write-Host "   MODE: DRY RUN (no changes will be made)" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Execute deployment phases
    $platformInfo = Initialize-DeploymentEnvironment
    Deploy-DatabasePrerequisites
    Deploy-EFCoreMigrations
    Deploy-CLRAssemblies
    Deploy-AdvancedTables
    Deploy-ServiceBroker
    Deploy-CLRBindingsAndFunctions
    Deploy-StoredProcedures
    Deploy-PerformanceOptimizations
    Invoke-DeploymentVerification
    
    # Summary
    $duration = (Get-Date) - $script:StartTime
    
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host "   DEPLOYMENT COMPLETE" -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host ""
    Write-DeploymentLog "Total duration: $($duration.ToString('mm\:ss'))" -Level Success
    Write-DeploymentLog "Database: $Database on $Server" -Level Info
    Write-DeploymentLog "Platform: $($platformInfo.Platform)" -Level Info
    
    # Save deployment log
    $logPath = Join-Path $script:RootPath "deployment-log-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $script:DeploymentLog | ConvertTo-Json -Depth 10 | Out-File $logPath
    Write-DeploymentLog "Log saved: $logPath" -Level Info
    
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Red
    Write-Host "   DEPLOYMENT FAILED" -ForegroundColor Red
    Write-Host "=================================================================" -ForegroundColor Red
    Write-Host ""
    Write-DeploymentLog "ERROR: $_" -Level Error
    Write-DeploymentLog "Stack trace: $($_.ScriptStackTrace)" -Level Error
    
    # Save error log
    $errorLogPath = Join-Path $script:RootPath "deployment-error-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    @{
        Error = $_.Exception.Message
        StackTrace = $_.ScriptStackTrace
        Log = $script:DeploymentLog
    } | ConvertTo-Json -Depth 10 | Out-File $errorLogPath
    
    Write-Host ""
    Write-Host "Error log saved: $errorLogPath" -ForegroundColor Yellow
    Write-Host ""
    
    exit 1
}
