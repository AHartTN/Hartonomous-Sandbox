namespace Hartonomous.Api.DTOs;

public class EmbeddingResponse
{
    public EmbeddingResponse(long atomId, long? atomEmbeddingId, bool wasExisting, string? duplicateReason, double? semanticSimilarity)
    {
        AtomId = atomId;
        AtomEmbeddingId = atomEmbeddingId;
        WasExisting = wasExisting;
        DuplicateReason = duplicateReason;
        SemanticSimilarity = semanticSimilarity;
    }
    
    public long AtomId { get; set; }
    public long? AtomEmbeddingId { get; set; }
    public bool WasExisting { get; set; }
    public string? DuplicateReason { get; set; }
    public double? SemanticSimilarity { get; set; }
}
