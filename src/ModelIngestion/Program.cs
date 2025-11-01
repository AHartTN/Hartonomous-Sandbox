using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Hartonomous.Infrastructure;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Core.Interfaces;
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
                    // Register Hartonomous infrastructure (EF Core, repositories, health checks)
                    services.AddHartonomousInfrastructure(context.Configuration);

                    // Register HttpClient for ModelDownloader
                    services.AddHttpClient("ModelDownloader", client =>
                    {
                        client.Timeout = TimeSpan.FromMinutes(30); // Large model downloads
                        client.DefaultRequestHeaders.Add("User-Agent", "Hartonomous/1.0");
                    });

                    // Register ingestion services with DI
                    services.AddScoped<IngestionOrchestrator>();
                    services.AddScoped<ModelIngestionService>();
                    services.AddScoped<ModelDownloader>();

                    // Register new focused services
                    services.AddScoped<EmbeddingTestService>();
                    services.AddScoped<QueryService>();
                    services.AddScoped<AtomicStorageTestService>();

                    // Register model format readers
                    services.AddScoped<IModelFormatReader<OnnxMetadata>, ModelFormats.OnnxModelReader>();
                    services.AddScoped<IModelFormatReader<SafetensorsMetadata>, ModelFormats.SafetensorsModelReader>();
                    services.AddScoped<ModelFormats.ModelReaderFactory>();

                    services.AddScoped<EmbeddingIngestionService>();
                    services.AddScoped<IEmbeddingIngestionService>(sp => sp.GetRequiredService<EmbeddingIngestionService>());

                    services.AddScoped<AtomicStorageService>();
                    services.AddScoped<IAtomicStorageService>(sp => sp.GetRequiredService<AtomicStorageService>());

                    var appInsightsSection = context.Configuration.GetSection("ApplicationInsights");
                    var appInsightsConnectionString = appInsightsSection.GetValue<string>("ConnectionString");

                    if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
                    {
                        services.AddApplicationInsightsTelemetryWorkerService(options =>
                        {
                            options.ConnectionString = appInsightsConnectionString;
                            options.EnableAdaptiveSampling = appInsightsSection.GetValue<bool?>("EnableAdaptiveSampling") ?? true;

                            options.EnablePerformanceCounterCollectionModule = appInsightsSection.GetValue<bool?>("EnablePerformanceCounters") ?? false;
                        });
                    }
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];

                    if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
                    {
                        logging.AddApplicationInsights(
                            configureTelemetryConfiguration: config => config.ConnectionString = appInsightsConnectionString,
                            configureApplicationInsightsLoggerOptions: options =>
                            {
                                options.TrackExceptionsAsExceptionTelemetry = true;
                            });
                    }
                    logging.SetMinimumLevel(LogLevel.Information);
                });
    }
}
