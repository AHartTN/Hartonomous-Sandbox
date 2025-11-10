param(
    [string]$SourceDir = "d:\Repositories\Hartonomous\src\Hartonomous.Database"
)

Write-Host "Cleaning SQL files for DACPAC compatibility..." -ForegroundColor Cyan

Get-ChildItem -Path $SourceDir -Filter *.sql -Recurse | ForEach-Object {
    $filePath = $_.FullName
    $content = Get-Content -Path $filePath -Raw
    
    if ($null -eq $content -or $content.Trim() -eq '') {
        Write-Host "Skipping empty file: $($_.Name)" -ForegroundColor Yellow
        return
    }
    
    $modified = $false
    
    # Remove USE database statements
    if ($content -match 'USE\s+\w+\s*;\s*GO') {
        $content = $content -replace 'USE\s+\w+\s*;\s*GO\s*', ''
        $modified = $true
    }
    
    # Remove IF OBJECT_ID wrappers for CREATE TABLE
    if ($content -match 'IF OBJECT_ID.*?\bBEGIN\s*CREATE TABLE') {
        $content = $content -replace 'IF OBJECT_ID\([^)]+\)[^\r\n]*\s*BEGIN\s*', ''
        $content = $content -replace '\s*END\s*GO\s*(?=IF|$)', "`nGO`n"
        $modified = $true
    }
    
    # Remove IF OBJECT_ID...DROP statements (handled by pre-deployment)
    if ($content -match 'IF OBJECT_ID.*?DROP') {
        $content = $content -replace 'IF OBJECT_ID\([^)]+\)[^\r\n]*DROP[^\r\n]*;?\s*', ''
        $modified = $true
    }
    
    # Remove IF EXISTS wrappers for CREATE INDEX/SPATIAL INDEX
    if ($content -match 'IF NOT EXISTS.*?CREATE (SPATIAL )?INDEX') {
        $content = $content -replace 'IF NOT EXISTS\s*\([^)]+\)\s*BEGIN\s*', ''
        $content = $content -replace '\s*END\s*GO\s*(?=IF|PRINT|$)', "`nGO`n"
        $modified = $true
    }
    
    # Remove PRINT statements
    if ($content -match 'PRINT\s+''') {
        $content = $content -replace "PRINT\s+'[^']*';\s*", ''
        $modified = $true
    }
    
    # Clean up excessive whitespace
    $content = $content -replace '(\r?\n){3,}', "`n`n"
    $content = $content.Trim() + "`n"
    
    if ($modified) {
        Set-Content -Path $filePath -Value $content -NoNewline
        Write-Host "Cleaned: $($_.Name)" -ForegroundColor Green
    }
}

Write-Host "`nCompleted cleaning SQL files." -ForegroundColor Cyan
