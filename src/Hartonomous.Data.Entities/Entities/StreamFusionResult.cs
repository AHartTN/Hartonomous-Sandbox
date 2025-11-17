using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class StreamFusionResult : IStreamFusionResult
{
    public int Id { get; set; }

    public string StreamIds { get; set; } = null!;

    public string FusionType { get; set; } = null!;

    public string? Weights { get; set; }

    public byte[]? FusedStream { get; set; }

    public int? ComponentCount { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }
}
