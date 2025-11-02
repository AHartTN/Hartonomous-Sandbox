using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Coordinates ingestion requests by delegating discovery to the orchestrator and persisting model artifacts.
/// </summary>
public class ModelIngestionProcessor
{
    private readonly ILogger<ModelIngestionProcessor> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly IModelLayerRepository _layerRepository;
    private readonly ModelIngestionOrchestrator _orchestrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelIngestionProcessor"/> class.
    /// </summary>
    /// <param name="logger">Logger for operational diagnostics.</param>
    /// <param name="modelRepository">Repository used to persist model definitions.</param>
    /// <param name="layerRepository">Repository used to store model layers.</param>
    /// <param name="orchestrator">Orchestrator responsible for reading model artifacts.</param>
    public ModelIngestionProcessor(
        ILogger<ModelIngestionProcessor> logger,
        IModelRepository modelRepository,
        IModelLayerRepository layerRepository,
        ModelIngestionOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <summary>
    /// Processes an ingestion request by reading the source model and persisting the resulting metadata and layers.
    /// </summary>
    /// <param name="request">Incoming ingestion request containing model location and overrides.</param>
    /// <param name="cancellationToken">Token that cancels ingestion work.</param>
    /// <returns>A result describing the success state and persisted model information.</returns>
    public async Task<ModelIngestionResult> ProcessAsync(ModelIngestionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await _orchestrator.IngestModelAsync(request.ModelPath, cancellationToken);

            if (!string.IsNullOrEmpty(request.CustomName))
            {
                model.ModelName = request.CustomName;
            }

            var savedModel = await _modelRepository.AddAsync(model, cancellationToken);

            if (model.Layers != null && model.Layers.Any())
            {
                foreach (var layer in model.Layers)
                {
                    layer.ModelId = savedModel.ModelId;
                }
                await _layerRepository.BulkInsertAsync(model.Layers, cancellationToken);
            }

            return new ModelIngestionResult
            {
                Success = true,
                ModelId = savedModel.ModelId,
                Model = savedModel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model ingestion processing failed");
            return new ModelIngestionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
