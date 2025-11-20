namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Raw decoded image data.
/// </summary>
public class RawImage
{
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required Pixel[,] Pixels { get; set; }
    
    public Pixel GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return new Pixel { R = 0, G = 0, B = 0, A = 255 };
        
        return Pixels[y, x];
    }
    
    public byte GetGrayscalePixel(int x, int y)
    {
        var pixel = GetPixel(x, y);
        return (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
    }
}
