using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Text.Json;

namespace Hartonomous.Infrastructure.ModelFormats;

/// <summary>
/// Handles database operations for building GGUF models, layers, and tensor segments.
/// </summary>
public class GGUFModelBuilder
{
    private readonly HartonomousDbContext _dbContext;
    private readonly GGUFGeometryBuilder _geometryBuilder;

    public GGUFModelBuilder(HartonomousDbContext dbContext, GGUFGeometryBuilder geometryBuilder)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _geometryBuilder = geometryBuilder ?? throw new ArgumentNullException(nameof(geometryBuilder));
    }

    /// <summary>
    /// Creates a new model entity from GGUF metadata.
    /// </summary>
    public async Task<Model> CreateModelAsync(ModelFormats.GGUFMetadata metadata, string filePath, CancellationToken cancellationToken = default)
    {
        var model = new Model
        {
            ModelName = Path.GetFileNameWithoutExtension(filePath),
            ModelType = "gguf",
            Architecture = metadata.Architecture,
            Config = JsonSerializer.Serialize(metadata.MetadataKV),
            ParameterCount = metadata.ParameterCount,
            IngestionDate = DateTime.UtcNow,
            UsageCount = 0
        };

        _dbContext.Models.Add(model);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return model;
    }

    /// <summary>
    /// Creates model layers from tensor information.
    /// </summary>
    public async Task<List<ModelLayer>> CreateLayersAsync(Model model, List<GGUFTensorInfo> tensors, CancellationToken cancellationToken = default)
    {
        var layers = new List<ModelLayer>();
        var layerGroups = tensors.GroupBy(t => ExtractLayerName(t.Name));

        foreach (var layerGroup in layerGroups)
        {
            var layerName = layerGroup.Key;
            var layerTensors = layerGroup.ToList();

            var layer = new ModelLayer
            {
                ModelId = model.ModelId,
                LayerIdx = layers.Count,
                LayerName = layerName,
                LayerType = ExtractLayerType(layerName),
                WeightsGeometry = layerTensors.Any() ? _geometryBuilder.CreateTensorGeometry(layerTensors.First()) : null,
                TensorShape = $"[{string.Join(",", layerTensors.SelectMany(t => t.Dimensions))}]",
                TensorDtype = "float32", // Default, would be determined from quantization
                QuantizationType = "none", // Would be determined from tensor types
                ParameterCount = layerTensors.Sum(t => t.ElementCount)
            };

            _dbContext.ModelLayers.Add(layer);
            layers.Add(layer);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create tensor segments for each layer
        foreach (var layer in layers)
        {
            var layerTensors = tensors.Where(t => ExtractLayerName(t.Name) == layer.LayerName).ToList();
            await CreateTensorSegmentsAsync(layer, layerTensors, cancellationToken);
        }

        return layers;
    }

    /// <summary>
    /// Creates tensor segments for a layer with geometry data.
    /// </summary>
    private async Task CreateTensorSegmentsAsync(ModelLayer layer, List<GGUFTensorInfo> tensors, CancellationToken cancellationToken = default)
    {
        foreach (var tensor in tensors)
        {
            var geometry = _geometryBuilder.CreateTensorGeometry(tensor);

            var segment = new LayerTensorSegment
            {
                LayerId = layer.LayerId,
                SegmentOrdinal = tensors.IndexOf(tensor),
                PointOffset = 0, // Would be calculated based on tensor position
                PointCount = (int)Math.Min(tensor.ElementCount, int.MaxValue),
                QuantizationType = tensor.Type.ToString(),
                GeometryFootprint = geometry,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.LayerTensorSegments.Add(segment);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates model statistics after processing.
    /// </summary>
    public async Task UpdateModelStatisticsAsync(Model model, List<ModelLayer> layers, CancellationToken cancellationToken = default)
    {
        model.ParameterCount = layers.Sum(l => l.ParameterCount);
        // Other statistics would be updated here

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Extracts layer name from tensor name (e.g., "blk.0.attn_q.weight" -> "blk.0").
    /// </summary>
    private static string ExtractLayerName(string tensorName)
    {
        if (string.IsNullOrEmpty(tensorName))
            return "unknown";

        // Common layer patterns in transformer models
        var parts = tensorName.Split('.');
        if (parts.Length >= 2)
        {
            // Handle patterns like "blk.0", "encoder.layer.0", etc.
            if (parts[0] == "blk" && int.TryParse(parts[1], out _))
            {
                return $"{parts[0]}.{parts[1]}";
            }
            else if (parts.Length >= 3 && parts[0] == "encoder" && parts[1] == "layer" && int.TryParse(parts[2], out _))
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            else if (parts.Length >= 3 && parts[0] == "decoder" && parts[1] == "layer" && int.TryParse(parts[2], out _))
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
        }

        // For embeddings, tokenizers, etc., use the first part
        return parts[0];
    }

    /// <summary>
    /// Extracts layer type from layer name.
    /// </summary>
    private static string ExtractLayerType(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
            return "unknown";

        var parts = layerName.Split('.');
        if (parts.Length >= 3)
        {
            // Extract type from patterns like "blk.0.attn_q" -> "attn_q"
            return string.Join(".", parts.Skip(2));
        }

        return "layer";
    }

    /// <summary>
    /// Validates that the model was created successfully.
    /// </summary>
    public async Task<bool> ValidateModelCreationAsync(Model model, CancellationToken cancellationToken = default)
    {
        var dbModel = await _dbContext.Models
            .Include(m => m.Layers)
            .ThenInclude(l => l.TensorSegments)
            .FirstOrDefaultAsync(m => m.ModelId == model.ModelId, cancellationToken);

        if (dbModel == null)
            return false;

        // Validate basic properties
        if (dbModel.ParameterCount <= 0)
            return false;

        // Validate layers exist
        if (!dbModel.Layers.Any())
            return false;

        // Validate tensor segments exist
        if (!dbModel.Layers.Any(l => l.TensorSegments.Any()))
            return false;

        return true;
    }
}

