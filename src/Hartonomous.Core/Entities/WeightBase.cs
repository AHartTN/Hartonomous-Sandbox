namespace Hartonomous.Core.Entities;

/// <summary>
/// Base class for dimension-specific weight tables.
/// Shared structure across Weights_768, Weights_1536, Weights_1998, Weights_3996.
/// </summary>
public abstract class WeightBase
{
    public long WeightId { get; set; }
    public int ModelId { get; set; }
    public int LayerIdx { get; set; }
    public string ComponentType { get; set; } = null!;
    public int? HeadIdx { get; set; }
    public int? FromPosition { get; set; }
    public int? ToPosition { get; set; }
    public float? ImportanceScore { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation
    public virtual ModelArchitecture? Model { get; set; }

    /// <summary>
    /// Abstract property for weight vector - implemented by derived classes
    /// with specific VECTOR dimensions.
    /// </summary>
    public abstract string WeightVectorJson { get; set; }

    /// <summary>
    /// Gets the actual embedding dimension for this weight type.
    /// </summary>
    public abstract int Dimension { get; }
}

/// <summary>
/// Weights with VECTOR(768) dimension.
/// Most common: BERT, GPT-2, sentence transformers.
/// </summary>
public class Weight768 : WeightBase
{
    /// <summary>
    /// Weight vector stored as JSON array format for SQL Server VECTOR(768) type.
    /// </summary>
    public override string WeightVectorJson { get; set; } = null!;

    public override int Dimension => 768;
}

/// <summary>
/// Weights with VECTOR(1536) dimension.
/// OpenAI embeddings and larger models.
/// </summary>
public class Weight1536 : WeightBase
{
    public override string WeightVectorJson { get; set; } = null!;

    public override int Dimension => 1536;
}

/// <summary>
/// Weights with VECTOR(1998) dimension.
/// Maximum float32 dimension in SQL Server 2025.
/// </summary>
public class Weight1998 : WeightBase
{
    public override string WeightVectorJson { get; set; } = null!;

    public override int Dimension => 1998;
}

/// <summary>
/// Weights with VECTOR(3996, float16) dimension.
/// Maximum float16 dimension in SQL Server 2025.
/// Requires PREVIEW_FEATURES enabled.
/// </summary>
public class Weight3996 : WeightBase
{
    public override string WeightVectorJson { get; set; } = null!;

    public override int Dimension => 3996;
}
