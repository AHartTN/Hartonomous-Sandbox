namespace Hartonomous.Api.Controllers;

public class SearchStatistics
{
    public long CandidatesEvaluated { get; set; }
    public double IndexHitRate { get; set; }
    public double CacheHitRate { get; set; }
    public int AverageEmbeddingTime { get; set; }
}
