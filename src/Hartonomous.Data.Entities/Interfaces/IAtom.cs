using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Entities;

public interface IAtom
{
    long AtomId { get; set; }
    byte[] ContentHash { get; set; }
    string Modality { get; set; }
    string? Subtype { get; set; }
    string? SourceUri { get; set; }
    string? SourceType { get; set; }
    byte[]? AtomicValue { get; set; }
    string? CanonicalText { get; set; }
    string? Content { get; set; }
    string? ContentType { get; set; }
    string? PayloadLocator { get; set; }
    byte[]? ComponentStream { get; set; }
    string? Metadata { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime? UpdatedAt { get; set; }
    bool IsActive { get; set; }
    bool IsDeleted { get; set; }
    int TenantId { get; set; }
    long ReferenceCount { get; set; }
    Geometry? SpatialKey { get; set; }
    Geometry? SpatialGeography { get; set; }
    ICollection<AtomComposition> AtomCompositionComponentAtoms { get; set; }
    ICollection<AtomComposition> AtomCompositionSourceAtoms { get; set; }
    ICollection<AtomConcept> AtomConcepts { get; set; }
    ICollection<AtomEmbedding> AtomEmbeddings { get; set; }
    ICollection<AtomPayloadStore> AtomPayloadStores { get; set; }
    ICollection<AtomRelation> AtomRelationSourceAtoms { get; set; }
    ICollection<AtomRelation> AtomRelationTargetAtoms { get; set; }
    AtomsLob? AtomsLob { get; set; }
    ICollection<AudioFrame> AudioFrames { get; set; }
    ICollection<EventAtom> EventAtoms { get; set; }
    ICollection<ImagePatch> ImagePatches { get; set; }
    ICollection<IngestionJobAtom> IngestionJobAtoms { get; set; }
    ICollection<ModelLayer> ModelLayers { get; set; }
    ICollection<TenantAtom> TenantAtoms { get; set; }
    ICollection<TensorAtom> TensorAtoms { get; set; }
}
