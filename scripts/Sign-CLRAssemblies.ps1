#Requires -Version 7.0
<#
.SYNOPSIS
    Automatically discover and sign all CLR assemblies for Hartonomous

.DESCRIPTION
    Dynamically discovers all DLL files in the build output and signs them.
    Idempotent: only signs assemblies that are unsigned or have invalid signatures.
    Skips Microsoft-signed assemblies (mscorlib, System.*, etc.)

.PARAMETER BuildOutputPath
    Path to the build output directory containing DLLs
    Default: src\Hartonomous.Database\bin\Release

.PARAMETER Configuration
    Build configuration (Debug or Release)
    Default: Release

.PARAMETER Force
    Force re-signing even if assemblies are already signed

.EXAMPLE
    .\Sign-CLRAssemblies.ps1

.EXAMPLE
    .\Sign-CLRAssemblies.ps1 -Configuration Debug

.EXAMPLE
    .\Sign-CLRAssemblies.ps1 -Force

.NOTES
    Part of the Hartonomous deployment pipeline.
    Automatically called by build-dacpac.ps1.
#>

param(
    [Parameter()]
    [string]$BuildOutputPath,
    
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent
$signingConfigPath = Join-Path $repoRoot ".signing-config"

# Default build output path
if (-not $BuildOutputPath) {
    $BuildOutputPath = Join-Path $repoRoot "src\Hartonomous.Database\bin\$Configuration"
}

Write-Host "=== CLR Assembly Signing ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Build Output: $BuildOutputPath" -ForegroundColor Gray
Write-Host ""

# Verify signing infrastructure exists
if (-not (Test-Path $signingConfigPath)) {
    Write-Host "⚠ Signing infrastructure not initialized!" -ForegroundColor Red
    Write-Host "Run: .\scripts\Initialize-CLRSigning.ps1" -ForegroundColor Yellow
    exit 1
}

# Load signing configuration
$config = Get-Content $signingConfigPath -Raw | ConvertFrom-Json
$certThumbprint = $config.CertificateThumbprint
$pfxPath = $config.PfxPath
$pfxPassword = $config.PfxPassword

Write-Host "Using certificate: $certThumbprint" -ForegroundColor Cyan

# Verify certificate exists
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $certThumbprint }
if (-not $cert) {
    Write-Host "⚠ Certificate not found in store!" -ForegroundColor Red
    Write-Host "Run: .\scripts\Initialize-CLRSigning.ps1 -Force" -ForegroundColor Yellow
    exit 1
}

# Verify PFX exists
if (-not (Test-Path $pfxPath)) {
    Write-Host "⚠ PFX file not found: $pfxPath" -ForegroundColor Red
    Write-Host "Run: .\scripts\Initialize-CLRSigning.ps1 -Force" -ForegroundColor Yellow
    exit 1
}

# Find signtool.exe (Windows SDK)
$signtool = $null
$sdkPaths = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe",
    "${env:ProgramFiles}\Windows Kits\10\bin\*\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\App Certification Kit\signtool.exe",
    "${env:ProgramFiles}\Windows Kits\10\App Certification Kit\signtool.exe"
)

foreach ($pattern in $sdkPaths) {
    $found = Get-ChildItem $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $signtool = $found.FullName
        break
    }
}

if (-not $signtool) {
    Write-Host "⚠ signtool.exe not found!" -ForegroundColor Red
    Write-Host "Install Windows SDK: https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using signtool: $signtool" -ForegroundColor Gray
Write-Host ""

# Discover all DLLs in build output
if (-not (Test-Path $BuildOutputPath)) {
    Write-Host "⚠ Build output path not found: $BuildOutputPath" -ForegroundColor Red
    Write-Host "Run: dotnet build first" -ForegroundColor Yellow
    exit 1
}

$allDlls = Get-ChildItem -Path $BuildOutputPath -Filter "*.dll" -Recurse

# Filter: Skip Microsoft-signed system assemblies
$systemAssemblyPrefixes = @(
    'mscorlib',
    'System.',
    'Microsoft.',
    'netstandard',
    'WindowsBase'
)

$dllsToSign = $allDlls | Where-Object {
    $name = $_.Name
    $skip = $false
    
    foreach ($prefix in $systemAssemblyPrefixes) {
        if ($name -like "$prefix*") {
            # Check if it's actually Microsoft-signed
            $sig = Get-AuthenticodeSignature $_.FullName
            if ($sig.SignerCertificate -and $sig.SignerCertificate.Subject -match 'Microsoft') {
                $skip = $true
                break
            }
        }
    }
    
    -not $skip
}

Write-Host "Discovered DLLs:" -ForegroundColor Cyan
Write-Host "  Total: $($allDlls.Count)" -ForegroundColor Gray
Write-Host "  Microsoft-signed (skipped): $($allDlls.Count - $dllsToSign.Count)" -ForegroundColor Gray
Write-Host "  To process: $($dllsToSign.Count)" -ForegroundColor Yellow
Write-Host ""

if ($dllsToSign.Count -eq 0) {
    Write-Host "✓ No assemblies to sign" -ForegroundColor Green
    exit 0
}

# Process each DLL
$signed = 0
$alreadySigned = 0
$failed = 0

foreach ($dll in $dllsToSign) {
    $relativePath = $dll.FullName.Substring($BuildOutputPath.Length + 1)
    
    # Check current signature
    $currentSig = Get-AuthenticodeSignature $dll.FullName
    $needsSigning = $false
    
    if ($Force) {
        $needsSigning = $true
        $reason = "Force"
    } elseif ($currentSig.Status -eq 'NotSigned') {
        $needsSigning = $true
        $reason = "NotSigned"
    } elseif ($currentSig.Status -ne 'Valid') {
        $needsSigning = $true
        $reason = $currentSig.Status
    } elseif ($currentSig.SignerCertificate.Thumbprint -ne $certThumbprint) {
        $needsSigning = $true
        $reason = "DifferentCert"
    }
    
    if ($needsSigning) {
        Write-Host "Signing: $relativePath" -ForegroundColor Yellow
        Write-Host "  Reason: $reason" -ForegroundColor Gray
        
        # Sign with signtool
        $signArgs = @(
            'sign',
            '/f', $pfxPath,
            '/p', $pfxPassword,
            '/fd', 'SHA256',
            '/tr', 'http://timestamp.digicert.com',
            '/td', 'SHA256',
            '/v',
            $dll.FullName
        )
        
        try {
            $output = & $signtool $signArgs 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ Signed successfully" -ForegroundColor Green
                $signed++
            } else {
                Write-Host "  ✗ Signing failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
                Write-Host "  $output" -ForegroundColor Red
                $failed++
            }
        } catch {
            Write-Host "  ✗ Signing failed: $_" -ForegroundColor Red
            $failed++
        }
    } else {
        Write-Host "✓ Already signed: $relativePath" -ForegroundColor Green
        $alreadySigned++
    }
}

Write-Host ""
Write-Host "=== Signing Summary ===" -ForegroundColor Cyan
Write-Host "  Signed: $signed" -ForegroundColor $(if ($signed -gt 0) { 'Green' } else { 'Gray' })
Write-Host "  Already Signed: $alreadySigned" -ForegroundColor $(if ($alreadySigned -gt 0) { 'Green' } else { 'Gray' })
Write-Host "  Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { 'Red' } else { 'Gray' })
Write-Host ""

if ($failed -gt 0) {
    Write-Host "⚠ Some assemblies failed to sign!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ All assemblies signed successfully" -ForegroundColor Green
exit 0
