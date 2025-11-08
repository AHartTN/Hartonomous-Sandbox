using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.Query;

public class FindRelatedAtomsRequest
{
    [Required]
    public long AtomId { get; set; }

    public string? RelationshipType { get; set; } // derives_from, similar_to, co_occurs_with, etc.

    [Range(1, 3)]
    public int MaxDepth { get; set; } = 1;

    [Range(1, 1000)]
    public int Limit { get; set; } = 50;

    public double? MinSimilarity { get; set; }
}
