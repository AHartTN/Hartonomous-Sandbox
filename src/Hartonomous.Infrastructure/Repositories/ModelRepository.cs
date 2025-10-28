using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using System.Data;

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
        
        // Use direct ADO.NET for entire insert to avoid EF Core's NTS binary serializer hitting array limits
        var connection = (SqlConnection)_context.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);
        
        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 300; // 5 minutes for large WKT parsing
            command.CommandText = @"
                INSERT INTO dbo.ModelLayers (
                    ModelId, LayerIdx, LayerName, LayerType, ParameterCount, 
                    TensorShape, TensorDtype, QuantizationType, Parameters, 
                    WeightsGeometry, AvgComputeTimeMs, CacheHitRate, QuantizationScale, QuantizationZeroPoint
                )
                OUTPUT INSERTED.LayerId
                VALUES (
                    @modelId, @layerIdx, @layerName, @layerType, @parameterCount,
                    @tensorShape, @tensorDtype, @quantizationType, @parameters,
                    geometry::STGeomFromWKB(@wkb, 0), @avgComputeTimeMs, @cacheHitRate, @quantizationScale, @quantizationZeroPoint
                )";
            
            command.Parameters.AddWithValue("@modelId", modelId);
            command.Parameters.AddWithValue("@layerIdx", layer.LayerIdx);
            command.Parameters.AddWithValue("@layerName", (object?)layer.LayerName ?? DBNull.Value);
            command.Parameters.AddWithValue("@layerType", (object?)layer.LayerType ?? DBNull.Value);
            command.Parameters.AddWithValue("@parameterCount", (object?)layer.ParameterCount ?? DBNull.Value);
            command.Parameters.AddWithValue("@tensorShape", (object?)layer.TensorShape ?? DBNull.Value);
            command.Parameters.AddWithValue("@tensorDtype", (object?)layer.TensorDtype ?? DBNull.Value);
            command.Parameters.AddWithValue("@quantizationType", (object?)layer.QuantizationType ?? DBNull.Value);
            command.Parameters.AddWithValue("@parameters", (object?)layer.Parameters ?? DBNull.Value);
            command.Parameters.AddWithValue("@avgComputeTimeMs", (object?)layer.AvgComputeTimeMs ?? DBNull.Value);
            command.Parameters.AddWithValue("@cacheHitRate", (object?)layer.CacheHitRate ?? DBNull.Value);
            command.Parameters.AddWithValue("@quantizationScale", (object?)layer.QuantizationScale ?? DBNull.Value);
            command.Parameters.AddWithValue("@quantizationZeroPoint", (object?)layer.QuantizationZeroPoint ?? DBNull.Value);
            
            // Binary WKB format - optimized for SQL Server geometry engine
            if (layer.WeightsGeometry != null)
            {
                var wkbWriter = new NetTopologySuite.IO.WKBWriter();
                var wkb = wkbWriter.Write(layer.WeightsGeometry);
                command.Parameters.Add("@wkb", SqlDbType.VarBinary, -1).Value = wkb;
            }
            else
            {
                command.Parameters.AddWithValue("@wkb", DBNull.Value);
            }            var layerId = await command.ExecuteScalarAsync(cancellationToken);
            layer.LayerId = Convert.ToInt64(layerId);
            
            _logger.LogInformation("Layer added successfully with ID: {LayerId}", layer.LayerId);
            return layer;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    public async Task UpdateLayerWeightsGeometryAsync(long layerId, LineString weightsGeometry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating WeightsGeometry for layer {LayerId} via ADO.NET (WKB binary format)", layerId);
        
        var connection = (SqlConnection)_context.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);
        
        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 300;
            command.CommandText = @"
                UPDATE dbo.ModelLayers 
                SET WeightsGeometry = geometry::STGeomFromWKB(@wkb, 0)
                WHERE LayerId = @layerId";
            
            var wkbWriter = new NetTopologySuite.IO.WKBWriter();
            var wkb = wkbWriter.Write(weightsGeometry);
            command.Parameters.AddWithValue("@layerId", layerId);
            command.Parameters.Add("@wkb", SqlDbType.VarBinary, -1).Value = wkb;
            
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            
            if (rowsAffected > 0)
                _logger.LogDebug("WeightsGeometry updated successfully for layer {LayerId} ({Points} points)", 
                    layerId, weightsGeometry.NumPoints);
            else
                _logger.LogWarning("No rows updated for layer {LayerId}", layerId);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
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
