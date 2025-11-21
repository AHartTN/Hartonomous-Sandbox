using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

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
    ICollection<AtomEmbedding> AtomEmbeddings { get; set; }
    ICollection<AttentionGenerationLog> AttentionGenerationLogs { get; set; }
    ICollection<AttentionInferenceResult> AttentionInferenceResults { get; set; }
    ICollection<CachedActivation> CachedActivations { get; set; }
    ICollection<Concept1> Concept1s { get; set; }
    ICollection<GenerationStream> GenerationStreams { get; set; }
    ICollection<InferenceCache> InferenceCaches { get; set; }
    ICollection<InferenceRequest> InferenceRequests { get; set; }
    ICollection<InferenceStep> InferenceSteps { get; set; }
    ICollection<ModelLayer> ModelLayers { get; set; }
    ICollection<ModelMetadatum> ModelMetadata { get; set; }
    ICollection<ModelVersionHistory> ModelVersionHistories { get; set; }
    ICollection<TensorAtomCoefficient> TensorAtomCoefficients { get; set; }
    ICollection<TensorAtom> TensorAtoms { get; set; }
    ICollection<TokenVocabulary> TokenVocabularies { get; set; }
    ICollection<TransformerInferenceResult> TransformerInferenceResults { get; set; }
    ICollection<WeightSnapshot> WeightSnapshots { get; set; }
}
