namespace Hartonomous.Core.Entities;

/// <summary>
/// Catalog of model architectures with embedding dimensions and routing information.
/// Maps models to their dimension-specific weight storage tables.
/// </summary>
public class ModelArchitecture
{
    public int ModelId { get; set; }
    public string ModelName { get; set; } = null!;
    public string ModelType { get; set; } = null!;
    public int EmbeddingDimension { get; set; }
    public string WeightsTableName { get; set; } = null!;
    public int LayerCount { get; set; }
    public long? ParameterCount { get; set; }
    public string? ArchitectureConfig { get; set; } // JSON metadata
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public virtual ICollection<WeightCatalog> Weights { get; set; } = new List<WeightCatalog>();

    /// <summary>
    /// Determines the appropriate weights table based on embedding dimension.
    /// </summary>
    public static string GetWeightsTableName(int dimension)
    {
        return dimension switch
        {
            768 => "Weights_768",
            1536 => "Weights_1536",
            1998 => "Weights_1998",
            3996 => "Weights_3996",
            _ => throw new ArgumentException($"Unsupported embedding dimension: {dimension}. Supported: 768, 1536, 1998, 3996")
        };
    }

    /// <summary>
    /// Validates that dimension is supported.
    /// </summary>
    public static bool IsDimensionSupported(int dimension)
    {
        return dimension is 768 or 1536 or 1998 or 3996;
    }
}
