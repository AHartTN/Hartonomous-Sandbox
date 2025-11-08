namespace Hartonomous.Core.Interfaces.ModelFormats;

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
