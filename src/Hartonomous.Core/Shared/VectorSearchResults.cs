namespace Hartonomous.Core.Shared;

/// <summary>
/// Result of a vector search operation
/// </summary>
public class VectorSearchResult
{
    public long AtomId { get; set; }
    public double Similarity { get; set; }
    public double SpatialDistance { get; set; }
    public byte[]? ContentHash { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// Result of a hybrid search operation
/// </summary>
public class HybridSearchResult
{
    public long AtomId { get; set; }
    public double VectorScore { get; set; }
    public double KeywordScore { get; set; }
    public double SpatialScore { get; set; }
    public double CombinedScore { get; set; }
    public byte[]? ContentHash { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// Result of an ensemble search operation
/// </summary>
public class EnsembleSearchResult
{
    public long AtomId { get; set; }
    public double Model1Score { get; set; }
    public double Model2Score { get; set; }
    public double Model3Score { get; set; }
    public double EnsembleScore { get; set; }
    public byte[]? ContentHash { get; set; }
    public string? ContentType { get; set; }
}
