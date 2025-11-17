using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class GenerationStreams : IGenerationStreams
{
    public Guid StreamId { get; set; }

    public long GenerationStreamId { get; set; }

    public int? ModelId { get; set; }

    public string? Scope { get; set; }

    public string? Model { get; set; }

    public string? GeneratedAtomIds { get; set; }

    public byte[]? ProvenanceStream { get; set; }

    public string? ContextMetadata { get; set; }

    public int TenantId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public virtual ICollection<GenerationStreamSegments> GenerationStreamSegments { get; set; } = new List<GenerationStreamSegments>();

    public virtual Models? ModelNavigation { get; set; }
}
