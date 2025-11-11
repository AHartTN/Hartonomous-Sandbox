# PowerShell script to audit all 196 commits systematically
# Outputs detailed audit report with actual file changes

$outputFile = "d:\Repositories\Hartonomous\audit\FULL_COMMIT_AUDIT.md"
$violationCount = 0

# Get all commits in chronological order
$commits = git log --oneline --reverse | ForEach-Object { ($_ -split ' ')[0] }

"# Complete Commit Audit: All 196 Commits`n" | Out-File $outputFile -Encoding UTF8
"**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" | Add-Content $outputFile -Encoding UTF8
"**Total Commits:** $($commits.Count)`n" | Add-Content $outputFile -Encoding UTF8
"---`n" | Add-Content $outputFile -Encoding UTF8

$commitNum = 1
foreach ($commit in $commits) {
    Write-Host "Processing commit $commitNum/$($commits.Count): $commit"
    
    # Get commit details
    $stats = git show --stat --format="%ai|%an|%s" $commit
    $statLines = $stats -split "`n"
    
    $metadata = $statLines[0] -split '\|'
    $date = $metadata[0]
    $author = $metadata[1]
    $message = $metadata[2]
    
    # Write commit header
    "`n## Commit $commitNum`: $commit`n" | Add-Content $outputFile -Encoding UTF8
    "**Date:** $date  " | Add-Content $outputFile -Encoding UTF8
    "**Author:** $author  " | Add-Content $outputFile -Encoding UTF8
    "**Message:** $message`n" | Add-Content $outputFile -Encoding UTF8
    
    # Parse file changes
    "### Files Changed:`n" | Add-Content $outputFile -Encoding UTF8
    
    $inFileList = $false
    $additions = 0
    $deletions = 0
    $filesAdded = @()
    $filesModified = @()
    $filesDeleted = @()
    $filesRenamed = @()
    
    foreach ($line in $statLines[1..($statLines.Length-1)]) {
        if ($line -match '^\s+\d+\s+files? changed') {
            # Summary line
            if ($line -match '(\d+) insertion') {
                $additions = [int]$matches[1]
            }
            if ($line -match '(\d+) deletion') {
                $deletions = [int]$matches[1]
            }
        }
        elseif ($line -match '^\s+(.+?)\s+\|\s+(\d+|Bin)') {
            $file = $matches[1].Trim()
            $changes = $matches[2]
            "- ``$file`` ($changes)" | Add-Content $outputFile -Encoding UTF8
        }
    }
    
    "`n**Summary:** +$additions, -$deletions`n" | Add-Content $outputFile -Encoding UTF8
    
    # Check for suspicious patterns
    $violations = @()
    
    if ($message -match 'WIP|Manual|Progress commit') {
        $violations += "VIOLATION: Vague commit message"
    }
    
    if ($message -match 'AI agent|stupid|suck|sabotage') {
        $violations += "VIOLATION: AI agent frustration documented"
        $violationCount++
    }
    
    if ($deletions -gt 100 -and $message -notmatch 'delete|remove|clean') {
        $violations += "VIOLATION: Large deletion ($deletions lines) without clear justification"
        $violationCount++
    }
    
    if ($message -match 'complete|done|finished' -and $additions -lt 50) {
        $violations += "WARNING: Claims completion but minimal code added"
    }
    
    if ($violations.Count -gt 0) {
        "`n**⚠️ ISSUES DETECTED:**`n" | Add-Content $outputFile -Encoding UTF8
        foreach ($v in $violations) {
            "- $v`n" | Add-Content $outputFile -Encoding UTF8
        }
    }
    
    "---`n" | Add-Content $outputFile -Encoding UTF8
    
    $commitNum++
}

"`n## Audit Summary`n" | Add-Content $outputFile -Encoding UTF8
"**Total Violations Found:** $violationCount`n" | Add-Content $outputFile -Encoding UTF8

Write-Host "`nAudit complete. Report saved to: $outputFile"
Write-Host "Total violations found: $violationCount"
