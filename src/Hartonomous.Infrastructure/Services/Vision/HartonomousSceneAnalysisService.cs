using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Self-contained scene analysis using color analysis and pattern detection.
/// Pure C# implementation.
/// </summary>
public class HartonomousSceneAnalysisService
{
    public async Task<SceneInfo> AnalyzeSceneAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var image = ImageDecoder.Decode(imageData);
            
            var sceneInfo = new SceneInfo
            {
                DominantColors = ExtractDominantColors(image),
                Tags = GenerateTags(image),
                Caption = GenerateCaption(image),
                CaptionConfidence = 0.6f
            };
            
            return sceneInfo;
        }, cancellationToken);
    }

    private List<DominantColor> ExtractDominantColors(RawImage image)
    {
        // Use k-means clustering to find dominant colors
        var samples = SamplePixels(image, 1000); // Sample 1000 pixels
        var clusters = KMeansClustering(samples, 5); // Find 5 dominant colors
        
        var dominantColors = new List<DominantColor>();
        var totalPixels = image.Width * image.Height;
        
        foreach (var cluster in clusters)
        {
            if (cluster.Members.Count > 0)
            {
                dominantColors.Add(new DominantColor
                {
                    R = cluster.Centroid.R,
                    G = cluster.Centroid.G,
                    B = cluster.Centroid.B,
                    Percentage = (float)cluster.Members.Count / samples.Count * 100
                });
            }
        }
        
        return dominantColors.OrderByDescending(c => c.Percentage).ToList();
    }

    private List<Pixel> SamplePixels(RawImage image, int sampleCount)
    {
        var samples = new List<Pixel>();
        var random = new Random(42); // Fixed seed for reproducibility
        
        var step = Math.Max(1, (image.Width * image.Height) / sampleCount);
        
        for (int i = 0; i < image.Width * image.Height; i += step)
        {
            var x = i % image.Width;
            var y = i / image.Width;
            
            if (y < image.Height)
            {
                samples.Add(image.GetPixel(x, y));
            }
        }
        
        return samples;
    }

    private List<ColorCluster> KMeansClustering(List<Pixel> pixels, int k)
    {
        var random = new Random(42);
        var clusters = new List<ColorCluster>();
        
        // Initialize centroids randomly
        for (int i = 0; i < k; i++)
        {
            var randomPixel = pixels[random.Next(pixels.Count)];
            clusters.Add(new ColorCluster
            {
                Centroid = randomPixel,
                Members = new List<Pixel>()
            });
        }
        
        // Iterate until convergence (max 20 iterations)
        for (int iteration = 0; iteration < 20; iteration++)
        {
            // Clear members
            foreach (var cluster in clusters)
            {
                cluster.Members.Clear();
            }
            
            // Assign pixels to nearest centroid
            foreach (var pixel in pixels)
            {
                var nearestCluster = clusters
                    .OrderBy(c => ColorDistance(pixel, c.Centroid))
                    .First();
                
                nearestCluster.Members.Add(pixel);
            }
            
            // Update centroids
            foreach (var cluster in clusters)
            {
                if (cluster.Members.Count > 0)
                {
                    cluster.Centroid = new Pixel
                    {
                        R = (byte)cluster.Members.Average(p => p.R),
                        G = (byte)cluster.Members.Average(p => p.G),
                        B = (byte)cluster.Members.Average(p => p.B),
                        A = 255
                    };
                }
            }
        }
        
        return clusters;
    }

    private double ColorDistance(Pixel a, Pixel b)
    {
        var dr = a.R - b.R;
        var dg = a.G - b.G;
        var db = a.B - b.B;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private List<Tag> GenerateTags(RawImage image)
    {
        var tags = new List<Tag>();
        
        // Analyze brightness
        var avgBrightness = CalculateAverageBrightness(image);
        
        if (avgBrightness < 85)
        {
            tags.Add(new Tag { Name = "dark", Confidence = 0.8f });
        }
        else if (avgBrightness > 170)
        {
            tags.Add(new Tag { Name = "bright", Confidence = 0.8f });
        }
        
        // Analyze color distribution
        var colorfulness = CalculateColorfulness(image);
        
        if (colorfulness < 0.2)
        {
            tags.Add(new Tag { Name = "monochrome", Confidence = 0.7f });
        }
        else if (colorfulness > 0.6)
        {
            tags.Add(new Tag { Name = "colorful", Confidence = 0.7f });
        }
        
        // Analyze edge density (texture)
        var edgeDensity = CalculateEdgeDensity(image);
        
        if (edgeDensity > 0.3)
        {
            tags.Add(new Tag { Name = "detailed", Confidence = 0.6f });
        }
        else if (edgeDensity < 0.1)
        {
            tags.Add(new Tag { Name = "smooth", Confidence = 0.6f });
        }
        
        return tags;
    }

    private double CalculateAverageBrightness(RawImage image)
    {
        double sum = 0;
        var count = 0;
        
        for (int y = 0; y < image.Height; y += 4) // Sample every 4th pixel
        {
            for (int x = 0; x < image.Width; x += 4)
            {
                var pixel = image.GetPixel(x, y);
                sum += (pixel.R + pixel.G + pixel.B) / 3.0;
                count++;
            }
        }
        
        return sum / count;
    }

    private double CalculateColorfulness(RawImage image)
    {
        double saturationSum = 0;
        var count = 0;
        
        for (int y = 0; y < image.Height; y += 4)
        {
            for (int x = 0; x < image.Width; x += 4)
            {
                var pixel = image.GetPixel(x, y);
                var max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
                var min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                var saturation = max > 0 ? (max - min) / (double)max : 0;
                
                saturationSum += saturation;
                count++;
            }
        }
        
        return saturationSum / count;
    }

    private double CalculateEdgeDensity(RawImage image)
    {
        int edgeCount = 0;
        var totalPixels = 0;
        
        // Simple edge detection: if pixel differs significantly from neighbor
        for (int y = 1; y < image.Height - 1; y += 2)
        {
            for (int x = 1; x < image.Width - 1; x += 2)
            {
                var current = image.GetGrayscalePixel(x, y);
                var right = image.GetGrayscalePixel(x + 1, y);
                var down = image.GetGrayscalePixel(x, y + 1);
                
                if (Math.Abs(current - right) > 30 || Math.Abs(current - down) > 30)
                {
                    edgeCount++;
                }
                
                totalPixels++;
            }
        }
        
        return (double)edgeCount / totalPixels;
    }

    private string GenerateCaption(RawImage image)
    {
        // Simple rule-based caption generation based on analyzed features
        var brightness = CalculateAverageBrightness(image);
        var colorfulness = CalculateColorfulness(image);
        var edgeDensity = CalculateEdgeDensity(image);
        
        var caption = "An image";
        
        if (brightness < 85)
        {
            caption += " with dark tones";
        }
        else if (brightness > 170)
        {
            caption += " with bright tones";
        }
        
        if (colorfulness > 0.6)
        {
            caption += " and vivid colors";
        }
        else if (colorfulness < 0.2)
        {
            caption += " in monochrome";
        }
        
        if (edgeDensity > 0.3)
        {
            caption += ", showing detailed texture";
        }
        else if (edgeDensity < 0.1)
        {
            caption += ", with smooth gradients";
        }
        
        return caption;
    }
}

public class ColorCluster
{
    public required Pixel Centroid { get; set; }
    public required List<Pixel> Members { get; set; }
}

public class SceneInfo
{
    public string? Caption { get; set; }
    public float CaptionConfidence { get; set; }
    public List<Tag> Tags { get; set; } = new();
    public List<DominantColor> DominantColors { get; set; } = new();
}

public class Tag
{
    public required string Name { get; set; }
    public required float Confidence { get; set; }
}

public class DominantColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public float Percentage { get; set; }
}
