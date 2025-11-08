namespace Hartonomous.Core.Interfaces.ModelFormats;

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
