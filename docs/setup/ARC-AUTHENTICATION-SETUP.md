# Azure Arc Managed Identity Authentication Setup Guide

This document outlines all manual prerequisite steps required to enable Azure Arc managed identity authentication for SQL Server in GitHub Actions workflows.

## Overview

The CI/CD pipeline uses Azure Arc machine managed identity to authenticate to SQL Server without storing credentials. This requires one-time Azure configuration and SQL Server setup.

---

## Prerequisites

### Required Software
- Azure CLI (`az`) installed and authenticated
- SQL Server 2022+ with Azure Arc extension installed
- PowerShell 5.1+ or PowerShell Core 7+
- GitHub Actions self-hosted runner (Windows)

### Required Azure Resources
- Azure subscription with appropriate permissions
- Resource group for Arc resources
- Azure Arc-enabled server (machine)
- SQL Server Arc extension installed on the machine

### Required Permissions
- **Azure**: Contributor or Owner on the subscription/resource group
- **SQL Server**: sysadmin role (for initial setup)
- **Microsoft Graph**: Application.ReadWrite.All (to grant app role assignments)

---

## One-Time Setup Steps

### 1. Enable Azure Arc on Windows Machine

If not already done, install and configure Azure Arc agent:

```powershell
# Download and install Azure Arc agent
# Follow: https://learn.microsoft.com/azure/azure-arc/servers/onboard-portal

# Verify Arc agent is running
azcmagent show

# Note the system-assigned managed identity Principal ID for later steps
```

### 2. Install SQL Server Arc Extension

Install the SQL Server extension on your Arc-enabled machine:

```powershell
# Get your machine name and resource group
$machineName = "HART-DESKTOP"  # Replace with your machine name
$resourceGroup = "rg-hartonomous"  # Replace with your resource group
$subscriptionId = "ed614e1a-7d8b-4608-90c8-66e86c37080b"  # Replace with your subscription

# Install SQL Server extension with INBOUND authentication enabled
az connectedmachine extension create `
  --name "WindowsAgent.SqlServer" `
  --machine-name $machineName `
  --resource-group $resourceGroup `
  --location "eastus" `
  --publisher "Microsoft.AzureData" `
  --type "WindowsAgent.SqlServer" `
  --settings '{
    "AzureAD": [
      {
        "instanceName": "MSSQLSERVER",
        "managedIdentityAuthSetting": "OUTBOUND AND INBOUND",
        "tenantId": "<YOUR-TENANT-ID>"
      }
    ],
    "SqlManagement": {
      "IsEnabled": "true"
    }
  }'
```

**CRITICAL**: The `managedIdentityAuthSetting` MUST be `"OUTBOUND AND INBOUND"` to allow Azure identities to authenticate to SQL Server.

### 3. Enable TCP/IP Protocol in SQL Server

Azure Arc authentication with tokens requires TCP/IP protocol:

```powershell
# Enable TCP/IP in registry
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp' -Name Enabled -Value 1

# Restart SQL Server (required!)
net stop MSSQLSERVER /y
net start MSSQLSERVER
```

**Why?**: `Invoke-Sqlcmd` with `-AccessToken` automatically uses `tcp:` protocol prefix. SQL Server must have TCP/IP enabled.

### 4. Grant Microsoft Graph Permissions to Managed Identity

SQL Server needs Graph API permissions to validate Azure AD logins:

```powershell
# Get your Arc machine's managed identity Principal ID
$principalId = "505c61a6-bcd6-4f22-aee5-5c6c0094ae0d"  # From: az connectedmachine show

# Get Microsoft Graph service principal resource ID
$graphResourceId = (az ad sp list --filter "appId eq '00000003-0000-0000-c000-000000000000'" --query "[0].id" -o tsv)

# Grant User.Read.All permission
$userReadAllId = "df021288-bdef-4463-88db-98f22de89214"
$body = @{
    principalId = $principalId
    resourceId = $graphResourceId
    appRoleId = $userReadAllId
} | ConvertTo-Json
$body | Out-File -FilePath temp_graph_perm.json -Encoding utf8
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body `@temp_graph_perm.json
Remove-Item temp_graph_perm.json

# Grant GroupMember.Read.All permission
$groupMemberReadAllId = "98830695-27a2-44f7-8c18-0c3ebc9698f6"
$body = @{
    principalId = $principalId
    resourceId = $graphResourceId
    appRoleId = $groupMemberReadAllId
} | ConvertTo-Json
$body | Out-File -FilePath temp_graph_perm.json -Encoding utf8
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body `@temp_graph_perm.json
Remove-Item temp_graph_perm.json

# Grant Application.Read.All permission
$appReadAllId = "9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30"
$body = @{
    principalId = $principalId
    resourceId = $graphResourceId
    appRoleId = $appReadAllId
} | ConvertTo-Json
$body | Out-File -FilePath temp_graph_perm.json -Encoding utf8
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body `@temp_graph_perm.json
Remove-Item temp_graph_perm.json
```

**Why?**: SQL Server validates Azure AD principals by querying Microsoft Graph API.

### 5. Update SQL Server Extension to Enable Inbound Authentication

If you initially installed with `"OUTBOUND ONLY"`, update it:

```powershell
$machineName = "HART-DESKTOP"
$resourceGroup = "rg-hartonomous"
$tenantId = "6c9c44c4-f04b-4b5f-bea0-f1069179799c"
$managedIdentityClientId = "505c61a6-bcd6-4f22-aee5-5c6c0094ae0d"

az connectedmachine extension update `
  --name "WindowsAgent.SqlServer" `
  --machine-name $machineName `
  --resource-group $resourceGroup `
  --settings "{
    `"AzureAD`": [
      {
        `"instanceName`": `"MSSQLSERVER`",
        `"managedIdentityAuthSetting`": `"OUTBOUND AND INBOUND`",
        `"managedIdentityClientId`": `"$managedIdentityClientId`",
        `"tenantId`": `"$tenantId`"
      }
    ],
    `"SqlManagement`": {
      `"IsEnabled`": `"true`"
    }
  }"
```

### 5a. **CRITICAL - SQL Server 2025 ONLY**: Configure Registry for Managed Identity

**SQL Server 2025** requires manual registry configuration. The Azure portal checkbox and extension setting are **NOT ENOUGH**. You must manually configure the registry:

```powershell
# Get your values
$tenantId = "6c9c44c4-f04b-4b5f-bea0-f1069179799c"  # Your tenant ID
$machineClientId = "88f7aa07-4a0d-4a25-8fa5-b3e35480a9c8"  # Arc machine App ID (NOT Principal ID!)

# Registry path
$regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\FederatedAuthentication"

# Configure all required values
Set-ItemProperty -Path $regPath -Name "ArcServerManagedIdentityClientId" -Value "" -Type String
Set-ItemProperty -Path $regPath -Name "HIMDSApiVersion" -Value "2020-06-01" -Type String
Set-ItemProperty -Path $regPath -Name "HIMDSEndpoint" -Value "http://localhost:40342/metadata/identity/oauth2/token" -Type String
Set-ItemProperty -Path $regPath -Name "ArcServerSystemAssignedManagedIdentityTenantId" -Value $tenantId -Type String
Set-ItemProperty -Path $regPath -Name "ArcServerSystemAssignedManagedIdentityClientId" -Value $machineClientId -Type String
Set-ItemProperty -Path $regPath -Name "PrimaryAADTenant" -Value $tenantId -Type String
Set-ItemProperty -Path $regPath -Name "AADChannelMaxBufferedMessageSize" -Value "200000" -Type String
Set-ItemProperty -Path $regPath -Name "AADGraphEndPoint" -Value "graph.windows.net" -Type String
Set-ItemProperty -Path $regPath -Name "AADGroupLookupMaxRetryAttempts" -Value "10" -Type String
Set-ItemProperty -Path $regPath -Name "AADGroupLookupMaxRetryDuration" -Value "30000" -Type String
Set-ItemProperty -Path $regPath -Name "AADGroupLookupRetryInitialBackoff" -Value "100" -Type String
Set-ItemProperty -Path $regPath -Name "AADServerAdminSid" -Value "00000000-0000-0000-0000-000000000000" -Type String
Set-ItemProperty -Path $regPath -Name "AuthenticationEndpoint" -Value "login.microsoftonline.com" -Type String
Set-ItemProperty -Path $regPath -Name "CacheMaxSize" -Value "300" -Type String
Set-ItemProperty -Path $regPath -Name "ClientCertBlackList" -Value "" -Type String
Set-ItemProperty -Path $regPath -Name "FederationMetadataEndpoint" -Value "login.windows.net" -Type String
Set-ItemProperty -Path $regPath -Name "GraphAPIEndpoint" -Value "graph.windows.net" -Type String
Set-ItemProperty -Path $regPath -Name "IssuerURL" -Value "https://sts.windows.net/" -Type String
Set-ItemProperty -Path $regPath -Name "OnBehalfOfAuthority" -Value "https://login.windows.net/" -Type String
Set-ItemProperty -Path $regPath -Name "STSURL" -Value "https://login.windows.net/" -Type String
Set-ItemProperty -Path $regPath -Name "MsGraphEndPoint" -Value "graph.microsoft.com" -Type String
Set-ItemProperty -Path $regPath -Name "SendX5c" -Value "false" -Type String
Set-ItemProperty -Path $regPath -Name "ServicePrincipalName" -Value "https://database.windows.net/" -Type String
Set-ItemProperty -Path $regPath -Name "ServicePrincipalNameForArcadia" -Value "https://sql.azuresynapse.net" -Type String
Set-ItemProperty -Path $regPath -Name "ServicePrincipalNameForArcadiaDogfood" -Value "https://sql.azuresynapse-dogfood.net" -Type String
Set-ItemProperty -Path $regPath -Name "ServicePrincipalNameNoSlash" -Value "https://database.windows.net" -Type String
Set-ItemProperty -Path $regPath -Name "AADBecWSConnectionPoolMaxSize" -Value "500" -Type String
```

**How to get the Arc machine App ID (Client ID)**:

```powershell
# Get App ID from Arc machine
az connectedmachine show --name HART-DESKTOP --resource-group rg-hartonomous --query "identity.principalId" -o tsv  # This is Principal ID
az ad sp show --id <Principal-ID-from-above> --query "appId" -o tsv  # This is the App ID you need
```

### 5b. **CRITICAL - SQL Server 2025 ONLY**: Grant SQL Server Service Account Permissions to Tokens Folder

SQL Server needs access to the Arc agent's token files:

```powershell
# Grant permissions (must run as Administrator)
$acl = Get-Acl "C:\ProgramData\AzureConnectedMachineAgent\Tokens"
$permission = "NT Service\MSSQLSERVER","ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl "C:\ProgramData\AzureConnectedMachineAgent\Tokens" $acl

# For named instances, use: NT Service\MSSQL$<instancename>
```

**Also add SQL Server service account to Hybrid agent extension applications group**:

1. Open **Computer Management** → **Local Users and Groups** → **Groups**
2. Double-click **Hybrid agent extension applications**
3. Click **Add...** and add `NT Service\MSSQLSERVER` (or `NT Service\MSSQL$<instancename>` for named instances)
4. Click **OK**

**CRITICAL**: After registry configuration and folder permissions, **RESTART SQL SERVER** to apply changes:

```powershell
net stop MSSQLSERVER /y
net start MSSQLSERVER
```

### 6. Create SQL Server Login for Arc Managed Identity

After SQL Server restarts with inbound auth enabled:

```sql
-- Create login using machine name
CREATE LOGIN [HART-DESKTOP] FROM EXTERNAL PROVIDER;

-- Grant sysadmin (or specific permissions as needed)
ALTER SERVER ROLE sysadmin ADD MEMBER [HART-DESKTOP];

-- Create database user (repeat for each database)
USE Hartonomous;
CREATE USER [HART-DESKTOP] FROM LOGIN [HART-DESKTOP];
```

**Verify the login was created:**

```sql
SELECT name, type_desc, create_date, is_disabled 
FROM sys.server_principals 
WHERE name = 'HART-DESKTOP';
```

### 7. Configure Bitdefender (or Other Antivirus) Exclusions

GitHub Actions runner creates temporary files that may be blocked:

```powershell
# Add exclusion for runner working directory
# Bitdefender -> Protection -> Antivirus -> Settings -> Manage Exceptions
# Add: D:\GitHub\actions-runner\_work
# Scope: On-access scanning, On-demand scanning, Embedded scripts
```

**Why?**: PowerShell scripts may create temporary SQL files that antivirus blocks.

### 8. Configure GitHub Secrets

Set environment-level secrets in GitHub:

- Navigate to: Repository → Settings → Environments → production
- Add secrets:
  - `SQL_SERVER`: `HART-DESKTOP` (machine name, NOT `tcp:HART-DESKTOP`)
  - `SQL_DATABASE`: `Hartonomous` (or your database name)

**IMPORTANT**: Do NOT include `tcp:` prefix in `SQL_SERVER` secret. PowerShell's `Invoke-Sqlcmd` with `-AccessToken` automatically adds it.

---

## Per-Environment Setup

Repeat these steps for each GitHub Actions environment (production, staging, etc.):

### 1. Create SQL Server Login (if different identity per environment)

If using different Arc machines per environment:

```sql
CREATE LOGIN [<MACHINE-NAME>] FROM EXTERNAL PROVIDER;
ALTER SERVER ROLE sysadmin ADD MEMBER [<MACHINE-NAME>];
```

### 2. Update GitHub Environment Secrets

Update `SQL_SERVER` and `SQL_DATABASE` for each environment.

---

## Verification Checklist

Before running workflows, verify:

- [ ] Azure Arc agent installed and connected (`azcmagent show`)
- [ ] SQL Server Arc extension installed with `"OUTBOUND AND INBOUND"` setting
- [ ] **SQL Server 2025 ONLY**: Registry configured with Arc managed identity values (step 5a)
- [ ] TCP/IP protocol enabled in SQL Server configuration
- [ ] SQL Server restarted after TCP/IP enabled
- [ ] Microsoft Graph permissions granted to managed identity (3 permissions)
- [ ] SQL Server restarted after Graph permissions granted
- [ ] SQL Server restarted after extension update
- [ ] **SQL Server 2025 ONLY**: SQL Server restarted after registry configuration
- [ ] SQL Server login created for Arc machine identity
- [ ] Login has appropriate permissions (sysadmin or specific roles)
- [ ] Database users created in target databases
- [ ] Bitdefender (or antivirus) exclusions configured
- [ ] GitHub secrets configured correctly (no `tcp:` prefix)

---

## Testing Authentication

Test the Arc managed identity authentication manually:

```powershell
# Get Arc HIMDS token
$endpoint = "http://localhost:40342/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://database.windows.net/"
$response = Invoke-WebRequest -Uri $endpoint -Headers @{Metadata='true'} -UseBasicParsing
$wwwAuth = $response.Headers.'WWW-Authenticate'
$enum = $wwwAuth.GetEnumerator()
$secretFile = $null
while ($enum.MoveNext()) {
    if ($enum.Current -match 'Basic realm=(.+)') {
        $secretFile = $matches[1]
        break
    }
}
$secret = Get-Content -Raw $secretFile
$tokenResponse = Invoke-RestMethod -Method GET -Uri $endpoint -Headers @{Metadata='true'; Authorization="Basic $secret"} -UseBasicParsing
$token = $tokenResponse.access_token

# Test SQL connection with token
Invoke-Sqlcmd -ServerInstance "HART-DESKTOP" -Database "master" -Query "SELECT SUSER_NAME() AS CurrentUser, @@SERVERNAME AS ServerName" -AccessToken $token -TrustServerCertificate
```

**Expected output**: `CurrentUser` should show `HART-DESKTOP` (the machine identity).

---

## Troubleshooting

### "Login failed for user ''"

**Cause**: Arc managed identity login not created in SQL Server.

**Solution**: Run step 6 (Create SQL Server Login).

### "Server identity does not have permissions to access MS Graph"

**Cause**: Missing Microsoft Graph permissions OR SQL Server hasn't reloaded after granting permissions.

**Solution**: 
1. Verify Graph permissions granted (step 4)
2. Restart SQL Server

### "Unable to query Azure AD certificate from local cert store"

**Cause**: SQL Server extension not configured for inbound authentication OR SQL Server not restarted.

**Solution**:
1. Verify extension has `"OUTBOUND AND INBOUND"` setting (step 5)
2. **SQL Server 2025**: Configure registry (step 5a)
3. Restart SQL Server

### "Command 'CREATE LOGIN FROM EXTERNAL PROVIDER' is not supported as Azure Active Directory is not configured"

**Cause**: SQL Server 2025 registry not configured for Arc managed identity.

**Solution**:
1. Configure registry with all required values (step 5a)
2. Verify `HIMDSEndpoint`, `ArcServerSystemAssignedManagedIdentityTenantId`, and `ArcServerSystemAssignedManagedIdentityClientId` are set
3. Restart SQL Server
4. Check SQL error log: `EXEC xp_readerrorlog 0, 1, 'Azure'`

### "tcp: No such host is known"

**Cause**: TCP/IP protocol disabled in SQL Server.

**Solution**: Run step 3 (Enable TCP/IP Protocol).

### Token acquisition fails with URI parse error

**Cause**: PowerShell WWW-Authenticate header parsing issue.

**Solution**: Use `GetEnumerator()` pattern (shown in Testing section).

---

## Automation Opportunities

The following steps **cannot be automated** and require manual setup:

1. Azure Arc agent installation (requires admin elevation)
2. SQL Server Arc extension installation (Azure resource creation)
3. **SQL Server 2025**: Manual registry configuration for Arc managed identity (step 5a)
4. Microsoft Graph permissions (requires high-privilege Azure AD permissions)
5. TCP/IP protocol enablement (registry change + SQL restart)
6. Bitdefender exclusion configuration (security software config)
7. Initial SQL Server login creation (requires sysadmin)
8. **SQL Server 2025**: Multiple SQL Server restarts required (after Graph permissions, after extension update, after registry config)

The following **could be automated** but require careful consideration:

- SQL Server extension update (via Azure CLI in workflow)
- Database user creation (via workflow after login exists)
- GitHub secrets management (via GitHub CLI with appropriate PAT)

---

## Security Considerations

### Managed Identity vs Service Principal

This setup uses **system-assigned managed identity** which:
- ✅ No credentials to store or rotate
- ✅ Automatically managed by Azure
- ✅ Tied to the Arc machine lifecycle
- ❌ Cannot be used across multiple machines

For multi-machine scenarios, consider **user-assigned managed identity**.

### Least Privilege

The setup grants `sysadmin` for simplicity. For production:

```sql
-- Create login
CREATE LOGIN [HART-DESKTOP] FROM EXTERNAL PROVIDER;

-- Grant specific permissions instead of sysadmin
GRANT VIEW SERVER STATE TO [HART-DESKTOP];
GRANT ALTER ANY DATABASE TO [HART-DESKTOP];

-- Per-database permissions
USE Hartonomous;
CREATE USER [HART-DESKTOP] FROM LOGIN [HART-DESKTOP];
ALTER ROLE db_owner ADD MEMBER [HART-DESKTOP];
```

### Token Scope

The Arc HIMDS token is scoped to `https://database.windows.net/`, which is the Azure SQL Database resource identifier. This works for both Azure SQL and Arc-enabled SQL Server with Azure AD authentication.

---

## References

- [Azure Arc-enabled SQL Server Overview](https://learn.microsoft.com/sql/sql-server/azure-arc/overview)
- [Configure Azure AD authentication for Arc SQL Server](https://learn.microsoft.com/sql/sql-server/azure-arc/configure-azure-ad-authentication)
- [Azure Arc HIMDS Documentation](https://learn.microsoft.com/azure/azure-arc/servers/managed-identity-authentication)
- [SQL Server External Provider Authentication](https://learn.microsoft.com/sql/t-sql/statements/create-login-transact-sql#external-provider)
- [Microsoft Graph Permissions Reference](https://learn.microsoft.com/graph/permissions-reference)

---

## Support

For issues or questions:
1. Check workflow logs in GitHub Actions
2. Review SQL Server error logs: `EXEC xp_readerrorlog`
3. Verify Arc agent status: `azcmagent show`
4. Check extension status: `az connectedmachine extension show`
