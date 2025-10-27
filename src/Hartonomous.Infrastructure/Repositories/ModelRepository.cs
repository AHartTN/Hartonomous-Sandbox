using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlTypes;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Model entity
/// </summary>
public class ModelRepository : IModelRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<ModelRepository> _logger;

    public ModelRepository(HartonomousDbContext context, ILogger<ModelRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Model?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting model by ID: {ModelId}", modelId);
        
        return await _context.Models
            .Include(m => m.Layers)
            .Include(m => m.Metadata)
            .FirstOrDefaultAsync(m => m.ModelId == modelId, cancellationToken);
    }

    public async Task<Model?> GetByNameAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting model by name: {ModelName}", modelName);
        
        return await _context.Models
            .Include(m => m.Layers)
            .Include(m => m.Metadata)
            .FirstOrDefaultAsync(m => m.ModelName == modelName, cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all models");
        
        return await _context.Models
            .Include(m => m.Metadata)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetByTypeAsync(string modelType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting models by type: {ModelType}", modelType);
        
        return await _context.Models
            .Where(m => m.ModelType == modelType)
            .Include(m => m.Metadata)
            .ToListAsync(cancellationToken);
    }

    public async Task<Model> AddAsync(Model model, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding new model: {ModelName}", model.ModelName);
        
        _context.Models.Add(model);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Model added successfully with ID: {ModelId}", model.ModelId);
        return model;
    }

    public async Task UpdateAsync(Model model, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating model: {ModelId}", model.ModelId);
        
        _context.Models.Update(model);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Model updated successfully");
    }

    public async Task DeleteAsync(int modelId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting model: {ModelId}", modelId);
        
        var model = await _context.Models.FindAsync(new object[] { modelId }, cancellationToken);
        if (model != null)
        {
            _context.Models.Remove(model);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Model deleted successfully");
        }
        else
        {
            _logger.LogWarning("Model not found for deletion: {ModelId}", modelId);
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

    // Layer operations (Phase 2)
    public async Task<ModelLayer> AddLayerAsync(int modelId, ModelLayer layer, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding layer to model {ModelId}: {LayerName}", modelId, layer.LayerName);
        
        layer.ModelId = modelId;
        _context.ModelLayers.Add(layer);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Layer added successfully with ID: {LayerId}", layer.LayerId);
        return layer;
    }

    public async Task UpdateLayerWeightsAsync(int layerId, SqlVector<float> weights, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating weights for layer {LayerId} - USING ADO.NET (SqlVector parameter pattern)", layerId);
        
        // PAINFULLY OBVIOUS: Use ADO.NET for SqlVector parameter (most efficient)
        // This is the ONE method where direct SqlConnection is justified
        var connection = (SqlConnection)_context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ModelLayers 
                SET Weights = @weights, 
                    UpdatedAt = SYSUTCDATETIME() 
                WHERE layer_id = @layerId";
            
            command.Parameters.AddWithValue("@layerId", layerId);
            command.Parameters.AddWithValue("@weights", weights);
            
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Layer weights updated successfully");
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<IEnumerable<ModelLayer>> GetLayersByModelIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting layers for model: {ModelId}", modelId);
        
        return await _context.ModelLayers
            .Where(l => l.ModelId == modelId)
            .OrderBy(l => l.LayerIdx)
            .ToListAsync(cancellationToken);
    }
}
