using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public partial class Atom : IAtom
{
    public long AtomId { get; set; }

    public byte[] ContentHash { get; set; } = null!;

    public string Modality { get; set; } = null!;

    public string? Subtype { get; set; }

    public string? SourceUri { get; set; }

    public string? SourceType { get; set; }

    public byte[]? AtomicValue { get; set; }

    public string? CanonicalText { get; set; }

    public string? Content { get; set; }

    public string? ContentType { get; set; }

    public string? PayloadLocator { get; set; }

    public byte[]? ComponentStream { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public int TenantId { get; set; }

    public long ReferenceCount { get; set; }

    public Geometry? SpatialKey { get; set; }

    public Geometry? SpatialGeography { get; set; }

    public virtual ICollection<AtomComposition> AtomCompositionComponentAtoms { get; set; } = new List<AtomComposition>();

    public virtual ICollection<AtomComposition> AtomCompositionSourceAtoms { get; set; } = new List<AtomComposition>();

    public virtual ICollection<AtomConcept> AtomConcepts { get; set; } = new List<AtomConcept>();

    public virtual ICollection<AtomEmbedding> AtomEmbeddings { get; set; } = new List<AtomEmbedding>();

    public virtual ICollection<AtomPayloadStore> AtomPayloadStores { get; set; } = new List<AtomPayloadStore>();

    public virtual ICollection<AtomRelation> AtomRelationSourceAtoms { get; set; } = new List<AtomRelation>();

    public virtual ICollection<AtomRelation> AtomRelationTargetAtoms { get; set; } = new List<AtomRelation>();

    public virtual AtomsLob? AtomsLob { get; set; }

    public virtual ICollection<AudioFrame> AudioFrames { get; set; } = new List<AudioFrame>();

    public virtual ICollection<EventAtom> EventAtoms { get; set; } = new List<EventAtom>();

    public virtual ICollection<ImagePatch> ImagePatches { get; set; } = new List<ImagePatch>();

    public virtual ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; } = new List<IngestionJobAtom>();

    public virtual ICollection<ModelLayer> ModelLayers { get; set; } = new List<ModelLayer>();

    public virtual ICollection<TenantAtom> TenantAtoms { get; set; } = new List<TenantAtom>();

    public virtual ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();
}
