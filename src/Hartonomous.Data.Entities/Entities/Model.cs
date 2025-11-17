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

    public virtual ICollection<AtomEmbedding> AtomEmbedding { get; set; } = new List<AtomEmbedding>();

    public virtual ICollection<AttentionGenerationLog> AttentionGenerationLog { get; set; } = new List<AttentionGenerationLog>();

    public virtual ICollection<AttentionInferenceResults> AttentionInferenceResults { get; set; } = new List<AttentionInferenceResults>();

    public virtual ICollection<CachedActivation> CachedActivation { get; set; } = new List<CachedActivation>();

    public virtual ICollection<Concepts> Concepts { get; set; } = new List<Concepts>();

    public virtual ICollection<GenerationStreams> GenerationStreams { get; set; } = new List<GenerationStreams>();

    public virtual ICollection<InferenceCache> InferenceCache { get; set; } = new List<InferenceCache>();

    public virtual ICollection<InferenceRequest> InferenceRequest { get; set; } = new List<InferenceRequest>();

    public virtual ICollection<InferenceStep> InferenceStep { get; set; } = new List<InferenceStep>();

    public virtual ICollection<ModelLayer> ModelLayer { get; set; } = new List<ModelLayer>();

    public virtual ICollection<ModelMetadata> ModelMetadata { get; set; } = new List<ModelMetadata>();

    public virtual ICollection<ModelVersionHistory> ModelVersionHistory { get; set; } = new List<ModelVersionHistory>();

    public virtual ICollection<TensorAtom> TensorAtom { get; set; } = new List<TensorAtom>();

    public virtual ICollection<TensorAtomCoefficient> TensorAtomCoefficient { get; set; } = new List<TensorAtomCoefficient>();

    public virtual ICollection<TokenVocabulary> TokenVocabulary { get; set; } = new List<TokenVocabulary>();

    public virtual ICollection<TransformerInferenceResults> TransformerInferenceResults { get; set; } = new List<TransformerInferenceResults>();

    public virtual ICollection<WeightSnapshot> WeightSnapshot { get; set; } = new List<WeightSnapshot>();
}
