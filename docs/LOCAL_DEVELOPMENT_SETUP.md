# Local Development Environment Setup

## Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Azure CLI (authenticated with `az login`)
- SQL Server 2025 (for FILESTREAM and CLR support)
- Neo4j (optional for local graph database testing)

## Configuration Strategy

This project uses a **secure configuration hierarchy**:

1. **Local Development**: User Secrets (secure, not in source control)
2. **Azure App Configuration**: Centralized configuration management
3. **Azure Key Vault**: Secure secret storage (passwords, API keys, connection strings)

## Local Setup

### Step 1: Azure Authentication

Ensure you're logged into Azure CLI (your identity is used for DefaultAzureCredential):

```powershell
az login
az account show
```

### Step 2: User Secrets (Already Configured)

The project is already initialized with User Secrets. You can verify:

```powershell
cd src/Hartonomous.Api
dotnet user-secrets list
```

**Current Configuration**:
- `Endpoints:AppConfiguration` = `https://appconfig-hartonomous.azconfig.io`

### Step 3: How It Works

When you run the application locally:

1. **DefaultAzureCredential** uses your Azure CLI login
2. App connects to **Azure App Configuration** using the endpoint from User Secrets
3. App Configuration pulls configuration values (some reference Key Vault)
4. App Configuration's **managed identity** reads secrets from **Key Vault**
5. All configuration is available via `IConfiguration` in your code

```
Your Local Machine → Azure App Configuration → Azure Key Vault
  (Azure CLI)           (Managed Identity)         (Secrets)
```

## Configuration Available from App Configuration

All configuration is automatically loaded from Azure App Configuration:

- **Azure AD Settings**: `AzureAd:*`
- **External ID Settings**: `EntraExternalId:*`
- **Neo4j Connection**: `Neo4j:Uri`, `Neo4j:Username`, `Neo4j:Password` (from Key Vault)
- **Azure Storage**: `AzureStorage:*`
- **Application Insights**: `ApplicationInsights:ConnectionString` (from Key Vault)
- **HuggingFace**: `HuggingFace:ApiToken` (from Key Vault)
- **Feature Flags**: Various feature toggles

## Adding Secrets (If Needed)

### For Local Testing Only
If you need to override a value locally:

```powershell
cd src/Hartonomous.Api
dotnet user-secrets set "SomeSection:SomeKey" "LocalValue"
```

### For All Environments (Production)
Add to Azure App Configuration:

```powershell
# Add a regular configuration value
az appconfig kv set --name appconfig-hartonomous --key "MyApp:Setting" --value "MyValue"

# Add a Key Vault reference for secrets
az keyvault secret set --vault-name kv-hartonomous --name "MySecret" --value "SecretValue"
az appconfig kv set-keyvault --name appconfig-hartonomous --key "MyApp:Secret" --secret-identifier "https://kv-hartonomous.vault.azure.net/secrets/MySecret"
```

## Environment Variables (Alternative Method)

If you prefer environment variables over User Secrets, set:

```powershell
# PowerShell
$env:Endpoints__AppConfiguration = "https://appconfig-hartonomous.azconfig.io"

# Windows Command Prompt
set Endpoints__AppConfiguration=https://appconfig-hartonomous.azconfig.io

# Add to Windows Environment Variables permanently via System Properties
```

**Note**: Use double underscore `__` for hierarchical keys in environment variables (replaces `:`)

## Running the Application

```powershell
cd src/Hartonomous.Api
dotnet run
```

The application will:
1. Load User Secrets
2. Connect to Azure App Configuration
3. Resolve Key Vault references
4. Start with all configuration available

## Troubleshooting

### "Unauthorized" errors when accessing App Configuration
- Run `az login` to authenticate
- Verify you have the "App Configuration Data Reader" role:
  ```powershell
  az role assignment list --scope /subscriptions/ed614e1a-7d8b-4608-90c8-66e86c37080b/resourceGroups/rg-hartonomous/providers/Microsoft.AppConfiguration/configurationStores/appconfig-hartonomous
  ```

### "Forbidden" errors when accessing Key Vault
- App Configuration managed identity needs "Key Vault Secrets User" role ✅ (already configured)
- For local debugging, you need "Key Vault Secrets User" role

### Missing configuration values
- Check App Configuration has the key:
  ```powershell
  az appconfig kv list --name appconfig-hartonomous --key "YourKey:*"
  ```
- Check Key Vault has the secret (for Key Vault references):
  ```powershell
  az keyvault secret list --vault-name kv-hartonomous
  ```

## Security Notes

- ✅ **User Secrets** are stored in `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json` (outside project directory)
- ✅ **User Secrets** are NOT checked into source control
- ✅ **Key Vault** secrets are encrypted at rest and in transit
- ✅ **Managed Identities** eliminate the need for credentials in code
- ❌ **Never** commit secrets to source control
- ❌ **Never** put secrets in `appsettings.json` or `appsettings.Development.json`

## References

- [Safe storage of app secrets in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure App Configuration documentation](https://learn.microsoft.com/en-us/azure/azure-app-configuration/)
- [Azure Key Vault configuration provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
