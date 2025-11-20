using Hartonomous.Core.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hartonomous.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Supports both Development (Azure disabled) and Production (Azure enabled) testing.
/// </summary>
public class HartonomousWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _environment = "Development";
    private bool _disposed;

    /// <summary>
    /// Creates a test factory for Development environment (default).
    /// xUnit IClassFixture requires EXACTLY ONE parameterless constructor.
    /// </summary>
    public HartonomousWebApplicationFactory()
    {
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
            
            // CRITICAL: Force shutdown after 1 second to prevent freezing
            services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(1));
        });
        
        // Suppress startup/shutdown logs to avoid delays
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }

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
