using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Hartonomous.Data;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    /// Include model layers for complete model queries.
    /// Uses AsSplitQuery to prevent cartesian explosion.
    /// </summary>
    protected override IQueryable<Model> IncludeRelatedEntities(IQueryable<Model> query)
    {
        return query
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
}
