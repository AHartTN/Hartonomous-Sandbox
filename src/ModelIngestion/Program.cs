using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
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
                    
                    // Register HttpClient for ModelDownloader
                    services.AddHttpClient("ModelDownloader", client =>
                    {
                        client.Timeout = TimeSpan.FromMinutes(30); // Large model downloads
                        client.DefaultRequestHeaders.Add("User-Agent", "Hartonomous/1.0");
                    });
                    
                    // Register ingestion services with DI
                    services.AddScoped<IngestionOrchestrator>();
                    services.AddScoped<ModelIngestionService>();
                    services.AddScoped<Hartonomous.Infrastructure.Services.ModelDownloader>();
                    
                    // Register new focused services
                    services.AddScoped<ModelDownloadService>();
                    services.AddScoped<EmbeddingTestService>();
                    services.AddScoped<QueryService>();
                    services.AddScoped<AtomicStorageTestService>();
                    
                    // Register model format readers
                    services.AddScoped<Hartonomous.Core.Interfaces.IModelFormatReader<Hartonomous.Core.Interfaces.OnnxMetadata>, ModelFormats.OnnxModelReader>();
                    services.AddScoped<Hartonomous.Core.Interfaces.IModelFormatReader<Hartonomous.Core.Interfaces.SafetensorsMetadata>, ModelFormats.SafetensorsModelReader>();
                    services.AddScoped<ModelFormats.ModelReaderFactory>();
                    
                    // Register model format readers
                    services.AddScoped<Hartonomous.Core.Interfaces.IModelFormatReader<Hartonomous.Core.Interfaces.OnnxMetadata>, ModelFormats.OnnxModelReader>();
                    services.AddScoped<Hartonomous.Core.Interfaces.IModelFormatReader<Hartonomous.Core.Interfaces.SafetensorsMetadata>, ModelFormats.SafetensorsModelReader>();
                    services.AddScoped<ModelFormats.ModelReaderFactory>();
                    
                    // Register EmbeddingIngestionService as IEmbeddingIngestionService
                    // Service requires IEmbeddingRepository (from infrastructure), ILogger, and configuration
                    services.AddScoped<Hartonomous.Core.Interfaces.IEmbeddingIngestionService>(sp =>
                    {
                        var embeddingRepo = sp.GetRequiredService<IEmbeddingRepository>();
                        var logger = sp.GetRequiredService<ILogger<EmbeddingIngestionService>>();
                        var config = sp.GetRequiredService<IConfiguration>();
                        
                        return new EmbeddingIngestionService(
                            embeddingRepo,
                            logger,
                            config);
                    });
                    
                    // Also register concrete type for backward compatibility
                    services.AddScoped(sp => 
                        (EmbeddingIngestionService)sp.GetRequiredService<Hartonomous.Core.Interfaces.IEmbeddingIngestionService>());
                    
                    // Register AtomicStorageService as IAtomicStorageService
                    services.AddScoped<Hartonomous.Core.Interfaces.IAtomicStorageService>(sp =>
                    {
                        var pixelRepo = sp.GetRequiredService<IAtomicPixelRepository>();
                        var audioSampleRepo = sp.GetRequiredService<IAtomicAudioSampleRepository>();
                        var textTokenRepo = sp.GetRequiredService<IAtomicTextTokenRepository>();
                        var logger = sp.GetRequiredService<ILogger<AtomicStorageService>>();
                        return new AtomicStorageService(logger, pixelRepo, audioSampleRepo, textTokenRepo);
                    });
                    
                    // Also register concrete type for backward compatibility
                    services.AddScoped(sp =>
                        (AtomicStorageService)sp.GetRequiredService<Hartonomous.Core.Interfaces.IAtomicStorageService>());
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
