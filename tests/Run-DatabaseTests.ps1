<#
.SYNOPSIS
    Executes Hartonomous database test suite and generates NUnit XML results

.DESCRIPTION
    Runs the database test suite and exports results in NUnit XML format
    for Azure DevOps test result publishing

.PARAMETER Server
    SQL Server instance (default: localhost)

.PARAMETER Database
    Database name (default: Hartonomous)

.PARAMETER OutputPath
    Path to save test results XML (default: ./test-results.xml)

.EXAMPLE
    .\Run-DatabaseTests.ps1 -Server localhost -Database Hartonomous -OutputPath test-results.xml
#>

param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [string]$OutputPath = "test-results.xml"
)

$ErrorActionPreference = "Stop"

Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host " HARTONOMOUS DATABASE TEST RUNNER" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server:   $Server" -ForegroundColor Gray
Write-Host "Database: $Database" -ForegroundColor Gray
Write-Host "Output:   $OutputPath" -ForegroundColor Gray
Write-Host ""

# Execute test suite
Write-Host "Executing test suite..." -ForegroundColor Yellow

try {
    $scriptPath = Join-Path $PSScriptRoot "test-runner.sql"
    
    if (-not (Test-Path $scriptPath)) {
        throw "Test runner script not found: $scriptPath"
    }

    # Run tests
    $output = sqlcmd -S $Server -d $Database -E -C -i $scriptPath 2>&1
    $exitCode = $LASTEXITCODE

    # Display output
    Write-Host $output

    # Extract XML from database using sqlcmd with :XML ON and -o
    Write-Host ""
    Write-Host "Extracting test results..." -ForegroundColor Yellow

    # Create temporary SQL script to extract XML
    $extractScript = @"
:XML ON
SELECT TOP 1 ResultsXml
FROM dbo.TestRunResults
ORDER BY ExecutedAt DESC
GO
"@

    $tempScript = Join-Path $env:TEMP "extract-test-results.sql"
    $extractScript | Out-File -FilePath $tempScript -Encoding ASCII

    # Run sqlcmd with XML output to file
    sqlcmd -S $Server -d $Database -E -C -i $tempScript -o $OutputPath -h-1 2>&1 | Out-Null
    
    # Clean up temp script
    Remove-Item $tempScript -ErrorAction SilentlyContinue
    
    if (Test-Path $OutputPath) {
        # Read the file and decode HTML entities  
        $xmlContent = Get-Content -Path $OutputPath -Raw
        $xmlContent = $xmlContent -replace '&lt;', '<' -replace '&gt;', '>' -replace '&quot;', '"' -replace '&amp;', '&'
        $xmlContent = $xmlContent.Trim()
        
        # Rewrite cleaned XML
        $xmlContent | Out-File -FilePath $OutputPath -Encoding UTF8
        
        Write-Host "? Test results saved to: $OutputPath" -ForegroundColor Green
        
        # Parse and display summary
        try {
            [xml]$xml = $xmlContent
            $testRun = $xml.'test-run'
            
            Write-Host ""
            Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
            Write-Host " TEST SUMMARY" -ForegroundColor Cyan
            Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
            Write-Host "Total:    $($testRun.total)" -ForegroundColor Gray
            Write-Host "Passed:   $($testRun.passed)" -ForegroundColor Green
            Write-Host "Failed:   $($testRun.failed)" -ForegroundColor $(if ([int]$testRun.failed -gt 0) { "Red" } else { "Gray" })
            Write-Host "Duration: $($testRun.duration)s" -ForegroundColor Gray
            Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
            Write-Host ""
        }
        catch {
            Write-Warning "Could not parse XML summary: $_"
        }
    }
    else {
        Write-Warning "Test results file not created: $OutputPath"
    }

    # Return appropriate exit code
    if ($exitCode -ne 0) {
        Write-Host "? TESTS FAILED" -ForegroundColor Red
        exit 1
    }
    else {
        Write-Host "? ALL TESTS PASSED" -ForegroundColor Green
        exit 0
    }
}
catch {
    Write-Host ""
    Write-Host "? ERROR: $_" -ForegroundColor Red
    exit 1
}
