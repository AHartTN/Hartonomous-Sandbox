using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

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
    ICollection<AtomComposition> AtomCompositionComponentAtoms { get; set; }
    ICollection<AtomComposition> AtomCompositionParentAtoms { get; set; }
    ICollection<AtomConcept> AtomConcepts { get; set; }
    ICollection<AtomEmbedding> AtomEmbeddings { get; set; }
    ICollection<AtomRelation> AtomRelationSourceAtoms { get; set; }
    ICollection<AtomRelation> AtomRelationTargetAtoms { get; set; }
    ICollection<EventAtom> EventAtoms { get; set; }
    ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; }
    ICollection<IngestionJob> IngestionJobs { get; set; }
    ICollection<ModelLayer> ModelLayers { get; set; }
    ICollection<TenantAtom> TenantAtoms { get; set; }
    ICollection<TensorAtomCoefficient> TensorAtomCoefficients { get; set; }
    ICollection<TensorAtom> TensorAtoms { get; set; }
}
