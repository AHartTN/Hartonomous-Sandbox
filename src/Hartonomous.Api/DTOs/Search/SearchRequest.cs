using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search;

public class HybridSearchRequest
{
    [Required]
    public required float[] QueryVector { get; set; }
    
    public double SpatialX { get; set; }
    public double SpatialY { get; set; }
    public double SpatialZ { get; set; }
    
    [Range(10, 10000)]
    public int SpatialCandidates { get; set; } = 100;
    
    [Range(1, 1000)]
    public int FinalTopK { get; set; } = 10;
    
    public string DistanceMetric { get; set; } = "cosine";
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }
}

public class HybridSearchResponse
{
    public required List<SearchResult> Results { get; set; }
    public int SpatialCandidatesFound { get; set; }
    public int FinalResults { get; set; }
}

public class CrossModalSearchRequest
{
    public string? QueryText { get; set; }
    public float[]? QueryEmbedding { get; set; }
    
    [Required]
    [MinLength(1)]
    public required List<string> TargetModalities { get; set; }
    
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;
    
    public string? DistanceMetric { get; set; }
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }
}

public class CrossModalSearchResponse
{
    public required List<SearchResult> Results { get; set; }
    public required string QueryModality { get; set; }
    public required List<string> TargetModalities { get; set; }
}

public class SearchResult
{
    public long AtomEmbeddingId { get; set; }
    public long AtomId { get; set; }
    public string? Modality { get; set; }
    public string? Subtype { get; set; }
    public string? SourceUri { get; set; }
    public string? SourceType { get; set; }
    public double Distance { get; set; }
    public double Similarity { get; set; }
    public double SpatialDistance { get; set; }
}
