# CLR Signing in CI/CD Environments

## Overview

This document covers how to handle CLR assembly signing in CI/CD pipelines (Azure DevOps and GitHub Actions) for ISO-certification compliance. The signing infrastructure is fully automated and idempotent, requiring only one-time certificate setup.

---

## Architecture

### Signing Pipeline Flow

```
┌─────────────────────────────────────────────────────────────┐
│ BUILD STAGE                                                 │
├─────────────────────────────────────────────────────────────┤
│ 1. Initialize-CLRSigning.ps1                                │
│    ├─ Check for existing certificate                        │
│    ├─ Create self-signed cert (if needed)                   │
│    ├─ Export to PFX (signing) and CER (SQL Server)          │
│    └─ Save config to .signing-config                        │
│                                                              │
│ 2. Build Solution (dotnet build / MSBuild)                  │
│    └─ Compile all CLR assemblies                            │
│                                                              │
│ 3. Sign-CLRAssemblies.ps1                                   │
│    ├─ Auto-discover all DLLs in build output                │
│    ├─ Filter: Skip Microsoft-signed system assemblies       │
│    ├─ Sign: Only NotSigned/Invalid/DifferentCert            │
│    └─ Report: Signed count, already signed, failed          │
│                                                              │
│ 4. Build DACPAC (MSBuild)                                   │
│    └─ Package signed assemblies into DACPAC                 │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ DEPLOY STAGE                                                │
├─────────────────────────────────────────────────────────────┤
│ 1. Deploy-CLRCertificate.ps1                                │
│    ├─ Read CER file, convert to hex                         │
│    ├─ CREATE CERTIFICATE in SQL Server                      │
│    ├─ CREATE LOGIN from certificate                         │
│    ├─ GRANT UNSAFE ASSEMBLY                                 │
│    └─ Enable CLR Strict Security (production)               │
│                                                              │
│ 2. Deploy DACPAC (SqlPackage)                               │
│    └─ Deploy signed assemblies to SQL Server                │
│                                                              │
│ 3. Verify Signatures                                        │
│    ├─ Query sys.assemblies for signature status             │
│    ├─ Query sys.configurations for CLR Strict Security      │
│    ├─ Fail if any unsigned assemblies found                 │
│    └─ Fail if CLR Strict Security disabled                  │
└─────────────────────────────────────────────────────────────┘
```

---

## Certificate Management

### One-Time Setup Options

There are **three approaches** for managing the signing certificate in CI/CD:

#### **Option 1: Self-Hosted Runner with Pre-Created Certificate** (Recommended)

**Best for:** Azure DevOps Local Agent Pool, GitHub self-hosted runners

**Setup Steps:**

1. **On the CI/CD agent machine**, run PowerShell as Administrator:

```powershell
cd D:\Repositories\Hartonomous
.\scripts\Initialize-CLRSigning.ps1
```

This creates:
- Certificate in `Cert:\LocalMachine\My` (requires admin)
- `certificates/HartonomousCLR.pfx` (for signing)
- `certificates/HartonomousCLR.cer` (for SQL Server)
- `.signing-config` (certificate metadata)

2. **Verify certificate creation:**

```powershell
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match 'Hartonomous CLR' }
```

3. **Commit `.signing-config` to repository** (contains thumbprint, no secrets):

```bash
git add .signing-config
git commit -m "Add CLR signing configuration"
git push
```

4. **DO NOT commit certificates** (already excluded in `.gitignore`):

```gitignore
certificates/
*.pfx
*.snk
```

**CI/CD Behavior:**
- `Initialize-CLRSigning.ps1` detects existing certificate (idempotent)
- Uses existing PFX/CER files from local filesystem
- No admin rights needed for subsequent runs (certificate already exists)

**Pros:**
- ✅ Simple setup (one-time admin PowerShell)
- ✅ No cloud secrets management needed
- ✅ Fast builds (no certificate creation overhead)
- ✅ Works with Local Agent Pool (HART-DESKTOP)

**Cons:**
- ⚠️ Certificate tied to specific agent machine
- ⚠️ Not suitable for cloud-hosted agents (ephemeral VMs)

---

#### **Option 2: Azure Key Vault (Cloud-Hosted Agents)**

**Best for:** Azure DevOps Microsoft-hosted agents, GitHub Actions ubuntu-latest/windows-latest

**Setup Steps:**

1. **Create Key Vault and upload certificate:**

```bash
# Create Azure Key Vault
az keyvault create \
  --name hartonomous-kv \
  --resource-group hartonomous-rg \
  --location eastus

# Generate certificate locally (one-time)
cd D:\Repositories\Hartonomous
.\scripts\Initialize-CLRSigning.ps1

# Upload PFX to Key Vault
az keyvault certificate import \
  --vault-name hartonomous-kv \
  --name HartonomousCLRCert \
  --file certificates/HartonomousCLR.pfx \
  --password "Hartonomous-CLR-Signing-2025"
```

2. **Grant pipeline service principal access:**

```bash
# Azure DevOps service connection principal
az keyvault set-policy \
  --name hartonomous-kv \
  --spn <service-principal-id> \
  --certificate-permissions get list \
  --secret-permissions get list

# GitHub Actions OIDC principal
az keyvault set-policy \
  --name hartonomous-kv \
  --object-id <github-oidc-principal-id> \
  --certificate-permissions get list \
  --secret-permissions get list
```

3. **Modify `Initialize-CLRSigning.ps1` to download from Key Vault:**

```powershell
# Add to Initialize-CLRSigning.ps1 (after parameter validation)

if ($env:AZURE_KEY_VAULT_NAME) {
    Write-Host "Downloading certificate from Azure Key Vault: $env:AZURE_KEY_VAULT_NAME"
    
    # Ensure Azure CLI is authenticated (done by pipeline)
    $kvName = $env:AZURE_KEY_VAULT_NAME
    $certName = "HartonomousCLRCert"
    
    # Download PFX as secret (Key Vault stores certs as secrets)
    $pfxBase64 = az keyvault secret show `
        --vault-name $kvName `
        --name $certName `
        --query value -o tsv
    
    # Decode and save to filesystem
    $pfxBytes = [Convert]::FromBase64String($pfxBase64)
    [IO.File]::WriteAllBytes($PfxPath, $pfxBytes)
    
    # Extract thumbprint from PFX
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($PfxPath, $PfxPassword)
    $Thumbprint = $cert.Thumbprint
    
    Write-Host "✓ Certificate downloaded from Key Vault (thumbprint: $Thumbprint)"
}
```

4. **Set environment variable in pipeline:**

**Azure DevOps** (`azure-pipelines.yml`):

```yaml
- task: PowerShell@2
  displayName: 'Initialize CLR Signing Certificate'
  inputs:
    targetType: 'filePath'
    filePath: 'scripts/Initialize-CLRSigning.ps1'
    pwsh: true
  env:
    AZURE_KEY_VAULT_NAME: 'hartonomous-kv'
```

**GitHub Actions** (`.github/workflows/ci-cd.yml`):

```yaml
- name: Initialize CLR Signing Certificate
  shell: pwsh
  env:
    AZURE_KEY_VAULT_NAME: 'hartonomous-kv'
  run: ./scripts/Initialize-CLRSigning.ps1
```

**Pros:**
- ✅ Works with cloud-hosted agents (ephemeral VMs)
- ✅ Centralized secret management
- ✅ Certificate rotation via Key Vault
- ✅ Auditable (Key Vault access logs)

**Cons:**
- ⚠️ Requires Azure subscription
- ⚠️ Adds complexity (Key Vault management)
- ⚠️ Slower builds (download overhead)

---

#### **Option 3: CurrentUser Certificate Store (No Admin Required)**

**Best for:** Development/testing, non-production environments

**Setup Steps:**

1. **Modify `Initialize-CLRSigning.ps1`** to use `CurrentUser` store:

```powershell
# Change line 89:
# OLD: -CertStoreLocation "Cert:\LocalMachine\My"
# NEW: -CertStoreLocation "Cert:\CurrentUser\My"

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=Hartonomous CLR Code Signing Certificate, O=Hartonomous, C=US" `
    -KeyUsage DigitalSignature `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(10) `
    -CertStoreLocation "Cert:\CurrentUser\My"  # ← Changed
```

2. **Run without administrator privileges:**

```powershell
cd D:\Repositories\Hartonomous
.\scripts\Initialize-CLRSigning.ps1
```

**CI/CD Behavior:**
- Certificate created in user profile (`C:\Users\<user>\AppData\Roaming\Microsoft\Crypto`)
- Each CI/CD agent user gets their own certificate
- Still idempotent (checks for existing certificate)

**Pros:**
- ✅ No admin rights required
- ✅ Simple setup
- ✅ Works on locked-down CI/CD agents

**Cons:**
- ⚠️ Certificate not machine-wide (user-specific)
- ⚠️ Different thumbprints per user (may cause issues)
- ⚠️ Not recommended for production (less secure than LocalMachine store)

---

## Azure DevOps Integration

### Current Pipeline (Updated)

**File:** `azure-pipelines.yml`

```yaml
- stage: BuildDatabase
  displayName: 'Build Database DACPAC'
  jobs:
  - job: BuildDACPAC
    displayName: 'Build Database DACPAC'
    steps:
    - task: UseDotNet@2
      displayName: 'Install .NET 10 SDK'
      inputs:
        packageType: 'sdk'
        version: '$(dotnetSdkVersion)'

    # NEW: Initialize certificate (idempotent)
    - task: PowerShell@2
      displayName: 'Initialize CLR Signing Certificate'
      inputs:
        targetType: 'filePath'
        filePath: 'scripts/Initialize-CLRSigning.ps1'
        pwsh: true
      continueOnError: false

    - task: PowerShell@2
      displayName: 'Build SQL Database Project with MSBuild'
      inputs:
        targetType: 'filePath'
        filePath: 'scripts/build-dacpac.ps1'
        arguments: '-ProjectPath "src/Hartonomous.Database/Hartonomous.Database.sqlproj" -OutputDir "$(Build.ArtifactStagingDirectory)/database/" -Configuration $(buildConfiguration)'
        pwsh: true

    # NEW: Sign assemblies (auto-discovery)
    - task: PowerShell@2
      displayName: 'Sign CLR Assemblies'
      inputs:
        targetType: 'filePath'
        filePath: 'scripts/Sign-CLRAssemblies.ps1'
        arguments: '-Configuration $(buildConfiguration) -Verbose'
        pwsh: true
      continueOnError: false

    - task: PowerShell@2
      displayName: 'Verify DACPAC Build'
      inputs:
        targetType: 'filePath'
        filePath: 'scripts/verify-dacpac.ps1'
        arguments: '-DacpacPath "$(Build.ArtifactStagingDirectory)/database/Hartonomous.Database.dacpac"'
        pwsh: true

- stage: DeployDatabase
  displayName: 'Deploy Database Schema'
  dependsOn: BuildDatabase
  jobs:
  - deployment: DatabaseDeployment
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: database

          # NEW: Deploy certificate to SQL Server (production mode)
          - task: PowerShell@2
            displayName: 'Deploy CLR Signing Certificate to SQL Server'
            inputs:
              targetType: 'filePath'
              filePath: '$(Pipeline.Workspace)/database/scripts/Deploy-CLRCertificate.ps1'
              arguments: '-Server "$(sqlServer)" -Database "$(sqlDatabase)" -EnableStrictSecurity $true'
              pwsh: true
            continueOnError: false

          # Deploy DACPAC...

          # NEW: Verify signatures after deployment
          - task: PowerShell@2
            displayName: 'Verify CLR Assembly Signatures and Security'
            inputs:
              targetType: 'inline'
              script: |
                # (See verification script in azure-pipelines.yml)
              pwsh: true
```

### First-Time Execution

1. **Run pipeline** (will fail on first run if certificate doesn't exist):

```
ERROR: Access is denied creating certificate in Cert:\LocalMachine\My
```

2. **On HART-DESKTOP agent**, run as Administrator:

```powershell
cd D:\Repositories\Hartonomous
.\scripts\Initialize-CLRSigning.ps1
```

3. **Re-run pipeline** (will now succeed - certificate exists):

```
✓ CLR signing certificate initialized
✓ CLR assemblies signed (X files signed, Y already signed)
✓ CLR signing certificate deployed with CLR Strict Security enabled
✓ CLR security verification complete - all assemblies signed
```

---

## GitHub Actions Integration

### Current Workflow (Updated)

**File:** `.github/workflows/ci-cd.yml`

```yaml
jobs:
  build-dacpac:
    name: Build Database DACPAC
    runs-on: [self-hosted, windows, sql-server]
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      # NEW: Initialize certificate (idempotent)
      - name: Initialize CLR Signing Certificate
        shell: pwsh
        run: ./scripts/Initialize-CLRSigning.ps1

      - name: Build DACPAC with MSBuild
        shell: pwsh
        run: |
          # (MSBuild logic)

      # NEW: Sign assemblies (auto-discovery)
      - name: Sign CLR Assemblies
        shell: pwsh
        run: ./scripts/Sign-CLRAssemblies.ps1 -Configuration Release -Verbose

      - name: Upload DACPAC artifact
        uses: actions/upload-artifact@v4
        with:
          name: database-dacpac
          path: artifacts/database/

  deploy-database:
    needs: build-dacpac
    runs-on: [self-hosted, windows, sql-server]
    environment: production
    steps:
      - name: Download DACPAC Artifact
        uses: actions/download-artifact@v4
        with:
          name: database-dacpac

      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      # NEW: Deploy certificate to SQL Server
      - name: Deploy CLR Signing Certificate to SQL Server
        shell: pwsh
        run: |
          ./scripts/Deploy-CLRCertificate.ps1 `
            -Server "${{ secrets.SQL_SERVER }}" `
            -Database "${{ secrets.SQL_DATABASE }}" `
            -EnableStrictSecurity $true

      # Deploy DACPAC...

      # NEW: Verify signatures
      - name: Verify CLR Assembly Signatures and Security
        shell: pwsh
        run: |
          # (See verification script in ci-cd.yml)
```

### First-Time Execution

Same as Azure DevOps: Run `Initialize-CLRSigning.ps1` as admin on self-hosted runner, then workflow succeeds on subsequent runs.

---

## Security Validation Queries

### Verify CLR Strict Security

```sql
SELECT name, CAST(value AS int) AS value, CAST(value_in_use AS int) AS value_in_use
FROM sys.configurations 
WHERE name = 'clr strict security';
```

**Expected (Production):**
```
name                  value  value_in_use
--------------------- ------ ------------
clr strict security   1      1
```

**Failure Condition:** `value_in_use = 0` (disabled)

---

### Verify All Assemblies Are Signed

```sql
SELECT 
  a.name AS AssemblyName,
  CASE a.permission_set 
    WHEN 1 THEN 'SAFE'
    WHEN 2 THEN 'EXTERNAL_ACCESS'
    WHEN 3 THEN 'UNSAFE_ACCESS'
  END AS PermissionSet,
  CASE 
    WHEN c.name IS NOT NULL THEN 'SIGNED (via Certificate)'
    WHEN ak.name IS NOT NULL THEN 'SIGNED (via Asymmetric Key)'
    ELSE 'UNSIGNED'
  END AS SignatureStatus,
  c.name AS CertificateName,
  c.thumbprint AS CertificateThumbprint
FROM sys.assemblies a
LEFT JOIN sys.assembly_files af ON a.assembly_id = af.assembly_id
LEFT JOIN sys.certificates c ON c.thumbprint = af.content
LEFT JOIN sys.asymmetric_keys ak ON ak.thumbprint = af.content
WHERE a.name NOT IN ('master', 'model', 'msdb', 'tempdb')
ORDER BY a.name;
```

**Expected (Production):**
```
AssemblyName            PermissionSet    SignatureStatus              CertificateName
----------------------- ---------------- ---------------------------- --------------------
Hartonomous.Clr         UNSAFE_ACCESS    SIGNED (via Certificate)     HartonomousCLRCert
MathNet.Numerics        UNSAFE_ACCESS    SIGNED (via Certificate)     HartonomousCLRCert
Neo4j.Driver            UNSAFE_ACCESS    SIGNED (via Certificate)     HartonomousCLRCert
...
```

**Failure Condition:** Any `SignatureStatus = 'UNSIGNED'`

---

### Verify Certificate and Login

```sql
-- Check certificate exists
SELECT name, thumbprint, subject, start_date, expiry_date
FROM sys.certificates
WHERE name = 'HartonomousCLRCert';

-- Check login exists
SELECT name, type_desc, create_date, modify_date
FROM sys.server_principals
WHERE name = 'HartonomousCLRLogin';

-- Check UNSAFE ASSEMBLY permission
SELECT pr.name AS PrincipalName, pe.permission_name, pe.state_desc
FROM sys.server_permissions pe
JOIN sys.server_principals pr ON pe.grantee_principal_id = pr.principal_id
WHERE pr.name = 'HartonomousCLRLogin'
  AND pe.permission_name = 'UNSAFE ASSEMBLY';
```

**Expected:**
```
-- Certificate
name                 thumbprint          subject                                          start_date           expiry_date
-------------------- ------------------- ------------------------------------------------ -------------------- --------------------
HartonomousCLRCert   0xABC123...         CN=Hartonomous CLR Code Signing Certificate...   2025-11-19 21:00:00  2035-11-19 21:00:00

-- Login
name                 type_desc           create_date          modify_date
-------------------- ------------------- -------------------- --------------------
HartonomousCLRLogin  CERTIFICATE_MAPPED  2025-11-19 21:00:00  2025-11-19 21:00:00

-- Permission
PrincipalName        permission_name     state_desc
-------------------- ------------------- ----------
HartonomousCLRLogin  UNSAFE ASSEMBLY     GRANT
```

---

## Troubleshooting

### Issue: "Access is denied" during certificate creation

**Symptom:**
```
New-SelfSignedCertificate: Access is denied. 0x80070005 (WIN32: 5 ERROR_ACCESS_DENIED)
```

**Cause:** Creating certificate in `Cert:\LocalMachine\My` requires Administrator privileges.

**Solution (Option 1 - Recommended):**
1. Open PowerShell as Administrator
2. Run `.\scripts\Initialize-CLRSigning.ps1`
3. Certificate is created and persisted (idempotent for subsequent runs)

**Solution (Option 2 - CurrentUser Store):**
Modify `Initialize-CLRSigning.ps1`:
```powershell
-CertStoreLocation "Cert:\CurrentUser\My"  # No admin required
```

**Solution (Option 3 - Azure Key Vault):**
Download certificate from Key Vault (see Option 2 above).

---

### Issue: Pipeline fails with "Assembly is not trusted"

**Symptom:**
```sql
Msg 10314, Level 16, State 11
An error occurred in the Microsoft .NET Framework while trying to load assembly id 65536.
The server may be running out of resources, or the assembly is not trusted...
```

**Cause:** CLR Strict Security is enabled, but certificate not deployed or assembly not signed.

**Solution:**
1. Verify certificate deployment:
   ```powershell
   sqlcmd -S <server> -d <database> -E -Q "SELECT * FROM sys.certificates WHERE name = 'HartonomousCLRCert'"
   ```

2. Verify assembly signatures:
   ```powershell
   Get-AuthenticodeSignature "path\to\assembly.dll"
   ```

3. Re-run `Deploy-CLRCertificate.ps1` and `Sign-CLRAssemblies.ps1`

---

### Issue: Unsigned assemblies in verification step

**Symptom:**
```
❌ ERROR: Unsigned assemblies detected in production mode!
```

**Cause:** Build or signing step failed, or new assemblies added without signing.

**Solution:**
1. Check `Sign-CLRAssemblies.ps1` output for errors
2. Verify signtool.exe is available:
   ```powershell
   where.exe signtool
   ```
3. Check for build output in unexpected locations:
   ```powershell
   Get-ChildItem -Path "src" -Recurse -Filter "*.dll" | Where-Object { $_.DirectoryName -match "bin\\Release" }
   ```
4. Re-run signing with `-Force`:
   ```powershell
   .\scripts\Sign-CLRAssemblies.ps1 -Configuration Release -Force -Verbose
   ```

---

### Issue: Certificate expired

**Symptom:**
```
New-SelfSignedCertificate: The certificate has expired.
```

**Cause:** Certificate created with 10-year validity (e.g., 2025-2035) has reached expiry date.

**Solution:**
1. Delete existing certificate:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match 'Hartonomous CLR' } | Remove-Item
   ```

2. Delete `.signing-config` and `certificates/` directory:
   ```powershell
   Remove-Item .signing-config
   Remove-Item -Recurse -Force certificates/
   ```

3. Re-run `Initialize-CLRSigning.ps1`:
   ```powershell
   .\scripts\Initialize-CLRSigning.ps1
   ```

4. Re-deploy to SQL Server:
   ```powershell
   .\scripts\Deploy-CLRCertificate.ps1 -Server <server> -Database <database> -EnableStrictSecurity $true
   ```

---

## ISO Certification Checklist

### Before Audit

- [ ] **All assemblies signed** (no `UNSIGNED` in sys.assemblies)
- [ ] **CLR Strict Security enabled** (`clr strict security = 1`)
- [ ] **Certificate valid** (not expired, 2048-bit RSA, SHA256)
- [ ] **Login exists** (`HartonomousCLRLogin` in sys.server_principals)
- [ ] **UNSAFE ASSEMBLY granted** (permission in sys.server_permissions)
- [ ] **TRUSTWORTHY OFF** (if using certificate-based signing, TRUSTWORTHY should be disabled for maximum security)
- [ ] **CI/CD verification passes** (no unsigned assemblies detected in deployment)
- [ ] **Audit trail** (pipeline logs show signing + verification steps)

### Audit Queries (Run in SSMS)

```sql
-- 1. Verify CLR Strict Security
EXEC sp_configure 'clr strict security';
-- Expected: run_value = 1

-- 2. Verify all assemblies signed
SELECT name, permission_set_desc,
       CASE WHEN (SELECT COUNT(*) FROM sys.certificates c
                  JOIN sys.assembly_files af ON c.thumbprint = af.content
                  WHERE af.assembly_id = a.assembly_id) > 0
            THEN 'SIGNED' ELSE 'UNSIGNED' END AS SignatureStatus
FROM sys.assemblies a
WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb');
-- Expected: All rows show SignatureStatus = 'SIGNED'

-- 3. Verify certificate and login
SELECT 'Certificate' AS ObjectType, name, subject FROM sys.certificates WHERE name = 'HartonomousCLRCert'
UNION ALL
SELECT 'Login' AS ObjectType, name, type_desc FROM sys.server_principals WHERE name = 'HartonomousCLRLogin';
-- Expected: 2 rows (certificate + login)

-- 4. Verify UNSAFE ASSEMBLY permission
SELECT pr.name, pe.permission_name, pe.state_desc
FROM sys.server_permissions pe
JOIN sys.server_principals pr ON pe.grantee_principal_id = pr.principal_id
WHERE pr.name = 'HartonomousCLRLogin' AND pe.permission_name = 'UNSAFE ASSEMBLY';
-- Expected: 1 row with state_desc = 'GRANT'

-- 5. Verify TRUSTWORTHY is OFF (highest security)
SELECT name, is_trustworthy_on FROM sys.databases WHERE name = 'Hartonomous';
-- Recommended: is_trustworthy_on = 0 (if using certificate signing)
```

### Documentation for Auditors

Provide auditors with:

1. **scripts/README-CLR-SIGNING.md** (complete signing infrastructure documentation)
2. **docs/operations/clr-signing-cicd.md** (this document)
3. **Pipeline logs** showing:
   - Certificate initialization
   - Assembly signing (with count of signed files)
   - Certificate deployment
   - Signature verification (no unsigned assemblies)
4. **SQL Server queries** (audit checklist above)

---

## References

### Microsoft Official Documentation

- [CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- [CREATE CERTIFICATE (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-certificate-transact-sql)
- [GRANT Server Permissions (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/statements/grant-server-permissions-transact-sql)
- [Code Signing in PowerShell](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-authenticodesignature)

### Internal Documentation

- [scripts/README-CLR-SIGNING.md](../../scripts/README-CLR-SIGNING.md) - Complete signing infrastructure
- [scripts/Initialize-CLRSigning.ps1](../../scripts/Initialize-CLRSigning.ps1) - Certificate management
- [scripts/Sign-CLRAssemblies.ps1](../../scripts/Sign-CLRAssemblies.ps1) - Auto-discovery and signing
- [scripts/Deploy-CLRCertificate.ps1](../../scripts/Deploy-CLRCertificate.ps1) - SQL Server deployment

---

## Summary

**Key Points:**

1. ✅ **One-time setup:** Create certificate on CI/CD agent (admin PowerShell)
2. ✅ **Idempotent:** All scripts safe to run multiple times
3. ✅ **Auto-discovery:** No hard-coded file lists (scans build output)
4. ✅ **Production-ready:** CLR Strict Security enabled by default
5. ✅ **Verified:** Every deployment validates signatures
6. ✅ **ISO-compliant:** No unsigned assemblies, no security bypasses

**For CI/CD agents:**

- **Local Agent Pool (HART-DESKTOP):** Pre-create certificate (Option 1)
- **Cloud-hosted agents:** Use Azure Key Vault (Option 2)
- **Development/testing:** Use CurrentUser store (Option 3)

**For auditors:**

- Run audit queries (see checklist above)
- Review pipeline logs (signing + verification)
- Verify no `UNSIGNED` assemblies in production
