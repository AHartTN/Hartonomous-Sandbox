using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public interface IGenerationStreamSegments
{
    long SegmentId { get; set; }
    long GenerationStreamId { get; set; }
    int SegmentOrdinal { get; set; }
    string SegmentKind { get; set; }
    string? ContentType { get; set; }
    string? Metadata { get; set; }
    byte[]? PayloadData { get; set; }
    SqlVector<float>? EmbeddingVector { get; set; }
    DateTime CreatedAt { get; set; }
    int TenantId { get; set; }
    GenerationStreams GenerationStream { get; set; }
}
