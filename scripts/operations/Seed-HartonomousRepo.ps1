<#
.SYNOPSIS
Seeds Hartonomous database with its own source code for self-awareness.

.DESCRIPTION
Ingests all .cs, .sql, and .md files from the Hartonomous repository into the
atomization pipeline. This creates the "cognitive kernel" - the system's understanding
of its own architecture.

This is the critical "eat your own dogfood" step that enables the system to:
- Answer questions about its own implementation
- Self-diagnose architectural issues
- Generate documentation from its own structure
- Improve its own code through self-reflection

.PARAMETER ApiBaseUrl
Base URL of the Hartonomous API (default: http://localhost:5000)

.PARAMETER TenantId
Tenant ID for ingestion (default: 1 for system tenant)

.PARAMETER BatchSize
Number of files to process in parallel (default: 5)

.PARAMETER IncludePatterns
File patterns to include (default: *.cs, *.sql, *.md)

.PARAMETER ExcludePaths
Paths to exclude (default: bin, obj, .git, .vs, node_modules, .archive)

.PARAMETER DryRun
If specified, only lists files without ingesting them

.EXAMPLE
.\Seed-HartonomousRepo.ps1 -ApiBaseUrl "http://localhost:5000" -TenantId 1

.EXAMPLE
.\Seed-HartonomousRepo.ps1 -DryRun

.NOTES
This script requires:
1. Hartonomous API to be running and accessible
2. SQL Server with Hartonomous database deployed
3. Appropriate authentication/authorization configured

The script tracks progress and can be safely restarted - duplicates will be
deduplicated by content hash at the database level (sp_IngestAtoms).
#>

param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [int]$TenantId = 1,
    [int]$BatchSize = 5,
    [string[]]$IncludePatterns = @("*.cs", "*.sql", "*.md"),
    [string[]]$ExcludePaths = @("bin", "obj", ".git", ".vs", "node_modules", ".archive", "packages", ".azurepipelines"),
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Script banner
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Hartonomous Self-Ingestion (Kernel Seeding)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get repository root (script is in /scripts/operations/)
$RepoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Write-Host "Repository Root: $RepoRoot" -ForegroundColor Gray
Write-Host "API Base URL:    $ApiBaseUrl" -ForegroundColor Gray
Write-Host "Tenant ID:       $TenantId" -ForegroundColor Gray
Write-Host "Batch Size:      $BatchSize" -ForegroundColor Gray
Write-Host ""

# Find all matching files
Write-Host "Scanning for files..." -ForegroundColor Yellow
$AllFiles = @()

foreach ($Pattern in $IncludePatterns) {
    $Files = Get-ChildItem -Path $RepoRoot -Filter $Pattern -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
        $FilePath = $_.FullName
        $ShouldExclude = $false
        
        foreach ($ExcludePath in $ExcludePaths) {
            if ($FilePath -like "*\$ExcludePath\*" -or $FilePath -like "*/$ExcludePath/*") {
                $ShouldExclude = $true
                break
            }
        }
        
        -not $ShouldExclude
    }
    
    $AllFiles += $Files
}

Write-Host "Found $($AllFiles.Count) files to ingest" -ForegroundColor Green
Write-Host ""

# Group by extension for statistics
$FilesByType = $AllFiles | Group-Object Extension | Sort-Object Count -Descending
Write-Host "File distribution:" -ForegroundColor Gray
foreach ($Group in $FilesByType) {
    Write-Host "  $($Group.Name): $($Group.Count) files" -ForegroundColor Gray
}
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - No files will be ingested" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Sample files that would be ingested:" -ForegroundColor Gray
    $AllFiles | Select-Object -First 20 | ForEach-Object {
        $RelativePath = $_.FullName.Substring($RepoRoot.Length + 1)
        Write-Host "  $RelativePath" -ForegroundColor Gray
    }
    if ($AllFiles.Count > 20) {
        Write-Host "  ... and $($AllFiles.Count - 20) more" -ForegroundColor Gray
    }
    exit 0
}

# Confirm with user
Write-Host "This will ingest $($AllFiles.Count) files into Tenant $TenantId" -ForegroundColor Yellow
$Confirmation = Read-Host "Continue? (y/n)"
if ($Confirmation -ne 'y') {
    Write-Host "Operation cancelled" -ForegroundColor Red
    exit 0
}
Write-Host ""

# Statistics
$Stats = @{
    TotalFiles = $AllFiles.Count
    SuccessCount = 0
    FailureCount = 0
    TotalBytes = 0
    StartTime = Get-Date
    Errors = @()
}

# Progress tracking
$ProcessedCount = 0
$BatchNumber = 1

# Process files in batches
for ($i = 0; $i -lt $AllFiles.Count; $i += $BatchSize) {
    $Batch = $AllFiles[$i..[Math]::Min($i + $BatchSize - 1, $AllFiles.Count - 1)]
    
    Write-Host "Processing batch $BatchNumber ($($Batch.Count) files)..." -ForegroundColor Cyan
    $BatchNumber++
    
    # Process batch in parallel using jobs
    $Jobs = @()
    
    foreach ($File in $Batch) {
        $Job = Start-Job -ScriptBlock {
            param($ApiUrl, $FilePath, $TenantId)
            
            try {
                # Read file content
                $FileBytes = [System.IO.File]::ReadAllBytes($FilePath)
                $FileName = [System.IO.Path]::GetFileName($FilePath)
                
                # Prepare multipart form data
                $Boundary = [System.Guid]::NewGuid().ToString()
                $ContentType = "multipart/form-data; boundary=$Boundary"
                
                $BodyLines = @(
                    "--$Boundary",
                    "Content-Disposition: form-data; name=`"file`"; filename=`"$FileName`"",
                    "Content-Type: application/octet-stream",
                    "",
                    [System.Text.Encoding]::UTF8.GetString($FileBytes),
                    "--$Boundary--"
                )
                
                $Body = $BodyLines -join "`r`n"
                
                # Call API
                $Response = Invoke-RestMethod `
                    -Uri "$ApiUrl/api/v1/ingestion/file?tenantId=$TenantId" `
                    -Method Post `
                    -ContentType $ContentType `
                    -Body $Body `
                    -TimeoutSec 300 `
                    -ErrorAction Stop
                
                return @{
                    Success = $true
                    FilePath = $FilePath
                    FileName = $FileName
                    Size = $FileBytes.Length
                    Response = $Response
                }
            }
            catch {
                return @{
                    Success = $false
                    FilePath = $FilePath
                    FileName = $FileName
                    Error = $_.Exception.Message
                }
            }
        } -ArgumentList $ApiBaseUrl, $File.FullName, $TenantId
        
        $Jobs += $Job
    }
    
    # Wait for batch to complete
    $JobResults = $Jobs | Wait-Job | Receive-Job
    $Jobs | Remove-Job
    
    # Process results
    foreach ($Result in $JobResults) {
        $ProcessedCount++
        $RelativePath = $Result.FilePath.Substring($RepoRoot.Length + 1)
        
        if ($Result.Success) {
            $Stats.SuccessCount++
            $Stats.TotalBytes += $Result.Size
            
            Write-Host "  ? $RelativePath" -ForegroundColor Green
            Write-Host "    Size: $($Result.Size) bytes, Atoms: $($Result.Response.itemsProcessed)" -ForegroundColor Gray
        }
        else {
            $Stats.FailureCount++
            $Stats.Errors += @{
                File = $RelativePath
                Error = $Result.Error
            }
            
            Write-Host "  ? $RelativePath" -ForegroundColor Red
            Write-Host "    Error: $($Result.Error)" -ForegroundColor Red
        }
    }
    
    # Show progress
    $ProgressPercent = [Math]::Round(($ProcessedCount / $Stats.TotalFiles) * 100, 1)
    Write-Host ""
    Write-Host "Progress: $ProcessedCount / $($Stats.TotalFiles) ($ProgressPercent%)" -ForegroundColor Cyan
    Write-Host ""
    
    # Small delay between batches to avoid overwhelming API
    if ($i + $BatchSize -lt $AllFiles.Count) {
        Start-Sleep -Milliseconds 500
    }
}

# Calculate summary statistics
$Stats.EndTime = Get-Date
$Stats.Duration = $Stats.EndTime - $Stats.StartTime
$Stats.AvgFileSizeMB = [Math]::Round($Stats.TotalBytes / 1MB / $Stats.SuccessCount, 2)
$Stats.TotalSizeMB = [Math]::Round($Stats.TotalBytes / 1MB, 2)
$Stats.FilesPerMinute = [Math]::Round($Stats.SuccessCount / $Stats.Duration.TotalMinutes, 1)

# Display summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Ingestion Complete" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Results:" -ForegroundColor Green
Write-Host "  Success:      $($Stats.SuccessCount) files" -ForegroundColor Green
Write-Host "  Failed:       $($Stats.FailureCount) files" -ForegroundColor $(if ($Stats.FailureCount -gt 0) { "Red" } else { "Gray" })
Write-Host "  Total Size:   $($Stats.TotalSizeMB) MB" -ForegroundColor Gray
Write-Host "  Avg Size:     $($Stats.AvgFileSizeMB) MB" -ForegroundColor Gray
Write-Host "  Duration:     $([Math]::Round($Stats.Duration.TotalMinutes, 1)) minutes" -ForegroundColor Gray
Write-Host "  Throughput:   $($Stats.FilesPerMinute) files/min" -ForegroundColor Gray
Write-Host ""

# Show errors if any
if ($Stats.Errors.Count -gt 0) {
    Write-Host "Errors:" -ForegroundColor Red
    foreach ($Error in $Stats.Errors) {
        Write-Host "  $($Error.File): $($Error.Error)" -ForegroundColor Red
    }
    Write-Host ""
}

# Next steps
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Verify atom ingestion: SELECT COUNT(*) FROM dbo.Atom WHERE TenantId = $TenantId" -ForegroundColor Gray
Write-Host "  2. Check Neo4j graph:     MATCH (n) WHERE n.tenantId = $TenantId RETURN count(n)" -ForegroundColor Gray
Write-Host "  3. Test self-query:       Ask API 'What is the IngestionService?'" -ForegroundColor Gray
Write-Host ""
Write-Host "The cognitive kernel is now seeded. The system can reflect on its own architecture." -ForegroundColor Green
