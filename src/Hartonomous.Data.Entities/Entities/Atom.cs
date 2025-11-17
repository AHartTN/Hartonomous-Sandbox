using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

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

    public virtual ICollection<AtomComposition> AtomCompositionComponentAtom { get; set; } = new List<AtomComposition>();

    public virtual ICollection<AtomComposition> AtomCompositionParentAtom { get; set; } = new List<AtomComposition>();

    public virtual ICollection<AtomConcepts> AtomConcepts { get; set; } = new List<AtomConcepts>();

    public virtual ICollection<AtomEmbedding> AtomEmbedding { get; set; } = new List<AtomEmbedding>();

    public virtual ICollection<AtomRelation> AtomRelationSourceAtom { get; set; } = new List<AtomRelation>();

    public virtual ICollection<AtomRelation> AtomRelationTargetAtom { get; set; } = new List<AtomRelation>();

    public virtual ICollection<EventAtoms> EventAtoms { get; set; } = new List<EventAtoms>();

    public virtual ICollection<IngestionJob> IngestionJob { get; set; } = new List<IngestionJob>();

    public virtual ICollection<IngestionJobAtom> IngestionJobAtom { get; set; } = new List<IngestionJobAtom>();

    public virtual ICollection<ModelLayer> ModelLayer { get; set; } = new List<ModelLayer>();

    public virtual ICollection<TenantAtom> TenantAtom { get; set; } = new List<TenantAtom>();

    public virtual ICollection<TensorAtom> TensorAtom { get; set; } = new List<TensorAtom>();

    public virtual ICollection<TensorAtomCoefficient> TensorAtomCoefficient { get; set; } = new List<TensorAtomCoefficient>();
}
