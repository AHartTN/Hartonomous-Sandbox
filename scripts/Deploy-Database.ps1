# Database Deployment Script
# Deploys Hartonomous.Database.dacpac to local SQL Server

param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$CreateDatabase = $false,
    [switch]$TrustServerCertificate = $true
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Hartonomous Database Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check for DACPAC
$dacpacPath = "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"
if (-not (Test-Path $dacpacPath)) {
    Write-Host "ERROR: DACPAC not found at $dacpacPath" -ForegroundColor Red
    Write-Host "Run: msbuild src\Hartonomous.Database\Hartonomous.Database.sqlproj /t:Build /p:Configuration=Release" -ForegroundColor Yellow
    exit 1
}

$dacpacInfo = Get-Item $dacpacPath
Write-Host "DACPAC Found:" -ForegroundColor Green
Write-Host "  Path: $($dacpacInfo.FullName)" -ForegroundColor Gray
Write-Host "  Size: $([math]::Round($dacpacInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host "  Modified: $($dacpacInfo.LastWriteTime)" -ForegroundColor Gray
Write-Host ""

# Check for SqlPackage
$sqlPackagePaths = @(
    "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
    "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
    "${env:ProgramFiles}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
    "${env:ProgramFiles(x86)}\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
)

$sqlPackage = $null

# First try dotnet tool
$dotnetToolPath = Get-Command sqlpackage -ErrorAction SilentlyContinue
if ($dotnetToolPath) {
    $sqlPackage = $dotnetToolPath.Source
}

# Fall back to traditional paths
if (-not $sqlPackage) {
    foreach ($path in $sqlPackagePaths) {
        if (Test-Path $path) {
            $sqlPackage = $path
            break
        }
    }
}

if (-not $sqlPackage) {
    Write-Host "ERROR: SqlPackage.exe not found" -ForegroundColor Red
    Write-Host "Install SQL Server Data Tools (SSDT) or use: dotnet tool install -g microsoft.sqlpackage" -ForegroundColor Yellow
    exit 1
}

Write-Host "SqlPackage: $sqlPackage" -ForegroundColor Green
Write-Host ""

# Build connection string
$connectionString = "Server=$Server;Database=$Database;Integrated Security=True;"
if ($TrustServerCertificate) {
    $connectionString += "TrustServerCertificate=True;"
}

Write-Host "Deployment Configuration:" -ForegroundColor Cyan
Write-Host "  Server: $Server" -ForegroundColor Gray
Write-Host "  Database: $Database" -ForegroundColor Gray
Write-Host "  Create DB: $CreateDatabase" -ForegroundColor Gray
Write-Host ""

# Test SQL Server connection
try {
    Write-Host "Testing SQL Server connection..." -ForegroundColor Yellow
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString.Replace("Database=$Database;", "Database=master;"))
    $conn.Open()
    $version = $conn.ServerVersion
    $conn.Close()
    Write-Host "? Connected to SQL Server (Version: $version)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR: Cannot connect to SQL Server at $Server" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Create database if requested
if ($CreateDatabase) {
    Write-Host "Creating database $Database..." -ForegroundColor Yellow
    try {
        $createConn = New-Object System.Data.SqlClient.SqlConnection($connectionString.Replace("Database=$Database;", "Database=master;"))
        $createConn.Open()
        $cmd = $createConn.CreateCommand()
        $cmd.CommandText = @"
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '$Database')
BEGIN
    CREATE DATABASE [$Database];
    PRINT 'Database created successfully';
END
ELSE
BEGIN
    PRINT 'Database already exists';
END
"@
        $cmd.ExecuteNonQuery() | Out-Null
        $createConn.Close()
        Write-Host "? Database ready" -ForegroundColor Green
        Write-Host ""
    } catch {
        Write-Host "ERROR creating database:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

# Deploy DACPAC
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploying DACPAC..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$deployArgs = @(
    "/Action:Publish",
    "/SourceFile:$dacpacPath",
    "/TargetServerName:$Server",
    "/TargetDatabaseName:$Database",
    "/TargetTrustServerCertificate:True",
    "/p:IncludeCompositeObjects=True",
    "/p:BlockOnPossibleDataLoss=False",
    "/p:AllowIncompatiblePlatform=True"
)

try {
    & $sqlPackage $deployArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "? DEPLOYMENT SUCCESSFUL" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Run smoke tests: sqlcmd -S $Server -d $Database -i tests\smoke-tests.sql" -ForegroundColor Gray
        Write-Host "2. Test OODA loop: POST to /api/v1/operations/autonomous/trigger" -ForegroundColor Gray
        Write-Host "3. Verify spatial queries work" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "ERROR: Deployment failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR during deployment:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
