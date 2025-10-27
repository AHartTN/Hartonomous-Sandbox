using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Generic interface for reading models from various file formats.
/// Extensible design supports ONNX, Safetensors, PyTorch, TensorFlow, HuggingFace, etc.
/// Outputs Core entities directly (not DTOs) for seamless EF Core persistence.
/// </summary>
/// <typeparam name="TMetadata">Format-specific metadata type (e.g., ONNX graph info, Safetensors header)</typeparam>
public interface IModelFormatReader<TMetadata> where TMetadata : class
{
    /// <summary>
    /// Read model from file and return Core entity ready for persistence.
    /// Populates Model with Layers collection and all metadata.
    /// </summary>
    /// <param name="modelPath">Absolute path to model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model entity with populated Layers</returns>
    Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extract format-specific metadata without full model read.
    /// Useful for model discovery and validation before ingestion.
    /// </summary>
    /// <param name="modelPath">Absolute path to model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Format-specific metadata</returns>
    Task<TMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the supported file extensions for this format reader.
    /// Used by factory pattern for automatic reader selection.
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }
    
    /// <summary>
    /// Gets the format name (e.g., "ONNX", "Safetensors", "PyTorch").
    /// </summary>
    string FormatName { get; }
}

/// <summary>
/// ONNX-specific metadata extracted from model file.
/// </summary>
public class OnnxMetadata
{
    public string? GraphName { get; set; }
    public string? ProducerName { get; set; }
    public string? Domain { get; set; }
    public string? Description { get; set; }
    public long Version { get; set; }
    public Dictionary<string, string[]>? InputShapes { get; set; }
    public Dictionary<string, string[]>? OutputShapes { get; set; }
}

/// <summary>
/// Safetensors-specific metadata extracted from model file.
/// </summary>
public class SafetensorsMetadata
{
    public Dictionary<string, string>? Header { get; set; }
    public Dictionary<string, TensorInfo>? Tensors { get; set; }
}

/// <summary>
/// Tensor information from Safetensors format.
/// </summary>
public class TensorInfo
{
    public string? DType { get; set; }
    public long[]? Shape { get; set; }
    public long[]? DataOffsets { get; set; }
}

/// <summary>
/// PyTorch-specific metadata (future implementation).
/// </summary>
public class PyTorchMetadata
{
    public string? ModelType { get; set; }
    public Dictionary<string, object>? StateDict { get; set; }
}

/// <summary>
/// TensorFlow SavedModel metadata (future implementation).
/// </summary>
public class TensorFlowMetadata
{
    public string? SavedModelVersion { get; set; }
    public string[]? Tags { get; set; }
    public Dictionary<string, string>? SignatureDefs { get; set; }
}
