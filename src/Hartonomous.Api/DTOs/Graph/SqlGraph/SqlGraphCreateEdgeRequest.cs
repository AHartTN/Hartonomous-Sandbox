using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Request to create an edge in SQL Server graph (graph.AtomGraphEdges)
/// </summary>
public class SqlGraphCreateEdgeRequest
{
    [Required]
    public long FromNodeId { get; set; }

    [Required]
    public long ToNodeId { get; set; }

    [Required]
    public required string EdgeType { get; set; } // 'DerivedFrom', 'ComponentOf', 'SimilarTo', etc.

    [Range(0.0, 1.0)]
    public double Weight { get; set; } = 1.0;

    public Dictionary<string, object>? Metadata { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
