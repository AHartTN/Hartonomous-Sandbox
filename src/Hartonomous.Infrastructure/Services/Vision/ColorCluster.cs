using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Services.Vision;

public class ColorCluster
{
    public required Pixel Centroid { get; set; }
    public required List<Pixel> Members { get; set; }
}
