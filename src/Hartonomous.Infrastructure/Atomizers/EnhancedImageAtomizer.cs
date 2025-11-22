using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Interfaces.Vision;
using Hartonomous.Core.Models.Vision;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Enhanced image atomizer with OCR, object detection, and scene analysis.
/// Atomizes: pixels (visual content) + extracted text (OCR) + detected objects + metadata.
/// </summary>
public class EnhancedImageAtomizer : BaseVisionAtomizer<byte[]>
{
    public EnhancedImageAtomizer(
        ILogger<EnhancedImageAtomizer> logger,
        IOcrService? ocrService = null,
        IObjectDetectionService? objectDetectionService = null,
        ISceneAnalysisService? sceneAnalysisService = null)
        : base(logger, ocrService, objectDetectionService, sceneAnalysisService)
    {
    }
    
    public override int Priority => 50;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        var imageTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/bmp", "image/webp", "image/tiff" };
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff" };
        
        return imageTypes.Contains(contentType?.ToLowerInvariant()) ||
               imageExtensions.Contains(fileExtension?.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(
        byte[] imageData,
        SourceMetadata source,
        ImageProcessingOptions options,
        CancellationToken cancellationToken)
    {
        return await AtomizeWithOptionsAsync(imageData, source, options, cancellationToken);
    }

    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        await AtomizeWithOptionsAsync(input, source, new ImageProcessingOptions(), cancellationToken);
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

        var imageInfo = await DecodeImageAsync(imageData, cancellationToken);
        var imageHash = CreateFileMetadataAtom(imageData, source, atoms);

        // 1. Atomize pixels
        AtomizePixels(imageInfo, imageHash, atoms, compositions, cancellationToken);

        // 2. OCR
        if (options.ExtractText)
        {
            var ocrResults = await ExecuteOcrAsync(imageData, warnings, cancellationToken);
            ProcessOcrResults(ocrResults, imageHash, atoms, compositions);
        }

        // 3. Object detection
        if (options.DetectObjects)
        {
            var detectedObjects = await ExecuteObjectDetectionAsync(imageData, warnings, cancellationToken);
            ProcessDetectedObjects(detectedObjects, imageHash, atoms, compositions);
        }

        // 4. Scene analysis
        if (options.AnalyzeScene)
        {
            var sceneInfo = await ExecuteSceneAnalysisAsync(imageData, warnings, cancellationToken);
            if (sceneInfo != null)
            {
                ProcessSceneInfo(sceneInfo, imageHash, atoms, compositions);
            }
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

    protected override string GetDetectedFormat() => "enhanced image with vision AI";
    protected override string GetModality() => "image";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        return System.Text.Encoding.UTF8.GetBytes($"enhanced-image:{source.FileName}:{input.Length}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        return $"{source.FileName ?? "image"} ({input.Length:N0} bytes, AI-enhanced)";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
        return $"{{\"type\":\"enhanced-image\",\"size\":{input.Length},\"fileName\":\"{source.FileName}\",\"aiProcessing\":true}}";
    }

    private void AtomizePixels(
        ImageInfo imageInfo,
        byte[] parentHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        CancellationToken cancellationToken)
    {
        var pixelHashes = new HashSet<string>();
        int pixelIndex = 0;

        for (int y = 0; y < imageInfo.Height; y++)
        {
            for (int x = 0; x < imageInfo.Width; x++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pixel = imageInfo.GetPixel(x, y);
                var rgba = new byte[] { pixel.R, pixel.G, pixel.B, pixel.A };
                var pixelHash = HashUtilities.ComputeSHA256(rgba);
                var pixelHashStr = Convert.ToBase64String(pixelHash);

                if (!pixelHashes.Contains(pixelHashStr))
                {
                    pixelHashes.Add(pixelHashStr);
                    
                    CreateContentAtom(
                        rgba,
                        "image",
                        "pixel-rgba",
                        $"rgba({pixel.R},{pixel.G},{pixel.B},{pixel.A})",
                        $"{{\"r\":{pixel.R},\"g\":{pixel.G},\"b\":{pixel.B},\"a\":{pixel.A}}}",
                        atoms);
                }

                CreateAtomComposition(parentHash, pixelHash, pixelIndex++, compositions, x: x, y: y, z: 0);
            }
        }
    }

    private async Task<ImageInfo> DecodeImageAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("Image decoding not implemented - integrate System.Drawing, SkiaSharp, or ImageSharp");
    }
}
