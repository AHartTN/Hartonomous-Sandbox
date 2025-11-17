using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IModel
{
    int ModelId { get; set; }
    string ModelName { get; set; }
    string ModelType { get; set; }
    string? ModelVersion { get; set; }
    string? Architecture { get; set; }
    string? Config { get; set; }
    long? ParameterCount { get; set; }
    DateTime IngestionDate { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? LastUsed { get; set; }
    long UsageCount { get; set; }
    double? AverageInferenceMs { get; set; }
    int TenantId { get; set; }
    bool IsActive { get; set; }
    string? MetadataJson { get; set; }
    byte[]? SerializedModel { get; set; }
    ICollection<AtomEmbedding> AtomEmbedding { get; set; }
    ICollection<AttentionGenerationLog> AttentionGenerationLog { get; set; }
    ICollection<AttentionInferenceResults> AttentionInferenceResults { get; set; }
    ICollection<CachedActivation> CachedActivation { get; set; }
    ICollection<Concepts> Concepts { get; set; }
    ICollection<GenerationStreams> GenerationStreams { get; set; }
    ICollection<InferenceCache> InferenceCache { get; set; }
    ICollection<InferenceRequest> InferenceRequest { get; set; }
    ICollection<InferenceStep> InferenceStep { get; set; }
    ICollection<ModelLayer> ModelLayer { get; set; }
    ICollection<ModelMetadata> ModelMetadata { get; set; }
    ICollection<ModelVersionHistory> ModelVersionHistory { get; set; }
    ICollection<TensorAtom> TensorAtom { get; set; }
    ICollection<TensorAtomCoefficient> TensorAtomCoefficient { get; set; }
    ICollection<TokenVocabulary> TokenVocabulary { get; set; }
    ICollection<TransformerInferenceResults> TransformerInferenceResults { get; set; }
    ICollection<WeightSnapshot> WeightSnapshot { get; set; }
}
