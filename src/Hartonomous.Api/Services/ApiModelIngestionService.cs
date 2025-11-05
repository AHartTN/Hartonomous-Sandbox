using System.Collections.Generic;
using System.IO;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Api.Services;

/// <summary>
/// Lightweight API-facing implementation of <see cref="IModelIngestionService"/> built around existing infrastructure components.
/// </summary>
public sealed class ApiModelIngestionService : IModelIngestionService
{
    private readonly ILogger<ApiModelIngestionService> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly ModelIngestionProcessor _processor;
    private readonly IIngestionStatisticsService _statisticsService;

    public ApiModelIngestionService(
        ILogger<ApiModelIngestionService> logger,
        IModelRepository modelRepository,
        ModelIngestionProcessor processor,
        IIngestionStatisticsService statisticsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    public string ServiceName => "ApiModelIngestionService";

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _modelRepository.GetCountAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Model ingestion service health check failed.");
            return false;
        }
    }

    public async Task<int> IngestAsync(string modelPath, string? modelName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("Model path cannot be empty.", nameof(modelPath));
        }

        if (!File.Exists(modelPath) && !Directory.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model path not found: {modelPath}", modelPath);
        }

        var request = new ModelIngestionRequest
        {
            ModelPath = modelPath,
            CustomName = modelName
        };

        var result = await _processor.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Model ingestion failed.");
        }

        var model = await _modelRepository.GetByIdAsync(result.ModelId, cancellationToken).ConfigureAwait(false);
        if (model is null)
        {
            throw new InvalidOperationException($"Model {result.ModelId} could not be reloaded after ingestion.");
        }

        _logger.LogInformation(
            "Model {ModelId} ingested successfully ({ModelName}, architecture={Architecture}).",
            model.ModelId,
            model.ModelName,
            model.Architecture ?? model.ModelType);

        return model.ModelId;
    }

    public async Task<int[]> IngestDirectoryAsync(string directoryPath, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
        var modelIds = new List<int>(files.Length);

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension is not (".safetensors" or ".onnx" or ".pt" or ".pth" or ".gguf"))
            {
                continue;
            }

            try
            {
                var id = await IngestAsync(file, null, cancellationToken).ConfigureAwait(false);
                modelIds.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping file during batch ingestion: {File}", file);
            }
        }

        return modelIds.ToArray();
    }

    public Task<IngestionStats> GetStatsAsync(CancellationToken cancellationToken = default)
        => _statisticsService.GetStatsAsync(cancellationToken);
}
