$files = @(
    'TimeSeriesVectorAggregates.cs',
    'RecommenderAggregates.cs',
    'ReasoningFrameworkAggregates.cs',
    'NeuralVectorAggregates.cs',
    'GraphVectorAggregates.cs',
    'DimensionalityReductionAggregates.cs',
    'AttentionGeneration.cs',
    'AnomalyDetectionAggregates.cs',
    'Analysis\AutonomousAnalyticsTVF.cs',
    'AdvancedVectorAggregates.cs'
)

foreach ($file in $files) {
    $path = Join-Path 'd:/Repositories/Hartonomous/src/SqlClr' $file
    $content = Get-Content $path -Raw
    
    # Replace SimpleJson.FloatArray with JsonConvert.SerializeObject
    $content = $content -replace 'SimpleJson\.FloatArray\(([^\)]+)\)', 'JsonConvert.SerializeObject($1)'
    
    # Replace SimpleJson.Number with direct value
    $content = $content -replace 'SimpleJson\.Number\(([^\)]+)\)', '$1'
    
    # Store original for multi-line Object replacements
    $lines = $content -split "`r?`n"
    $newLines = @()
    $skip = 0
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($skip -gt 0) {
            $skip--
            continue
        }
        
        $line = $lines[$i]
        
        # Detect SimpleJson.Object( start
        if ($line -match '^\s+var\s+\w+\s*=\s*SimpleJson\.Object\(') {
            # Collect all lines until closing );
            $objLines = @($line)
            $depth = 1
            $j = $i + 1
            while ($j -lt $lines.Count -and $depth -gt 0) {
                $nextLine = $lines[$j]
                $objLines += $nextLine
                if ($nextLine -match '\(') { $depth++ }
                if ($nextLine -match '\)') { $depth-- }
                $j++
            }
            
            # Parse tuple entries
            $tuples = @()
            $objContent = ($objLines -join ' ') -replace '^\s+var\s+(\w+)\s*=\s*SimpleJson\.Object\(\s*', '' -replace '\s*\);?\s*$', ''
            $parts = $objContent -split ',\s*(?=\()'
            
            foreach ($part in $parts) {
                if ($part -match '\("([^"]+)",\s*(.+)\)') {
                    $key = $matches[1]
                    $value = $matches[2].Trim()
                    # Strip SimpleJson.* wrappers
                    $value = $value -replace 'SimpleJson\.FloatArray\(([^\)]+)\)', 'JsonConvert.SerializeObject($1)'
                    $value = $value -replace 'SimpleJson\.Number\(([^\)]+)\)', '$1'
                    $value = $value -replace 'SimpleJson\.Array\(([^\)]+)\)', 'JsonConvert.SerializeObject($1)'
                    $tuples += "                $key = $value"
                }
            }
            
            $varName = if ($objLines[0] -match 'var\s+(\w+)\s*=') { $matches[1] } else { 'result' }
            $indent = if ($objLines[0] -match '^(\s+)') { $matches[1] } else { '            ' }
            
            $newLines += "${indent}var $varName = new"
            $newLines += "${indent}{"
            $newLines += $tuples
            $newLines += "${indent}};"
            
            $skip = ($objLines.Count - 1)
            continue
        }
        
        # Handle direct SimpleJson.Array calls
        $line = $line -replace 'SimpleJson\.Array\(([^\)]+)\)', 'JsonConvert.SerializeObject($1)'
        
        $newLines += $line
    }
    
    $newContent = $newLines -join "`r`n"
    Set-Content $path $newContent -NoNewline
    Write-Host "Converted $file"
}
