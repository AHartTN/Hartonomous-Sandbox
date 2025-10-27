using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Per-frame video spatial data (like images).
/// Maps to dbo.VideoFrames table from 02_MultiModalData.sql.
/// </summary>
public sealed class VideoFrame
{
    public long FrameId { get; set; }
    public long VideoId { get; set; }
    public long FrameNumber { get; set; }
    public long TimestampMs { get; set; }

    // Frame as spatial data (like ImagePatches) - NetTopologySuite types
    public Geometry? PixelCloud { get; set; }
    public Geometry? ObjectRegions { get; set; }

    // Motion information
    public Geometry? MotionVectors { get; set; } // MULTILINESTRING showing pixel movement
    public Geometry? OpticalFlow { get; set; } // Vector field representation

    // Frame embedding
    public SqlVector<float>? FrameEmbedding { get; set; } // VECTOR(768)

    // Frame similarity hash (for deduplication)
    public byte[]? PerceptualHash { get; set; }

    // Navigation property
    public Video Video { get; set; } = null!;
}
