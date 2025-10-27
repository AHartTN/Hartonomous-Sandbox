using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hartonomous.Infrastructure;
using System;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Production ingestion service for model and embedding data
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();
                
                // Get the ingestion service and run
                var ingestionService = host.Services.GetRequiredService<IngestionOrchestrator>();
                await ingestionService.RunAsync(args);
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddEnvironmentVariables()
                          .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    var connectionString = context.Configuration.GetConnectionString("HartonomousDb")
                        ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found");

                    var deduplicationThreshold = context.Configuration.GetValue<double>("Ingestion:DeduplicationThreshold", 0.95);

                    // Register Hartonomous infrastructure (EF Core, repositories, health checks)
                    services.AddHartonomousInfrastructure(context.Configuration);
                    
                    // Register ingestion services with connection string and configuration
                    services.AddScoped<IngestionOrchestrator>();
                    services.AddScoped(sp => new EmbeddingIngestionService(
                        connectionString,
                        embeddingModel: "production-model",
                        embeddingDimension: 768,
                        deduplicationThreshold: deduplicationThreshold));
                    services.AddScoped(sp => new AtomicStorageService(connectionString));
                    
                    // Placeholder for future model ingestion
                    // services.AddScoped<ModelIngestionService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }
}
