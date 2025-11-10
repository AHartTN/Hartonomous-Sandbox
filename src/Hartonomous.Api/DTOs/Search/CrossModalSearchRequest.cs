using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Search
{
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
}
