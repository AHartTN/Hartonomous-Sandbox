using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class Model : IModel
{
    public int ModelId { get; set; }

    public string ModelName { get; set; } = null!;

    public string ModelType { get; set; } = null!;

    public string? ModelVersion { get; set; }

    public string? Architecture { get; set; }

    public string? Config { get; set; }

    public long? ParameterCount { get; set; }

    public DateTime IngestionDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsed { get; set; }

    public long UsageCount { get; set; }

    public double? AverageInferenceMs { get; set; }

    public int TenantId { get; set; }

    public bool IsActive { get; set; }

    public string? MetadataJson { get; set; }

    public byte[]? SerializedModel { get; set; }

    public virtual ICollection<AtomEmbedding> AtomEmbeddings { get; set; } = new List<AtomEmbedding>();

    public virtual ICollection<AttentionGenerationLog> AttentionGenerationLogs { get; set; } = new List<AttentionGenerationLog>();

    public virtual ICollection<AttentionInferenceResult> AttentionInferenceResults { get; set; } = new List<AttentionInferenceResult>();

    public virtual ICollection<CachedActivation> CachedActivations { get; set; } = new List<CachedActivation>();

    public virtual ICollection<Concept> Concepts { get; set; } = new List<Concept>();

    public virtual ICollection<GenerationStream> GenerationStreams { get; set; } = new List<GenerationStream>();

    public virtual ICollection<InferenceCache> InferenceCaches { get; set; } = new List<InferenceCache>();

    public virtual ICollection<InferenceRequest> InferenceRequests { get; set; } = new List<InferenceRequest>();

    public virtual ICollection<InferenceStep> InferenceSteps { get; set; } = new List<InferenceStep>();

    public virtual ICollection<ModelLayer> ModelLayers { get; set; } = new List<ModelLayer>();

    public virtual ModelMetadatum? ModelMetadatum { get; set; }

    public virtual ICollection<ModelVersionHistory> ModelVersionHistories { get; set; } = new List<ModelVersionHistory>();

    public virtual ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();

    public virtual ICollection<TokenVocabulary> TokenVocabularies { get; set; } = new List<TokenVocabulary>();

    public virtual ICollection<TransformerInferenceResult> TransformerInferenceResults { get; set; } = new List<TransformerInferenceResult>();

    public virtual ICollection<WeightSnapshot> WeightSnapshots { get; set; } = new List<WeightSnapshot>();
}
