using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Interfaces.Vision;
using Hartonomous.Core.Models.Vision;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Base class for atomizers that use vision services (OCR, object detection, scene analysis).
/// Extends BaseAtomizer with optional vision service integration.
/// </summary>
public abstract class BaseVisionAtomizer<TInput> : BaseAtomizer<TInput>
{
    protected readonly IOcrService? OcrService;
    protected readonly IObjectDetectionService? ObjectDetectionService;
    protected readonly ISceneAnalysisService? SceneAnalysisService;

    protected BaseVisionAtomizer(
        ILogger logger,
        IOcrService? ocrService = null,
        IObjectDetectionService? objectDetectionService = null,
        ISceneAnalysisService? sceneAnalysisService = null)
        : base(logger)
    {
        OcrService = ocrService;
        ObjectDetectionService = objectDetectionService;
        SceneAnalysisService = sceneAnalysisService;
    }

    /// <summary>
    /// Process OCR results and create text atoms with spatial positioning.
    /// </summary>
    protected void ProcessOcrResults(
        List<OcrRegion> ocrResults,
        byte[] parentHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        int baseSequenceIndex = 0)
    {
        int regionIndex = 0;

        foreach (var region in ocrResults)
        {
            var text = region.Text;
            var chars = text.ToCharArray();
            
            for (int i = 0; i < chars.Length; i++)
            {
                var charBytes = System.Text.Encoding.UTF8.GetBytes(new[] { chars[i] });
                var charHash = CreateContentAtom(
                    charBytes,
                    "text",
                    "ocr-char",
                    chars[i].ToString(),
                    $"{{\"source\":\"ocr\",\"confidence\":{region.Confidence:F2}}}",
                    atoms);

                CreateAtomComposition(
                    parentHash,
                    charHash,
                    baseSequenceIndex + regionIndex * 10000 + i,
                    compositions,
                    x: region.BoundingBox.X + (int)(region.BoundingBox.Width * (i / (double)chars.Length)),
                    y: region.BoundingBox.Y + region.BoundingBox.Height / 2,
                    z: regionIndex);
            }

            regionIndex++;
        }
    }

    /// <summary>
    /// Process detected objects and create label atoms with spatial positioning.
    /// </summary>
    protected void ProcessDetectedObjects(
        List<DetectedObject> detectedObjects,
        byte[] parentHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        int baseSequenceIndex = 1000000)
    {
        int objectIndex = 0;

        foreach (var obj in detectedObjects)
        {
            var labelChars = obj.Label.ToCharArray();
            
            for (int i = 0; i < labelChars.Length; i++)
            {
                var charBytes = System.Text.Encoding.UTF8.GetBytes(new[] { labelChars[i] });
                var charHash = CreateContentAtom(
                    charBytes,
                    "text",
                    "object-label-char",
                    labelChars[i].ToString(),
                    $"{{\"source\":\"object-detection\",\"confidence\":{obj.Confidence:F2}}}",
                    atoms);

                CreateAtomComposition(
                    parentHash,
                    charHash,
                    baseSequenceIndex + objectIndex * 1000 + i,
                    compositions,
                    x: obj.BoundingBox.X + obj.BoundingBox.Width / 2,
                    y: obj.BoundingBox.Y + obj.BoundingBox.Height / 2,
                    z: 100 + objectIndex);
            }

            objectIndex++;
        }
    }

    /// <summary>
    /// Process scene analysis results and create caption/tag/color atoms.
    /// </summary>
    protected void ProcessSceneInfo(
        SceneInfo sceneInfo,
        byte[] parentHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        int baseSequenceIndex = 2000000)
    {
        int sequenceIndex = baseSequenceIndex;

        // Caption
        if (!string.IsNullOrEmpty(sceneInfo.Caption))
        {
            var captionChars = sceneInfo.Caption.ToCharArray();
            
            for (int i = 0; i < captionChars.Length; i++)
            {
                var charBytes = System.Text.Encoding.UTF8.GetBytes(new[] { captionChars[i] });
                var charHash = CreateContentAtom(
                    charBytes,
                    "text",
                    "caption-char",
                    captionChars[i].ToString(),
                    $"{{\"source\":\"scene-caption\",\"confidence\":{sceneInfo.CaptionConfidence:F2}}}",
                    atoms);

                CreateAtomComposition(parentHash, charHash, sequenceIndex++, compositions, x: i, y: 0, z: 200);
            }
        }

        // Tags
        int tagIndex = 0;
        foreach (var tag in sceneInfo.Tags)
        {
            var tagChars = tag.Name.ToCharArray();
            
            for (int i = 0; i < tagChars.Length; i++)
            {
                var charBytes = System.Text.Encoding.UTF8.GetBytes(new[] { tagChars[i] });
                var charHash = CreateContentAtom(
                    charBytes,
                    "text",
                    "tag-char",
                    tagChars[i].ToString(),
                    $"{{\"source\":\"scene-tag\",\"confidence\":{tag.Confidence:F2}}}",
                    atoms);

                CreateAtomComposition(parentHash, charHash, sequenceIndex++, compositions, x: i, y: tagIndex, z: 201);
            }
            
            tagIndex++;
        }

        // Dominant colors
        int colorIndex = 0;
        foreach (var color in sceneInfo.DominantColors)
        {
            var rgba = new byte[] { color.R, color.G, color.B, 255 };
            var colorHash = CreateContentAtom(
                rgba,
                "image",
                "dominant-color",
                $"rgb({color.R},{color.G},{color.B})",
                $"{{\"source\":\"scene-analysis\",\"percentage\":{color.Percentage:F2}}}",
                atoms);

            CreateAtomComposition(parentHash, colorHash, sequenceIndex++, compositions, x: colorIndex, y: 0, z: 202);
            colorIndex++;
        }
    }

    /// <summary>
    /// Execute OCR with error handling and warnings.
    /// </summary>
    protected async Task<List<OcrRegion>> ExecuteOcrAsync(
        byte[] imageData,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (OcrService == null)
        {
            warnings.Add("OCR requested but service not available");
            return new List<OcrRegion>();
        }

        try
        {
            var results = await OcrService.ExtractTextAsync(imageData, cancellationToken);
            if (results.Count > 0)
            {
                warnings.Add($"OCR extracted {results.Count} text regions");
            }
            return results;
        }
        catch (System.Exception ex)
        {
            warnings.Add($"OCR failed: {ex.Message}");
            return new List<OcrRegion>();
        }
    }

    /// <summary>
    /// Execute object detection with error handling and warnings.
    /// </summary>
    protected async Task<List<DetectedObject>> ExecuteObjectDetectionAsync(
        byte[] imageData,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (ObjectDetectionService == null)
        {
            warnings.Add("Object detection requested but service not available");
            return new List<DetectedObject>();
        }

        try
        {
            var results = await ObjectDetectionService.DetectObjectsAsync(imageData, cancellationToken);
            if (results.Count > 0)
            {
                warnings.Add($"Detected {results.Count} objects");
            }
            return results;
        }
        catch (System.Exception ex)
        {
            warnings.Add($"Object detection failed: {ex.Message}");
            return new List<DetectedObject>();
        }
    }

    /// <summary>
    /// Execute scene analysis with error handling and warnings.
    /// </summary>
    protected async Task<SceneInfo?> ExecuteSceneAnalysisAsync(
        byte[] imageData,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (SceneAnalysisService == null)
        {
            warnings.Add("Scene analysis requested but service not available");
            return null;
        }

        try
        {
            var result = await SceneAnalysisService.AnalyzeSceneAsync(imageData, cancellationToken);
            if (result.Tags.Count > 0 || !string.IsNullOrEmpty(result.Caption))
            {
                warnings.Add($"Scene analysis: {result.Tags.Count} tags, caption: {!string.IsNullOrEmpty(result.Caption)}");
            }
            return result;
        }
        catch (System.Exception ex)
        {
            warnings.Add($"Scene analysis failed: {ex.Message}");
            return null;
        }
    }
}
