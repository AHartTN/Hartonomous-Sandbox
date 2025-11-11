# Enhanced audit script - Track file lifecycles and sabotage patterns
# For each file: created where? modified how? deleted when? restored when? wired up?

$outputFile = "d:\Repositories\Hartonomous\audit\FILE_SABOTAGE_TRACKING.md"
$fileLifecycles = @{}
$violations = @()

# Get all commits in chronological order
$commits = git log --oneline --reverse | ForEach-Object { ($_ -split ' ')[0] }

Write-Host "Building file lifecycle map across $($commits.Count) commits..."

$commitNum = 1
foreach ($commit in $commits) {
    Write-Host "Processing commit $commitNum/$($commits.Count): $commit"
    
    # Get detailed file status for this commit
    $nameStatus = git show --name-status --format="%ai|%an|%s" $commit
    $lines = $nameStatus -split "`n"
    
    $metadata = $lines[0] -split '\|'
    $date = $metadata[0]
    $author = $metadata[1]
    $message = $metadata[2]
    
    # Parse file changes
    foreach ($line in $lines[1..($lines.Length-1)]) {
        if ($line -match '^([AMDRC]\d*)\s+(.+?)(\s+(.+))?$') {
            $status = $matches[1]
            $file = $matches[2]
            $newFile = if ($matches[4]) { $matches[4].Trim() } else { $null }
            
            # Skip build artifacts
            if ($file -match '[\\/](bin|obj)[\\/]' -or 
                $file -match '\.(dll|pdb|exe|cache)$' -or
                ($newFile -and $newFile -match '[\\/](bin|obj)[\\/]')) {
                continue
            }
            
            # Initialize file tracking
            if (-not $fileLifecycles.ContainsKey($file)) {
                $fileLifecycles[$file] = @{
                    FirstSeen = $commit
                    FirstSeenDate = $date
                    FirstSeenMessage = $message
                    History = @()
                    CurrentStatus = 'UNKNOWN'
                    DeletedIn = $null
                    RestoredIn = $null
                    MovedTo = $null
                }
            }
            
            $fileLifecycles[$file].History += @{
                Commit = $commit
                CommitNum = $commitNum
                Date = $date
                Status = $status
                Message = $message
                MovedTo = $newFile
            }
            
            # Track specific patterns
            switch -Regex ($status) {
                '^A' {
                    # File added
                    if ($fileLifecycles[$file].CurrentStatus -eq 'DELETED') {
                        # PATTERN: File was deleted and now re-added
                        $fileLifecycles[$file].RestoredIn = $commit
                        $violations += @{
                            Type = 'DELETE_THEN_RESTORE'
                            File = $file
                            DeletedIn = $fileLifecycles[$file].DeletedIn
                            RestoredIn = $commit
                            Message = "File deleted then restored - check if complete"
                        }
                    }
                    $fileLifecycles[$file].CurrentStatus = 'EXISTS'
                }
                '^D' {
                    # File deleted
                    $fileLifecycles[$file].CurrentStatus = 'DELETED'
                    $fileLifecycles[$file].DeletedIn = $commit
                    
                    # Check if file was recently created (within 10 commits)
                    $createdCommit = ($fileLifecycles[$file].History | Where-Object { $_.Status -match '^A' } | Select-Object -First 1).CommitNum
                    if ($createdCommit -and ($commitNum - $createdCommit) -lt 10) {
                        $violations += @{
                            Type = 'CREATED_THEN_DELETED_QUICKLY'
                            File = $file
                            CreatedIn = $fileLifecycles[$file].FirstSeen
                            DeletedIn = $commit
                            CommitSpan = $commitNum - $createdCommit
                            Message = "File created then deleted within $($commitNum - $createdCommit) commits"
                        }
                    }
                }
                '^R\d+' {
                    # File renamed/moved
                    $fileLifecycles[$file].CurrentStatus = 'MOVED'
                    $fileLifecycles[$file].MovedTo = $newFile
                    
                    # Track the new file location
                    if ($newFile -and -not $fileLifecycles.ContainsKey($newFile)) {
                        $fileLifecycles[$newFile] = @{
                            FirstSeen = $commit
                            FirstSeenDate = $date
                            FirstSeenMessage = "Renamed from $file"
                            History = @()
                            CurrentStatus = 'EXISTS'
                            MovedFrom = $file
                        }
                    }
                }
                '^M' {
                    # File modified
                    $fileLifecycles[$file].CurrentStatus = 'EXISTS'
                }
            }
        }
    }
    
    $commitNum++
}

Write-Host "`nGenerating sabotage report..."

# Write output
"# File Lifecycle Sabotage Tracking`n" | Out-File $outputFile -Encoding UTF8
"**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" | Add-Content $outputFile -Encoding UTF8
"**Total Files Tracked:** $($fileLifecycles.Count)`n" | Add-Content $outputFile -Encoding UTF8
"**Violations Found:** $($violations.Count)`n" | Add-Content $outputFile -Encoding UTF8
"---`n" | Add-Content $outputFile -Encoding UTF8

# Report violations by type
"`n## Sabotage Pattern: Files Created Then Deleted`n" | Add-Content $outputFile -Encoding UTF8

$createdThenDeleted = $violations | Where-Object { $_.Type -eq 'CREATED_THEN_DELETED_QUICKLY' } | Sort-Object CommitSpan
"**Count:** $($createdThenDeleted.Count)`n" | Add-Content $outputFile -Encoding UTF8

foreach ($v in $createdThenDeleted) {
    "### File: ``$($v.File)``" | Add-Content $outputFile -Encoding UTF8
    "- **Created:** $($v.CreatedIn)" | Add-Content $outputFile -Encoding UTF8
    "- **Deleted:** $($v.DeletedIn)" | Add-Content $outputFile -Encoding UTF8
    "- **Lifespan:** $($v.CommitSpan) commits" | Add-Content $outputFile -Encoding UTF8
    "- **Issue:** $($v.Message)`n" | Add-Content $outputFile -Encoding UTF8
}

"`n## Sabotage Pattern: Files Deleted Then Restored`n" | Add-Content $outputFile -Encoding UTF8

$deletedThenRestored = $violations | Where-Object { $_.Type -eq 'DELETE_THEN_RESTORE' }
"**Count:** $($deletedThenRestored.Count)`n" | Add-Content $outputFile -Encoding UTF8

foreach ($v in $deletedThenRestored) {
    "### File: ``$($v.File)``" | Add-Content $outputFile -Encoding UTF8
    "- **Deleted:** $($v.DeletedIn)" | Add-Content $outputFile -Encoding UTF8
    "- **Restored:** $($v.RestoredIn)" | Add-Content $outputFile -Encoding UTF8
    "- **Issue:** $($v.Message) - VERIFY IF COMPLETE AND WIRED UP`n" | Add-Content $outputFile -Encoding UTF8
}

"`n## Currently Deleted Files (Never Restored)`n" | Add-Content $outputFile -Encoding UTF8

$permanentlyDeleted = $fileLifecycles.GetEnumerator() | Where-Object { $_.Value.CurrentStatus -eq 'DELETED' }
"**Count:** $($permanentlyDeleted.Count)`n" | Add-Content $outputFile -Encoding UTF8

foreach ($f in ($permanentlyDeleted | Select-Object -First 50)) {
    "- ``$($f.Key)`` - Deleted in $($f.Value.DeletedIn)" | Add-Content $outputFile -Encoding UTF8
}

if ($permanentlyDeleted.Count -gt 50) {
    "`n*(Showing first 50 of $($permanentlyDeleted.Count) deleted files)*`n" | Add-Content $outputFile -Encoding UTF8
}

"`n## Files That Were Moved`n" | Add-Content $outputFile -Encoding UTF8

$movedFiles = $fileLifecycles.GetEnumerator() | Where-Object { $_.Value.MovedTo }
"**Count:** $($movedFiles.Count)`n" | Add-Content $outputFile -Encoding UTF8

foreach ($f in $movedFiles) {
    "- ``$($f.Key)`` → ``$($f.Value.MovedTo)``" | Add-Content $outputFile -Encoding UTF8
}

"`n## Complete File Lifecycle Details`n" | Add-Content $outputFile -Encoding UTF8
"*(Files with complex histories - multiple adds/deletes/moves)*`n" | Add-Content $outputFile -Encoding UTF8

$complexFiles = $fileLifecycles.GetEnumerator() | Where-Object { $_.Value.History.Count -gt 3 } | Sort-Object { $_.Value.History.Count } -Descending

foreach ($f in ($complexFiles | Select-Object -First 30)) {
    "`n### ``$($f.Key)``" | Add-Content $outputFile -Encoding UTF8
    "**Total Changes:** $($f.Value.History.Count)" | Add-Content $outputFile -Encoding UTF8
    "**Current Status:** $($f.Value.CurrentStatus)" | Add-Content $outputFile -Encoding UTF8
    "**History:**`n" | Add-Content $outputFile -Encoding UTF8
    
    foreach ($h in $f.Value.History) {
        $action = switch -Regex ($h.Status) {
            '^A' { 'ADDED' }
            '^M' { 'MODIFIED' }
            '^D' { 'DELETED' }
            '^R' { "RENAMED to $($h.MovedTo)" }
            default { $h.Status }
        }
        "- Commit $($h.CommitNum) ($($h.Commit)): **$action** - $($h.Date) - $($h.Message)" | Add-Content $outputFile -Encoding UTF8
    }
}

Write-Host "`nSabotage tracking complete!"
Write-Host "Report: $outputFile"
Write-Host "Total violations: $($violations.Count)"
Write-Host "- Created then deleted: $($createdThenDeleted.Count)"
Write-Host "- Deleted then restored: $($deletedThenRestored.Count)"
Write-Host "- Permanently deleted: $($permanentlyDeleted.Count)"
Write-Host "- Moved files: $($movedFiles.Count)"
