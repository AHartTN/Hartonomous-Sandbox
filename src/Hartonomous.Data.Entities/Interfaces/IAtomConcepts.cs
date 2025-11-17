using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtomConcepts
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
    Atoms Atom { get; set; }
    Concepts Concept { get; set; }
}
