using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a video with metadata and global embeddings, without embedded binary payloads.
/// Video frames are stored separately for efficient temporal and spatial queries.
/// Maps to dbo.Videos table from 02_MultiModalData.sql.
/// </summary>
public sealed class Video
{
    /// <summary>
    /// Gets or sets the unique identifier for the video.
    /// </summary>
    public long VideoId { get; set; }

    /// <summary>
    /// Gets or sets the file system path where the video is stored.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Gets or sets the frames per second (FPS) of the video.
    /// </summary>
    public int Fps { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the video in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the width of the video resolution in pixels.
    /// </summary>
    public int ResolutionWidth { get; set; }

    /// <summary>
    /// Gets or sets the height of the video resolution in pixels.
    /// </summary>
    public int ResolutionHeight { get; set; }

    /// <summary>
    /// Gets or sets the total number of frames in the video.
    /// </summary>
    public long NumFrames { get; set; }

    /// <summary>
    /// Gets or sets the video format/codec (e.g., 'mp4', 'avi', 'webm', 'h264', 'vp9').
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the global video embedding vector (typically from video transformer or 3D CNN).
    /// </summary>
    public SqlVector<float>? GlobalEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the dimensionality of the global embedding vector.
    /// </summary>
    public int? GlobalEmbeddingDim { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., detected scenes, audio tracks, subtitles).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the video was ingested into the system.
    /// </summary>
    public DateTime? IngestionDate { get; set; }

    /// <summary>
    /// Gets or sets the collection of individual video frames.
    /// </summary>
    public ICollection<VideoFrame> Frames { get; set; } = new List<VideoFrame>();
}
