using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class AtomConcept : IAtomConcept
{
    public long AtomConceptId { get; set; }

    public long AtomId { get; set; }

    public long ConceptId { get; set; }

    public double? Similarity { get; set; }

    public bool IsPrimary { get; set; }

    public double MembershipScore { get; set; }

    public double? DistanceToCentroid { get; set; }

    public DateTime AssignedAt { get; set; }

    public int TenantId { get; set; }

    public virtual Atom Atom { get; set; } = null!;

    public virtual Concept Concept { get; set; } = null!;
}
