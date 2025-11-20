using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Health;

/// <summary>
/// Health check for Azure Key Vault connectivity and access permissions.
/// </summary>
public sealed class KeyVaultHealthCheck : IHealthCheck
{
    private readonly ILogger<KeyVaultHealthCheck> _logger;
    private readonly SecretClient _secretClient;

    public KeyVaultHealthCheck(
        ILogger<KeyVaultHealthCheck> logger,
        IOptions<KeyVaultOptions> options,
        IOptions<AzureOptions> azureOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var keyVaultOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        var azureOpts = azureOptions?.Value ?? throw new ArgumentNullException(nameof(azureOptions));
        
        if (!keyVaultOptions.Enabled)
        {
            throw new InvalidOperationException("Key Vault is not enabled in configuration.");
        }

        var credential = azureOpts.UseManagedIdentity 
            ? new DefaultAzureCredential() 
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true });
            
        _secretClient = new SecretClient(new Uri(keyVaultOptions.VaultUri!), credential);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to list secrets (requires Key Vault Secrets User role minimum)
            var secretsPages = _secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
            await using var enumerator = secretsPages.GetAsyncEnumerator(cancellationToken);
            
            // Just verify we can enumerate (don't need to read all)
            if (await enumerator.MoveNextAsync())
            {
                _logger.LogDebug("Key Vault health check passed.");
                return HealthCheckResult.Healthy("Key Vault is accessible.");
            }

            return HealthCheckResult.Degraded("Key Vault is accessible but contains no secrets.");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogWarning(ex, "Key Vault health check failed due to insufficient permissions.");
            return HealthCheckResult.Unhealthy("Key Vault access denied (403). Check RBAC permissions.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key Vault health check failed.");
            return HealthCheckResult.Unhealthy("Key Vault connection failed.", ex);
        }
    }
}
