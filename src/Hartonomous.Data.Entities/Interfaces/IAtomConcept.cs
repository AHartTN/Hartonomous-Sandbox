using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IAtomConcept
{
    long AtomConceptId { get; set; }
    long AtomId { get; set; }
    long ConceptId { get; set; }
    double? Similarity { get; set; }
    bool IsPrimary { get; set; }
    double MembershipScore { get; set; }
    double? DistanceToCentroid { get; set; }
    DateTime AssignedAt { get; set; }
    int TenantId { get; set; }
    Atom Atom { get; set; }
    Concept Concept { get; set; }
}
