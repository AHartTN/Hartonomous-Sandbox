using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.SqlGraph;

/// <summary>
/// Request to find shortest path in SQL Server graph using SHORTEST_PATH
/// </summary>
public class SqlGraphShortestPathRequest
{
    [Required]
    public long StartAtomId { get; set; }

    [Required]
    public long EndAtomId { get; set; }

    public string? EdgeTypeFilter { get; set; }
}
