using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Media;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Services.Vision;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes raster images into individual RGBA pixel atoms with X,Y spatial coordinates.
/// Supports PNG, JPEG, GIF, BMP, TIFF, WebP via ImageSharp library.
/// Each pixel becomes a 4-byte atom (R, G, B, A) with massive deduplication potential.
/// </summary>
public class ImageAtomizer : BaseAtomizer<byte[]>
{
    public ImageAtomizer(ILogger<ImageAtomizer> logger) : base(logger) { }

    public override int Priority => 20;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("image/") == true)
        {
            // Exclude SVG (vector graphics, handled separately)
            if (contentType == "image/svg+xml")
                return false;
            return true;
        }

        var imageExtensions = new[] { "png", "jpg", "jpeg", "gif", "bmp", "tiff", "tif", "webp" };
        return fileExtension != null && imageExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        ImageMetadata? imageMetadata = null;
        try
        {
            imageMetadata = ImageMetadataExtractor.ExtractMetadata(input);
        }
        catch (Exception ex)
        {
            warnings.Add($"Metadata extraction failed: {ex.Message}");
        }

        using var image = Image.Load<Rgba32>(input);
        
        var width = image.Width;
        var height = image.Height;
        var totalPixels = width * height;

        CompressionMetrics? compressionMetrics = null;
        if (imageMetadata != null)
        {
            try
            {
                // Calculate raw pixel data size for compression analysis
                var rawPixelDataSize = width * height * 4; // RGBA = 4 bytes per pixel
                // Create synthetic raw data representation for compression ratio calculation
                var rawPixelDataBytes = new byte[Math.Min(rawPixelDataSize, 1024)]; // Sample for analysis
                compressionMetrics = CompressionAnalyzer.AnalyzeImage(imageMetadata, rawPixelDataBytes);
            }
            catch (Exception ex)
            {
                warnings.Add($"Compression analysis failed: {ex.Message}");
            }
        }

        // Create parent atom for the entire image with JSON metadata
        var imageHash = CreateFileMetadataAtom(input, source, atoms);
        var uniquePixelHashes = new HashSet<string>();

        // Process pixels
        for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int x = 0; x < width; x++)
            {
                var pixel = image[x, y];
                
                // Create 4-byte RGBA atom
                var pixelBytes = new byte[] { pixel.R, pixel.G, pixel.B, pixel.A };
                var pixelHash = HashUtilities.ComputeSHA256(pixelBytes);
                var pixelHashStr = Convert.ToBase64String(pixelHash);

                // Only add unique pixel atoms to list (content-addressable deduplication)
                if (!uniquePixelHashes.Contains(pixelHashStr))
                {
                    var pixelAtom = new AtomData
                    {
                        AtomicValue = pixelBytes,
                        ContentHash = pixelHash,
                        Modality = "image",
                        Subtype = "rgba-pixel",
                        ContentType = "application/octet-stream",
                        CanonicalText = $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}{(pixel.A != 255 ? pixel.A.ToString("X2") : "")}",
                        Metadata = $"{{\"r\":{pixel.R},\"g\":{pixel.G},\"b\":{pixel.B},\"a\":{pixel.A}}}"
                    };
                    atoms.Add(pixelAtom);
                    uniquePixelHashes.Add(pixelHashStr);
                }

                // Link pixel to image with spatial position
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = imageHash,
                    ComponentAtomHash = pixelHash,
                    SequenceIndex = y * width + x, // Row-major order
                    Position = new SpatialPosition 
                    { 
                        X = x, 
                        Y = y, 
                        Z = 0, // Could use Z for layers in multi-layer formats
                        M = null // Could use M for frame number in animated GIFs
                    }
                });
            }

            // Report progress for large images
            if (y % 100 == 0 && y > 0)
            {
                var progress = (double)y / height * 100;
                warnings.Add($"Progress: {progress:F1}% ({y}/{height} rows)");
            }
        }

        var deduplicationRatio = totalPixels > 0 
            ? (1.0 - (double)uniquePixelHashes.Count / totalPixels) * 100 
            : 0;

        warnings.Add($"Deduplication: {deduplicationRatio:F1}% ({uniquePixelHashes.Count:N0} unique colors from {totalPixels:N0} pixels)");

        await Task.CompletedTask;
    }

    protected override string GetDetectedFormat()
    {
        return "raster image";
    }

    protected override string GetModality() => "image";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        using var image = Image.Load<Rgba32>(input);
        return Encoding.UTF8.GetBytes($"image:{source.FileName}:{image.Width}x{image.Height}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        using var image = Image.Load<Rgba32>(input);
        return $"{source.FileName ?? "image"} ({image.Width}×{image.Height})";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
        using var image = Image.Load<Rgba32>(input);
        
        ImageMetadata? imageMetadata = null;
        CompressionMetrics? compressionMetrics = null;
        
        try
        {
            imageMetadata = ImageMetadataExtractor.ExtractMetadata(input);
            if (imageMetadata != null)
            {
                var rawPixelDataSize = image.Width * image.Height * 4;
                var rawPixelDataBytes = new byte[Math.Min(rawPixelDataSize, 1024)];
                compressionMetrics = CompressionAnalyzer.AnalyzeImage(imageMetadata, rawPixelDataBytes);
            }
        }
        catch
        {
            // Fallback
        }

        if (imageMetadata != null)
        {
            try
            {
                return MetadataJsonSerializer.SerializeImageMetadata(imageMetadata, compressionMetrics);
            }
            catch
            {
                // Fallback
            }
        }

        return $"{{\"mediaType\":\"image\",\"width\":{image.Width},\"height\":{image.Height},\"format\":\"{image.Metadata.DecodedImageFormat?.Name ?? "Unknown"}\"}}";
    }
}
