using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Services.Vision;

public class SceneInfo
{
    public string? Caption { get; set; }
    public float CaptionConfidence { get; set; }
    public List<Tag> Tags { get; set; } = new();
    public List<DominantColor> DominantColors { get; set; } = new();
}
