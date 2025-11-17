# Hartonomous Complete Setup Prerequisites

**All manual setup steps required before the CI/CD pipeline will work.**

---

## System Requirements

### Hardware
- Windows Server 2019+ or Windows 10/11 Pro
- 16GB+ RAM
- 100GB+ available disk space
- Multi-core processor (4+ cores recommended)

### Software
- **SQL Server 2025 RC1** (or SQL Server 2022+)
- **Visual Studio 2022** Enterprise/Professional or MSBuild Tools
- **.NET 10 SDK** (for .NET 10 projects)
- **.NET Framework 4.8.1** (for CLR assemblies)
- **PowerShell 7.5+** (for scripts and GitHub Actions)
- **SQL Server Data Tools (SSDT)** (for DACPAC builds)
- **SqlPackage CLI** (for DACPAC deployments)
- **Azure CLI** (`az`) - for Arc management
- **Git** (for source control)

---

## 1. SQL Server Configuration

### 1.1 Enable TCP/IP Protocol

**Required for Arc token-based authentication.**

```powershell
# Enable TCP/IP in registry
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp' -Name Enabled -Value 1

# Restart SQL Server
net stop MSSQLSERVER /y
net start MSSQLSERVER
```

**Verify**: SQL Server Configuration Manager → Protocols → TCP/IP should show "Enabled"

### 1.2 Enable CLR Integration

```sql
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
EXEC sp_configure 'clr strict security', 0; RECONFIGURE;
```

### 1.3 Set TRUSTWORTHY Property

```sql
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;
```

### 1.4 Configure Mixed Mode Authentication

SQL Server must support both Windows and SQL authentication (if using SQL logins).

---

## 2. Azure Arc Setup (For GitHub Actions Authentication)

### 2.1 Install Azure Arc Agent

```powershell
# Download and install from Azure Portal
# Follow: https://portal.azure.com → Azure Arc → Servers → Add
```

### 2.2 Install SQL Server Arc Extension

```powershell
az connectedmachine extension create `
  --name "WindowsAgent.SqlServer" `
  --machine-name "<MACHINE-NAME>" `
  --resource-group "<RESOURCE-GROUP>" `
  --location "eastus" `
  --publisher "Microsoft.AzureData" `
  --type "WindowsAgent.SqlServer" `
  --settings '{
    "AzureAD": [{
      "instanceName": "MSSQLSERVER",
      "managedIdentityAuthSetting": "OUTBOUND AND INBOUND",
      "tenantId": "<TENANT-ID>"
    }],
    "SqlManagement": {"IsEnabled": "true"}
  }'
```

**CRITICAL**: `managedIdentityAuthSetting` MUST be `"OUTBOUND AND INBOUND"` (not `"OUTBOUND ONLY"`).

### 2.3 Grant Microsoft Graph Permissions

```powershell
$principalId = "<ARC-MACHINE-PRINCIPAL-ID>"
$graphResourceId = "<MS-GRAPH-RESOURCE-ID>"

# User.Read.All
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body '{
  "principalId": "'$principalId'",
  "resourceId": "'$graphResourceId'",
  "appRoleId": "df021288-bdef-4463-88db-98f22de89214"
}'

# GroupMember.Read.All
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body '{
  "principalId": "'$principalId'",
  "resourceId": "'$graphResourceId'",
  "appRoleId": "98830695-27a2-44f7-8c18-0c3ebc9698f6"
}'

# Application.Read.All
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body '{
  "principalId": "'$principalId'",
  "resourceId": "'$graphResourceId'",
  "appRoleId": "9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30"
}'
```

### 2.4 **SQL Server 2025 ONLY**: Configure Registry for Arc Managed Identity

**CRITICAL**: SQL Server 2025 requires manual registry configuration.

```powershell
$tenantId = "<TENANT-ID>"
$machineClientId = "<ARC-MACHINE-APP-ID>"  # NOT Principal ID!
$regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\FederatedAuthentication"

# See docs/setup/ARC-AUTHENTICATION-SETUP.md for full list of 25+ registry values
Set-ItemProperty -Path $regPath -Name "HIMDSEndpoint" -Value "http://localhost:40342/metadata/identity/oauth2/token" -Type String
Set-ItemProperty -Path $regPath -Name "ArcServerSystemAssignedManagedIdentityTenantId" -Value $tenantId -Type String
Set-ItemProperty -Path $regPath -Name "ArcServerSystemAssignedManagedIdentityClientId" -Value $machineClientId -Type String
# ... (25+ more values - see full documentation)
```

**Get Arc machine App ID**:
```powershell
$principalId = (az connectedmachine show --name <MACHINE-NAME> --resource-group <RG> --query "identity.principalId" -o tsv)
az ad sp show --id $principalId --query "appId" -o tsv  # This is the App ID
```

**After registry config, RESTART SQL SERVER**:
```powershell
net stop MSSQLSERVER /y
net start MSSQLSERVER
```

### 2.5 Create SQL Server Login for Arc Managed Identity

```sql
CREATE LOGIN [<MACHINE-NAME>] FROM EXTERNAL PROVIDER;
ALTER SERVER ROLE sysadmin ADD MEMBER [<MACHINE-NAME>];

-- Create database user
USE Hartonomous;
CREATE USER [<MACHINE-NAME>] FROM LOGIN [<MACHINE-NAME>];
ALTER ROLE db_owner ADD MEMBER [<MACHINE-NAME>];
```

---

## 3. GitHub Actions Runner Setup

### 3.1 Install Self-Hosted Runner

1. Repository → Settings → Actions → Runners → New self-hosted runner
2. Follow setup instructions for Windows
3. Install as Windows Service (run as SYSTEM or dedicated service account)
4. Apply labels: `self-hosted`, `windows`, `sql-server`

### 3.2 Configure Antivirus Exclusions

**Bitdefender** (or other AV):
- Path: `D:\GitHub\actions-runner\_work`
- Scope: On-access scanning, On-demand scanning, Embedded scripts

**Why**: PowerShell scripts create temp SQL files that antivirus may block.

### 3.3 Configure GitHub Secrets

Repository → Settings → Environments → production:

| Secret Name | Value | Notes |
|------------|-------|-------|
| `SQL_SERVER` | `HART-DESKTOP` | Machine name, NO `tcp:` prefix |
| `SQL_DATABASE` | `Hartonomous` | Database name |

**CRITICAL**: Do NOT include `tcp:` prefix in `SQL_SERVER` - `Invoke-Sqlcmd` adds it automatically when using `-AccessToken`.

---

## 4. Visual Studio / MSBuild Setup

### 4.1 Install Visual Studio 2022

**Required components**:
- .NET desktop development
- Data storage and processing (for SSDT)
- .NET Framework 4.8.1 development tools

**OR** install standalone:
- **MSBuild Tools 2022**
- **SQL Server Data Tools (SSDT)**

### 4.2 Verify MSBuild Path

```powershell
# Workflow expects MSBuild at one of these paths:
"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
"C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
```

### 4.3 Install SqlPackage CLI

Download from: https://learn.microsoft.com/sql/tools/sqlpackage/sqlpackage-download

Add to PATH or ensure available at expected location.

---

## 5. Strong Name Key (SNK) Setup

### 5.1 Generate or Copy SNK Files

CLR assemblies require strong naming:

```powershell
# Generate new keys (if not exist)
sn -k dependencies/Hartonomous.snk
sn -k dependencies/SqlClrKey.snk
```

**OR** copy existing keys from secure location.

### 5.2 Secure SNK Files

- **Production**: Store in Azure Key Vault, retrieve during build
- **Development**: Local file with restricted permissions

---

## 6. Database Initialization

### 6.1 Create Database

```sql
CREATE DATABASE Hartonomous
ON PRIMARY (
  NAME = N'Hartonomous',
  FILENAME = N'D:\SQLData\Hartonomous.mdf',
  SIZE = 1GB,
  FILEGROWTH = 256MB
)
LOG ON (
  NAME = N'Hartonomous_log',
  FILENAME = N'D:\SQLLogs\Hartonomous_log.ldf',
  SIZE = 512MB,
  FILEGROWTH = 128MB
);
```

### 6.2 Initial Schema Setup

First deployment will create schema via DACPAC.

---

## 7. External CLR Dependencies

### 7.1 Copy Dependencies to `dependencies/` Folder

Required assemblies:
- `System.Text.Json.dll`
- `System.Memory.dll`
- `System.Buffers.dll`
- `System.Runtime.CompilerServices.Unsafe.dll`
- `Microsoft.Bcl.AsyncInterfaces.dll`
- (Others as needed)

**OR** restore via NuGet during build.

### 7.2 Verify Assembly Versions

CLR assemblies must match SQL Server's CLR host version (.NET Framework 4.8.1).

---

## 8. Neo4j Setup (Optional - For Graph Sync)

### 8.1 Install Neo4j

Download from: https://neo4j.com/download/

### 8.2 Configure Connection

Update `appsettings.json` in `Hartonomous.Workers.Neo4jSync`:

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "<password>"
  }
}
```

---

## 9. Network Configuration

### 9.1 Firewall Rules

Allow inbound connections:
- **SQL Server**: Port 1433 (TCP)
- **GitHub Actions Runner**: Outbound HTTPS (443)
- **Arc HIMDS**: Localhost only (40342)

### 9.2 DNS Configuration

Ensure machine hostname resolves correctly for Arc authentication.

---

## 10. Service Account Permissions

### 10.1 SQL Server Service Account

Default: `NT Service\MSSQLSERVER`

**Required permissions**:
- Read/Execute on `C:\ProgramData\AzureConnectedMachineAgent\Tokens\`
- Member of **Hybrid agent extension applications** group (for SQL Server 2025 Arc auth)

### 10.2 GitHub Actions Runner Service Account

Default: `SYSTEM` or dedicated service account

**Required permissions**:
- Read/Write on `D:\GitHub\actions-runner\_work`
- Execute PowerShell scripts
- Access to Arc HIMDS endpoint (localhost:40342)

---

## Verification Checklist

- [ ] SQL Server installed and running
- [ ] TCP/IP protocol enabled and SQL Server restarted
- [ ] CLR integration enabled
- [ ] TRUSTWORTHY property set
- [ ] Azure Arc agent installed and connected
- [ ] SQL Server Arc extension installed with `"OUTBOUND AND INBOUND"`
- [ ] **SQL Server 2025**: Registry configured with Arc managed identity values
- [ ] Microsoft Graph permissions granted (3 permissions)
- [ ] **SQL Server 2025**: SQL Server restarted after registry config
- [ ] SQL Server login created for Arc machine identity
- [ ] Database created
- [ ] Visual Studio/MSBuild installed and accessible
- [ ] SqlPackage CLI installed
- [ ] SNK files present in `dependencies/`
- [ ] External CLR dependencies in `dependencies/`
- [ ] GitHub Actions runner installed and running
- [ ] Runner has correct labels (`self-hosted`, `windows`, `sql-server`)
- [ ] Antivirus exclusions configured
- [ ] GitHub secrets configured (`SQL_SERVER`, `SQL_DATABASE`)
- [ ] Firewall rules configured

---

## Troubleshooting

See detailed troubleshooting in:
- `docs/setup/ARC-AUTHENTICATION-SETUP.md` - Arc authentication issues
- `docs/setup/ARC-SETUP-CHECKLIST.md` - Quick setup checklist
- GitHub Actions workflow logs - Build/deployment failures

---

## Quick Start (After Prerequisites)

1. Trigger workflow: Repository → Actions → CI/CD Pipeline → Run workflow
2. Monitor: Check workflow logs for any failures
3. Verify: Connect to SQL Server and verify schema deployment

---

## References

- [Azure Arc SQL Server Setup](docs/setup/ARC-AUTHENTICATION-SETUP.md)
- [SQL Server CLR Assembly Deployment](docs/guides/CLR-DEPLOYMENT.md)
- [GitHub Actions Self-Hosted Runners](https://docs.github.com/en/actions/hosting-your-own-runners)
- [SQL Server Data Tools (SSDT)](https://learn.microsoft.com/sql/ssdt/)
- [SqlPackage Documentation](https://learn.microsoft.com/sql/tools/sqlpackage/)
