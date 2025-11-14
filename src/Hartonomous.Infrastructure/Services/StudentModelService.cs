using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Provides helpers for extracting distilled "student" models from existing parent models.
/// </summary>
public class StudentModelService : IStudentModelService
{
    /// <summary>
    /// Entity Framework context used for model persistence operations.
    /// </summary>
    private readonly HartonomousDbContext _context;

    /// <summary>
    /// Repository used to query and persist model layer entities.
    /// </summary>
    private readonly IModelLayerRepository _layerRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="StudentModelService"/> class.
    /// </summary>
    /// <param name="context">Database context that tracks model entities.</param>
    /// <param name="layerRepository">Repository that exposes layer persistence APIs.</param>
    public StudentModelService(HartonomousDbContext context, IModelLayerRepository layerRepository)
    {
        _context = context;
        _layerRepository = layerRepository;
    }

    /// <summary>
    /// Builds a student model by selecting the most important layers based on a target ratio.
    /// </summary>
    /// <param name="parentModelId">Identifier of the parent model to distill.</param>
    /// <param name="targetSizeRatio">Fraction of layers to keep from the parent model (0-1 range).</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous work.</param>
    /// <returns>The distilled student model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the parent model cannot be located.</exception>
    public async Task<Model> ExtractByImportanceAsync(
        int parentModelId,
        double targetSizeRatio,
        CancellationToken cancellationToken = default)
    {
        var parentModel = await _context.Models
            .FirstOrDefaultAsync(m => m.ModelId == parentModelId, cancellationToken);

        if (parentModel == null)
            throw new InvalidOperationException($"Parent model {parentModelId} not found");

        var studentModel = new Model
        {
            ModelName = $"{parentModel.ModelName}_Student_{targetSizeRatio:P0}",
            ModelType = $"student_{parentModel.ModelType}",
            Architecture = $"distilled_{parentModel.Architecture}",
            IngestionDate = DateTime.UtcNow
        };

        _context.Models.Add(studentModel);
        await _context.SaveChangesAsync(cancellationToken);

        var parentLayers = await _layerRepository.GetByModelAsync(parentModelId, cancellationToken);
        var targetLayerCount = (int)(parentLayers.Count * targetSizeRatio);

        var layersToExtract = parentLayers
            .OrderBy(l => l.LayerIdx)
            .Take(targetLayerCount)
            .ToList();

        foreach (var layer in layersToExtract)
        {
            var studentLayer = new ModelLayer
            {
                ModelId = studentModel.ModelId,
                LayerIdx = layer.LayerIdx,
                LayerName = layer.LayerName,
                LayerType = layer.LayerType,
                WeightsGeometry = layer.WeightsGeometry,
                TensorShape = layer.TensorShape,
                TensorDtype = layer.TensorDtype,
                ParameterCount = layer.ParameterCount
            };

            await _layerRepository.AddAsync(studentLayer, cancellationToken);
        }

        return studentModel;
    }

    /// <summary>
    /// Creates a student model by copying a fixed number of layers from a parent model.
    /// </summary>
    /// <param name="parentModelId">Identifier of the parent model.</param>
    /// <param name="targetLayerCount">Number of layers to include in the student.</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous work.</param>
    /// <returns>The distilled student model containing the requested layer count.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the parent model cannot be found.</exception>
    public async Task<Model> ExtractByLayersAsync(
        int parentModelId,
        int targetLayerCount,
        CancellationToken cancellationToken = default)
    {
        var parentModel = await _context.Models.FindAsync([parentModelId], cancellationToken);
        if (parentModel == null)
            throw new InvalidOperationException($"Parent model {parentModelId} not found");

        var studentModel = new Model
        {
            ModelName = $"{parentModel.ModelName}_Student_L{targetLayerCount}",
            ModelType = $"student_{parentModel.ModelType}",
            Architecture = $"distilled_{parentModel.Architecture}",
            IngestionDate = DateTime.UtcNow
        };

        _context.Models.Add(studentModel);
        await _context.SaveChangesAsync(cancellationToken);

        var sql = $@"
            INSERT INTO dbo.ModelLayers (ModelId, LayerIdx, LayerName, LayerType, WeightsGeometry, TensorShape, TensorDtype, ParameterCount)
            SELECT TOP ({targetLayerCount})
                @studentModelId,
                LayerIdx,
                LayerName,
                LayerType,
                WeightsGeometry,
                TensorShape,
                TensorDtype,
                ParameterCount
            FROM dbo.ModelLayers
            WHERE ModelId = @parentModelId
            ORDER BY LayerIdx";

        await _context.Database.ExecuteSqlRawAsync(sql,
            new SqlParameter("@studentModelId", studentModel.ModelId),
            new SqlParameter("@parentModelId", parentModelId),
            cancellationToken);

        return studentModel;
    }

    /// <summary>
    /// Creates a student model consisting of layers whose weights fall within a specific value range.
    /// </summary>
    /// <param name="parentModelId">Identifier of the parent model.</param>
    /// <param name="minValue">Minimum weight value to include.</param>
    /// <param name="maxValue">Maximum weight value to include.</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous work.</param>
    /// <returns>The distilled student model composed of the filtered layer set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the parent model cannot be located.</exception>
    public async Task<Model> ExtractBySpatialRegionAsync(
        int parentModelId,
        double minValue,
        double maxValue,
        CancellationToken cancellationToken = default)
    {
        var parentModel = await _context.Models.FindAsync([parentModelId], cancellationToken);
        if (parentModel == null)
            throw new InvalidOperationException($"Parent model {parentModelId} not found");

        var studentModel = new Model
        {
            ModelName = $"{parentModel.ModelName}_Student_Range_{minValue}_{maxValue}",
            ModelType = $"student_{parentModel.ModelType}",
            Architecture = $"distilled_{parentModel.Architecture}",
            IngestionDate = DateTime.UtcNow
        };

        _context.Models.Add(studentModel);
        await _context.SaveChangesAsync(cancellationToken);

        var parentLayers = await _layerRepository.GetLayersByWeightRangeAsync(
            parentModelId,
            minValue,
            maxValue,
            cancellationToken);

        foreach (var layer in parentLayers)
        {
            var studentLayer = new ModelLayer
            {
                ModelId = studentModel.ModelId,
                LayerIdx = layer.LayerIdx,
                LayerName = layer.LayerName,
                LayerType = layer.LayerType,
                WeightsGeometry = layer.WeightsGeometry,
                TensorShape = layer.TensorShape,
                TensorDtype = layer.TensorDtype,
                ParameterCount = layer.ParameterCount
            };

            await _layerRepository.AddAsync(studentLayer, cancellationToken);
        }

        return studentModel;
    }

    /// <summary>
    /// Compares two models and returns summary statistics highlighting their similarities.
    /// </summary>
    /// <param name="modelAId">Identifier of the first model.</param>
    /// <param name="modelBId">Identifier of the second model.</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous work.</param>
    /// <returns>Comparison metrics showing parameter counts, shared layers, and compression ratio.</returns>
    public async Task<ModelComparisonResult> CompareModelsAsync(
        int modelAId,
        int modelBId,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM dbo.ModelLayers WHERE ModelId = @modelAId) as ModelAParams,
                (SELECT COUNT(*) FROM dbo.ModelLayers WHERE ModelId = @modelBId) as ModelBParams,
                (SELECT COUNT(*)
                 FROM dbo.ModelLayers a
                 INNER JOIN dbo.ModelLayers b ON a.LayerIdx = b.LayerIdx
                 WHERE a.ModelId = @modelAId AND b.ModelId = @modelBId) as SharedLayers";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@modelAId", modelAId));
        command.Parameters.Add(new SqlParameter("@modelBId", modelBId));

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var modelAParams = reader.GetInt32(0);
            var modelBParams = reader.GetInt32(1);
            var sharedLayers = reader.GetInt32(2);

            double compressionRatio = 0d;
            if (modelAParams > 0)
            {
                compressionRatio = modelBParams > 0
                    ? (double)modelAParams / modelBParams
                    : modelAParams;
            }

            return new ModelComparisonResult(
                modelAParams,
                modelBParams,
                compressionRatio,
                0,
                0,
                sharedLayers,
                sharedLayers > 0 ? (double)sharedLayers / Math.Max(modelAParams, modelBParams) : 0
            );
        }

        return new ModelComparisonResult(0, 0, 0, 0, 0, 0, 0);
    }
}
