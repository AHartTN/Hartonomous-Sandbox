using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Services.Vision;

public class Contour
{
    public List<(int x, int y)> Points { get; set; } = new();
    public required BoundingBox BoundingBox { get; set; }
}
