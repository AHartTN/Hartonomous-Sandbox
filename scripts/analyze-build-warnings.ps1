# Analyze DACPAC Build Warnings
param(
    [string]$LogFile = "D:\Repositories\Hartonomous\build-output.log"
)

$warnings = Get-Content $LogFile | Where-Object { $_ -match "Build warning SQL" }

$categorized = @{
    "CaseSensitivity" = @()
    "MissingTables" = @()
    "MissingProcs" = @()
    "ServiceBroker" = @()
    "SystemViews" = @()
    "AmbiguousReference" = @()
    "Other" = @()
}

foreach ($warning in $warnings) {
    if ($warning -match "SQL71558.*differs only by case") {
        $categorized["CaseSensitivity"] += $warning
    }
    elseif ($warning -match "unresolved reference to object \[dbo\]\.\[sp_") {
        $categorized["MissingProcs"] += $warning
    }
    elseif ($warning -match "unresolved reference to object \[(billing|provenance)\]") {
        $categorized["MissingTables"] += $warning
    }
    elseif ($warning -match "//Hartonomous|Service|Contract|Message") {
        $categorized["ServiceBroker"] += $warning
    }
    elseif ($warning -match "\[sys\]\.\[") {
        $categorized["SystemViews"] += $warning
    }
    elseif ($warning -match "ambiguous") {
        $categorized["AmbiguousReference"] += $warning
    }
    else {
        $categorized["Other"] += $warning
    }
}

Write-Host "`n=== DACPAC Warning Analysis ===" -ForegroundColor Cyan
Write-Host "Total Warnings: $($warnings.Count)" -ForegroundColor Yellow

foreach ($category in $categorized.Keys | Sort-Object) {
    $count = $categorized[$category].Count
    if ($count -gt 0) {
        Write-Host "`n$category : $count warnings" -ForegroundColor Green
        $categorized[$category] | Select-Object -First 3 | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Gray
        }
    }
}

# Output unique case sensitivity issues
Write-Host "`n=== Case Sensitivity Issues ===" -ForegroundColor Cyan
$categorized["CaseSensitivity"] | ForEach-Object {
    if ($_ -match "\[([^\]]+)\]\.\[@?(\w+)\].*differs only by case from.*\[([^\]]+)\]\.\[@?(\w+)\]") {
        Write-Host "  $($matches[1]).$($matches[2]) vs $($matches[3]).$($matches[4])" -ForegroundColor Yellow
    }
} | Sort-Object -Unique
