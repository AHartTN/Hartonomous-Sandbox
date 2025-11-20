# Azure Arc SQL Server Service Principal Authentication Setup

## Overview

The pipeline now uses **Microsoft Entra Service Principal authentication** to deploy to Azure Arc-enabled on-premises SQL Server. This is the correct approach for on-premises Arc servers (managed identity is Azure-only).

## Why Service Principal Instead of SQL Authentication?

1. **Security**: No plaintext passwords in pipeline variables
2. **Azure Arc Integration**: Proper authentication method for Arc-enabled SQL Server
3. **Centralized Identity Management**: Managed through Microsoft Entra ID
4. **Certificate Support**: Can use certificates instead of secrets for enhanced security

## Prerequisites

### 1. Azure Arc SQL Server Setup

Your SQL Server must be:
- **Connected to Azure Arc** (already done based on your comment "these are deploying to azure arc on prem servers")
- **SQL Server 2022 or later** for Microsoft Entra authentication support
- **On-premises or Azure Stack** (not Azure SQL Database)

### 2. Service Principal Creation

Create a service principal for the deployment pipeline:

**Option A: Using Azure Portal**
1. Go to Azure Portal → Microsoft Entra ID → App Registrations
2. Click "New registration"
3. Name: `Hartonomous-Pipeline-SP`
4. Click "Register"
5. Note the **Application (client) ID** and **Directory (tenant) ID**
6. Go to "Certificates & secrets" → "New client secret"
7. Add description: "Pipeline deployment secret"
8. Set expiration (recommendation: 24 months with regular rotation)
9. **Copy the secret value immediately** (only shown once)

**Option B: Using Azure CLI**
```bash
# Create service principal
az ad sp create-for-rbac --name "Hartonomous-Pipeline-SP" --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group}

# Output will include:
# - appId (Application ID)
# - password (Client Secret)
# - tenant (Tenant ID)
```

### 3. Grant SQL Server Permissions to Service Principal

The service principal needs permissions on your SQL Server:

**Using Azure Portal:**
1. Go to your Arc-enabled SQL Server resource in Azure Portal
2. Go to Access Control (IAM)
3. Add role assignment:
   - Role: **SQL Server Contributor** or custom role with SQL permissions
   - Assign access to: **Service principal**
   - Select: Your service principal (`Hartonomous-Pipeline-SP`)

**Using Azure CLI:**
```bash
# Get the service principal object ID
SP_OBJECT_ID=$(az ad sp show --id {app-id} --query id -o tsv)

# Assign role to Arc SQL Server resource
az role assignment create \
  --assignee $SP_OBJECT_ID \
  --role "SQL Server Contributor" \
  --scope /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.AzureArcData/sqlServerInstances/{arc-sql-server-name}
```

### 4. Configure SQL Server for Microsoft Entra Authentication

Follow the official Microsoft tutorial:
https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/entra-authentication-setup-tutorial

Key steps:
1. Create Azure Key Vault (if not exists)
2. Create certificate for SQL Server
3. Configure SQL Server to use Microsoft Entra ID
4. Enable the service principal as a login in SQL Server:

```sql
-- Connect to SQL Server with sysadmin privileges
USE [master]
GO

-- Create login from service principal
CREATE LOGIN [Hartonomous-Pipeline-SP] FROM EXTERNAL PROVIDER
GO

-- Grant necessary permissions
ALTER SERVER ROLE [sysadmin] ADD MEMBER [Hartonomous-Pipeline-SP]
GO

-- Verify
SELECT name, type_desc, is_disabled 
FROM sys.server_principals 
WHERE name = 'Hartonomous-Pipeline-SP'
GO
```

## Azure DevOps Configuration

### 1. Create Azure Service Connection

In Azure DevOps:
1. Go to Project Settings → Service connections
2. Click "New service connection"
3. Select "Azure Resource Manager" → Next
4. Authentication method: **Service principal (manual)**
5. Enter details:
   - **Environment**: Azure Cloud (or Azure Stack if applicable)
   - **Scope Level**: Subscription
   - **Subscription ID**: Your Azure subscription ID
   - **Subscription Name**: Your Azure subscription name
   - **Service Principal Id**: The **Application (client) ID** from step 2
   - **Service Principal Key** or **Certificate**: The client secret from step 2
   - **Tenant ID**: The **Directory (tenant) ID** from step 2
6. **Connection name**: Use a descriptive name (e.g., `AzureArcSQLConnection`)
7. **Security**: Check "Grant access permission to all pipelines" (or manually grant per pipeline)
8. Click "Verify and save"

### 2. Update Pipeline Variables

Update your `azure-pipelines.yml` or pipeline variables:

**Required Variables:**
- `azureServiceConnection`: The name of the service connection you created above
- `sqlServer`: Your SQL Server hostname (e.g., `localhost` or `server.domain.com`)
- `sqlDatabase`: Target database name (e.g., `Hartonomous`)

**Remove These Variables:**
- ~~`sqlUsername`~~ (no longer needed)
- ~~`sqlPassword`~~ (no longer needed)

**In Azure DevOps UI:**
1. Go to Pipelines → Select your pipeline → Edit
2. Click Variables (top right)
3. Add variable:
   - **Name**: `azureServiceConnection`
   - **Value**: The exact name of your service connection (e.g., `AzureArcSQLConnection`)

## How It Works

### Authentication Flow

1. **AzureCLI@2 Task** authenticates using the service principal from the service connection
2. **az account get-access-token** retrieves a short-lived access token for `https://database.windows.net/`
3. **Access token is passed** to:
   - SqlPackage via `/AccessToken:$token`
   - PowerShell scripts via `-AccessToken $token` parameter
4. Scripts use **Invoke-Sqlcmd -AccessToken** to execute SQL commands
5. **Token automatically expires** after use (typically 1 hour)

### Updated Pipeline Tasks

All SQL deployment tasks now use this pattern:

```yaml
- task: AzureCLI@2
  displayName: 'Task Name (Service Principal Auth)'
  inputs:
    azureSubscription: '$(azureServiceConnection)'
    scriptType: 'pscore'
    scriptLocation: 'inlineScript'
    inlineScript: |
      # Get access token
      $token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
      
      # Use token with script or command
      & script.ps1 -Server "$(sqlServer)" -UseAzureAD -AccessToken $token
```

## Security Best Practices

### 1. Service Principal Secret Rotation
- Rotate client secrets every 12-24 months
- Use certificates instead of secrets when possible
- Store secrets in Azure Key Vault
- Update service connection in Azure DevOps after rotation

### 2. Least Privilege Access
- Grant only necessary permissions to service principal
- Use custom roles instead of `Contributor` or `sysadmin` if possible
- Limit scope to specific resource groups

### 3. Audit and Monitoring
- Enable audit logging on SQL Server
- Monitor service principal sign-ins in Microsoft Entra ID
- Review Azure Activity Log for resource access

### 4. Pipeline Security
- Use secret variables for sensitive data
- Enable "Grant access permission to all pipelines" only when necessary
- Review pipeline permissions regularly

## Troubleshooting

### Error: "Failed to retrieve access token"

**Cause**: Service principal doesn't have access to subscription or resource

**Solution**:
1. Verify service connection is valid: Test connection in Azure DevOps
2. Check service principal has role assignment on subscription/resource group
3. Ensure tenant ID is correct

### Error: "Login failed for user 'NT AUTHORITY\\ANONYMOUS LOGON'"

**Cause**: SQL Server not configured for Microsoft Entra authentication

**Solution**:
1. Follow the setup tutorial: https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/entra-authentication-setup-tutorial
2. Verify certificate is properly configured in Azure Key Vault
3. Restart SQL Server service after configuration

### Error: "Cannot find the object because it does not exist"

**Cause**: Service principal login not created in SQL Server

**Solution**:
```sql
CREATE LOGIN [Hartonomous-Pipeline-SP] FROM EXTERNAL PROVIDER
ALTER SERVER ROLE [sysadmin] ADD MEMBER [Hartonomous-Pipeline-SP]
```

### Error: "The token provided is not valid"

**Cause**: Token expired or wrong resource URL

**Solution**:
- Ensure using `https://database.windows.net/` as resource URL (NOT `https://management.azure.com/`)
- Token lifetime is ~1 hour, ensure not reusing old tokens
- Verify service principal credentials are current

## References

- [SqlPackage Authentication with Service Principal](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage?view=sql-server-ver17#authentication)
- [Microsoft Entra Authentication for SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/azure-ad-authentication-sql-server-overview)
- [Azure Arc-enabled SQL Server Setup](https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/entra-authentication-setup-tutorial)
- [Service Principals in Azure DevOps](https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/service-principal-managed-identity)
- [Connect to Azure with Service Principal](https://learn.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure)

## Quick Start Checklist

- [ ] Service principal created in Microsoft Entra ID
- [ ] Service principal has role assignment on Azure resources
- [ ] SQL Server configured for Microsoft Entra authentication
- [ ] Service principal login created in SQL Server with sysadmin role
- [ ] Azure service connection created in Azure DevOps
- [ ] Pipeline variable `azureServiceConnection` configured
- [ ] Old `sqlUsername` and `sqlPassword` variables removed
- [ ] Pipeline run tested successfully

## Support

If you encounter issues:
1. Review the [troubleshooting section](#troubleshooting) above
2. Check Azure DevOps pipeline logs for specific error messages
3. Verify SQL Server logs for authentication errors
4. Consult Microsoft documentation links in References section
