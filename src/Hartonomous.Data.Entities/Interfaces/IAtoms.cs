using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtoms
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
    ICollection<AtomCompositions> AtomCompositionsComponentAtom { get; set; }
    ICollection<AtomCompositions> AtomCompositionsParentAtom { get; set; }
    ICollection<AtomConcepts> AtomConcepts { get; set; }
    ICollection<AtomEmbeddings> AtomEmbeddings { get; set; }
    ICollection<AtomRelations> AtomRelationsSourceAtom { get; set; }
    ICollection<AtomRelations> AtomRelationsTargetAtom { get; set; }
    ICollection<EventAtoms> EventAtoms { get; set; }
    ICollection<IngestionJobAtoms> IngestionJobAtoms { get; set; }
    ICollection<IngestionJobs> IngestionJobs { get; set; }
    ICollection<ModelLayers> ModelLayers { get; set; }
    ICollection<TenantAtoms> TenantAtoms { get; set; }
    ICollection<TensorAtomCoefficients> TensorAtomCoefficients { get; set; }
    ICollection<TensorAtoms> TensorAtoms { get; set; }
}
