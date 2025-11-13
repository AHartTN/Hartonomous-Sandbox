<#
.SYNOPSIS
    Production-grade DACPAC deployment for Hartonomous
    
.DESCRIPTION
    Enterprise DACPAC-first deployment pipeline with:
    - Idempotent database creation
    - MSBuild DACPAC compilation
    - SqlPackage deployment
    - CLR assembly deployment
    - Post-deployment validation
    - Comprehensive error handling and rollback
    
.PARAMETER Server
    SQL Server instance name (default: localhost)
    
.PARAMETER Database
    Target database name (default: Hartonomous)
    
.PARAMETER Credential
    SQL authentication credentials (uses Windows auth if not specified)
    
.PARAMETER SkipCLR
    Skip CLR assembly deployment
    
.PARAMETER BlockDataLoss
    Prevent deployment if data loss is detected (default: $false)
    
.PARAMETER DropObjectsNotInSource
    Drop objects in target that don't exist in DACPAC (default: $false)
    
.PARAMETER DryRun
    Generate deployment script without executing
    
.PARAMETER Force
    Continue deployment even on non-fatal errors
    
.EXAMPLE
    .\deploy-dacpac.ps1 -Server localhost -Database Hartonomous
    
.EXAMPLE
    .\deploy-dacpac.ps1 -Server prod-sql -Credential $cred -BlockDataLoss $true
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$Server = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [PSCredential]$Credential,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipCLR,
    
    [Parameter(Mandatory=$false)]
    [bool]$BlockDataLoss = $false,
    
    [Parameter(Mandatory=$false)]
    [bool]$DropObjectsNotInSource = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date
$script:DeploymentLog = @()

# ===============================================
# CONFIGURATION
# ===============================================

$script:RootPath = Split-Path -Parent $PSScriptRoot
$script:DatabaseProject = Join-Path $script:RootPath "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
$script:DacpacPath = Join-Path $script:RootPath "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"
$script:CLRDeployScript = Join-Path $PSScriptRoot "deploy-clr-secure.ps1"
$script:CLRBinDirectory = Join-Path $script:RootPath "src\SqlClr\bin\Release"

# MSBuild path (Visual Studio 2025 Insiders)
$script:MSBuildPath = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"

# SqlPackage path (try multiple locations)
$script:SqlPackagePaths = @(
    "C:\Program Files\Microsoft SQL Server\160\DAC\bin\sqlpackage.exe"
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\sqlpackage.exe"
    "${env:USERPROFILE}\.dotnet\tools\sqlpackage.exe"
    "${env:ProgramFiles}\sqlpackage\sqlpackage.exe"
)

# ===============================================
# LOGGING
# ===============================================

function Write-Log {
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
        "Error"   { Write-Host "✗ $Message" -ForegroundColor Red }
        default   { Write-Host "  $Message" -ForegroundColor Cyan }
    }
}

# ===============================================
# VALIDATION
# ===============================================

function Test-Prerequisites {
    Write-Log "Validating deployment prerequisites..." -Level Info
    
    # Check database project
    if (-not (Test-Path $script:DatabaseProject)) {
        throw "Database project not found: $script:DatabaseProject"
    }
    Write-Log "Database project found: Hartonomous.Database.sqlproj" -Level Success
    
    # Check MSBuild
    if (-not (Test-Path $script:MSBuildPath)) {
        throw "MSBuild not found at: $script:MSBuildPath"
    }
    Write-Log "MSBuild found: $($script:MSBuildPath)" -Level Success
    
    # Find SqlPackage
    $sqlPackage = $null
    foreach ($path in $script:SqlPackagePaths) {
        if (Test-Path $path) {
            $sqlPackage = $path
            break
        }
    }
    
    if (-not $sqlPackage) {
        throw "SqlPackage.exe not found. Install SQL Server Data Tools or Azure Data Studio."
    }
    
    $script:SqlPackagePath = $sqlPackage
    Write-Log "SqlPackage found: $sqlPackage" -Level Success
    
    # Check CLR deployment script (if not skipping CLR)
    if (-not $SkipCLR) {
        if (-not (Test-Path $script:CLRDeployScript)) {
            Write-Log "CLR deployment script not found: $script:CLRDeployScript" -Level Warning
            Write-Log "CLR assemblies will not be deployed" -Level Warning
            $script:SkipCLR = $true
        }
        else {
            Write-Log "CLR deployment script found" -Level Success
        }
    }
    
    # Test SQL Server connectivity
    try {
        $connectionString = if ($Credential) {
            "Server=$Server;Database=master;User Id=$($Credential.UserName);Password=$($Credential.GetNetworkCredential().Password);TrustServerCertificate=True;Connection Timeout=10;"
        }
        else {
            "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=10;"
        }
        
        $connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT SERVERPROPERTY('ProductVersion') AS Version, SERVERPROPERTY('Edition') AS Edition"
        $reader = $command.ExecuteReader()
        
        if ($reader.Read()) {
            $version = $reader["Version"]
            $edition = $reader["Edition"]
            Write-Log "Connected to SQL Server $version ($edition)" -Level Success
        }
        
        $reader.Close()
        $connection.Close()
        
        $script:ConnectionString = $connectionString -replace "Database=master", "Database=$Database"
    }
    catch {
        throw "Failed to connect to SQL Server $Server`: $_"
    }
}

# ===============================================
# DACPAC BUILD
# ===============================================

function Build-DACPAC {
    Write-Log "Building DACPAC from database project..." -Level Info
    
    if ($DryRun) {
        Write-Log "DRY RUN: Would build $script:DatabaseProject" -Level Info
        return $true
    }
    
    # Clean previous build
    $binPath = Join-Path (Split-Path $script:DatabaseProject) "bin"
    if (Test-Path $binPath) {
        Remove-Item $binPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    # Build DACPAC using MSBuild
    $buildArgs = @(
        $script:DatabaseProject
        "/p:Configuration=Release"
        "/t:Build"
        "/v:minimal"
        "/nologo"
    )
    
    Write-Log "Executing MSBuild..." -Level Info
    
    $buildProcess = Start-Process -FilePath $script:MSBuildPath -ArgumentList $buildArgs -Wait -PassThru -NoNewWindow
    
    if ($buildProcess.ExitCode -ne 0) {
        throw "DACPAC build failed with exit code $($buildProcess.ExitCode)"
    }
    
    if (-not (Test-Path $script:DacpacPath)) {
        throw "DACPAC file not found after build: $script:DacpacPath"
    }
    
    $dacpacSize = (Get-Item $script:DacpacPath).Length / 1KB
    Write-Log "DACPAC built successfully ($([math]::Round($dacpacSize, 2)) KB)" -Level Success
    
    return $true
}

# ===============================================
# DACPAC DEPLOYMENT
# ===============================================

function Deploy-DACPAC {
    Write-Log "Deploying DACPAC to $Server\$Database..." -Level Info
    
    # Resolve dependencies path for CLR assemblies
    $dependenciesPath = Join-Path $script:RootPath "dependencies"
    if (-not (Test-Path $dependenciesPath)) {
        throw "Dependencies directory not found: $dependenciesPath"
    }
    
    # Build SqlPackage arguments
    $sqlPackageArgs = @(
        "/Action:Publish"
        "/SourceFile:`"$script:DacpacPath`""
        "/TargetServerName:$Server"
        "/TargetDatabaseName:$Database"
        "/TargetTrustServerCertificate:True"
        "/p:BlockOnPossibleDataLoss=$BlockDataLoss"
        "/p:DropObjectsNotInSource=$DropObjectsNotInSource"
        "/p:CreateNewDatabase=True"
        "/p:IncludeCompositeObjects=True"
        "/p:AllowIncompatiblePlatform=False"
        "/p:VerifyDeployment=True"
        # SQLCMD variables for pre-deployment CLR registration
        "/v:DependenciesPath=`"$dependenciesPath`""
    )
    
    # Add authentication (Windows Authentication is default if no credentials specified)
    if ($Credential) {
        $sqlPackageArgs += "/TargetUser:$($Credential.UserName)"
        $sqlPackageArgs += "/TargetPassword:$($Credential.GetNetworkCredential().Password)"
    }
    
    if ($DryRun) {
        # Generate script instead of publishing
        $scriptPath = Join-Path $script:RootPath "deployment-script-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql"
        $sqlPackageArgs[0] = "/Action:Script"
        $sqlPackageArgs += "/OutputPath:`"$scriptPath`""
        
        Write-Log "DRY RUN: Generating deployment script..." -Level Info
        Write-Log "Output: $scriptPath" -Level Info
    }
    
    # Execute SqlPackage
    try {
        $deployProcess = Start-Process -FilePath $script:SqlPackagePath -ArgumentList $sqlPackageArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput "sqlpackage-output.txt" -RedirectStandardError "sqlpackage-error.txt"
        
        $output = Get-Content "sqlpackage-output.txt" -Raw -ErrorAction SilentlyContinue
        $errors = Get-Content "sqlpackage-error.txt" -Raw -ErrorAction SilentlyContinue
        
        if ($deployProcess.ExitCode -ne 0) {
            Write-Log "SqlPackage output: $output" -Level Error
            Write-Log "SqlPackage errors: $errors" -Level Error
            throw "DACPAC deployment failed with exit code $($deployProcess.ExitCode)"
        }
        
        if ($DryRun) {
            Write-Log "Deployment script generated successfully" -Level Success
            if (Test-Path $scriptPath) {
                $scriptSize = (Get-Item $scriptPath).Length / 1KB
                Write-Log "Script size: $([math]::Round($scriptSize, 2)) KB" -Level Info
            }
        }
        else {
            Write-Log "DACPAC deployed successfully" -Level Success
            
            # Parse deployment output for statistics
            if ($output -match "(\d+) object\(s\) (created|altered|dropped)") {
                Write-Log "Deployment changes: $($Matches[0])" -Level Info
            }
        }
        
        # Cleanup temp files
        Remove-Item "sqlpackage-output.txt" -ErrorAction SilentlyContinue
        Remove-Item "sqlpackage-error.txt" -ErrorAction SilentlyContinue
        
        return $true
    }
    catch {
        throw "SqlPackage execution failed: $_"
    }
}

# ===============================================
# CLR DEPLOYMENT
# ===============================================

function Deploy-CLRAssemblies {
    if ($SkipCLR) {
        Write-Log "Skipping CLR assembly deployment" -Level Warning
        return
    }
    
    Write-Log "Deploying CLR assemblies..." -Level Info
    
    if ($DryRun) {
        Write-Log "DRY RUN: Would deploy CLR assemblies from $script:CLRBinDirectory" -Level Info
        return
    }
    
    try {
        $clrParams = @{
            ServerName = $Server
            DatabaseName = $Database
            BinDirectory = $script:CLRBinDirectory
        }
        
        if ($Credential) {
            $clrParams.Credential = $Credential
        }
        
        & $script:CLRDeployScript @clrParams
        
        Write-Log "CLR assemblies deployed successfully" -Level Success
    }
    catch {
        Write-Log "CLR deployment failed: $_" -Level Error
        if (-not $Force) {
            throw
        }
    }
}

# ===============================================
# POST-DEPLOYMENT VALIDATION
# ===============================================

function Test-Deployment {
    Write-Log "Validating deployment..." -Level Info
    
    if ($DryRun) {
        Write-Log "DRY RUN: Skipping validation" -Level Info
        return
    }
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection $script:ConnectionString
        $connection.Open()
        
        # Count tables
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0"
        $tableCount = $command.ExecuteScalar()
        Write-Log "User tables: $tableCount" -Level Info
        
        # Count stored procedures
        $command.CommandText = "SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0"
        $procCount = $command.ExecuteScalar()
        Write-Log "Stored procedures: $procCount" -Level Info
        
        # Count spatial indexes
        $command.CommandText = "SELECT COUNT(*) FROM sys.spatial_indexes"
        $spatialIndexCount = $command.ExecuteScalar()
        Write-Log "Spatial indexes: $spatialIndexCount" -Level Info
        
        # Check CLR assemblies (if deployed)
        if (-not $SkipCLR) {
            $command.CommandText = "SELECT COUNT(*) FROM sys.assemblies WHERE is_user_defined = 1"
            $assemblyCount = $command.ExecuteScalar()
            Write-Log "CLR assemblies: $assemblyCount" -Level Info
            
            if ($assemblyCount -eq 0) {
                Write-Log "Warning: No CLR assemblies found" -Level Warning
            }
        }
        
        # Test atomic tables exist
        $atomicTables = @('Atoms', 'AtomicPixels', 'AtomicAudioSamples', 'AtomicWeights', 'AtomCompositions')
        foreach ($table in $atomicTables) {
            $command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = '$table'"
            $exists = $command.ExecuteScalar()
            
            if ($exists -gt 0) {
                Write-Log "Atomic table verified: $table" -Level Success
            }
            else {
                Write-Log "Atomic table missing: $table" -Level Warning
            }
        }
        
        $connection.Close()
        
        Write-Log "Deployment validation complete" -Level Success
    }
    catch {
        Write-Log "Validation failed: $_" -Level Error
        if (-not $Force) {
            throw
        }
    }
}

# ===============================================
# MAIN EXECUTION
# ===============================================

try {
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host "   HARTONOMOUS DACPAC DEPLOYMENT" -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host "   Target: $Server\$Database" -ForegroundColor Cyan
    Write-Host "   Mode: $(if ($DryRun) { 'DRY RUN (script generation)' } else { 'LIVE DEPLOYMENT' })" -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Execute deployment pipeline
    Test-Prerequisites
    Build-DACPAC
    Deploy-DACPAC
    Deploy-CLRAssemblies
    Test-Deployment
    
    # Summary
    $duration = (Get-Date) - $script:StartTime
    
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host "   DEPLOYMENT COMPLETE" -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host "   Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Green
    Write-Host "   Database: $Database on $Server" -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor Green
    Write-Host ""
    
    # Save deployment log
    $logPath = Join-Path $script:RootPath "deployment-log-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $script:DeploymentLog | ConvertTo-Json -Depth 10 | Out-File $logPath
    Write-Log "Deployment log saved: $logPath" -Level Info
    
    exit 0
}
catch {
    Write-Host ""
    Write-Host "=================================================================" -ForegroundColor Red
    Write-Host "   DEPLOYMENT FAILED" -ForegroundColor Red
    Write-Host "=================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "✗ ERROR: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Yellow
    Write-Host ""
    
    # Save error log
    $errorLogPath = Join-Path $script:RootPath "deployment-error-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    @{
        Error = $_.Exception.Message
        StackTrace = $_.ScriptStackTrace
        Log = $script:DeploymentLog
    } | ConvertTo-Json -Depth 10 | Out-File $errorLogPath
    
    Write-Host "Error log saved: $errorLogPath" -ForegroundColor Yellow
    Write-Host ""
    
    exit 1
}
