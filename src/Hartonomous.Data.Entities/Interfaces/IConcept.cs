using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities.Entities;

public interface IConcept
{
    long ConceptId { get; set; }
    int TenantId { get; set; }
    string ConceptName { get; set; }
    string? Description { get; set; }
    string? ConceptType { get; set; }
    long? ParentConceptId { get; set; }
    byte[]? CentroidVector { get; set; }
    Geometry? Domain { get; set; }
    double? Radius { get; set; }
    int AtomCount { get; set; }
    decimal? Confidence { get; set; }
    string? Metadata { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    ICollection<Atom> Atoms { get; set; }
    ICollection<Concept> InverseParentConcept { get; set; }
    Concept? ParentConcept { get; set; }
}
