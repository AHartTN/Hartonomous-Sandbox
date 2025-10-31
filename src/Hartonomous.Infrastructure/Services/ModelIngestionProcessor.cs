using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

public class ModelIngestionProcessor
{
    private readonly ILogger<ModelIngestionProcessor> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly IModelLayerRepository _layerRepository;
    private readonly ModelIngestionOrchestrator _orchestrator;

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
