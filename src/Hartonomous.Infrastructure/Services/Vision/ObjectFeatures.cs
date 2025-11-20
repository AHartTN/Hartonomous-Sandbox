namespace Hartonomous.Infrastructure.Services.Vision;

public class ObjectFeatures
{
    public int Area { get; set; }
    public double Perimeter { get; set; }
    public double AspectRatio { get; set; }
    public double Circularity { get; set; }
    public double Convexity { get; set; }
    public int[]? ColorHistogram { get; set; }
}
