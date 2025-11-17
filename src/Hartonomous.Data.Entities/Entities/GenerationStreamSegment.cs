using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities.Entities;

public partial class GenerationStreamSegment : IGenerationStreamSegment
{
    public long SegmentId { get; set; }

    public long GenerationStreamId { get; set; }

    public int SegmentOrdinal { get; set; }

    public string SegmentKind { get; set; } = null!;

    public string? ContentType { get; set; }

    public string? Metadata { get; set; }

    public byte[]? PayloadData { get; set; }

    public SqlVector<float>? EmbeddingVector { get; set; }

    public DateTime CreatedAt { get; set; }

    public int TenantId { get; set; }

    public virtual GenerationStream GenerationStream { get; set; } = null!;
}
