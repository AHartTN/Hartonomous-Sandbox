using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers;

/// <summary>
/// TRUE ATOMIC PIXEL ATOMIZER
/// 
/// Decomposes images into individual RGB/RGBA pixels.
/// Each unique color becomes a deduplicated atom, spatial position stored in AtomCompositions.
/// 
/// Philosophy: "Sky blue" (#87CEEB) appears in 10,000 images → stored ONCE, referenced 10M times.
/// 1920×1080 image = 2M pixels → typically ~100K unique colors after dedup (95% reduction).
/// </summary>
public sealed class PixelAtomizer : IAtomizer<byte[]>
{
    private readonly ILogger<PixelAtomizer>? _logger;

    public PixelAtomizer(ILogger<PixelAtomizer>? logger = null)
    {
        _logger = logger;
    }

    public string Modality => "image";

    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        byte[] imageBytes,
        AtomizationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            _logger?.LogWarning("Empty image data for atomic pixel decomposition");
            yield break;
        }

        Image<Rgba32>? image = null;
        try
        {
            image = Image.Load<Rgba32>(imageBytes);
            
            _logger?.LogDebug(
                "Atomizing {Width}×{Height} image into {TotalPixels} individual pixel atoms",
                image.Width, image.Height, image.Width * image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    
                    // 4-byte RGBA value
                    var rgbaBytes = new byte[] { pixel.R, pixel.G, pixel.B, pixel.A };
                    var pixelHash = SHA256.HashData(rgbaBytes);

                    // Spatial key: POINT(X, Y, 0, 0) for 2D image coordinates
                    var spatialWkt = $"POINT({x} {y} 0 0)";

                    yield return new AtomCandidate
                    {
                        Modality = "image",
                        Subtype = "rgba-pixel",
                        AtomicValue = rgbaBytes,
                        CanonicalText = $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}{pixel.A:X2}",
                        SourceUri = context.SourceUri ?? "unknown",
                        SourceType = "image",
                        ContentHash = Convert.ToHexString(pixelHash),
                        
                        // Position as spatial geometry
                        SpatialKey = spatialWkt,
                        
                        Metadata = new Dictionary<string, object>
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["r"] = pixel.R,
                            ["g"] = pixel.G,
                            ["b"] = pixel.B,
                            ["a"] = pixel.A,
                            ["colorSpace"] = "sRGB",
                            ["rgbHex"] = $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}"
                        },
                        
                        QualityScore = pixel.A / 255.0  // Use alpha as quality indicator
                    };
                }
                
                // Yield periodically to allow cancellation
                if (y % 100 == 0)
                    await Task.Yield();
            }
        }
        finally
        {
            image?.Dispose();
        }
    }
}
