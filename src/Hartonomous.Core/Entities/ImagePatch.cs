using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a fine-grained rectangular patch within an image for detailed spatial analysis.
/// Enables patch-based vision transformers, object detection, and local feature extraction.
/// Maps to dbo.ImagePatches table from 02_MultiModalData.sql.
/// </summary>
public sealed class ImagePatch
{
    /// <summary>
    /// Gets or sets the unique identifier for the image patch.
    /// </summary>
    public long PatchId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent image.
    /// </summary>
    public long ImageId { get; set; }

    /// <summary>
    /// Gets or sets the X coordinate of the patch's top-left corner (in pixels).
    /// </summary>
    public int PatchX { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate of the patch's top-left corner (in pixels).
    /// </summary>
    public int PatchY { get; set; }

    /// <summary>
    /// Gets or sets the width of the patch in pixels.
    /// </summary>
    public int PatchWidth { get; set; }

    /// <summary>
    /// Gets or sets the height of the patch in pixels.
    /// </summary>
    public int PatchHeight { get; set; }

    /// <summary>
    /// Gets or sets the geometric region (typically POLYGON) representing the patch's spatial extent.
    /// Enables spatial queries and overlap detection.
    /// </summary>
    public Geometry PatchRegion { get; set; } = null!;

    /// <summary>
    /// Gets or sets the embedding vector for this patch (typically from vision transformer or CNN).
    /// </summary>
    public SqlVector<float>? PatchEmbedding { get; set; }

    /// <summary>
    /// Gets or sets a POINT in RGB color space representing the dominant color of the patch.
    /// </summary>
    public Geometry? DominantColor { get; set; }

    /// <summary>
    /// Gets or sets the mean intensity value across all pixels in the patch.
    /// </summary>
    public float? MeanIntensity { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation of intensity values in the patch.
    /// </summary>
    public float? StdIntensity { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent image.
    /// </summary>
    public Image Image { get; set; } = null!;
}
