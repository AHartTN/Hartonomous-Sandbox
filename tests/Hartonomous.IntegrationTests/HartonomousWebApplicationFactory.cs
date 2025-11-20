using Hartonomous.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hartonomous.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Supports both Development (Azure disabled) and Production (Azure enabled) testing.
/// </summary>
public class HartonomousWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly bool _enableAzureServices;

    /// <summary>
    /// Creates a test factory for the specified environment.
    /// </summary>
    /// <param name="environment">Environment name (Development, Production, etc.)</param>
    /// <param name="enableAzureServices">Enable Azure App Config, Key Vault, App Insights</param>
    public HartonomousWebApplicationFactory(string environment = "Development", bool enableAzureServices = false)
    {
        _environment = environment;
        _enableAzureServices = enableAzureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        
        builder.ConfigureServices(services =>
        {
            // Ensure localhost database connection for all test environments
            services.PostConfigure<DatabaseOptions>(options =>
            {
                options.HartonomousDb = "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;";
            });
        });
        
        // Suppress startup/shutdown logs to avoid delays
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Ensure all scoped services are disposed before factory disposal
            // This prevents hanging during EF Core connection cleanup
            Services?.Dispose();
        }
        base.Dispose(disposing);
    }
}
