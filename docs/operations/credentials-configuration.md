# Credential Configuration Guide

This document explains how credentials are managed across different environments in Hartonomous, following Azure best practices.

## Configuration Priority Order

ASP.NET Core loads configuration in this order (highest priority first):
1. **Command-line arguments** (highest)
2. **Environment variables**
3. **User secrets** (Development only)
4. `appsettings.{Environment}.json`
5. `appsettings.json` (lowest)

Later sources **override** earlier ones.

## Never Hardcode Credentials

❌ **NEVER** hardcode credentials in:
- `appsettings.json`
- `appsettings.{Environment}.json`
- Source code files

✅ **ALWAYS** use:
- User secrets for local development
- Environment variables for test/staging
- Key Vault with managed identity for production

## Environment-Specific Configuration

### Development Environment

Use **User Secrets** (NOT committed to source control):

```powershell
# Set Neo4j credentials
dotnet user-secrets set "Neo4j:Password" "neo4jneo4j" --project src/Hartonomous.Api

# Set connection strings
dotnet user-secrets set "ConnectionStrings:HartonomousDb" "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;" --project src/Hartonomous.Api
```

User secrets stored in:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Test/Staging Environment

Use **Environment Variables** (set in test fixtures or CI/CD):

```powershell
# PowerShell
$env:Neo4j__Uri = "bolt://localhost:7687"
$env:Neo4j__Username = "neo4j"
$env:Neo4j__Password = "neo4jneo4j"
$env:Neo4j__Database = "neo4j"
$env:ConnectionStrings__HartonomousDb = "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;"
```

Note: Use `__` (double underscore) for hierarchical keys in environment variables.

### Production Environment

Use **Azure Key Vault** with **Managed Identity**:

1. **Store secrets in Key Vault**:
   ```bash
   az keyvault secret set --vault-name "hartonomous-kv" --name "Neo4j-Password" --value "<production-password>"
   ```

2. **Reference in App Configuration**:
   ```json
   {
     "Neo4j:Password": {
       "uri": "https://hartonomous-kv.vault.azure.net/secrets/Neo4j-Password"
     }
   }
   ```

3. **Assign Managed Identity**:
   ```bash
   # For App Service
   az webapp identity assign --name "hartonomous-api" --resource-group "hartonomous-rg"
   
   # Grant Key Vault access
   az keyvault set-policy --name "hartonomous-kv" --object-id <managed-identity-id> --secret-permissions get list
   ```

## Integration Testing

The `ProductionConfigWebApplicationFactory` uses environment variables to override `appsettings.Staging.json`:

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment("Staging");
    
    // Environment variables override appsettings.Staging.json
    Environment.SetEnvironmentVariable("Neo4j__Password", "neo4jneo4j");
    Environment.SetEnvironmentVariable("ConnectionStrings__HartonomousDb", 
        "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;");
}
```

The `appsettings.Staging.json` file contains **empty placeholders only** - no actual credentials.

## Key Vault Custom Secret Resolver (Optional)

For graceful fallback when Key Vault is unavailable:

```csharp
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.SetSecretResolver((uri) =>
    {
        // Fallback to environment variable if Key Vault auth fails
        var secretName = uri.Split('/').Last();
        return Environment.GetEnvironmentVariable($"KeyVault__{secretName}") 
            ?? throw new InvalidOperationException($"Secret not found: {secretName}");
    });
});
```

## DefaultAzureCredential Authentication Chain

When using `DefaultAzureCredential`, authentication is attempted in this order:
1. **Environment variables** (`AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`)
2. **Managed Identity** (Arc-enabled servers, App Service, Azure VMs)
3. **Azure CLI** (`az login`)
4. **Visual Studio** (signed-in account)
5. **Visual Studio Code** (signed-in account)

Each method fails gracefully and tries the next.

## Security Best Practices

1. ✅ Use User Secrets for local development
2. ✅ Use Environment Variables for test/staging
3. ✅ Use Key Vault + Managed Identity for production
4. ✅ Keep `appsettings.*.json` files with placeholder values only
5. ✅ Add `secrets.json` to `.gitignore`
6. ❌ Never commit credentials to source control
7. ❌ Never hardcode passwords in configuration files
8. ❌ Never share production credentials via email/chat

## Troubleshooting

### "Neo4j password is required" error
- **Development**: Run `dotnet user-secrets set "Neo4j:Password" "neo4jneo4j"`
- **Tests**: Set `$env:Neo4j__Password = "neo4jneo4j"` before running tests
- **Production**: Verify Key Vault reference and managed identity permissions

### Key Vault authentication failures
- Check managed identity is assigned: `az webapp identity show --name <app-name> --resource-group <rg-name>`
- Verify Key Vault access policy: `az keyvault show --name <kv-name> --query properties.accessPolicies`
- Check `DefaultAzureCredential` logs in Application Insights

### Configuration not loading
- Check environment name: `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`
- Verify configuration priority: Command-line > Env vars > User secrets > appsettings.{Env}.json > appsettings.json
- Use `IConfiguration.GetDebugView()` to see all loaded values and their sources

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)
- [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
