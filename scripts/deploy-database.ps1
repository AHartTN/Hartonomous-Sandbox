#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Idempotent database deployment script for Hartonomous.
    
.DESCRIPTION
    Checks current database state and applies only necessary changes:
    - Creates database if missing
    - Applies EF Core migrations
    - Deploys stored procedures (CREATE OR ALTER)
    - Seeds initial data if tables empty
    
.PARAMETER ServerName
    SQL Server instance (default: localhost)
    
.PARAMETER DatabaseName
    Database name (default: Hartonomous)
    
.PARAMETER SkipMigrations
    Skip EF Core migrations
    
.PARAMETER SkipProcedures
    Skip stored procedure deployment
    
.PARAMETER SkipClr
    Skip SQL CLR deployment

.PARAMETER Verbose
    Show detailed progress
    
.EXAMPLE
    .\deploy-database.ps1
    
.EXAMPLE
    .\deploy-database.ps1 -ServerName ".\SQLEXPRESS" -Verbose
#>

[CmdletBinding()]
param(
    [string]$ServerName = "localhost",
    [string]$DatabaseName = "Hartonomous",
    [switch]$SkipMigrations = $false,
    [switch]$SkipProcedures = $false,
    [switch]$SkipClr = $false
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Hartonomous Database Deployment" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Test SQL Server connection
function Test-SqlConnection {
    param([string]$Server, [string]$Database = "master")
    try {
        $query = "SELECT @@VERSION"
        $result = sqlcmd -S $Server -d $Database -E -C -Q $query -h -1 -W 2>&1
        if ($LASTEXITCODE -ne 0) { return $false }
        return $true
    } catch {
        return $false
    }
}

# Check if database exists
function Test-DatabaseExists {
    param([string]$Server, [string]$DbName)
    $query = "SELECT COUNT(*) FROM sys.databases WHERE name = '$DbName'"
    $result = sqlcmd -S $Server -d master -E -C -Q $query -h -1 -W
    return [int]$result -gt 0
}

# Get table count
function Get-TableCount {
    param([string]$Server, [string]$DbName)
    $query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory'"
    $result = sqlcmd -S $Server -d $DbName -E -C -Q $query -h -1 -W
    return [int]$result
}

# Get procedure count
function Get-ProcedureCount {
    param([string]$Server, [string]$DbName)
    $query = "SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0"
    $result = sqlcmd -S $Server -d $DbName -E -C -Q $query -h -1 -W
    return [int]$result
}

# Get record count for a table
function Get-RecordCount {
    param([string]$Server, [string]$DbName, [string]$TableName)
    $query = "IF OBJECT_ID('$TableName') IS NOT NULL SELECT COUNT(*) FROM $TableName ELSE SELECT 0"
    $result = sqlcmd -S $Server -d $DbName -E -C -Q $query -h -1 -W 2>$null
    return [int]$result
}

# Main deployment logic
Write-Host "► Checking SQL Server connection..." -ForegroundColor Yellow
if (-not (Test-SqlConnection -Server $ServerName)) {
    Write-Host "  ✗ Cannot connect to SQL Server at $ServerName" -ForegroundColor Red
    Write-Host "  Please ensure SQL Server is running and accessible." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Connected to SQL Server" -ForegroundColor Green

Write-Host "`n► Checking database state..." -ForegroundColor Yellow
$dbExists = Test-DatabaseExists -Server $ServerName -DbName $DatabaseName

if (-not $dbExists) {
    Write-Host "  Database '$DatabaseName' not found - will be created by EF migrations" -ForegroundColor Cyan
} else {
    $tableCount = Get-TableCount -Server $ServerName -DbName $DatabaseName
    $procCount = Get-ProcedureCount -Server $ServerName -DbName $DatabaseName
    Write-Host "  ✓ Database exists: $tableCount tables, $procCount procedures" -ForegroundColor Green
}

# Apply EF Core migrations
if (-not $SkipMigrations) {
    Write-Host "`n► Checking EF Core migrations..." -ForegroundColor Yellow
    $dataProjectPath = Join-Path $repoRoot "src\Hartonomous.Data"

    if (-not (Test-Path $dataProjectPath)) {
        Write-Host "  ✗ Data project not found at $dataProjectPath" -ForegroundColor Red
        exit 1
    }

    Push-Location $repoRoot
    try {
        Write-Host "  Applying migrations with dotnet ef database update..." -ForegroundColor Cyan
        $arguments = @("ef", "database", "update", "--project", $dataProjectPath, "--no-build")
        dotnet @arguments 2>&1 | Out-Host

        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ✗ Migration failed" -ForegroundColor Red
            exit 1
        }

        Write-Host "  ✓ Migrations applied successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "`n► Skipping EF migrations (--SkipMigrations)" -ForegroundColor Yellow
}

# Deploy stored procedures
if (-not $SkipProcedures) {
    Write-Host "`n► Deploying stored procedures..." -ForegroundColor Yellow
    
    $sqlProcDir = Join-Path $repoRoot "sql\procedures"
    $procFiles = Get-ChildItem -Path $sqlProcDir -Filter "*.sql" | Sort-Object Name
    
    if ($procFiles.Count -eq 0) {
        Write-Host "  No procedure files found in $sqlProcDir" -ForegroundColor Yellow
    } else {
        $deployed = 0
        $failed = 0
        
        foreach ($file in $procFiles) {
            Write-Host "  Processing $($file.Name)..." -ForegroundColor Cyan
            
            $sqlContent = Get-Content $file.FullName -Raw
            
            # Replace CREATE PROCEDURE with CREATE OR ALTER for idempotency
            $sqlContent = $sqlContent -replace '\bCREATE\s+PROCEDURE\b', 'CREATE OR ALTER PROCEDURE'
            $sqlContent = $sqlContent -replace '\bCREATE\s+PROC\b', 'CREATE OR ALTER PROC'
            
            # Execute SQL
            $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
            $sqlContent | Out-File -FilePath $tempFile -Encoding UTF8
            
            try {
                sqlcmd -S $ServerName -d $DatabaseName -E -C -i $tempFile -b 2>&1 | Out-Null
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "    ✓ Deployed" -ForegroundColor Green
                    $deployed++
                } else {
                    Write-Host "    ✗ Failed" -ForegroundColor Red
                    $failed++
                }
            }
            finally {
                Remove-Item $tempFile -ErrorAction SilentlyContinue
            }
        }
        
        Write-Host "`n  Summary: $deployed deployed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
    }
} else {
    Write-Host "`n► Skipping stored procedures (--SkipProcedures)" -ForegroundColor Yellow
}

# Deploy SQL CLR assembly
if (-not $SkipClr) {
    Write-Host "`n► Deploying SQL CLR assembly..." -ForegroundColor Yellow

    $clrProjectPath = Join-Path $repoRoot "src\SqlClr\SqlClrFunctions.csproj"
    if (-not (Test-Path $clrProjectPath)) {
        Write-Host "  ✗ SQL CLR project not found at $clrProjectPath" -ForegroundColor Red
        Write-Host "  Set --SkipClr if you wish to ignore CLR deployment." -ForegroundColor Red
        exit 1
    }

    # Ensure CLR is enabled on the server
    Write-Host "  Enabling CLR integration on SQL Server (if required)..." -ForegroundColor Cyan
    $enableClrScript = @"
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE WITH OVERRIDE;
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE WITH OVERRIDE;
"@
    sqlcmd -S $ServerName -d master -E -C -Q $enableClrScript 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Failed to enable CLR on SQL Server" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✓ CLR integration enabled" -ForegroundColor Green

    # Build the CLR assembly (Release configuration)
    Write-Host "  Building SqlClrFunctions.dll (Release)..." -ForegroundColor Cyan
    Push-Location $repoRoot
    $buildSucceeded = $false
    try {
        dotnet build $clrProjectPath -c Release -v minimal 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $buildSucceeded = $true
        } else {
            Write-Host "  dotnet build failed, attempting MSBuild..." -ForegroundColor Yellow
            $msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
                -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2>$null | Select-Object -First 1
            if (-not $msbuildPath) {
                Write-Host "  ✗ MSBuild not found; cannot build CLR assembly" -ForegroundColor Red
            } else {
                & $msbuildPath $clrProjectPath /p:Configuration=Release /v:m 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    $buildSucceeded = $true
                }
            }
        }
    }
    finally {
        Pop-Location
    }

    if (-not $buildSucceeded) {
        Write-Host "  ✗ Failed to build SqlClrFunctions.dll" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✓ CLR assembly built" -ForegroundColor Green

    $assemblyPath = Join-Path $repoRoot "src\SqlClr\bin\Release\SqlClrFunctions.dll"
    if (-not (Test-Path $assemblyPath)) {
        Write-Host "  ✗ Assembly not found at $assemblyPath" -ForegroundColor Red
        exit 1
    }

    $assemblyBytes = [System.IO.File]::ReadAllBytes($assemblyPath)
    if ($assemblyBytes.Length -eq 0) {
        Write-Host "  ✗ Assembly file is empty" -ForegroundColor Red
        exit 1
    }

    $hexBuilder = New-Object System.Text.StringBuilder($assemblyBytes.Length * 2)
    foreach ($b in $assemblyBytes) {
        [void]$hexBuilder.AppendFormat("{0:X2}", $b)
    }
    $assemblyHex = $hexBuilder.ToString()

    $clrDeployScript = @"
USE [$DatabaseName];
GO

DROP FUNCTION IF EXISTS dbo.clr_CreatePointCloud;
DROP FUNCTION IF EXISTS dbo.clr_GeometryConvexHull;
DROP FUNCTION IF EXISTS dbo.clr_PointInRegion;
DROP FUNCTION IF EXISTS dbo.clr_RegionOverlap;
DROP FUNCTION IF EXISTS dbo.clr_GeometryCentroid;
DROP FUNCTION IF EXISTS dbo.clr_VectorDotProduct;
DROP FUNCTION IF EXISTS dbo.clr_VectorCosineSimilarity;
DROP FUNCTION IF EXISTS dbo.clr_VectorEuclideanDistance;
DROP FUNCTION IF EXISTS dbo.clr_VectorAdd;
DROP FUNCTION IF EXISTS dbo.clr_VectorSubtract;
DROP FUNCTION IF EXISTS dbo.clr_VectorScale;
DROP FUNCTION IF EXISTS dbo.clr_VectorNorm;
DROP FUNCTION IF EXISTS dbo.clr_VectorNormalize;
DROP FUNCTION IF EXISTS dbo.clr_VectorLerp;
DROP FUNCTION IF EXISTS dbo.clr_VectorSoftmax;
DROP FUNCTION IF EXISTS dbo.clr_VectorArgMax;
DROP FUNCTION IF EXISTS dbo.clr_AudioWaveform;
DROP FUNCTION IF EXISTS dbo.clr_AudioRms;
DROP FUNCTION IF EXISTS dbo.clr_AudioPeak;
DROP FUNCTION IF EXISTS dbo.clr_AudioDownsample;
DROP FUNCTION IF EXISTS dbo.clr_ImagePointCloud;
DROP FUNCTION IF EXISTS dbo.clr_ImageAverageColor;
DROP FUNCTION IF EXISTS dbo.clr_ImageHistogram;
DROP FUNCTION IF EXISTS dbo.clr_GenerateImagePatches;
DROP FUNCTION IF EXISTS dbo.clr_GenerateImageGeometry;
DROP FUNCTION IF EXISTS dbo.clr_SemanticFeaturesJson;
GO

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions' AND is_user_defined = 1)
    DROP ASSEMBLY SqlClrFunctions;
GO

CREATE ASSEMBLY SqlClrFunctions
FROM 0x$assemblyHex
WITH PERMISSION_SET = SAFE;
GO

CREATE OR ALTER FUNCTION dbo.clr_CreatePointCloud(@coordinates NVARCHAR(MAX))
RETURNS geometry
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].CreatePointCloud;
GO

CREATE OR ALTER FUNCTION dbo.clr_GeometryConvexHull(@geom geometry)
RETURNS geometry
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].ConvexHull;
GO

CREATE OR ALTER FUNCTION dbo.clr_PointInRegion(@point geometry, @region geometry)
RETURNS bit
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].PointInRegion;
GO

CREATE OR ALTER FUNCTION dbo.clr_RegionOverlap(@region1 geometry, @region2 geometry)
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].RegionOverlap;
GO

CREATE OR ALTER FUNCTION dbo.clr_GeometryCentroid(@geom geometry)
RETURNS geometry
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].Centroid;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorDotProduct(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX))
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorDotProduct;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorCosineSimilarity(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX))
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorCosineSimilarity;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorEuclideanDistance(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX))
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorEuclideanDistance;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorAdd(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorAdd;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorSubtract(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSubtract;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorScale(@vector VARBINARY(MAX), @scalar float)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorScale;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorNorm(@vector VARBINARY(MAX))
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNorm;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorNormalize(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorNormalize;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorLerp(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX), @t float)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorLerp;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorSoftmax(@vector VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorSoftmax;
GO

CREATE OR ALTER FUNCTION dbo.clr_VectorArgMax(@vector VARBINARY(MAX))
RETURNS int
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.VectorOperations].VectorArgMax;
GO

CREATE OR ALTER FUNCTION dbo.clr_AudioWaveform(@audio VARBINARY(MAX), @channelCount int, @sampleRate int, @maxPoints int)
RETURNS geometry
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioToWaveform;
GO

CREATE OR ALTER FUNCTION dbo.clr_AudioRms(@audio VARBINARY(MAX), @channelCount int)
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputeRms;
GO

CREATE OR ALTER FUNCTION dbo.clr_AudioPeak(@audio VARBINARY(MAX), @channelCount int)
RETURNS float
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioComputePeak;
GO

CREATE OR ALTER FUNCTION dbo.clr_AudioDownsample(@audio VARBINARY(MAX), @channelCount int, @factor int)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AudioProcessing].AudioDownsample;
GO

CREATE OR ALTER FUNCTION dbo.clr_ImagePointCloud(@image VARBINARY(MAX), @width int, @height int, @sampleStep int)
RETURNS geometry
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageToPointCloud;
GO

CREATE OR ALTER FUNCTION dbo.clr_ImageAverageColor(@image VARBINARY(MAX), @width int, @height int)
RETURNS NVARCHAR(16)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageAverageColor;
GO

CREATE OR ALTER FUNCTION dbo.clr_ImageHistogram(@image VARBINARY(MAX), @width int, @height int, @bins int)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageProcessing].ImageLuminanceHistogram;
GO

CREATE OR ALTER FUNCTION dbo.clr_GenerateImagePatches(
    @width int,
    @height int,
    @patchSize int,
    @steps int,
    @guidanceScale float,
    @guideX float,
    @guideY float,
    @guideZ float,
    @seed int)
RETURNS TABLE (
    patch_x INT,
    patch_y INT,
    spatial_x FLOAT,
    spatial_y FLOAT,
    spatial_z FLOAT,
    patch geometry)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateGuidedPatches;
GO

CREATE OR ALTER FUNCTION dbo.clr_GenerateImageGeometry(
    @width int,
    @height int,
    @patchSize int,
    @steps int,
    @guidanceScale float,
    @guideX float,
    @guideY float,
    @guideZ float,
    @seed int)
RETURNS geometry
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ImageGeneration].GenerateGuidedGeometry;
GO

CREATE OR ALTER FUNCTION dbo.clr_SemanticFeaturesJson(@text NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SemanticAnalysis].ComputeSemanticFeatures;
GO
"@

    $tempClrFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $clrDeployScript | Out-File -FilePath $tempClrFile -Encoding UTF8

    try {
        sqlcmd -S $ServerName -d $DatabaseName -E -C -b -i $tempClrFile 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ✗ Failed to deploy SQL CLR assembly" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Remove-Item $tempClrFile -ErrorAction SilentlyContinue
    }

    Write-Host "  ✓ SQL CLR assembly deployed and functions created" -ForegroundColor Green
} else {
    Write-Host "`n► Skipping SQL CLR deployment (--SkipClr)" -ForegroundColor Yellow
}

# Verify final state
Write-Host "`n► Verifying deployment..." -ForegroundColor Yellow

if (Test-DatabaseExists -Server $ServerName -DbName $DatabaseName) {
    $finalTableCount = Get-TableCount -Server $ServerName -DbName $DatabaseName
    $finalProcCount = Get-ProcedureCount -Server $ServerName -DbName $DatabaseName
    
    Write-Host "  ✓ Database state:" -ForegroundColor Green
    Write-Host "    - Tables: $finalTableCount" -ForegroundColor Cyan
    Write-Host "    - Procedures: $finalProcCount" -ForegroundColor Cyan
    
    # Check key tables
    $embeddingsCount = Get-RecordCount -Server $ServerName -DbName $DatabaseName -TableName "dbo.Embeddings_Production"
    $modelsCount = Get-RecordCount -Server $ServerName -DbName $DatabaseName -TableName "dbo.Models"
    $vocabCount = Get-RecordCount -Server $ServerName -DbName $DatabaseName -TableName "dbo.TokenVocabulary"
    
    Write-Host "    - Embeddings: $embeddingsCount records" -ForegroundColor Cyan
    Write-Host "    - Models: $modelsCount records" -ForegroundColor Cyan
    Write-Host "    - TokenVocabulary: $vocabCount records" -ForegroundColor Cyan
    
    if ($vocabCount -eq 0) {
        Write-Host "`n  ⚠ TokenVocabulary is empty - run seeding script to populate" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ✗ Database verification failed" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Deployment Complete" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green

Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Seed vocabulary: sqlcmd -S $ServerName -d $DatabaseName -E -C -i sql\procedures\16_SeedTokenVocabularyWithVector.sql" -ForegroundColor White
Write-Host "  2. Test ingestion: dotnet run --project src\ModelIngestion" -ForegroundColor White
Write-Host "  3. Verify data: sqlcmd -S $ServerName -d $DatabaseName -E -C -Q `"SELECT COUNT(*) FROM dbo.Embeddings_Production`"" -ForegroundColor White
Write-Host "  4. Validate CLR: SELECT dbo.clr_PointInRegion(geometry::Point(0,0,0), geometry::STGeomFromText('POLYGON((0 0,1 0,1 1,0 1,0 0))',0));" -ForegroundColor White
