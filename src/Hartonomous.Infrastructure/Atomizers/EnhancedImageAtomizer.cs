using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Enhanced image atomizer with OCR, object detection, and scene analysis.
/// Atomizes: pixels (visual content) + extracted text (OCR) + detected objects + metadata.
/// </summary>
public class EnhancedImageAtomizer : IAtomizer<byte[]>
{
    private readonly IOcrService? _ocrService;
    private readonly IObjectDetectionService? _objectDetectionService;
    private readonly ISceneAnalysisService? _sceneAnalysisService;
    private const int MaxAtomSize = 64;
    
    public int Priority => 50;

    public EnhancedImageAtomizer(
        IOcrService? ocrService = null,
        IObjectDetectionService? objectDetectionService = null,
        ISceneAnalysisService? sceneAnalysisService = null)
    {
        _ocrService = ocrService;
        _objectDetectionService = objectDetectionService;
        _sceneAnalysisService = sceneAnalysisService;
    }

    public async Task<AtomizationResult> AtomizeAsync(
        byte[] imageData,
        SourceMetadata source,
        ImageProcessingOptions options,
        CancellationToken cancellationToken)
    {
        return await AtomizeWithOptionsAsync(imageData, source, options, cancellationToken);
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        var imageTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/bmp", "image/webp", "image/tiff" };
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff" };
        
        return imageTypes.Contains(contentType?.ToLowerInvariant()) ||
               imageExtensions.Contains(fileExtension?.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(
        byte[] imageData,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        // Default: only extract pixels, no semantic analysis
        return await AtomizeWithOptionsAsync(imageData, source, new ImageProcessingOptions(), cancellationToken);
    }

    private async Task<AtomizationResult> AtomizeWithOptionsAsync(
        byte[] imageData,
        SourceMetadata source,
        ImageProcessingOptions options,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Decode image to get dimensions and pixel data
            var imageInfo = await DecodeImageAsync(imageData, cancellationToken);
            
            // Create image identifier atom (hash of image data)
            var imageHash = SHA256.HashData(imageData);
            var imageIdBytes = imageHash.Take(32).ToArray(); // Use first 32 bytes of hash as ID
            var imageIdAtom = new AtomData
            {
                AtomicValue = imageIdBytes,
                ContentHash = SHA256.HashData(imageIdBytes),
                Modality = "image",
                Subtype = "image-id",
                ContentType = source.ContentType,
                CanonicalText = Convert.ToBase64String(imageIdBytes),
                Metadata = $"{{\"width\":{imageInfo.Width},\"height\":{imageInfo.Height},\"format\":\"{imageInfo.Format}\"}}"
            };
            atoms.Add(imageIdAtom);
            
            // 1. ATOMIZE PIXELS (visual content for retrieval)
            var pixelAtoms = AtomizePixels(imageInfo, imageIdAtom.ContentHash, compositions, cancellationToken);
            atoms.AddRange(pixelAtoms);

            // 2. OCR - Extract and atomize text from image (opt-in)
            if (options.ExtractText && _ocrService != null)
            {
                try
                {
                    var ocrResults = await _ocrService!.ExtractTextAsync(imageData, cancellationToken);
                    var ocrAtoms = AtomizeOcrResults(ocrResults, imageIdAtom.ContentHash, compositions);
                    atoms.AddRange(ocrAtoms);
                    
                    if (ocrResults.Any())
                    {
                        warnings.Add($"OCR extracted {ocrResults.Count} text regions");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"OCR failed: {ex.Message}");
                }
            }
            else if (options.ExtractText && _ocrService == null)
            {
                warnings.Add("OCR requested but service not available");
            }

            // 3. OBJECT DETECTION - Detect and atomize objects/entities (opt-in)
            if (options.DetectObjects && _objectDetectionService != null)
            {
                try
                {
                    var detectedObjects = await _objectDetectionService!.DetectObjectsAsync(imageData, cancellationToken);
                    
                    // Identify objects if requested
                    if (options.IdentifyObjects)
                    {
                        // Object identification already happens in detection service
                        // The Label field contains the identified object type
                    }
                    
                    var objectAtoms = AtomizeDetectedObjects(detectedObjects, imageIdAtom.ContentHash, compositions);
                    atoms.AddRange(objectAtoms);
                    
                    if (detectedObjects.Any())
                    {
                        warnings.Add($"Detected {detectedObjects.Count} objects");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Object detection failed: {ex.Message}");
                }
            }
            else if (options.DetectObjects && _objectDetectionService == null)
            {
                warnings.Add("Object detection requested but service not available");
            }

            // 4. SCENE ANALYSIS - Extract semantic tags, captions, colors (opt-in)
            if (options.AnalyzeScene && _sceneAnalysisService != null)
            {
                try
                {
                    var sceneInfo = await _sceneAnalysisService!.AnalyzeSceneAsync(imageData, cancellationToken);
                    var sceneAtoms = AtomizeSceneInfo(sceneInfo, imageIdAtom.ContentHash, compositions);
                    atoms.AddRange(sceneAtoms);
                    
                    if (sceneInfo.Tags.Any() || !string.IsNullOrEmpty(sceneInfo.Caption))
                    {
                        warnings.Add($"Scene analysis: {sceneInfo.Tags.Count} tags, caption: {!string.IsNullOrEmpty(sceneInfo.Caption)}");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Scene analysis failed: {ex.Message}");
                }
            }
            else if (options.AnalyzeScene && _sceneAnalysisService == null)
            {
                warnings.Add("Scene analysis requested but service not available");
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count(),
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(EnhancedImageAtomizer),
                    DetectedFormat = $"Image {imageInfo.Width}x{imageInfo.Height} ({imageInfo.Format})",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Enhanced image atomization failed: {ex.Message}");
            throw;
        }
    }

    private List<AtomData> AtomizePixels(
        ImageInfo imageInfo,
        byte[] parentHash,
        List<AtomComposition> compositions,
        CancellationToken cancellationToken)
    {
        var atoms = new List<AtomData>();
        var pixelHashes = new HashSet<string>();
        int pixelIndex = 0;

        for (int y = 0; y < imageInfo.Height; y++)
        {
            for (int x = 0; x < imageInfo.Width; x++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pixel = imageInfo.GetPixel(x, y);
                var rgba = new byte[] { pixel.R, pixel.G, pixel.B, pixel.A };
                var pixelHash = SHA256.HashData(rgba);
                var pixelHashStr = Convert.ToBase64String(pixelHash);

                // Deduplicate identical pixels
                if (!pixelHashes.Contains(pixelHashStr))
                {
                    pixelHashes.Add(pixelHashStr);
                    
                    atoms.Add(new AtomData
                    {
                        AtomicValue = rgba,
                        ContentHash = pixelHash,
                        Modality = "image",
                        Subtype = "pixel-rgba",
                        ContentType = "image/x-raw-rgba",
                        CanonicalText = $"rgba({pixel.R},{pixel.G},{pixel.B},{pixel.A})",
                        Metadata = $"{{\"r\":{pixel.R},\"g\":{pixel.G},\"b\":{pixel.B},\"a\":{pixel.A}}}"
                    });
                }

                // Link image → pixel with spatial position
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentHash,
                    ComponentAtomHash = pixelHash,
                    SequenceIndex = pixelIndex++,
                    Position = new SpatialPosition { X = x, Y = y, Z = 0 }
                });
            }
        }

        return atoms;
    }

    private List<AtomData> AtomizeOcrResults(
        List<OcrRegion> ocrResults,
        byte[] parentHash,
        List<AtomComposition> compositions)
    {
        var atoms = new List<AtomData>();
        int regionIndex = 0;

        foreach (var region in ocrResults)
        {
            // Atomize each word/text region to individual characters
            var text = region.Text;
            var chars = text.ToCharArray();
            
            for (int i = 0; i < chars.Length; i++)
            {
                var charBytes = Encoding.UTF8.GetBytes(new[] { chars[i] });
                var charHash = SHA256.HashData(charBytes);
                
                // Check if this character atom already exists
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(charHash)))
                {
                    atoms.Add(new AtomData
                    {
                        AtomicValue = charBytes,
                        ContentHash = charHash,
                        Modality = "text",
                        Subtype = "ocr-char",
                        ContentType = "text/plain",
                        CanonicalText = chars[i].ToString(),
                        Metadata = $"{{\"source\":\"ocr\",\"confidence\":{region.Confidence:F2}}}"
                    });
                }

                // Link image → character with spatial position from OCR region
                // X,Y = character position within region + region offset
                // Z = region index (layering multiple text regions)
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentHash,
                    ComponentAtomHash = charHash,
                    SequenceIndex = regionIndex * 10000 + i, // Ensure uniqueness across regions
                    Position = new SpatialPosition
                    {
                        X = region.BoundingBox.X + (int)(region.BoundingBox.Width * (i / (double)chars.Length)),
                        Y = region.BoundingBox.Y + region.BoundingBox.Height / 2,
                        Z = regionIndex // Layer index
                    }
                });
            }

            regionIndex++;
        }

        return atoms;
    }

    private List<AtomData> AtomizeDetectedObjects(
        List<DetectedObject> detectedObjects,
        byte[] parentHash,
        List<AtomComposition> compositions)
    {
        var atoms = new List<AtomData>();
        int objectIndex = 0;

        foreach (var obj in detectedObjects)
        {
            // Atomize object label to characters
            var labelChars = obj.Label.ToCharArray();
            
            for (int i = 0; i < labelChars.Length; i++)
            {
                var charBytes = Encoding.UTF8.GetBytes(new[] { labelChars[i] });
                var charHash = SHA256.HashData(charBytes);
                
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(charHash)))
                {
                    atoms.Add(new AtomData
                    {
                        AtomicValue = charBytes,
                        ContentHash = charHash,
                        Modality = "text",
                        Subtype = "object-label-char",
                        ContentType = "text/plain",
                        CanonicalText = labelChars[i].ToString(),
                        Metadata = $"{{\"source\":\"object-detection\",\"confidence\":{obj.Confidence:F2}}}"
                    });
                }

                // Link image → character with spatial position from object bounding box
                // Position = center of detected object's bounding box
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentHash,
                    ComponentAtomHash = charHash,
                    SequenceIndex = 1000000 + objectIndex * 1000 + i,
                    Position = new SpatialPosition
                    {
                        X = obj.BoundingBox.X + obj.BoundingBox.Width / 2,
                        Y = obj.BoundingBox.Y + obj.BoundingBox.Height / 2,
                        Z = 100 + objectIndex // Higher Z to separate from OCR layer
                    }
                });
            }

            objectIndex++;
        }

        return atoms;
    }

    private List<AtomData> AtomizeSceneInfo(
        SceneInfo sceneInfo,
        byte[] parentHash,
        List<AtomComposition> compositions)
    {
        var atoms = new List<AtomData>();
        int sequenceIndex = 2000000; // High base to avoid collisions

        // Atomize caption to characters
        if (!string.IsNullOrEmpty(sceneInfo.Caption))
        {
            var captionChars = sceneInfo.Caption.ToCharArray();
            
            for (int i = 0; i < captionChars.Length; i++)
            {
                var charBytes = Encoding.UTF8.GetBytes(new[] { captionChars[i] });
                var charHash = SHA256.HashData(charBytes);
                
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(charHash)))
                {
                    atoms.Add(new AtomData
                    {
                        AtomicValue = charBytes,
                        ContentHash = charHash,
                        Modality = "text",
                        Subtype = "caption-char",
                        ContentType = "text/plain",
                        CanonicalText = captionChars[i].ToString(),
                        Metadata = $"{{\"source\":\"scene-caption\",\"confidence\":{sceneInfo.CaptionConfidence:F2}}}"
                    });
                }

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentHash,
                    ComponentAtomHash = charHash,
                    SequenceIndex = sequenceIndex++,
                    Position = new SpatialPosition { X = i, Y = 0, Z = 200 } // Caption layer
                });
            }
        }

        // Atomize tags to characters
        int tagIndex = 0;
        foreach (var tag in sceneInfo.Tags)
        {
            var tagChars = tag.Name.ToCharArray();
            
            for (int i = 0; i < tagChars.Length; i++)
            {
                var charBytes = Encoding.UTF8.GetBytes(new[] { tagChars[i] });
                var charHash = SHA256.HashData(charBytes);
                
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(charHash)))
                {
                    atoms.Add(new AtomData
                    {
                        AtomicValue = charBytes,
                        ContentHash = charHash,
                        Modality = "text",
                        Subtype = "tag-char",
                        ContentType = "text/plain",
                        CanonicalText = tagChars[i].ToString(),
                        Metadata = $"{{\"source\":\"scene-tag\",\"confidence\":{tag.Confidence:F2}}}"
                    });
                }

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentHash,
                    ComponentAtomHash = charHash,
                    SequenceIndex = sequenceIndex++,
                    Position = new SpatialPosition { X = i, Y = tagIndex, Z = 201 } // Tag layer
                });
            }
            
            tagIndex++;
        }

        // Atomize dominant colors
        int colorIndex = 0;
        foreach (var color in sceneInfo.DominantColors)
        {
            var rgba = new byte[] { color.R, color.G, color.B, 255 };
            var colorHash = SHA256.HashData(rgba);
            
            if (!atoms.Any(a => a.ContentHash.SequenceEqual(colorHash)))
            {
                atoms.Add(new AtomData
                {
                    AtomicValue = rgba,
                    ContentHash = colorHash,
                    Modality = "image",
                    Subtype = "dominant-color",
                    ContentType = "image/x-raw-rgba",
                    CanonicalText = $"rgb({color.R},{color.G},{color.B})",
                    Metadata = $"{{\"source\":\"scene-analysis\",\"percentage\":{color.Percentage:F2}}}"
                });
            }

            compositions.Add(new AtomComposition
            {
                ParentAtomHash = parentHash,
                ComponentAtomHash = colorHash,
                SequenceIndex = sequenceIndex++,
                Position = new SpatialPosition { X = colorIndex, Y = 0, Z = 202 } // Color layer
            });
            
            colorIndex++;
        }

        return atoms;
    }

    private async Task<ImageInfo> DecodeImageAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        // This is a placeholder - actual implementation would use System.Drawing, SkiaSharp, or ImageSharp
        // For now, return mock data
        await Task.CompletedTask;
        
        // TODO: Implement actual image decoding
        // Use System.Drawing.Image, SkiaSharp.SKBitmap, or SixLabors.ImageSharp.Image
        throw new NotImplementedException("Image decoding not implemented - integrate System.Drawing, SkiaSharp, or ImageSharp");
    }
}

// Supporting types for image processing services

public interface IOcrService
{
    Task<List<OcrRegion>> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken);
}

public interface IObjectDetectionService
{
    Task<List<DetectedObject>> DetectObjectsAsync(byte[] imageData, CancellationToken cancellationToken);
}

public interface ISceneAnalysisService
{
    Task<SceneInfo> AnalyzeSceneAsync(byte[] imageData, CancellationToken cancellationToken);
}

public class OcrRegion
{
    public required string Text { get; set; }
    public required BoundingBox BoundingBox { get; set; }
    public required float Confidence { get; set; }
}

public class DetectedObject
{
    public required string Label { get; set; }
    public required BoundingBox BoundingBox { get; set; }
    public required float Confidence { get; set; }
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

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ImageInfo
{
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required string Format { get; set; }
    public required Func<int, int, Pixel> GetPixel { get; set; }
}

public struct Pixel
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }
}
