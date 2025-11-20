using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Services.Vision;

public class Component
{
    public List<(int x, int y)> Pixels { get; set; } = new();
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
