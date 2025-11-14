# =============================================
# Batch Delete: v4-Incompatible Procedures
# Deletes procedures referencing deleted tables and removed columns
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "=== Batch Delete: v4-Incompatible Procedures ===" -ForegroundColor Cyan
Write-Host ""

# Procedures referencing deleted tables or removed v4-only columns
$proceduresToDelete = @(
    # Deleted table references (AtomsLOB, AtomPayloadStore, etc.)
    "dbo.sp_AtomizeAudio.sql",
    "dbo.sp_AtomizeImage.sql",
    "dbo.sp_AtomizeModel.sql",
    "dbo.sp_ExtractStudentModel.sql",
    "dbo.sp_GetRecentWeightChanges.sql",
    "dbo.sp_CompareWeightsAtTimes.sql",
    "dbo.sp_GetWeightEvolution.sql",
    "dbo.sp_CreateWeightSnapshot.sql",

    # v4 atomic procedures (replaced by _Governed versions)
    "dbo.sp_AtomizeModel_Atomic.sql",
    "dbo.sp_AtomizeAudio_Atomic.sql",
    "dbo.sp_AtomizeImage_Atomic.sql",
    "dbo.sp_AtomizeText_Atomic.sql",

    # Other v4 incompatible procedures
    "dbo.sp_DeleteAtomicVectors.sql",
    "dbo.sp_GetAtomicDeduplicationStats.sql",
    "dbo.sp_ManageHartonomousIndexes.sql",
    "Autonomy.SelfImprovement.sql",
    "dbo.sp_AutonomousImprovement.sql",
    "dbo.sp_RecordUsage.sql",
    "dbo.sp_ReconstructOperationTimeline.sql",

    # Procedure referencing non-existent view
    "dbo.sp_ReconstructVector.sql"
)

Write-Host "Procedures to delete: $($proceduresToDelete.Count)" -ForegroundColor Yellow
Write-Host ""

$deletedCount = 0
$notFoundCount = 0
$errorCount = 0

foreach ($procFile in $proceduresToDelete) {
    $path = "src\Hartonomous.Database\Procedures\$procFile"

    try {
        if (Test-Path $path) {
            Remove-Item $path -Force
            Write-Host "[DELETED] $procFile" -ForegroundColor Green
            $deletedCount++
        } else {
            Write-Host "[NOT FOUND] $procFile" -ForegroundColor Gray
            $notFoundCount++
        }
    }
    catch {
        Write-Host "[ERROR] Failed to delete $procFile : $_" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Deleted: $deletedCount procedures" -ForegroundColor Green
Write-Host "Not found: $notFoundCount procedures" -ForegroundColor Gray
Write-Host "Errors: $errorCount procedures" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })

Write-Host ""
Write-Host "Done! These procedures are incompatible with v5 design:" -ForegroundColor Cyan
Write-Host "  - Reference deleted tables (AtomsLOB, AtomPayloadStore, etc.)" -ForegroundColor Yellow
Write-Host "  - Use removed columns (Content NVARCHAR(MAX), Metadata, etc.)" -ForegroundColor Yellow
Write-Host "  - Violate 64-byte atomic limit" -ForegroundColor Yellow
Write-Host "  - Replaced by _Governed versions or deleted by design" -ForegroundColor Yellow
