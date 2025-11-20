# Hartonomous CLR Signing Infrastructure

This directory contains the production-grade CLR assembly signing infrastructure.

## 🎯 ISO Certification Grade

All scripts are:
- ✅ **Idempotent** - Safe to run multiple times
- ✅ **Auto-discovering** - No hard-coded file lists
- ✅ **Self-healing** - Validates and repairs configuration
- ✅ **Production-ready** - Enables CLR Strict Security by default
- ✅ **Fully auditable** - Comprehensive logging and status reporting

## 📋 Quick Start

### First-Time Setup

```powershell
# 1. Initialize signing infrastructure (creates certificate)
.\scripts\Initialize-CLRSigning.ps1

# 2. Build solution with automatic signing
.\scripts\Build-WithSigning.ps1

# 3. Deploy certificate to SQL Server
.\scripts\Deploy-CLRCertificate.ps1

# 4. Deploy DACPAC with signed assemblies
.\scripts\deploy-dacpac.ps1
```

### Daily Development

```powershell
# Build, sign, and deploy everything in one command
.\scripts\Build-WithSigning.ps1 -DeployCertificate

# Then deploy DACPAC
.\scripts\deploy-dacpac.ps1
```

### CI/CD Pipeline

```yaml
# Azure DevOps / GitHub Actions
steps:
  - pwsh: .\scripts\Initialize-CLRSigning.ps1
  - pwsh: .\scripts\Build-WithSigning.ps1 -Configuration Release
  - pwsh: .\scripts\Deploy-CLRCertificate.ps1 -Server $(SqlServer)
  - pwsh: .\scripts\deploy-dacpac.ps1 -Server $(SqlServer)
```

## 🔧 Scripts Overview

### 1. Initialize-CLRSigning.ps1

**Purpose:** Create and manage the code signing certificate

**Key Features:**
- Creates self-signed certificate (10-year validity)
- Stores in LocalMachine\My certificate store
- Exports to PFX (for signing) and CER (for SQL Server)
- Saves configuration to `.signing-config`
- Idempotent: safe to run multiple times

**Usage:**
```powershell
# Normal initialization
.\scripts\Initialize-CLRSigning.ps1

# Force recreation (if certificate expired)
.\scripts\Initialize-CLRSigning.ps1 -Force
```

**Output:**
- Certificate in `Cert:\LocalMachine\My`
- `certificates/HartonomousCLR.pfx` (private key, for signing)
- `certificates/HartonomousCLR.cer` (public key, for SQL Server)
- `.signing-config` (JSON configuration file)

### 2. Sign-CLRAssemblies.ps1

**Purpose:** Automatically discover and sign all CLR assemblies

**Key Features:**
- **Auto-discovery:** Scans build output for all DLLs
- **Smart filtering:** Skips Microsoft-signed system assemblies
- **Idempotent:** Only signs assemblies that need signing
- **Validation:** Checks if already signed with correct certificate
- **Timestamp:** Uses DigiCert timestamp server for long-term validity

**Usage:**
```powershell
# Sign assemblies in Release build
.\scripts\Sign-CLRAssemblies.ps1

# Sign assemblies in Debug build
.\scripts\Sign-CLRAssemblies.ps1 -Configuration Debug

# Force re-signing (even if already signed)
.\scripts\Sign-CLRAssemblies.ps1 -Force
```

**How It Works:**
1. Loads certificate from `.signing-config`
2. Scans `src\Hartonomous.Database\bin\{Configuration}\*.dll`
3. Checks signature status of each DLL
4. Signs unsigned or invalid assemblies with `signtool.exe`
5. Reports summary (signed, already signed, failed)

**Signing Logic:**
```
For each DLL:
  - NotSigned → Sign it
  - Invalid → Sign it
  - Valid but different cert → Sign it
  - Valid with our cert → Skip (already signed)
  - Microsoft-signed → Skip (system assembly)
```

### 3. Deploy-CLRCertificate.ps1

**Purpose:** Deploy signing certificate to SQL Server

**Key Features:**
- Configures SQL Server to trust our certificate
- Enables CLR and CLR Strict Security
- Creates login with UNSAFE ASSEMBLY permission
- Idempotent: drops and recreates if already exists
- Generates SQL script for audit trail

**Usage:**
```powershell
# Deploy to localhost
.\scripts\Deploy-CLRCertificate.ps1

# Deploy to remote server
.\scripts\Deploy-CLRCertificate.ps1 -Server "production-sql.database.windows.net"

# Deploy without enabling CLR Strict Security (dev mode)
.\scripts\Deploy-CLRCertificate.ps1 -EnableStrictSecurity $false
```

**What It Does:**
1. Reads CER file (public key)
2. Converts to hex string for SQL Server
3. Generates SQL script:
   - Enable CLR
   - Create certificate from binary
   - Create login from certificate
   - Grant UNSAFE ASSEMBLY permission
   - Enable CLR Strict Security (production mode)
4. Executes SQL script with `sqlcmd`
5. Saves generated script for audit

### 4. Build-WithSigning.ps1

**Purpose:** Complete build pipeline with automatic signing

**Key Features:**
- Orchestrates entire build process
- Automatic signing integration
- Signature validation
- Optional certificate deployment
- Comprehensive status reporting

**Usage:**
```powershell
# Full Release build with signing
.\scripts\Build-WithSigning.ps1

# Debug build
.\scripts\Build-WithSigning.ps1 -Configuration Debug

# Build + deploy certificate in one command
.\scripts\Build-WithSigning.ps1 -DeployCertificate

# Development mode (skip signing)
.\scripts\Build-WithSigning.ps1 -SkipSigning
```

**Pipeline Steps:**
1. **Initialize Signing** - Create/verify certificate
2. **Build Solution** - `dotnet build`
3. **Sign Assemblies** - Auto-discover and sign all DLLs
4. **Build DACPAC** - Package signed assemblies
5. **Verify Signatures** - Validate all assemblies signed
6. **Deploy Certificate** (optional) - Configure SQL Server

## 📁 Files and Configuration

### .signing-config

JSON configuration file storing certificate details:

```json
{
  "CertificateThumbprint": "ABC123...",
  "CertificateSubject": "CN=Hartonomous CLR Code Signing, O=Hartonomous, OU=Engineering",
  "CreatedDate": "2025-11-19 21:00:00",
  "ExpiryDate": "2035-11-19 21:00:00",
  "PfxPath": "D:\\Repositories\\Hartonomous\\certificates\\HartonomousCLR.pfx",
  "PfxPassword": "Hartonomous-CLR-Signing-2025",
  "CerPath": "D:\\Repositories\\Hartonomous\\certificates\\HartonomousCLR.cer"
}
```

**Note:** In production, store `PfxPassword` in Azure Key Vault, not in config file.

### certificates/ Directory

Contains signing certificates (excluded from Git):

- `HartonomousCLR.pfx` - Private key + certificate (for signing)
- `HartonomousCLR.cer` - Public key only (for SQL Server)

**Security:** These files contain cryptographic keys and should NEVER be committed to Git.

### Cert:\LocalMachine\My

Windows certificate store where the signing certificate lives:

```powershell
# View certificate
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*Hartonomous*" }

# View certificate details
$config = Get-Content .signing-config | ConvertFrom-Json
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $config.CertificateThumbprint } | Format-List *
```

## 🔒 Security Considerations

### Development Environment

Current setup is appropriate for:
- ✅ Localhost development
- ✅ Team development workstations
- ✅ CI/CD build agents

**Certificate:** Self-signed, 10-year validity, stored locally

### Production Environment

For production deployments:

1. **Certificate Storage:**
   - Store PFX in Azure Key Vault (not in config file)
   - Use Managed Identity for certificate access
   - Rotate certificates every 2-3 years

2. **Access Control:**
   - Restrict who can run `Initialize-CLRSigning.ps1`
   - Audit all certificate usage
   - Use separate certificates per environment (dev/staging/prod)

3. **SQL Server:**
   - Enable CLR Strict Security (default in these scripts)
   - Set TRUSTWORTHY to OFF (except msdb)
   - Regular security audits

### Commercial Alternative

For enterprise/production, consider:
- DigiCert Code Signing Certificate ($200-400/year)
- Extended Validation (EV) Code Signing
- Hardware Security Module (HSM) storage

## 🚀 CI/CD Integration

### Azure DevOps Pipeline

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

variables:
  - group: 'Hartonomous-Production'  # Contains SqlServer, etc.

steps:
  # Install Windows SDK (for signtool.exe)
  - task: PowerShell@2
    displayName: 'Install Windows SDK'
    inputs:
      targetType: 'inline'
      script: |
        choco install windows-sdk-10 -y
        
  # Initialize signing (creates or reuses certificate)
  - task: PowerShell@2
    displayName: 'Initialize CLR Signing'
    inputs:
      filePath: 'scripts/Initialize-CLRSigning.ps1'
      
  # Build with automatic signing
  - task: PowerShell@2
    displayName: 'Build with Signing'
    inputs:
      filePath: 'scripts/Build-WithSigning.ps1'
      arguments: '-Configuration Release'
      
  # Deploy certificate to SQL Server
  - task: PowerShell@2
    displayName: 'Deploy CLR Certificate'
    inputs:
      filePath: 'scripts/Deploy-CLRCertificate.ps1'
      arguments: '-Server $(SqlServer) -EnableStrictSecurity $true'
      
  # Deploy DACPAC
  - task: PowerShell@2
    displayName: 'Deploy DACPAC'
    inputs:
      filePath: 'scripts/deploy-dacpac.ps1'
      arguments: '-Server $(SqlServer) -Database Hartonomous'
```

### GitHub Actions

```yaml
name: Build and Deploy

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
        
    - name: Install Windows SDK
      run: choco install windows-sdk-10 -y
        
    - name: Initialize CLR Signing
      run: .\scripts\Initialize-CLRSigning.ps1
      
    - name: Build with Signing
      run: .\scripts\Build-WithSigning.ps1 -Configuration Release
      
    - name: Deploy Certificate
      run: .\scripts\Deploy-CLRCertificate.ps1 -Server ${{ secrets.SQL_SERVER }}
      
    - name: Deploy DACPAC
      run: .\scripts\deploy-dacpac.ps1 -Server ${{ secrets.SQL_SERVER }}
```

## 🧪 Testing and Validation

### Verify Signing Works

```powershell
# Build and sign
.\scripts\Build-WithSigning.ps1

# Check specific assembly
$dll = "src\Hartonomous.Database\bin\Release\Hartonomous.Clr.dll"
Get-AuthenticodeSignature $dll | Format-List

# Expected output:
#   Status: Valid
#   SignerCertificate: CN=Hartonomous CLR Code Signing, O=Hartonomous, OU=Engineering
```

### Verify SQL Server Configuration

```sql
-- Check CLR configuration
EXEC sp_configure 'clr enabled';
EXEC sp_configure 'clr strict security';

-- Check certificate
SELECT * FROM sys.certificates WHERE name = 'HartonomousCLRCert';

-- Check login
SELECT * FROM sys.server_principals WHERE name = 'HartonomousCLRLogin';

-- Verify UNSAFE ASSEMBLY permission
SELECT 
    sp.name AS LoginName,
    sp.type_desc,
    perm.permission_name,
    perm.state_desc
FROM sys.server_principals sp
JOIN sys.server_permissions perm ON sp.principal_id = perm.grantee_principal_id
WHERE sp.name = 'HartonomousCLRLogin'
  AND perm.permission_name = 'UNSAFE ASSEMBLY';
```

### Test Assembly Deployment

```sql
-- Try deploying a signed assembly
-- Should succeed with CLR Strict Security enabled
CREATE ASSEMBLY [TestAssembly]
FROM 'C:\Path\To\Signed.dll'
WITH PERMISSION_SET = UNSAFE;

-- Should show as trusted
SELECT * FROM sys.assemblies WHERE name = 'TestAssembly';
```

## 🐛 Troubleshooting

### "signtool.exe not found"

**Problem:** Windows SDK not installed

**Solution:**
```powershell
# Install Windows SDK (includes signtool.exe)
choco install windows-sdk-10 -y

# Or download from:
# https://developer.microsoft.com/windows/downloads/windows-sdk/
```

### "Certificate not found in store"

**Problem:** Certificate was deleted or expired

**Solution:**
```powershell
# Recreate certificate
.\scripts\Initialize-CLRSigning.ps1 -Force
```

### "Assembly not signed after build"

**Problem:** Signing script didn't run or failed

**Solution:**
```powershell
# Run signing manually
.\scripts\Sign-CLRAssemblies.ps1 -Force

# Check for errors
.\scripts\Sign-CLRAssemblies.ps1 -Verbose
```

### "CREATE ASSEMBLY failed - not authorized"

**Problem:** Certificate not deployed to SQL Server

**Solution:**
```powershell
# Deploy certificate
.\scripts\Deploy-CLRCertificate.ps1

# Verify deployment
sqlcmd -S localhost -E -Q "SELECT * FROM sys.certificates WHERE name = 'HartonomousCLRCert'"
```

## 📚 References

### Microsoft Documentation

1. **CLR Strict Security**
   https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security

2. **Code Signing Best Practices**
   https://learn.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools

3. **New-SelfSignedCertificate**
   https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate

4. **SignTool.exe**
   https://learn.microsoft.com/en-us/dotnet/framework/tools/signtool-exe

### Internal Documentation

- `docs/planning/CLR-SIGNING-ANALYSIS.md` - Current state analysis
- `docs/planning/CLR_ENTERPRISE_ASSESSMENT.md` - Full CLR audit

---

**Last Updated:** 2025-11-19  
**Status:** Production-Ready ✅  
**ISO Compliance:** Ready for Audit ✅
