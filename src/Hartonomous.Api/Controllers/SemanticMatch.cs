namespace Hartonomous.Api.Controllers;

public class SemanticMatch
{
    public long AtomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public double Distance { get; set; }
    public string Method { get; set; } = string.Empty;
    public int EmbeddingDimensions { get; set; }
}
