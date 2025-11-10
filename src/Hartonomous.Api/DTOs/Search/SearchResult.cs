namespace Hartonomous.Api.DTOs.Search
{
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
}
