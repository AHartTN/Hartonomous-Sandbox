using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class StreamOrchestrationResults : IStreamOrchestrationResults
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

    public virtual ICollection<EventAtoms> EventAtoms { get; set; } = new List<EventAtoms>();

    public virtual ICollection<EventGenerationResults> EventGenerationResults { get; set; } = new List<EventGenerationResults>();
}
