# =============================================
# Run-CoreTests.ps1
# Quick validation script for Hartonomous
# Runs unit and database tests with summary
# =============================================

$ErrorActionPreference = "Continue"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "HARTONOMOUS CORE TESTS" -ForegroundColor Cyan
Write-Host "Quick validation of unit and database tests" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$totalPassed = 0
$totalFailed = 0
$totalSkipped = 0

# =============================================
# UNIT TESTS
# =============================================
Write-Host "[1/2] Running Unit Tests..." -ForegroundColor Yellow
Write-Host ""

$unitTestResult = dotnet test tests/Hartonomous.UnitTests --no-build --verbosity quiet --nologo 2>&1 | Out-String

if ($unitTestResult -match "Passed:\s+(\d+)") {
    $unitPassed = [int]$Matches[1]
    $totalPassed += $unitPassed
}

if ($unitTestResult -match "Failed:\s+(\d+)") {
    $unitFailed = [int]$Matches[1]
    $totalFailed += $unitFailed
}

if ($unitTestResult -match "Skipped:\s+(\d+)") {
    $unitSkipped = [int]$Matches[1]
    $totalSkipped += $unitSkipped
}

$unitTotal = $unitPassed + $unitFailed + $unitSkipped

if ($unitFailed -eq 0) {
    Write-Host "  ? Unit Tests: PASSED ($unitPassed/$unitTotal)" -ForegroundColor Green
} else {
    Write-Host "  ? Unit Tests: $unitFailed FAILURES ($unitPassed/$unitTotal passed)" -ForegroundColor Yellow
}

Write-Host ""

# =============================================
# DATABASE TESTS
# =============================================
Write-Host "[2/2] Running Database Tests..." -ForegroundColor Yellow
Write-Host "  (Uses SQL Server LocalDB - no Docker required)" -ForegroundColor Gray
Write-Host ""

$dbTestResult = dotnet test tests/Hartonomous.DatabaseTests --no-build --verbosity quiet --nologo 2>&1 | Out-String

if ($dbTestResult -match "Passed:\s+(\d+)") {
    $dbPassed = [int]$Matches[1]
    $totalPassed += $dbPassed
} else {
    $dbPassed = 0
}

if ($dbTestResult -match "Failed:\s+(\d+)") {
    $dbFailed = [int]$Matches[1]
    $totalFailed += $dbFailed
} else {
    $dbFailed = 0
}

if ($dbTestResult -match "Skipped:\s+(\d+)") {
    $dbSkipped = [int]$Matches[1]
    $totalSkipped += $dbSkipped
} else {
    $dbSkipped = 0
}

$dbTotal = $dbPassed + $dbFailed + $dbSkipped

if ($dbTotal -eq 0) {
    Write-Host "  ? Database Tests: No tests found" -ForegroundColor Yellow
} elseif ($dbFailed -eq 0) {
    Write-Host "  ? Database Tests: PASSED ($dbPassed/$dbTotal)" -ForegroundColor Green
} else {
    Write-Host "  ? Database Tests: $dbFailed FAILURES ($dbPassed/$dbTotal passed)" -ForegroundColor Yellow
}

Write-Host ""

# =============================================
# SUMMARY
# =============================================
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

$totalTests = $totalPassed + $totalFailed + $totalSkipped
$passRate = if ($totalTests -gt 0) { [math]::Round(($totalPassed / $totalTests) * 100, 1) } else { 0 }

Write-Host ""
Write-Host "Total Tests:   $totalTests" -ForegroundColor White
Write-Host "Passed:        $totalPassed" -ForegroundColor Green
Write-Host "Failed:        $totalFailed" -ForegroundColor $(if ($totalFailed -eq 0) { "Green" } else { "Red" })
Write-Host "Skipped:       $totalSkipped" -ForegroundColor Gray
Write-Host "Pass Rate:     $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })
Write-Host ""

if ($totalFailed -eq 0) {
    Write-Host "? ALL TESTS PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "? $totalFailed TEST(S) FAILED" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To see detailed failures:" -ForegroundColor Gray
    Write-Host "  dotnet test tests/Hartonomous.UnitTests --verbosity normal" -ForegroundColor Gray
    Write-Host "  dotnet test tests/Hartonomous.DatabaseTests --verbosity normal" -ForegroundColor Gray
    exit 1
}
