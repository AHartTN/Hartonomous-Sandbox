using Azure.Data.AppConfiguration;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Health;

/// <summary>
/// Health check for Azure App Configuration connectivity and access permissions.
/// </summary>
public sealed class AppConfigurationHealthCheck : IHealthCheck
{
    private readonly ILogger<AppConfigurationHealthCheck> _logger;
    private readonly ConfigurationClient _configClient;

    public AppConfigurationHealthCheck(
        ILogger<AppConfigurationHealthCheck> logger,
        IOptions<AzureAppConfigurationOptions> options,
        IOptions<AzureOptions> azureOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var appConfigOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        var azureOpts = azureOptions?.Value ?? throw new ArgumentNullException(nameof(azureOptions));
        
        if (!appConfigOptions.Enabled)
        {
            throw new InvalidOperationException("Azure App Configuration is not enabled in configuration.");
        }

        var credential = azureOpts.UseManagedIdentity 
            ? new DefaultAzureCredential() 
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true });
            
        _configClient = new ConfigurationClient(new Uri(appConfigOptions.Endpoint!), credential);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to read a single setting to verify connectivity
            var settingsPages = _configClient.GetConfigurationSettingsAsync(
                new SettingSelector { KeyFilter = "*" },
                cancellationToken);

            await using var enumerator = settingsPages.GetAsyncEnumerator(cancellationToken);
            
            if (await enumerator.MoveNextAsync())
            {
                _logger.LogDebug("App Configuration health check passed.");
                return HealthCheckResult.Healthy("App Configuration is accessible.");
            }

            return HealthCheckResult.Degraded("App Configuration is accessible but contains no settings.");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogWarning(ex, "App Configuration health check failed due to insufficient permissions.");
            return HealthCheckResult.Unhealthy("App Configuration access denied (403). Check RBAC permissions.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "App Configuration health check failed.");
            return HealthCheckResult.Unhealthy("App Configuration connection failed.", ex);
        }
    }
}
