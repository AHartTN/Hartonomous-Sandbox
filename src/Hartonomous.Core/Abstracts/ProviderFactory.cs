using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Factory for creating embedders based on configuration.
/// Allows swapping between different embedding providers.
/// </summary>
public class EmbedderFactory : BaseFactory<string, IEmbedder>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public EmbedderFactory(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;

        // Register default embedders
        RegisterDefaultEmbedders();
    }

    private void RegisterDefaultEmbedders()
    {
        // Text embedders
        Register("DatabaseTextEmbedder", () => new DatabaseTextEmbedder(
            _loggerFactory.CreateLogger<DatabaseTextEmbedder>(),
            _configuration));

        // Image embedders
        Register("DatabaseImageEmbedder", () => new DatabaseImageEmbedder(
            _loggerFactory.CreateLogger<DatabaseImageEmbedder>(),
            _configuration));

        // Audio embedders
        Register("DatabaseAudioEmbedder", () => new DatabaseAudioEmbedder(
            _loggerFactory.CreateLogger<DatabaseAudioEmbedder>(),
            _configuration));

        // Video embedders
        Register("DatabaseVideoEmbedder", () => new DatabaseVideoEmbedder(
            _loggerFactory.CreateLogger<DatabaseVideoEmbedder>(),
            _configuration));
    }

    /// <summary>
    /// Create an embedder for the specified modality.
    /// </summary>
    /// <param name="modality">The modality (text, image, audio, video)</param>
    /// <param name="provider">Optional specific provider name</param>
    /// <returns>The embedder instance</returns>
    public IEmbedder CreateForModality(string modality, string? provider = null)
    {
        var providerName = provider ?? GetDefaultProviderForModality(modality);
        return Create(providerName);
    }

    private string GetDefaultProviderForModality(string modality)
    {
        return modality.ToLowerInvariant() switch
        {
            "text" => "DatabaseTextEmbedder",
            "image" => "DatabaseImageEmbedder",
            "audio" => "DatabaseAudioEmbedder",
            "video" => "DatabaseVideoEmbedder",
            _ => throw new ArgumentException($"Unknown modality: {modality}", nameof(modality))
        };
    }
}

/// <summary>
/// Factory for creating storage providers based on configuration.
/// </summary>
public class StorageProviderFactory : BaseFactory<string, object>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public StorageProviderFactory(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;

        // Register default storage providers
        RegisterDefaultProviders();
    }

    private void RegisterDefaultProviders()
    {
        // Embedding storage
        Register("SqlServerEmbeddingStorage", () => new SqlServerEmbeddingStorageProvider(
            _loggerFactory.CreateLogger<SqlServerEmbeddingStorageProvider>(),
            _configuration));

        // Model storage
        Register("SqlServerModelStorage", () => new SqlServerModelStorageProvider(
            _loggerFactory.CreateLogger<SqlServerModelStorageProvider>(),
            _configuration));

        // Atomic storage
        Register("SqlServerAtomicStorage", () => new SqlServerAtomicStorageProvider(
            _loggerFactory.CreateLogger<SqlServerAtomicStorageProvider>(),
            _configuration));
    }

    /// <summary>
    /// Create an embedding storage provider.
    /// </summary>
    /// <param name="provider">Optional specific provider name</param>
    /// <returns>The storage provider instance</returns>
    public IEmbeddingStorageProvider CreateEmbeddingStorage(string? provider = null)
    {
        var providerName = provider ?? "SqlServerEmbeddingStorage";
        return (IEmbeddingStorageProvider)Create(providerName);
    }

    /// <summary>
    /// Create a model storage provider.
    /// </summary>
    /// <param name="provider">Optional specific provider name</param>
    /// <returns>The storage provider instance</returns>
    public IModelStorageProvider CreateModelStorage(string? provider = null)
    {
        var providerName = provider ?? "SqlServerModelStorage";
        return (IModelStorageProvider)Create(providerName);
    }

    /// <summary>
    /// Create an atomic storage provider.
    /// </summary>
    /// <param name="provider">Optional specific provider name</param>
    /// <returns>The storage provider instance</returns>
    public IAtomicStorageProvider CreateAtomicStorage(string? provider = null)
    {
        var providerName = provider ?? "SqlServerAtomicStorage";
        return (IAtomicStorageProvider)Create(providerName);
    }
}

/// <summary>
/// Factory for creating service providers based on configuration.
/// </summary>
public class ServiceProviderFactory : BaseFactory<string, object>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public ServiceProviderFactory(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;

        // Register default service providers
        RegisterDefaultProviders();
    }

    private void RegisterDefaultProviders()
    {
        // Message publishers
        Register("EventHubPublisher", () => new EventHubMessagePublisher(
            _loggerFactory.CreateLogger<EventHubMessagePublisher>(),
            _configuration));

        // Model downloaders
        Register("HuggingFaceDownloader", () => new HuggingFaceModelDownloader(
            _loggerFactory.CreateLogger<HuggingFaceModelDownloader>(),
            _configuration));

        Register("OllamaDownloader", () => new OllamaModelDownloader(
            _loggerFactory.CreateLogger<OllamaModelDownloader>(),
            _configuration));

        // Model format readers
        Register("SafetensorsReader", () => new SafetensorsModelReader(
            _loggerFactory.CreateLogger<SafetensorsModelReader>()));

        Register("OnnxReader", () => new OnnxModelReader(
            _loggerFactory.CreateLogger<OnnxModelReader>()));

        Register("PyTorchReader", () => new PyTorchModelReader(
            _loggerFactory.CreateLogger<PyTorchModelReader>()));

        Register("GGUFReader", () => new GGUFModelReader(
            _loggerFactory.CreateLogger<GGUFModelReader>()));
    }

    /// <summary>
    /// Create a message publisher.
    /// </summary>
    /// <param name="provider">Optional specific provider name</param>
    /// <returns>The publisher instance</returns>
    public IMessagePublisher CreateMessagePublisher(string? provider = null)
    {
        var providerName = provider ?? "EventHubPublisher";
        return (IMessagePublisher)Create(providerName);
    }

    /// <summary>
    /// Create a model downloader.
    /// </summary>
    /// <param name="provider">Optional specific provider name</param>
    /// <returns>The downloader instance</returns>
    public IModelDownloader CreateModelDownloader(string? provider = null)
    {
        var providerName = provider ?? "HuggingFaceDownloader";
        return (IModelDownloader)Create(providerName);
    }

    /// <summary>
    /// Create a model format reader.
    /// </summary>
    /// <param name="format">The format (safetensors, onnx, pytorch, gguf)</param>
    /// <returns>The reader instance</returns>
    public IModelFormatReader CreateModelReader(string format)
    {
        var providerName = format.ToLowerInvariant() switch
        {
            "safetensors" => "SafetensorsReader",
            "onnx" => "OnnxReader",
            "pytorch" => "PyTorchReader",
            "gguf" => "GGUFReader",
            _ => throw new ArgumentException($"Unknown format: {format}", nameof(format))
        };
        return (IModelFormatReader)Create(providerName);
    }
}