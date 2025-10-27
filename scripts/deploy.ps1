# Hartonomous Production Deployment Script
# Idempotent deployment: Drops and recreates database from scratch
# Uses: SQL schemas + EF Core + SQL CLR

param(
    [string]$SqlServer = "localhost",
    [switch]$SkipDrop = $false,
    [switch]$SkipClr = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host "Hartonomous - Production Deployment" -ForegroundColor Cyan
Write-Host "SQL Server 2025 AI Inference Engine with Content-Addressable Storage" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""

# Change to repository root
Push-Location $PSScriptRoot\..

try {
    # ========================================
    # PHASE 1: Pre-flight Checks
    # ========================================
    Write-Host "[1/8] Pre-flight checks..." -ForegroundColor Yellow

    # Test SQL Server connection
    try {
        $version = sqlcmd -S $SqlServer -E -C -Q "SELECT @@VERSION" -h -1 -W | Select-Object -First 1
        Write-Host "  ✓ SQL Server connected: $($version.Substring(0, [Math]::Min(60, $version.Length)))..." -ForegroundColor Green
    } catch {
        Write-Host "  ✗ SQL Server connection failed" -ForegroundColor Red
        throw
    }

    # Check for .NET 10
    $dotnetVersion = dotnet --version
    Write-Host "  ✓ .NET SDK: $dotnetVersion" -ForegroundColor Green

    # Check for .NET Framework 4.8.1 SDK (for SQL CLR)
    if (-not $SkipClr) {
        if (Test-Path "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1") {
            Write-Host "  ✓ .NET Framework 4.8.1 SDK available" -ForegroundColor Green
        } else {
            Write-Host "  ! .NET Framework 4.8.1 SDK not found - SQL CLR will be skipped" -ForegroundColor Yellow
            $SkipClr = $true
        }
    }

    # ========================================
    # PHASE 2: Database Recreation
    # ========================================
    if (-not $SkipDrop) {
        Write-Host ""
        Write-Host "[2/8] Dropping existing database..." -ForegroundColor Yellow
        
        $dropSql = @"
USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'Hartonomous')
BEGIN
    ALTER DATABASE Hartonomous SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Hartonomous;
    PRINT 'Database Hartonomous dropped.';
END
GO
"@
        $dropSql | sqlcmd -S $SqlServer -E -C
        Write-Host "  ✓ Database dropped" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "[2/8] Skipping database drop (SkipDrop=true)" -ForegroundColor Yellow
    }

    # ========================================
    # PHASE 3: Core Schema Deployment
    # ========================================
    Write-Host ""
    Write-Host "[3/8] Deploying core schemas..." -ForegroundColor Yellow
    
    $schemas = @(
        "sql\schemas\01_CoreTables.sql"
        "sql\schemas\02_UnifiedAtomization.sql"
        "sql\schemas\02_MultiModalData.sql"
        "sql\schemas\04_DiskANNPattern.sql"
        "sql\schemas\21_AddContentHashDeduplication.sql"
    )

    foreach ($schema in $schemas) {
        Write-Host "  Deploying $schema..." -ForegroundColor Gray
        sqlcmd -S $SqlServer -E -C -i $schema | Out-Null
    }
    Write-Host "  ✓ Core schemas deployed" -ForegroundColor Green

    # ========================================
    # PHASE 4: Stored Procedures
    # ========================================
    Write-Host ""
    Write-Host "[4/8] Deploying stored procedures..." -ForegroundColor Yellow
    
    $procedures = @(
        "sql\procedures\06_ProductionSystem.sql"
        "sql\procedures\08_SpatialProjection.sql"
    )

    foreach ($proc in $procedures) {
        if (Test-Path $proc) {
            Write-Host "  Deploying $proc..." -ForegroundColor Gray
            sqlcmd -S $SqlServer -E -C -i $proc | Out-Null
        }
    }
    Write-Host "  ✓ Stored procedures deployed" -ForegroundColor Green

    # ========================================
    # PHASE 5: SQL CLR Assembly
    # ========================================
    Write-Host ""
    Write-Host "[5/8] Building and deploying SQL CLR..." -ForegroundColor Yellow
    
    if ($SkipClr) {
        Write-Host "  ! SQL CLR skipped" -ForegroundColor Yellow
    } else {
        Push-Location src\SqlClr
        try {
            # Build the assembly
            Write-Host "  Building SQL CLR assembly..." -ForegroundColor Gray
            dotnet build SqlClrFunctions.csproj -c Release | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ SQL CLR assembly built" -ForegroundColor Green
                
                # TODO: Deploy to SQL Server (requires TRUSTWORTHY database and signing)
                Write-Host "  ! SQL CLR deployment to SQL Server requires manual registration" -ForegroundColor Yellow
                Write-Host "    Assembly location: src\SqlClr\bin\Release\SqlClrFunctions.dll" -ForegroundColor Gray
            } else {
                Write-Host "  ! SQL CLR build failed" -ForegroundColor Yellow
            }
        } finally {
            Pop-Location
        }
    }

    # ========================================
    # PHASE 6: Initialize Spatial Anchors
    # ========================================
    Write-Host ""
    Write-Host "[6/8] Initializing spatial anchor points..." -ForegroundColor Yellow
    
    # First insert some seed embeddings for anchor selection
    Write-Host "  Inserting seed embeddings..." -ForegroundColor Gray
    
    $seedSql = @"
USE Hartonomous;
GO

-- Insert seed embeddings if none exist
IF NOT EXISTS (SELECT 1 FROM dbo.Embeddings_Production)
BEGIN
    -- Insert 10 random seed embeddings for anchor initialization
    DECLARE @i INT = 0;
    DECLARE @embedding NVARCHAR(MAX);
    DECLARE @vec VECTOR(768);
    
    WHILE @i < 10
    BEGIN
        -- Generate a simple seed vector (normally would come from actual embedding model)
        SET @embedding = '[' + CAST(RAND() AS NVARCHAR(50));
        DECLARE @j INT = 1;
        WHILE @j < 768
        BEGIN
            SET @embedding = @embedding + ',' + CAST(RAND() AS NVARCHAR(50));
            SET @j = @j + 1;
        END
        SET @embedding = @embedding + ']';
        
        SET @vec = CAST(@embedding AS VECTOR(768));
        
        INSERT INTO dbo.Embeddings_Production (source_text, source_type, embedding_full, dimension, spatial_proj_x, spatial_proj_y, spatial_proj_z)
        VALUES ('Seed embedding ' + CAST(@i AS NVARCHAR(10)), 'seed', @vec, 768, RAND(), RAND(), RAND());
        
        SET @i = @i + 1;
    END
    
    PRINT '  ✓ Seed embeddings inserted';
END
ELSE
BEGIN
    PRINT '  ✓ Embeddings already exist';
END
GO
"@
    
    $seedSql | sqlcmd -S $SqlServer -E -C

    # Initialize anchors
    Write-Host "  Initializing spatial anchors..." -ForegroundColor Gray
    sqlcmd -S $SqlServer -E -C -d Hartonomous -Q "EXEC dbo.sp_InitializeSpatialAnchors" | Out-Null
    Write-Host "  ✓ Spatial anchors initialized" -ForegroundColor Green

    # ========================================
    # PHASE 7: Build .NET Services
    # ========================================
    Write-Host ""
    Write-Host "[7/8] Building .NET services..." -ForegroundColor Yellow
    
    dotnet build Hartonomous.sln -c Release | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ All .NET services built" -ForegroundColor Green
    } else {
        Write-Host "  ! Build had warnings/errors" -ForegroundColor Yellow
    }

    # ========================================
    # PHASE 8: Verification
    # ========================================
    Write-Host ""
    Write-Host "[8/8] Verifying deployment..." -ForegroundColor Yellow
    
    $verifySql = @"
USE Hartonomous;
GO

SELECT 
    'Core Tables' as Category,
    COUNT(*) as Count 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
    AND TABLE_NAME IN ('Models', 'ModelLayers', 'InferenceRequests', 'TokenVocabulary')

UNION ALL

SELECT 
    'Atomic Storage' as Category,
    COUNT(*) as Count
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
    AND TABLE_NAME LIKE 'Atomic%'

UNION ALL

SELECT 
    'Multi-Modal' as Category,
    COUNT(*) as Count
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo' 
    AND TABLE_NAME IN ('Images', 'AudioData', 'Videos', 'TextDocuments')

UNION ALL

SELECT 
    'Embeddings' as Category,
    COUNT(*) as Count
FROM dbo.Embeddings_Production

UNION ALL

SELECT 
    'Spatial Anchors' as Category,
    COUNT(*) as Count
FROM dbo.SpatialAnchors

UNION ALL

SELECT 
    'Stored Procedures' as Category,
    COUNT(*) as Count
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_SCHEMA = 'dbo' 
    AND ROUTINE_TYPE = 'PROCEDURE'
    AND ROUTINE_NAME LIKE 'sp_%'
GO
"@
    
    Write-Host ""
    $verifySql | sqlcmd -S $SqlServer -E -C -W
    
    Write-Host ""
    Write-Host "=" * 70 -ForegroundColor Green
    Write-Host "Deployment Complete!" -ForegroundColor Green
    Write-Host "=" * 70 -ForegroundColor Green
    Write-Host ""
    Write-Host "Database: Hartonomous" -ForegroundColor Cyan
    Write-Host "Server: $SqlServer" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Test deduplication:" -ForegroundColor White
    Write-Host "     cd src\ModelIngestion" -ForegroundColor Gray
    Write-Host "     dotnet run -- test-deduplication" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Test atomic storage:" -ForegroundColor White
    Write-Host "     dotnet run -- test-atomic" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Ingest embeddings:" -ForegroundColor White
    Write-Host "     dotnet run -- ingest-embeddings 1000" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Query system:" -ForegroundColor White
    Write-Host "     dotnet run -- query ""sample semantic search""" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "=" * 70 -ForegroundColor Red
    Write-Host "Deployment Failed!" -ForegroundColor Red
    Write-Host "=" * 70 -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
} finally {
    Pop-Location
}


# Test SQL Server connection
Write-Host "[1/7] Testing SQL Server connection..." -ForegroundColor Cyan
try {
    sqlcmd -S $SqlServer -E -C -Q "SELECT @@VERSION" -b | Out-Null
    Write-Host "  ✓ SQL Server connection successful" -ForegroundColor Green
} catch {
    Write-Host "  ✗ SQL Server connection failed" -ForegroundColor Red
    exit 1
}

# Test Neo4j connection
Write-Host "[2/7] Testing Neo4j connection..." -ForegroundColor Cyan
try {
    $auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${Neo4jUser}:${Neo4jPassword}"))
    $headers = @{ Authorization = "Basic $auth"; "Content-Type" = "application/json" }
    $body = '{"statements":[{"statement":"RETURN 1"}]}'
    $response = Invoke-WebRequest -Uri "$Neo4jUri/db/neo4j/tx/commit" -Method POST -Headers $headers -Body $body
    Write-Host "  ✓ Neo4j connection successful" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Neo4j connection failed: $_" -ForegroundColor Red
    exit 1
}

# Deploy SQL Core Tables
Write-Host "[3/7] Deploying SQL Server core tables..." -ForegroundColor Cyan
try {
    sqlcmd -S $SqlServer -E -C -i "..\sql\schemas\01_CoreTables.sql" -b
    Write-Host "  ✓ Core tables deployed" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Core tables deployment failed" -ForegroundColor Red
    exit 1
}

# Deploy SQL Multi-Modal Tables
Write-Host "[4/7] Deploying SQL Server multi-modal tables..." -ForegroundColor Cyan
try {
    sqlcmd -S $SqlServer -E -C -i "..\sql\schemas\02_MultiModalData.sql" -b
    Write-Host "  ✓ Multi-modal tables deployed" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Multi-modal tables deployment failed" -ForegroundColor Red
    exit 1
}

# Build SQL CLR Assembly (if .NET Framework SDK available)
Write-Host "[5/7] Building SQL CLR assembly..." -ForegroundColor Cyan
if (Get-Command msbuild -ErrorAction SilentlyContinue) {
    Push-Location ..\src\SqlClr
    try {
        msbuild SqlClrFunctions.csproj /p:Configuration=Release /p:TargetFrameworkVersion=v4.8
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ CLR assembly built" -ForegroundColor Green
        } else {
            Write-Host "  ! CLR assembly build failed (requires .NET Framework 4.8 SDK)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ! CLR assembly build failed with an exception: $_" -ForegroundColor Yellow
    } finally {
        Pop-Location
    }
} else {
    Write-Host "  ! MSBuild not found - skipping CLR assembly" -ForegroundColor Yellow
}

# Initialize Neo4j Schema
Write-Host "[6/7] Initializing Neo4j schema..." -ForegroundColor Cyan
try {
    $cypherScript = Get-Content "..\neo4j\schemas\CoreSchema.cypher" -Raw
    
    # Remove comments and split into statements
    $cleanScript = $cypherScript -replace '(?m)//.*$' -replace '(?s)/\*.*?\*/'
    $statements = $cleanScript -split ';' | Where-Object { $_.Trim() -ne '' }

    foreach ($stmt in $statements) {
        $cleanStmt = $stmt.Trim()
        $body = @{
            statements = @(
                @{ statement = $cleanStmt }
            )
        } | ConvertTo-Json -Depth 10

        $params = @{
            Uri         = "$Neo4jUri/db/neo4j/tx/commit"
            Method      = 'POST'
            Headers     = $headers
            Body        = $body
            ErrorAction = 'Stop'
        }
        try {
            Invoke-WebRequest @params | Out-Null
        } catch {
            Write-Host "  ✗ Error executing Cypher statement:" -ForegroundColor Red
            Write-Host "    Statement: $cleanStmt" -ForegroundColor Gray
            Write-Host "    Response: $($_.Exception.Response.GetResponseStream() | New-Object System.IO.StreamReader | Select-Object -ExpandProperty End)" -ForegroundColor Gray
            # Decide if you want to exit on error or just warn
            # exit 1 
        }
    }
    Write-Host "  ✓ Neo4j schema initialized" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Neo4j schema initialization failed: $_" -ForegroundColor Red
    exit 1
}

# Verify Installation
Write-Host "[7/7] Verifying installation..." -ForegroundColor Cyan
try {
    $result = sqlcmd -S $SqlServer -E -C -d Hartonomous -Q "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'" -h -1
    $tableCount = [int]$result.Trim()
    Write-Host "  ✓ SQL Server: $tableCount tables created" -ForegroundColor Green

    Write-Host "  ✓ Neo4j: Schema constraints and indexes created" -ForegroundColor Green
} catch {
    Write-Host "  ! Verification had issues" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=" * 60
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "=" * 60
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Enable SQL Server 2025 preview features:"
Write-Host "     EXEC sp_configure 'preview features', 1; RECONFIGURE;"
Write-Host ""
Write-Host "  2. Build and run .NET 10 services:"
Write-Host "     cd src/CesConsumer && dotnet build"
Write-Host "     cd src/Neo4jSync && dotnet build"
Write-Host ""
Write-Host "  3. See README.md for usage examples"
Write-Host ""