using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Repositories;

public class ModelRepository : IModelRepository
{
    private readonly HartonomousDbContext _context;

    public ModelRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await _context.Models
            .Include(m => m.Layers)
            .FirstOrDefaultAsync(m => m.ModelId == modelId, cancellationToken);
    }

    public async Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default)
    {
        return await _context.Models
            .Include(m => m.Layers)
            .FirstOrDefaultAsync(m => m.ModelName == modelName, cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Models
            .Include(m => m.Layers)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default)
    {
        return await _context.Models
            .Where(m => m.ModelType == modelType)
            .Include(m => m.Layers)
            .ToListAsync(cancellationToken);
    }

    public async Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default)
    {
        _context.Models.Add(model);
        await _context.SaveChangesAsync(cancellationToken);
        return model;
    }

    public async Task UpdateAsync(Model model, CancellationToken cancellationToken = default)
    {
        _context.Models.Update(model);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int modelId, CancellationToken cancellationToken = default)
    {
        var model = await GetByIdAsync(modelId, cancellationToken);
        if (model != null)
        {
            _context.Models.Remove(model);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await _context.Models.AnyAsync(m => m.ModelId == modelId, cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Models.CountAsync(cancellationToken);
    }

    public async Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default)
    {
        layer.ModelId = modelId;
        _context.ModelLayers.Add(layer);
        await _context.SaveChangesAsync(cancellationToken);
        return layer;
    }

    public async Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default)
    {
        var layer = await _context.ModelLayers.FindAsync([layerId], cancellationToken);
        if (layer != null)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await _context.ModelLayers
            .Where(l => l.ModelId == modelId)
            .OrderBy(l => l.LayerIdx)
            .ToListAsync(cancellationToken);
    }
}
