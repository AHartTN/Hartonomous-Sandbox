# =============================================
# BUILD VALIDATION SCRIPT
# Ensures zero warnings and zero errors
# =============================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$TreatWarningsAsErrors
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptRoot

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "HARTONOMOUS BUILD VALIDATION" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Treat Warnings As Errors: $TreatWarningsAsErrors" -ForegroundColor White
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

Push-Location $RepoRoot

try {
    # =============================================
    # STEP 1: CLEAN
    # =============================================
    Write-Host "[STEP 1] Cleaning solution..." -ForegroundColor Yellow
    
    dotnet clean Hartonomous.sln --configuration $Configuration --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Clean successful" -ForegroundColor Green
    } else {
        Write-Host "  ? Clean failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    
    # =============================================
    # STEP 2: RESTORE
    # =============================================
    Write-Host "[STEP 2] Restoring packages..." -ForegroundColor Yellow
    
    dotnet restore Hartonomous.sln --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Restore successful" -ForegroundColor Green
    } else {
        Write-Host "  ? Restore failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    
    # =============================================
    # STEP 3: BUILD
    # =============================================
    Write-Host "[STEP 3] Building solution..." -ForegroundColor Yellow
    
    $buildArgs = @(
        "build",
        "Hartonomous.sln",
        "--configuration", $Configuration,
        "--no-restore",
        "--verbosity", "normal"
    )
    
    if ($TreatWarningsAsErrors) {
        $buildArgs += "-warnaserror"
    }
    
    $buildOutput = & dotnet $buildArgs 2>&1 | Out-String
    
    # Parse build output
    $errorPattern = "(\d+) Error\(s\)"
    $warningPattern = "(\d+) Warning\(s\)"
    
    $errorMatch = [regex]::Match($buildOutput, $errorPattern)
    $warningMatch = [regex]::Match($buildOutput, $warningPattern)
    
    $errorCount = if ($errorMatch.Success) { [int]$errorMatch.Groups[1].Value } else { 0 }
    $warningCount = if ($warningMatch.Success) { [int]$warningMatch.Groups[1].Value } else { 0 }
    
    Write-Host ""
    Write-Host "  Build Results:" -ForegroundColor White
    Write-Host "    Errors:   $errorCount" -ForegroundColor $(if ($errorCount -eq 0) { "Green" } else { "Red" })
    Write-Host "    Warnings: $warningCount" -ForegroundColor $(if ($warningCount -eq 0) { "Green" } else { "Yellow" })
    
    if ($errorCount -gt 0) {
        Write-Host ""
        Write-Host "  ? BUILD FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "Errors:" -ForegroundColor Red
        $buildOutput | Select-String "error CS\d+" | ForEach-Object { 
            Write-Host "  $_" -ForegroundColor Red 
        }
        exit 1
    }
    
    if ($warningCount -gt 0) {
        Write-Host ""
        Write-Host "  ? BUILD SUCCEEDED WITH WARNINGS" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        $buildOutput | Select-String "warning CS\d+" | ForEach-Object { 
            Write-Host "  $_" -ForegroundColor Yellow 
        }
        
        if ($TreatWarningsAsErrors) {
            Write-Host ""
            Write-Host "  ? Warnings treated as errors" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host ""
        Write-Host "  ? BUILD SUCCESSFUL (0 errors, 0 warnings)" -ForegroundColor Green
    }
    
    Write-Host ""
    
    # =============================================
    # STEP 4: TEST
    # =============================================
    Write-Host "[STEP 4] Running tests..." -ForegroundColor Yellow
    
    $testOutput = & dotnet test Hartonomous.sln `
        --configuration $Configuration `
        --no-build `
        --verbosity normal `
        --logger "console;verbosity=normal" 2>&1 | Out-String
    
    # Parse test results
    $passedPattern = "Passed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)"
    $testMatch = [regex]::Match($testOutput, $passedPattern)
    
    if ($testMatch.Success) {
        $failed = [int]$testMatch.Groups[1].Value
        $passed = [int]$testMatch.Groups[2].Value
        $skipped = [int]$testMatch.Groups[3].Value
        $total = [int]$testMatch.Groups[4].Value
        
        Write-Host ""
        Write-Host "  Test Results:" -ForegroundColor White
        Write-Host "    Passed:  $passed" -ForegroundColor Green
        Write-Host "    Failed:  $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
        Write-Host "    Skipped: $skipped" -ForegroundColor $(if ($skipped -eq 0) { "Green" } else { "Yellow" })
        Write-Host "    Total:   $total" -ForegroundColor White
        
        $passRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 2) } else { 0 }
        Write-Host "    Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -eq 100) { "Green" } else { "Yellow" })
        
        if ($failed -gt 0) {
            Write-Host ""
            Write-Host "  ? TESTS FAILED" -ForegroundColor Red
            Write-Host ""
            Write-Host "Failed tests:" -ForegroundColor Red
            $testOutput | Select-String "^\s+Failed\s+" | ForEach-Object { 
                Write-Host "  $_" -ForegroundColor Red 
            }
            exit 1
        } else {
            Write-Host ""
            Write-Host "  ? ALL TESTS PASSED" -ForegroundColor Green
        }
    } else {
        Write-Host "  ? Could not parse test results" -ForegroundColor Yellow
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ? Tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    
    Write-Host ""
    
    # =============================================
    # SUMMARY
    # =============================================
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "VALIDATION COMPLETE" -ForegroundColor Cyan
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "? Build:   0 errors, $warningCount warnings" -ForegroundColor Green
    if ($testMatch.Success) {
        Write-Host "? Tests:   $passed/$total passed ($passRate%)" -ForegroundColor Green
    }
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host ""
    
    exit 0

} finally {
    Pop-Location
}
