using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Request to create a node in SQL Server graph (graph.AtomGraphNodes)
/// </summary>
public class SqlGraphCreateNodeRequest
{
    [Required]
    public long AtomId { get; set; }

    [Required]
    public required string NodeType { get; set; } // 'Atom', 'Model', 'Concept', etc.

    public Dictionary<string, object>? Metadata { get; set; }

    public float? EmbeddingX { get; set; }
    public float? EmbeddingY { get; set; }
    public float? EmbeddingZ { get; set; }
}
