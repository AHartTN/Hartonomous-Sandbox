using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Request to traverse SQL Server graph using MATCH syntax
/// </summary>
public class SqlGraphTraverseRequest
{
    [Required]
    public long StartAtomId { get; set; }

    public long? EndAtomId { get; set; }

    [Range(1, 5)]
    public int MaxDepth { get; set; } = 3;

    public string? EdgeTypeFilter { get; set; } // Optional: 'DerivedFrom', 'SimilarTo', etc.

    public string Direction { get; set; } = "outbound"; // outbound, inbound, both
}
