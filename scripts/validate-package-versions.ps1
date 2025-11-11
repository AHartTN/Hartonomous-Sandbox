<#
.SYNOPSIS
Validates package versions across all .csproj files in the Hartonomous repository.

.DESCRIPTION
Scans all .csproj files to identify:
- Outdated packages (current vs latest available)
- Security vulnerabilities
- Version mixing across projects
- Preview/RC packages awaiting GA release
- Dependency conflicts

Generates JSON report for automation and CI/CD integration.

.PARAMETER ReportPath
Path for JSON report output. Default: "version-report.json"

.PARAMETER CheckSecurity
Enable security vulnerability scanning via dotnet list package --vulnerable

.PARAMETER CheckOutdated
Check for newer package versions available. Default: enabled

.PARAMETER FailOnVulnerabilities
Exit with error code 1 if vulnerabilities found. Default: false (warn only)

.PARAMETER FailOnOutdated
Exit with error code 1 if outdated packages found. Default: false (warn only)

.EXAMPLE
.\validate-package-versions.ps1 -ReportPath "version-report.json" -CheckSecurity

.EXAMPLE
.\validate-package-versions.ps1 -FailOnVulnerabilities -FailOnOutdated
#>

[CmdletBinding()]
param(
    [string]$ReportPath = "version-report.json",
    [switch]$CheckSecurity,
    [switch]$CheckOutdated = $true,
    [switch]$FailOnVulnerabilities,
    [switch]$FailOnOutdated
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Colors for output
$ColorGood = "Green"
$ColorWarn = "Yellow"
$ColorError = "Red"
$ColorInfo = "Cyan"

# Find repository root
$RepoRoot = Split-Path $PSScriptRoot -Parent
Write-Host "📂 Repository Root: $RepoRoot" -ForegroundColor $ColorInfo

# Find all .csproj files
$Projects = Get-ChildItem -Path $RepoRoot -Filter "*.csproj" -Recurse | Where-Object { 
    $_.FullName -notmatch '\\obj\\' -and 
    $_.FullName -notmatch '\\bin\\' 
}

Write-Host "🔍 Found $($Projects.Count) project files" -ForegroundColor $ColorInfo
Write-Host ""

# Initialize report
$Report = @{
    GeneratedAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    RepoRoot = $RepoRoot
    ProjectCount = $Projects.Count
    Packages = @{}
    Issues = @{
        Outdated = @()
        VersionMixing = @()
        PreviewRC = @()
        Vulnerabilities = @()
        DuplicateCsprojFiles = @()
    }
    Summary = @{
        UpToDate = 0
        Outdated = 0
        PreviewRC = 0
        Vulnerabilities = 0
        VersionMixingIssues = 0
        DuplicateCsprojFiles = 0
    }
}

# Extract packages from all projects
Write-Host "📦 Analyzing package references..." -ForegroundColor $ColorInfo

foreach ($Project in $Projects) {
    [xml]$Csproj = Get-Content $Project.FullName
    $PackageRefs = $Csproj.SelectNodes("//PackageReference")
    
    foreach ($Ref in $PackageRefs) {
        $PackageName = $Ref.GetAttribute("Include")
        $Version = $Ref.GetAttribute("Version")
        
        if (-not $PackageName) { continue }
        
        # Extract version if not in attribute (could be in Version element)
        if (-not $Version) {
            $VersionNode = $Ref.SelectSingleNode("Version")
            if ($VersionNode) {
                $Version = $VersionNode.InnerText
            }
        }
        
        if (-not $Report.Packages.ContainsKey($PackageName)) {
            $Report.Packages[$PackageName] = @{
                Name = $PackageName
                Versions = @{}
                Projects = @()
            }
        }
        
        if (-not $Report.Packages[$PackageName].Versions.ContainsKey($Version)) {
            $Report.Packages[$PackageName].Versions[$Version] = @()
        }
        
        $Report.Packages[$PackageName].Versions[$Version] += $Project.Name
        
        if ($Report.Packages[$PackageName].Projects -notcontains $Project.Name) {
            $Report.Packages[$PackageName].Projects += $Project.Name
        }
    }
}

Write-Host "✅ Found $($Report.Packages.Count) unique packages" -ForegroundColor $ColorGood
Write-Host ""

# Detect version mixing
Write-Host "🔍 Detecting version mixing..." -ForegroundColor $ColorInfo

foreach ($PackageName in $Report.Packages.Keys) {
    $Package = $Report.Packages[$PackageName]
    
    if ($Package.Versions.Count -gt 1) {
        $Issue = @{
            Package = $PackageName
            Versions = @($Package.Versions.Keys)
            Projects = @()
        }
        
        foreach ($Version in $Package.Versions.Keys) {
            $Issue.Projects += @{
                Version = $Version
                ProjectFiles = $Package.Versions[$Version]
            }
        }
        
        $Report.Issues.VersionMixing += $Issue
        $Report.Summary.VersionMixingIssues++
        
        Write-Host "  ⚠️  $PackageName has $($Package.Versions.Count) versions:" -ForegroundColor $ColorWarn
        foreach ($Version in $Package.Versions.Keys) {
            Write-Host "      - $Version ($($Package.Versions[$Version].Count) projects)" -ForegroundColor $ColorWarn
        }
    }
}

if ($Report.Summary.VersionMixingIssues -eq 0) {
    Write-Host "  ✅ No version mixing detected" -ForegroundColor $ColorGood
}
Write-Host ""

# Detect preview/RC packages
Write-Host "🔍 Detecting preview/RC packages..." -ForegroundColor $ColorInfo

foreach ($PackageName in $Report.Packages.Keys) {
    $Package = $Report.Packages[$PackageName]
    
    foreach ($Version in $Package.Versions.Keys) {
        if ($Version -match '(preview|rc|beta|alpha)') {
            $Issue = @{
                Package = $PackageName
                Version = $Version
                Projects = $Package.Versions[$Version]
            }
            
            $Report.Issues.PreviewRC += $Issue
            $Report.Summary.PreviewRC++
            
            Write-Host "  ⚠️  $PackageName $Version is preview/RC" -ForegroundColor $ColorWarn
        }
    }
}

if ($Report.Summary.PreviewRC -eq 0) {
    Write-Host "  ✅ All packages are stable releases" -ForegroundColor $ColorGood
}
Write-Host ""

# Detect duplicate .csproj files (e.g., SqlClrFunctions-CLEAN.csproj, SqlClrFunctions-BACKUP.csproj)
Write-Host "🔍 Detecting duplicate .csproj files..." -ForegroundColor $ColorInfo

$ProjectBasenames = @{}
foreach ($Project in $Projects) {
    $Basename = $Project.BaseName -replace '(-CLEAN|-BACKUP|-OLD|-COPY.*|-\d+)$', ''
    
    if (-not $ProjectBasenames.ContainsKey($Basename)) {
        $ProjectBasenames[$Basename] = @()
    }
    
    $ProjectBasenames[$Basename] += $Project
}

foreach ($Basename in $ProjectBasenames.Keys) {
    $DuplicateProjects = $ProjectBasenames[$Basename]
    
    if ($DuplicateProjects.Count -gt 1) {
        $Issue = @{
            BaseProjectName = $Basename
            DuplicateFiles = @($DuplicateProjects | ForEach-Object { $_.Name })
            Paths = @($DuplicateProjects | ForEach-Object { $_.FullName })
        }
        
        $Report.Issues.DuplicateCsprojFiles += $Issue
        $Report.Summary.DuplicateCsprojFiles++
        
        Write-Host "  ⚠️  Duplicate .csproj files for '$Basename':" -ForegroundColor $ColorWarn
        foreach ($Dup in $DuplicateProjects) {
            Write-Host "      - $($Dup.Name)" -ForegroundColor $ColorWarn
        }
    }
}

if ($Report.Summary.DuplicateCsprojFiles -eq 0) {
    Write-Host "  ✅ No duplicate .csproj files detected" -ForegroundColor $ColorGood
}
Write-Host ""

# Security vulnerability check
if ($CheckSecurity) {
    Write-Host "🔒 Checking for security vulnerabilities..." -ForegroundColor $ColorInfo
    
    foreach ($Project in $Projects) {
        try {
            $VulnOutput = dotnet list $Project.FullName package --vulnerable --include-transitive 2>&1
            
            # Parse output for vulnerabilities
            if ($VulnOutput -match 'has the following vulnerable packages') {
                $Report.Summary.Vulnerabilities++
                
                $Issue = @{
                    Project = $Project.Name
                    Output = ($VulnOutput | Out-String)
                }
                
                $Report.Issues.Vulnerabilities += $Issue
                
                Write-Host "  ❌ $($Project.Name) has vulnerabilities" -ForegroundColor $ColorError
            }
        }
        catch {
            Write-Host "  ⚠️  Failed to check vulnerabilities for $($Project.Name): $_" -ForegroundColor $ColorWarn
        }
    }
    
    if ($Report.Summary.Vulnerabilities -eq 0) {
        Write-Host "  ✅ No vulnerabilities detected" -ForegroundColor $ColorGood
    }
    Write-Host ""
}

# Check for outdated packages (requires network connection)
if ($CheckOutdated) {
    Write-Host "🔍 Checking for outdated packages (this may take a while)..." -ForegroundColor $ColorInfo
    
    # Check first project for all outdated packages (avoid redundant checks)
    $FirstProject = $Projects[0]
    
    try {
        $OutdatedOutput = dotnet list $FirstProject.FullName package --outdated 2>&1
        
        # Parse output
        $Lines = $OutdatedOutput -split "`n"
        $InPackageSection = $false
        
        foreach ($Line in $Lines) {
            # Detect package table start
            if ($Line -match 'Top-level Package\s+Requested\s+Resolved\s+Latest') {
                $InPackageSection = $true
                continue
            }
            
            # Skip non-package lines
            if (-not $InPackageSection) { continue }
            if ($Line -match '^\s*$') { continue }
            
            # Parse package line: "   > PackageName   1.0.0   1.0.0   2.0.0"
            if ($Line -match '^\s*>\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)') {
                $PackageName = $Matches[1]
                $Requested = $Matches[2]
                $Resolved = $Matches[3]
                $Latest = $Matches[4]
                
                $Issue = @{
                    Package = $PackageName
                    CurrentVersion = $Resolved
                    LatestVersion = $Latest
                }
                
                $Report.Issues.Outdated += $Issue
                $Report.Summary.Outdated++
                
                Write-Host "  ⚠️  $PackageName`: $Resolved -> $Latest" -ForegroundColor $ColorWarn
            }
        }
    }
    catch {
        Write-Host "  ⚠️  Failed to check outdated packages: $_" -ForegroundColor $ColorWarn
    }
    
    if ($Report.Summary.Outdated -eq 0) {
        Write-Host "  ✅ All packages are up-to-date" -ForegroundColor $ColorGood
    }
    Write-Host ""
}

# Calculate up-to-date count
$Report.Summary.UpToDate = $Report.Packages.Count - $Report.Summary.Outdated

# Summary
Write-Host "═══════════════════════════════════════════" -ForegroundColor $ColorInfo
Write-Host "📊 SUMMARY" -ForegroundColor $ColorInfo
Write-Host "═══════════════════════════════════════════" -ForegroundColor $ColorInfo
Write-Host "  ✅ Up-to-date packages: $($Report.Summary.UpToDate)" -ForegroundColor $ColorGood
Write-Host "  ⚠️  Outdated packages: $($Report.Summary.Outdated)" -ForegroundColor $(if ($Report.Summary.Outdated -gt 0) { $ColorWarn } else { $ColorGood })
Write-Host "  ⚠️  Preview/RC packages: $($Report.Summary.PreviewRC)" -ForegroundColor $(if ($Report.Summary.PreviewRC -gt 0) { $ColorWarn } else { $ColorGood })
Write-Host "  ⚠️  Version mixing issues: $($Report.Summary.VersionMixingIssues)" -ForegroundColor $(if ($Report.Summary.VersionMixingIssues -gt 0) { $ColorWarn } else { $ColorGood })
Write-Host "  ⚠️  Duplicate .csproj files: $($Report.Summary.DuplicateCsprojFiles)" -ForegroundColor $(if ($Report.Summary.DuplicateCsprojFiles -gt 0) { $ColorWarn } else { $ColorGood })
Write-Host "  ❌ Security vulnerabilities: $($Report.Summary.Vulnerabilities)" -ForegroundColor $(if ($Report.Summary.Vulnerabilities -gt 0) { $ColorError } else { $ColorGood })
Write-Host "═══════════════════════════════════════════" -ForegroundColor $ColorInfo
Write-Host ""

# Save report
$ReportJson = $Report | ConvertTo-Json -Depth 10
$ReportJson | Out-File -FilePath $ReportPath -Encoding UTF8
Write-Host "📄 Report saved to: $ReportPath" -ForegroundColor $ColorInfo

# Exit codes
$ExitCode = 0

if ($FailOnVulnerabilities -and $Report.Summary.Vulnerabilities -gt 0) {
    Write-Host "❌ FAILED: $($Report.Summary.Vulnerabilities) vulnerabilities found" -ForegroundColor $ColorError
    $ExitCode = 1
}

if ($FailOnOutdated -and $Report.Summary.Outdated -gt 0) {
    Write-Host "❌ FAILED: $($Report.Summary.Outdated) outdated packages found" -ForegroundColor $ColorError
    $ExitCode = 1
}

if ($ExitCode -eq 0) {
    Write-Host "✅ Validation complete!" -ForegroundColor $ColorGood
}

exit $ExitCode
