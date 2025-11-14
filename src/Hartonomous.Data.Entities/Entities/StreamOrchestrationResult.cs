using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class StreamOrchestrationResult : IStreamOrchestrationResult
{
    public int Id { get; set; }

    public string SensorType { get; set; } = null!;

    public DateTime TimeWindowStart { get; set; }

    public DateTime TimeWindowEnd { get; set; }

    public string AggregationLevel { get; set; } = null!;

    public byte[]? ComponentStream { get; set; }

    public int? ComponentCount { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<EventAtom> EventAtoms { get; set; } = new List<EventAtom>();

    public virtual ICollection<EventGenerationResult> EventGenerationResults { get; set; } = new List<EventGenerationResult>();
}
