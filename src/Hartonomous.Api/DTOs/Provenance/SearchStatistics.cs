namespace Hartonomous.Api.DTOs.Provenance;

public class SearchStatistics
{
    public long CandidatesEvaluated { get; set; }
    public double IndexHitRate { get; set; }
    public double CacheHitRate { get; set; }
    public long AverageEmbeddingTime { get; set; }
}
