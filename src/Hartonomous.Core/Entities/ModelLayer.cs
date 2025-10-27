using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a single layer in a decomposed neural network model.
/// Stores weights as VECTOR type for efficient storage and querying.
/// </summary>
public class ModelLayer
{
    /// <summary>
    /// Gets or sets the unique identifier for the layer.
    /// </summary>
    public long LayerId { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the parent model.
    /// </summary>
    public int ModelId { get; set; }
    
    /// <summary>
    /// Gets or sets the zero-based index of this layer in the model.
    /// </summary>
    public int LayerIdx { get; set; }
    
    /// <summary>
    /// Gets or sets the human-readable name of the layer.
    /// </summary>
    public string? LayerName { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the layer (e.g., 'embedding', 'attention', 'feedforward').
    /// </summary>
    public string? LayerType { get; set; }
    
    /// <summary>
    /// Gets or sets the layer weights as a VECTOR.
    /// Small layers (less than 1998 dimensions float32) are stored directly.
    /// Large layers should be chunked across multiple rows.
    /// VECTOR type provides binary storage, efficient queries, and automatic deduplication.
    /// </summary>
    public SqlVector<float>? Weights { get; set; }
    
    /// <summary>
    /// Gets or sets the quantization type applied to the weights (e.g., 'int8', 'int4', 'fp16').
    /// </summary>
    public string? QuantizationType { get; set; }
    
    /// <summary>
    /// Gets or sets the quantization scale factor.
    /// </summary>
    public double? QuantizationScale { get; set; }
    
    /// <summary>
    /// Gets or sets the quantization zero point.
    /// </summary>
    public double? QuantizationZeroPoint { get; set; }
    
    /// <summary>
    /// Gets or sets the layer parameters as JSON (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? Parameters { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of parameters in this layer.
    /// </summary>
    public long? ParameterCount { get; set; }
    
    /// <summary>
    /// Gets or sets the cache hit rate for this layer's activations.
    /// </summary>
    public double? CacheHitRate { get; set; } = 0.0;
    
    /// <summary>
    /// Gets or sets the average computation time in milliseconds for this layer.
    /// </summary>
    public double? AvgComputeTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the parent model.
    /// </summary>
    public Model Model { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the collection of cached activations for this layer.
    /// </summary>
    public ICollection<CachedActivation> CachedActivations { get; set; } = new List<CachedActivation>();
}
