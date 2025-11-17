# Azure Arc Authentication - Quick Setup Checklist

**Complete these steps in order. Each step is required.**

---

## ☐ Step 1: Install SQL Server Arc Extension

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

**⚠️ CRITICAL**: Must use `"OUTBOUND AND INBOUND"` - not just `"OUTBOUND ONLY"`

---

## ☐ Step 2: Enable TCP/IP Protocol

```powershell
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp' -Name Enabled -Value 1
net stop MSSQLSERVER /y
net start MSSQLSERVER
```

**Why?**: Token authentication requires TCP/IP protocol.

---

## ☐ Step 3: Grant Microsoft Graph Permissions

```powershell
# Get Principal ID
$principalId = (az connectedmachine show --name "<MACHINE-NAME>" --resource-group "<RESOURCE-GROUP>" --query "identity.principalId" -o tsv)

# Get Graph Resource ID
$graphResourceId = (az ad sp list --filter "appId eq '00000003-0000-0000-c000-000000000000'" --query "[0].id" -o tsv)

# User.Read.All
$body = @{principalId=$principalId;resourceId=$graphResourceId;appRoleId="df021288-bdef-4463-88db-98f22de89214"} | ConvertTo-Json
$body | Out-File temp.json -Encoding utf8
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body @temp.json
Remove-Item temp.json

# GroupMember.Read.All
$body = @{principalId=$principalId;resourceId=$graphResourceId;appRoleId="98830695-27a2-44f7-8c18-0c3ebc9698f6"} | ConvertTo-Json
$body | Out-File temp.json -Encoding utf8
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body @temp.json
Remove-Item temp.json

# Application.Read.All
$body = @{principalId=$principalId;resourceId=$graphResourceId;appRoleId="9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30"} | ConvertTo-Json
$body | Out-File temp.json -Encoding utf8
az rest --method POST --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" --body @temp.json
Remove-Item temp.json
```

---

## ☐ Step 4: Configure SQL Server 2025 Registry (CRITICAL!)

**SQL Server 2025 ONLY** - Must manually configure registry:

```powershell
# Get values
$tenantId = "<TENANT-ID>"
$machineClientId = "<ARC-MACHINE-APP-ID>"  # NOT Principal ID! Get with: az ad sp show --id <principal-id>
$regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQLServer\FederatedAuthentication"

# Configure registry
Set-ItemProperty -Path $regPath -Name "HIMDSEndpoint" -Value "http://localhost:40342/metadata/identity/oauth2/token" -Type String
Set-ItemProperty -Path $regPath -Name "ArcServerSystemAssignedManagedIdentityTenantId" -Value $tenantId -Type String
Set-ItemProperty -Path $regPath -Name "ArcServerSystemAssignedManagedIdentityClientId" -Value $machineClientId -Type String
Set-ItemProperty -Path $regPath -Name "PrimaryAADTenant" -Value $tenantId -Type String
Set-ItemProperty -Path $regPath -Name "HIMDSApiVersion" -Value "2020-06-01" -Type String
Set-ItemProperty -Path $regPath -Name "MsGraphEndPoint" -Value "graph.microsoft.com" -Type String
Set-ItemProperty -Path $regPath -Name "AADGraphEndPoint" -Value "graph.windows.net" -Type String
Set-ItemProperty -Path $regPath -Name "GraphAPIEndpoint" -Value "graph.windows.net" -Type String
Set-ItemProperty -Path $regPath -Name "AuthenticationEndpoint" -Value "login.microsoftonline.com" -Type String
Set-ItemProperty -Path $regPath -Name "ServicePrincipalName" -Value "https://database.windows.net/" -Type String
# ... (see full doc for all 25+ required values)
```

**Why?**: SQL Server 2025 requires manual registry config. Azure portal checkbox is NOT enough!

---

## ☐ Step 5: Restart SQL Server (CRITICAL!)

```powershell
net stop MSSQLSERVER /y
net start MSSQLSERVER
```

**Why?**: SQL Server must restart to recognize Graph permissions, inbound auth settings, and registry configuration.

---

## ☐ Step 6: Create SQL Server Login

```sql
CREATE LOGIN [<MACHINE-NAME>] FROM EXTERNAL PROVIDER;
ALTER SERVER ROLE sysadmin ADD MEMBER [<MACHINE-NAME>];
```

**Verify:**

```sql
SELECT name, type_desc FROM sys.server_principals WHERE name = '<MACHINE-NAME>';
```

---

## ☐ Step 7: Create Database Users

```sql
USE <DATABASE-NAME>;
CREATE USER [<MACHINE-NAME>] FROM LOGIN [<MACHINE-NAME>];
ALTER ROLE db_owner ADD MEMBER [<MACHINE-NAME>];
```

---

## ☐ Step 8: Configure Antivirus Exclusion

Add exclusion for GitHub Actions runner directory:

- Path: `D:\GitHub\actions-runner\_work`
- Scope: On-access, On-demand, Embedded scripts

---

## ☐ Step 9: Configure GitHub Secrets

Repository → Settings → Environments → production:

- `SQL_SERVER`: `<MACHINE-NAME>` (**NO** `tcp:` prefix)
- `SQL_DATABASE`: `<DATABASE-NAME>`

---

## Verify Setup

```powershell
# Get token
$endpoint = "http://localhost:40342/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://database.windows.net/"
$response = Invoke-WebRequest -Uri $endpoint -Headers @{Metadata='true'} -UseBasicParsing
$wwwAuth = $response.Headers.'WWW-Authenticate'
$enum = $wwwAuth.GetEnumerator()
while ($enum.MoveNext()) {
    if ($enum.Current -match 'Basic realm=(.+)') { $secretFile = $matches[1]; break }
}
$secret = Get-Content -Raw $secretFile
$tokenResponse = Invoke-RestMethod -Method GET -Uri $endpoint -Headers @{Metadata='true'; Authorization="Basic $secret"} -UseBasicParsing
$token = $tokenResponse.access_token

# Test connection
Invoke-Sqlcmd -ServerInstance "<MACHINE-NAME>" -Database "master" -Query "SELECT SUSER_NAME()" -AccessToken $token -TrustServerCertificate
```

**Expected**: Returns `<MACHINE-NAME>`

---

## Common Mistakes

- ❌ Using `"OUTBOUND ONLY"` instead of `"OUTBOUND AND INBOUND"`
- ❌ **SQL Server 2025**: Forgetting manual registry configuration (MOST COMMON!)
- ❌ **SQL Server 2025**: Using Principal ID instead of App ID in registry
- ❌ Forgetting to restart SQL Server after Graph permissions
- ❌ Forgetting to restart SQL Server after registry configuration
- ❌ Including `tcp:` prefix in `SQL_SERVER` secret
- ❌ TCP/IP protocol not enabled
- ❌ Missing Graph permissions (need all 3)

---

**See full documentation**: `docs/setup/ARC-AUTHENTICATION-SETUP.md`
