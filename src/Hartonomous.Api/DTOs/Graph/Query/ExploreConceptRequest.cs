using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.Query;

public class ExploreConceptRequest
{
    [Required]
    public required string ConceptText { get; set; }

    public int? ModelId { get; set; }

    [Range(1, 1000)]
    public int TopK { get; set; } = 20;

    public double? MinSimilarity { get; set; } = 0.7;
}
