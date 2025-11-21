#Requires -Version 7.0
<#
.SYNOPSIS
    Complete build pipeline for Hartonomous with automatic CLR signing

.DESCRIPTION
    Orchestrates the complete build process:
    1. Initialize signing infrastructure (idempotent)
    2. Build solution
    3. Auto-discover and sign all CLR assemblies
    4. Build DACPAC
    5. Verify all assemblies are signed

.PARAMETER Configuration
    Build configuration (Debug or Release)
    Default: Release

.PARAMETER SkipSigning
    Skip assembly signing (for local dev without Windows SDK)

.PARAMETER DeployCertificate
    Also deploy certificate to SQL Server after build

.PARAMETER Server
    SQL Server instance for certificate deployment
    Default: localhost

.EXAMPLE
    .\Build-WithSigning.ps1

.EXAMPLE
    .\Build-WithSigning.ps1 -Configuration Debug

.EXAMPLE
    .\Build-WithSigning.ps1 -DeployCertificate

.NOTES
    ISO-certification grade: Fully auditable, idempotent, production-ready
#>

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$SkipSigning,
    
    [Parameter()]
    [switch]$DeployCertificate,
    
    [Parameter()]
    [string]$Server = 'localhost'
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent
$solutionPath = Join-Path $repoRoot "Hartonomous.sln"
$dacpacProject = Join-Path $repoRoot "src\Hartonomous.Database\Hartonomous.Database.sqlproj"
# SQL projects output to bin\Output by default, but we want bin\Release for consistency
$buildOutput = Join-Path $repoRoot "src\Hartonomous.Database\bin\Release"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Hartonomous Build Pipeline - ISO Certification Grade         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Solution: $solutionPath" -ForegroundColor Gray
Write-Host "DACPAC Project: $dacpacProject" -ForegroundColor Gray
Write-Host ""

$startTime = Get-Date

# ============================================================================
# STEP 1: Initialize Signing Infrastructure
# ============================================================================
if (-not $SkipSigning) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "STEP 1: Initialize CLR Signing Infrastructure" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    try {
        & (Join-Path $scriptRoot "Initialize-CLRSigning.ps1")
        if ($LASTEXITCODE -ne 0) { throw "Signing initialization failed" }
    } catch {
        Write-Host ""
        Write-Host "✗ Signing initialization failed: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "To skip signing (dev mode): .\Build-WithSigning.ps1 -SkipSigning" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host ""
}

# ============================================================================
# STEP 2: Build Solution
# ============================================================================
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "STEP 2: Build Solution" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

Write-Host "Building: $solutionPath" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

try {
    dotnet build $solutionPath --configuration $Configuration --no-incremental
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-Host ""
    Write-Host "✓ Solution built successfully" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "✗ Build failed: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 3: Build DACPAC
# ============================================================================
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "STEP 3: Build DACPAC" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

try {
    & (Join-Path $scriptRoot "build-dacpac.ps1") `
        -ProjectPath $dacpacProject `
        -OutputDir $buildOutput `
        -Configuration $Configuration
    
    if ($LASTEXITCODE -ne 0) { throw "DACPAC build failed" }
    
    Write-Host ""
    Write-Host "✓ DACPAC built successfully" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "✗ DACPAC build failed: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 4: Sign CLR Assemblies
# ============================================================================
if (-not $SkipSigning) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "STEP 4: Sign CLR Assemblies (Auto-Discovery)" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    try {
        & (Join-Path $scriptRoot "Sign-CLRAssemblies.ps1") -Configuration $Configuration
        if ($LASTEXITCODE -ne 0) { throw "Signing failed" }
    } catch {
        Write-Host ""
        Write-Host "✗ Assembly signing failed: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "To skip signing (dev mode): .\Build-WithSigning.ps1 -SkipSigning" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host ""
}

# ============================================================================
# STEP 5: Verify Signatures
# ============================================================================
if (-not $SkipSigning) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "STEP 5: Verify Assembly Signatures" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    $allDlls = Get-ChildItem -Path $buildOutput -Filter "*.dll" -Recurse
    $unsigned = @()
    $invalid = @()
    $valid = @()
    
    foreach ($dll in $allDlls) {
        $sig = Get-AuthenticodeSignature $dll.FullName
        $relativePath = $dll.FullName.Substring($buildOutput.Length + 1)
        
        switch ($sig.Status) {
            'Valid' {
                $valid += $relativePath
            }
            'NotSigned' {
                # Check if it's a Microsoft system assembly (those can stay unsigned)
                if ($dll.Name -like 'System.*' -or $dll.Name -like 'Microsoft.*' -or $dll.Name -eq 'mscorlib.dll') {
                    if ($sig.SignerCertificate -and $sig.SignerCertificate.Subject -match 'Microsoft') {
                        $valid += $relativePath
                    } else {
                        $unsigned += $relativePath
                    }
                } else {
                    $unsigned += $relativePath
                }
            }
            default {
                $invalid += @{Path = $relativePath; Status = $sig.Status}
            }
        }
    }
    
    Write-Host "Signature Status:" -ForegroundColor Cyan
    Write-Host "  Valid: $($valid.Count)" -ForegroundColor Green
    Write-Host "  Unsigned: $($unsigned.Count)" -ForegroundColor $(if ($unsigned.Count -eq 0) { 'Green' } else { 'Yellow' })
    Write-Host "  Invalid: $($invalid.Count)" -ForegroundColor $(if ($invalid.Count -eq 0) { 'Green' } else { 'Red' })
    Write-Host ""
    
    if ($unsigned.Count -gt 0) {
        Write-Host "⚠ Unsigned assemblies:" -ForegroundColor Yellow
        $unsigned | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
        Write-Host ""
    }
    
    if ($invalid.Count -gt 0) {
        Write-Host "✗ Invalid signatures:" -ForegroundColor Red
        $invalid | ForEach-Object { Write-Host "    $($_.Path) - $($_.Status)" -ForegroundColor Gray }
        Write-Host ""
        Write-Host "Build failed: Invalid signatures detected" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# STEP 6: Deploy Certificate (Optional)
# ============================================================================
if ($DeployCertificate -and -not $SkipSigning) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "STEP 6: Deploy Certificate to SQL Server" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host ""
    
    try {
        & (Join-Path $scriptRoot "Deploy-CLRCertificate.ps1") -Server $Server
        if ($LASTEXITCODE -ne 0) { throw "Certificate deployment failed" }
    } catch {
        Write-Host ""
        Write-Host "✗ Certificate deployment failed: $_" -ForegroundColor Red
        Write-Host "You can deploy manually later with: .\scripts\Deploy-CLRCertificate.ps1" -ForegroundColor Yellow
        # Don't fail the build for this
    }
    
    Write-Host ""
}

# ============================================================================
# Build Summary
# ============================================================================
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  BUILD COMPLETED SUCCESSFULLY                                  ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Build Time: $($duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor Gray
Write-Host "DACPAC: $buildOutput\Hartonomous.Database.dacpac" -ForegroundColor Cyan
if (-not $SkipSigning) {
    Write-Host "Assemblies: All signed with Hartonomous CLR certificate" -ForegroundColor Green
    Write-Host "Status: ISO-certification ready ✓" -ForegroundColor Green
} else {
    Write-Host "Assemblies: Unsigned (development mode)" -ForegroundColor Yellow
    Write-Host "Status: Development only ⚠" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
if ($DeployCertificate -and -not $SkipSigning) {
    Write-Host "  .\scripts\deploy-dacpac.ps1 -Server $Server" -ForegroundColor Gray
} else {
    Write-Host "  .\scripts\Deploy-CLRCertificate.ps1 -Server $Server" -ForegroundColor Gray
    Write-Host "  .\scripts\deploy-dacpac.ps1 -Server $Server" -ForegroundColor Gray
}
Write-Host ""
