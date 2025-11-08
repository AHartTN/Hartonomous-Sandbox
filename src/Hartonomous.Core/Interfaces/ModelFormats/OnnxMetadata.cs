namespace Hartonomous.Core.Interfaces.ModelFormats;

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
