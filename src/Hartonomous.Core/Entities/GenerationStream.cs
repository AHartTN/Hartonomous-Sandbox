using System;

namespace Hartonomous.Core.Entities;

public class GenerationStream
{
    public Guid StreamId { get; set; }

    public string? Scope { get; set; }

    public string? Model { get; set; }

    public DateTime CreatedUtc { get; set; }

    public byte[] Stream { get; set; } = Array.Empty<byte>();

    public long PayloadSizeBytes { get; private set; }
}
