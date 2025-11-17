using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class EventGenerationResult : IEventGenerationResult
{
    public int Id { get; set; }

    public int StreamId { get; set; }

    public string EventType { get; set; } = null!;

    public double Threshold { get; set; }

    public string ClusteringMethod { get; set; } = null!;

    public int EventsGenerated { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StreamOrchestrationResult Stream { get; set; } = null!;
}
