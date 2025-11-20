using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.Infrastructure.FileType;
using Hartonomous.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Configurations;

/// <summary>
/// Dependency injection registration for data ingestion services.
/// </summary>
public static class IngestionServiceRegistration
{
    /// <summary>
    /// Register all ingestion services: file type detection, atomizers, bulk insert.
    /// </summary>
    public static IServiceCollection AddIngestionServices(
        this IServiceCollection services)
    {
        // File type detector
        services.AddSingleton<IFileTypeDetector, FileTypeDetector>();

        // Byte[] atomizers (file content)
        services.AddTransient<IAtomizer<byte[]>, TextAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, ImageAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, ArchiveAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, VideoFileAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, AudioFileAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, DocumentAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, CodeFileAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, ModelFileAtomizer>();

        // String atomizers (URLs and model identifiers)
        services.AddHttpClient(); // For WebFetchAtomizer, OllamaModelAtomizer, HuggingFaceModelAtomizer
        services.AddTransient<IAtomizer<string>>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var detector = sp.GetRequiredService<IFileTypeDetector>();
            var byteAtomizers = sp.GetServices<IAtomizer<byte[]>>();
            return new WebFetchAtomizer(httpClient, detector, byteAtomizers);
        });

        // AI model platform atomizers
        services.AddTransient<IAtomizer<string>>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama");
            return new OllamaModelAtomizer(httpClient);
        });

        services.AddTransient<IAtomizer<string>>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("HuggingFace");
            var byteAtomizers = sp.GetServices<IAtomizer<byte[]>>();
            return new HuggingFaceModelAtomizer(httpClient, byteAtomizers);
        });

        // Specialized atomizers (database, git)
        services.AddTransient<IAtomizer<DatabaseConnectionInfo>, DatabaseAtomizer>();
        services.AddTransient<IAtomizer<GitRepositoryInfo>>(sp =>
        {
            var detector = sp.GetRequiredService<IFileTypeDetector>();
            var byteAtomizers = sp.GetServices<IAtomizer<byte[]>>();
            return new GitRepositoryAtomizer(detector, byteAtomizers);
        });

        // Bulk insert service (registered as interface)
        services.AddScoped<IAtomBulkInsertService>(sp => 
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<AtomBulkInsertService>>();
            return new AtomBulkInsertService(databaseOptions.HartonomousDb, logger);
        });

        // Streaming atomizers (for real-time telemetry, video, audio)
        services.AddTransient<Hartonomous.Infrastructure.Atomizers.TelemetryStreamAtomizer>();
        services.AddTransient<Hartonomous.Infrastructure.Atomizers.VideoStreamAtomizer>();
        services.AddTransient<Hartonomous.Infrastructure.Atomizers.AudioStreamAtomizer>();

        // Streaming ingestion service (manages real-time sessions)
        services.AddScoped<Hartonomous.Infrastructure.Services.StreamingIngestionService>();

        return services;
    }
}
