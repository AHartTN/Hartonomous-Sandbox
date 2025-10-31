using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Repositories;

public class ModelLayerRepository : IModelLayerRepository
{
    private readonly HartonomousDbContext _context;

    public ModelLayerRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<ModelLayer?> GetByIdAsync(long layerId, CancellationToken cancellationToken = default)
    {
        return await _context.ModelLayers
            .Include(l => l.Model)
            .FirstOrDefaultAsync(l => l.LayerId == layerId, cancellationToken);
    }

    public async Task<IReadOnlyList<ModelLayer>> GetByModelAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await _context.ModelLayers
            .Where(l => l.ModelId == modelId)
            .OrderBy(l => l.LayerIdx)
            .ToListAsync(cancellationToken);
    }

    public async Task<ModelLayer> AddAsync(ModelLayer layer, CancellationToken cancellationToken = default)
    {
        _context.ModelLayers.Add(layer);
        await _context.SaveChangesAsync(cancellationToken);
        return layer;
    }

    public async Task BulkInsertAsync(IEnumerable<ModelLayer> layers, CancellationToken cancellationToken = default)
    {
        await _context.ModelLayers.AddRangeAsync(layers, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ModelLayer layer, CancellationToken cancellationToken = default)
    {
        _context.ModelLayers.Update(layer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long layerId, CancellationToken cancellationToken = default)
    {
        var layer = await GetByIdAsync(layerId, cancellationToken);
        if (layer != null)
        {
            _context.ModelLayers.Remove(layer);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ModelLayer>> GetLayersByWeightRangeAsync(
        int modelId,
        double minValue,
        double maxValue,
        CancellationToken cancellationToken = default)
    {
        var layers = await _context.ModelLayers
            .Where(l => l.ModelId == modelId && l.WeightsGeometry != null)
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
        var layers = await _context.ModelLayers
            .Where(l => l.ModelId == modelId && l.WeightsGeometry != null)
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
