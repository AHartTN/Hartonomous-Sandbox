# Extract and categorize ALL build warnings
$logFile = "D:\Repositories\Hartonomous\build-output.log"
$output = "D:\Repositories\Hartonomous\warnings-detailed.txt"

$content = Get-Content $logFile -Raw
$warnings = $content -split "`r?`n" | Where-Object { $_ -match "Build warning" }

# Group by file
$byFile = @{}
foreach ($warning in $warnings) {
    if ($warning -match "([^\\]+\.sql)\((\d+),(\d+),(\d+),(\d+)\): Build warning (SQL\d+): (.+) \[") {
        $file = $matches[1]
        $line = $matches[2]
        $code = $matches[6]
        $message = $matches[7]
        
        if (-not $byFile.ContainsKey($file)) {
            $byFile[$file] = @()
        }
        $byFile[$file] += @{
            Line = $line
            Code = $code
            Message = $message
            Full = $warning
        }
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("=== DACPAC BUILD WARNINGS - COMPREHENSIVE REPORT ===")
[void]$sb.AppendLine("Total Warnings: $($warnings.Count)")
[void]$sb.AppendLine("")

# Sort files by warning count
$sorted = $byFile.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending

foreach ($entry in $sorted) {
    $file = $entry.Key
    $fileWarnings = $entry.Value
    
    [void]$sb.AppendLine("=" * 80)
    [void]$sb.AppendLine("FILE: $file ($($fileWarnings.Count) warnings)")
    [void]$sb.AppendLine("=" * 80)
    
    # Group by warning type
    $byType = $fileWarnings | Group-Object { $_.Message -replace '\[.*?\]', '[OBJECT]' }
    
    foreach ($group in $byType) {
        [void]$sb.AppendLine("")
        [void]$sb.AppendLine("  Pattern: $($group.Name)")
        [void]$sb.AppendLine("  Count: $($group.Count)")
        [void]$sb.AppendLine("  Lines: $($group.Group | ForEach-Object { $_.Line } | Sort-Object -Unique | Join-String -Separator ', ')")
        [void]$sb.AppendLine("")
        $group.Group | Select-Object -First 2 | ForEach-Object {
            [void]$sb.AppendLine("    $($_.Full)")
        }
    }
    [void]$sb.AppendLine("")
}

$sb.ToString() | Out-File $output -Encoding UTF8
Write-Host "Detailed warnings written to: $output" -ForegroundColor Green
Write-Host "Total files with warnings: $($byFile.Count)" -ForegroundColor Yellow
Write-Host "Total warnings: $($warnings.Count)" -ForegroundColor Yellow
