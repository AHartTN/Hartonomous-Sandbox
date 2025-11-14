using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public partial class Video : IVideo
{
    public long VideoId { get; set; }

    public string? SourcePath { get; set; }

    public int Fps { get; set; }

    public long DurationMs { get; set; }

    public int ResolutionWidth { get; set; }

    public int ResolutionHeight { get; set; }

    public long NumFrames { get; set; }

    public string? Format { get; set; }

    public SqlVector<float>? GlobalEmbedding { get; set; }

    public string? Metadata { get; set; }

    public DateTime? IngestionDate { get; set; }

    public virtual ICollection<VideoFrame> VideoFrames { get; set; } = new List<VideoFrame>();
}
