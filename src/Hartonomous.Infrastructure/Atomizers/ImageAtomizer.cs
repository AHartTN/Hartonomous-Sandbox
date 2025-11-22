using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Media;
using Hartonomous.Infrastructure.Services.Vision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes raster images into individual RGBA pixel atoms with X,Y spatial coordinates.
/// Supports PNG, JPEG, GIF, BMP, TIFF, WebP via ImageSharp library.
/// Each pixel becomes a 4-byte atom (R, G, B, A) with massive deduplication potential.
/// </summary>
public class ImageAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 20;

    public bool CanHandle(string contentType, string? fileExtension)
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

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Extract metadata using ImageMetadataExtractor (static class)
            ImageMetadata? imageMetadata = null;
            try
            {
                imageMetadata = ImageMetadataExtractor.ExtractMetadata(input);
            }
            catch (Exception ex)
            {
                warnings.Add($"Metadata extraction failed: {ex.Message}");
            }

            // Load image using ImageSharp
            using var image = Image.Load<Rgba32>(input);
            
            var width = image.Width;
            var height = image.Height;
            var totalPixels = width * height;

            // Analyze compression (if metadata was extracted)
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
            var imageHash = SHA256.HashData(input);
            var imageMetadataBytes = Encoding.UTF8.GetBytes($"image:{source.FileName}:{width}x{height}");
            
            // Serialize metadata to JSON for Atom.Metadata field (native json data type)
            string? metadataJson = null;
            if (imageMetadata != null)
            {
                try
                {
                    metadataJson = MetadataJsonSerializer.SerializeImageMetadata(imageMetadata, compressionMetrics);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Metadata JSON serialization failed: {ex.Message}");
                }
            }
            else
            {
                // Fallback: minimal metadata if extraction failed
                metadataJson = $"{{\"mediaType\":\"image\",\"width\":{width},\"height\":{height},\"format\":\"{image.Metadata.DecodedImageFormat?.Name ?? "Unknown"}\"}}";
            }

            var imageAtom = new AtomData
            {
                AtomicValue = imageMetadataBytes.Length <= MaxAtomSize ? imageMetadataBytes : imageMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = imageHash,
                Modality = "image",
                Subtype = "image-metadata",
                ContentType = source.ContentType,
                CanonicalText = $"{source.FileName ?? "image"} ({width}×{height})",
                Metadata = metadataJson // JSON metadata with EXIF, format, compression info
            };
            atoms.Add(imageAtom);

            // Track unique pixel colors for deduplication metrics
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
                    var pixelHash = SHA256.HashData(pixelBytes);
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

            sw.Stop();

            var deduplicationRatio = totalPixels > 0 
                ? (1.0 - (double)uniquePixelHashes.Count / totalPixels) * 100 
                : 0;

            warnings.Add($"Deduplication: {deduplicationRatio:F1}% ({uniquePixelHashes.Count:N0} unique colors from {totalPixels:N0} pixels)");

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Count, // Already deduplicated at atomizer level
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(ImageAtomizer),
                    DetectedFormat = $"{image.Metadata.DecodedImageFormat?.Name ?? "Unknown"} ({width}×{height})",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (UnknownImageFormatException ex)
        {
            warnings.Add($"Unknown image format: {ex.Message}");
            throw new InvalidOperationException("Image format not supported", ex);
        }
        catch (Exception ex)
        {
            warnings.Add($"Image atomization failed: {ex.Message}");
            throw;
        }
    }
}
