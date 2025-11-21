# ?? **END-TO-END INTEGRATION TEST**
# Tests the complete flow: File Upload ? Atom Creation ? Job Queue ? Embedding Generation

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Hartonomous Integration Test - Step A" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$solutionDir = "D:\Repositories\Hartonomous"
$workerDir = "$solutionDir\src\Hartonomous.Workers.EmbeddingGenerator"
$testFile = "$solutionDir\tests\test_sample.txt"

Write-Host "Step 1: Verify Prerequisites" -ForegroundColor Yellow
Write-Host "- Checking Ollama is running..."

try {
    $ollamaCheck = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method GET -TimeoutSec 2
    Write-Host "? Ollama is running" -ForegroundColor Green
    
    $models = ($ollamaCheck.Content | ConvertFrom-Json).models
    Write-Host "   Available models: $($models.name -join ', ')" -ForegroundColor Gray
} catch {
    Write-Host "? Ollama is NOT running. Start it with: ollama serve" -ForegroundColor Red
    Write-Host "   Or check if it's on a different port" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Step 2: Build Solution" -ForegroundColor Yellow
Set-Location $solutionDir
dotnet build Hartonomous.sln --configuration Debug | Select-Object -Last 3
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "? Build successful" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Run SQL Smoke Test" -ForegroundColor Yellow
Write-Host "   Execute tests\Quick_Smoke_Test.sql in SSMS" -ForegroundColor Gray
Write-Host "   Press any key when complete..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "Step 4: Create Test File" -ForegroundColor Yellow
if (-not (Test-Path $testFile)) {
    @"
This is a test document for embedding generation.

Hartonomous is a cognitive database system that combines:
- Atomic decomposition of all data types
- Semantic embeddings for cross-modal search
- Spatial indexing with Hilbert curves
- Real-time transformer inference with CLR functions

This test will verify the complete end-to-end flow.
"@ | Out-File -FilePath $testFile -Encoding UTF8
    Write-Host "? Test file created: $testFile" -ForegroundColor Green
} else {
    Write-Host "? Test file exists: $testFile" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 5: Start Embedding Generator Worker" -ForegroundColor Yellow
Write-Host "   Starting worker in separate window..." -ForegroundColor Gray

$workerJob = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project `"$workerDir\Hartonomous.Workers.EmbeddingGenerator.csproj`" --configuration Debug" `
    -WorkingDirectory $workerDir `
    -PassThru `
    -WindowStyle Normal

Write-Host "? Worker started (PID: $($workerJob.Id))" -ForegroundColor Green
Write-Host "   Watch the worker window for log output" -ForegroundColor Gray

Write-Host ""
Write-Host "Step 6: Upload Test File via API" -ForegroundColor Yellow
Write-Host "   Option 1: Use Postman/Swagger to upload $testFile" -ForegroundColor Gray
Write-Host "   Option 2: Run this SQL to manually create job:" -ForegroundColor Gray
Write-Host @"

USE Hartonomous;
INSERT INTO dbo.BackgroundJob (JobType, Payload, Status, TenantId, Priority, MaxRetries, AttemptCount, CreatedAtUtc)
VALUES (
    'GenerateEmbedding',
    '{"AtomId": <INSERT_ATOM_ID>, "TenantId": 0, "Modality": "text"}',
    0, -- Pending
    0, -- TenantId
    5, -- Priority
    3, -- MaxRetries
    0, -- AttemptCount
    GETUTCDATE()
);
"@ -ForegroundColor Cyan

Write-Host ""
Write-Host "Step 7: Monitor Job Processing" -ForegroundColor Yellow
Write-Host "   Run this query to check job status:" -ForegroundColor Gray
Write-Host @"

-- Check pending jobs
SELECT TOP 5 
    JobId,
    JobType,
    Status,
    Payload,
    CreatedAtUtc
FROM dbo.BackgroundJob
WHERE JobType = 'GenerateEmbedding'
ORDER BY CreatedAtUtc DESC;

-- Check embeddings created
SELECT TOP 5 
    AtomId,
    Dimension,
    SpatialKey.ToString() AS SpatialKey,
    HilbertValue,
    SpatialBucketX,
    SpatialBucketY,
    SpatialBucketZ,
    CreatedAt
FROM dbo.AtomEmbedding 
ORDER BY CreatedAt DESC;
"@ -ForegroundColor Cyan

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "? Test Environment Ready!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Watch worker logs in the spawned window" -ForegroundColor White
Write-Host "2. Upload test file or create job manually" -ForegroundColor White
Write-Host "3. Verify embedding created with spatial data" -ForegroundColor White
Write-Host "4. Press Ctrl+C to stop worker when done" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to stop worker and exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Cleanup
Write-Host ""
Write-Host "Stopping worker..." -ForegroundColor Yellow
Stop-Process -Id $workerJob.Id -Force
Write-Host "? Worker stopped" -ForegroundColor Green
