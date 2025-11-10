using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Analytics
{
    public class TopAtomsRequest
    {
        [Range(1, 1000)]
        public int TopK { get; set; } = 100;
        
        public string? Modality { get; set; }
        public string? OrderBy { get; set; } = "reference_count"; // reference_count, embedding_count, last_accessed
    }
}
