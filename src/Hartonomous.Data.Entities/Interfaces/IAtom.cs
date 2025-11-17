using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtom
{
    long AtomId { get; set; }
    int TenantId { get; set; }
    string Modality { get; set; }
    string? Subtype { get; set; }
    byte[] ContentHash { get; set; }
    string? ContentType { get; set; }
    string? SourceType { get; set; }
    string? SourceUri { get; set; }
    string? CanonicalText { get; set; }
    string? Metadata { get; set; }
    byte[]? AtomicValue { get; set; }
    long ReferenceCount { get; set; }
    ICollection<AtomComposition> AtomCompositionComponentAtom { get; set; }
    ICollection<AtomComposition> AtomCompositionParentAtom { get; set; }
    ICollection<AtomConcepts> AtomConcepts { get; set; }
    ICollection<AtomEmbedding> AtomEmbedding { get; set; }
    ICollection<AtomRelation> AtomRelationSourceAtom { get; set; }
    ICollection<AtomRelation> AtomRelationTargetAtom { get; set; }
    ICollection<EventAtoms> EventAtoms { get; set; }
    ICollection<IngestionJob> IngestionJob { get; set; }
    ICollection<IngestionJobAtom> IngestionJobAtom { get; set; }
    ICollection<ModelLayer> ModelLayer { get; set; }
    ICollection<TenantAtom> TenantAtom { get; set; }
    ICollection<TensorAtom> TensorAtom { get; set; }
    ICollection<TensorAtomCoefficient> TensorAtomCoefficient { get; set; }
}
