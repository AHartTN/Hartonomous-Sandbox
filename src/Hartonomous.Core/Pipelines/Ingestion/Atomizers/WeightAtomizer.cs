using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers;

/// <summary>
/// TRUE ATOMIC WEIGHT ATOMIZER
/// 
/// Decomposes AI model weights into individual float32 values.
/// Each unique weight becomes a deduplicated atom with tensor position as GEOMETRY.
/// 
/// Philosophy: GPT-4 scale (1.76T params) → ~100M unique float32 values after quantization/dedup.
/// Weight updates only store CHANGED coefficients, not entire matrices.
/// Spatial queries: "Find all weights > 0.9 in attention layers" = instant.
/// </summary>
public sealed class WeightAtomizer : IAtomizer<ModelWeightData>
{
    private readonly ILogger<WeightAtomizer>? _logger;

    public WeightAtomizer(ILogger<WeightAtomizer>? logger = null)
    {
        _logger = logger;
    }

    public string Modality => "model";

    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        ModelWeightData model,
        AtomizationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (model == null || model.Layers == null)
        {
            _logger?.LogWarning("Empty model data for atomic weight decomposition");
            yield break;
        }

        _logger?.LogDebug(
            "Atomizing model '{ModelName}' with {LayerCount} layers into individual float32 weight atoms",
            model.ModelName, model.Layers.Count);

        long totalWeightCount = 0;
        foreach (var layer in model.Layers)
        {
            totalWeightCount += layer.Weights?.LongLength ?? 0;
        }

        _logger?.LogInformation(
            "Model contains {TotalWeights:N0} total weights to atomize",
            totalWeightCount);

        foreach (var layer in model.Layers)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (layer.Weights == null || layer.Weights.Length == 0)
                continue;

            var layerId = layer.LayerId;
            var shape = layer.Shape ?? new int[] { layer.Weights.Length };

            for (int i = 0; i < layer.Weights.Length; i++)
            {
                var weight = layer.Weights[i];
                var (row, col) = IndexToRowCol(i, shape);
                
                // 4-byte float32 value
                var weightBytes = BitConverter.GetBytes(weight);
                var weightHash = SHA256.HashData(weightBytes);

                // Spatial key: POINT(layerId, row, col, 0) for 3D tensor position
                var spatialWkt = $"POINT({layerId} {row} {col} 0)";

                yield return new AtomCandidate
                {
                    Modality = "model",
                    Subtype = "float32-weight",
                    AtomicValue = weightBytes,
                    CanonicalText = weight.ToString("G9"),  // Full float precision
                    SourceUri = context.SourceUri ?? model.ModelName ?? "unknown",
                    SourceType = "model-weights",
                    ContentHash = Convert.ToHexString(weightHash),
                    
                    // Position as spatial geometry
                    SpatialKey = spatialWkt,
                    
                    Metadata = new Dictionary<string, object>
                    {
                        ["modelName"] = model.ModelName ?? "unknown",
                        ["layerId"] = layerId,
                        ["layerName"] = layer.LayerName ?? $"layer_{layerId}",
                        ["layerType"] = layer.LayerType ?? "unknown",
                        ["position"] = i,
                        ["row"] = row,
                        ["col"] = col,
                        ["tensorShape"] = string.Join("×", shape),
                        ["dtype"] = "float32",
                        ["value"] = weight
                    },
                    
                    QualityScore = 1.0
                };

                // Yield periodically
                if (i % 10000 == 0 && i > 0)
                    await Task.Yield();
            }
        }
    }

    private static (int row, int col) IndexToRowCol(int index, int[] shape)
    {
        if (shape.Length == 1)
            return (index, 0);
        
        if (shape.Length == 2)
        {
            int row = index / shape[1];
            int col = index % shape[1];
            return (row, col);
        }

        // For higher dimensions, flatten to 2D
        int totalCols = 1;
        for (int i = 1; i < shape.Length; i++)
            totalCols *= shape[i];
        
        return (index / totalCols, index % totalCols);
    }
}

/// <summary>
/// Model weight data for atomization
/// </summary>
public class ModelWeightData
{
    public string? ModelName { get; set; }
    public List<LayerWeightData> Layers { get; set; } = new();
}

/// <summary>
/// Layer weight data
/// </summary>
public class LayerWeightData
{
    public int LayerId { get; set; }
    public string? LayerName { get; set; }
    public string? LayerType { get; set; }
    public int[]? Shape { get; set; }
    public float[]? Weights { get; set; }
}
