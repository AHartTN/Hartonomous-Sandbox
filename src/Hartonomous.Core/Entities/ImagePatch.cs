using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Fine-grained patch-level image data for detailed analysis.
/// Maps to dbo.ImagePatches table from 02_MultiModalData.sql.
/// </summary>
public sealed class ImagePatch
{
    public long PatchId { get; set; }
    public long ImageId { get; set; }

    // Patch coordinates
    public int PatchX { get; set; } // Top-left corner
    public int PatchY { get; set; }
    public int PatchWidth { get; set; }
    public int PatchHeight { get; set; }

    // Patch as spatial region - NetTopologySuite type
    public Geometry PatchRegion { get; set; } = null!;

    // Patch features
    public SqlVector<float>? PatchEmbedding { get; set; } // VECTOR(768)
    public Geometry? DominantColor { get; set; } // POINT(r, g, b) in color space

    // Statistics
    public float? MeanIntensity { get; set; }
    public float? StdIntensity { get; set; }

    // Navigation property
    public Image Image { get; set; } = null!;
}
