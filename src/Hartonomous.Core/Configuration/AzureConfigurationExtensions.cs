using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Extension methods for configuring Azure services in application startup.
/// Eliminates duplicate configuration code across 5+ Program.cs files.
/// </summary>
public static class AzureConfigurationExtensions
{
    /// <summary>
    /// Adds all Azure configuration sources (App Config, Key Vault, Monitoring).
    /// </summary>
    public static IHostApplicationBuilder AddAzureConfiguration(this IHostApplicationBuilder builder)
    {
        builder.AddAzureAppConfiguration();
        builder.AddAzureKeyVault();
        builder.AddAzureMonitoring();
        return builder;
    }

    private static void AddAzureAppConfiguration(this IHostApplicationBuilder builder)
    {
        var endpoint = builder.Configuration["AzureAppConfigurationEndpoint"];
        if (string.IsNullOrEmpty(endpoint)) return;

        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(new Uri(endpoint), new DefaultAzureCredential())
                   .UseFeatureFlags();
        });
    }

    private static void AddAzureKeyVault(this IHostApplicationBuilder builder)
    {
        var uri = builder.Configuration["KeyVaultUri"];
        if (string.IsNullOrEmpty(uri)) return;

        builder.Configuration.AddAzureKeyVault(
            new Uri(uri),
            new DefaultAzureCredential());
    }

    private static void AddAzureMonitoring(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrEmpty(connectionString)) return;

        builder.Services.AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            });
    }
}
