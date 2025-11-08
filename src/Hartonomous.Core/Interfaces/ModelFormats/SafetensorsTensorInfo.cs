namespace Hartonomous.Core.Interfaces.ModelFormats;

/// <summary>
/// Tensor information from Safetensors format.
/// </summary>
public class SafetensorsTensorInfo
{
    public string? DType { get; set; }
    public long[]? Shape { get; set; }
    public long[]? DataOffsets { get; set; }
}
