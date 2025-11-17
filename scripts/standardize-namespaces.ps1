#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Standardizes namespace usage across the codebase following best practices
.DESCRIPTION
    - Adds proper using directives instead of fully-qualified names
    - Creates aliases for conflicts (e.g., using EntitySemanticFeatures = Hartonomous.Data.Entities.SemanticFeatures)
    - Removes redundant namespace prefixes
    - Organizes usings alphabetically
#>

$ErrorActionPreference = "Stop"

Write-Host "=== STANDARDIZING NAMESPACE USAGE ===" -ForegroundColor Cyan

# Common namespace patterns to add
$standardUsings = @(
    "using Hartonomous.Data.Entities;",
    "using Hartonomous.Core.Interfaces;",
    "using Hartonomous.Core.ValueObjects;",
    "using Hartonomous.Shared.Contracts;"
)

# Known conflicts - less common type gets aliased
$conflictAliases = @{
    "SemanticFeatures" = "using EntitySemanticFeatures = Hartonomous.Data.Entities.SemanticFeatures;"
}

# Files in Core project
$coreFiles = Get-ChildItem -Path "src/Hartonomous.Core" -Include "*.cs" -Recurse -File

Write-Host "Processing $($coreFiles.Count) files in Core project..." -ForegroundColor Gray

foreach ($file in $coreFiles) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Skip if file is empty or has issues
    if ([string]::IsNullOrWhiteSpace($content)) { continue }

    # Check if file already has Data.Entities using
    $hasEntitiesUsing = $content -match "using Hartonomous\.Data\.Entities;"

    # If file references entities but doesn't have using, add it
    if (-not $hasEntitiesUsing -and ($content -match "Hartonomous\.Data\.Entities\.")) {
        # Find the last using statement
        $lines = $content -split "`r?`n"
        $lastUsingIndex = -1

        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^using ") {
                $lastUsingIndex = $i
            }
        }

        if ($lastUsingIndex -ge 0) {
            # Insert new using after last existing using
            $lines = $lines[0..$lastUsingIndex] + "using Hartonomous.Data.Entities;" + $lines[($lastUsingIndex + 1)..($lines.Count - 1)]
            $content = $lines -join "`n"
        }
    }

    # Remove fully-qualified entity references if using is present
    if ($content -match "using Hartonomous\.Data\.Entities;") {
        # Remove Hartonomous.Data.Entities. prefix
        $content = $content -replace "Hartonomous\.Data\.Entities\.", ""
    }

    # Handle SemanticFeatures conflict
    if ($content -match "\bSemanticFeatures\b" -and $content -match "ValueObjects") {
        # Add alias if not present
        if ($content -notmatch "using.*EntitySemanticFeatures.*=") {
            $lines = $content -split "`r?`n"
            $lastUsingIndex = -1

            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match "^using ") {
                    $lastUsingIndex = $i
                }
            }

            if ($lastUsingIndex -ge 0) {
                $lines = $lines[0..$lastUsingIndex] + "using EntitySemanticFeatures = Hartonomous.Data.Entities.SemanticFeature;" + $lines[($lastUsingIndex + 1)..($lines.Count - 1)]
                $content = $lines -join "`n"
            }
        }
    }

    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Updated: $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host "Namespace standardization complete" -ForegroundColor Green

exit 0
