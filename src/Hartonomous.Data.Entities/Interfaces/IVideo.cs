using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public interface IVideo
{
    long VideoId { get; set; }
    string? SourcePath { get; set; }
    int Fps { get; set; }
    long DurationMs { get; set; }
    int ResolutionWidth { get; set; }
    int ResolutionHeight { get; set; }
    long NumFrames { get; set; }
    string? Format { get; set; }
    SqlVector<float>? GlobalEmbedding { get; set; }
    string? Metadata { get; set; }
    DateTime? IngestionDate { get; set; }
    ICollection<VideoFrame> VideoFrames { get; set; }
}
