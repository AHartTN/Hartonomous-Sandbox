using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IConceptEvolution
{
    long EvolutionId { get; set; }
    long ConceptId { get; set; }
    byte[] PreviousCentroid { get; set; }
    byte[] NewCentroid { get; set; }
    double CentroidShift { get; set; }
    int AtomCountDelta { get; set; }
    int MemberCountChange { get; set; }
    double? CoherenceDelta { get; set; }
    double? CoherenceChange { get; set; }
    string? EvolutionType { get; set; }
    string? EvolutionReason { get; set; }
    DateTime RecordedAt { get; set; }
    int TenantId { get; set; }
    Concept Concept { get; set; }
}
