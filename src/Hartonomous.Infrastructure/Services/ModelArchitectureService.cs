using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Service for managing ModelArchitecture catalog and routing to dimension-specific tables.
/// </summary>
public class ModelArchitectureService : IModelArchitectureService
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<ModelArchitectureService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ModelArchitectureService(
        HartonomousDbContext context,
        ILogger<ModelArchitectureService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<ModelArchitecture?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await _context.ModelArchitectures
            .FirstOrDefaultAsync(m => m.ModelId == modelId && m.IsActive, cancellationToken);
    }

    public async Task<ModelArchitecture?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default)
    {
        return await _context.ModelArchitectures
            .FirstOrDefaultAsync(m => m.ModelName == modelName && m.IsActive, cancellationToken);
    }

    public async Task<ModelArchitecture> RegisterModelAsync(
        string modelName,
        string modelType,
        int embeddingDimension,
        int layerCount,
        long? parameterCount = null,
        string? architectureConfig = null,
        CancellationToken cancellationToken = default)
    {
        // Validate dimension
        if (!ModelArchitecture.IsDimensionSupported(embeddingDimension))
        {
            throw new ArgumentException(
                $"Unsupported embedding dimension: {embeddingDimension}. Supported: 768, 1536, 1998, 3996",
                nameof(embeddingDimension));
        }

        // Check if model already exists
        var existing = await GetByNameAsync(modelName, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Model '{modelName}' already exists with ID {existing.ModelId}");
        }

        var weightsTableName = ModelArchitecture.GetWeightsTableName(embeddingDimension);

        var model = new ModelArchitecture
        {
            ModelName = modelName,
            ModelType = modelType,
            EmbeddingDimension = embeddingDimension,
            WeightsTableName = weightsTableName,
            LayerCount = layerCount,
            ParameterCount = parameterCount,
            ArchitectureConfig = architectureConfig,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.ModelArchitectures.Add(model);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Registered model {ModelName} (ID: {ModelId}) with dimension {Dimension}, routing to {Table}",
            model.ModelName, model.ModelId, model.EmbeddingDimension, model.WeightsTableName);

        return model;
    }

    public async Task<IReadOnlyList<ModelArchitecture>> GetByDimensionAsync(
        int dimension,
        CancellationToken cancellationToken = default)
    {
        return await _context.ModelArchitectures
            .Where(m => m.EmbeddingDimension == dimension && m.IsActive)
            .OrderBy(m => m.ModelName)
            .ToListAsync(cancellationToken);
    }

    public IWeightRepository<WeightBase> GetWeightRepositoryForModel(int modelId)
    {
        var model = _context.ModelArchitectures
            .FirstOrDefault(m => m.ModelId == modelId && m.IsActive);

        if (model == null)
        {
            throw new InvalidOperationException($"Model with ID {modelId} not found or inactive");
        }

        // Route to dimension-specific repository
        return model.EmbeddingDimension switch
        {
            768 => (IWeightRepository<WeightBase>)_serviceProvider.GetService(typeof(IWeightRepository<Weight768>))!,
            1536 => (IWeightRepository<WeightBase>)_serviceProvider.GetService(typeof(IWeightRepository<Weight1536>))!,
            1998 => (IWeightRepository<WeightBase>)_serviceProvider.GetService(typeof(IWeightRepository<Weight1998>))!,
            3996 => (IWeightRepository<WeightBase>)_serviceProvider.GetService(typeof(IWeightRepository<Weight3996>))!,
            _ => throw new NotSupportedException($"Unsupported dimension: {model.EmbeddingDimension}")
        };
    }

    public async Task UpdateAsync(ModelArchitecture model, CancellationToken cancellationToken = default)
    {
        model.LastModifiedDate = DateTime.UtcNow;
        _context.ModelArchitectures.Update(model);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(int modelId, CancellationToken cancellationToken = default)
    {
        var model = await GetByIdAsync(modelId, cancellationToken);
        if (model != null)
        {
            model.IsActive = false;
            model.LastModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deactivated model {ModelName} (ID: {ModelId})", model.ModelName, modelId);
        }
    }
}
