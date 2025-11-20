using Hartonomous.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hartonomous.IntegrationTests;

/// <summary>
/// WebApplicationFactory configured to test Production configuration paths
/// (Azure App Configuration, Key Vault, Application Insights, etc.)
/// Uses in-memory configuration overrides to enable Azure services without real Azure resources.
/// </summary>
public class ProductionConfigWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Staging environment to avoid Development-specific behavior
        // but not Production (which might have different validation requirements)
        builder.UseEnvironment("Staging");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Load base configuration from appsettings.json to get real Azure endpoints
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            
            // Override specific settings for test environment
            // Uses REAL Azure endpoints from appsettings.json but with test-specific overrides
            var testConfig = new Dictionary<string, string>
            {
                // Enable Azure App Configuration - uses real endpoint from appsettings.json
                ["AzureAppConfiguration:Enabled"] = "true",
                
                // Enable Key Vault - uses real endpoint from appsettings.json  
                ["KeyVault:Enabled"] = "true",
                
                // ApplicationInsights connection string from appsettings.json is used
                
                // AzureAd settings from appsettings.json are used
                
                // Disable authentication for tests
                ["Authentication:DisableAuth"] = "true",
                
                // Use localhost database for actual data operations
                ["ConnectionStrings:HartonomousDb"] = "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;",
                
                // Enable managed identity for HART-DESKTOP (NT AUTHORITY\NETWORK SERVICE)
                // This will use DefaultAzureCredential which attempts:
                // 1. Environment variables
                // 2. Managed Identity (Arc-enabled for HART-DESKTOP)
                // 3. Azure CLI
                // 4. Visual Studio
                // Failures are graceful and don't crash the app
                ["Azure:UseManagedIdentity"] = "true",
                
                // Enable Swagger UI to test production Swagger paths
                ["Swagger:EnableUI"] = "true"
            };
            
            config.AddInMemoryCollection(testConfig!);
        });
        
        builder.ConfigureServices(services =>
        {
            // Override DatabaseOptions to ensure localhost SQL Server is used
            services.PostConfigure<DatabaseOptions>(options =>
            {
                options.HartonomousDb = "Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;";
            });
        });
        
        // Suppress Azure configuration errors for testing
        // The Azure config code will execute but fail gracefully
        builder.UseSetting("SuppressStatusMessages", "true");
    }
}
