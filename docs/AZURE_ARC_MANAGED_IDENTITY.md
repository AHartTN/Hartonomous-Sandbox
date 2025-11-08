# Azure Arc SQL Managed Identity Configuration

## Overview

All 4 Hartonomous services are configured to use **Azure Managed Identity** authentication with Azure SQL, eliminating the need for connection string credentials. This document outlines the Azure Arc SQL Server configuration requirements.

## Connection String Configuration

### Verified Production Settings

All services use the following connection string pattern:

```
Server=tcp:hartonomous-sql.database.windows.net,1433;
Database=Hartonomous;
Authentication=ActiveDirectoryManagedIdentity;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
MultipleActiveResultSets=true;
Pooling=true;
```

### Service-Specific Pool Sizes

| Service | Min Pool Size | Max Pool Size | Rationale |
|---------|---------------|---------------|-----------|
| **Api** | 10 | 200 | High concurrency web API |
| **CesConsumer** | 5 | 50 | Background message processing |
| **ModelIngestion** | 5 | 100 | Batch ML model ingestion |
| **Neo4jSync** | 5 | 50 | Periodic graph synchronization |

## Azure Arc SQL Server Requirements

### 1. Azure Arc-Enabled SQL Server

**Register SQL Server with Azure Arc**:

```powershell
# Install Azure Arc agent
$servicePrincipalClientId = "YOUR_APP_ID"
$servicePrincipalSecret = "YOUR_SECRET"

# Download installation script
Invoke-WebRequest -Uri https://aka.ms/azcmagent-windows -OutFile AzureConnectedMachineAgent.msi

# Install agent
msiexec /i AzureConnectedMachineAgent.msi /quiet /norestart

# Connect to Azure Arc
& "$env:ProgramW6432\AzureConnectedMachineAgent\azcmagent.exe" connect `
    --service-principal-id $servicePrincipalClientId `
    --service-principal-secret $servicePrincipalSecret `
    --resource-group "hartonomous-rg" `
    --tenant-id "YOUR_TENANT_ID" `
    --location "eastus" `
    --subscription-id "YOUR_SUBSCRIPTION_ID" `
    --cloud "AzureCloud"
```

**Verify Arc Agent**:

```powershell
# Check service status
Get-Service -Name himds

# Verify connection
& "$env:ProgramW6432\AzureConnectedMachineAgent\azcmagent.exe" show
```

### 2. System-Assigned Managed Identity

**Enable managed identity for Arc SQL Server**:

```bash
# Azure CLI
az connectedmachine identity assign \
    --resource-group hartonomous-rg \
    --name hartonomous-sql-server
```

**Retrieve Principal ID**:

```bash
az connectedmachine show \
    --resource-group hartonomous-rg \
    --name hartonomous-sql-server \
    --query identity.principalId -o tsv
```

### 3. Azure SQL Database Permissions

**Grant database access to managed identity**:

```sql
-- Connect to Azure SQL Database as admin
USE [Hartonomous];
GO

-- Create login for Arc SQL Server managed identity
CREATE USER [hartonomous-sql-server] FROM EXTERNAL PROVIDER;
GO

-- Grant database permissions
ALTER ROLE db_owner ADD MEMBER [hartonomous-sql-server];
GO

-- Verify user created
SELECT name, type_desc, authentication_type_desc
FROM sys.database_principals
WHERE name = 'hartonomous-sql-server';
```

**Expected Output**:

```
name                      type_desc            authentication_type_desc
------------------------- -------------------- ------------------------
hartonomous-sql-server    EXTERNAL_USER        EXTERNAL
```

### 4. Local Token Access Configuration

**Azure Arc agent token path**:

```
C:\ProgramData\AzureConnectedMachineAgent\Tokens\
```

**SQL Server service account requirements**:

1. **Add SQL service account to Arc agent group**:

```powershell
# Get SQL Server service account
$sqlServiceAccount = (Get-WmiObject Win32_Service | Where-Object {$_.Name -eq 'MSSQLSERVER'}).StartName

# Add to Hybrid Agent Extension Applications group
Add-LocalGroupMember -Group "Hybrid agent extension applications" -Member $sqlServiceAccount
```

2. **Grant token folder permissions**:

```powershell
$tokenPath = "C:\ProgramData\AzureConnectedMachineAgent\Tokens"
$acl = Get-Acl $tokenPath

# Add read permissions for SQL service account
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $sqlServiceAccount,
    "ReadAndExecute",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($accessRule)
Set-Acl -Path $tokenPath -AclObject $acl
```

3. **Restart SQL Server**:

```powershell
Restart-Service -Name MSSQLSERVER
```

## Application Configuration

### appsettings.Production.json (All Services)

All 4 services have production configs created with managed identity:

#### Hartonomous.Api

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=tcp:hartonomous-sql.database.windows.net,1433;Database=Hartonomous;Authentication=ActiveDirectoryManagedIdentity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=true;Min Pool Size=10;Max Pool Size=200;Pooling=true"
  },
  "ApplicationInsights": {
    "ConnectionString": "@Microsoft.KeyVault(SecretUri=https://hartonomous-kv.vault.azure.net/secrets/AppInsights-ConnectionString)"
  }
}
```

#### CesConsumer / Neo4jSync

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=tcp:hartonomous-sql.database.windows.net,1433;Database=Hartonomous;Authentication=ActiveDirectoryManagedIdentity;...",
    "ServiceBus": "@Microsoft.KeyVault(SecretUri=https://hartonomous-kv.vault.azure.net/secrets/ServiceBus-ConnectionString)"
  }
}
```

#### ModelIngestion

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=tcp:hartonomous-sql.database.windows.net,1433;Database=Hartonomous;Authentication=ActiveDirectoryManagedIdentity;..."
  }
}
```

### Key Vault Access (Optional)

If using Key Vault references for Application Insights/Service Bus:

```bash
# Grant managed identity access to Key Vault
az keyvault set-policy \
    --name hartonomous-kv \
    --object-id <MANAGED_IDENTITY_PRINCIPAL_ID> \
    --secret-permissions get list
```

## Verification

### 1. Test Managed Identity Authentication

**From SQL Server machine**:

```powershell
# Test token acquisition
$token = & "$env:ProgramW6432\AzureConnectedMachineAgent\azcmagent.exe" token -r https://database.windows.net/

if ($token) {
    Write-Host "✓ Token acquired successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Token acquisition failed" -ForegroundColor Red
}
```

### 2. Test Database Connection

**From application runtime**:

```csharp
// Program.cs test code
using Microsoft.Data.SqlClient;

var connectionString = builder.Configuration.GetConnectionString("HartonomousDb");
using var connection = new SqlConnection(connectionString);

try {
    await connection.OpenAsync();
    Console.WriteLine("✓ Managed identity authentication successful");
    
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT SUSER_SNAME()";
    var user = await command.ExecuteScalarAsync();
    Console.WriteLine($"  Authenticated as: {user}");
}
catch (Exception ex) {
    Console.WriteLine($"✗ Authentication failed: {ex.Message}");
}
```

### 3. Monitor Connection Health

**Azure SQL Query**:

```sql
-- View active connections using managed identity
SELECT
    session_id,
    login_name,
    host_name,
    program_name,
    login_time,
    last_request_start_time
FROM sys.dm_exec_sessions
WHERE login_name = 'hartonomous-sql-server'
ORDER BY login_time DESC;
```

## Troubleshooting

### Error: "Login failed for user 'NT AUTHORITY\ANONYMOUS LOGON'"

**Cause**: SQL service account cannot access Arc token folder

**Fix**:

```powershell
# Verify SQL service account is in Arc group
Get-LocalGroupMember -Group "Hybrid agent extension applications"

# Check token folder permissions
icacls "C:\ProgramData\AzureConnectedMachineAgent\Tokens"

# Restart SQL Server after adding permissions
Restart-Service MSSQLSERVER
```

### Error: "The token provided is expired or invalid"

**Cause**: Arc agent not connected or token refresh failed

**Fix**:

```powershell
# Check Arc agent status
& "$env:ProgramW6432\AzureConnectedMachineAgent\azcmagent.exe" show

# Reconnect if necessary
& "$env:ProgramW6432\AzureConnectedMachineAgent\azcmagent.exe" disconnect
& "$env:ProgramW6432\AzureConnectedMachineAgent\azcmagent.exe" connect ...

# Restart himds service
Restart-Service -Name himds
```

### Error: "Cannot open database 'Hartonomous' requested by the login"

**Cause**: Managed identity has no database permissions

**Fix**:

```sql
-- Connect to Azure SQL as admin
USE [Hartonomous];
CREATE USER [hartonomous-sql-server] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [hartonomous-sql-server];
```

## Security Best Practices

### 1. Principle of Least Privilege

Instead of `db_owner`, use granular permissions:

```sql
-- Create custom role for application
CREATE ROLE hartonomous_app;

-- Grant specific permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO hartonomous_app;
GRANT EXECUTE ON SCHEMA::dbo TO hartonomous_app;

-- Assign to managed identity
ALTER ROLE hartonomous_app ADD MEMBER [hartonomous-sql-server];
```

### 2. Network Security

**Restrict Azure SQL firewall**:

```bash
# Allow only Arc-enabled SQL Server's public IP
az sql server firewall-rule create \
    --resource-group hartonomous-rg \
    --server hartonomous-sql \
    --name AllowArcSqlServer \
    --start-ip-address <ARC_SERVER_PUBLIC_IP> \
    --end-ip-address <ARC_SERVER_PUBLIC_IP>
```

### 3. Audit Logging

**Enable Azure SQL auditing**:

```bash
az sql server audit-policy update \
    --resource-group hartonomous-rg \
    --name hartonomous-sql \
    --state Enabled \
    --storage-account hartonomous-audit-logs
```

## Production Deployment Checklist

- [ ] Azure Arc agent installed and connected
- [ ] System-assigned managed identity enabled
- [ ] SQL service account added to 'Hybrid agent extension applications' group
- [ ] Token folder permissions granted (ReadAndExecute)
- [ ] SQL Server restarted
- [ ] Azure SQL user created from external provider
- [ ] Database permissions granted (db_owner or custom role)
- [ ] Connection tested from application
- [ ] Key Vault permissions configured (if using Key Vault)
- [ ] Firewall rules configured
- [ ] Audit logging enabled
- [ ] Monitoring dashboards configured

## References

- [Azure Arc-enabled SQL Server Overview](https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/overview)
- [Configure Azure AD authentication for Arc SQL](https://learn.microsoft.com/en-us/azure/azure-arc/servers/managed-identity-authentication)
- [Microsoft.Data.SqlClient Managed Identity](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql/azure-active-directory-authentication)
- [Azure SQL Database Firewall Rules](https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-configure)

## Conclusion

✅ All 4 Hartonomous services use managed identity authentication
✅ Connection pooling optimized per service workload
✅ No credentials in configuration files
✅ Production-ready Azure Arc SQL Server configuration documented
