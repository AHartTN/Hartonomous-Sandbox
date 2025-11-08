namespace Hartonomous.Core.Interfaces.ModelFormats;

/// <summary>
/// TensorFlow SavedModel metadata (future implementation).
/// </summary>
public class TensorFlowMetadata
{
    public string? SavedModelVersion { get; set; }
    public string[]? Tags { get; set; }
    public Dictionary<string, string>? SignatureDefs { get; set; }
}
