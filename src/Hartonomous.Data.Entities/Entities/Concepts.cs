using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class Concepts : IConcepts
{
    public long ConceptId { get; set; }

    public string ConceptName { get; set; } = null!;

    public string? Description { get; set; }

    public byte[] CentroidVector { get; set; } = null!;

    public byte[]? Centroid { get; set; }

    public Geometry? CentroidSpatialKey { get; set; }

    public Geometry? ConceptDomain { get; set; }

    public long? HilbertValue { get; set; }

    public int VectorDimension { get; set; }

    public int MemberCount { get; set; }

    public int AtomCount { get; set; }

    public double? CoherenceScore { get; set; }

    public double? Coherence { get; set; }

    public double? SeparationScore { get; set; }

    public int? SpatialBucket { get; set; }

    public string DiscoveryMethod { get; set; } = null!;

    public int ModelId { get; set; }

    public int TenantId { get; set; }

    public DateTime DiscoveredAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AtomConcepts> AtomConcepts { get; set; } = new List<AtomConcepts>();

    public virtual ICollection<ConceptEvolution> ConceptEvolution { get; set; } = new List<ConceptEvolution>();

    public virtual Model Model { get; set; } = null!;
}
