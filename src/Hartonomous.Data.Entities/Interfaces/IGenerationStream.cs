using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IGenerationStream
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
    ICollection<GenerationStreamSegment> GenerationStreamSegments { get; set; }
    Model? ModelNavigation { get; set; }
}
