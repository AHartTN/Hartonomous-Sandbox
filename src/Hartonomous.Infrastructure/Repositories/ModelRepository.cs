using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Hartonomous.Data;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IModelRepository"/>.
/// Inherits common CRUD operations from EfRepository base class.
/// </summary>
public class ModelRepository : EfRepository<Model, int>, IModelRepository
{
    public ModelRepository(HartonomousDbContext context, ILogger<ModelRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Models are identified by ModelId property.
    /// </summary>
    protected override Expression<Func<Model, int>> GetIdExpression() => model => model.ModelId;

    /// <summary>
    /// Include model layers and metadata for complete model queries.
    /// Uses AsSplitQuery to prevent cartesian explosion.
    /// </summary>
    protected override IQueryable<Model> IncludeRelatedEntities(IQueryable<Model> query)
    {
        return query
            .Include(m => m.Metadata)
            .Include(m => m.Layers)
            .AsSplitQuery();
    }

    // Domain-specific queries

    public async Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet)
            .FirstOrDefaultAsync(m => m.ModelName == modelName, cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(m => m.ModelType == modelType)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Query models by capability for ensemble orchestration.
    /// Returns models that support ANY of the specified tasks and ALL of the required modalities.
    /// Filters by parsing Model.Metadata.SupportedTasks/SupportedModalities JSON.
    /// </summary>
    public async Task<IEnumerable<Model>> GetModelsByCapabilityAsync(
        TaskType[] tasks,
        Modality requiredModalities = Modality.None,
        int minCount = 1,
        CancellationToken cancellationToken = default)
    {
        // Convert enum arrays to JSON strings for comparison
        var taskStrings = tasks.Select(t => t.ToJsonString()).ToArray();

        // Get all models with metadata
        var modelsWithMetadata = await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(m => m.Metadata != null &&
                       m.Metadata.SupportedTasks != null &&
                       m.Metadata.SupportedModalities != null)
            .ToListAsync(cancellationToken);

        // Filter in-memory by parsing JSON (client-side evaluation for complex JSON queries)
        var matchingModels = modelsWithMetadata
            .Where(model =>
            {
                // Parse supported tasks
                var supportedTasks = EnumExtensions.ParseTaskTypes(model.Metadata!.SupportedTasks);
                var supportsAnyTask = tasks.Any(t => (supportedTasks & t) != TaskType.None);

                if (!supportsAnyTask)
                    return false;

                // Parse supported modalities
                if (requiredModalities != Modality.None)
                {
                    var supportedModalities = EnumExtensions.ParseModalities(model.Metadata!.SupportedModalities);
                    var supportsAllModalities = (supportedModalities & requiredModalities) == requiredModalities;

                    if (!supportsAllModalities)
                        return false;
                }

                return true;
            })
            .OrderByDescending(m => m.Metadata!.SupportedTasks != null ? 1 : 0) // Prioritize models with complete metadata
            .ThenByDescending(m => m.ParameterCount ?? 0) // Larger models first (assumes higher capability)
            .Take(Math.Max(minCount, 100)) // Safety limit
            .ToList();

        Logger.LogInformation(
            "Found {Count} models matching capabilities: Tasks={Tasks}, Modalities={Modalities}",
            matchingModels.Count,
            string.Join(",", tasks.Select(t => t.ToJsonString())),
            requiredModalities.ToJsonArray());

        return matchingModels;
    }

    // Model layer management

    public async Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default)
    {
        layer.ModelId = modelId;
        Context.ModelLayers.Add(layer);
        await Context.SaveChangesAsync(cancellationToken);
        return layer;
    }

    /// <summary>
    /// Update model layer weights using efficient ExecuteUpdate.
    /// Converts SqlVector to geometry for SQL Server 2025 storage.
    /// </summary>
    public async Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default)
    {
        if (weights.IsNull)
        {
            return;
        }

        var layer = await Context.ModelLayers
            .FirstOrDefaultAsync(l => l.LayerId == layerId, cancellationToken)
            .ConfigureAwait(false);

        if (layer is null)
        {
            return;
        }

        var expectedDimension = layer.ParameterCount.HasValue && layer.ParameterCount.Value > 0
            ? (int)Math.Min(layer.ParameterCount.Value, VectorUtility.SqlVectorMaxDimensions)
            : Math.Min(weights.Length, VectorUtility.SqlVectorMaxDimensions);

        var dense = VectorUtility.Materialize(weights, expectedDimension);
        if (dense.Length == 0)
        {
            return;
        }

        layer.WeightsGeometry = GeometryConverter.ToLineString(dense, srid: 0);
        layer.ParameterCount = dense.Length;

        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await Context.ModelLayers
            .Where(l => l.ModelId == modelId)
            .OrderBy(l => l.LayerIdx)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Model>> GetByIdsAsync(IReadOnlyList<int> modelIds, CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(m => modelIds.Contains(m.ModelId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Model>> GetActiveModelsAsync(CancellationToken cancellationToken = default)
    {
        // For now, return all models. In future, could filter by IsActive flag if added to entity
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .ToListAsync(cancellationToken);
    }
}
