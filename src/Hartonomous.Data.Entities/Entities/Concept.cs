using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class Concept : IConcept
{
    public long ConceptId { get; set; }

    public string ConceptName { get; set; } = null!;

    public string? Description { get; set; }

    public byte[] CentroidVector { get; set; } = null!;

    public byte[]? Centroid { get; set; }

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

    public virtual ICollection<AtomConcept> AtomConcepts { get; set; } = new List<AtomConcept>();

    public virtual ICollection<ConceptEvolution> ConceptEvolutions { get; set; } = new List<ConceptEvolution>();

    public virtual Model Model { get; set; } = null!;
}
