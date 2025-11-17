using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class Atoms : IAtoms
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

    public virtual ICollection<AtomCompositions> AtomCompositionsComponentAtom { get; set; } = new List<AtomCompositions>();

    public virtual ICollection<AtomCompositions> AtomCompositionsParentAtom { get; set; } = new List<AtomCompositions>();

    public virtual ICollection<AtomConcepts> AtomConcepts { get; set; } = new List<AtomConcepts>();

    public virtual ICollection<AtomEmbeddings> AtomEmbeddings { get; set; } = new List<AtomEmbeddings>();

    public virtual ICollection<AtomRelations> AtomRelationsSourceAtom { get; set; } = new List<AtomRelations>();

    public virtual ICollection<AtomRelations> AtomRelationsTargetAtom { get; set; } = new List<AtomRelations>();

    public virtual ICollection<EventAtoms> EventAtoms { get; set; } = new List<EventAtoms>();

    public virtual ICollection<IngestionJobAtoms> IngestionJobAtoms { get; set; } = new List<IngestionJobAtoms>();

    public virtual ICollection<IngestionJobs> IngestionJobs { get; set; } = new List<IngestionJobs>();

    public virtual ICollection<ModelLayers> ModelLayers { get; set; } = new List<ModelLayers>();

    public virtual ICollection<TenantAtoms> TenantAtoms { get; set; } = new List<TenantAtoms>();

    public virtual ICollection<TensorAtomCoefficients> TensorAtomCoefficients { get; set; } = new List<TensorAtomCoefficients>();

    public virtual ICollection<TensorAtoms> TensorAtoms { get; set; } = new List<TensorAtoms>();
}
