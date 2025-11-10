using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search
{
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
}
