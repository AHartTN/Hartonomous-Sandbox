using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// High-level orchestrator for model ingestion.
/// Auto-detects format and delegates to appropriate reader.
/// Handles single-file, multi-file, and multi-model scenarios.
/// </summary>
public class ModelIngestionOrchestrator
{
    private readonly IModelDiscoveryService _discoveryService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModelIngestionOrchestrator> _logger;

    /// <summary>
    /// Initializes a new <see cref="ModelIngestionOrchestrator"/> with discovery, service resolution, and logging dependencies.
    /// </summary>
    /// <param name="discoveryService">Service used to detect model formats.</param>
    /// <param name="serviceProvider">Service provider for resolving format-specific readers.</param>
    /// <param name="logger">Logger used to capture orchestration diagnostics.</param>
    public ModelIngestionOrchestrator(
        IModelDiscoveryService discoveryService,
        IServiceProvider serviceProvider,
        ILogger<ModelIngestionOrchestrator> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ingest a model from any supported format.
    /// Auto-detects format and uses appropriate reader.
    /// </summary>
    /// <param name="modelPath">Path to model file or directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ingested model entity</returns>
    public async Task<Model> IngestModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting model ingestion from: {Path}", modelPath);

        // Detect format
        var formatInfo = await _discoveryService.DetectFormatAsync(modelPath, cancellationToken);
        _logger.LogInformation("Detected format: {Format} (confidence: {Confidence:P0})",
            formatInfo.Format, formatInfo.Confidence);

        if (formatInfo.Confidence < 0.5)
        {
            throw new NotSupportedException(
                $"Unable to reliably detect model format at: {modelPath} (confidence: {formatInfo.Confidence:P0})");
        }

        // Get appropriate reader and ingest
        Model model = formatInfo.Format switch
        {
            "ONNX" => await IngestWithReaderAsync<OnnxMetadata>(modelPath, cancellationToken),
            "PyTorch" => await IngestWithReaderAsync<PyTorchMetadata>(modelPath, cancellationToken),
            "Safetensors" => await IngestWithReaderAsync<SafetensorsMetadata>(modelPath, cancellationToken),
            "GGUF" => await IngestWithReaderAsync<GGUFMetadata>(modelPath, cancellationToken),
            _ => throw new NotSupportedException($"Model format '{formatInfo.Format}' is not yet supported")
        };

        _logger.LogInformation("Model ingestion complete: {ModelName} (ID: {ModelId}, Type: {Type})",
            model.ModelName, model.ModelId, model.ModelType);

        return model;
    }

    /// <summary>
    /// Ingest multiple related models (e.g., Stable Diffusion 3.5 components).
    /// Returns one model per component (text_encoder, unet, vae, etc.)
    /// </summary>
    /// <param name="modelDirectory">Directory containing multiple model files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of ingested models</returns>
    public async Task<IEnumerable<Model>> IngestMultiModelAsync(
        string modelDirectory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting multi-model ingestion from: {Path}", modelDirectory);

        if (!Directory.Exists(modelDirectory))
        {
            throw new DirectoryNotFoundException($"Model directory not found: {modelDirectory}");
        }

        var models = new List<Model>();

        // Find all model files in directory
        var modelFiles = Directory.GetFiles(modelDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext == ".onnx" || ext == ".safetensors" || ext == ".gguf" ||
                       ext == ".bin" || ext == ".pth" || ext == ".pt";
            })
            .ToList();

        if (!modelFiles.Any())
        {
            _logger.LogWarning("No model files found in directory: {Path}", modelDirectory);
            return models;
        }

        _logger.LogInformation("Found {Count} model files to ingest", modelFiles.Count);

        // Ingest each model file
        foreach (var file in modelFiles)
        {
            try
            {
                var model = await IngestModelAsync(file, cancellationToken);
                models.Add(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest model file: {File}", file);
                // Continue with remaining files
            }
        }

        _logger.LogInformation("Multi-model ingestion complete: {SuccessCount}/{TotalCount} models ingested",
            models.Count, modelFiles.Count);

        return models;
    }

    /// <summary>
    /// Validate that a model path contains a supported format.
    /// </summary>
    /// <param name="modelPath">Path to the model file or directory to validate.</param>
    /// <param name="cancellationToken">Token used to cancel the validation operation.</param>
    /// <returns><c>true</c> when the model format is recognized with sufficient confidence; otherwise <c>false</c>.</returns>
    public async Task<bool> ValidateModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var formatInfo = await _discoveryService.DetectFormatAsync(modelPath, cancellationToken);
            return formatInfo.Confidence >= 0.5;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Model validation failed for: {Path}", modelPath);
            return false;
        }
    }

    /// <summary>
    /// Get information about a model without ingesting it.
    /// </summary>
    /// <param name="modelPath">Path to the model file or directory.</param>
    /// <param name="cancellationToken">Token that cancels discovery work.</param>
    /// <returns>Format information describing the detected model.</returns>
    public async Task<ModelFormatInfo> GetModelInfoAsync(
        string modelPath,
        CancellationToken cancellationToken = default)
    {
        return await _discoveryService.DetectFormatAsync(modelPath, cancellationToken);
    }

    private async Task<Model> IngestWithReaderAsync<TMetadata>(
        string modelPath,
        CancellationToken cancellationToken) where TMetadata : class
    {
        // Try to get reader from DI
        var reader = _serviceProvider.GetService(typeof(IModelFormatReader<TMetadata>)) as IModelFormatReader<TMetadata>;

        if (reader == null)
        {
            throw new InvalidOperationException(
                $"No reader registered for metadata type: {typeof(TMetadata).Name}. " +
                $"Ensure the reader is registered in DI with AddScoped<IModelFormatReader<{typeof(TMetadata).Name}>, ReaderImplementation>()");
        }

        _logger.LogDebug("Using reader: {ReaderType}", reader.GetType().Name);

        // Validate format
        var isValid = await reader.ValidateFormatAsync(modelPath, cancellationToken);
        if (!isValid)
        {
            throw new InvalidDataException($"Model validation failed using {reader.GetType().Name} for: {modelPath}");
        }

        // Read and ingest
        return await reader.ReadAsync(modelPath, cancellationToken);
    }
}
