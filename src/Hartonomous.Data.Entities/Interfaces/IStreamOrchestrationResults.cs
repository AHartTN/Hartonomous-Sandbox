using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IStreamOrchestrationResults
{
    int Id { get; set; }
    string SensorType { get; set; }
    DateTime TimeWindowStart { get; set; }
    DateTime TimeWindowEnd { get; set; }
    string AggregationLevel { get; set; }
    byte[]? ComponentStream { get; set; }
    int? ComponentCount { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
    ICollection<EventAtoms> EventAtoms { get; set; }
    ICollection<EventGenerationResults> EventGenerationResults { get; set; }
}
