using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IConcepts
{
    long ConceptId { get; set; }
    string ConceptName { get; set; }
    string? Description { get; set; }
    byte[] CentroidVector { get; set; }
    byte[]? Centroid { get; set; }
    Geometry? CentroidSpatialKey { get; set; }
    Geometry? ConceptDomain { get; set; }
    long? HilbertValue { get; set; }
    int VectorDimension { get; set; }
    int MemberCount { get; set; }
    int AtomCount { get; set; }
    double? CoherenceScore { get; set; }
    double? Coherence { get; set; }
    double? SeparationScore { get; set; }
    int? SpatialBucket { get; set; }
    string DiscoveryMethod { get; set; }
    int ModelId { get; set; }
    int TenantId { get; set; }
    DateTime DiscoveredAt { get; set; }
    DateTime? LastUpdatedAt { get; set; }
    bool IsActive { get; set; }
    ICollection<AtomConcepts> AtomConcepts { get; set; }
    ICollection<ConceptEvolution> ConceptEvolution { get; set; }
    Model Model { get; set; }
}
