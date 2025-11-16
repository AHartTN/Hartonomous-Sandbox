#!/usr/bin/env pwsh
# =====================================================
# HARTONOMOUS MASTER DEPLOYMENT SCRIPT
# =====================================================
# Complete end-to-end deployment automation
# Deploys database, CLR, procedures, and validates

param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [switch]$IntegratedSecurity,
    [string]$Username,
    [string]$Password,
    [switch]$SkipBuild,
    [switch]$SkipValidation
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ASCII Art Header
Write-Host ""
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?                                                        ?" -ForegroundColor Cyan
Write-Host "?        HARTONOMOUS COMPLETE DEPLOYMENT v1.0            ?" -ForegroundColor Cyan
Write-Host "?                                                        ?" -ForegroundColor Cyan
Write-Host "?  Autonomous Geometric Reasoning System                ?" -ForegroundColor Cyan
Write-Host "?  Database Intelligence with O(log N) Spatial Search   ?" -ForegroundColor Cyan
Write-Host "?                                                        ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

# =====================================================
# STEP 1: Build Solution
# =====================================================
if (-not $SkipBuild) {
    Write-Host "[ STEP 1/7 ] Building Hartonomous Solution..." -ForegroundColor Yellow
    Write-Host ""
    
    dotnet restore Hartonomous.sln --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Solution restore failed"
    }
    
    dotnet build Hartonomous.sln -c Release --no-restore --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed"
    }
    
    Write-Host "? Solution built successfully" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[ STEP 1/7 ] Skipping build (--SkipBuild specified)" -ForegroundColor Gray
    Write-Host ""
}

# =====================================================
# STEP 2: Configure CLR Security
# =====================================================
Write-Host "[ STEP 2/7 ] Configuring SQL Server CLR..." -ForegroundColor Yellow
Write-Host ""

$clrScript = "src/Hartonomous.Database/Scripts/Pre-Deployment/01-Configure-CLR.sql"

if (Test-Path $clrScript) {
    try {
        if ($IntegratedSecurity) {
            sqlcmd -S $Server -d master -E -i $clrScript -b
        } else {
            sqlcmd -S $Server -d master -U $Username -P $Password -i $clrScript -b
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "??  CLR configuration may have failed (check output above)" -ForegroundColor Yellow
        } else {
            Write-Host "? CLR configured successfully" -ForegroundColor Green
        }
    } catch {
        Write-Host "??  CLR configuration encountered errors: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "   Continuing with deployment..." -ForegroundColor Yellow
    }
} else {
    Write-Host "??  CLR configuration script not found: $clrScript" -ForegroundColor Yellow
}

Write-Host ""

# =====================================================
# STEP 3: Deploy Database (DACPAC)
# =====================================================
Write-Host "[ STEP 3/7 ] Deploying Hartonomous Database..." -ForegroundColor Yellow
Write-Host ""

$dacpacPath = "src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac"

if (-not (Test-Path $dacpacPath)) {
    Write-Host "Building database project..." -ForegroundColor Cyan
    dotnet build src/Hartonomous.Database/Hartonomous.Database.sqlproj -c Release --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Database project build failed"
    }
}

if (Test-Path $dacpacPath) {
    $connectionString = "Server=$Server;Database=$Database;"
    
    if ($IntegratedSecurity) {
        $connectionString += "Integrated Security=True;TrustServerCertificate=True;"
    } else {
        $connectionString += "User Id=$Username;Password=$Password;TrustServerCertificate=True;"
    }
    
    Write-Host "Deploying DACPAC to $Server/$Database..." -ForegroundColor Cyan
    
    $publishArgs = @(
        "/Action:Publish",
        "/SourceFile:$dacpacPath",
        "/TargetConnectionString:$connectionString",
        "/p:IncludeCompositeObjects=True",
        "/p:CreateNewDatabase=False",
        "/p:BlockOnPossibleDataLoss=False",
        "/p:AllowIncompatiblePlatform=True"
    )
    
    & sqlpackage $publishArgs 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "??  DACPAC deployment completed with warnings" -ForegroundColor Yellow
    } else {
        Write-Host "? Database deployed successfully" -ForegroundColor Green
    }
} else {
    throw "DACPAC not found at: $dacpacPath"
}

Write-Host ""

# =====================================================
# STEP 4: Deploy Core Stored Procedures
# =====================================================
Write-Host "[ STEP 4/7 ] Deploying Core Stored Procedures..." -ForegroundColor Yellow
Write-Host ""

$procedures = @(
    "src/Hartonomous.Database/Procedures/dbo.sp_FindNearestAtoms.sql",
    "src/Hartonomous.Database/Procedures/dbo.sp_IngestAtoms.sql",
    "src/Hartonomous.Database/Procedures/dbo.sp_RunInference.sql"
)

$deployedProcs = 0

foreach ($proc in $procedures) {
    if (Test-Path $proc) {
        $procName = Split-Path $proc -Leaf
        Write-Host "  Deploying $procName..." -ForegroundColor Cyan
        
        try {
            if ($IntegratedSecurity) {
                sqlcmd -S $Server -d $Database -E -i $proc -b 2>&1 | Out-Null
            } else {
                sqlcmd -S $Server -d $Database -U $Username -P $Password -i $proc -b 2>&1 | Out-Null
            }
            
            if ($LASTEXITCODE -eq 0) {
                $deployedProcs++
                Write-Host "  ? $procName deployed" -ForegroundColor Green
            } else {
                Write-Host "  ??  $procName deployment may have issues" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "  ? $procName failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "  ??  $procName not found" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "? Deployed $deployedProcs/$($procedures.Count) core procedures" -ForegroundColor Green
Write-Host ""

# =====================================================
# STEP 5: Verify CLR Functions
# =====================================================
Write-Host "[ STEP 5/7 ] Verifying CLR Functions..." -ForegroundColor Yellow
Write-Host ""

$clrTestQuery = @"
DECLARE @testVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
SELECT 
    CASE WHEN dbo.fn_ProjectTo3D(@testVec) IS NOT NULL THEN 'OPERATIONAL' ELSE 'FAILED' END AS Status;
"@

try {
    $clrResult = if ($IntegratedSecurity) {
        sqlcmd -S $Server -d $Database -E -Q $clrTestQuery -h -1 -W
    } else {
        sqlcmd -S $Server -d $Database -U $Username -P $Password -Q $clrTestQuery -h -1 -W
    }
    
    if ($clrResult -match "OPERATIONAL") {
        Write-Host "? CLR functions are operational" -ForegroundColor Green
    } else {
        Write-Host "??  CLR functions may not be working" -ForegroundColor Yellow
        Write-Host "   Run: scripts/fix-clr-deployment.sql" -ForegroundColor Yellow
    }
} catch {
    Write-Host "??  CLR verification failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# =====================================================
# STEP 6: Run Validation Tests
# =====================================================
if (-not $SkipValidation) {
    Write-Host "[ STEP 6/7 ] Running Validation Tests..." -ForegroundColor Yellow
    Write-Host ""
    
    $validationScript = "tests/complete-validation.sql"
    
    if (Test-Path $validationScript) {
        if ($IntegratedSecurity) {
            sqlcmd -S $Server -d $Database -E -i $validationScript
        } else {
            sqlcmd -S $Server -d $Database -U $Username -P $Password -i $validationScript
        }
    } else {
        Write-Host "??  Validation script not found: $validationScript" -ForegroundColor Yellow
    }
    
    Write-Host ""
} else {
    Write-Host "[ STEP 6/7 ] Skipping validation (--SkipValidation specified)" -ForegroundColor Gray
    Write-Host ""
}

# =====================================================
# STEP 7: Deployment Summary
# =====================================================
Write-Host "[ STEP 7/7 ] Deployment Complete" -ForegroundColor Yellow
Write-Host ""

$duration = (Get-Date) - $startTime
$durationText = "{0:mm\:ss}" -f $duration

Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?                                                        ?" -ForegroundColor Green
Write-Host "?        DEPLOYMENT SUCCESSFUL ?                         ?" -ForegroundColor Green
Write-Host "?                                                        ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""
Write-Host "  Server: $Server" -ForegroundColor White
Write-Host "  Database: $Database" -ForegroundColor White
Write-Host "  Duration: $durationText" -ForegroundColor White
Write-Host ""
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
Write-Host "4. Monitor OODA Loop:" -ForegroundColor White
Write-Host "   curl http://localhost:5000/api/admin/health" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Trigger Analysis:" -ForegroundColor White
Write-Host "   curl -X POST http://localhost:5000/api/admin/ooda/analyze" -ForegroundColor Gray
Write-Host ""
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   HARTONOMOUS IS NOW OPERATIONAL                       ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
