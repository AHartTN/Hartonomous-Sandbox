namespace Hartonomous.Api.DTOs;

public class SearchResponse
{
    public SearchResponse(List<SearchResultItem> results, int totalResults, double queryDuration)
    {
        Results = results;
        TotalResults = totalResults;
        QueryDuration = queryDuration;
    }
    
    public List<SearchResultItem> Results { get; set; }
    public int TotalResults { get; set; }
    public double QueryDuration { get; set; }
}

public class SearchResultItem
{
    public SearchResultItem(long atomId, long atomEmbeddingId, string? canonicalText, string? modality, double similarity, double? spatialDistance)
    {
        AtomId = atomId;
        AtomEmbeddingId = atomEmbeddingId;
        CanonicalText = canonicalText;
        Modality = modality;
        SimilarityScore = similarity;
        SpatialDistance = spatialDistance;
    }
    
    public long AtomId { get; set; }
    public long AtomEmbeddingId { get; set; }
    public string? CanonicalText { get; set; }
    public string? Modality { get; set; }
    public double SimilarityScore { get; set; }
    public double? SpatialDistance { get; set; }
    public string? ContentHash { get; set; }
}
