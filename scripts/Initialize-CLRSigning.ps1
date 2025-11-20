#Requires -Version 7.0
<#
.SYNOPSIS
    Initialize CLR code signing infrastructure for Hartonomous

.DESCRIPTION
    Creates a self-signed code signing certificate if it doesn't exist.
    Idempotent: safe to run multiple times.
    Certificate is valid for 10 years and stored in LocalMachine\My.

.PARAMETER Force
    Force recreation of certificate even if one exists

.EXAMPLE
    .\Initialize-CLRSigning.ps1
    
.EXAMPLE
    .\Initialize-CLRSigning.ps1 -Force

.NOTES
    Part of the Hartonomous deployment pipeline.
    Certificate thumbprint is stored in .signing-config for consistency.
#>

param(
    [Parameter()]
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent
$signingConfigPath = Join-Path $repoRoot ".signing-config"
$certsDir = Join-Path $repoRoot "certificates"

# Ensure certificates directory exists
if (-not (Test-Path $certsDir)) {
    New-Item -ItemType Directory -Path $certsDir -Force | Out-Null
}

# Certificate configuration
$certSubject = "CN=Hartonomous CLR Code Signing, O=Hartonomous, OU=Engineering"
$certFriendlyName = "Hartonomous CLR Code Signing Certificate"
$pfxPassword = ConvertTo-SecureString -String "Hartonomous-CLR-Signing-2025" -Force -AsPlainText
$pfxPath = Join-Path $certsDir "HartonomousCLR.pfx"
$cerPath = Join-Path $certsDir "HartonomousCLR.cer"

Write-Host "=== Hartonomous CLR Signing Infrastructure ===" -ForegroundColor Cyan
Write-Host ""

# Check if we have an existing configuration
$existingThumbprint = $null
if (Test-Path $signingConfigPath) {
    $config = Get-Content $signingConfigPath -Raw | ConvertFrom-Json
    $existingThumbprint = $config.CertificateThumbprint
    Write-Host "Found existing signing config: $existingThumbprint" -ForegroundColor Yellow
}

# Check if certificate exists in store
$existingCert = $null
if ($existingThumbprint) {
    $existingCert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $existingThumbprint }
    
    if ($existingCert) {
        Write-Host "✓ Certificate found in LocalMachine\My store" -ForegroundColor Green
        Write-Host "  Subject: $($existingCert.Subject)" -ForegroundColor Gray
        Write-Host "  Expires: $($existingCert.NotAfter)" -ForegroundColor Gray
        
        # Check if cert is still valid
        if ($existingCert.NotAfter -lt (Get-Date)) {
            Write-Host "⚠ Certificate has expired! Will create new one." -ForegroundColor Yellow
            $Force = $true
        } elseif ($existingCert.NotAfter -lt (Get-Date).AddMonths(6)) {
            Write-Host "⚠ Certificate expires in less than 6 months" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠ Certificate not found in store (thumbprint: $existingThumbprint)" -ForegroundColor Yellow
        Write-Host "  Will create new certificate" -ForegroundColor Yellow
        $Force = $true
    }
}

if (-not $existingCert -or $Force) {
    Write-Host ""
    Write-Host "Creating new code signing certificate..." -ForegroundColor Cyan
    
    # Remove old cert if forcing
    if ($Force -and $existingCert) {
        Write-Host "Removing old certificate: $existingThumbprint" -ForegroundColor Yellow
        Remove-Item -Path "Cert:\LocalMachine\My\$existingThumbprint" -Force
    }
    
    # Create new self-signed certificate
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certSubject `
        -FriendlyName $certFriendlyName `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -KeyLength 2048 `
        -KeyAlgorithm RSA `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(10) `
        -CertStoreLocation "Cert:\LocalMachine\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")  # Code Signing EKU
    
    Write-Host "✓ Certificate created successfully" -ForegroundColor Green
    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host "  Subject: $($cert.Subject)" -ForegroundColor Gray
    Write-Host "  Expires: $($cert.NotAfter)" -ForegroundColor Gray
    
    # Export to PFX (for signing tools)
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pfxPassword -Force | Out-Null
    Write-Host "✓ Exported to PFX: $pfxPath" -ForegroundColor Green
    
    # Export to CER (public key for SQL Server)
    Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null
    Write-Host "✓ Exported to CER: $cerPath" -ForegroundColor Green
    
    # Save configuration
    $config = @{
        CertificateThumbprint = $cert.Thumbprint
        CertificateSubject = $cert.Subject
        CreatedDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        ExpiryDate = $cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss")
        PfxPath = $pfxPath
        PfxPassword = "Hartonomous-CLR-Signing-2025"  # In production, use Azure Key Vault
        CerPath = $cerPath
    }
    
    $config | ConvertTo-Json | Set-Content $signingConfigPath -Force
    Write-Host "✓ Configuration saved to: $signingConfigPath" -ForegroundColor Green
    
    $existingCert = $cert
} else {
    Write-Host ""
    Write-Host "✓ Using existing certificate (no changes needed)" -ForegroundColor Green
    
    # Ensure exports exist
    if (-not (Test-Path $pfxPath)) {
        Write-Host "Exporting to PFX..." -ForegroundColor Yellow
        Export-PfxCertificate -Cert $existingCert -FilePath $pfxPath -Password $pfxPassword -Force | Out-Null
        Write-Host "✓ Exported to PFX: $pfxPath" -ForegroundColor Green
    }
    
    if (-not (Test-Path $cerPath)) {
        Write-Host "Exporting to CER..." -ForegroundColor Yellow
        Export-Certificate -Cert $existingCert -FilePath $cerPath -Force | Out-Null
        Write-Host "✓ Exported to CER: $cerPath" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Signing Infrastructure Ready ===" -ForegroundColor Green
Write-Host "Certificate: $($existingCert.Thumbprint)" -ForegroundColor Cyan
Write-Host "PFX: $pfxPath" -ForegroundColor Cyan
Write-Host "CER: $cerPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run .\scripts\Sign-CLRAssemblies.ps1 to sign assemblies" -ForegroundColor Gray
Write-Host "  2. Run .\scripts\Deploy-CLRCertificate.ps1 to configure SQL Server" -ForegroundColor Gray
Write-Host ""

return @{
    CertificateThumbprint = $existingCert.Thumbprint
    Certificate = $existingCert
    PfxPath = $pfxPath
    CerPath = $cerPath
}
