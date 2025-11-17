using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class Atom : IAtom
{
    public long AtomId { get; set; }

    public int TenantId { get; set; }

    public string Modality { get; set; } = null!;

    public string? Subtype { get; set; }

    public byte[] ContentHash { get; set; } = null!;

    public string? ContentType { get; set; }

    public string? SourceType { get; set; }

    public string? SourceUri { get; set; }

    public string? CanonicalText { get; set; }

    public string? Metadata { get; set; }

    public byte[]? AtomicValue { get; set; }

    public long ReferenceCount { get; set; }

    public virtual ICollection<AtomComposition> AtomCompositionComponentAtoms { get; set; } = new List<AtomComposition>();

    public virtual ICollection<AtomComposition> AtomCompositionParentAtoms { get; set; } = new List<AtomComposition>();

    public virtual ICollection<AtomConcept> AtomConcepts { get; set; } = new List<AtomConcept>();

    public virtual ICollection<AtomEmbedding> AtomEmbeddings { get; set; } = new List<AtomEmbedding>();

    public virtual ICollection<AtomRelation> AtomRelationSourceAtoms { get; set; } = new List<AtomRelation>();

    public virtual ICollection<AtomRelation> AtomRelationTargetAtoms { get; set; } = new List<AtomRelation>();

    public virtual ICollection<EventAtom> EventAtoms { get; set; } = new List<EventAtom>();

    public virtual ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; } = new List<IngestionJobAtom>();

    public virtual ICollection<IngestionJob> IngestionJobs { get; set; } = new List<IngestionJob>();

    public virtual ICollection<ModelLayer> ModelLayers { get; set; } = new List<ModelLayer>();

    public virtual ICollection<TenantAtom> TenantAtoms { get; set; } = new List<TenantAtom>();

    public virtual ICollection<TensorAtomCoefficient> TensorAtomCoefficients { get; set; } = new List<TensorAtomCoefficient>();

    public virtual ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();
}
