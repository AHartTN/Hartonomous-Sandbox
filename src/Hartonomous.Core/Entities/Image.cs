using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Primary image storage with spatial and vector representations.
/// Maps to dbo.Images table from 02_MultiModalData.sql.
/// </summary>
public sealed class Image
{
    public long ImageId { get; set; }
    public string? SourcePath { get; set; }
    public string? SourceUrl { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }
    public int Channels { get; set; } // 1 (grayscale), 3 (RGB), 4 (RGBA)
    public string? Format { get; set; } // 'png', 'jpg', 'bmp'

    // Spatial representations (pixels as geometry) - NetTopologySuite types
    public Geometry? PixelCloud { get; set; } // MULTIPOINT: representative pixels
    public Geometry? EdgeMap { get; set; } // LINESTRING: detected edges
    public Geometry? ObjectRegions { get; set; } // MULTIPOLYGON: segmented objects
    public Geometry? SaliencyRegions { get; set; } // POLYGON: attention regions

    // Vector representations
    public SqlVector<float>? GlobalEmbedding { get; set; } // VECTOR(1536)
    public int? GlobalEmbeddingDim { get; set; }

    // Metadata
    public string? Metadata { get; set; } // JSON

    public DateTime? IngestionDate { get; set; }
    public DateTime? LastAccessed { get; set; }
    public long AccessCount { get; set; }

    // Navigation properties
    public ICollection<ImagePatch> Patches { get; set; } = new List<ImagePatch>();
}
