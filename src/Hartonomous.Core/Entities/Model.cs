namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents an AI model ingested into the system.
/// </summary>
public class Model
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public int ModelId { get; set; }
    
    /// <summary>
    /// Gets or sets the human-readable name of the model.
    /// </summary>
    public required string ModelName { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the model (e.g., 'transformer', 'cnn', 'diffusion').
    /// </summary>
    public required string ModelType { get; set; }
    
    /// <summary>
    /// Gets or sets the specific architecture variant (e.g., 'bert-base', 'gpt2-small').
    /// </summary>
    public string? Architecture { get; set; }
    
    /// <summary>
    /// Gets or sets the model configuration as JSON (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? Config { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of parameters in the model.
    /// </summary>
    public long? ParameterCount { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the model was ingested.
    /// </summary>
    public DateTime IngestionDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the timestamp of the last inference operation using this model.
    /// </summary>
    public DateTime? LastUsed { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of times this model has been used for inference.
    /// </summary>
    public long UsageCount { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the average inference time in milliseconds.
    /// </summary>
    public double? AverageInferenceMs { get; set; }

    /// <summary>
    /// Gets or sets the collection of layers in this model.
    /// </summary>
    public ICollection<ModelLayer> Layers { get; set; } = new List<ModelLayer>();
    
    /// <summary>
    /// Gets or sets the collection of inference requests that used this model.
    /// </summary>
    public ICollection<InferenceRequest> InferenceRequests { get; set; } = new List<InferenceRequest>();
    
    /// <summary>
    /// Gets or sets the extended metadata for this model.
    /// </summary>
    public ModelMetadata? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the collection of atom embeddings produced by this model.
    /// </summary>
    public ICollection<AtomEmbedding> AtomEmbeddings { get; set; } = new List<AtomEmbedding>();

    /// <summary>
    /// Gets or sets the collection of tensor atoms associated with this model.
    /// </summary>
    public ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();
}
