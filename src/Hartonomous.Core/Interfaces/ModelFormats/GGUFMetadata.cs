namespace Hartonomous.Core.Interfaces.ModelFormats;

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
