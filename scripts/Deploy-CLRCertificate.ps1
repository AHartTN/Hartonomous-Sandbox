#Requires -Version 7.0
<#
.SYNOPSIS
    Deploy CLR signing certificate to SQL Server

.DESCRIPTION
    Configures SQL Server to trust the Hartonomous CLR signing certificate.
    Idempotent: safe to run multiple times.
    Enables CLR Strict Security (production-ready configuration).

.PARAMETER Server
    SQL Server instance name
    Default: localhost

.PARAMETER EnableStrictSecurity
    Enable CLR Strict Security (recommended for production)
    Default: $true

.EXAMPLE
    .\Deploy-CLRCertificate.ps1

.EXAMPLE
    .\Deploy-CLRCertificate.ps1 -Server "production-sql.database.windows.net"

.EXAMPLE
    .\Deploy-CLRCertificate.ps1 -EnableStrictSecurity $false

.NOTES
    Part of the Hartonomous deployment pipeline.
    Requires sysadmin or CONTROL SERVER permission.
#>

param(
    [Parameter()]
    [string]$Server = 'localhost',
    
    [Parameter()]
    [bool]$EnableStrictSecurity = $true
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path $scriptRoot -Parent
$signingConfigPath = Join-Path $repoRoot ".signing-config"

Write-Host "=== Deploy CLR Certificate to SQL Server ===" -ForegroundColor Cyan
Write-Host "Server: $Server" -ForegroundColor Gray
Write-Host ""

# Load signing configuration
if (-not (Test-Path $signingConfigPath)) {
    Write-Host "⚠ Signing infrastructure not initialized!" -ForegroundColor Red
    Write-Host "Run: .\scripts\Initialize-CLRSigning.ps1" -ForegroundColor Yellow
    exit 1
}

$config = Get-Content $signingConfigPath -Raw | ConvertFrom-Json
$cerPath = $config.CerPath
$certThumbprint = $config.CertificateThumbprint

# Verify CER file exists
if (-not (Test-Path $cerPath)) {
    Write-Host "⚠ CER file not found: $cerPath" -ForegroundColor Red
    Write-Host "Run: .\scripts\Initialize-CLRSigning.ps1 -Force" -ForegroundColor Yellow
    exit 1
}

# Read CER file as hex string for SQL Server
$cerBytes = [System.IO.File]::ReadAllBytes($cerPath)
$cerHex = ($cerBytes | ForEach-Object { $_.ToString('X2') }) -join ''

Write-Host "Certificate thumbprint: $certThumbprint" -ForegroundColor Cyan
Write-Host "Certificate file: $cerPath" -ForegroundColor Gray
Write-Host "Certificate size: $($cerBytes.Length) bytes" -ForegroundColor Gray
Write-Host ""

# Generate SQL script
$sqlScript = @"
-- =============================================
-- Deploy Hartonomous CLR Signing Certificate
-- Auto-generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
-- Certificate: $certThumbprint
-- =============================================

USE master;
GO

PRINT 'Configuring CLR security...';

-- 1. Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
PRINT '  ✓ CLR enabled';

-- 2. Check if certificate already exists
IF EXISTS (SELECT 1 FROM sys.certificates WHERE name = 'HartonomousCLRCert')
BEGIN
    PRINT '  ⚠ Certificate already exists, dropping...';
    
    -- Drop login first (if exists)
    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'HartonomousCLRLogin')
    BEGIN
        DROP LOGIN [HartonomousCLRLogin];
        PRINT '    ✓ Dropped existing login';
    END
    
    -- Drop certificate
    DROP CERTIFICATE [HartonomousCLRCert];
    PRINT '    ✓ Dropped existing certificate';
END

-- 3. Create certificate from binary
CREATE CERTIFICATE [HartonomousCLRCert]
FROM BINARY = 0x$cerHex;
PRINT '  ✓ Certificate created';

-- 4. Create login from certificate
CREATE LOGIN [HartonomousCLRLogin]
FROM CERTIFICATE [HartonomousCLRCert];
PRINT '  ✓ Login created';

-- 5. Grant UNSAFE ASSEMBLY permission
GRANT UNSAFE ASSEMBLY TO [HartonomousCLRLogin];
PRINT '  ✓ UNSAFE ASSEMBLY permission granted';

-- 6. Enable CLR Strict Security (if requested)
$(if ($EnableStrictSecurity) {
@"
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;
PRINT '  ✓ CLR Strict Security enabled (production-ready)';
"@
} else {
@"
-- CLR Strict Security NOT enabled (development mode)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
PRINT '  ⚠ CLR Strict Security disabled (development mode only)';
"@
})

-- 7. Verify configuration
DECLARE @clr_enabled INT, @clr_strict INT;
SELECT @clr_enabled = CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr enabled';
SELECT @clr_strict = CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr strict security';

PRINT '';
PRINT '=== Configuration Summary ===';
PRINT '  CLR Enabled: ' + CASE WHEN @clr_enabled = 1 THEN 'YES' ELSE 'NO' END;
PRINT '  CLR Strict Security: ' + CASE WHEN @clr_strict = 1 THEN 'ENABLED (Production)' ELSE 'DISABLED (Development)' END;
PRINT '  Certificate: HartonomousCLRCert';
PRINT '  Login: HartonomousCLRLogin';
PRINT '  Permission: UNSAFE ASSEMBLY';
PRINT '';
PRINT '✓ CLR signing infrastructure deployed successfully';
GO
"@

# Save SQL script
$sqlScriptPath = Join-Path $scriptRoot "Deploy-CLRCertificate-Generated.sql"
$sqlScript | Set-Content $sqlScriptPath -Force
Write-Host "Generated SQL script: $sqlScriptPath" -ForegroundColor Cyan
Write-Host ""

# Execute SQL script
Write-Host "Deploying to SQL Server: $Server" -ForegroundColor Yellow

try {
    # Use sqlcmd with trust server certificate
    $sqlcmdArgs = @(
        '-S', $Server,
        '-E',  # Windows Authentication
        '-C',  # Trust server certificate
        '-i', $sqlScriptPath,
        '-b'   # Exit with error code on SQL errors
    )
    
    Write-Host "Executing: sqlcmd $($sqlcmdArgs -join ' ')" -ForegroundColor Gray
    Write-Host ""
    
    & sqlcmd @sqlcmdArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed with exit code $LASTEXITCODE"
    }
    
    Write-Host ""
    Write-Host "✓ Certificate deployed successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify: SELECT * FROM sys.certificates WHERE name = 'HartonomousCLRCert'" -ForegroundColor Gray
    Write-Host "  2. Verify: SELECT * FROM sys.server_principals WHERE name = 'HartonomousCLRLogin'" -ForegroundColor Gray
    Write-Host "  3. Deploy DACPAC with signed assemblies" -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "✗ Deployment failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual deployment:" -ForegroundColor Yellow
    Write-Host "  Run the SQL script manually: $sqlScriptPath" -ForegroundColor Gray
    exit 1
}
