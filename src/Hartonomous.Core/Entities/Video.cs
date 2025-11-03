using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Primary video storage without embedded binary payloads.
/// Maps to dbo.Videos table from 02_MultiModalData.sql.
/// </summary>
public sealed class Video
{
    public long VideoId { get; set; }
    public string? SourcePath { get; set; }
    public int Fps { get; set; }
    public long DurationMs { get; set; }
    public int ResolutionWidth { get; set; }
    public int ResolutionHeight { get; set; }
    public long NumFrames { get; set; }
    public string? Format { get; set; }

    // Global representation
    public SqlVector<float>? GlobalEmbedding { get; set; } // VECTOR(768)
    public int? GlobalEmbeddingDim { get; set; }

    // Metadata
    public string? Metadata { get; set; } // JSON

    public DateTime? IngestionDate { get; set; }

    // Navigation properties
    public ICollection<VideoFrame> Frames { get; set; } = new List<VideoFrame>();
}
