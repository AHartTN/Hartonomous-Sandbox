using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IModelLayerRepository"/>.
/// Inherits base CRUD from EfRepository, adds specialized geometry operations.
/// </summary>
public class ModelLayerRepository : EfRepository<ModelLayer, long>, IModelLayerRepository
{
    public ModelLayerRepository(HartonomousDbContext context, ILogger<ModelLayerRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// ModelLayers are identified by LayerId property.
    /// </summary>
    protected override Expression<Func<ModelLayer, long>> GetIdExpression() => l => l.LayerId;

    /// <summary>
    /// Include parent model for layer queries.
    /// </summary>
    protected override IQueryable<ModelLayer> IncludeRelatedEntities(IQueryable<ModelLayer> query)
    {
        return query.Include(l => l.Model);
    }

    // Domain-specific queries

    public async Task<IReadOnlyList<ModelLayer>> GetByModelAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.ModelId == modelId)
            .OrderBy(l => l.LayerIdx)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<ModelLayer> layers, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(layers, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    // Geometry-specific operations

    public async Task<IReadOnlyList<ModelLayer>> GetLayersByWeightRangeAsync(
        int modelId,
        double minValue,
        double maxValue,
        CancellationToken cancellationToken = default)
    {
        var layers = await DbSet
            .Where(l => l.ModelId == modelId && l.WeightsGeometry != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return layers
            .Where(l => l.WeightsGeometry!.Coordinates.Any(c => c.Y >= minValue && c.Y <= maxValue))
            .ToList();
    }

    public async Task<IReadOnlyList<ModelLayer>> GetLayersByImportanceAsync(
        int modelId,
        double minImportance,
        CancellationToken cancellationToken = default)
    {
        var layers = await DbSet
            .Where(l => l.ModelId == modelId && l.WeightsGeometry != null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return layers
            .Where(l => l.WeightsGeometry!.Coordinates.Any(c => c.Z >= minImportance))
            .ToList();
    }

    public float[] ExtractWeightsFromGeometry(LineString geometry)
    {
        var numPoints = geometry.NumPoints;
        var weights = new float[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            var point = geometry.GetPointN(i);
            weights[i] = (float)point.Y;
        }

        return weights;
    }

    public LineString CreateGeometryFromWeights(
        float[] weights,
        float[]? importanceScores = null,
        float[]? temporalMetadata = null)
    {
        var coordinates = new Coordinate[weights.Length];

        for (int i = 0; i < weights.Length; i++)
        {
            var z = importanceScores?[i] ?? 0.0;
            var m = temporalMetadata?[i] ?? 0.0;
            coordinates[i] = new CoordinateZM(i, weights[i], z, m);
        }

        return new LineString(coordinates);
    }
}
