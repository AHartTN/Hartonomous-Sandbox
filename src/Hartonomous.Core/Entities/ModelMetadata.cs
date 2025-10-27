namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents extended metadata for a model, including capabilities and performance characteristics.
/// </summary>
public class ModelMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for the metadata.
    /// </summary>
    public int MetadataId { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the associated model.
    /// </summary>
    public int ModelId { get; set; }
    
    /// <summary>
    /// Gets or sets the supported tasks as a JSON array (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? SupportedTasks { get; set; }
    
    /// <summary>
    /// Gets or sets the supported modalities as a JSON array (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? SupportedModalities { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum input sequence length.
    /// </summary>
    public int? MaxInputLength { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum output sequence length.
    /// </summary>
    public int? MaxOutputLength { get; set; }
    
    /// <summary>
    /// Gets or sets the dimensionality of embeddings produced by this model.
    /// </summary>
    public int? EmbeddingDimension { get; set; }
    
    /// <summary>
    /// Gets or sets the performance metrics as JSON (mapped to SQL Server 2025 JSON type).
    /// </summary>
    public string? PerformanceMetrics { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the training dataset.
    /// </summary>
    public string? TrainingDataset { get; set; }
    
    /// <summary>
    /// Gets or sets the date when the model was trained.
    /// </summary>
    public DateOnly? TrainingDate { get; set; }
    
    /// <summary>
    /// Gets or sets the license under which the model is distributed.
    /// </summary>
    public string? License { get; set; }
    
    /// <summary>
    /// Gets or sets the source URL where the model can be obtained.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Gets or sets the associated model.
    /// </summary>
    public Model Model { get; set; } = null!;
}
