using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IGenerationStreams
{
    Guid StreamId { get; set; }
    long GenerationStreamId { get; set; }
    int? ModelId { get; set; }
    string? Scope { get; set; }
    string? Model { get; set; }
    string? GeneratedAtomIds { get; set; }
    byte[]? ProvenanceStream { get; set; }
    string? ContextMetadata { get; set; }
    int TenantId { get; set; }
    DateTime CreatedUtc { get; set; }
    ICollection<GenerationStreamSegment> GenerationStreamSegment { get; set; }
    Model? ModelNavigation { get; set; }
}
