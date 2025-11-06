using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a single frame from a video with spatial and motion analysis data.
/// Stores geometric representations of frame content and motion vectors for temporal analysis.
/// Maps to dbo.VideoFrames table from 02_MultiModalData.sql.
/// </summary>
public sealed class VideoFrame
{
    /// <summary>
    /// Gets or sets the unique identifier for the video frame.
    /// </summary>
    public long FrameId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the parent video.
    /// </summary>
    public long VideoId { get; set; }

    /// <summary>
    /// Gets or sets the sequential frame number within the video (0-based).
    /// </summary>
    public long FrameNumber { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of this frame in milliseconds from the start of the video.
    /// </summary>
    public long TimestampMs { get; set; }

    /// <summary>
    /// Gets or sets a MULTIPOINT geometry representing representative pixels in the frame.
    /// </summary>
    public Geometry? PixelCloud { get; set; }

    /// <summary>
    /// Gets or sets a MULTIPOLYGON geometry representing segmented objects in the frame.
    /// </summary>
    public Geometry? ObjectRegions { get; set; }

    /// <summary>
    /// Gets or sets a MULTILINESTRING geometry showing pixel movement vectors between frames.
    /// Each line represents the displacement of a tracked feature point.
    /// </summary>
    public Geometry? MotionVectors { get; set; }

    /// <summary>
    /// Gets or sets a vector field representation of optical flow across the frame.
    /// Encodes dense motion estimation for every pixel or region.
    /// </summary>
    public Geometry? OpticalFlow { get; set; }

    /// <summary>
    /// Gets or sets the frame embedding vector (typically from video model or image encoder).
    /// </summary>
    public SqlVector<float>? FrameEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the perceptual hash for frame deduplication and similarity detection.
    /// </summary>
    public byte[]? PerceptualHash { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent video.
    /// </summary>
    public Video Video { get; set; } = null!;
}
