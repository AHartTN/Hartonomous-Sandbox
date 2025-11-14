using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class ConceptEvolution : IConceptEvolution
{
    public long EvolutionId { get; set; }

    public long ConceptId { get; set; }

    public byte[] PreviousCentroid { get; set; } = null!;

    public byte[] NewCentroid { get; set; } = null!;

    public double CentroidShift { get; set; }

    public int AtomCountDelta { get; set; }

    public int MemberCountChange { get; set; }

    public double? CoherenceDelta { get; set; }

    public double? CoherenceChange { get; set; }

    public string? EvolutionType { get; set; }

    public string? EvolutionReason { get; set; }

    public DateTime RecordedAt { get; set; }

    public int TenantId { get; set; }

    public virtual Concept Concept { get; set; } = null!;
}
