using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Generic factory for creating model format readers.
/// Automatically detects format based on file extensions.
/// </summary>
public class ModelReaderFactory : BaseFactory<string, IModelFormatReader>
{
    private readonly ILogger<ModelReaderFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ModelReaderFactory(
        ILogger<ModelReaderFactory> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Register built-in readers
        RegisterReaders();
    }

    /// <summary>
    /// Create a reader for the specified file path.
    /// Automatically detects format based on file extension.
    /// </summary>
    /// <param name="filePath">The file path to create a reader for</param>
    /// <returns>The appropriate reader instance</returns>
    public IModelFormatReader CreateForFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            throw new ArgumentException("File must have an extension", nameof(filePath));

        return Create(extension);
    }

    /// <summary>
    /// Get all supported file extensions.
    /// </summary>
    /// <returns>Collection of supported extensions</returns>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return GetSupportedKeys();
    }

    /// <summary>
    /// Check if a file extension is supported.
    /// </summary>
    /// <param name="extension">The file extension (with or without dot)</param>
    /// <returns>True if supported</returns>
    public bool IsExtensionSupported(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;

        if (!extension.StartsWith('.'))
            extension = '.' + extension;

        return CanCreate(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Register all built-in model readers.
    /// </summary>
    private void RegisterReaders()
    {
        // Safetensors reader
        Register(".safetensors", () => new ModelIngestion.ModelFormats.SafetensorsModelReader(
            _serviceProvider.GetRequiredService<ILogger<ModelIngestion.ModelFormats.SafetensorsModelReader>>()));

        // ONNX reader
        Register(".onnx", () => new ModelIngestion.ModelFormats.OnnxModelReader(
            _serviceProvider.GetRequiredService<ILogger<ModelIngestion.ModelFormats.OnnxModelReader>>()));

        // PyTorch readers
        Register(".pt", () => new ModelIngestion.ModelFormats.PyTorchModelReader(
            _serviceProvider.GetRequiredService<ILogger<ModelIngestion.ModelFormats.PyTorchModelReader>>()));
        Register(".pth", () => new ModelIngestion.ModelFormats.PyTorchModelReader(
            _serviceProvider.GetRequiredService<ILogger<ModelIngestion.ModelFormats.PyTorchModelReader>>()));

        // TODO: Add GGUF reader when implemented
        // Register(".gguf", () => new GGUFModelReader(
        //     _serviceProvider.GetRequiredService<ILogger<GGUFModelReader>>()));

        _logger.LogInformation("Registered {Count} model readers", Registrations.Count);
    }
}

/// <summary>
/// Generic processor for model ingestion workflows.
/// Orchestrates the entire model ingestion process.
/// </summary>
public class ModelIngestionProcessor : BaseProcessor<ModelIngestionRequest, ModelIngestionResult>
{
    private readonly IModelRepository _modelRepository;
    private readonly ModelReaderFactory _readerFactory;

    public ModelIngestionProcessor(
        ILogger<ModelIngestionProcessor> logger,
        IModelRepository modelRepository,
        ModelReaderFactory readerFactory)
        : base(logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
    }

    public override string ServiceName => "ModelIngestionProcessor";

    public override async Task<ModelIngestionResult> ProcessAsync(
        ModelIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        Logger.LogInformation("Processing model ingestion: {Path}", request.ModelPath);

        try
        {
            // Create appropriate reader
            var reader = _readerFactory.CreateForFile(request.ModelPath);

            // Validate format
            var isValid = await reader.ValidateFormatAsync(request.ModelPath, cancellationToken);
            if (!isValid)
            {
                return new ModelIngestionResult
                {
                    Success = false,
                    ErrorMessage = $"Invalid format for {reader.FormatName}"
                };
            }

            // Read model
            var model = await reader.ReadAsync(request.ModelPath, cancellationToken);

            // Apply custom name if provided
            if (!string.IsNullOrEmpty(request.CustomName))
            {
                model.ModelName = request.CustomName;
            }

            // Save to repository
            var modelId = await _modelRepository.AddAsync(model, cancellationToken);

            Logger.LogInformation("✓ Model ingested successfully: ID={Id}", modelId);

            return new ModelIngestionResult
            {
                Success = true,
                ModelId = modelId,
                Model = model
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process model: {Path}", request.ModelPath);
            return new ModelIngestionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override bool CanProcess(ModelIngestionRequest input)
    {
        return input != null &&
               !string.IsNullOrEmpty(input.ModelPath) &&
               _readerFactory.IsExtensionSupported(Path.GetExtension(input.ModelPath));
    }
}

/// <summary>
/// Request object for model ingestion.
/// </summary>
public class ModelIngestionRequest
{
    /// <summary>
    /// Gets or sets the path to the model file.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional custom name for the model.
    /// </summary>
    public string? CustomName { get; set; }

    /// <summary>
    /// Gets or sets additional metadata to attach.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Result object for model ingestion.
/// </summary>
public class ModelIngestionResult
{
    /// <summary>
    /// Gets or sets whether the ingestion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the model ID if successful.
    /// </summary>
    public int ModelId { get; set; }

    /// <summary>
    /// Gets or sets the ingested model if successful.
    /// </summary>
    public Model? Model { get; set; }

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; set; }
}