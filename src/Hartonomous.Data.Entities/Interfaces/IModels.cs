using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IModels
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
    ICollection<AtomEmbeddings> AtomEmbeddings { get; set; }
    ICollection<AttentionGenerationLog> AttentionGenerationLog { get; set; }
    ICollection<AttentionInferenceResults> AttentionInferenceResults { get; set; }
    ICollection<CachedActivations> CachedActivations { get; set; }
    ICollection<Concepts> Concepts { get; set; }
    ICollection<GenerationStreams> GenerationStreams { get; set; }
    ICollection<InferenceCache> InferenceCache { get; set; }
    ICollection<InferenceRequests> InferenceRequests { get; set; }
    ICollection<InferenceSteps> InferenceSteps { get; set; }
    ICollection<ModelLayers> ModelLayers { get; set; }
    ICollection<ModelMetadata> ModelMetadata { get; set; }
    ICollection<ModelVersionHistory> ModelVersionHistory { get; set; }
    ICollection<TensorAtomCoefficients> TensorAtomCoefficients { get; set; }
    ICollection<TensorAtoms> TensorAtoms { get; set; }
    ICollection<TokenVocabulary> TokenVocabulary { get; set; }
    ICollection<TransformerInferenceResults> TransformerInferenceResults { get; set; }
    ICollection<WeightSnapshots> WeightSnapshots { get; set; }
}
