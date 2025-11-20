using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

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
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appConfigUri = configuration["Azure:AppConfiguration:Uri"]
            ?? "https://appconfig-hartonomous.azconfig.io";

        var credential = new DefaultAzureCredential();
        _configClient = new ConfigurationClient(new Uri(appConfigUri), credential);
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
