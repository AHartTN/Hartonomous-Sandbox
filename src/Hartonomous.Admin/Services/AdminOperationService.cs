using System.IO;
using System.Linq;
using Hartonomous.Admin.Operations;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Admin.Services;

public sealed class AdminOperationService
{
    private readonly AdminOperationCoordinator _operationCoordinator;
    private readonly ModelIngestionOrchestrator _ingestionOrchestrator;
    private readonly IStudentModelService _studentModelService;
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<AdminOperationService> _logger;

    public AdminOperationService(
        AdminOperationCoordinator operationCoordinator,
        ModelIngestionOrchestrator ingestionOrchestrator,
        IStudentModelService studentModelService,
        IModelRepository modelRepository,
        ILogger<AdminOperationService> logger)
    {
        _operationCoordinator = operationCoordinator;
        _ingestionOrchestrator = ingestionOrchestrator;
        _studentModelService = studentModelService;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public Task<IReadOnlyCollection<AdminOperationStatus>> GetRecentOperationsAsync()
    {
        var operations = _operationCoordinator.GetRecent();
        return Task.FromResult((IReadOnlyCollection<AdminOperationStatus>)operations);
    }

    public async Task<IEnumerable<Model>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminOperationStatus> QueueModelIngestionAsync(
        string modelPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("Model path is required", nameof(modelPath));
        }

        var normalizedPath = Path.GetFullPath(modelPath);
        var description = $"Ingest model from {normalizedPath}";

        _logger.LogInformation("Queueing ingestion for {Path}", normalizedPath);

        return await _operationCoordinator.EnqueueAsync(
            operationType: "ModelIngestion",
            description,
            work: async token =>
            {
                var model = await _ingestionOrchestrator.IngestModelAsync(normalizedPath, token).ConfigureAwait(false);
                return AdminOperationOutcome.Succeeded($"Model {model.ModelName} (ID {model.ModelId}) ingested");
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminOperationStatus> QueueMultiModelIngestionAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path is required", nameof(directoryPath));
        }

        var normalizedPath = Path.GetFullPath(directoryPath);
        var description = $"Bulk ingest models from {normalizedPath}";

        _logger.LogInformation("Queueing bulk ingestion for {Directory}", normalizedPath);

        return await _operationCoordinator.EnqueueAsync(
            operationType: "ModelIngestion",
            description,
            work: async token =>
            {
                var models = await _ingestionOrchestrator.IngestMultiModelAsync(normalizedPath, token).ConfigureAwait(false);
                var count = models.Count();
                return AdminOperationOutcome.Succeeded($"Bulk ingestion completed ({count} models)");
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminOperationStatus> QueueStudentModelByRatioAsync(
        int parentModelId,
        double targetRatio,
        CancellationToken cancellationToken = default)
    {
        if (targetRatio <= 0 || targetRatio > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetRatio), "Target ratio must be between 0 and 1");
        }

        var description = $"Extract student model ({targetRatio:P0}) from model {parentModelId}";

        return await _operationCoordinator.EnqueueAsync(
            operationType: "ModelExtraction.Importance",
            description,
            work: async token =>
            {
                var student = await _studentModelService.ExtractByImportanceAsync(parentModelId, targetRatio, token).ConfigureAwait(false);
                return AdminOperationOutcome.Succeeded($"Created student model {student.ModelName} (ID {student.ModelId})");
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminOperationStatus> QueueStudentModelByLayersAsync(
        int parentModelId,
        int layerCount,
        CancellationToken cancellationToken = default)
    {
        if (layerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(layerCount), "Layer count must be positive");
        }

        var description = $"Extract {layerCount} layers from model {parentModelId}";

        return await _operationCoordinator.EnqueueAsync(
            operationType: "ModelExtraction.Layers",
            description,
            work: async token =>
            {
                var student = await _studentModelService.ExtractByLayersAsync(parentModelId, layerCount, token).ConfigureAwait(false);
                return AdminOperationOutcome.Succeeded($"Created student model {student.ModelName} (ID {student.ModelId})");
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminOperationStatus> QueueStudentModelBySpatialWindowAsync(
        int parentModelId,
        double minValue,
        double maxValue,
        CancellationToken cancellationToken = default)
    {
        if (minValue >= maxValue)
        {
            throw new ArgumentException("Minimum value must be less than maximum value");
        }

        var description = $"Extract spatial window ({minValue} - {maxValue}) from model {parentModelId}";

        return await _operationCoordinator.EnqueueAsync(
            operationType: "ModelExtraction.Spatial",
            description,
            work: async token =>
            {
                var student = await _studentModelService.ExtractBySpatialRegionAsync(parentModelId, minValue, maxValue, token).ConfigureAwait(false);
                return AdminOperationOutcome.Succeeded($"Created student model {student.ModelName} (ID {student.ModelId})");
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<AdminOperationStatus> QueueModelComparisonAsync(
        int modelAId,
        int modelBId,
        CancellationToken cancellationToken = default)
    {
        if (modelAId == modelBId)
        {
            throw new ArgumentException("Model comparison requires two distinct models");
        }

        var description = $"Compare models {modelAId} and {modelBId}";

        return await _operationCoordinator.EnqueueAsync(
            operationType: "ModelComparison",
            description,
            work: async token =>
            {
                var result = await _studentModelService.CompareModelsAsync(modelAId, modelBId, token).ConfigureAwait(false);
                var message = $"Compression ratio {result.CompressionRatio:F2}, shared layers {result.SharedLayers}";
                return AdminOperationOutcome.Succeeded(message);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
