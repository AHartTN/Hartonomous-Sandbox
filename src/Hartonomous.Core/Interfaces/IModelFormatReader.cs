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
    /// Validate that the file at modelPath is in the expected format.
    /// Quick check without full parsing (e.g., magic number, header validation).
    /// </summary>
    /// <param name="modelPath">Absolute path to model file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file is valid for this format</returns>
    Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default);

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
    public List<string> Files { get; set; } = new();
    public Dictionary<string, string> GlobalMetadata { get; set; } = new();
    public Dictionary<string, SafetensorsTensorInfo> Tensors { get; set; } = new();
    public string? Format { get; set; }
    public string? Architecture { get; set; }
    public int TensorCount { get; set; }
    public long TotalSizeBytes { get; set; }

    // Legacy compatibility
    public Dictionary<string, string>? Header { get; set; }
}

/// <summary>
/// Tensor information from Safetensors format.
/// </summary>
public class SafetensorsTensorInfo
{
    public string? DType { get; set; }
    public long[]? Shape { get; set; }
    public long[]? DataOffsets { get; set; }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
[Obsolete("Use SafetensorsTensorInfo instead")]
public class TensorInfo : SafetensorsTensorInfo
{
}

/// <summary>
/// PyTorch-specific metadata.
/// </summary>
public class PyTorchMetadata
{
    public string? ModelType { get; set; }
    public string? Architecture { get; set; }
    public int? NumLayers { get; set; }
    public int? HiddenSize { get; set; }
    public int? IntermediateSize { get; set; }
    public int? NumAttentionHeads { get; set; }
    public int? NumKeyValueHeads { get; set; }
    public int? VocabSize { get; set; }
    public int? MaxPositionEmbeddings { get; set; }
    public string? ActivationFunction { get; set; }
    public float? RmsNormEps { get; set; }
    public Dictionary<string, object> RawConfig { get; set; } = new();
    public List<string> ShardFiles { get; set; } = new();
    public bool HasTokenizer { get; set; }
    public Dictionary<string, object>? StateDict { get; set; }
}

/// <summary>
/// GGUF (GPT-Generated Unified Format) quantized model metadata.
/// Used by llama.cpp, ollama, and other quantized inference engines.
/// </summary>
public class GGUFMetadata
{
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public uint Version { get; set; }
    public int TensorCount { get; set; }
    public string? Architecture { get; set; }
    public string? QuantizationType { get; set; }
    public string? FileType { get; set; }
    public int? LayerCount { get; set; }
    public int? ContextLength { get; set; }
    public int? EmbeddingLength { get; set; }
    public int? AttentionHeadCount { get; set; }
    public long? ParameterCount { get; set; }
    public Dictionary<string, object> MetadataKV { get; set; } = new();
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
