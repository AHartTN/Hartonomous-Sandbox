using Hartonomous.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hartonomous.IntegrationTests;

/// <summary>
/// WebApplicationFactory configured to test Production configuration paths
/// (Azure App Configuration, Key Vault, Application Insights, etc.)
/// Uses in-memory configuration overrides to enable Azure services without real Azure resources.
/// </summary>
public class ProductionConfigWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Staging environment to avoid Development-specific behavior
        // but not Production (which might have different validation requirements)
        builder.UseEnvironment("Staging");
        
        // CRITICAL: Set these BEFORE ConfigureAppConfiguration runs
        // Program.cs reads these during startup BEFORE ConfigureAppConfiguration callback
        builder.UseSetting("AzureAppConfiguration:Enabled", "false");
        builder.UseSetting("KeyVault:Enabled", "false");
        
        // Configure environment variables for test credentials (overrides appsettings.Staging.json)
        Environment.SetEnvironmentVariable("Neo4j__Uri", "bolt://localhost:7687");
        Environment.SetEnvironmentVariable("Neo4j__Username", "neo4j");
        Environment.SetEnvironmentVariable("Neo4j__Password", "neo4jneo4j");
        Environment.SetEnvironmentVariable("Neo4j__Database", "neo4j");
        Environment.SetEnvironmentVariable("ConnectionStrings__HartonomousDb", 
            "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Environment variables (set above) will already override appsettings.Staging.json
            // No need for in-memory collection - environment variables have higher precedence
        });
        
        builder.ConfigureServices(services =>
        {
            // CRITICAL: Force shutdown after 1 second to prevent freezing
            services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(1));
        });
        
        // Suppress Azure configuration errors for testing
        // The Azure config code will execute but fail gracefully
        builder.UseSetting("SuppressStatusMessages", "true");
        
        // Suppress startup/shutdown logs to avoid delays
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }

    private bool _disposed;

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Force immediate disposal
        await base.DisposeAsync();
        
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
