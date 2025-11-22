namespace Hartonomous.Core.Models.Media;

public class CompressionMetrics
{
    public string MediaType { get; set; } = "Unknown";
    public string Format { get; set; } = "Unknown";
    public int CompressedSizeBytes { get; set; }
    public long UncompressedSizeBytes { get; set; }
    public double CompressionRatio { get; set; }
    public bool IsLossless { get; set; }
    public string EstimatedQuality { get; set; } = "Unknown";
    public Dictionary<string, string> Properties { get; set; } = new();
    
    public string CompressionType => IsLossless ? "Lossless" : "Lossy";
    
    public string CompressionRatioFormatted => 
        CompressionRatio > 0 ? $"{CompressionRatio:F2}:1" : "N/A";
    
    public string SpaceSavings => 
        CompressionRatio > 0 ? $"{(1 - 1 / CompressionRatio) * 100:F1}%" : "N/A";
}
