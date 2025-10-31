using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a single layer in a decomposed neural network model.
/// Stores weights as GEOMETRY (LINESTRING ZM) for variable-dimension tensors with rich metadata.
/// X = index, Y = weight value, Z = importance/gradient, M = iteration/depth.
/// Exploits spatial indexes for O(log n) queries instead of O(n) VECTOR scans.
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
    /// Gets or sets the layer weights as GEOMETRY (LINESTRING ZM).
    /// LINESTRING stores variable-length tensors without dimension limits (up to 1B+ points).
    /// X coordinate = index in tensor (0, 1, 2, ...)
    /// Y coordinate = weight value (actual float value)
    /// Z coordinate = importance score (gradient magnitude, attention weight, pruning priority)
    /// M coordinate = temporal/structural metadata (training iteration, layer depth, update timestamp)
    /// Supports spatial indexes for O(log n) queries instead of O(n) scans.
    /// </summary>
    public LineString? WeightsGeometry { get; set; }
    
    /// <summary>
    /// Gets or sets the tensor shape as JSON array (e.g., "[3584, 3584]" for 2D matrix).
    /// Used to reconstruct original tensor structure from flattened LINESTRING.
    /// For 1D tensors: "[n]", for 2D: "[rows, cols]", for 3D: "[d1, d2, d3]".
    /// </summary>
    public string? TensorShape { get; set; }
    
    /// <summary>
    /// Gets or sets the data type of the tensor (e.g., "float32", "float16", "bfloat16").
    /// </summary>
    public string? TensorDtype { get; set; } = "float32";
    
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

    /// <summary>
    /// Gets or sets the collection of tensor atoms derived from this layer.
    /// </summary>
    public ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();

    /// <summary>
    /// Gets or sets the collection of tensor atom coefficients referencing this layer.
    /// </summary>
    public ICollection<TensorAtomCoefficient> TensorAtomCoefficients { get; set; } = new List<TensorAtomCoefficient>();
}
