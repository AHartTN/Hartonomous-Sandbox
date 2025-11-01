namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Service for discovering and identifying AI model formats.
/// Handles multi-file models (PyTorch shards, Stable Diffusion components, etc.)
/// </summary>
public interface IModelDiscoveryService
{
    /// <summary>
    /// Detect the format of a model from file path or directory.
    /// Supports single-file models (.onnx) and multi-file models (Llama directories).
    /// </summary>
    /// <param name="path">Path to model file or directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detected format information</returns>
    Task<ModelFormatInfo> DetectFormatAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all files required to load a model.
    /// For single-file: returns [model.onnx]
    /// For multi-file: returns [pytorch_model-00001.bin, pytorch_model-00002.bin, ..., config.json, tokenizer.json]
    /// </summary>
    /// <param name="modelPath">Path to model file or directory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of file paths needed for model</returns>
    Task<IEnumerable<string>> GetModelFilesAsync(string modelPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a path contains a valid model.
    /// </summary>
    Task<bool> IsValidModelAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a detected model format.
/// </summary>
public class ModelFormatInfo
{
    /// <summary>
    /// Format name: "PyTorch", "Safetensors", "ONNX", "GGUF", "TensorFlow"
    /// </summary>
    public required string Format { get; set; }

    /// <summary>
    /// Specific architecture if detectable: "Llama", "Stable-Diffusion", "Whisper", "BERT"
    /// </summary>
    public string? Architecture { get; set; }

    /// <summary>
    /// True if model spans multiple files (PyTorch shards, SD3.5 components)
    /// </summary>
    public bool IsMultiFile { get; set; }

    /// <summary>
    /// True if model is sharded across multiple weight files
    /// </summary>
    public bool IsSharded { get; set; }

    /// <summary>
    /// Number of shards if IsSharded = true
    /// </summary>
    public int? ShardCount { get; set; }

    /// <summary>
    /// File extensions found: [".bin", ".json", ".safetensors"]
    /// </summary>
    public IEnumerable<string> Extensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Required files: ["config.json", "tokenizer.json", "pytorch_model.bin"]
    /// </summary>
    public IEnumerable<string> RequiredFiles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Optional files: ["tokenizer_config.json", "special_tokens_map.json"]
    /// </summary>
    public IEnumerable<string> OptionalFiles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Extracted metadata from config files
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Confidence score (0.0-1.0) for format detection
    /// </summary>
    public double Confidence { get; set; } = 1.0;
}
